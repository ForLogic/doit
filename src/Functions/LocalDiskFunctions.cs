using ICSharpCode.SharpZipLib.Zip;
using LumenWorks.Framework.IO.Csv;
using Microsoft.WindowsAzure.Storage;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace DoIt.Functions
{
	internal class LocalDiskFunctions : FunctionsNodeHandlerBase
	{
		public LocalDiskFunctions(XmlNode node):base(node){}

		public override bool Execute()
		{
			if (Node == null)
				return false;
			var lstNodes = Util.GetChildNodes(Node, "ListFiles", "MoveFile", "MoveFolder", "CopyFile", "DeleteFile", "DeleteFolder");
			foreach (var n in lstNodes)
				switch (n.Name.ToLower()){
					case "listfiles": ListFiles(n); break;
					case "movefile": MoveFile(n); break;
					case "movefolder": MoveFolder(n); break;
					case "copyfile": CopyFile(n); break;
					case "deletefile": DeleteFile(n); break;
					case "deletefolder": DeleteFolder(n); break;
				}
			return true;
		}

		// list files
		void ListFiles(XmlNode n){
			var to = Util.GetStr(n, "to");
			var path = Program.Shared.ReplaceTags(Util.GetStr(n, "path"));
			if (string.IsNullOrEmpty(path))
				return;
			var searchPattern = Util.GetStr(n, "searchPattern");
			var allDirectories = Util.GetStr(n, "allDirectories", "false") == "true";
			var fetchAttributes = Util.GetStr(n, "fetchAttributes", "false") == "true";
			var where = Program.Shared.ReplaceTags(Util.GetStr(n, "where"));
			var sort = Util.GetStr(n, "sort");
			var regex = Util.GetStr(n, "regex");
			var dt = new DataTable();
			dt.Columns.Add("full_path", typeof(string));
			dt.Columns.Add("directory", typeof(string));
			dt.Columns.Add("filename", typeof(string));
			dt.Columns.Add("extension", typeof(string));
			dt.Columns.Add("creation_time", typeof(DateTime));
			dt.Columns.Add("last_write_time", typeof(DateTime));
			dt.Columns.Add("length", typeof(long));
			if (Directory.Exists(path)){
				var lst = Directory.GetFiles(path, searchPattern, allDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
				if (!string.IsNullOrEmpty(regex))
					lst = lst.Where(file => Regex.IsMatch(file, regex)).ToArray();
				foreach (var file in lst){
					var lstData = new List<object>(){file, Path.GetDirectoryName(file), Path.GetFileName(file), Path.GetExtension(file)};
					if(fetchAttributes){
						var fi = new FileInfo(file);
						lstData.AddRange(new object[]{fi.CreationTime, fi.LastWriteTime, fi.Length});
					}
					dt.Rows.Add(lstData.ToArray());
				}
				if (!string.IsNullOrEmpty(where)||!string.IsNullOrEmpty(sort)){
					var lstRows = dt.Select(where, sort);
					var lstRowsToRemove = dt.Rows.Cast<DataRow>().Where(r => !lstRows.Contains(r)).ToList();
					foreach (var r in lstRowsToRemove)
						dt.Rows.Remove(r);
				}
			}
			lock (Program.Shared.LockDataTables){
				Program.Shared.DataTables[to+";"+Program.Shared.GetSequence()] = dt;
			}
		}

		// move file
		void MoveFile(XmlNode n){
			var path = Program.Shared.ReplaceTags(Util.GetStr(n, "path"));
			var to = Program.Shared.ReplaceTags(Util.GetStr(n, "to"));
			if (!string.IsNullOrEmpty(path) && File.Exists(path)){
				var dir = Path.GetDirectoryName(to);
				if (!Directory.Exists(dir))
					Directory.CreateDirectory(dir);
				File.Move(path, to);
			}
		}

		// move folder
		void MoveFolder(XmlNode n){
			var path = Program.Shared.ReplaceTags(Util.GetStr(n, "path"));
			var to = Program.Shared.ReplaceTags(Util.GetStr(n, "to"));
			if (!string.IsNullOrEmpty(path) && File.Exists(path))
				Directory.Move(path, to);
		}

		// copy file
		void CopyFile(XmlNode n){
			var path = Program.Shared.ReplaceTags(Util.GetStr(n, "path"));
			var to = Program.Shared.ReplaceTags(Util.GetStr(n, "to"));
			var overwrite = Util.GetStr(n, "overwrite", "true").ToLower() == "true";
			if (!string.IsNullOrEmpty(path) && File.Exists(path)){
				var folder = Path.GetDirectoryName(path);
				if (!Directory.Exists(folder))
					Directory.CreateDirectory(folder);
				File.Copy(path, to, overwrite);
			}
		}

		// delete file
		void DeleteFile(XmlNode n){
			var path = Program.Shared.ReplaceTags(Util.GetStr(n, "path"));
			if (!string.IsNullOrEmpty(path) && File.Exists(path))
				File.Delete(path);
		}

		// delete folder
		void DeleteFolder(XmlNode n){
			var path = Program.Shared.ReplaceTags(Util.GetStr(n, "path"));
			var recursive = Util.GetStr(n, "recursive", "false").ToLower() == "true";
			if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
				Directory.Delete(path, recursive);
		}
	}
}
