using ICSharpCode.SharpZipLib.Zip;
using LumenWorks.Framework.IO.Csv;
using Microsoft.WindowsAzure.Storage;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace DoIt.Functions
{
	internal class SleepFunctions : FunctionsNodeHandlerBase
	{
		public SleepFunctions(XmlNode node):base(node){}

		public override bool Execute()
		{
			if (Node == null)
				return false;
			var time = Util.GetStr(Node, "time", "0");
			var timeSpan = Util.GetTimeSpan(time);
			if (timeSpan == TimeSpan.Zero)
				return false;
			Program.Shared.WriteLogLine("Current thread is sleeping for {0}...", time);
			Thread.Sleep(timeSpan);
			return true;
		}
	}
}
