﻿// Copyright 2016 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleCloudExtension.CloudExplorerSources.Gae
{
    /// <summary>
    /// This class represents a view of a GAE version in the Google Cloud Explorer Window.
    /// </summary>
    class VersionViewModel : TreeHierarchy, ICloudExplorerItemSource
    {
        private static readonly TreeLeaf s_loadingPlaceholder = new TreeLeaf
        {
            Caption = Resources.CloudExplorerGaeLoadingInstancesCaption,
            IsLoading = true
        };
        private static readonly TreeLeaf s_noItemsPlacehoder = new TreeLeaf
        {
            Caption = Resources.CloudExplorerGaeNoInstancesFoundCaption,
            IsWarning = true
        };
        private static readonly TreeLeaf s_errorPlaceholder = new TreeLeaf
        {
            Caption = Resources.CloudExplorerGaeFailedToLoadInstancesCaption,
            IsError = true
        };

        private readonly ServiceViewModel _owner;

        private bool _resourcesLoaded;

        public readonly Google.Apis.Appengine.v1.Data.Version version;

        public event EventHandler ItemChanged;

        public object Item => GetItem();

        public VersionViewModel(
            ServiceViewModel owner, Google.Apis.Appengine.v1.Data.Version version)
        {
            _owner = owner;
            this.version = version;

            Caption = GetCaption();

            _resourcesLoaded = false;
            Children.Add(s_loadingPlaceholder);
        }

        protected override async void OnIsExpandedChanged(bool newValue)
        {
            base.OnIsExpandedChanged(newValue);
            try
            {
                // If this is the first time the node has been expanded load it's resources.
                if (!_resourcesLoaded && newValue)
                {
                    _resourcesLoaded = true;
                    var instances = await LoadInstanceList();
                    Children.Clear();
                    if (instances == null)
                    {
                        Children.Add(s_errorPlaceholder);
                    }
                    else
                    {
                        foreach (var item in instances)
                        {
                            Children.Add(item);
                        }
                        if (Children.Count == 0)
                        {
                            Children.Add(s_noItemsPlacehoder);
                        }
                    }
                }
            }
            catch (DataSourceException ex)
            {
                GcpOutputWindow.OutputLine(Resources.CloudExplorerGaeFailedInstancesMessage);
                GcpOutputWindow.OutputLine(ex.Message);
                GcpOutputWindow.Activate();

                throw new CloudExplorerSourceException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Load a list of instances.
        /// </summary>
        private async Task<List<InstanceViewModel>> LoadInstanceList()
        {
            var instances = await _owner.DataSource.Value.GetInstanceListAsync(_owner.service.Id, version.Id);
            return instances?.Select(x => new InstanceViewModel(this, x)).ToList();
        }

        /// <summary>
        /// Get a caption for a the version.
        /// Formated as 'versionId (traffic%)' if a traffic allocation is present, 'versionId' otherwise.
        /// </summary>
        private string GetCaption()
        {
            double? trafficAllocation = GaeServiceExtensions.GetTrafficAllocation(_owner.service, version.Id);
            if (trafficAllocation == null)
            {
                return version.Id;
            }
            string percent = ((double)trafficAllocation).ToString("P", CultureInfo.InvariantCulture);
            return String.Format("{0} ({1})", version.Id, percent);
        }

        public VersionItem GetItem() => new VersionItem(version);
    }
}
