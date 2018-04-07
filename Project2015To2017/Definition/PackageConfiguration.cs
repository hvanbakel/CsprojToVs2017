using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Project2015To2017.Definition
{
    public sealed class PackageConfiguration
    {
	    public PackageConfiguration(
				string id, string version, string authors, string description,
				string copyright, string licenseUrl, string projectUrl, string iconUrl, string tags,
				string releaseNotes, bool requiresLicenseAcceptance,
				IEnumerable<XElement> dependencies
		    )
	    {
		    Id = id;
		    Version = version;
		    Authors = authors;
		    Description = description;
		    Copyright = copyright;
		    LicenseUrl = licenseUrl;
		    ProjectUrl = projectUrl;
		    IconUrl = iconUrl;
		    Tags = tags;
		    ReleaseNotes = releaseNotes;
		    RequiresLicenseAcceptance = requiresLicenseAcceptance;
		    Dependencies = dependencies.ToList().AsReadOnly();
	    }

	    public string Id { get; }
        public string Version { get; }
        public string Authors { get; }
        public string Description { get; }
        public string Copyright { get; }
        public string LicenseUrl { get; }
        public string ProjectUrl { get; }
        public string IconUrl { get; }
        public string Tags { get; }
        public string ReleaseNotes { get; }
        public bool RequiresLicenseAcceptance { get; }
	    public IReadOnlyList<XElement> Dependencies { get; }
    }
}
