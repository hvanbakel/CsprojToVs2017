using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
			this.Id = id;
			this.DiagnosticCode = $"W{id.ToString(CultureInfo.InvariantCulture).PadLeft(3, '0')}";
		}

		public abstract IReadOnlyList<IDiagnosticResult> Analyze(Project project);

		/// <summary>
		/// Report found issue using user-selected means of logging
		/// </summary>
		/// <param name="project">Project in which the issue was found</param>
		/// <param name="message">Informative message about the issue</param>
		/// <param name="source">File or directory for user reference</param>
		/// <param name="sourceLine">File line for user reference</param>
		protected DiagnosticResult CreateDiagnosticResult(Project project, string message, FileSystemInfo source = null, uint sourceLine = uint.MaxValue)
		{
			if (source == null && sourceLine != uint.MaxValue)
			{
				throw new ArgumentNullException(nameof(source));
			}

			return new DiagnosticResult
			{
				Code = DiagnosticCode,
				Message = message,
				Location = new DiagnosticLocation
				{
					Source = source,
					SourceLine = sourceLine
				},
				Project = project
			};
		}

		public override int GetHashCode()
		{
			return (int)this.Id;
		}

		public override string ToString()
		{
			return this.DiagnosticCode;
		}

		public bool Equals(DiagnosticBase other)
		{
			if (other is null) return false;
			if (ReferenceEquals(this, other)) return true;
			return this.Id == other.Id;
		}

		public override bool Equals(object obj)
		{
			if (obj is null) return false;
			if (ReferenceEquals(this, obj)) return true;
			return obj is DiagnosticBase other && Equals(other);
		}
	}
}