using ICSharpCode.SharpZipLib.Zip;
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
	internal class ConditionFunctions : FunctionsNodeHandlerBase
	{
		public ConditionFunctions(XmlNode node):base(node){}

		public override bool Execute()
		{
			if (Node == null)
				return false;
			var type = Util.GetStr(Node, "type", "").ToLower();
			switch(type){
				case "has-disk-space": return HasDiskSpace(Node);
				case "file-exists": return FileExists(Node);
				case "folder-exists": return FolderExists(Node);
				case "has-rows": return HasRows(Node);
				case "is-datetime": return IsDateTime(Node);
				case "if": return If(Node);
			}
			return false;
		}


		// has disk space
		bool HasDiskSpace(XmlNode node){
			var min = Util.GetStr(node, "min", "0");
			if (!min.IsMatch("^\\d+$"))
				return false;
			var freeSpace = Util.GetFreeSpace(Util.GetStr(node, "drive", "C:\\"));
			var conditionNode = Util.GetChildNode(node, freeSpace / 1024 / 1024 >= Convert.ToInt64(min) ? "True" : "False");
			return Program.Shared.ExecuteCommands(conditionNode);
		}

		// file exists
		bool FileExists(XmlNode node){
			var path = Util.GetStr(node, "path", "");
			if (string.IsNullOrEmpty(path))
				return false;
			var conditionNode = Util.GetChildNode(node, File.Exists(Program.Shared.ReplaceTags(path)) ? "True" : "False");
			return Program.Shared.ExecuteCommands(conditionNode);
		}

		// folder exists
		bool FolderExists(XmlNode node){
			var path = Util.GetStr(node, "path", "");
			if (string.IsNullOrEmpty(path))
				return false;
			var conditionNode = Util.GetChildNode(node, Directory.Exists(Program.Shared.ReplaceTags(path)) ? "True" : "False");
			return Program.Shared.ExecuteCommands(conditionNode);
		}

		// has rows
		bool HasRows(XmlNode node){
			var data = Util.GetStr(node, "data");
			var dt = Program.Shared.GetDataTable(Program.Shared.ThreadID(), data);
			var conditionNode = Util.GetChildNode(node, dt != null && dt.Rows.Count > 0 ? "True" : "False");
			return Program.Shared.ExecuteCommands(conditionNode);
		}

		// is date/time
		bool IsDateTime(XmlNode node){
			var days = Util.GetStr(node, "days", "all");
			var time = Util.GetStr(node, "time");
			var conditionNode = Util.GetChildNode(node, Util.IsTodayInList(days) && Util.IsTimeToRun(time) ? "True" : "False");
			return Program.Shared.ExecuteCommands(conditionNode);
		}

		// if
		bool If(XmlNode node){
			var value1 = Program.Shared.ReplaceTags(Util.GetStr(node, "value1"));
			var value2 = Program.Shared.ReplaceTags(Util.GetStr(node, "value2"));
			var comparison = Util.GetStr(node, "comparison", "equals").ToLower();
			var valueType = Util.GetStr(node, "valueType", "string").ToLower();
			var result = false;
			if(valueType == "string"){
				switch (comparison){
					case "equals": result = string.Compare(value1, value2) == 0; break;
					case "less": result = string.Compare(value1, value2) < 0; break;
					case "greater": result = string.Compare(value1, value2) > 0; break;
				}
			}else if (valueType == "numeric"){
				var v1 = string.IsNullOrEmpty(value1) || !value1.IsNumber() ? null : new Nullable<decimal>(Convert.ToDecimal(value1));
				var v2 = string.IsNullOrEmpty(value2) || !value2.IsNumber() ? null : new Nullable<decimal>(Convert.ToDecimal(value2));
				switch (comparison){
					case "equals": result = v1 == v2; break;
					case "less": result = v1 < v2; break;
					case "greater": result = v1 > v2; break;
				}
			}else if (valueType == "date"){
				var v1 = Util.ParseDateTime(value1);
				var v2 = Util.ParseDateTime(value2);
				switch (comparison){
					case "equals": result = v1 == v2; break;
					case "less": result = v1 < v2; break;
					case "greater": result = v1 > v2; break;
				}
			}
			return Program.Shared.ExecuteCommands(Util.GetChildNode(node, result ? "True" : "False"));
		}
	}
}
