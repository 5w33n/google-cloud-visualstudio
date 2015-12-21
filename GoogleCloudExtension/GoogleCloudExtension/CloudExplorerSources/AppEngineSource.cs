﻿using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.GCloud.Models;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.CloudExplorerSources
{
    internal class AppEngineSource : ICloudExplorerSource
    {
        private AppEngineRoot _root;

        public AppEngineSource()
        {
            // Add a weak event handler to receive notifications of the deployment of app engine instances.
            // We also need to invalidate the list if the account or project changed.
            var handler = new WeakAction<object, EventArgs>(this.InvalidateAppEngineAppList);
            ExtensionEvents.AppEngineDeployed += handler.Invoke;
            GCloudWrapper.Instance.AccountOrProjectChanged += handler.Invoke;

            _root = new AppEngineRoot();

            // Start loading apps.
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
                return;
            }

            try
            {
                _root.Children.Clear();
                _root.Children.Add(new TreeLeaf { Content = "Loading..." });
                var apps = await AppEngineClient.GetAppEngineAppListAsync();
                var nodes = apps
                    .GroupBy(x => x.Module)
                    .OrderBy(x => x.Key)
                    .Select(x => MakeModuleHierarchy(x))
                    .ToList();

                _root.Children.Clear();
                foreach(var node in nodes)
                {
                    _root.Children.Add(node);
                }
            }
            catch (GCloudException ex)
            {
                AppEngineOutputWindow.OutputLine("Failed to load the list of AppEngine apps.");
                AppEngineOutputWindow.OutputLine(ex.Message);
                AppEngineOutputWindow.Activate();
            }
        }

        private TreeHierarchy MakeModuleHierarchy(IGrouping<string, ModuleAndVersion> src)
        {
            var versions = src
                .OrderBy(x => x, new VersionComparer())
                .Select(x => new ModuleAndVersionViewModel(this, x));
            return new TreeHierarchy(versions) { Content = src.Key };
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
