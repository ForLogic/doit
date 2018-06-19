using ICSharpCode.SharpZipLib.Zip;
using LumenWorks.Framework.IO.Csv;
using Microsoft.WindowsAzure.Storage;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DoIt.Functions
{
	internal class SqlFunctions : FunctionsNodeHandlerBase
	{
		public SqlFunctions(XmlNode node):base(node){}

		public override bool Execute()
		{
			if (Node == null)
				return false;
			Action<SqlConnection, List<XmlNode>> runSQL = (SqlConnection conn, List<XmlNode> lstNodes) => {
				using (var trans = conn.BeginTransaction()){
					var commit = false;
					foreach (XmlNode n in lstNodes){
						var tagName = String.IsNullOrWhiteSpace(n.Name) ? null : n.Name.ToLower();
						if(tagName == "execute")
							commit = Execute(trans, n);
						else if(tagName == "scalar")
							commit = Scalar(trans, n);
						else if(tagName == "select")
							Select(trans, n);
					}
					if (commit) trans.Commit();
					else trans.Rollback();
				}
			};
			var database = Util.GetStr(Node, "database");
			using (var conn = new SqlConnection(Program.Shared.Databases[database])){
				conn.Open();
				runSQL(conn, Node.ChildNodes.Cast<XmlNode>().ToList());
				conn.Close();
			}
			foreach (XmlNode transNode in Node.ChildNodes.Cast<XmlNode>().Where(n2 => n2.Name.ToLower() == "trans")){
				var tDatabase = Util.GetStr(transNode, "database", database);
				using (var conn = new SqlConnection(Program.Shared.Databases[tDatabase])){
					conn.Open();
					runSQL(conn, transNode.ChildNodes.Cast<XmlNode>().ToList());
					conn.Close();
				}
			}
			return true;
		}

		void Select(SqlTransaction trans, XmlNode n)
		{
			var to = Util.GetStr(n, "to");
			var cmdNode = Util.GetChildNode(n, "Cmd");
			var parametersNode = Util.GetChildNode(n, "Params");
			var lstParameters = GetParameters(parametersNode);
			var sql = Program.Shared.ReplaceTags(cmdNode == null && parametersNode == null ? n.InnerText : cmdNode.InnerText);
			var timeout = Util.GetStr(n, "timeout");
			//Program.Shared.WriteLogLine("SQL Select: {0}", sql);
			var dt = Util.Select(sql, trans, lstParameters, string.IsNullOrEmpty(timeout) || !timeout.IsMatch("\\d+") ? null : new Nullable<int>(Convert.ToInt32(timeout)));
			lock (Program.Shared.LockDataTables){
				Program.Shared.DataTables[to + ";" + Program.Shared.GetSequence()] = dt;
			}
			//Program.Shared.WriteLogLine("SQL Select: {0} row(s) found", dt.Rows.Count);
		}

		bool Scalar(SqlTransaction trans, XmlNode n)
		{
			var to = Util.GetStr(n, "to");
			var cmdNode = Util.GetChildNode(n, "Cmd");
			var parametersNode = Util.GetChildNode(n, "Params");
			var lstParameters = GetParameters(parametersNode);
			var sql = Program.Shared.ReplaceTags(cmdNode == null && parametersNode == null ? n.InnerText : cmdNode.InnerText);
			var timeout = Util.GetStr(n, "timeout");
			//Program.Shared.WriteLogLine("SQL Scalar: {0}", sql);
			var obj = Util.Scalar(sql, trans, lstParameters, string.IsNullOrEmpty(timeout) || !timeout.IsMatch("\\d+") ? null : new Nullable<int>(Convert.ToInt32(timeout)));
			lock (Program.Shared.LockVariables){
				Program.Shared.Variables[to + ";" + Program.Shared.GetSequence()] = obj;
			}
			//Program.Shared.WriteLogLine("SQL Scalar: Value returned is \"{0}\"", obj == null || obj == DBNull.Value ? "null" : Convert.ToString(obj));
			return true;
		}

		bool Execute(SqlTransaction trans, XmlNode n)
		{
			var to = Util.GetStr(n, "to");
			var cmdNode = Util.GetChildNode(n, "Cmd");
			var parametersNode = Util.GetChildNode(n, "Params");
			var lstParameters = GetParameters(parametersNode);
			var sql = Program.Shared.ReplaceTags(cmdNode == null && parametersNode == null ? n.InnerText : cmdNode.InnerText);
			var timeout = Util.GetStr(n, "timeout");
			//Program.Shared.WriteLogLine("SQL Execute: {0}", sql);
			var rows = Util.Execute(sql, trans, lstParameters, string.IsNullOrEmpty(timeout) || !timeout.IsMatch("\\d+") ? null : new Nullable<int>(Convert.ToInt32(timeout)));
			if (!string.IsNullOrEmpty(to))
				lock (Program.Shared.LockVariables){
					Program.Shared.Variables[to + ";" + Program.Shared.GetSequence()] = rows;
				}
			//Program.Shared.WriteLogLine("SQL Execute: {0} row(s) affected", rows);
			return true;
		}

		SqlParameter[] GetParameters(XmlNode node)
		{
			if (node == null)
				return null;
			var lstNodes = Util.GetChildNodes(node);
			var lst = new List<SqlParameter>();
			foreach (var n in lstNodes){
				var type = Util.GetStr(n, "type");
				var str = Program.Shared.ReplaceTags(n.InnerText);
				var value = GetParameterValue(str, type);
				lst.Add(new SqlParameter(n.Name, value));
			}
			return lst.ToArray();
		}

		object GetParameterValue(string value, string type)
		{
			if (string.IsNullOrEmpty(value))
				return DBNull.Value;
			if (string.IsNullOrEmpty(type))
				return value;
			type = type.ToLower();
			if (type == "int")
				return Convert.ToInt32(value);
			if (type == "long")
				return Convert.ToInt64(value);
			if (type == "date" || type == "datetime")
				return Util.ParseDateTime(value);
			if (type == "datetimeoffset")
				return Util.ParseDateTimeOffset(value);
			return value;
		}
	}
}
