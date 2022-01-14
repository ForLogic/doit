using ICSharpCode.SharpZipLib.Zip;
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
	internal class SetValueFunctions : FunctionsNodeHandlerBase
	{
		public SetValueFunctions(XmlNode node):base(node){}

		public override bool Execute()
		{
			if (Node == null)
				return false;
			var lstNodes = Util.GetChildNodes(Node, "FileProp", "BlobProp", "Calc", "CalcDate", "String", "Date", "Coalesce");
			foreach (var n in lstNodes)
				switch (n.Name.ToLower()){
					case "fileprop": FileProp(n); break;
					case "blobprop": BlobProp(n); break;
					case "calc": Calc(n); break;
					case "calcdate": CalcDate(n); break;
					case "string": String(n); break;
					case "date": Date(n); break;
					case "coalesce": Coalesce(n); break;
				}
			return true;
		}

		// file properties
		void FileProp(XmlNode n){
			var path = Program.Shared.ReplaceTags(Util.GetStr(n, "path"));
			var prop = Util.GetStr(n, "prop", "");
			var to = Util.GetStr(n, "to");
			var length = 0L;
			var creation = null as DateTime?;
			var modification = null as DateTime?;
			var lstProps=prop.ToLower().Split(new string[]{",",";"," "}, StringSplitOptions.RemoveEmptyEntries);
			var lstTo=to.ToLower().Split(new string[]{",",";"," "}, StringSplitOptions.RemoveEmptyEntries);
			if (File.Exists(path)){
				var fi = new FileInfo(path);
				length = fi.Length;
				creation = fi.CreationTime;
				modification = fi.LastWriteTime;
			}
			for (var x=0;x<lstProps.Length && x<lstTo.Length;x++)
				switch (lstProps[x]){
					case "length": lock (Program.Shared.LockVariables) { Program.Shared.Variables[lstTo[x]+";"+Program.Shared.GetSequence()] = length; } break;
					case "creation": lock (Program.Shared.LockVariables) { Program.Shared.Variables[lstTo[x]+";"+Program.Shared.GetSequence()] = creation; } break;
					case "modification": lock (Program.Shared.LockVariables) { Program.Shared.Variables[lstTo[x]+";"+Program.Shared.GetSequence()] = modification; } break;
				}
			Program.Shared.WriteLogLine(string.Format("Values \"{0}\" set from file \"{1}\" to variables \"{2}\".", prop, path, to));
		}

		// blob properties
		void BlobProp(XmlNode n){
			var fromStorage = Util.GetStr(n, "fromStorage");
			var name = Program.Shared.ReplaceTags(Util.GetStr(n, "name"));
			var prop = Util.GetStr(n, "prop", "");
			var to = Util.GetStr(n, "to");
			if (!name.Contains("/")){
				lock (Program.Shared.LockVariables) { Program.Shared.Variables[to+";"+Program.Shared.GetSequence()] = null; }
				return;
			}
			var blobClient = CloudStorageAccount.Parse(Program.Shared.Storages[fromStorage]).CreateCloudBlobClient();
			var blobContainer = blobClient.GetContainerReference(name.Remove(name.IndexOf("/")));
			var blob = blobContainer.GetBlockBlobReference(name.Substring(name.IndexOf("/")+1));
			var length = 0L;
			var modification = null as DateTime?;
			if (blob.Exists()){
				blob.FetchAttributes();
				length = blob.Properties.Length;
				modification = blob.Properties.LastModified==null ? null : new Nullable<DateTime>(blob.Properties.LastModified.Value.DateTime);
			}
			var lstProps=prop.ToLower().Split(new string[]{",",";"," "}, StringSplitOptions.RemoveEmptyEntries);
			var lstTo=to.ToLower().Split(new string[]{",",";"," "}, StringSplitOptions.RemoveEmptyEntries);
			for (var x=0;x<lstProps.Length && x<lstTo.Length;x++)
				switch (lstProps[x]){
					case "length": lock (Program.Shared.LockVariables) { Program.Shared.Variables[lstTo[x]+";"+Program.Shared.GetSequence()] = length; } break;
					case "modification": lock (Program.Shared.LockVariables) { Program.Shared.Variables[lstTo[x]+";"+Program.Shared.GetSequence()] = modification; } break;
				}
			Program.Shared.WriteLogLine(string.Format("Values \"{0}\" set from blob \"{1}\" to variables \"{2}\".", prop, blob.Uri.ToString(), to));
		}

		// calc
		void Calc(XmlNode n){
			var operation = Util.GetStr(n, "operation", "+");
			var value1 = Program.Shared.ReplaceTags(Util.GetStr(n, "value1", "0"));
			var value2 = Program.Shared.ReplaceTags(Util.GetStr(n, "value2", "0"));
			var to = Util.GetStr(n, "to");
			var v1 = string.IsNullOrEmpty(value1) ? 0m : Convert.ToDecimal(value1);
			var v2 = string.IsNullOrEmpty(value2) ? 0m : Convert.ToDecimal(value2);
			var result = null as decimal?;
			switch (operation){
				case "+": result = v1 + v2; break;
				case "-": result = v1 - v2; break;
				case "*": result = v1 * v2; break;
				case "/": result = v1 / v2; break;
			}
			lock (Program.Shared.LockVariables){
				Program.Shared.Variables[to + ";" + Program.Shared.GetSequence()] = result;
			}
			Program.Shared.WriteLogLine(string.Format("Operation \"{0}\" to variable \"{1}\" with values \"{2}\" and \"{3}\" (result = {4})", operation, to, v1, v2, result));
		}

		// calc date
		void CalcDate(XmlNode n){
			var add = Program.Shared.ReplaceTags(Util.GetStr(n, "add", "0"));
			var operation = Util.GetStr(n, "operation", add.Trim().StartsWith("-") ? "-" : "+");
			var value = Program.Shared.ReplaceTags(Util.GetStr(n, "value"));
			var to = Util.GetStr(n, "to");
			var date = string.IsNullOrEmpty(value) ? DateTime.Now : Util.ParseDateTime(value);
			var result = null as DateTime?;
			switch (operation){
				case "+": result = Util.GetDateTime("+"+add.Trim(' ', '-', '+'), date); break;
				case "-": result = Util.GetDateTime("-"+add.Trim(' ', '-', '+'), date); break;
			}
			lock (Program.Shared.LockVariables){
				Program.Shared.Variables[to + ";" + Program.Shared.GetSequence()] = result;
			}
			Program.Shared.WriteLogLine(string.Format("Operation \"{0}\" to variable \"{1}\" with values \"{2}\" and \"{3}\" (result = \"{4}\")", operation, to, value, add, result == null ? "null" : Util.GetDateTimeString(result.Value)));
		}

		// string
		void String(XmlNode n){
			var value = Program.Shared.ReplaceTags(Util.GetStr(n, "value"));
			var to = Util.GetStr(n, "to");
			var regex = Util.GetStr(n, "regex");
			var matchIndex = Util.GetStr(n, "matchIndex");
			var regexFlags = Util.GetStr(n, "regexFlags");
			var regexGroup = Util.GetStr(n, "regexGroup");
			var isRegexGroupInt = !string.IsNullOrEmpty(regexGroup) && regexGroup.IsMatch("^\\d+$");
			var regexGroupInt = isRegexGroupInt ? Convert.ToInt32(regexGroup) : 0;
			var options = RegexOptions.None;
			if (!string.IsNullOrEmpty(regexFlags))
			{
				if (regexFlags.Contains("i"))
					options = options | RegexOptions.IgnoreCase;
				if (regexFlags.Contains("m"))
					options = options | RegexOptions.Multiline;
			}

			var mIndex = string.IsNullOrEmpty(matchIndex) || !matchIndex.IsMatch("^\\d+$") ? null : new int?(Convert.ToInt32(matchIndex));
			if (!string.IsNullOrEmpty(regex))
			{
				var lstMatches = Regex.Matches(value, regex, options);
				if (lstMatches.Count == 0 || !lstMatches.Cast<Match>().Any(m => m.Success))
					value = "";

				var index = -1;
				foreach (Match m in lstMatches)
                {
					if (!m.Success)
						continue;

					index++;
					if (mIndex != null && mIndex != index)
						continue;

					if (m.Groups.Count > 0)
					{
						var g = string.IsNullOrEmpty(regexGroup) ?
							m.Groups[0] :
							(isRegexGroupInt ? m.Groups[regexGroupInt] : m.Groups[regexGroup]);
						value = g?.Value ?? "";
					}
					else
						value = m.Value;
				}
            }
			lock (Program.Shared.LockVariables)
				Program.Shared.Variables[to+";"+Program.Shared.GetSequence()] = value;
			Program.Shared.WriteLogLine(string.Format("String variable named \"{0}\" was set to \"{1}\"", to, value));
		}

		// date
		void Date(XmlNode n){
			var value = Program.Shared.ReplaceTags(Util.GetStr(n, "value", "{now}"));
			var to = Util.GetStr(n, "to");
			var date = Util.ParseDateTime(value);
			lock (Program.Shared.LockVariables){
				Program.Shared.Variables[to+";"+Program.Shared.GetSequence()] = date;
			}
			Program.Shared.WriteLogLine(string.Format("Date variable named \"{0}\" was set to \"{1}\"", to, value));
		}

		// coalesce
		void Coalesce(XmlNode n)
		{
			var values = Program.Shared.ReplaceTags(Util.GetStr(n, "values"));
			var to = Util.GetStr(n, "to");
			var array = values?.Split(new string[]{",",";"},StringSplitOptions.RemoveEmptyEntries);
			var value = null as object;
			if (array != null)
				foreach (var k in array){
					value = Program.Shared.GetVariable(Program.Shared.ThreadID(), k);
					if (value != null)
						break;
				}
			lock (Program.Shared.LockVariables){
				Program.Shared.Variables[to+";"+Program.Shared.GetSequence()] = value;
			}
			Program.Shared.WriteLogLine(string.Format("Coalesce: variable named \"{0}\" was set to \"{1}\"", to, value));
		}
	}
}
