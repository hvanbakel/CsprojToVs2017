using Project2015To2017.Definition;

namespace Project2015To2017.Caching
{
	public class NoProjectCache : IProjectCache
	{
		public static IProjectCache Instance => new NoProjectCache();

		private NoProjectCache()
		{

		}

		public void Add(string key, Project project)
		{
		}

		public bool TryGetValue(string key, out Project project)
		{
			project = null;
			return false;
		}

		public void Purge()
		{
		}
	}
}