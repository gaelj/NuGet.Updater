﻿using System;
using System.Collections.Generic;
using System.Linq;
using NeoGet.Tools.Hierarchy.Entities;
using NuGet.Frameworks;
using NuGet.Packaging.Core;

namespace NeoGet.Tools.Hierarchy.Extensions
{
	public static class PackageHierarchyItemExtensions
	{
		public static IEnumerable<PackageIdentity> GetAllIdentities(this PackageHierarchy hierarchy)
			=> hierarchy?.Packages?.SelectMany(i => i.GetAllIdentities()).Distinct() ?? Array.Empty<PackageIdentity>();

		private static IEnumerable<PackageIdentity> GetAllIdentities(this PackageHierarchyItem hierarchyItem)
			=> new[] { hierarchyItem.Identity }
			.Concat(hierarchyItem.Dependencies?.SelectMany(p => p.Value.SelectMany(i => i.GetAllIdentities())) ?? Array.Empty<PackageIdentity>());

		public static IEnumerable<PackageIdentity> GetDependenciesIdentities(this PackageHierarchyItem hierarchyItem)
			=> hierarchyItem.Dependencies?.SelectMany(d => d.Value).Select(i => i.Identity).Distinct() ?? Array.Empty<PackageIdentity>();

		public static Dictionary<NuGetFramework, PackageHierarchyItem[]> GetMatchingDependencies(this PackageHierarchyItem item, NuGetFramework framework)
		{
			var dependencies = new Dictionary<NuGetFramework, PackageHierarchyItem[]>();

			if(item.Dependencies == null)
			{
				return dependencies;
			}
			else if(framework == null)
			{
				dependencies = item.Dependencies;
			}
			else
			{
				var nearestFramework = new FrameworkReducer().GetNearest(framework, item.Dependencies.Keys);

				dependencies = item.Dependencies
					.Where(d => d.Key == nearestFramework)
					.Take(1)
					.ToDictionary(p => p.Key, p => p.Value);
			}

			return dependencies
				.Where(p => p.Value?.Any() ?? false)
				.ToDictionary(p => p.Key, p => p.Value);
		}
	}
}
