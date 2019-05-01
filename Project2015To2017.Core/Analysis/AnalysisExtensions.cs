using System;
using System.Xml;
using System.Xml.Linq;

namespace Project2015To2017.Analysis
{
	public static class AnalysisExtensions
	{
		public static string GetSourcePath(this IDiagnosticLocation self)
		{
			if (self == null) throw new ArgumentNullException(nameof(self));

			return self.SourcePath ?? self.Source?.FullName;
		}

		public static IDiagnosticResult LoadLocationFromElement(this IDiagnosticResult self, XElement element)
		{
			if (self == null) throw new ArgumentNullException(nameof(self));
			if (element == null) throw new ArgumentNullException(nameof(element));

			if (self.Location.SourceLine != uint.MaxValue)
			{
				return self;
			}

			if (element is IXmlLineInfo elementOnLine && elementOnLine.HasLineInfo())
			{
				return new DiagnosticResult(self)
				{
					Location = new DiagnosticLocation(self.Location)
					{
						SourceLine = (uint)elementOnLine.LineNumber
					}
				};
			}

			return self;
		}
	}
}