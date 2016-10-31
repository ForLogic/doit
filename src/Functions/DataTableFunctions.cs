using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DoIt.Functions
{
	internal class DataTableFunctions : FunctionsNodeHandlerBase
	{
		public DataTableFunctions(XmlNode node):base(node){}

		public override bool Execute()
		{
			if (Node == null)
				return false;
			var lstNodes = Util.GetChildNodes(Node, "Count", "Sum", "Avg", "Max", "Min", "SetRowValue", "GetDataRow", "Diff", "Join", "Intersect", "RemoveRows", "InsertRow");
			foreach (var n in lstNodes)
				switch (n.Name.ToLower()){
					case "count": Count(n); break;
					case "sum": Sum(n); break;
					case "avg": Avg(n); break;
					case "max": Max(n); break;
					case "min": Min(n); break;
					case "setrowvalue": SetRowValue(n); break;
					case "getdatarow": GetDataRow(n); break;
					case "diff": Diff(n); break;
					case "join": Join(n); break;
					case "intersect": Intersect(n); break;
					case "removerows": RemoveRows(n); break;
					case "insertrow": InsertRow(n); break;
				}
			return true;
		}

		// count
		void Count(XmlNode n){
			var data = Util.GetStr(n, "data");
			var where = Util.GetStr(n, "where");
			var to = Util.GetStr(n, "to");
			var dt = Program.Shared.GetDataTable(Program.Shared.ThreadID(), data);
			var count = string.IsNullOrEmpty(where) ? dt.Rows.Count : dt.Select(where).Length;
			lock (Program.Shared.LockVariables){
				Program.Shared.Variables[to+";"+ Program.Shared.GetSequence()] = count;
			}
			Program.Shared.WriteLogLine(string.Format("Count set from \"{0}\" where \"{1}\" to variable \"{2}\" (count = {3})", data, where, to, count));
		}

		// sum
		void Sum(XmlNode n){
			var data = Util.GetStr(n, "data");
			var column = Util.GetStr(n, "column");
			var where = Util.GetStr(n, "where");
			var to = Util.GetStr(n, "to");
			var dt = Program.Shared.GetDataTable(Program.Shared.ThreadID(), data);
			var lstRows = string.IsNullOrEmpty(where) ? dt.Rows.Cast<DataRow>().ToList() : dt.Select(where).ToList();
			var sum = lstRows.Where(r => r[column] != DBNull.Value).Sum(r => Convert.ToDecimal(r[column]));
			lock (Program.Shared.LockVariables){
				Program.Shared.Variables[to+";"+Program.Shared.GetSequence()] = sum;
			}
			Program.Shared.WriteLogLine(string.Format("Sum set from \"{0}.{1}\" where \"{2}\" to variable \"{3}\" (sum = {4}).", data, column, where, to, sum));
		}

		// avg
		void Avg(XmlNode n){
			var data = Util.GetStr(n, "data");
			var column = Util.GetStr(n, "column");
			var where = Util.GetStr(n, "where");
			var to = Util.GetStr(n, "to");
			var dt = Program.Shared.GetDataTable(Program.Shared.ThreadID(), data);
			var lstRows = string.IsNullOrEmpty(where) ? dt.Rows.Cast<DataRow>().ToList() : dt.Select(where).ToList();
			var avg = lstRows.Where(r => r[column] != DBNull.Value).Average(r => Convert.ToDecimal(r[column]));
			lock (Program.Shared.LockVariables){
				Program.Shared.Variables[to+";"+Program.Shared.GetSequence()] = avg;
			}
			Program.Shared.WriteLogLine(string.Format("Avg set from \"{0}.{1}\" where \"{2}\" to variable \"{3}\" (avg = {4})", data, column, where, to, avg));
		}

		// max
		void Max(XmlNode n){
			var data = Util.GetStr(n, "data");
			var column = Util.GetStr(n, "column");
			var where = Util.GetStr(n, "where");
			var to = Util.GetStr(n, "to");
			var dt = Program.Shared.GetDataTable(Program.Shared.ThreadID(), data);
			var lstRows = string.IsNullOrEmpty(where) ? dt.Rows.Cast<DataRow>().ToList() : dt.Select(where).ToList();
			var cType = dt.Columns[column].DataType.Name;
			var max = null as object;
			if (cType.In("Byte","Int16","Int32","Int64","Decimal","Single","Double"))
				max = lstRows.Where(r => r[column] != DBNull.Value).Max(r => Convert.ToDecimal(r[column]));
			else if (cType.In("DateTime"))
				max = lstRows.Where(r => r[column] != DBNull.Value).Max(r => Convert.ToDateTime(r[column]));
			else
				max = lstRows.Where(r => r[column] != DBNull.Value).Max(r => Convert.ToString(r[column]));
			lock (Program.Shared.LockVariables){
				Program.Shared.Variables[to+";"+Program.Shared.GetSequence()] = max;
			}
			Program.Shared.WriteLogLine(string.Format("Max set from \"{0}.{1}\" where \"{2}\" to variable \"{3}\" (max = {4})", data, column, where, to, max));
		}

		// min
		void Min(XmlNode n){
			var data = Util.GetStr(n, "data");
			var column = Util.GetStr(n, "column");
			var where = Util.GetStr(n, "where");
			var to = Util.GetStr(n, "to");
			var dt = Program.Shared.GetDataTable(Program.Shared.ThreadID(), data);
			var lstRows = string.IsNullOrEmpty(where) ? dt.Rows.Cast<DataRow>().ToList() : dt.Select(where).ToList();
			var cType = dt.Columns[column].DataType.Name;
			var min = null as object;
			if (cType.In("Byte","Int16","Int32","Int64","Decimal","Single","Double"))
				min = lstRows.Where(r => r[column] != DBNull.Value).Min(r => Convert.ToDecimal(r[column]));
			else if (cType.In("DateTime"))
				min = lstRows.Where(r => r[column] != DBNull.Value).Min(r => Convert.ToDateTime(r[column]));
			else
				min = lstRows.Where(r => r[column] != DBNull.Value).Min(r => Convert.ToString(r[column]));
			lock (Program.Shared.LockVariables){
				Program.Shared.Variables[to+";"+Program.Shared.GetSequence()] = min;
			}
			Program.Shared.WriteLogLine(string.Format("Min set from \"{0}.{1}\" where \"{2}\" to variable \"{3}\" (min = {4})", data, column, where, to, min));
		}

		// set row value
		void SetRowValue(XmlNode n){
			var data = Util.GetStr(n, "data");
			var where = Program.Shared.ReplaceTags(Util.GetStr(n, "where"));
			var lstColumns = Util.GetChildNodes(n, "Column").Select(n2 => new { Name = Util.GetStr(n2, "name"), Type = Util.GetStr(n2, "type"), Value = Program.Shared.ReplaceTags(n2.InnerXml) }).ToArray();
			var dt = Program.Shared.GetDataTable(Program.Shared.ThreadID(), data);
			var lstRows = string.IsNullOrEmpty(where) ? dt.Rows.Cast<DataRow>().ToArray() : dt.Select(where);
			foreach (var c in lstColumns){
				lock (Program.Shared.LockDataTables){
					if (!dt.Columns.Contains(c.Name))
						dt.Columns.Add(c.Name, Util.GetType(c.Type));
					foreach (DataRow r in lstRows)
						r[c.Name] = Util.GetValue(c.Value, c.Type);
				}
			}
		}

		// get datarow
		void GetDataRow(XmlNode n){
			var fromData = Util.GetStr(n, "fromData");
			var to = Util.GetStr(n, "to");
			var where = Program.Shared.ReplaceTags(Util.GetStr(n, "where"));
			var index = Util.GetStr(n, "index", "0");
			var lst = new Dictionary<string, object>();
			var dt = Program.Shared.GetDataTable(Program.Shared.ThreadID(), fromData);
			var rows = dt.Select(where);
			var x = index.ToLower() == "last" ? rows.Length - 1 : Convert.ToInt32(index);
			var r = rows.Length == 0 || x > rows.Length - 1 ? null : rows[x];
			foreach (DataColumn c in dt.Columns)
				lst[c.ColumnName] = r == null ? null : r[c];
			lock (Program.Shared.LockVariables){
				Program.Shared.Variables[to+";"+Program.Shared.GetSequence()] = lst;
			}
		}

		// diff
		void Diff(XmlNode n){
			var to = Util.GetStr(n, "to");
			var inData = Util.GetStr(n, "inData");
			var notInData = Util.GetStr(n, "notInData");
			var columns = Util.GetStr(n, "columns");
			var lstColumns = columns.Split(new char[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
			var inDT = Program.Shared.DataTables[inData + ";" + Program.Shared.GetSequence()];
			var notInDT = Program.Shared.DataTables[notInData + ";" + Program.Shared.GetSequence()];
			var dt = new DataTable();
			foreach (DataColumn c in inDT.Columns)
				dt.Columns.Add(c.ColumnName, c.DataType);
			var lstRows = inDT.Rows.Cast<DataRow>().Where(r1 => !notInDT.Rows.Cast<DataRow>().Any(r2 => lstColumns.All(c => Convert.ToString(r1[c]) == Convert.ToString(r2[c])))).ToList();
			foreach (DataRow r in lstRows)
				dt.Rows.Add(r.ItemArray);
			lock (Program.Shared.LockDataTables){
				Program.Shared.DataTables[to+";"+Program.Shared.GetSequence()] = dt;
			}
			Program.Shared.WriteLogLine("DataTable diff to \"{0}\" comparing columns \"{1}\", in data \"{2}\" / not in data \"{3}\" - Resulting Rows: {4}", to, columns, inData, notInData, dt.Rows.Count);
		}

		// join
		void Join(XmlNode n){
			var data = Util.GetStr(n, "data");
			var to = Util.GetStr(n, "to");
			var lstData = data.Split(new char[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
			var lstDT = new DataTable[lstData.Length];
			for (var x = 0; x < lstData.Length; x++)
				lstDT[x] = Program.Shared.GetDataTable(Program.Shared.ThreadID(), lstData[x]);
			var dt = new DataTable();
			foreach (DataTable d in lstDT)
				foreach (DataColumn c in d.Columns)
					if (!dt.Columns.Contains(c.ColumnName))
						dt.Columns.Add(c.ColumnName, c.DataType);
			foreach (DataTable d in lstDT)
				foreach (DataRow r in d.Rows){
					var newRow = dt.NewRow();
					foreach (DataColumn c in d.Columns)
						newRow[c.ColumnName] = r[c.ColumnName];
					dt.Rows.Add(newRow);
				}
			lock (Program.Shared.LockDataTables){
				Program.Shared.DataTables[to+";"+Program.Shared.GetSequence()] = dt;
			}
			Program.Shared.WriteLogLine("DataTable join to \"{0}\", from data \"{1}\" - Resulting Rows: {2}", to, data, dt.Rows.Count);
		}

		// intersect
		void Intersect(XmlNode n){
			var data = Util.GetStr(n, "data");
			var to = Util.GetStr(n, "to");
			var columns = Util.GetStr(n, "columns");
			var rowsFrom = Convert.ToInt32(Util.GetStr(n, "rowsFrom", "0"));
			var lstData = data.Split(new char[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
			var lstColumns = columns.Split(new char[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
			if (lstData.Length == 0 || lstColumns.Length == 0)
				return;
			var lstDT = new DataTable[lstData.Length];
			for (var x = 0; x < lstData.Length; x++)
				lstDT[x] = Program.Shared.GetDataTable(Program.Shared.ThreadID(), lstData[x]);
			var dt = new DataTable();
			foreach (DataColumn c in lstDT[rowsFrom].Columns)
				if (!dt.Columns.Contains(c.ColumnName))
					dt.Columns.Add(c.ColumnName, c.DataType);
			var lstRows = lstDT[rowsFrom].Rows.Cast<DataRow>().ToArray();
			foreach (var r in lstRows){
				var rowMatch = true;
				for (var x = 0; x < lstDT.Length; x++){
					if (x == rowsFrom)
						continue;
					if(!lstDT[x].Rows.Cast<DataRow>().Any(r2 => lstColumns.All(c => Convert.ToString(r2[c])== Convert.ToString(r[c])))){
						rowMatch = false;
						break;
					}
				}
				if (rowMatch){
					var newRow = dt.NewRow();
					foreach (DataColumn col in dt.Columns)
						newRow[col.ColumnName] = r[col.ColumnName];
					dt.Rows.Add(newRow);
				}
			}
			lock (Program.Shared.LockDataTables){
				Program.Shared.DataTables[to+";"+Program.Shared.GetSequence()] = dt;
			}
			Program.Shared.WriteLogLine("DataTable intersect to \"{0}\", from data \"{1}\" comparing columns \"{2}\" - Resulting Rows: {3}", to, data, columns, dt.Rows.Count);
		}

		// remove rows
		void RemoveRows(XmlNode n){
			var from = Util.GetStr(n, "from");
			if (string.IsNullOrEmpty(from))
				return;
			var where = Program.Shared.ReplaceTags(Util.GetStr(n, "where"));
			var dt = Program.Shared.GetDataTable(Program.Shared.ThreadID(), from);
			if (dt == null)
				return;
			if (string.IsNullOrEmpty(where)){
				dt.Rows.Clear();
				return;
			}
			var lstRows = dt.Select(where);
			foreach (var r in lstRows)
				dt.Rows.Remove(r);
		}

		// insert a new row
		void InsertRow(XmlNode n){
			var to = Util.GetStr(n, "to");
			if (string.IsNullOrEmpty(to))
				return;
			var lstColumns = Util.GetChildNodes(n, "Column").Select(n2 => new {Name=Util.GetStr(n2, "name"), Type=Util.GetStr(n2, "type"), Value=Program.Shared.ReplaceTags(n2.InnerXml)}).ToArray();
			var dt = Program.Shared.GetDataTable(Program.Shared.ThreadID(), to);
			if (dt == null)
				return;
			var r = dt.NewRow();
			foreach (var c in lstColumns){
				if (!dt.Columns.Contains(c.Name))
					dt.Columns.Add(c.Name, Util.GetType(c.Type));
				r[c.Name] = Util.GetValue(c.Value, c.Type);
			}
			dt.Rows.Add(r);
		}
	}
}
