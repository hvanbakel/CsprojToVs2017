using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Project2015To2017.Transforms;

namespace Project2015To2017.Definition
{
	public static class ProjectExtensions
	{
		private static readonly Guid WpfGuid = Guid.ParseExact("60DC8134-EBA5-43B8-BCC9-BB4BC16C2548", "D");

		public static IEnumerable<XElement> UnconditionalGroups(this Project project)
		{
			return project.PropertyGroups.Where(x => x.Attribute("Condition") == null);
		}

		public static IEnumerable<XElement> ConditionalGroups(this Project project)
		{
			return project.PropertyGroups.Where(x => x.Attribute("Condition") != null);
		}

		public static XElement PrimaryPropertyGroup(this Project project)
		{
			return project.UnconditionalGroups().First();
		}

		public static (IReadOnlyList<XElement> unconditional, IReadOnlyList<(string condition, XElement element)> conditional)
			PropertyAll(this Project project, string name)
		{
			if (name == null) throw new ArgumentNullException(nameof(name));
			if (project.PropertyGroups == null) throw new ArgumentNullException(nameof(project.PropertyGroups));

			var unconditional = new List<XElement>();
			var conditional = new List<(string condition, XElement element)>();
			foreach (var element in project.PropertyGroups.ElementsAnyNamespace(name))
			{
				if (!element.PropertyCondition(out var condition))
				{
					unconditional.Add(element);
					continue;
				}

				conditional.Add((condition, element));
			}

			return (unconditional, conditional);
		}

		public static XElement Property(this Project project, string name, bool tryConditional = false)
		{
			if (name == null) throw new ArgumentNullException(nameof(name));

			var (unconditional, conditional) = project.PropertyAll(name);
			return unconditional.LastOrDefault()
			       ?? (tryConditional ? conditional.Select(x => x.element).LastOrDefault() : null);
		}

		public static IEnumerable<(Guid guid, XElement source)> IterateProjectTypeGuids(this Project project)
		{
			var (unconditional, conditional) = project.PropertyAll("ProjectTypeGuids");
			if (conditional.Count > 0)
			{
				// log unexpected case
			}

			var guidTypes = new HashSet<Guid>();
			foreach (var element in unconditional)
			{
				// parse the CSV list
				foreach (var guid in element.Value
					.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries)
					.Select(x =>
						Guid.TryParse(x.Trim(), out var res)
							? res
							: /* unexpected case */ Guid.Empty)
					.Where(x => x != Guid.Empty))
				{
					if (guidTypes.Add(guid))
						yield return (guid, element);
				}
			}
		}

		public static PropertyFindResult FindExistingElements(this Project project, params string[] names)
		{
			var result = new PropertyFindResult
			{
				OtherUnconditionalElements = new List<XElement>(),
				OtherConditionalElements = new List<XElement>()
			};

			foreach (var name in names)
			{
				var (unconditional, conditional) = project.PropertyAll(name);
				foreach (var child in unconditional)
				{
					Store(child, ref result.LastUnconditionalElement, result.OtherUnconditionalElements);
				}

				foreach (var (_, child) in conditional)
				{
					Store(child, ref result.LastConditionalElement, result.OtherConditionalElements);
				}
			}

			return result;

			void Store(XElement child, ref XElement lastElement, IList<XElement> others)
			{
				if (lastElement != null)
				{
					if (child.IsAfter(lastElement))
					{
						others.Add(lastElement);
						lastElement = child;
					}
					else
					{
						others.Add(child);
					}
				}
				else
				{
					lastElement = child;
				}
			}
		}

		public static IEnumerable<XElement> AllUnconditional(this PropertyFindResult self)
		{
			return self.LastUnconditionalElement != null
				? self.OtherUnconditionalElements.Concat(new[] {self.LastUnconditionalElement})
				: Array.Empty<XElement>();
		}

		public static IEnumerable<XElement> AllConditional(this PropertyFindResult self)
		{
			return self.LastConditionalElement != null
				? self.OtherConditionalElements.Concat(new[] {self.LastConditionalElement})
				: Array.Empty<XElement>();
		}

		public static IEnumerable<XElement> All(this PropertyFindResult self)
		{
			return self.LastElementIsConditional
				? self.AllConditional().Concat(self.AllUnconditional())
				: self.AllUnconditional().Concat(self.AllConditional());
		}

		public ref struct PropertyFindResult
		{
			public XElement LastUnconditionalElement;
			public XElement LastConditionalElement;

			public bool LastElementIsConditional => LastConditionalElement?.IsAfter(LastUnconditionalElement) ?? false;

			public bool LastElementIsUnconditional =>
				LastUnconditionalElement?.IsAfter(LastConditionalElement) ?? false;

			public IList<XElement> OtherUnconditionalElements;
			public IList<XElement> OtherConditionalElements;

			public bool FoundAny => LastConditionalElement != null || LastUnconditionalElement != null;

			public static implicit operator bool(PropertyFindResult self) => self.FoundAny;
		}

		public static void ReplacePropertiesWith(this Project project, XElement newElement, params string[] names)
		{
			var findResult = project.FindExistingElements(names);

			if (!findResult)
			{
				if (newElement != null)
				{
					project.PrimaryPropertyGroup().Add(newElement);
				}

				return;
			}

			XElement lastExisting = null;
			foreach (var element in findResult.All())
			{
				lastExisting?.Remove();
				lastExisting = element;
			}

			if (newElement == null)
			{
				lastExisting?.Remove();
				return;
			}

			if (lastExisting != null)
			{
				if (!lastExisting.PropertyCondition(out _))
				{
					lastExisting.ReplaceWith(newElement);
					return;
				}

				lastExisting.Remove();
			}

			project.PrimaryPropertyGroup().Add(newElement);
		}

		public static void SetProperty(this Project project, string elementName, string value)
		{
			XElement newElement = null;
			if (!string.IsNullOrWhiteSpace(value))
			{
				newElement = new XElement(elementName, value);
			}

			project.ReplacePropertiesWith(newElement, elementName);
		}

		public static bool IsWindowsPresentationFoundationProject(this Project project)
		{
			if (project == null) throw new ArgumentNullException(nameof(project));

			if (project.Property("UseWpf")?.Value == "true")
				return true;

			if (project.Property("ExtrasEnableWpfProjectSetup")?.Value == "true")
				return true;

			return project.IterateProjectTypeGuids().Any(x => x.guid == WpfGuid);
		}

		public static bool IsWindowsFormsProject(this Project project)
		{
			if (project == null) throw new ArgumentNullException(nameof(project));

			if (project.Property("UseWindowsForms")?.Value == "true")
				return true;

			if (project.Property("ExtrasEnableWinFormsProjectSetup")?.Value == "true")
				return true;

			return project.Property("MyType", tryConditional: true)?.Value == "WindowsForms";
		}

		public static DirectoryInfo TryFindBestRootDirectory(this Project project)
		{
			if (project == null) throw new ArgumentNullException(nameof(project));

			return project.Solution?.FilePath.Directory ?? project.FilePath.Directory;
		}

		public static IEnumerable<FileInfo> FindAllWildcardFiles(this Project project, string extension)
		{
			return project.ProjectFolder
				.GetFiles("*." + extension, SearchOption.AllDirectories)
				.Where(x => !project.IsInIntermediateOutputDirectory(x.FullName));
		}

		public static bool IsInIntermediateOutputDirectory(this Project project, string path)
		{
			var isPathRooted = Path.IsPathRooted(path);
			var pathWithProjectPath = isPathRooted
				? path
				: Path.Combine(project.ProjectFolder.FullName, path);
			return project.IntermediateOutputPaths.Any(intermediatePath =>
			{
				if (path.IsSubPathOf(intermediatePath))
					return true;
				if (!isPathRooted && pathWithProjectPath.IsSubPathOf(intermediatePath))
					return true;
				if (Path.IsPathRooted(intermediatePath))
					return false;
				var intermediatePathWithProjectPath = Path.Combine(project.ProjectFolder.FullName, intermediatePath);
				if (path.IsSubPathOf(intermediatePathWithProjectPath))
					return true;
				if (!isPathRooted && pathWithProjectPath.IsSubPathOf(intermediatePathWithProjectPath))
					return true;
				return false;
			});
		}
	}
}