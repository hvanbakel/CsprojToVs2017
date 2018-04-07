namespace Project2015To2017.Definition
{
    public class ProjectReference
    {
	    public ProjectReference(string include, string aliases)
	    {
		    Include = include;
		    Aliases = aliases;
	    }

	    public string Include { get; }
		public string Aliases { get; }
    }
}
