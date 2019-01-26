using System;
using System.Collections.Generic;
using System.Linq;
using Project2015To2017.Definition;

namespace Project2015To2017.Migrate2017
{
	public static class SystemNuGetPackages
	{
		private static readonly (string name, string version)[] Packages45 =
		{
			("Microsoft.CSharp", "4.*"),
			("System.Collections", "4.*"),
			("System.Collections.Concurrent", "4.*"),
			("System.ComponentModel", "4.*"),
			("System.ComponentModel.Annotations", "4.*"),
			("System.ComponentModel.Composition", "4.*"),
			("System.ComponentModel.EventBasedAsync", "4.*"),
			("System.ComponentModel.Primitives", "4.*"),
			("System.ComponentModel.TypeConverter", "4.*"),
			("System.Data.Common", "4.*"),
			("System.Data.DataSetExtensions", "4.*"),
			("System.Diagnostics.Contracts", "4.*"),
			("System.Diagnostics.Debug", "4.*"),
			("System.Diagnostics.Tools", "4.*"),
			("System.Diagnostics.Tracing", "4.*"),
			("System.DirectoryServices", "4.*"),
			("System.DirectoryServices.AccountManagement", "4.*"),
			("System.DirectoryServices.Protocols", "4.*"),
			("System.Drawing.Primitives", "4.*"),
			("System.Dynamic.Runtime", "4.*"),
			("System.Globalization", "4.*"),
			("System.IO", "4.*"),
			("System.IO.Compression", "4.*"),
			("System.Linq", "4.*"),
			("System.Linq.Expressions", "4.*"),
			("System.Linq.Parallel", "4.*"),
			("System.Linq.Queryable", "4.*"),
			("System.Management", "4.*"),
			("System.Net.Http", "4.*"),
			("System.Net.Http.Rtc", "4.*"),
			("System.Net.NetworkInformation", "4.*"),
			("System.Net.Primitives", "4.*"),
			("System.Net.Requests", "4.*"),
			("System.ObjectModel", "4.*"),
			("System.Reflection", "4.*"),
			("System.Reflection.Context", "4.*"),
			("System.Reflection.Emit", "4.*"),
			("System.Reflection.Emit.ILGeneration", "4.*"),
			("System.Reflection.Emit.Lightweight", "4.*"),
			("System.Reflection.Extensions", "4.*"),
			("System.Reflection.Primitives", "4.*"),
			("System.Resources.Reader", "4.*"),
			("System.Resources.ResourceManager", "4.*"),
			("System.Runtime", "4.*"),
			("System.Runtime.Caching", "4.*"),
			("System.Runtime.Extensions", "4.*"),
			("System.Runtime.InteropServices", "4.*"),
			("System.Runtime.InteropServices.RuntimeInformation", "4.*"),
			("System.Runtime.InteropServices.WindowsRuntime", "4.*"),
			("System.Runtime.Numerics", "4.*"),
			("System.Runtime.Serialization.Json", "4.*"),
			("System.Runtime.Serialization.Primitives", "4.*"),
			("System.Runtime.Serialization.Xml", "4.*"),
			("System.Security.Principal", "4.*"),
			("System.ServiceModel.Duplex", "4.*"),
			("System.ServiceModel.Http", "4.*"),
			("System.ServiceModel.NetTcp", "4.*"),
			("System.ServiceModel.Primitives", "4.*"),
			("System.ServiceModel.Security", "4.*"),
			("System.Text.Encoding", "4.*"),
			("System.Text.Encoding.Extensions", "4.*"),
			("System.Text.RegularExpressions", "4.*"),
			("System.Threading", "4.*"),
			("System.Threading.Tasks", "4.*"),
			("System.Threading.Tasks.Dataflow", "4.*"),
			("System.Threading.Tasks.Parallel", "4.*"),
			("System.Threading.Timer", "4.*"),
			("System.ValueTuple", "4.*"),
			("System.Xml.ReaderWriter", "4.*"),
			("System.Xml.XDocument", "4.*"),
			("System.Xml.XmlSerializer", "4.*"),
		};

		private static readonly (string name, string version)[] Packages46 =
		{
			("Microsoft.Win32.Primitives", "4.*"),
			("System.AppContext", "4.*"),
			("System.Collections.NonGeneric", "4.*"),
			("System.Collections.Specialized", "4.*"),
			("System.Console", "4.*"),
			("System.Diagnostics.FileVersionInfo", "4.*"),
			("System.Diagnostics.Process", "4.*"),
			("System.Diagnostics.StackTrace", "4.*"),
			("System.Diagnostics.TextWriterTraceListener", "4.*"),
			("System.Diagnostics.TraceSource", "4.*"),
			("System.Globalization.Calendars", "4.*"),
			("System.Globalization.Extensions", "4.*"),
			("System.IO.Compression.ZipFile", "4.*"),
			("System.IO.FileSystem", "4.*"),
			("System.IO.FileSystem.DriveInfo", "4.*"),
			("System.IO.FileSystem.Primitives", "4.*"),
			("System.IO.FileSystem.Watcher", "4.*"),
			("System.IO.MemoryMappedFiles", "4.*"),
			("System.IO.Pipes", "4.*"),
			("System.IO.UnmanagedMemoryStream", "4.*"),
			("System.Net.NameResolution", "4.*"),
			("System.Net.Ping", "4.*"),
			("System.Net.Security", "4.*"),
			("System.Net.Sockets", "4.*"),
			("System.Net.WebHeaderCollection", "4.*"),
			("System.Net.WebSockets.Client", "4.*"),
			("System.Net.WebSockets", "4.*"),
			("System.Resources.Writer", "4.*"),
			("System.Runtime.CompilerServices.VisualC", "4.*"),
			("System.Runtime.Handles", "4.*"),
			("System.Runtime.Serialization.Formatters", "4.*"),
			("System.Security.Claims", "4.*"),
			("System.Security.Cryptography.Algorithms", "4.*"),
			("System.Security.Cryptography.Csp", "4.*"),
			("System.Security.Cryptography.Encoding", "4.*"),
			("System.Security.Cryptography.Primitives", "4.*"),
			("System.Security.Cryptography.X509Certificates", "4.*"),
			("System.Security.SecureString", "4.*"),
			("System.Threading.Overlapped", "4.*"),
			("System.Threading.Thread", "4.*"),
			("System.Threading.ThreadPool", "4.*"),
			("System.Xml.XmlDocument", "4.*"),
			("System.Xml.XPath", "4.*"),
			("System.Xml.XPath.XDocument", "4.*"),
		};

		private static readonly (string name, string version)[] Packages461 =
		{
			("System.IO.IsolatedStorage", "4.*"),
		};

		private const string MaxTargetFramework = "net499";
		private const string MaxTargetStandard = "netstandard9.9";
		private static readonly string[] IncompatiblePrefixes = {"net1", "net2", "net3", "net40"};

		public static IReadOnlyList<(string name, string version, AssemblyReference reference)>
			DetectUpgradeableReferences(Project project)
		{
			const StringComparison comparison = StringComparison.OrdinalIgnoreCase;
			var list = new List<(string name, string version, AssemblyReference reference)>();

			var minTargetFramework = MaxTargetFramework;
			var minTargetStandard = MaxTargetStandard;

			foreach (var framework in project.TargetFrameworks)
			{
				foreach (var incompatiblePrefix in IncompatiblePrefixes)
				{
					if (framework.StartsWith(incompatiblePrefix, comparison))
					{
						return list;
					}
				}

				if (framework.StartsWith("netstandard", comparison) &&
				    string.Compare(minTargetStandard, framework, comparison) > 0)
				{
					minTargetStandard = framework;
				}
				else if (framework.StartsWith("net4", comparison) &&
				         string.Compare(minTargetFramework, framework, comparison) > 0)
				{
					minTargetFramework = framework;
				}
			}

			ReportForVersion(Packages45);

			var hasFramework = minTargetFramework != MaxTargetFramework;
			var hasStandard = minTargetStandard != MaxTargetStandard;

			if (
				(hasFramework && string.Compare(minTargetFramework, "net46", comparison) >= 0)
				||
				(hasStandard && string.Compare(minTargetStandard, "netstandard1.3", comparison) >= 0)
			)
			{
				ReportForVersion(Packages46);
			}

			if (
				(hasFramework && string.Compare(minTargetFramework, "net461", comparison) >= 0)
				||
				(hasStandard && string.Compare(minTargetStandard, "netstandard1.4", comparison) >= 0)
			)
			{
				ReportForVersion(Packages461);
			}

			return list;

			void ReportForVersion(ICollection<(string name, string version)> packages)
			{
				var matchingAssemblyRefs = project.AssemblyReferences
					.Select(x => x.Include.ToLowerInvariant())
					.Distinct()
					.Intersect(packages.Select(x => x.name.ToLowerInvariant()));

				foreach (var assembly in matchingAssemblyRefs)
				{
					var correctCaseAssembly = packages
						.First(x => string.Equals(x.name, assembly, comparison));
					var correctCaseDefinition = project.AssemblyReferences
						.First(x => string.Equals(x.Include, assembly, comparison));

					list.Add((correctCaseAssembly.name, correctCaseAssembly.version, correctCaseDefinition));
				}
			}
		}
	}
}
