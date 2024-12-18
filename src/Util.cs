﻿using System;
using System.Xml;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Data.SqlClient;
using System.Data;
using ICSharpCode.SharpZipLib.Zip;
using System.Text;

namespace DoIt
{
	internal class Util
	{
		public static String GetFileSize(Int64 lengthInBytes, Int32 decimalPlaces = 2)
		{
			var value = Convert.ToDecimal(lengthInBytes);
			if (value < 1024)
				return Math.Round(value, decimalPlaces) + " bytes";
			value /= 1024m;
			if (value < 1024)
				return Math.Round(value, decimalPlaces) + " KB";
			value /= 1024m;
			if (value < 1024)
				return Math.Round(value, decimalPlaces) + " MB";
			value /= 1024m;
			return Math.Round(value, decimalPlaces) + " GB";
		}

		public static T GetEnumValue<T>(string value, T defaultValue) where T:struct
		{
			if (string.IsNullOrEmpty(value))
				return defaultValue;
			var lstValues1 = value.Split(new char[]{',',';','|',' '}, StringSplitOptions.RemoveEmptyEntries);
			var lstValues2 = Enum.GetNames(typeof(T));
			if (!lstValues1.All(v => lstValues2.Any(s => s.ToLower() == v.ToLower())))
				return defaultValue;
			return (T)Enum.Parse(typeof(T), string.Join(",", lstValues1), true);
		}

		public static XmlNode[] GetChildNodes(XmlNode node, params string[] childNames)
		{
			if (node == null)
				return null;
			if(childNames == null || childNames.Length == 0 || childNames.All(n => string.IsNullOrEmpty(n)))
				return node.ChildNodes.Cast<XmlNode>().Where(n => n.Name != "#comment" && n.Name != "#whitespace").ToArray();
			return node.ChildNodes.Cast<XmlNode>().Where(n => childNames.Any(n2 => n2 != null && n.Name.ToLower() == n2.ToLower())).ToArray();
		}

		public static XmlNode GetChildNode(XmlNode node, params string[] childNames)
		{
			var lst = GetChildNodes(node, childNames);
			return lst == null ? null : lst.FirstOrDefault();
		}

		public static String GetStr(XmlNode node, String dataName, String defaultValue = null)
		{
			if (node == null)
				return defaultValue;
			var atr = node.Attributes[dataName];
			if (atr != null && !String.IsNullOrEmpty(atr.Value))
				return atr.Value;
			var subn = GetChildNode(node, dataName);
			return (subn == null || String.IsNullOrEmpty(subn.InnerText)) ? defaultValue : subn.InnerText;
		}

		public static String GetContentType(String filename, Boolean isExtension = false)
		{
			var ext = isExtension ? filename : Path.GetExtension(filename);
			switch (ext.ToLower()){
				case ".pdf": return "application/pdf";
				case ".zip": return "application/zip";
				case ".js": return "application/javascript";
				case ".gif": return "image/gif";
				case ".jpg": return "image/jpeg";
				case ".jpeg": return "image/jpeg";
				case ".png": return "image/png";
				case ".ico": return "image/x-icon";
				case ".tif": return "image/tiff";
				case ".tiff": return "image/tiff";
				case ".eml": return "message/rfc822";
				case ".mp4": return "video/mp4";
				case ".mp3": return "audio/mpeg";
				case ".mov": return "video/quicktime";
				case ".mpg": return "video/mpeg";
				case ".avi": return "video/x-msvideo";
				case ".wmv": return "video/x-ms-wmv";
				case ".xls": return "application/vnd.ms-excel";
				case ".doc": return "application/msword";
				case ".ppt": return "application/vnd.ms-powerpoint";
				case ".pps": return "application/vnd.ms-powerpoint";
				case ".xlsx": return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
				case ".docx": return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
				case ".pptx": return "application/vnd.openxmlformats-officedocument.presentationml.presentation";
				case ".xltx": return "application/vnd.openxmlformats-officedocument.spreadsheetml.template";
				case ".dotx": return "application/vnd.openxmlformats-officedocument.wordprocessingml.template";
				case ".ppsx": return "application/vnd.openxmlformats-officedocument.presentationml.slideshow";
				case ".rtf": return "application/rtf";
				case ".css": return "text/css";
				case ".csv": return "text/csv";
				case ".txt": return "text/plain";
				case ".xml": return "text/xml";
				case ".htm": return "text/html";
				case ".html": return "text/html";
			}
			return "application/octet-stream";
		}

		public static String GetTempFileName(String extension = null)
		{
			var temp = Path.GetTempFileName();
			if (String.IsNullOrEmpty(extension))
				return temp;
			var filename = Path.GetFileNameWithoutExtension(temp);
			return Path.Combine(Path.GetDirectoryName(temp), filename + (extension.StartsWith(".") ? extension : "." + extension));
		}

		public static string GetConfigData(string configStr, string dataType, bool toLower = false, string defaultValue = null)
		{
			if (String.IsNullOrEmpty(configStr) || String.IsNullOrEmpty(dataType))
				return null;
			var allData = configStr.Split(new Char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (var str in allData)
				if (str.Trim().ToLower().StartsWith(dataType.ToLower() + "=")){
					var data = str.Split(new Char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
					if (data.Length == 2){
						var value = data[1];
						if (String.IsNullOrEmpty(value))
							return defaultValue;
						return toLower ? value.ToLower() : value;
					}
				}
			return null;
		}

		public static string GetArg(string[] args, string key, bool toLower = false)
		{
			if (args == null || args.Length == 0)
				return null;
			foreach (var arg in args){
				if (String.IsNullOrEmpty(arg))
					continue;
				var str = arg;
				if (!str.StartsWith("/") && !str.StartsWith("-") && !str.StartsWith("--"))
					continue;
				if (str.StartsWith("/"))
					str = arg.Remove(0, 1);
				if (str.StartsWith("--"))
					str = arg.Remove(0, 2);
				if (str.StartsWith("-"))
					str = arg.Remove(0, 1);
				if (!str.StartsWith(key, StringComparison.InvariantCultureIgnoreCase))
					continue;
				var index = str.IndexOf("=");
				if (index == -1 || index == str.Length - 1)
					return toLower ? key.ToLower() : key;
				var value = str.Substring(index + 1);
				if (String.IsNullOrEmpty(value))
					return value;
				value = value.Trim(new Char[] { '"', ' ' });
				return toLower ? value.ToLower() : value;
			}
			return null;
		}

		public static object GetValue(object value, string type)
		{
			if (value == null || string.IsNullOrEmpty(type))
				return value;
            var str = Convert.ToString(value);
			switch(type.ToLower()){
				case "byte": return Convert.ToByte(value);
				case "short": return Convert.ToInt16(value);
				case "int": return Convert.ToInt32(value);
				case "long": return Convert.ToInt64(value);
				case "decimal": return Convert.ToDecimal(value);
				case "float": return Convert.ToSingle(value);
				case "double": return Convert.ToDouble(value);
				case "string": return Convert.ToString(value);
				case "datetime": return value is DateTime ? new Nullable<DateTime>(Convert.ToDateTime(value)) : ParseDateTime(Convert.ToString(value));
				case "datetimeoffset":
                    return value is DateTime ? 
                    new Nullable<DateTimeOffset>(new DateTimeOffset(Convert.ToDateTime(value))) :
                    value is string && !string.IsNullOrEmpty(str) && Regex.IsMatch(str,@"^\d{4}\-\d{2}\-\d{2} \d{2}:\d{2}:\d{2}\.\d{7}$") ?
                    new Nullable<DateTimeOffset>(new DateTimeOffset(DateTime.ParseExact(str, "yyyy-MM-dd HH:mm:ss.fffffff", null), TimeSpan.Zero)) :
					value is string && !string.IsNullOrEmpty(str) && Regex.IsMatch(str, @"^\d{4}\-\d{2}\-\d{2} \d{2}:\d{2}:\d{2}\.\d{7} (\+|\-)?\d{2}:\d{2}$") ?
					new Nullable<DateTimeOffset>(DateTimeOffset.ParseExact(str, "yyyy-MM-dd HH:mm:ss.fffffff zzz", null)) :
					ParseDateTimeOffset(str);
				case "bool": return Convert.ToBoolean(value);
			}
			return value;
		}

		public static Type GetType(string type)
		{
			if (string.IsNullOrEmpty(type))
				return typeof(object);
			switch(type.ToLower()){
				case "byte": return typeof(byte);
				case "short": return typeof(short);
				case "int": return typeof(int);
				case "long": return typeof(long);
				case "decimal": return typeof(decimal);
				case "float": return typeof(float);
				case "double": return typeof(double);
				case "string": return typeof(string);
				case "datetime": return typeof(DateTime);
				case "datetimeoffset": return typeof(DateTimeOffset);
				case "bool": return typeof(bool);
			}
			return typeof(object);
		}

		public static string GetStrData(string str, string tag, object data)
		{
			var pattern = "\\{(?<tag>" + tag.Replace("$", "\\$").Replace(".", "\\.") + ")(?<params>\\:+.+?)*\\}";
			var tagLower = tag.ToLower();
			var m = Regex.Match(str, pattern);
			var oldData = data;
			while (m != null && m.Success){
				data = oldData;
				var p = m.Groups["params"];
				if (!p.Success){
					var valueStr = null as string;
					if (data is DateTimeOffset)
						valueStr = Util.GetDateTimeOffsetString((DateTimeOffset)data);
					else if (data is DateTime)
						valueStr = Util.GetDateTimeString((DateTime)data);
					else
						valueStr = Convert.ToString(data);
					str = str
						.Remove(m.Index, m.Length)
						.Insert(m.Index, tagLower == "rand" ? Util.Random(15) : valueStr);
					m = Regex.Match(str, pattern);
					continue;
				}

				var array = p.Value.Split(new char[] { ':' }, StringSplitOptions.None);
				if (tag == "rand"){
					var len = array.Length >= 2 && Regex.IsMatch(array[1], "^\\d+$") ? Convert.ToInt32(array[1]) : 15;
					var chars = array.Length == 3 && !string.IsNullOrEmpty(array[2]) ? array[2] : "0123456789ABCDEFGHIJKLMNOPQRSTUVXWYZabcdefghijklmnopqrstuvxwyz";
					if (chars.Length == 7 || chars.Length == 5)
						switch (chars.ToLower()){
							case "numbers": chars = "0123456789"; break;
							case "upper": chars = "ABCDEFGHIJKLMNOPQRSTUVXWYZ"; break;
							case "lower": chars = "abcdefghijklmnopqrstuvxwyz"; break;
						}
					var rand = Util.Random(len, chars);
					str = str
						.Remove(m.Index, m.Length)
						.Insert(m.Index, rand);
					m = Regex.Match(str, pattern);
					continue;
				}

				foreach (var v in array)
					if (!string.IsNullOrEmpty(v) && !v.StartsWith(">"))
						switch (v.ToLower()){
							case "onlypathchars": data = Convert.ToString(data).OnlyPathChars(); break;
							case "filename": data = Convert.ToString(data).GetFileName(); break;
							case "fileextension": data = Convert.ToString(data).GetFileExtension(); break;
							case "bytes": data = GetBytes(Convert.ToDecimal(data)); break;
							default: data = string.Format($"{{0:{v}}}", data); break;
						}

				var value = null as string; //data is DateTimeOffset ? string.Format("{0:yyyy-MM-dd HH:mm:ss.fffffff zzz}", data) : Convert.ToString(data);
				if (data is DateTimeOffset)
					value = string.Format("{0:yyyy-MM-dd HH:mm:ss.fffffff zzz}", data);
				else
					value = Convert.ToString(data);

				if (string.IsNullOrEmpty(value)) {
					var last = array.Length > 0 ? array[array.Length - 1] : null;
					if (!string.IsNullOrEmpty(last) && last.StartsWith(">"))
						value = last.Remove(0, 1);
				}

				str = str
					.Remove(m.Index, m.Length)
					.Insert(m.Index, value);
				m = Regex.Match(str, pattern);
			}
			return str;
		}

		static string GetBytes(decimal bytes)
        {
			if (bytes < 1024m)
				return bytes + " bytes";
			bytes = bytes / 1024m;
			if (bytes < 1024m)
				return Math.Round(bytes, 1) + " KB";
			bytes = bytes / 1024m;
			if (bytes < 1024)
				return Math.Round(bytes, 1) + " MB";
			bytes = bytes / 1024m;
			if (bytes < 1024)
				return Math.Round(bytes, 1) + " GB";
			bytes = bytes / 1024m;
			return Math.Round(bytes, 1) + " TB";
		}

		static string[] GetTagParameters(string str, string tag)
		{
			var lst = new List<string>();
			var strLower = str.ToLower();
			var tagLower = tag.ToLower();
			while (true){
				var index1 = strLower.IndexOf("{" + tagLower + ":");
				var index2 = strLower.IndexOf("}", index1 + 1);
				if (index1 == -1 || index2 == -1)
					break;
				var parameter = str.Substring(index1 + tag.Length + 2, index2 - index1 - (tag.Length + 2));
				if (!string.IsNullOrEmpty(parameter))
					lst.Add(parameter);
				str = str.Remove(index1, index2 - index1 + 1);
			}
			return lst.ToArray();
		}

        public static string Random(int charCount, string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVXWYZabcdefghijklmnopqrstuvxwyz")
        {
            var sb = new StringBuilder();
            for (var x = 0; x < charCount; x++){
				var rValue = StaticRandom.Next(chars.Length);
				sb.Append(chars[rValue]);
			}
            return sb.ToString();
        }

		public static string GetDateTimeString(DateTime date)
		{
			if (date.Hour == 0 && date.Minute == 0 && date.Second == 0 && date.Millisecond == 0)
				return date.ToString("yyyy-MM-dd");
			if(date.Millisecond == 0)
				return date.ToString("yyyy-MM-dd HH:mm:ss");
			return date.ToString("yyyy-MM-dd HH:mm:ss.fffffff");
		}

		public static string GetDateTimeOffsetString(DateTimeOffset date)
		{
			if (date.Hour == 0 && date.Minute == 0 && date.Second == 0 && date.Millisecond == 0)
				return date.ToString("yyyy-MM-dd");
			if(date.Millisecond == 0)
				return date.ToString("yyyy-MM-dd HH:mm:sszzz");
			return date.ToString("yyyy-MM-dd HH:mm:ss.fffffffzzz");
		}

		public static DateTime? ParseDateTime(string value)
		{
			if (string.IsNullOrEmpty(value))
				return null;
			if (value.IsMatch("^\\d{4}-\\d{2}-\\d{2} \\d{2}\\:\\d{2}:\\d{2}\\.\\d{7}$"))
				return DateTime.ParseExact(value, "yyyy-MM-dd HH:mm:ss.fffffff", null);
			if (value.IsMatch("^\\d{4}-\\d{2}-\\d{2} \\d{2}\\:\\d{2}:\\d{2}\\.\\d{3}$"))
				return DateTime.ParseExact(value, "yyyy-MM-dd HH:mm:ss.fff", null);
			if (value.IsMatch("^\\d{4}-\\d{2}-\\d{2} \\d{2}\\:\\d{2}:\\d{2}$"))
				return DateTime.ParseExact(value, "yyyy-MM-dd HH:mm:ss", null);
			if (value.IsMatch("^\\d{4}-\\d{2}-\\d{2}$"))
				return DateTime.ParseExact(value, "yyyy-MM-dd", null);
			return null;
		}

		public static DateTimeOffset? ParseDateTimeOffset(string value)
		{
			if (string.IsNullOrEmpty(value))
				return null;
			if (value.IsMatch("^\\d{4}-\\d{2}-\\d{2} \\d{2}\\:\\d{2}:\\d{2}\\.\\d{7}(\\+|\\-)\\d{2}:\\d{2}$"))
				return DateTimeOffset.ParseExact(value, "yyyy-MM-dd HH:mm:ss.fffffffzzz", null);
			if (value.IsMatch("^\\d{4}-\\d{2}-\\d{2} \\d{2}\\:\\d{2}:\\d{2}\\.\\d{3}(\\+|\\-)\\d{2}:\\d{2}$"))
				return DateTimeOffset.ParseExact(value, "yyyy-MM-dd HH:mm:ss.fffzzz", null);
			if (value.IsMatch("^\\d{4}-\\d{2}-\\d{2} \\d{2}\\:\\d{2}:\\d{2}(\\+|\\-)\\d{2}:\\d{2}$"))
				return DateTime.ParseExact(value, "yyyy-MM-dd HH:mm:sszzz", null);
			if (value.IsMatch("^\\d{4}-\\d{2}-\\d{2}$"))
				return DateTime.ParseExact(value, "yyyy-MM-dd", null);
			return null;
		}

		public static bool IsTodayInList(String days)
		{
			if (String.IsNullOrEmpty(days))
				return false;
			var array = days.ToLower().Split(new string[] { ",", ";", "|" }, StringSplitOptions.RemoveEmptyEntries).Select(d => d.Trim()).ToArray();
			if (array.Contains("all") || array.Contains(Convert.ToString(DateTime.Today.Day)))
				return true;
			var lst1 = new List<String>() { "sun", "mon", "tue", "wed", "thu", "fri", "sat" };
            var lst2 = new List<String>() { "sunday", "monday", "tuesday", "wednesday", "thursday", "friday", "saturday" };
            var currentIndex = Convert.ToInt32(DateTime.Today.DayOfWeek);
            if (array.Select(item => lst1.IndexOf(item)).Any(index => index == currentIndex) ||
                array.Select(item => lst2.IndexOf(item)).Any(index => index == currentIndex))
				return true;

            foreach (var item in array)
            {
				var m1 = Regex.Match(item, "^(?<year>\\d{4})-(?<month>\\d{2})-(?<day>\\d{2})$");
				if (m1.Success) {
					var y = Convert.ToInt32(m1.Groups["year"].Value);
					var m = Convert.ToInt32(m1.Groups["month"].Value);
					var d = Convert.ToInt32(m1.Groups["day"].Value);
					var date = new DateTime(y, m, d);
					if (date.CompareTo(DateTime.Today) == 0)
						return true;
					continue;
				}
                var m2 = Regex.Match(item, "^(?<g1>1st|first|2nd|second|3rd|third|4th|fourth|last) (?<g2>sun|sunday|mon|monday|tue|tuesday|wed|wednesday|thu|thursday|fri|friday|sat|saturday)$", RegexOptions.IgnoreCase);
                if (!m2.Success)
                    continue;
                var index = m2.Groups["g1"].Value;
                var dayOfWeek = m2.Groups["g2"].Value;
                if (IsDayOfWeek(dayOfWeek, index))
                    return true;
            }
			return false;
		}

        public static bool IsDayOfWeek(string dayOfWeek, string index)
        {
            var m1 = Regex.Match(dayOfWeek, "^(?<g0>sun|sunday)|(?<g1>mon|monday)|(?<g2>tue|tuesday)|(?<g3>wed|wednesday)|(?<g4>thu|thursday)|(?<g5>fri|friday)|(?<g6>sat|saturday)$", RegexOptions.IgnoreCase);
            var m2 = Regex.Match(index, "^(?<g1>1st|first)|(?<g2>2nd|second)|(?<g3>3rd|third)|(?<g4>4th|fourth)|(?<g5>last)$", RegexOptions.IgnoreCase);
            if (!m1.Success || !m2.Success)
                return false;

            var g1Name = "";
            for (var x = 0; x < 7; x++)
                if (m1.Groups["g" + x].Success){
                    g1Name = "g" + x;
                    break;
                }
            var g2Name = "";
            for (var x = 1; x < 6; x++)
                if (m2.Groups["g" + x].Success){
                    g2Name = "g" + x;
                    break;
                }

            var g1 = m1.Groups[g1Name];
            var g2 = m2.Groups[g2Name];

            var d1 = Convert.ToInt32(g1Name.Remove(0, 1));
            var i1 = Convert.ToInt32(g2Name.Remove(0, 1));

            var d = DateTime.Today;
            var dw = Convert.ToInt32(d.DayOfWeek);
            if (dw != d1)
                return false;

            var dwIndex = GetDayOfWeekMonthIndex(d);
            var isLastDW = IsLastDayOfWeekInMonth(d);

            return
                (i1 < 5 && i1 == dwIndex) ||
                (i1 == 5 && isLastDW);
        }

        public static int GetDayOfWeekMonthIndex(DateTime date)
        {
            var m = date.Month;
            var counter = 0;
            while (date.Month == m){
                date = date.AddDays(-7);
                counter++;
            }
            return counter;
        }

         public static bool IsLastDayOfWeekInMonth(DateTime date)
        {
            var m = date.Month;
            date = date.AddDays(7);
            return date.Month != m;
        }

       public static DateTime GetDateTime(string timespan, DateTime? date = null)
		{
			return GetDateTimeOffset(timespan, new DateTimeOffset(date == null ? DateTime.Now : date.Value)).DateTime;
		}

		public static DateTimeOffset GetDateTimeOffset(string timespan, DateTimeOffset? date = null)
		{
			if (date == null)
				date = DateTimeOffset.Now;
			if (string.IsNullOrEmpty(timespan))
				return date.Value;
			timespan = timespan.ToLower();
			var m = Regex.Match(timespan, "^(?<operation>\\-|\\+)?\\s*(?<value>\\d+)\\s*(?<measure>millisecond|second|minute|hour|day|week|month|year)s?$");
			if (!m.Success)
				throw new ArgumentException($"Invalid GetDate parameter \"{timespan}\"");
			var operation = m.Groups["operation"].Success ? m.Groups["operation"].Value : "+";
			var value = Convert.ToInt32(m.Groups["value"].Value) * (operation == "+" ? 1 : -1);
			var measure = m.Groups["measure"].Value;
			switch(measure){
				case "millisecond": return date.Value.AddMilliseconds(value);
				case "second": return date.Value.AddSeconds(value);
				case "minute": return date.Value.AddMinutes(value);
				case "hour": return date.Value.AddHours(value);
				case "day": return date.Value.AddDays(value);
				case "week": return date.Value.AddDays(value*7);
				case "month": return date.Value.AddMonths(value);
				case "year": return date.Value.AddYears(value);
			}
			return date.Value;
		}

		public static TimeSpan GetTimeSpan(string timespan)
		{
			if (string.IsNullOrEmpty(timespan) || timespan.Trim() == "0")
				return TimeSpan.Zero;
			timespan = timespan.ToLower();
			var m = Regex.Match(timespan, "^(?<value>\\d+)\\s*(?<measure>ms|millisecond|s|second|min|minute|h|hour|day|week|month)s?$");
			if (!m.Success)
				throw new ArgumentException($"Invalid GetTimeSpan parameter \"{timespan}\"");
			var value = Convert.ToInt32(m.Groups["value"].Value);
			var measure = m.Groups["measure"].Value;
			switch(measure){
                case "ms":
                case "millisecond": return TimeSpan.FromMilliseconds(value);
                case "s":
                case "second": return TimeSpan.FromSeconds(value);
                case "min":
                case "minute": return TimeSpan.FromMinutes(value);
                case "h":
                case "hour": return TimeSpan.FromHours(value);
				case "day": return TimeSpan.FromDays(value);
				case "week": return TimeSpan.FromDays(value * 7);
                case "month": return TimeSpan.FromDays(value * 30);
            }
			return TimeSpan.Zero;
		}

		public static Boolean IsTimeToRun(String time)
		{
			if (String.IsNullOrEmpty(time))
				return true;
			if (Regex.IsMatch(time, "^\\d+$") && Convert.ToInt32(time) == DateTime.Now.Hour)
				return true;
			if (Regex.IsMatch(time, "^\\d+\\-\\d+$")){
				var array = time.Split('-');
				var h1 = Convert.ToInt32(array[0]);
				var h2 = Convert.ToInt32(array[1]);
				var h = DateTime.Now.Hour;
				return (h1<=h2 && h>=h1 && h<=h2) || (h1>=h2 && (h>h1 || h<h2));
			}
			return false;
		}

		public static long GetFreeSpace(string drive)
		{
			foreach (DriveInfo di in DriveInfo.GetDrives())
				if (di.IsReady && di.Name == drive)
					return di.TotalFreeSpace;
			return -1;
		}

		public static string[] GetFiles(string path, string searchPattern = "*.*", SearchOption searchOption = SearchOption.AllDirectories)
		{
			var lstAllFiles = new List<string>();
			var lstPatterns = (searchPattern??"*.*").Split(new char[]{'|'}, StringSplitOptions.RemoveEmptyEntries);
			foreach (var pattern in lstPatterns)
				lstAllFiles.AddRange(Directory.GetFiles(path, pattern, searchOption));
			return lstAllFiles.ToArray();
		}

		public static string GetFileToSend(String filename, Boolean zipFile, String zipEntryName = null)
		{
			if (!zipFile)
				return filename;
			var zipFilename = Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename) + ".zip");
			if (File.Exists(zipFilename))
				File.Delete(zipFilename);
			if (string.IsNullOrEmpty(zipEntryName))
				zipEntryName = Path.GetFileName(filename);
			using (var zipStream = new ZipOutputStream(File.Create(zipFilename))){
				var fi = new FileInfo(filename);
				zipStream.PutNextEntry(new ZipEntry(zipEntryName) {DateTime=fi.LastWriteTime, Size=fi.Length});
				using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
					fs.CopyTo(zipStream);
			}
			return zipFilename;
		}

		#region database helpers
		public static DataTable Select(string sql, string dbConnectionString, Int32? commandTimeout = null)
		{
			using (var conn = new SqlConnection(dbConnectionString)){
				conn.Open();
				var dt = new DataTable();
				using (var cmd = new SqlCommand(sql, conn)){
					if (commandTimeout != null)
						cmd.CommandTimeout = commandTimeout.Value;
					return Select(cmd);
				}
			}
		}

		public static DataTable Select(string sql, SqlConnection connection, SqlTransaction transaction, SqlParameter[] parameters = null, Int32? commandTimeout = null)
		{
			using (var cmd = new SqlCommand(sql, connection, transaction)){
				if (commandTimeout != null)
					cmd.CommandTimeout = commandTimeout.Value;
				if (parameters != null)
					foreach (var p in parameters)
						cmd.Parameters.Add(p);
				return Select(cmd);
			}
		}

		public static DataTable Select(SqlCommand cmd)
		{
			if (cmd == null){
				Console.WriteLine("SQL Select: null SqlCommand reference.");
				Program.Shared.WriteLogLine("SQL Select: null SqlCommand reference.");
				return new DataTable();
			}
			var sql = GetCommandString(cmd).Trim();
			Console.WriteLine($"SQL Select: {sql}");
			Program.Shared.WriteLogLine($"SQL Select: {sql}");
			var dt = new DataTable();
			using (var da = new SqlDataAdapter(cmd))
				da.Fill(dt);
			Console.WriteLine($"SQL Select: {dt.Rows.Count} row(s) found");
			Program.Shared.WriteLogLine($"SQL Select: {dt.Rows.Count} row(s) found");
			return dt;
		}

		public static object Scalar(string sql, string dbConnectionString, Int32? commandTimeout = null)
		{
			using (var conn = new SqlConnection(dbConnectionString)){
				conn.Open();
				using (var cmd = new SqlCommand(sql, conn)){
					if (commandTimeout != null)
						cmd.CommandTimeout = commandTimeout.Value;
					return Scalar(cmd);
				}
			}
		}

		public static object Scalar(string sql, SqlConnection connection, SqlTransaction transaction, SqlParameter[] parameters = null, Int32? commandTimeout = null)
		{
			using (var cmd = new SqlCommand(sql, connection, transaction)){
				if (commandTimeout != null)
					cmd.CommandTimeout = commandTimeout.Value;
				if (parameters != null)
					foreach (var p in parameters)
						cmd.Parameters.Add(p);
				return Scalar(cmd);
			}
		}

		public static object Scalar(SqlCommand cmd)
		{
			if (cmd == null){
				Console.WriteLine("SQL Scalar: null SqlCommand reference.");
				Program.Shared.WriteLogLine("SQL Scalar: null SqlCommand reference.");
				return 0;
			}
			var sql = GetCommandString(cmd).Trim();
			Console.WriteLine($"SQL Scalar: {sql}");
			Program.Shared.WriteLogLine($"SQL Scalar: {sql}");
			var obj = cmd.ExecuteScalar();
			Console.WriteLine($"SQL Scalar: Result is \"{(obj == DBNull.Value ? "null" : Convert.ToString(obj))}\"");
			Program.Shared.WriteLogLine($"SQL Scalar: Result is \"{(obj == DBNull.Value ? "null" : Convert.ToString(obj))}\"");
			return obj;
		}

		public static int Execute(string sql, string dbConnectionString, Int32? commandTimeout = null)
		{
			using (var conn = new SqlConnection(dbConnectionString)){
				conn.Open();
				using (var cmd = new SqlCommand(sql, conn)){
					if (commandTimeout != null)
						cmd.CommandTimeout = commandTimeout.Value;
					return Execute(cmd);
				}
			}
		}

		public static int Execute(string sql, SqlConnection connection, SqlTransaction transaction, SqlParameter[] parameters = null, Int32? commandTimeout = null)
		{
			using (var cmd = new SqlCommand(sql, connection, transaction)){
				if (commandTimeout != null)
					cmd.CommandTimeout = commandTimeout.Value;
				if (parameters != null)
					foreach (var p in parameters)
						cmd.Parameters.Add(p);
				return Execute(cmd);
			}
		}

		public static int Execute(SqlCommand cmd)
		{
			if (cmd == null){
				Console.WriteLine("SQL Execute: null SqlCommand reference.");
				Program.Shared.WriteLogLine("SQL Execute: null SqlCommand reference.");
				return 0;
			}
			var sql = GetCommandString(cmd).Trim();
			Console.WriteLine($"SQL Execute: {sql}");
			Program.Shared.WriteLogLine($"SQL Execute: {sql}");
			var rows = cmd.ExecuteNonQuery();
			Console.WriteLine($"SQL Execute: {rows} row(s) affected");
			Program.Shared.WriteLogLine($"SQL Execute: {rows} row(s) affected");
			return rows;
		}
		#endregion


		public static string GetCommandString(SqlCommand cmd)
		{
			var s = System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator;
			Func<SqlParameter, string> fnGetString = (SqlParameter p) => {
				if (p.Value == null || p.Value == DBNull.Value)
					return "null";
				switch (p.SqlDbType){
					case SqlDbType.Char:
					case SqlDbType.VarChar:
					case SqlDbType.NChar:
					case SqlDbType.NVarChar:
					case SqlDbType.Text:
					case SqlDbType.NText:
						return $"'{Convert.ToString(p.Value).Replace("'", "''")}'";
					case SqlDbType.Date:
						return $"'{Convert.ToDateTime(p.Value).ToString("yyyy-MM-dd")}'";
					case SqlDbType.DateTime:
					case SqlDbType.DateTime2:
						var v1 = Convert.ToDateTime(p.Value);
						return $"'{v1.ToString("yyyy-MM-dd" + (v1.Hour == 0 && v1.Minute == 0 && v1.Second == 0 && v1.Millisecond == 0 ? null : " HH:mm:ss.fff"))}'";
					case SqlDbType.DateTimeOffset:
						var v2 = (DateTimeOffset)p.Value;
						return $"'{v2.ToString("yyyy-MM-dd HH:mm:ss.fffffff zzz")}'";
					case SqlDbType.Bit:
						return Convert.ToBoolean(p.Value) ? "1" : "0";
					case SqlDbType.TinyInt:
					case SqlDbType.SmallInt:
					case SqlDbType.Int:
					case SqlDbType.BigInt:
						return Convert.ToString(p.Value);
					case SqlDbType.Decimal:
					case SqlDbType.Float:
					case SqlDbType.Real:
						var v3 = Convert.ToDecimal(p.Value);
						return Convert.ToString(v3).Replace(s, ".");
				}
				return $"'{Convert.ToString(p.Value).Replace("'", "''")}'";
			};
			var sql = cmd.CommandText;
			foreach (SqlParameter p in cmd.Parameters)
				sql = ReplaceSQLParameter(sql, "@" + p.ParameterName, fnGetString(p));
			return sql;
		}

		public static string ReplaceSQLParameter(string sql, string paramName, string paramValue)
		{
			var index = -1;
			while (true){
				index = index >= sql.Length ? -1 : sql.IndexOf(paramName, index + 1);
				if (index == -1)
					return sql;
				var replace = sql.Length == index + paramName.Length || !System.Text.RegularExpressions.Regex.IsMatch(Convert.ToString(sql[index + paramName.Length]), "[a-zA-Z0-9_]+");
				if (replace){
					sql = sql.Remove(index, paramName.Length).Insert(index, paramValue);
					index += paramValue.Length;
				}
				else
					index += paramName.Length;
			}
		}

	}
}
