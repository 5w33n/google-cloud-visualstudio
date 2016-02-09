﻿// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.GCloud.Models;
using GoogleCloudExtension.Utils;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Media;

namespace GoogleCloudExtension.CloudExplorerSources.AppEngine
{
    internal class AppEngineSource : ICloudExplorerSource
    {
        private const string IconResourcePath = "CloudExplorerSources/AppEngine/Resources/ic_view_module.png";
        private static readonly Lazy<ImageSource> s_moduleIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadResource(IconResourcePath));
        private static readonly TreeLeaf s_loadingPlaceholder = new TreeLeaf
        {
            Content = "Loading modules...",
            IsLoading = true
        };

        private readonly AppEngineRootViewModel _root = new AppEngineRootViewModel();

        public AppEngineSource()
        {
            // Add a weak event handler to receive notifications of the deployment of app engine instances.
            // We also need to invalidate the list if the account or project changed.
            var handler = new WeakAction<object, EventArgs>(this.InvalidateAppEngineAppList);
            ExtensionEvents.AppEngineDeployed += handler.Invoke;
            GCloudWrapper.Instance.AccountOrProjectChanged += handler.Invoke;

            LoadAppEngineAppListAsync();
        }

        #region ICloudExplorerSource

        public TreeHierarchy GetRoot()
        {
            return _root;
        }

        public void Refresh()
        {
            // Reload the apps.
            LoadAppEngineAppListAsync();
        }

        #endregion

        /// <summary>
        /// Loads the list of app engine apps, changing the state of the properties
        /// as the process advances.
        /// </summary>
        internal async void LoadAppEngineAppListAsync()
        {
            if (!GCloudWrapper.Instance.ValidateGCloudInstallation())
            {
                Debug.WriteLine("Cannot find GCloud, disabling the AppEngine tool window.");
                _root.Children.Clear();
                _root.Children.Add(new TreeLeaf { Content = "Please install gcloud..." });
                return;
            }

            try
            {
                _root.Children.Clear();
                _root.Children.Add(s_loadingPlaceholder);
                _root.IsExpanded = true;
                var apps = await AppEngineClient.GetAppEngineAppListAsync();
                var nodes = apps
                    .GroupBy(x => x.Module)
                    .OrderBy(x => x.Key)
                    .Select(x => MakeModuleHierarchy(x))
                    .ToList();

                _root.Children.Clear();
                foreach (var node in nodes)
                {
                    _root.Children.Add(node);
                }
            }
            catch (GCloudException ex)
            {
                GcpOutputWindow.OutputLine("Failed to load the list of AppEngine apps.");
                GcpOutputWindow.OutputLine(ex.Message);
                GcpOutputWindow.Activate();
            }
        }

        private TreeHierarchy MakeModuleHierarchy(IGrouping<string, ModuleAndVersion> src)
        {
            var versions = src
                .OrderBy(x => x, new VersionComparer())
                .Select(x => new ModuleAndVersionViewModel(this, x));
            return new TreeHierarchy(versions) { Content = src.Key, Icon = s_moduleIcon.Value };
        }

        #region Command handlers

        private void OnRefresh()
        {
            LoadAppEngineAppListAsync();
        }

        #endregion

        private void InvalidateAppEngineAppList(object src, EventArgs args)
        {
            Debug.WriteLine("AppEngine app list invalidated.");
            LoadAppEngineAppListAsync();
        }
    }
}
