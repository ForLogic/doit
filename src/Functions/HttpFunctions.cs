using ICSharpCode.SharpZipLib.Zip;
using LumenWorks.Framework.IO.Csv;
using Microsoft.WindowsAzure.Storage;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DoIt.Functions
{
	internal class HttpFunctions : FunctionsNodeHandlerBase
	{
		public HttpFunctions(XmlNode node):base(node){}

		public override bool Execute()
		{
			if (Node == null)
				return false;
			foreach (var n in Util.GetChildNodes(Node, "Download")){
				var url = Program.Shared.ReplaceTags(Util.GetStr(n, "url"));
				var toFile = Program.Shared.ReplaceTags(Util.GetStr(n, "toFile"));
				if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(toFile))
					continue;
				using (var c = new WebClient())
					c.DownloadFile(url, toFile);
			}
			return true;
		}
	}
}
