using System;
using System.Collections.Generic;
using System.Linq;
using Project2015To2017.Definition;

namespace Project2015To2017.Analysis.Diagnostics
{
	public sealed class W021SystemNuGetPackagesDiagnostic : DiagnosticBase
	{
		private const string MaxTargetFramework = "net499";
		private const string MaxTargetStandard = "netstandard9.9";

		public W021SystemNuGetPackagesDiagnostic() : base(21)
		{
		}

		private static readonly string[] IncompatiblePrefixes = {"net1", "net2", "net3", "net40"};

		private static readonly string[] Packages45 =
		{
			"System.Collections",
			"System.Collections.Concurrent",
			"System.ComponentModel",
			"System.ComponentModel.Annotations",
			"System.ComponentModel.Composition",
			"System.ComponentModel.EventBasedAsync",
			"System.ComponentModel.Primitives",
			"System.ComponentModel.TypeConverter",
			"System.Data.Common",
			"System.Data.DataSetExtensions",
			"System.Diagnostics.Contracts",
			"System.Diagnostics.Debug",
			"System.Diagnostics.Tools",
			"System.Diagnostics.Tracing",
			"System.DirectoryServices",
			"System.DirectoryServices.AccountManagement",
			"System.DirectoryServices.Protocols",
			"System.Drawing.Primitives",
			"System.Dynamic.Runtime",
			"System.Globalization",
			"System.IO",
			"System.IO.Compression",
			"System.Linq",
			"System.Linq.Expressions",
			"System.Linq.Parallel",
			"System.Linq.Queryable",
			"System.Management",
			"System.Net.Http",
			"System.Net.Http.Rtc",
			"System.Net.NetworkInformation",
			"System.Net.Primitives",
			"System.Net.Requests",
			"System.ObjectModel",
			"System.Reflection",
			"System.Reflection.Context",
			"System.Reflection.Emit",
			"System.Reflection.Emit.ILGeneration",
			"System.Reflection.Emit.Lightweight",
			"System.Reflection.Extensions",
			"System.Reflection.Primitives",
			"System.Resources.Reader",
			"System.Resources.ResourceManager",
			"System.Runtime",
			"System.Runtime.Caching",
			"System.Runtime.Extensions",
			"System.Runtime.InteropServices",
			"System.Runtime.InteropServices.RuntimeInformation",
			"System.Runtime.InteropServices.WindowsRuntime",
			"System.Runtime.Numerics",
			"System.Runtime.Serialization.Json",
			"System.Runtime.Serialization.Primitives",
			"System.Runtime.Serialization.Xml",
			"System.Security.Principal",
			"System.ServiceModel.Duplex",
			"System.ServiceModel.Http",
			"System.ServiceModel.NetTcp",
			"System.ServiceModel.Primitives",
			"System.ServiceModel.Security",
			"System.Text.Encoding",
			"System.Text.Encoding.Extensions",
			"System.Text.RegularExpressions",
			"System.Threading",
			"System.Threading.Tasks",
			"System.Threading.Tasks.Dataflow",
			"System.Threading.Tasks.Parallel",
			"System.Threading.Timer",
			"System.ValueTuple",
			"System.Xml.ReaderWriter",
			"System.Xml.XDocument",
			"System.Xml.XmlSerializer",
		};

		private static readonly string[] Packages46 =
		{
			"Microsoft.Win32.Primitives",
			"System.AppContext",
			"System.Collections.NonGeneric",
			"System.Collections.Specialized",
			"System.Console",
			"System.Diagnostics.FileVersionInfo",
			"System.Diagnostics.Process",
			"System.Diagnostics.StackTrace",
			"System.Diagnostics.TextWriterTraceListener",
			"System.Diagnostics.TraceSource",
			"System.Globalization.Calendars",
			"System.Globalization.Extensions",
			"System.IO.Compression.ZipFile",
			"System.IO.FileSystem",
			"System.IO.FileSystem.DriveInfo",
			"System.IO.FileSystem.Primitives",
			"System.IO.FileSystem.Watcher",
			"System.IO.MemoryMappedFiles",
			"System.IO.Pipes",
			"System.IO.UnmanagedMemoryStream",
			"System.Net.NameResolution",
			"System.Net.Ping",
			"System.Net.Security",
			"System.Net.Sockets",
			"System.Net.WebHeaderCollection",
			"System.Net.WebSockets.Client",
			"System.Net.WebSockets",
			"System.Resources.Writer",
			"System.Runtime.CompilerServices.VisualC",
			"System.Runtime.Handles",
			"System.Runtime.Serialization.Formatters",
			"System.Security.Claims",
			"System.Security.Cryptography.Algorithms",
			"System.Security.Cryptography.Csp",
			"System.Security.Cryptography.Encoding",
			"System.Security.Cryptography.Primitives",
			"System.Security.Cryptography.X509Certificates",
			"System.Security.SecureString",
			"System.Threading.Overlapped",
			"System.Threading.Thread",
			"System.Threading.ThreadPool",
			"System.Xml.XmlDocument",
			"System.Xml.XPath",
			"System.Xml.XPath.XDocument",
		};

		private static readonly string[] Packages461 =
		{
			"System.IO.IsolatedStorage",
		};

		public StringComparison Comparison = ExtensionMethods.BestAvailableStringIgnoreCaseComparison;

		public override IReadOnlyList<IDiagnosticResult> Analyze(Project project)
		{
			var list = new List<IDiagnosticResult>();

			var minTargetFramework = MaxTargetFramework;
			var minTargetStandard = MaxTargetStandard;

			foreach (var framework in project.TargetFrameworks)
			{
				foreach (var incompatiblePrefix in IncompatiblePrefixes)
				{
					if (framework.StartsWith(incompatiblePrefix, Comparison))
					{
						return list;
					}
				}

				if (framework.StartsWith("netstandard", Comparison) &&
				    string.Compare(minTargetStandard, framework, Comparison) > 0)
				{
					minTargetStandard = framework;
				}
				else if (framework.StartsWith("net4", Comparison) &&
				         string.Compare(minTargetFramework, framework, Comparison) > 0)
				{
					minTargetFramework = framework;
				}
			}

			ReportForVersion(project, list, Packages45);

			var hasFramework = minTargetFramework != MaxTargetFramework;
			var hasStandard = minTargetStandard != MaxTargetStandard;

			if (
				(hasFramework && string.Compare(minTargetFramework, "net46", Comparison) >= 0)
				||
				(hasStandard && string.Compare(minTargetStandard, "netstandard1.3", Comparison) >= 0)
			)
			{
				ReportForVersion(project, list, Packages46);
			}

			if (
				(hasFramework && string.Compare(minTargetFramework, "net461", Comparison) >= 0)
				||
				(hasStandard && string.Compare(minTargetStandard, "netstandard1.4", Comparison) >= 0)
			)
			{
				ReportForVersion(project, list, Packages461);
			}

			return list;
		}

		private void ReportForVersion(Project project, ICollection<IDiagnosticResult> list,
			ICollection<string> packages)
		{
			foreach (var assembly in project.AssemblyReferences.Select(x => x.Include.ToLowerInvariant()).Distinct()
				.Intersect(packages.Select(x => x.ToLowerInvariant())))
			{
				var comparison = ExtensionMethods.BestAvailableStringIgnoreCaseComparison;
				var correctCaseAssembly = packages.First(x =>
					string.Equals(x, assembly, comparison));
				var correctCaseDefinition = project.AssemblyReferences.First(x =>
					string.Equals(x.Include, assembly, comparison));

				list.Add(
					CreateDiagnosticResult(project,
							$"A better way to reference '{correctCaseAssembly}' assembly is using respective '{correctCaseAssembly}' NuGet package. It will simplify porting to other runtimes and enable possible .NET SDK tooling improvements.",
							project.FilePath)
						.LoadLocationFromElement(correctCaseDefinition.DefinitionElement));
			}
		}
	}
}