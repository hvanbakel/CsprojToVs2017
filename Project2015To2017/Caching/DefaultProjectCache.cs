using System.Collections.Concurrent;
using Project2015To2017.Definition;

namespace Project2015To2017.Caching
{
	public class DefaultProjectCache : IProjectCache
	{
		private readonly ConcurrentDictionary<string, Project> dictionary = new ConcurrentDictionary<string, Project>();

		public void Add(string key, Project project)
		{
			this.dictionary.AddOrUpdate(key, project, (s, p) => p);
		}

		public void Purge()
		{
			this.dictionary.Clear();
		}

		public bool TryGetValue(string key, out Project project)
		{
			return this.dictionary.TryGetValue(key, out project);
		}
	}
}