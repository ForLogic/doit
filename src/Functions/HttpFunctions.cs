using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
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
			var lstNodes = Util.GetChildNodes(Node, "Request");
			foreach (var n in lstNodes)
				switch (n.Name.ToLower()) {
					case "request": Request(n); break;
				}
			return true;
		}

		void Request(XmlNode n)
        {
			var method = Program.Shared.ReplaceTags(Util.GetStr(n, "method", "get"));
			var url = Program.Shared.ReplaceTags(Util.GetStr(n, "url"));
			var toFile = Program.Shared.ReplaceTags(Util.GetStr(n, "toFile"));
			var toVar = Program.Shared.ReplaceTags(Util.GetStr(n, "toVar"));
			var hasTo = !string.IsNullOrEmpty(toFile) || !string.IsNullOrEmpty(toVar);
			if (string.IsNullOrEmpty(url) || !hasTo)
				return;

			var headersNode = Util.GetChildNode(n, "Headers");
			var lstHeaders = Util.GetChildNodes(headersNode, "Header");

			var m = GetMethod(method);
			using (var msg = new HttpRequestMessage(m, url))
			using (var http = new HttpClient())
            {
				if (lstHeaders != null)
					foreach (var hNode in lstHeaders)
					{
						var headerName = Util.GetStr(hNode, "name");
						if (!string.IsNullOrEmpty(headerName))
							msg.Headers.Add(headerName, hNode.InnerXml);
					}

				var task = http.SendAsync(msg);
				task.Wait();
				var rs = task.Result;
				if (!string.IsNullOrEmpty(toVar))
                {
					var t2 = rs.Content.ReadAsStringAsync();
					t2.Wait();
					lock (Program.Shared.LockVariables)
						Program.Shared.Variables[toVar + ";" + Program.Shared.GetSequence()] = t2.Result;
				}
				if (!string.IsNullOrEmpty(toFile))
				{
					var t2 = rs.Content.ReadAsStreamAsync();
					t2.Wait();
					using (var fs = new FileStream(toFile, FileMode.Create, FileAccess.Write))
						t2.Result.CopyTo(fs);
				}
			}
		}

		HttpMethod GetMethod(string method)
        {
			if (string.IsNullOrWhiteSpace(method))
				return HttpMethod.Get;
			switch (method.ToLower().Trim())
            {
				case "get": return HttpMethod.Get;
				case "post": return HttpMethod.Post;
				case "put": return HttpMethod.Put;
				case "delete": return HttpMethod.Delete;
			}
			return HttpMethod.Get;
		}
	}
}
