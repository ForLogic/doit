using System;
using System.Xml;

namespace DoIt.Functions
{
	internal abstract class FunctionsNodeHandlerBase
	{
		public XmlNode Node { get; set; }

		public FunctionsNodeHandlerBase(XmlNode node)
		{
			Node = node;
		}

		public abstract bool Execute();
	}
}
