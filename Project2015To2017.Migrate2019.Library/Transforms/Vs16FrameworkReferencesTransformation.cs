using System;
using System.Collections.Immutable;
using System.Linq;
using Project2015To2017.Definition;
using Project2015To2017.Transforms;

namespace Project2015To2017.Migrate2019.Library.Transforms
{
	public sealed class Vs16FrameworkReferencesTransformation : ILegacyOnlyProjectTransformation
	{
		private const string SdkExtrasVersion = "MSBuild.Sdk.Extras/2.0.31";
		private const string WindowsDesktopVersion = "Microsoft.NET.Sdk.WindowsDesktop";
		private static readonly Guid XamarinAndroid = Guid.ParseExact("EFBA0AD7-5A72-4C68-AF49-83D382785DCF", "D");
		private static readonly Guid XamarinIos = Guid.ParseExact("6BC8ED88-2882-458C-8E55-DFD12B67127B", "D");
		private static readonly Guid Uap = Guid.ParseExact("A5A43C5B-DE2A-4C0C-9213-0A381AF9435A", "D");

		public void Transform(Project definition)
		{
			var isWindowsPresentationFoundationProject = definition.IsWindowsPresentationFoundationProject();
			var isWindowsFormsProject = definition.IsWindowsFormsProject();
			var isExtraPlatform = false;

			var guidTypes = definition.IterateProjectTypeGuids().ToImmutableHashSet();

			if (guidTypes.Any(x => x.guid == XamarinAndroid))
			{
				definition.TargetFrameworks.Add("xamarin.android");
				isExtraPlatform = true;
			}

			if (guidTypes.Any(x => x.guid == XamarinIos))
			{
				definition.TargetFrameworks.Add("xamarin.ios");
				isExtraPlatform = true;
			}

			if (guidTypes.Any(x => x.guid == Uap))
			{
				definition.TargetFrameworks.Add("uap");
				isExtraPlatform = true;
			}

			if (isExtraPlatform)
			{
				definition.ProjectSdk = SdkExtrasVersion;
			}
			else if (isWindowsPresentationFoundationProject || isWindowsFormsProject)
			{
				definition.ProjectSdk =
					definition.ProjectSdk = WindowsDesktopVersion;
			}

			if (isWindowsPresentationFoundationProject)
			{
				definition.SetProperty("UseWPF", "true");
				definition.SetProperty("ExtrasEnableWpfProjectSetup", null);
			}

			if (isWindowsFormsProject)
			{
				definition.SetProperty("UseWindowsForms", "true");
				definition.SetProperty("ExtrasEnableWinFormsProjectSetup", null);
			}
		}
	}
}