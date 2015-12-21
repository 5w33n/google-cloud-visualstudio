﻿using GoogleCloudExtension.GCloud.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.CloudExplorerSources
{
    /// <summary>
    /// This comparer will ensure that the resulting order has the default version as the
    /// top of the list, followed by the rest of the versions sorted alphabetically by their
    /// names.
    /// </summary>
    internal class VersionComparer : IComparer<ModuleAndVersion>
    {
        public int Compare(ModuleAndVersion x, ModuleAndVersion y)
        {
            // There's only one default version, so both having the default bit
            // set means is the same version.
            if (x.IsDefault && y.IsDefault)
            {
                return 0;
            }

            // Ensure the default version is first.
            if (x.IsDefault)
            {
                return -1;
            }
            else if (y.IsDefault)
            {
                return 1;
            }

            // No default version, compare by name.
            return x.Version.CompareTo(y.Version);
        }
    }
}
