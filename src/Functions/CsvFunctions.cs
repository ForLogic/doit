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
	internal class CsvFunctions : FunctionsNodeHandlerBase
	{
		public CsvFunctions(XmlNode node):base(node){}

		public override bool Execute()
		{
			if (Node == null)
				return false;
			var path = Program.Shared.ReplaceTags(Util.GetStr(Node, "path"));
			var separator = Util.GetStr(Node, "separator", ";");
			if (string.IsNullOrEmpty(path))
				return false;
			var lstNodes = Util.GetChildNodes(Node, "WriteLine", "WriteData", "Load");
			foreach (var n in lstNodes)
				switch (n.Name.ToLower()){
					case "writeline": WriteLine(path, separator, n); break;
					case "writedata": WriteData(path, separator, n); break;
					case "load": Load(path, separator, n); break;
				}
			return true;
		}

		// write line
		void WriteLine(string path, string separator, XmlNode n){
			var append = Util.GetStr(n, "append", "true").ToLower() == "true";
			var lstColumns = Util.GetChildNodes(n, "Column").Select(n2 => Program.Shared.ReplaceTags(n2.InnerXml)).ToArray();
			if (!Directory.Exists(Path.GetDirectoryName(path)))
				Directory.CreateDirectory(Path.GetDirectoryName(path));
			using (var fw = new FileStream(path, append ? FileMode.Append : FileMode.Create, FileAccess.Write))
			using (var sw = new StreamWriter(fw, Encoding.Default)){
				for (var x=0; x<lstColumns.Length; x++){
					sw.Write("\""+ (Program.Shared.ReplaceTags(lstColumns[x]) ?? "").Replace("\"","\"\"")+"\"");
					if (x < lstColumns.Length - 1)
						sw.Write(separator);
				}
				sw.WriteLine();
			}
		}

		// write data
		void WriteData(string path, string separator, XmlNode n){
			var data = Util.GetStr(n, "data");
			if (string.IsNullOrEmpty(data))
				return;
			var where = Program.Shared.ReplaceTags(Util.GetStr(n, "where"));
			var append = Util.GetStr(n, "append", "true").ToLower() == "true";
			var datatable = Program.Shared.GetDataTable(Program.Shared.ThreadID(), data);
			if (datatable == null)
				return;
			var lstColumns = Util.GetChildNodes(n, "Column").Select(n2 => new { Header = Util.GetStr(n2, "header"), Value = Program.Shared.ReplaceTags(n2.InnerXml) }).Where(n2 => !string.IsNullOrEmpty(n2.Value)).ToDictionary(n2 => n2.Header, n2 => n2.Value);
			var lastColumn = lstColumns.Keys.LastOrDefault();
			if (!Directory.Exists(Path.GetDirectoryName(path)))
				Directory.CreateDirectory(Path.GetDirectoryName(path));
			using (var fw = new FileStream(path, append ? FileMode.Append : FileMode.Create, FileAccess.Write))
			using (var sw = new StreamWriter(fw, Encoding.Default)){
				foreach (var c in lstColumns.Keys){
					sw.Write("\""+c.Replace("\"","\"\"")+"\"");
					if (c != lastColumn)
						sw.Write(separator);
				}
				sw.WriteLine();
				var lstRows = string.IsNullOrEmpty(where) ? datatable.Rows.Cast<DataRow>().ToArray() : datatable.Select(where);
				foreach (DataRow r in lstRows){
					foreach (var c in lstColumns.Keys){
						sw.Write("\""+Program.Shared.ReplaceTags(lstColumns[c], new Dictionary<string, DataRow>(){{data,r}}).Replace("\"","\"\"")+"\"");
						if (c != lastColumn)
							sw.Write(separator);
					}
					sw.WriteLine();
				}
			}
		}

		// load
		void Load(string path, string separator, XmlNode n){
			var to = Program.Shared.ReplaceTags(Util.GetStr(n, "to"));
			if (string.IsNullOrEmpty(to))
				return;
			var hasHeaders = Util.GetStr(n, "hasHeaders", "true").ToLower() == "true";
			var where = Program.Shared.ReplaceTags(Util.GetStr(n, "where"));
			var dt = new DataTable();
			using (var sr = new StreamReader(path, Encoding.Default))
			using (var csv = new CsvReader(sr, hasHeaders, separator[0])){
				if (hasHeaders)
					foreach (var h in csv.GetFieldHeaders())
						dt.Columns.Add(h);
				if (dt.Columns.Count == 0)
					for (var x = 0; x < csv.FieldCount; x++)
						dt.Columns.Add("Column" + (x+1));
				while (csv.ReadNextRecord()){
					var rowData = new string[csv.FieldCount];
					for (var x = 0; x < rowData.Length; x++)
						rowData[x] = csv[x];
					dt.Rows.Add(rowData);
				}
			}
			lock (Program.Shared.LockDataTables){
				Program.Shared.DataTables[to+";"+Program.Shared.GetSequence()] = dt;
			}
		}
	}
}
