namespace Project2015To2017.Definition
{
    public sealed class AssemblyAttributes
    {
	    public AssemblyAttributes()
	    {
	    }

	    public AssemblyAttributes(string title, string company, string product, string copyright, string informationalVersion, string version, string assemblyName, string description, string configuration, string fileVersion)
	    {
		    Title = title;
		    Company = company;
		    Product = product;
		    Copyright = copyright;
		    InformationalVersion = informationalVersion;
		    Version = version;
		    AssemblyName = assemblyName;
		    Description = description;
		    Configuration = configuration;
		    FileVersion = fileVersion;
	    }

	    public AssemblyAttributes(string assemblyName, string informationalVersion,
									string copyright, string description, string company)
	    {
			AssemblyName = assemblyName;
		    Description = description;
			Copyright = copyright;
		    InformationalVersion = informationalVersion;
		    Company = company;
		}

	    public string Title { get; }
        public string Company { get; }
        public string Product { get; }
        public string Copyright { get; }
        public string InformationalVersion { get; }
        public string Version { get; }
        public string AssemblyName { get; }
	    public string Description { get; }
	    public string Configuration { get; }
	    public string FileVersion { get; }

	    public AssemblyAttributes WithCompany(string company)
	    {
		    return new AssemblyAttributes(
						Title, company,
						Product, Copyright,
						InformationalVersion, Version,
						AssemblyName, Description,
						Configuration, FileVersion
					);

	    }
    }
}
