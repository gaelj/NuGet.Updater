﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NeoGet.Contracts;
using NeoGet.Entities;
using NeoGet.Helpers;
using NeoGet.Tools.Hierarchy.Entities;
using NeoGet.Tools.Hierarchy.Extensions;
using NuGet.Common;
using NuGet.Packaging;
using NuGet.Packaging.Core;

namespace NeoGet.Tools.Hierarchy
{
	public class NuGetHierarchy
	{
		private readonly ILogger _log;
		private readonly string _target;
		private readonly IEnumerable<IPackageFeed> _sources;

		public NuGetHierarchy(string target, IEnumerable<IPackageFeed> sources, ILogger log)
		{
			_target = target;
			_sources = sources;
			_log = log;
		}

		public async Task<PackageHierarchy> RunAsync(CancellationToken ct)
		{
			var references = await SolutionHelper.GetPackageReferences(ct, _target, FileType.All, _log);
			var identities = new HashSet<PackageIdentity>(references.Select(r => r.Identity));

			var packages = await GetPackagesWithDependencies(ct, identities);

			return new PackageHierarchy(_target, packages);
		}

		private async Task<IEnumerable<PackageHierarchyItem>> GetPackagesWithDependencies(
			CancellationToken ct,
			IEnumerable<PackageIdentity> packages,
			IEnumerable<PackageHierarchyItem> knownPackages = null
		)
		{
			var resolvedPackages = await Task.WhenAll(packages.Select(p => GetHierarchy(ct, p)));

			knownPackages = resolvedPackages.Concat(knownPackages ?? Array.Empty<PackageHierarchyItem>());

			var packagesToRetrieve = resolvedPackages
				.SelectMany(p => p.GetDependenciesIdentities()) //Get resolved dependencies
				.Except(knownPackages.Select(i => i.Identity)) //Remove the packages we already have dependencies for or not
				.ToArray();

			if(packagesToRetrieve.Any())
			{
				//Get the packages still needed
				var subDependencies = await GetPackagesWithDependencies(ct, packagesToRetrieve, knownPackages);

				foreach(var item in resolvedPackages.Where(i => i.Dependencies != null).SelectMany(i => i.Dependencies.Values.SelectMany(x => x)))
				{
					item.Dependencies = subDependencies.FirstOrDefault(i => i.Identity == item.Identity)?.Dependencies;
				}
			}

			return resolvedPackages;
		}

		private async Task<PackageHierarchyItem> GetHierarchy(CancellationToken ct, PackageIdentity package)
		{
			PackageHierarchyItem hierarchy = null;

			foreach(var source in _sources)
			{
				try
				{
					var dependencies = await source.GetDependencies(ct, package);

					hierarchy = new PackageHierarchyItem(package, dependencies);

					_log.LogInformation($"Found {hierarchy.Dependencies.Count} dependencies for {package}");
				}
				catch(PackageNotFoundException ex)
				{
					_log.LogInformation(ex.Message);
				}
			}

			if(hierarchy == null)
			{
				hierarchy = new PackageHierarchyItem(package);
			}

			return hierarchy;
		}
	}
}
