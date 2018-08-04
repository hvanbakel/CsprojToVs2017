using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using Project2015To2017.Definition;

namespace Project2015To2017.Analysis
{
	public abstract class DiagnosticBase : IEquatable<DiagnosticBase>
	{
		private AnalysisOptions _options;
		private IReporter _reporter;
		public uint Id { get; }

		protected internal AnalysisOptions Options
		{
			get => _options;
			set => _options = value ?? throw new ArgumentNullException(nameof(value));
		}

		protected internal IReporter Reporter
		{
			get => _reporter;
			set => _reporter = value ?? throw new ArgumentNullException(nameof(value));
		}

		public virtual bool SkipForLegacyProject => false;
		public virtual bool SkipForModernProject => false;

		/// <inheritdoc />
		protected DiagnosticBase(uint id)
		{
			Id = id;
		}

		protected abstract void AnalyzeImpl(Project project);

		public void Analyze(Project project)
		{
			if (_options == null)
			{
				throw new IllegalDiagnosticStateException("Analyzer options are not set.");
			}

			if (_reporter == null)
			{
				throw new IllegalDiagnosticStateException("Reporter to use is not set.");
			}

			AnalyzeImpl(project);
		}

		/// <summary>
		/// Report found issue using user-selected means of logging
		/// </summary>
		/// <param name="message">Informative message about the issue</param>
		/// <param name="element">XML element that is the source of the issue</param>
		/// <param name="source">File or directory for user reference</param>
		/// <param name="sourceLine">File line for user reference</param>
		protected void Report(string message, XElement element = null, FileSystemInfo source = null, uint sourceLine = uint.MaxValue)
		{
			if (source == null && sourceLine != uint.MaxValue)
			{
				throw new ArgumentNullException(nameof(source));
			}

			if (sourceLine == uint.MaxValue && element is IXmlLineInfo elementOnLine && elementOnLine.HasLineInfo())
			{
				sourceLine = (uint) elementOnLine.LineNumber;
			}

			Reporter.Report(DiagnosticCode, message, source, sourceLine);
		}

		public string DiagnosticCode => $"W{Id.ToString().PadLeft(3, '0')}";

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
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return Id == other.Id;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			return obj is DiagnosticBase other && Equals(other);
		}
	}
}