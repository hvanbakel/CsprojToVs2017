using System;
using System.Collections.Immutable;
using System.Linq;
using Project2015To2017.Definition;
using Project2015To2017.Transforms;

namespace Project2015To2017.Migrate2017.Transforms
{
	public sealed class FrameworkReferencesTransformation : ILegacyOnlyProjectTransformation
	{
		private const string SdkExtrasVersion = "MSBuild.Sdk.Extras/1.6.65";
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

			if (isWindowsPresentationFoundationProject || isWindowsFormsProject || isExtraPlatform)
			{
				definition.ProjectSdk = SdkExtrasVersion;
			}

			if (isWindowsPresentationFoundationProject)
			{
				definition.SetProperty("ExtrasEnableWpfProjectSetup", "true");
			}

			if (isWindowsFormsProject)
			{
				definition.SetProperty("ExtrasEnableWinFormsProjectSetup", "true");
			}
		}
	}
}
