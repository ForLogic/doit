using ICSharpCode.SharpZipLib.Zip;
using LumenWorks.Framework.IO.Csv;
using Microsoft.WindowsAzure.Storage;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DoIt.Functions
{
	internal class DatabaseFunctions : FunctionsNodeHandlerBase
	{
		public DatabaseFunctions(XmlNode node):base(node){}

		public override bool Execute()
		{
			if (Node == null)
				return false;
			var id = Util.GetStr(Node, "id");
			var lstNodes = Util.GetChildNodes(Node, "Backup", "BackupLog");
			foreach (var n in lstNodes)
				switch (n.Name.ToLower())
				{
					case "backup": Backup(n, id); break;
					case "backuplog": BackupLog(n, id); break;
				}
			return true;
		}

		void Backup(XmlNode n, string id)
		{
			var toFile = Program.Shared.ReplaceTags(Util.GetStr(n, "toFile"));
			var type = Program.Shared.ReplaceTags(Util.GetStr(n, "type", "bacpac"));
			var connStr = Program.Shared.Databases[id];
			var dbName = Util.GetConfigData(connStr, "Initial Catalog");
			Program.Shared.WriteLogLine("Starting Database Backup.");
			if (type.ToLower() == "bacpac"){
				var ds = new Microsoft.SqlServer.Dac.DacServices(connStr);
				ds.ExportBacpac(toFile, dbName);
			}else if (type.ToLower() == "bak"){
				var timeout = Util.GetStr(n, "timeout", "1800").IsNumber()?Convert.ToInt32(Util.GetStr(n, "timeout", "1800")):1800;
				var withOptions = Util.GetStr(n, "withOptions");
				Util.Execute("backup database " + dbName + " to disk='" + toFile + "' " + withOptions, connStr, timeout);
			}
			Program.Shared.DbBackups.Add(toFile);
			var toVar = Util.GetStr(n, "toVar");
			if (!string.IsNullOrEmpty(toVar))
				lock (Program.Shared.LockVariables) { Program.Shared.Variables[toVar + ";" + Program.Shared.GetSequence()] = toFile; }
			Program.Shared.WriteLogLine(String.Format("Finished Database Backup (File: {0}; Size: {1}).", toFile, Util.GetFileSize(new FileInfo(toFile).Length)));
		}

		void BackupLog(XmlNode n, string id)
		{
			var toFile = Program.Shared.ReplaceTags(Util.GetStr(n, "toFile"));
			var connStr = Program.Shared.Databases[id];
			var dbName = Util.GetConfigData(connStr, "Initial Catalog");
			Program.Shared.WriteLogLine("Starting Database Log Backup.");
			var timeout = Util.GetStr(n, "timeout", "1800").IsNumber()?Convert.ToInt32(Util.GetStr(n, "timeout", "1800")):1800;
			var withOptions = Util.GetStr(n, "withOptions");
			Util.Execute("backup log " + dbName + " to disk='" + toFile + "' " + withOptions, connStr, timeout);
			Program.Shared.DbBackups.Add(toFile);
			var toVar = Util.GetStr(n, "toVar");
			if (!string.IsNullOrEmpty(toVar))
				lock (Program.Shared.LockVariables) { Program.Shared.Variables[toVar + ";" + Program.Shared.GetSequence()] = toFile; }
			Program.Shared.WriteLogLine(String.Format("Finished Database Log Backup (File: {0}; Size: {1}).", toFile, Util.GetFileSize(new FileInfo(toFile).Length)));
		}
	}
}
