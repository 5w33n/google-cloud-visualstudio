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

using Google;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.DataSources;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleCloudExtension.CloudExplorerSources.Gce
{
    public class GceSourceRootViewModel : SourceRootViewModelBase
    {
        private static readonly TreeLeaf s_loadingPlaceholder = new TreeLeaf
        {
            Caption = "Loading zones...",
            IsLoading = true
        };
        private static readonly TreeLeaf s_errorPlaceholder = new TreeLeaf
        {
            Caption = "Failed to load zones.",
            IsError = true
        };
        private static readonly TreeLeaf s_noItemsPlacehoder = new TreeLeaf
        {
            Caption = "No zones found."
        };
        private static readonly TreeLeaf s_noZonesPlaceholder = new TreeLeaf { Caption = "No zones" };

        private bool _showOnlyWindowsInstances = false;
        private IList<InstancesPerZone> _instancesPerZone;
        private Lazy<GceDataSource> _dataSource;

        public GceDataSource DataSource => _dataSource.Value;

        public override TreeLeaf ErrorPlaceholder => s_errorPlaceholder;

        public override TreeLeaf LoadingPlaceholder => s_loadingPlaceholder;

        public override TreeLeaf NoItemsPlaceholder => s_noItemsPlacehoder;

        public override string RootCaption => "Google Compute Engine";

        public bool ShowOnlyWindowsInstances
        {
            get { return _showOnlyWindowsInstances; }
            set
            {
                if (value == _showOnlyWindowsInstances)
                {
                    return;
                }
                _showOnlyWindowsInstances = value;

                PresentZoneViewModels();
            }
        }

        public override void Initialize(ICloudSourceContext context)
        {
            base.Initialize(context);

            InvalidateProjectOrAccount();
        }

        public override void InvalidateProjectOrAccount()
        {
            Debug.WriteLine("New credentials, invalidating data source for GCE");
            _dataSource = new Lazy<GceDataSource>(CreateDataSource);
        }

        private GceDataSource CreateDataSource()
        {
            if (CredentialsStore.Default.CurrentProjectId != null)
            {
                return new GceDataSource(
                    CredentialsStore.Default.CurrentProjectId,
                    CredentialsStore.Default.CurrentGoogleCredential,
                    GoogleCloudExtensionPackage.ApplicationName);
            }
            else
            {
                return null;
            }
        }

        protected override async Task LoadDataOverride()
        {
            try
            {
                _instancesPerZone = await _dataSource.Value.GetAllInstancesPerZonesAsync();
                PresentZoneViewModels();
            }
            catch (DataSourceException ex)
            {
                var innerEx = ex.InnerException as GoogleApiException;
                if (innerEx != null && innerEx.HttpStatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    Debug.WriteLine("The GCE API is not enabled.");

                    // Show the node that notifies users that the API is disabled.
                    Children.Clear();
                    Children.Add(new DisabledApiWarning(Context.CurrentProject));
                    return;
                }

                throw new CloudExplorerSourceException(ex.Message, ex);
            }
        }

        private void PresentZoneViewModels()
        {
            if (_instancesPerZone == null)
            {
                return;
            }

            var zones = GetZoneViewModels();
            Children.Clear();
            foreach (var zone in zones)
            {
                Children.Add(zone);
            }
            if (Children.Count == 0)
            {
                Children.Add(s_noZonesPlaceholder);
            }
        }

        private IList<ZoneViewModel> GetZoneViewModels()
        {
            return _instancesPerZone?
                .OrderBy(x => x.Zone.Name)
                .Select(x => new ZoneViewModel(this, x, _showOnlyWindowsInstances)).ToList();
        }
    }
}