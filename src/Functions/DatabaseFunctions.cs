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
			foreach (var n in Util.GetChildNodes(Node, "Backup")){
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
			return true;
		}
	}
}
