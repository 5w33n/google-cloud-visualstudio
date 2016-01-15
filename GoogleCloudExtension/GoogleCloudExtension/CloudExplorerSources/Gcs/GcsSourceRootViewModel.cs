﻿using System;
using GoogleCloudExtension.CloudExplorer;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using GoogleCloudExtension.GCloud;
using System.Linq;
using System.Windows.Media;
using GoogleCloudExtension.Utils;

namespace GoogleCloudExtension.CloudExplorerSources.Gcs
{
    internal class GcsSourceRootViewModel : TreeHierarchy
    {
        private const string IconResourcePath = "CloudExplorerSources/Gcs/Resources/storage.png";
        private static readonly Lazy<ImageSource> s_storageIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadResource(IconResourcePath));

        private bool _loaded = false;
        private bool _loading = false;

        public GcsSourceRootViewModel()
        {
            Content = "Google Cloud Storage";
            Icon = s_storageIcon.Value;
            IsExpanded = false;
            Children.Add(new TreeLeaf { Content = "Loading..." });
        }

        protected override async void OnIsExpandedChanged(bool newValue)
        {
            if (_loading)
            {
                return;
            }
            if (newValue && !_loaded)
            {
                try
                {
                    _loading = true;
                    Debug.WriteLine("Loading list of buckets.");
                    var buckets = await LoadBucketList();
                    Children.Clear();
                    foreach (var item in buckets)
                    {
                        Children.Add(item);
                    }
                    if (Children.Count == 0)
                    {
                        Children.Add(new TreeLeaf { Content = "No buckets" });
                    }
                    _loaded = true;
                }
                finally
                {
                    _loading = false;
                }
            }
        }

        private async Task<List<BucketViewModel>> LoadBucketList()
        {
            var currentCredentials = await GCloudWrapper.Instance.GetCurrentCredentialsAsync();
            var buckets = await GcsDataSource.GetBucketListAsync(currentCredentials.ProjectId);
            return buckets.Select(x => new BucketViewModel(x)).ToList();
        }
    }
}