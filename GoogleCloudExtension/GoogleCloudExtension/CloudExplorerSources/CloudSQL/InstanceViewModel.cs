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

using Google.Apis.SQLAdmin.v1beta4.Data;
using GoogleCloudExtension.Analytics;
using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.MySQLForVisualStudio;
using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.Data;
using Microsoft.VisualStudio.Shell;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;

namespace GoogleCloudExtension.CloudExplorerSources.CloudSQL
{
    class InstanceViewModel : TreeHierarchy, ICloudExplorerItemSource
    {
        // TODO(talarico): Create an icon for instances.
        private const string IconResourcePath = "CloudExplorerSources/Gcs/Resources/bucket_icon.png";
        private static readonly Lazy<ImageSource> s_instanceIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadImage(IconResourcePath));

        private readonly CloudSQLSourceRootViewModel _owner;
        private readonly DatabaseInstance _instance;
        private readonly Lazy<InstanceItem> _item;
        private readonly WeakCommand _openAddDataConnectionDialog;

        public event EventHandler ItemChanged;

        public object Item => _item.Value;

        public InstanceViewModel(CloudSQLSourceRootViewModel owner, DatabaseInstance instance)
        {
            _owner = owner;
            _instance = instance;
            _item = new Lazy<InstanceItem>(GetItem);
            _openAddDataConnectionDialog = new WeakCommand(OpenDataConnectionDialog);

            Caption = _instance.Name;
            Icon = s_instanceIcon.Value;

            var menuItems = new List<MenuItem>
            {
                new MenuItem { Header = "Add Data Connection", Command = _openAddDataConnectionDialog },
            };
            ContextMenu = new ContextMenu { ItemsSource = menuItems };
        }

        private void OpenDataConnectionDialog()
        {
            ExtensionAnalytics.ReportCommand(CommandName.OpenMySQLDataConnectionDialog, CommandInvocationSource.Button);

            // Create a data connection dialog and add all possible data sources to it.
            DataConnectionDialogFactory factory = (DataConnectionDialogFactory)Package.GetGlobalService(typeof(DataConnectionDialogFactory));
            DataConnectionDialog dialog = factory.CreateConnectionDialog();
            dialog.AddAllSources();

            // Check if the MySQL data source exists.
            if (dialog.AvailableSources.Contains(MySQLUtils.MySQLDataSource))
            {
                // Pre select the MySQL data source.
                dialog.SelectedSource = MySQLUtils.MySQLDataSource;

                // Create the connection string to pre populate the server address in the dialog.
                MySqlConnectionStringBuilder builderPrePopulate = new MySqlConnectionStringBuilder();
                InstanceItem instance = _item.Value;
                builderPrePopulate.Server = String.IsNullOrEmpty(instance.IpAddress) ? instance.Ipv6Address : instance.IpAddress;
                dialog.DisplayConnectionString = builderPrePopulate.GetConnectionString(false);

                bool addDataConnection = dialog.ShowDialog();
                if (addDataConnection)
                {
                    ExtensionAnalytics.ReportCommand(CommandName.AddMySQLDataConnection, CommandInvocationSource.Button);

                    // Create a name for the data connection
                    MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder(dialog.DisplayConnectionString);
                    string database = $"{_instance.Project}[{builder.Server}][{builder.Database}]";

                    // Add the MySQL data connection to the data explorer
                    DataExplorerConnectionManager manager = (DataExplorerConnectionManager)Package.GetGlobalService(typeof(DataExplorerConnectionManager));
                    manager.AddConnection(database, MySQLUtils.MySQLDataProvider, dialog.EncryptedConnectionString, true);
                }
            }
            else
            {
                // MySQL for Visual Studio isn't installed, prompt the user to install it.
                ExtensionAnalytics.ReportEvent("MySQLForVisualStudio", "Missing");
                MySQLForVisualStudioWindow.PromptUser();
            }
        }

        public InstanceItem GetItem() => new InstanceItem(_instance);
    }
}
