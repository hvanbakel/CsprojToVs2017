using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Project2015To2017.Transforms;

namespace Project2015To2017
{
	public static partial class Extensions
	{
		public static IReadOnlyCollection<ITransformation> IterateTransformations(this ITransformationSet set,
			ILogger logger, ConversionOptions conversionOptions)
		{
			var all = set.Transformations(logger, conversionOptions);
			var (normal, others) = all.Split(FilterTargetNormalTransformations);
			var (early, late) = others.Split(x =>
				((ITransformationWithTargetMoment)x).ExecutionMoment == TargetTransformationExecutionMoment.Early);
			var res = new List<ITransformation>(all.Count);
			TopologicalSort(early, res, logger);
			TopologicalSort(normal, res, logger);
			TopologicalSort(late, res, logger);
			return res;
		}

		private static void TopologicalSort(
			IReadOnlyList<ITransformation> source,
			ICollection<ITransformation> target,
			ILogger logger)
		{
			var count = source.Count;
			if (count == 0)
			{
				return;
			}

			// When Span<T> becomes available - replace with
			// var used = count <= 256 ? stackalloc byte[count] : new byte[count];
			var used = new byte[count];
			var res = new LinkedList<ITransformation>();
			var mappings = new Dictionary<string, int>();
			for (var i = 0; i < count; i++)
			{
				var transformation = source[i];
				if (transformation == null)
				{
					throw new ArgumentNullException(nameof(transformation),
						"Transformation set must not contain null items");
				}

				mappings.Add(transformation.GetType().Name, i);
			}

			for (var i = 0; i < count; i++)
			{
				if (used[i] != 0)
				{
					continue;
				}

				TopologicalSortInternal(source, i, used, mappings, res, logger);
			}

			// topological order on reverse graph is reverse topological order on the original
			var item = res.Last;
			while (item != null)
			{
				target.Add(item.Value);
				item = item.Previous;
			}
		}

		private static void TopologicalSortInternal(
			IReadOnlyList<ITransformation> source,
			int vertex,
			byte[] used,
			IDictionary<string, int> mappings,
			LinkedList<ITransformation> res,
			ILogger logger)
		{
			if (used[vertex] == 1)
			{
				throw new InvalidOperationException(
					"Transformation set contains dependency cycle, DAG is required to build transformation tree");
			}

			if (used[vertex] != 0)
			{
				return;
			}

			used[vertex] = 1;
			var item = source[vertex];
			var name = item.GetType().Name;
			if (item is ITransformationWithDependencies itemWithDependencies)
			{
				foreach (var dependencyName in itemWithDependencies.DependOn)
				{
					if (!mappings.TryGetValue(dependencyName, out var mapping))
					{
						logger.LogWarning($"Unable to find {dependencyName} as dependency for {name}");
						continue;
					}

					TopologicalSortInternal(source, mapping, used, mappings, res, logger);
				}
			}

			used[vertex] = 2;
			res.AddFirst(item);
		}

		private static bool FilterTargetNormalTransformations(ITransformation x)
		{
			if (x is ITransformationWithTargetMoment m)
			{
				return m.ExecutionMoment == TargetTransformationExecutionMoment.Normal;
			}

			return true;
		}
	}
}