namespace Project2015To2017.Definition
{
    public sealed class PackageReference
    {
	    public PackageReference(string id, string version)
	    {
		    Id = id;
		    Version = version;
		}

	    public PackageReference(string id, string version, bool isDevelopmentDependency)
				: this(id, version)
	    {
		    IsDevelopmentDependency = isDevelopmentDependency;
	    }

	    public string Id { get; }
        public string Version { get; }
        public bool IsDevelopmentDependency { get; }

	    public PackageReference WithVersion(string version)
	    {
		    return new PackageReference(Id, version, IsDevelopmentDependency);
	    }
    }
}
