﻿using System;
using System.Collections.Generic;
using System.Linq;
using NeoGet.Tools.Hierarchy.Entities;
using NuGet.Frameworks;

namespace NeoGet.Tools.Hierarchy.Extensions
{
	public static class PackageHierarchyExtensions
	{
		public static IEnumerable<string> GetSummary(this PackageHierarchy hierarchy)
		{
			yield return hierarchy.Source;

			foreach(var line in GetSummary(hierarchy.Packages, p => p.GetSummary()))
			{
				yield return line;
			}
		}

		/// <summary>
		/// Gets a summary of the hierarchy for the specific item, optionally limited to a specific framework.
		/// </summary>
		/// <param name="item">The item for which to generate a summary.</param>
		/// <param name="framework">The framework to limit the hierarchy to.</param>
		private static IEnumerable<string> GetSummary(
			this PackageHierarchyItem item,
			NuGetFramework framework = null
		)
		{
			var dependencies = item.GetMatchingDependencies(framework);

			yield return (dependencies.Any() ? "─┬" : "──") + item.Identity;

			if(dependencies.Any())
			{
				//We only want to display the target framework if;
				//- we're at the root (framework should not a value)
				//- the dependencies found target the same framework as the current one (it could be a matching framework, like NetStandard 1.0 can be used by a NetStandard 2.0 package)
				var displayFramework = framework == null || dependencies.First().Key != framework;

				foreach(var line in dependencies.GetSummary(d => GetSummary(d.Key, d.Value, displayFramework), displayFramework))
				{
					yield return line;
				}
			}
		}

		/// <summary>
		/// Gets a summary of the packages present for the given framework.
		/// </summary>
		/// <param name="framework">The target framework.</param>
		/// <param name="packages">The packages</param>
		/// <param name="displayFramework">Indicates whether to display the target framework name.</param>
		/// <returns></returns>
		private static IEnumerable<string> GetSummary(NuGetFramework framework, PackageHierarchyItem[] packages, bool displayFramework = false)
		{
			if(displayFramework)
			{
				yield return "[" + framework.DotNetFrameworkName + "]";
			}

			foreach(var line in packages.GetSummary(p => p.GetSummary(framework)))
			{
				yield return line;
			}
		}

		/// <summary>
		/// A generic version for the summary construction.
		/// Simplifies the inclusion of the "tree" characters.
		/// </summary>
		/// <param name="items">The items to display.</param>
		/// <param name="itemSummaryFactory">The method used to retrieve a item's summary.</param>
		/// <param name="includePrefix">Indicates whether to include the prefix in each item summary.</param>
		/// <returns>A tree-like summary for the given items.</returns>
		private static IEnumerable<string> GetSummary<T>(
			this ICollection<T> items,
			Func<T, IEnumerable<string>> itemSummaryFactory,
			bool includePrefix = true
		)
		{
			for(var i = 1; i <= items.Count; i++)
			{
				var item = items.ElementAt(i - 1);
				var isLast = i == items.Count;

				var isFirst = true;

				foreach(var line in itemSummaryFactory(item))
				{
					var prefix = "";

					if(includePrefix)
					{
						prefix = isFirst
							? (isLast ? " └" : " ├")
							: (isLast ? "  " : " │");
					}

					isFirst = false;

					yield return prefix + line;
				}
			}
		}
	}
}
