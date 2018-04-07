namespace Project2015To2017.Definition
{
    // Reference
    public class AssemblyReference
    {
	    public AssemblyReference(
		    string include
	    )
	    {
		    Include = include;
		}

		public AssemblyReference(
		    string include, string hintPath
		) : this(include)
	    {
			HintPath = hintPath;
		}

	    public AssemblyReference(
				string include, string embedInteropTypes,
				string hintPath, string isPrivate, string specificVersion
		    ) : this(include, hintPath)
	    {
		    EmbedInteropTypes = embedInteropTypes;
		    Private = isPrivate;
		    SpecificVersion = specificVersion;
	    }

		// Attributes
	    public string Include { get; }

        // Elements
        public string EmbedInteropTypes { get; }
        public string HintPath { get; }
        public string Private { get; }
        public string SpecificVersion { get; }
    }
}
