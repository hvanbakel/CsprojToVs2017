using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using Project2015To2017.Definition;

namespace Project2015To2017.Analysis
{
	public abstract class DiagnosticBase : IEquatable<DiagnosticBase>, IDiagnostic
	{
		public uint Id { get; }
		public string DiagnosticCode { get; }

		public virtual bool SkipForLegacyProject => false;
		public virtual bool SkipForModernProject => false;

		protected DiagnosticBase(uint id)
		{
			Id = id;
			DiagnosticCode = $"W{id.ToString(CultureInfo.InvariantCulture).PadLeft(3, '0')}";
		}

		public abstract IReadOnlyList<IDiagnosticResult> Analyze(Project project);

		/// <summary>
		/// Report found issue using user-selected means of logging
		/// </summary>
		/// <param name="message">Informative message about the issue</param>
		/// <param name="element">XML element that is the source of the issue</param>
		/// <param name="source">File or directory for user reference</param>
		/// <param name="sourceLine">File line for user reference</param>
		protected DiagnosticResult CreateDiagnosticResult(string message, XElement element = null, FileSystemInfo source = null, uint sourceLine = uint.MaxValue)
		{
			if (source == null && sourceLine != uint.MaxValue)
			{
				throw new ArgumentNullException(nameof(source));
			}

			if (sourceLine == uint.MaxValue && element is IXmlLineInfo elementOnLine && elementOnLine.HasLineInfo())
			{
				sourceLine = (uint) elementOnLine.LineNumber;
			}

			return new DiagnosticResult
			{
				Code = DiagnosticCode,
				Message = message,
				Location = new DiagnosticLocation
				{
					Source = source,
					SourceLine = sourceLine
				}
			};
		}

		public override int GetHashCode()
		{
			return (int) Id;
		}

		public override string ToString()
		{
			return DiagnosticCode;
		}

		public bool Equals(DiagnosticBase other)
		{
			if (other is null) return false;
			if (ReferenceEquals(this, other)) return true;
			return Id == other.Id;
		}

		public override bool Equals(object obj)
		{
			if (obj is null) return false;
			if (ReferenceEquals(this, obj)) return true;
			return obj is DiagnosticBase other && Equals(other);
		}
	}
}