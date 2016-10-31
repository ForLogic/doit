using ICSharpCode.SharpZipLib.Zip;
using LumenWorks.Framework.IO.Csv;
using Microsoft.WindowsAzure.Storage;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DoIt.Functions
{
	internal class ProcessFunctions : FunctionsNodeHandlerBase
	{
		public ProcessFunctions(XmlNode node):base(node){}

		public override bool Execute()
		{
			if (Node == null)
				return false;
			var lstNodes = Util.GetChildNodes(Node, "Start", "Kill", "List");
			foreach (var n in lstNodes)
				switch (n.Name.ToLower()){
					case "start": Start(n); break;
					case "kill": Kill(n); break;
					case "list": List(n); break;
				}
			return true;
		}

		void Start(XmlNode n)
		{
			var path = Program.Shared.ReplaceTags(Util.GetStr(n, "path"));
			var args = Program.Shared.ReplaceTags(Util.GetStr(n, "args"));
			var wait = Util.GetStr(n, "wait", "true").ToLower() == "true";
			var timeSpan = Util.GetTimeSpan(Util.GetStr(n, "time"));
			var process = Process.Start(path, args);
			if (wait){
				var ms = Convert.ToInt32(timeSpan.TotalMilliseconds);
				if (ms > 0)
					process.WaitForExit(ms);
				else
					process.WaitForExit();
			}
		}

		void Kill(XmlNode n)
		{
			var id = Convert.ToInt32(Program.Shared.ReplaceTags(Util.GetStr(n, "id", "0")));
			var name = Program.Shared.ReplaceTags(Util.GetStr(n, "name"));
			if (id > 0)
				Process.GetProcessById(id);
			if (!string.IsNullOrEmpty(name)){
				var lst = Process.GetProcessesByName(name);
				foreach (var p in lst)
					p.Kill();
			}
		}

		void List(XmlNode n)
		{
			var name = Program.Shared.ReplaceTags(Util.GetStr(n, "name"));
			var regex = Program.Shared.ReplaceTags(Util.GetStr(n, "regex"));
			var machine = Program.Shared.ReplaceTags(Util.GetStr(n, "machine"));
			var to = Util.GetStr(n, "to");
			var lst = string.IsNullOrEmpty(name) ? 
				(string.IsNullOrEmpty(machine) ? Process.GetProcesses() : Process.GetProcesses(machine)):
				(string.IsNullOrEmpty(machine) ? Process.GetProcessesByName(name) : Process.GetProcessesByName(name, machine));
			if (!string.IsNullOrEmpty(regex))
				lst = lst.Where(p => p.ProcessName.IsMatch(regex)).ToArray();
			var dt = new DataTable();
			dt.Columns.Add("id", typeof(int));
			dt.Columns.Add("session_id", typeof(int));
			dt.Columns.Add("name", typeof(string));
			dt.Columns.Add("machine", typeof(string));
			dt.Columns.Add("start", typeof(DateTime));
			dt.Columns.Add("filename", typeof(string));
			foreach (var p in lst)
				dt.Rows.Add(p.Id, p.SessionId, p.ProcessName, p.MachineName, p.StartTime, p.MainModule.FileName);
			lock (Program.Shared.LockDataTables){
				Program.Shared.DataTables[to+";"+Program.Shared.GetSequence()] = dt;
			}
		}
	}
}
