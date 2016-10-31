using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DoIt
{
	public static class Extensions
	{
		public static Boolean IsMatch(this String str, String pattern)
		{
			if (String.IsNullOrEmpty(str))
				return false;
			return Regex.IsMatch(str, pattern, RegexOptions.IgnoreCase);
		}

		public static Boolean IsNumber(this Object obj)
		{
			if (obj is Int16 || obj is Int32 || obj is Int64)
				return true;
			if (obj is Decimal || obj is Single || obj is Double)
				return true;
			if (obj is String && !String.IsNullOrEmpty(obj as String) && (obj as String).IsMatch("^((\\+|\\-)?[0-9]+([0-9]*(\\.|\\" + NumberFormatInfo.CurrentInfo.NumberDecimalSeparator + ")?[0-9]+)?)?$"))
				return true;
			return false;
		}

		public static String Concat<t>(this IEnumerable<t> lst, Func<t, Object> valueFunction, String separator = ", ", String format = null, String defaultValue = null, Boolean distinct = true)
		{
			if (lst == null || lst.Count() == 0)
				return defaultValue;
			var newlst = (distinct ? lst.Select(item => valueFunction(item)).Distinct() : lst.Select(item => valueFunction(item))).ToList();
			var str = new StringBuilder();
			foreach (var value in newlst)
			{
				var valuestr = Convert.ToString(value);
				if (!String.IsNullOrEmpty(valuestr))
					str.Append(String.IsNullOrEmpty(format) ? valuestr + separator : String.Format("{0:" + format + "}", value) + separator);
			}
			return String.IsNullOrEmpty(str.ToString()) ? null : str.Remove(str.Length - separator.Length, separator.Length).ToString();
		}

		public static String GetFullMessage(this Exception ex, Boolean stackTrace = true, Int32 counter = 1)
		{
			if (ex == null)
				return "";

			var msg = "";
			if (ex.InnerException != null)
			{
				msg += GetFullMessage(ex.InnerException, stackTrace, counter + 1);
				msg += Environment.NewLine + Environment.NewLine;
			}
			msg += counter + " - " + ex.GetType().FullName + ": " + ex.Message + Environment.NewLine;
			if (stackTrace)
				msg += ex.StackTrace;
			return msg;
		}

		static String ReplaceAll(this String str, List<Char> oldChars, List<Char> newChars)
		{
			if (String.IsNullOrEmpty(str) || oldChars == null || newChars == null)
				return str;
			var builder = new StringBuilder(str);
			foreach (var c in oldChars)
				builder.Replace(c, newChars[oldChars.FindIndex(cc => cc == c)]);
			return builder.ToString();
		}

		public static int Count(this string str, string strToCount)
		{
			var index = str.IndexOf(strToCount);
			var count = 0;
			while (index != -1){
				count++;
				str = str.Remove(index, strToCount.Length);
				index = str.IndexOf(strToCount, index);
			}
			return count;
		}

		public static String RemoveAccents(this String str)
		{
			if (String.IsNullOrEmpty(str))
				return str;

			String lst1 = "áéíóúàèìòùäëïöüãõâêîôûçÁÉÍÓÚÀÈÌÒÙÄËÏÖÜÃÕÂÊÎÔÛÇ";
			String lst2 = "aeiouaeiouaeiouaoaeioucAEIOUAEIOUAEIOUAOAEIOUC";
			return str.ReplaceAll(lst1.ToCharArray().ToList(), lst2.ToCharArray().ToList());
		}

		public static String GetFileName(this String str, Boolean toLower = false)
		{
			if (String.IsNullOrEmpty(str))
				return str;
			var filename = str.RemoveAccents().OnlyChars("0123456789abcdefghijklmnopqrstuvxwyzABCDEFGHIJKLMNOPQRSTUVXWYZ_- ().", "_");
			return toLower? filename.ToLower() : filename;
		}

		public static String GetFileExtension(this String str, Boolean toLower = false)
		{
			if (String.IsNullOrEmpty(str))
				return null;
			var index = str.LastIndexOf('.');
			if (index == -1)
				return null;
			var extension = str.Substring(index);
			return toLower ? extension.ToLower() : extension;
		}

		public static String OnlyChars(this String str, String charListToKeep, String replaceFor = null)
		{
			if (String.IsNullOrEmpty(str))
				return str;
			var newStr = new StringBuilder();
			foreach (var c in str)
				if (charListToKeep.Any(cc => cc == c))
					newStr.Append(c);
				else if (!String.IsNullOrEmpty(replaceFor))
					newStr.Append(replaceFor);
			return newStr.ToString();
		}

		public static String OnlyNumbers(this String str)
		{
			return str.OnlyChars("0123456789");
		}

		public static String OnlyChars(this String str)
		{
			return str.OnlyChars("abcdefghijklmnopqrstuvxwyzABCDEFGHIJKLMNOPQRSTUVXWYZ");
		}

		public static String OnlyCharsAndNumbers(this String str)
		{
			return str.OnlyChars("0123456789abcdefghijklmnopqrstuvxwyzABCDEFGHIJKLMNOPQRSTUVXWYZ");
		}

		public static StringBuilder Append(this StringBuilder sb, params String[] strValues)
		{
			if (strValues == null || strValues.Length == 0)
				return sb;
			foreach (var str in strValues)
				sb.Append(str);
			return sb;
		}

		public static StringBuilder AppendLine(this StringBuilder sb, params Object[] strValues)
		{
			if (strValues == null || strValues.Length == 0)
				return sb;
			foreach (var str in strValues)
				sb.Append(str);
			sb.AppendLine();
			return sb;
		}

		public static Boolean In<T>(this T obj, params T[] values)
		{
			if (obj == null || values == null || values.Length == 0)
				return false;
			foreach (var v in values)
				if (obj.Equals(v))
					return true;
			return false;
		}

		public static bool Remove<T>(this List<T> list, Func<T,bool> func)
		{
			if (list == null || list.Count == 0)
				return false;
			foreach (var obj in list)
				if(func(obj)){
					list.Remove(obj);
					return true;
				}
			return false;
		}

		public static bool RemoveKey<T1,T2>(this Dictionary<T1,T2> list, T1 key)
		{
			if (list == null || list.Count == 0)
				return false;
			if (!list.ContainsKey(key))
				return false;
			list.Remove(key);
			return true;
		}

		#region datatable serializers
		static DataColumn[] GetColumns(DataTable dt, params string[] columns)
		{
			if (dt == null)
				return new DataColumn[0];
			return columns == null || columns.Length == 0 ?
				dt.Columns.Cast<DataColumn>().ToArray():
				dt.Columns.Cast<DataColumn>().Where(c1 => columns.Any(c2 => c2.ToLower() == c1.ColumnName.ToLower())).ToArray();
		}

		static string[] GetColumns(DataRow row, params string[] columns)
		{
			if (row == null)
				return new string[0];
			if(row.Table == null){
				var rs = new string[row.ItemArray.Length];
				for(var x=0; x<rs.Length; x++)
					rs[x] = "column"+x;
				return rs;
			}
			return columns == null || columns.Length == 0 ?
				row.Table.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray() :
				row.Table.Columns.Cast<DataColumn>().Where(c1 => columns.Any(c2 => c2.ToLower() == c1.ColumnName.ToLower())).Select(c => c.ColumnName).ToArray();
		}

		public static String ToCSV(this DataRow row, params string[] columns)
		{
			var lstColumns = GetColumns(row, columns);
			var sb = new StringBuilder();
			for (var x = 0; x < lstColumns.Length; x++)
				sb.Append("\"" + lstColumns[x], x < lstColumns.Length - 1 ? "\";" : "\"");
			sb.AppendLine();
			for (var y = 0; y < lstColumns.Length; y++)
				sb.Append("\"" + GetValueToCSV(row[y]), y < lstColumns.Length - 1 ? "\";" : "\"");
			sb.AppendLine();
			return sb.ToString();
		}

		public static String ToCSV(this DataTable dt, params string[] columns)
		{
			var lstColumns = GetColumns(dt, columns);
			var sb = new StringBuilder();
			for (var x = 0; x < lstColumns.Length; x++)
				sb.Append("\"" + lstColumns[x].ColumnName, x < lstColumns.Length - 1 ? "\";" : "\"");
			sb.AppendLine();
			for (var x = 0; x < dt.Rows.Count; x++){
				for (var y = 0; y < lstColumns.Length; y++)
					sb.Append("\"" + GetValueToCSV(dt.Rows[x][y]), y < lstColumns.Length - 1 ? "\";" : "\"");
				sb.AppendLine();
			}
			return sb.ToString();
		}

		public static String ToJSON(this DataRow row, params string[] columns)
		{
			var lstColumns = GetColumns(row, columns);
			var sb = new StringBuilder();
			sb.Append("{");
			for (var y = 0; y < lstColumns.Length; y++)
				sb.Append("\"", lstColumns[y], "\":", GetValueToJS(row[y]), y < lstColumns.Length - 1 ? "," : "");
			sb.AppendLine("}");
			return sb.ToString();
		}

		public static String ToJSON(this DataTable dt, params string[] columns)
		{
			var lstColumns = GetColumns(dt, columns);
			var sb = new StringBuilder();
			sb.AppendLine("[");
			for (var x = 0; x < dt.Rows.Count; x++){
				sb.Append("{");
				for (var y = 0; y < lstColumns.Length; y++)
					sb.Append("\"", lstColumns[y].ColumnName, "\":", GetValueToJS(dt.Rows[x][y]), y < lstColumns.Length - 1 ? "," : "");
				sb.Append("}", x < dt.Rows.Count - 1 ? "," : "");
				sb.AppendLine();
			}
			sb.AppendLine("]");
			return sb.ToString();
		}

		public static String ToXML(this DataRow row, params string[] columns)
		{
			var lstColumns = GetColumns(row, columns);
			var sb = new StringBuilder();
			sb.AppendLine("<data>");
			for (var y = 0; y < lstColumns.Length; y++){
				var value = row[y];
				if (value == DBNull.Value || value == null)
					sb.AppendLine("<", lstColumns[y], "/>");
				else
					sb.Append("<", lstColumns[y], ">", GetValueToXML(value), "</", lstColumns[y], ">");
			}
			sb.AppendLine("</data>");
			return sb.ToString();
		}

		public static String ToXML(this DataTable dt, params string[] columns)
		{
			var lstColumns = GetColumns(dt, columns);
			var sb = new StringBuilder();
			sb.AppendLine("<data>");
			for (var x = 0; x < dt.Rows.Count; x++){
				sb.AppendLine("<row>");
				for (var y = 0; y < lstColumns.Length; y++){
					var value = dt.Rows[x][y];
					if (value == DBNull.Value || value == null)
						sb.AppendLine("<", lstColumns[y].ColumnName, "/>");
					else
						sb.AppendLine("<", lstColumns[y].ColumnName, ">", GetValueToXML(value), "</", lstColumns[y].ColumnName, ">");
				}
				sb.AppendLine("</row>");
			}
			sb.AppendLine("</data>");
			return sb.ToString();
		}

		static String GetValueToCSV(Object data)
		{
			if (data == null || data == DBNull.Value)
				return null;
			if (data is Int16 || data is Int32 || data is Int64 || data is Nullable<Int16> || data is Nullable<Int32> || data is Nullable<Int64>)
				return Convert.ToString(data);
			if (data is Decimal || data is Single || data is Double || data is Nullable<Decimal> || data is Nullable<Single> || data is Nullable<Double>)
				return Convert.ToString(Convert.ToDecimal(Convert.ToDouble(data))).Replace(System.Globalization.NumberFormatInfo.CurrentInfo.NumberGroupSeparator, "").Replace(System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator, ".");
			if (data is DateTime || data is Nullable<DateTime>){
				var date = Convert.ToDateTime(data);
				return date.Hour != 0 || date.Minute != 0 || date.Second != 0 ? Convert.ToDateTime(data).ToString("yyyy-MM-dd HH:mm:ss") : Convert.ToDateTime(data).ToString("yyyy-MM-dd");
			}
			if (data is Boolean || data is Nullable<Boolean>)
				return Convert.ToBoolean(data) ? "True" : "False";
			return Convert.ToString(data).Replace("\"", "\\\"");
		}

		static String GetValueToXML(Object data)
		{
			var str = GetValueToCSV(data);
			return System.Security.SecurityElement.Escape(str);
		}

		public static string GetValueToJS(Object data)
		{
			if (data == null || data == DBNull.Value)
				return "null";
			if (data is Int16 || data is Int32 || data is Int64 || data is Nullable<Int16> || data is Nullable<Int32> || data is Nullable<Int64>)
				return Convert.ToString(data);
			if (data is Decimal || data is Single || data is Double || data is Nullable<Decimal> || data is Nullable<Single> || data is Nullable<Double>)
				return Convert.ToString(Convert.ToDecimal(Convert.ToDouble(data))).Replace(System.Globalization.NumberFormatInfo.CurrentInfo.NumberGroupSeparator, "").Replace(System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator, ".");
			if (data is DateTime || data is Nullable<DateTime>){
				var date = Convert.ToDateTime(data);
				return date.Hour != 0 || date.Minute != 0 || date.Second != 0 ? String.Format("\"{0:yyyy-MM-dd HH:mm:ss}\"", date) : String.Format("\"{0:yyyy-MM-dd}\"", date);
			}
			if (data is Boolean || data is Nullable<Boolean>)
				return Convert.ToBoolean(data) ? "true" : "false";
			return "\"" + Convert.ToString(data).Replace("\"", "\\\"") + "\"";
		}
		#endregion

		#region sql
		public static String GetSQL(this String sql, params Object[] parameters)
		{
			if (String.IsNullOrEmpty(sql) || parameters == null || parameters.Length == 0)
				return sql;

			var index = -1;
			var sqlAux = sql.Replace("?", "@?@");
			var counter = 0;
			while (true)
			{
				index = sqlAux.IndexOf("@?@", index + 1);
				if (index == -1 || counter >= parameters.Length)
					return sqlAux.Replace("@?@", "?");

				var curValue = parameters[counter];
				var strValue = null as String;
				if (curValue == null)
					strValue = "null";
				else if (curValue is String)
				{
					var str = Convert.ToString(curValue);
					strValue = String.IsNullOrEmpty(str) ? "null" : "'" + str.Replace("'", "''") + "'";
				}
				else if (curValue.IsNumber())
				{
					var number = Convert.ToString(curValue);
					strValue = number.Replace(NumberFormatInfo.CurrentInfo.NumberDecimalSeparator, ".");
				}
				else if (curValue is DateTime)
				{
					var date = Convert.ToDateTime(curValue);
					strValue = "'" + (date.Hour == 0 && date.Minute == 0 && date.Second == 0 && date.Millisecond == 0 ? date.ToString("MM/dd/yyyy") : date.ToString("MM/dd/yyyy HH:mm:ss.fff")) + "'";
				}
				else if (curValue is Boolean)
				{
					var boolean = Convert.ToInt32(curValue);
					strValue = (boolean == 0 ? "0" : "1");
				}

				sqlAux = sqlAux.Remove(index, 3);
				sqlAux = sqlAux.Insert(index, strValue);
				counter++;
			}
		}
		#endregion
	}
}
