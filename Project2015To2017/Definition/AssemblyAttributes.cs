namespace Project2015To2017.Definition
{
    public sealed class AssemblyAttributes
    {	
	    public string Title { get; set; }
        public string Company { get; set; }
        public string Product { get; set; }
        public string Copyright { get; set; }
        public string InformationalVersion { get; set; }
        public string Version { get; set; }
	    public string Description { get; set; }
	    public string Configuration { get; set; }
	    public string FileVersion { get; set; }

	    private bool Equals(AssemblyAttributes other)
	    {
		    return string.Equals(Title, other.Title)
		           && string.Equals(Company, other.Company)
		           && string.Equals(Product, other.Product)
		           && string.Equals(Copyright, other.Copyright)
		           && string.Equals(InformationalVersion, other.InformationalVersion)
		           && string.Equals(Version, other.Version)
		           && string.Equals(Description, other.Description)
		           && string.Equals(Configuration, other.Configuration)
		           && string.Equals(FileVersion, other.FileVersion);
	    }

	    public override bool Equals(object obj)
	    {
		    if (obj is null) return false;
		    if (ReferenceEquals(this, obj)) return true;
		    return obj is AssemblyAttributes attributes && Equals(attributes);
	    }
    }
}
