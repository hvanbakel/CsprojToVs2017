using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Project2015To2017.Definition
{
    public sealed class PackageConfiguration
    {
	    public string Id { get; set; }
        public string Version { get; set; }
        public string Authors { get; set; }
        public string Description { get; set; }
        public string Copyright { get; set; }
        public string LicenseUrl { get; set; }
        public string ProjectUrl { get; set; }
        public string IconUrl { get; set; }
        public string Tags { get; set; }
        public string ReleaseNotes { get; set; }
        public bool RequiresLicenseAcceptance { get; set; }
	    public IList<XElement> Dependencies { get; set; }
    }
}
