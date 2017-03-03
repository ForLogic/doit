using ICSharpCode.SharpZipLib.Zip;
using LumenWorks.Framework.IO.Csv;
using Microsoft.WindowsAzure.Storage;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using static DoIt.Program;

namespace DoIt.Functions
{
	internal class MailFunctions : FunctionsNodeHandlerBase
	{
		public MailFunctions(XmlNode node):base(node){}

		public override bool Execute()
		{
			var smtp1 = Util.GetStr(Node, "smtp");
			var smtp2 = Util.GetStr(Node, "server");
			var smtp = "";
			if (!string.IsNullOrEmpty(smtp1))
				smtp = smtp1;
			else if (!string.IsNullOrEmpty(smtp2) && Shared.MailServers.ContainsKey(smtp2))
				smtp = Shared.MailServers[smtp2];
			if (string.IsNullOrEmpty(smtp))
				return false;
			var to = Util.GetStr(Node, "to");
			var host = Util.GetConfigData(smtp, "host");
			if (String.IsNullOrEmpty(to) || String.IsNullOrEmpty(host))
				return false;
			var subject = Util.GetStr(Node, "subject", "DoIt - Task Script").Trim();
			var body = Util.GetStr(Node, "Body");
			var from = Util.GetConfigData(smtp, "from");
			var port = Convert.ToInt32(Util.GetConfigData(smtp,"port",false,"25"));
			var ssl = Util.GetConfigData(smtp, "ssl") == "true";
			var user = Util.GetConfigData(smtp, "user");
			var pass = Util.GetConfigData(smtp, "pass");
			var mail = new MailMessage();
			var lstFilesSent = new List<String>();
			var lstFilesToDelete = new List<String>();
			var attachmentsNode = Util.GetChildNode(Node, "Attachments");
			if (attachmentsNode != null)
				foreach (XmlNode n in Util.GetChildNodes(attachmentsNode, "File", "SqlQuery")){
					var tagName = String.IsNullOrWhiteSpace(n.Name) ? null : n.Name.ToLower();
					var required = Util.GetStr(n, "required", "false").ToLower() == "true";
					var deleteSource = Util.GetStr(n, "deleteSource", "false") == "true";
					var zip = Util.GetStr(n, "zip", "false").ToLower() == "true";
					var filename = null as String;
					if (tagName == "file"){
						var path = Program.Shared.ReplaceTags(Util.GetStr(n, "path"));
						if (required && !File.Exists(path))
							continue;
						filename = Util.GetFileToSend(path, zip);
						if (zip)
							lstFilesToDelete.Add(filename);
						if (deleteSource)
							lstFilesToDelete.Add(path);
					}else if (tagName == "sqlquery"){
						var sql = n.InnerXml;
						var database = Util.GetStr(n, "database", Program.Shared.Databases.Keys.FirstOrDefault());
						var dt = Util.Select(sql, Program.Shared.Databases[database]);
						if (required && dt.Rows.Count == 0)
							continue;
						var dataFormat=Util.GetStr(n, "dataFormat", "csv").ToLower();
						if(!dataFormat.In("csv", "xml", "json"))
							dataFormat = "csv";
						var tempFile = Util.GetTempFileName(dataFormat);
						using (var fs = File.CreateText(tempFile))
							switch (dataFormat){
								case "csv": fs.Write(dt.ToCSV()); break;
								case "xml": fs.Write(dt.ToXML()); break;
								case "json": fs.Write(dt.ToJSON()); break;
							}
						filename = Util.GetFileToSend(tempFile, Util.GetStr(n, "zip", "false").ToLower() == "true");
						lstFilesToDelete.Add(tempFile);
						lstFilesToDelete.Add(filename);
					}
					lstFilesSent.Add(filename);
					var attachmentName = Util.GetStr(n, "attachmentName", Path.GetFileName(filename));
					var sr = new FileStream(filename, FileMode.Open, FileAccess.Read);
					mail.Attachments.Add(new Attachment(sr, attachmentName, Util.GetContentType(attachmentName)));
				}
			mail.From = new MailAddress(from);
			foreach (var m in to.Split(new String[]{",",";"," "}, StringSplitOptions.RemoveEmptyEntries))
				mail.To.Add(new MailAddress(m));
			mail.Subject = subject;
			mail.Body = body;
			var smtpClient = new SmtpClient(host, port);
			smtpClient.EnableSsl = ssl;
			smtpClient.Credentials = new NetworkCredential(user, pass);
			smtpClient.Send(mail);
			foreach (var at in mail.Attachments){
				at.ContentStream.Dispose();
				at.Dispose();
			}
			Program.Shared.WriteLogLine(String.Format("Mail Sent: {0}; To: {1}; {2};", subject, to, lstFilesSent.Count > 0 ? "Attachments: " + lstFilesSent.Concat(f => f) : "No Attachments"));
			foreach (var f in lstFilesToDelete)
				if (File.Exists(f))
					File.Delete(f);
			return true;
		}
	}
}
