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
	internal class ForEachFunctions : FunctionsNodeHandlerBase
	{
		public ForEachFunctions(XmlNode node):base(node){}

		public override bool Execute()
		{
			var itemFrom = Program.Shared.ReplaceTags(Util.GetStr(Node, "itemFrom"));
			if (string.IsNullOrEmpty(itemFrom))
				return false;
			var where = Program.Shared.ReplaceTags(Util.GetStr(Node, "where"));
			var sort = Program.Shared.ReplaceTags(Util.GetStr(Node, "sort"));
			var parallel = Convert.ToInt32(Util.GetStr(Node, "parallel", "1"));
			var dt = Program.Shared.GetDataTable(Program.Shared.ThreadID(), itemFrom);
			if (dt == null)
				return false;
			var lstRows = string.IsNullOrEmpty(where) && string.IsNullOrEmpty(sort) ? dt.Rows.Cast<DataRow>().ToArray() : dt.Select(where, sort);
			if(parallel > 1){
				var parentSequence = Program.Shared.GetSequence();
				Task task = Task.Run(() => Parallel.ForEach(lstRows, new ParallelOptions{MaxDegreeOfParallelism=parallel}, (r, state) => {
					var sequence = parentSequence + "." + Program.Shared.ThreadID();
					lock (Program.Shared.LockSequence){
						if (Thread.CurrentThread.Name == null){
							Thread.CurrentThread.Name = sequence;
							Program.Shared.ThreadSequence.Add(sequence);
						}
					}
					var itemID = itemFrom+";"+sequence;
					lock(Program.Shared.LockCurrentRows){
						Program.Shared.CurrentRows[itemID] = r;
					}
					var stop = Program.Shared.ExecuteCommands(Node);
					lock(Program.Shared.LockCurrentRows){
						Program.Shared.CurrentRows.RemoveKey(itemID);
					}
					if (stop)
						state.Stop();
				}));
				task.Wait();
			}else{
				var itemID = itemFrom+";"+ Program.Shared.GetSequence();
				foreach (DataRow r in lstRows){
					lock(Program.Shared.LockCurrentRows)
						Program.Shared.CurrentRows[itemID] = r;
					if (Program.Shared.ExecuteCommands(Node))
						break;
				}
				lock(Program.Shared.LockCurrentRows)
					Program.Shared.CurrentRows.RemoveKey(itemID);
			}
			return true;
		}
	}
}
