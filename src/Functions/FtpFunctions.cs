using ICSharpCode.SharpZipLib.Zip;
using LumenWorks.Framework.IO.Csv;
using Microsoft.WindowsAzure.Storage;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace DoIt.Functions
{
	internal class FtpFunctions : FunctionsNodeHandlerBase
	{
		public FtpFunctions(XmlNode node):base(node){}

		public override bool Execute()
		{
			if (Node == null)
				return false;
			var host = Program.Shared.ReplaceTags(Util.GetStr(Node, "host"));
			var port = Program.Shared.ReplaceTags(Util.GetStr(Node, "port", "21"));
			var user = Program.Shared.ReplaceTags(Util.GetStr(Node, "user"));
			var pass = Program.Shared.ReplaceTags(Util.GetStr(Node, "password"));
			var lstNodes = Util.GetChildNodes(Node, "List", "Download", "Upload", "CreateFolder", "DeleteFolder", "DeleteFile");
			foreach (var n in lstNodes)
				switch (n.Name.ToLower())
				{
					case "list": List(n, host, port, user, pass); break;
					case "download": Download(n, host, port, user, pass); break;
					case "upload": Upload(n, host, port, user, pass); break;
					case "createfolder": CreateFolder(n, host, port, user, pass); break;
					case "deletefolder": DeleteFolder(n, host, port, user, pass); break;
					case "deletefile": DeleteFile(n, host, port, user, pass); break;
				}
			return true;
		}

		void List(XmlNode n, string host, string port, string user, string pass)
		{
			var path = Program.Shared.ReplaceTags(Util.GetStr(n, "path"));
			var dt = GetList(host, port, user, pass, path);
			var to = Program.Shared.ReplaceTags(Util.GetStr(n, "to"));
			lock (Program.Shared.LockDataTables)
				Program.Shared.DataTables[to+";"+Program.Shared.GetSequence()] = dt;
		}

		void Download(XmlNode n, string host, string port, string user, string pass)
		{
			var path = Program.Shared.ReplaceTags(Util.GetStr(n, "path"));
			var toFile = Program.Shared.ReplaceTags(Util.GetStr(n, "toFile"));
			var rqt = GetFtpRequest(host, port, user, pass, WebRequestMethods.Ftp.DownloadFile, path);
			using (var rs = rqt.GetResponse() as FtpWebResponse)
			using (var stream = rs.GetResponseStream())
			using (var fs = new FileStream(toFile, FileMode.Create, FileAccess.Write))
				stream.CopyTo(fs);
		}

		void Upload(XmlNode n, string host, string port, string user, string pass)
		{
			var toPath = Program.Shared.ReplaceTags(Util.GetStr(n, "toPath"));
			var file = Program.Shared.ReplaceTags(Util.GetStr(n, "file"));
			var rqt = GetFtpRequest(host, port, user, pass, WebRequestMethods.Ftp.UploadFile, toPath);
			using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read))
			using (var rs = rqt.GetRequestStream())
				fs.CopyTo(rs);
		}

		void CreateFolder(XmlNode n, string host, string port, string user, string pass)
		{
			var path = Program.Shared.ReplaceTags(Util.GetStr(n, "path"));
			var lstPaths = path.Split('/', '\\');
			for (var x=0; x<lstPaths.Length; x++) {
				var basePath = lstPaths.Where((string str, int index) => index < x).Concat(str => str, "/") ?? "/";
				var newFolder = lstPaths[x];
				var dt = GetList(host, port, user, pass, basePath);
				var rows = dt.Select($"type='folder' and name='{newFolder}'");
				if (rows.Length > 0)
					continue;
				var rqt = GetFtpRequest(host, port, user, pass, WebRequestMethods.Ftp.MakeDirectory, basePath + "/" + newFolder);
				using (var rs = rqt.GetResponse() as FtpWebResponse)
					Program.Shared.WriteLogLine($"Ftp folder created: {path} - Response status code: {rs.StatusCode}");
			}
		}

		void DeleteFolder(XmlNode n, string host, string port, string user, string pass)
		{
			var path = Program.Shared.ReplaceTags(Util.GetStr(n, "path"));
			var rqt = GetFtpRequest(host, port, user, pass, WebRequestMethods.Ftp.RemoveDirectory, path);
			using (var rs = rqt.GetResponse() as FtpWebResponse)
				Program.Shared.WriteLogLine($"Ftp folder deleted: {path} - Response status code: {rs.StatusCode}");
		}

		void DeleteFile(XmlNode n, string host, string port, string user, string pass)
		{
			var path = Program.Shared.ReplaceTags(Util.GetStr(n, "path"));
			var rqt = GetFtpRequest(host, port, user, pass, WebRequestMethods.Ftp.DeleteFile, path);
			using (var rs = rqt.GetResponse() as FtpWebResponse)
				Program.Shared.WriteLogLine($"Ftp folder deleted: {path} - Response status code: {rs.StatusCode}");
		}

		DataTable GetList(string host, string port, string user, string pass, string path)
		{
			var rqt = GetFtpRequest(host, port, user, pass, WebRequestMethods.Ftp.ListDirectoryDetails, path);
			var str = null as string;
			using (var rs = rqt.GetResponse() as FtpWebResponse)
			using (var stream = rs.GetResponseStream())
			using (var sr = new StreamReader(stream))
				str = sr.ReadToEnd();

			var lst = str.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
			var pattern = @"^(?<datetime>\d+-\d+-\d+\s+\d+:\d+(?:AM|PM))\s+(?<length><DIR>|\d+)\s+(?<name>.+)$";
			var dt = new DataTable();
			dt.Columns.Add("name", typeof(string));
			dt.Columns.Add("type", typeof(string));
			dt.Columns.Add("length", typeof(long));
			dt.Columns.Add("datetime", typeof(DateTime));
			foreach (var item in lst){
				var m = Regex.Match(item, pattern);
				if (!m.Success)
					continue;
				var d = m.Groups["datetime"].Value.Replace("\t", " ").Replace("  ", " ");
				var l = m.Groups["length"].Value;

				var name = m.Groups["name"].Value;
				var type = l == "<DIR>" ? "folder" : "file";
				var len = l == "<DIR>" ? null : new Nullable<long>(Convert.ToInt64(l));
				var date = DateTime.ParseExact("08-10-18 06:57PM", "MM-dd-yy hh:mmtt", System.Globalization.CultureInfo.InvariantCulture);
				dt.Rows.Add(name, type, len, date);
			}
			return dt;
		}

		FtpWebRequest GetFtpRequest(string host, string port, string user, string pass, string method, string path)
		{
			var uri = GetUri(host, port, user, pass, path);
			var rqt = FtpWebRequest.Create(uri) as FtpWebRequest;
			rqt.Method = method;
			rqt.EnableSsl = host.ToLower().StartsWith("ftps:");
			rqt.Credentials = new NetworkCredential(user, pass);
			return rqt;
		}

		Uri GetUri(string host, string port, string user, string pass, string path)
		{
			var uri = new Uri(host);
			if (!string.IsNullOrEmpty(path))
			{
				var builder = new UriBuilder(uri);
				builder.Path = path;
				uri = builder.Uri;
			}
			if (!string.IsNullOrEmpty(port) && port.IsMatch("^\\d+$") && port != "21")
			{
				var value = Convert.ToInt32(port);
				var builder = new UriBuilder(uri);
				builder.Port = value;
				uri = builder.Uri;
			}
			if (!string.IsNullOrEmpty(uri.Scheme) && uri.Scheme.ToLower() == "ftps")
			{
				var builder = new UriBuilder(uri);
				builder.Scheme = "ftp";
				uri = builder.Uri;
			}
			return uri;
		}
	}
}
