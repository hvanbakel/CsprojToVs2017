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
        public string AssemblyName { get; set; }
        public string Description { get; set; }
		public string Configuration { get; internal set; }
		public string FileVersion { get; internal set; }
	}
}
