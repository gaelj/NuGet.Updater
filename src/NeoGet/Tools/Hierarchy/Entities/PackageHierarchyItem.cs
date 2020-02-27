﻿using System.Collections.Generic;
using System.Linq;
using NeoGet.Extensions;
using NuGet.Frameworks;
using NuGet.Packaging.Core;

namespace NeoGet.Tools.Hierarchy.Entities
{
	public class PackageHierarchyItem
	{
		public PackageHierarchyItem(PackageIdentity identity)
		{
			Identity = identity;
		}

		public PackageHierarchyItem(PackageIdentity identity, IDictionary<NuGetFramework, PackageHierarchyItem[]> dependencies)
			: this(identity)
		{
			Dependencies = new Dictionary<NuGetFramework, PackageHierarchyItem[]>(dependencies);
		}

		public PackageHierarchyItem(PackageIdentity identity, IDictionary<NuGetFramework, PackageDependency[]> dependencies)
			: this(identity, dependencies.ToDictionary(g => g.Key, g => g.Value.Select(d => new PackageHierarchyItem(d.GetIdentity())).ToArray()))
		{
		}

		public PackageIdentity Identity { get; set; }

		public Dictionary<NuGetFramework, PackageHierarchyItem[]> Dependencies { get; set; }

		public override string ToString() => Identity.ToString();
	}
}
