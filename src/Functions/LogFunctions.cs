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
	internal class LogFunctions : FunctionsNodeHandlerBase
	{
		public LogFunctions(XmlNode node):base(node){}

		public override bool Execute()
		{
			if (Node == null)
				return false;

            var str = Program.Shared.ReplaceTags(Node.InnerXml);
            Program.Shared.IsLogEnabled = Util.GetStr(Node, "enabled", "true").ToLower() == "true";
			Program.Shared.WriteLogLine(str);
            Console.WriteLine(str);
			return true;
		}
	}
}
