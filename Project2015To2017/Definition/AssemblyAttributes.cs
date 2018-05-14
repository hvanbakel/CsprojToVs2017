using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Project2015To2017.Definition
{
	public sealed class AssemblyAttributes
	{
		public CompilationUnitSyntax FileContents { get; set; }
			= (CompilationUnitSyntax)CSharpSyntaxTree.ParseText(@"").GetRoot();

		public string Title
		{
			get => GetAttribute(typeof(AssemblyTitleAttribute));
			set => SetAttribute(typeof(AssemblyTitleAttribute), value);
		}

		public string Company {
			get => GetAttribute(typeof(AssemblyCompanyAttribute));
			set => SetAttribute(typeof(AssemblyCompanyAttribute), value);
		}

		public string Product {
			get => GetAttribute(typeof(AssemblyProductAttribute));
			set => SetAttribute(typeof(AssemblyProductAttribute), value);
		}

		public string Copyright {
			get => GetAttribute(typeof(AssemblyCopyrightAttribute));
			set => SetAttribute(typeof(AssemblyCopyrightAttribute), value);
		}

		public string InformationalVersion {
			get => GetAttribute(typeof(AssemblyInformationalVersionAttribute));
			set => SetAttribute(typeof(AssemblyInformationalVersionAttribute), value);
		}

		public string Version {
			get => GetAttribute(typeof(AssemblyVersionAttribute));
			set => SetAttribute(typeof(AssemblyVersionAttribute), value);
		}

		public string Description {
			get => GetAttribute(typeof(AssemblyDescriptionAttribute));
			set => SetAttribute(typeof(AssemblyDescriptionAttribute), value);
		}

		public string Configuration {
			get => GetAttribute(typeof(AssemblyConfigurationAttribute));
			set => SetAttribute(typeof(AssemblyConfigurationAttribute), value);
		}

		public string FileVersion {
			get => GetAttribute(typeof(AssemblyFileVersionAttribute));
			set => SetAttribute(typeof(AssemblyFileVersionAttribute), value);
		}

		public FileInfo File { get; set; }

		private (AttributeListSyntax attList, AttributeSyntax att) FindAttribute(Type attributeType)
		{
			//Find the specified attribute and which attribute list it is in

			var attName = attributeType.Name;
			var altAttName = AttributeShortName(attName);

			return FileContents
					.AttributeLists
					.Select(x =>
						(
							attList: x,
							matchingAtts: x.Attributes
										   .Where(att => att.Name.ToString() == attName || att.Name.ToString() == altAttName)
										   .ToImmutableArray()
						)
					)
					.Where(x => x.matchingAtts.Any())
					.Select(x => (x.attList, att: x.matchingAtts.Single()))
					.LastOrDefault();
		}

		private string GetAttribute(Type attributeType)
		{
			var att = FindAttribute(attributeType).att;

			//Make the assumption that it just has a single string argument
			//because all of the attributes we currently look for do have
			return att?.ArgumentList.Arguments.Single().ToString().Trim('"');
		}

		private void SetAttribute(Type attributeType, string value)
		{
			var att = FindAttribute(attributeType);

			SyntaxList<AttributeListSyntax> newAttLists;

			if (value == null)
			{
				if (att.att == null)
				{
					return;
				}

				var newAttList = att.attList.RemoveNode(att.att, SyntaxRemoveOptions.KeepNoTrivia);

				if (newAttList.Attributes.Any())
				{
					newAttLists = FileContents.AttributeLists.Replace(att.attList, newAttList);
				}
				else
				{
					newAttLists = FileContents.AttributeLists.Remove(att.attList);
				}
			}
			else if (att.att == null)
			{
				var attList = CreateAttribute(attributeType, value);

				newAttLists = FileContents.AttributeLists.Add(attList);
			}
			else
			{
				var currentArgs = att.att.ArgumentList;
				var mainArg = currentArgs.Arguments.First();
				var newMainArg = mainArg.WithExpression(SyntaxFactory.ParseExpression($"\"{value}\""));

				var newArgs = currentArgs.Arguments.Replace(mainArg, newMainArg);

				var newNode = att.att.WithArgumentList(currentArgs.WithArguments(newArgs));

				var newAttList = att.attList.ReplaceNode(att.att, newNode);
				newAttLists = FileContents.AttributeLists.Replace(att.attList, newAttList);
			}

			FileContents = FileContents
							.WithAttributeLists(
								newAttLists
							);
		}

		private static AttributeListSyntax CreateAttribute(Type attributeType, string value)
		{
			var root = (CompilationUnitSyntax) SyntaxFactory
				.ParseSyntaxTree($"[assembly: {attributeType.Name}(\"{value}\")]")
				.GetRoot();

			var attList = root.AttributeLists.Single();

			return attList;
		}

		private string AttributeShortName(string fullName)
		{
			return fullName.Substring(0, fullName.Length - 9);
		}

		private bool Equals(AssemblyAttributes other)
		{
			return FileContents.ToFullString() == other.FileContents.ToFullString();
		}

		public override bool Equals(object obj)
		{
			if (obj is null) return false;
			if (ReferenceEquals(this, obj)) return true;
			return obj is AssemblyAttributes attributes && Equals(attributes);
		}
	}
}
