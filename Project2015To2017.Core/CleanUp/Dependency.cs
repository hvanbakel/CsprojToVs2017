using System;
using System.Collections.Generic;
using System.Linq;

namespace Project2015To2017.CleanUp
{
	internal class Dependency : IEquatable<Dependency>
	{
		public Dependency(string name, string version)
		{
			Name = name;
			Version = version;
		}

		public string Name { get; }

		public string Version { get; }

		public string Parent { get; set; }

		public List<Dependency> Children { get; set; } = new List<Dependency>();

		public List<Dependency> ContainingPackages { get; set; }

		public bool Equals(Dependency other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return string.Equals(Name, other.Name) && string.Equals(Version, other.Version);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((Dependency)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return ((Name != null ? Name.GetHashCode() : 0) * 397) ^ (Version != null ? Version.GetHashCode() : 0);
			}
		}

		public override string ToString() => $"{Name} - {Version} => ContainingPackages: {string.Join(", ", ContainingPackages?.Select(p => p.Parent))}";
	}
}
