using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Xml;
using DoIt.Functions;
using System.Diagnostics;
using System.Security.Principal;

namespace DoIt
{
	class Program
	{
		static Program()
		{
			Thread.CurrentThread.Name = "."+Shared.ThreadID();
		}

		static void Main(string[] args)
		{
			var configFile = Util.GetArg(args, "config") ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DoIt.config.xml");
			if (!File.Exists(configFile))
				return;

			// load xml
			var xml = Shared.Document = new XmlDocument();
			xml.PreserveWhitespace = true;
			xml.Load(configFile);
			var configNode = Util.GetChildNode(xml, "Configuration");
			if (configNode == null){
				Console.WriteLine("Configuration node not found.");
				return;
			}

			// settings
			var cryptKey = Util.GetArg(args, "cryptKey");
			LoadSettings(configNode, cryptKey);

			var version = Assembly.GetExecutingAssembly().GetName().Version;
			var user = WindowsIdentity.GetCurrent();
			var process = Process.GetCurrentProcess();
			var encryptionKey = Util.GetArg(args, "encryptionKey");
			var decryptionKey = Util.GetArg(args, "decryptionKey");
			if (!string.IsNullOrEmpty(encryptionKey) || !string.IsNullOrEmpty(decryptionKey)){
				LogStart(args, process, version, user);
				var lstConnStrNode = xml.SelectNodes("Configuration/Settings/ConnectionStrings");
				if (lstConnStrNode != null)
					foreach(XmlNode connStrNode in lstConnStrNode){
						foreach (XmlElement n in Util.GetChildNodes(connStrNode, "Database", "Storage", "MailServer", "SharedAccessSignature"))
							if (!string.IsNullOrEmpty(n.InnerXml)){
								if (!string.IsNullOrEmpty(encryptionKey) && Util.GetStr(n, "encrypted", "false").ToLower() != "true"){
									n.InnerXml = System.Security.SecurityElement.Escape(n.InnerXml.Encrypt(encryptionKey));
									n.SetAttribute("encrypted", "true");
								}
								if (!string.IsNullOrEmpty(decryptionKey) && Util.GetStr(n, "encrypted", "false").ToLower() == "true"){
									n.InnerXml = n.InnerXml.Decrypt(decryptionKey);
									n.RemoveAttribute("encrypted");
								}
							}
				}
				if (!string.IsNullOrEmpty(encryptionKey)){
					Shared.WriteLogLine("Configuration file was encrypted.");
					Console.WriteLine("Configuration file was encrypted.");
				}
				if (!string.IsNullOrEmpty(decryptionKey)){
					Shared.WriteLogLine("Configuration file was decrypted.");
					Console.WriteLine("Configuration file was decrypted.");
				}
				xml.Save(configFile);
				LogEnd(args, process, version, user);
				return;
			}

			// execute
			LogStart(args, process, version, user);
			Shared.ExecuteCommands(Util.GetChildNode(configNode, "Execute"));
			System.Threading.Tasks.Task.WaitAll(Shared.StorageUploadTasks.ToArray());
			LogEnd(args, process, version, user);
		}

		static void LogStart(string[] args, Process process, Version version, WindowsIdentity user)
		{
			Shared.WriteLogLine("*******************");
			Shared.WriteLogLine(String.Format("Application Started: \"{0}{1}\" v{2}{3}", process.ProcessName, args==null || args.Length==0 ? null : " " + args.Concat(arg => arg, " "), version.ToString(), user == null ? null : " (User: "+user.Name+")"));
		}

		static void LogEnd(string[] args, Process process, Version version, WindowsIdentity user)
		{
			Shared.WriteLogLine(String.Format("Application Finished: \"{0}\" v{1}{2}", process.ProcessName, version.ToString(), user == null ? null : " (User: " + user.Name + ")"));
			Shared.WriteLogLine();
			Shared.WriteLogLine();
		}

		static void LoadSettings(XmlNode configNode, string cryptKey)
		{
			var settingsNode = Util.GetChildNode(configNode, "Settings");
			if (settingsNode == null)
				return;

			// settings: logFile
			var logFile = Util.GetChildNode(settingsNode, "LogFile");
			if (logFile != null){
				Shared.LogFile = Shared.ReplaceTags(logFile.InnerText);
				var toVar = Util.GetStr(logFile, "toVar");
				if (!string.IsNullOrEmpty(toVar))
					lock (Shared.LockVariables) { Shared.Variables[toVar + ";" + Shared.GetSequence()] = Shared.LogFile; }
				if (!Directory.Exists(Path.GetDirectoryName(Shared.LogFile)))
					Directory.CreateDirectory(Path.GetDirectoryName(Shared.LogFile));
			}

			// settings: connectionStrings
			var connectionStrings = Util.GetChildNode(settingsNode, "ConnectionStrings");
			if (connectionStrings != null)
				foreach (XmlNode n in Util.GetChildNodes(connectionStrings, "Database", "Storage", "MailServer", "SharedAccessSignature")){
					switch (n.Name.ToLower()){
						case "database": Shared.Databases[Util.GetStr(n, "id", "1")] = string.IsNullOrEmpty(cryptKey) || string.IsNullOrEmpty(n.InnerText) || Util.GetStr(n, "encrypted", "false").ToLower()!="true" ? n.InnerText : new System.Security.SecurityElement("element", n.InnerXml.Decrypt(cryptKey)).Text; break;
						case "storage": Shared.Storages[Util.GetStr(n, "id", "1")] = string.IsNullOrEmpty(cryptKey) || string.IsNullOrEmpty(n.InnerText) || Util.GetStr(n, "encrypted", "false").ToLower()!="true" ? n.InnerText : new System.Security.SecurityElement("element", n.InnerXml.Decrypt(cryptKey)).Text; break;
						case "mailserver": Shared.MailServers[Util.GetStr(n, "id", "1")] = string.IsNullOrEmpty(cryptKey) || string.IsNullOrEmpty(n.InnerText) || Util.GetStr(n, "encrypted", "false").ToLower()!="true" ? n.InnerText : new System.Security.SecurityElement("element", n.InnerXml.Decrypt(cryptKey)).Text; break;
						case "sharedaccesssignature": Shared.SharedAccessSignatures[Util.GetStr(n, "id", "1")] = string.IsNullOrEmpty(cryptKey) || string.IsNullOrEmpty(n.InnerText) || Util.GetStr(n, "encrypted", "false").ToLower()!="true" ? n.InnerText : new System.Security.SecurityElement("element", n.InnerXml.Decrypt(cryptKey)).Text; break;
					}
				}
			

			// settings: exceptions
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
			var exceptions = Util.GetChildNode(settingsNode, "Exceptions");
			if (exceptions != null){
				var smtp1 = Util.GetStr(exceptions, "smtp");
				var smtp2 = Util.GetStr(exceptions, "mailServer");
				if (!string.IsNullOrEmpty(smtp1))
					Shared.Smtp = smtp1;
				else if (!string.IsNullOrEmpty(smtp2) && Shared.MailServers.ContainsKey(smtp2))
					Shared.Smtp = Shared.MailServers[smtp2];
				Shared.MailSubject = Util.GetStr(exceptions, "mailSubject");
				Shared.AttachLogFile = Util.GetStr(exceptions, "attachLogFile") == "true";
				foreach (XmlNode n in exceptions.SelectNodes("Mail"))
					Shared.Emails.Add(n.InnerText);
			}
		}

		static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			WriteExceptionData(e.ExceptionObject as Exception);
		}

		static void WriteExceptionData(Exception ex)
		{
			var str = String.Format("Exception: {0}", ex.GetFullMessage(true));
			Console.WriteLine(str);
			Shared.WriteLogLine(str);

			if (String.IsNullOrEmpty(Shared.Smtp) || Shared.Emails == null || Shared.Emails.Count == 0)
				return;
			var mail = new MailMessage();
			mail.From = new MailAddress(Util.GetConfigData(Shared.Smtp, "from"));
			foreach (var m in Shared.Emails)
				mail.To.Add(new MailAddress(m));
			mail.Subject = string.IsNullOrEmpty(Shared.MailSubject) ? "DoIt: " + ex.Message : Shared.MailSubject;
			mail.Body = str;
			var zipLogFile = null as String;
			if (Shared.AttachLogFile && File.Exists(Shared.LogFile)){
				zipLogFile = Util.GetFileToSend(Shared.LogFile, true);
				mail.Attachments.Add(new Attachment(zipLogFile));
			}
			var smtp = new SmtpClient(Util.GetConfigData(Shared.Smtp,"host"), Convert.ToInt32(Util.GetConfigData(Shared.Smtp,"port",false,"25")));
			smtp.EnableSsl = Util.GetConfigData(Shared.Smtp, "ssl") == "true";
			smtp.Credentials = new NetworkCredential(Util.GetConfigData(Shared.Smtp, "user"), Util.GetConfigData(Shared.Smtp, "pass"));
			smtp.Send(mail);
			if (!String.IsNullOrEmpty(zipLogFile) && File.Exists(zipLogFile))
				File.Delete(zipLogFile);
			Shared.WriteLogLine(String.Format("Exception Mail Sent To: {0}", Shared.Emails.Concat(m => m)));
		}

		internal static class Shared
		{
			public static String LogFile { get; set; }
			public static string Smtp { get; set; }
			public static string MailSubject { get; set; }
			public static bool AttachLogFile { get; set; }
			public static Dictionary<String, String> Storages { get; set; }
			public static Dictionary<String, String> Databases { get; set; }
			public static Dictionary<String, String> MailServers { get; set; }
			public static Dictionary<String, String> SharedAccessSignatures { get; set; }
			public static List<String> DbBackups { get; set; }
			public static List<String> ZipFiles { get; set; }
			public static List<string> Emails { get; set; }
			public static Dictionary<String, DataTable> DataTables { get; set; }
			public static Dictionary<String, DataRow> CurrentRows { get; set; }
			public static Dictionary<String, Object> Variables { get; set; }
			public static List<System.Threading.Tasks.Task> StorageUploadTasks { get; set; }
			public static int MainThreadID { get; set; }
			public static object LockDataTables { get; set; }
			public static object LockCurrentRows { get; set; }
			public static object LockVariables { get; set; }
			public static object LockSequence { get; set; }
			public static List<string> ThreadSequence { get; set; }
			public static bool IsLogEnabled { get; set; }
			public static XmlDocument Document { get; set; }

			static Shared()
			{
				Emails = new List<String>();
				Storages = new Dictionary<String, String>();
				Databases = new Dictionary<String, String>();
				MailServers = new Dictionary<String, String>();
				SharedAccessSignatures = new Dictionary<String, String>();
				DbBackups = new List<String>();
				ZipFiles = new List<String>();
				DataTables = new Dictionary<String, DataTable>();
				CurrentRows = new Dictionary<String, DataRow>();
				Variables = new Dictionary<String, Object>();
				StorageUploadTasks = new List<System.Threading.Tasks.Task>();
				LockDataTables = new object();
				LockCurrentRows = new object();
				LockVariables = new object();
				LockSequence = new object();
				MainThreadID = Shared.ThreadID();
				ThreadSequence = new List<string>(){Thread.CurrentThread.Name};
				IsLogEnabled = true;
			}

			public static bool ExecuteCommands(XmlNode node)
			{
				if (node == null)
					return false;
				foreach (XmlNode subNode in node.ChildNodes){
					if (subNode.Name == "#comment" || subNode.Name == "#whitespace")
                        continue;
					switch (subNode.Name.ToLower()){
						case "database": new DatabaseFunctions(subNode).Execute(); break;
						case "zip": new ZipFunctions(subNode).Execute(); break;
						case "process": new ProcessFunctions(subNode).Execute(); break;
						case "sql": new SqlFunctions(subNode).Execute(); break;
						case "mail": new MailFunctions(subNode).Execute(); break;
						case "condition": if (new ConditionFunctions(subNode).Execute()) return true; break;
						case "setvalue": new SetValueFunctions(subNode).Execute(); break;
						case "datatable": new DataTableFunctions(subNode).Execute(); break;
						case "foreach": new ForEachFunctions(subNode).Execute(); break;
						case "csv": new CsvFunctions(subNode).Execute(); break;
						case "log": new LogFunctions(subNode).Execute(); break;
						case "storage": new StorageFunctions(subNode).Execute(); break;
						case "localdisk": new LocalDiskFunctions(subNode).Execute(); break;
						case "sleep": new SleepFunctions(subNode).Execute(); break;
						case "try": new TryFunctions(subNode).Execute(); break;
						case "exception": new ExceptionFunctions(subNode).Execute(); break;
						case "http": new HttpFunctions(subNode).Execute(); break;
						case "stop": return true;
					}
				}
				return false;
			}

			public static void WriteLogLine()
			{
				WriteLogLine(false, null, null);
			}

			public static void WriteLogLine(String line = null, params object[] args)
			{
				WriteLogLine(true, line, args);
			}

			public static void WriteLogLine(bool addDateTimeInfo = true, String line = null, params object[] args)
			{
				if (!IsLogEnabled)
					return;
				using (var fs = new FileStream(LogFile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
				using (var sw = new StreamWriter(fs, Encoding.Default)) {
					if (addDateTimeInfo && !string.IsNullOrWhiteSpace(line))
						sw.Write(string.Format("[{0:yyyy-MM-dd HH:mm:ss.fff} - Thread {1}] ", DateTime.Now, ThreadID()));
					if (!string.IsNullOrEmpty(line) && args != null && args.Length > 0)
						line = string.Format(line, args);
					sw.WriteLine(line);
					sw.Flush();
				}
			}

			public static void WriteLog(String line = null, params object[] args)
			{
				WriteLog(true, line, args);
			}

			public static void WriteLog(bool addDateTimeInfo = true, String line = null, params object[] args)
			{
				if (!IsLogEnabled)
					return;
				using (var fs = new FileStream(LogFile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
				using (var sw = new StreamWriter(fs, Encoding.Default)){
					if (addDateTimeInfo && !string.IsNullOrWhiteSpace(line))
						sw.Write(string.Format("[{0:yyyy-MM-dd HH:mm:ss.fff} - Thread {1}] ", DateTime.Now, ThreadID()));
					if (!string.IsNullOrEmpty(line) && args != null && args.Length > 0)
						line = string.Format(line, args);
					sw.Write(line);
					sw.Flush();
				}
			}

			public static String ReplaceTags(String str, Dictionary<string, DataRow> lstCurrentRows = null)
			{
				if (String.IsNullOrEmpty(str))
					return null;
				str = str
					.Replace("%logFile%", LogFile)
					.Replace("%app%", Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory))
					.Replace("%programdata%", Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData))
					.Replace("%appdata%", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData))
					.Replace("%temp%", Path.GetDirectoryName(Path.GetTempPath()))
					.Replace("%windir%", Environment.GetFolderPath(Environment.SpecialFolder.Windows))
					.Replace("%windows%", Environment.GetFolderPath(Environment.SpecialFolder.Windows))
					.Replace("%system%", Environment.GetFolderPath(Environment.SpecialFolder.System))
					.Replace("%programs%", Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles))
					.Replace("%programfiles%", Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles))
					.Replace("%documents%", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments))
					.Replace("%desktop%", Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
				str = Util.GetStrData(str, "now", DateTime.Now);
				str = Util.GetStrData(str, "today", DateTime.Today);
				str = Util.GetStrData(str, "guid", Guid.NewGuid());
                str = Util.GetStrData(str, "rand", null);
                for (var x=0;x<DbBackups.Count;x++){
					str = str.Replace("%dbBackup" + (x+1) + "%", DbBackups[x]);
					str = str.Replace("%dbBackupFilename" + (x+1) + "%", Path.GetFileName(DbBackups[x]));
				}
				for (var x=0;x<ZipFiles.Count;x++){
					str = str.Replace("%zipFile" + (x+1) + "%", ZipFiles[x]);
					str = str.Replace("%zipFilename" + (x+1) + "%", Path.GetFileName(ZipFiles[x]));
				}
				Func<string, Dictionary<string, DataRow>, string> replaceRowsData = (string str2, Dictionary<string, DataRow> lstRows) => {
					foreach (var k in lstRows.Keys){
						var r = lstRows[k];
						var dt = GetDataTable(ThreadID(), k);
						var index = dt.Rows.IndexOf(r);
						str2 = Util.GetStrData(str2, k+".$RowsCount", dt.Rows.Count);
						str2 = Util.GetStrData(str2, k+".$RowIndex0", index);
						str2 = Util.GetStrData(str2, k+".$RowIndex1", index+1);
						foreach (DataColumn c in dt.Columns){
							str2 = Util.GetStrData(str2, k+"."+c.ColumnName, r[c.ColumnName]);
						}
					}
					return str2;
				};
				if (lstCurrentRows != null)
					str = replaceRowsData(str, GetCurrentRows(ThreadID(), lstCurrentRows));
				str = replaceRowsData(str, GetCurrentRows(ThreadID()));
				var lstVariables = GetVariables(ThreadID());
				foreach (var k in lstVariables.Keys){
					var v = lstVariables[k];
					if (v is Dictionary<string, object>){
						var lst = v as Dictionary<string, object>;
						foreach (var k2 in lst.Keys)
							str = Util.GetStrData(str, k+"."+k2, lst[k2]);
					}else
						str = Util.GetStrData(str, k, lstVariables[k]);
				}
				return str;
			}

			public static string GetSequence()
			{
				return Thread.CurrentThread.Name;
			}

			public static int ThreadID()
			{
				return Thread.CurrentThread.ManagedThreadId;
			}

			public static Dictionary<string, DataRow> GetCurrentRows(int threadID, Dictionary<string, DataRow> lstRows = null)
			{
				if (lstRows == null)
					lstRows = CurrentRows;
				var lst = new Dictionary<string,DataRow>();
				lock (LockCurrentRows){
					var sequence = GetSequence();
					foreach (var data in lstRows.Keys)
						if (!data.Contains(';'))
							lst[data] = lstRows[data];
						else {
							var s = data.Substring(data.IndexOf(';')+1);
							if (sequence == s || sequence.Contains(s))
								lst[data.Remove(data.LastIndexOf(';'))] = lstRows[data];
						}
				}
				return lst;
			}

			public static DataRow GetCurrentRow(int threadID, string data, Dictionary<string, DataRow> lstRows = null)
			{
				var lst = GetCurrentRows(threadID, lstRows);
				return lst.ContainsKey(data) ? lst[data] : null;
			}

			public static Dictionary<string, DataTable> GetDataTables(int threadID)
			{
				var lst = new Dictionary<string, DataTable>();
				lock (LockDataTables){
					var sequence = GetSequence();
					foreach (var data in DataTables.Keys)
						if (!data.Contains(';'))
							lst[data] = DataTables[data];
						else {
							var s = data.Substring(data.IndexOf(';')+1);
							if (sequence == s || sequence.Contains(s))
								lst[data.Remove(data.LastIndexOf(';'))] = DataTables[data];
						}
				}
				return lst;
			}

			public static DataTable GetDataTable(string data)
			{
				return GetDataTable(ThreadID(), data);
			}

			public static DataTable GetDataTable(int threadID, string data)
			{
				var lst = GetDataTables(threadID);
				return lst.ContainsKey(data) ? lst[data] : null;
			}

			public static Dictionary<string, object> GetVariables(int threadID)
			{
				var lst = new Dictionary<string, object>();
				lock (LockVariables){
					var sequence = GetSequence();
					foreach (var name in Variables.Keys)
						if (!name.Contains(';'))
							lst[name] = Variables[name];
						else {
							var s = name.Substring(name.IndexOf(';')+1);
							if (sequence == s || sequence.Contains(s))
								lst[name.Remove(name.LastIndexOf(';'))] = Variables[name];
						}
				}
				return lst;
			}

			public static object GetVariable(int threadID, string name)
			{
				var lst = GetVariables(threadID);
				return lst.ContainsKey(name) ? lst[name] : null;
			}
		}
	}
}
