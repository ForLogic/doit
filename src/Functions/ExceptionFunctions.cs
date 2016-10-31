using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;

namespace DoIt.Functions
{
	internal class ExceptionFunctions : FunctionsNodeHandlerBase
	{
		public ExceptionFunctions(XmlNode node):base(node){}

		public override bool Execute()
		{
			if (Node == null)
				return false;
			var assembly = Util.GetStr(Node, "assembly");
			var type = Util.GetStr(Node, "type", "System.Exception");
			var message = Util.GetStr(Node, "message");
			var asm = null as Assembly;
			if (string.IsNullOrEmpty(assembly))
				asm = Assembly.GetExecutingAssembly();
			else{
				var asmName = Assembly.GetExecutingAssembly().GetReferencedAssemblies().FirstOrDefault(item => item.Name.ToLower() == assembly.ToLower());
				asm = asmName != null ? Assembly.Load(asmName) : Assembly.LoadFrom(Path.IsPathRooted(assembly) ? assembly : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, assembly));
			}
			if (asm == null)
				return false;
			var exType = Type.GetType(type) ?? asm.GetType(type);
			var ctor = exType.GetConstructor(new[]{typeof(string)});
			throw ctor.Invoke(new object[]{message}) as Exception;
		}
	}
}
