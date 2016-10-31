using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;

namespace DoIt.Functions
{
	internal class TryFunctions : FunctionsNodeHandlerBase
	{
		public TryFunctions(XmlNode node):base(node){}

		public override bool Execute()
		{
			if (Node == null)
				return false;
			var retry = Convert.ToInt32(Util.GetStr(Node, "retry", "1"));
			var sleep = Util.GetStr(Node, "sleep", "1 second");
			var lstExceptions = Util.GetChildNodes(Util.GetChildNode(Node, "Catch"), "Exception");
			if (lstExceptions == null){
				var n = Program.Shared.Document.CreateElement("Exception");
				n.SetAttribute("type", "System.Exception");
				n.SetAttribute("withMessage", "");
				lstExceptions = new XmlNode[]{n};
			}
			var lst = lstExceptions.Select(n => new {Type=Util.GetStr(n, "type"), WithMessage=Util.GetStr(n, "withMessage")}).ToArray();
			var success = false;
			for(var x=0; x<retry; x++){
				try{
					Program.Shared.ExecuteCommands(Util.GetChildNode(Node, "Execute"));
					success = true;
					break;
				}catch(Exception ex) when (lst.Any(item => item.Type.ToLower() == ex.GetType().FullName.ToLower() && (string.IsNullOrEmpty(item.WithMessage) || string.IsNullOrEmpty(ex.Message) || ex.Message.ToLower().Contains(item.WithMessage.ToLower())))){
					Program.Shared.WriteLogLine("Try command has caught exception {0} - Exception Message: {1} (Retry {2}; Sleep {3}ms)", ex.GetType().FullName, ex.Message, x + 1, sleep);
					Thread.Sleep(Util.GetTimeSpan(sleep));
				}
			}
			Program.Shared.ExecuteCommands(success ? Util.GetChildNode(Node, "Success") : Util.GetChildNode(Node, "Fail"));
			return true;
		}
	}
}
