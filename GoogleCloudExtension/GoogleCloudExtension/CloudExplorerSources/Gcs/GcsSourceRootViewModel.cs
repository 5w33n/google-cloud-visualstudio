﻿// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.CloudExplorerSources.Utils;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;

namespace GoogleCloudExtension.CloudExplorerSources.Gcs
{
    internal class GcsSourceRootViewModel : TreeHierarchy
    {
        private const string IconResourcePath = "CloudExplorerSources/Gcs/Resources/storage.png";
        private static readonly Lazy<ImageSource> s_storageIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadResource(IconResourcePath));

        private static readonly TreeLeaf s_loadingPlaceholder = new TreeLeaf
        {
            Content = "Loading buckets...",
            IsLoading = true
        };
        private static readonly TreeLeaf s_noItemsPlacehoder = new TreeLeaf
        {
            Content = "No buckets found."
        };
        private static readonly TreeLeaf s_errorPlaceholder = new TreeLeaf
        {
            Content = "Failed to list buckets.",
            IsError = true
        };

        private bool _loaded = false;
        private bool _loading = false;

        public GcsSourceRootViewModel()
        {
            Content = "Google Cloud Storage";
            Icon = s_storageIcon.Value;
            IsExpanded = false;
            Children.Add(s_loadingPlaceholder);
        }

        protected override async void OnIsExpandedChanged(bool newValue)
        {
            if (_loading)
            {
                return;
            }
            if (newValue && !_loaded)
            {
                await LoadBuckets();
            }
        }

        private async Task LoadBuckets()
        {
            try
            {
                _loading = true;

                var gcloudValidationResult = await EnvironmentUtils.ValidateGCloudInstallation();
                if (!gcloudValidationResult.IsValidGCloudInstallation())
                {
                    Children.Clear();
                    Children.Add(CommonUtils.GetErrorItem(gcloudValidationResult));
                }
                else
                {
                    Debug.WriteLine("Loading list of buckets.");
                    var buckets = await LoadBucketList();
                    Children.Clear();
                    if (buckets == null)
                    {
                        Children.Add(s_errorPlaceholder);
                    }
                    else
                    {
                        foreach (var item in buckets)
                        {
                            Children.Add(item);
                        }
                        if (Children.Count == 0)
                        {
                            Children.Add(s_noItemsPlacehoder);
                        }
                    }
                }

                _loaded = true;
            }
            catch (DataSourceException ex)
            {
                GcpOutputWindow.OutputLine("Failed to load the list of GCS buckets.");
                GcpOutputWindow.OutputLine(ex.Message);
                GcpOutputWindow.Activate();

                Children.Clear();
                Children.Add(s_errorPlaceholder);
            }
            finally
            {
                _loading = false;
            }
        }

        private async Task<List<BucketViewModel>> LoadBucketList()
        {
            var currentCredentials = await GCloudWrapper.Instance.GetCurrentCredentialsAsync();
            var oauthToken = await GCloudWrapper.Instance.GetAccessTokenAsync();
            var buckets = await GcsDataSource.GetBucketListAsync(currentCredentials.ProjectId, oauthToken);
            return buckets?.Select(x => new BucketViewModel(x)).ToList();
        }

        internal async Task Refresh()
        {
            if (!_loaded)
            {
                return;
            }

            _loaded = false;
            ResetChildren();
            await LoadBuckets();
        }

        private void ResetChildren()
        {
            Children.Clear();
            Children.Add(s_loadingPlaceholder);
        }
    }
}