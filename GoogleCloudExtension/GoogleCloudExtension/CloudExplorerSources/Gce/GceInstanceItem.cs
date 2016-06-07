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

using Google.Apis.Compute.v1.Data;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Utils;
using System.ComponentModel;

namespace GoogleCloudExtension.CloudExplorerSources.Gce
{
    /// <summary>
    /// This class represents a GCE instance in the Properties Window.
    /// </summary>
    public class GceInstanceItem : PropertyWindowItemBase
    {
        private const string Category = "Instance Properties";

        protected Instance Instance { get; }

        public GceInstanceItem(Instance instance) : base(className: "Instance Properties", componentName: instance.Name)
        {
            Instance = instance;
        }

        [Category(Category)]
        [Description("The name of the instance")]
        public string Name => Instance.Name;

        [Category(Category)]
        [Description("The zone of the instance")]
        public string Zone => Instance.GetZoneName();

        [Category(Category)]
        [Description("The machine type for the instance")]
        [DisplayName("Machine Type")]
        public string MachineType => Instance.GetMachineType();

        [Category(Category)]
        [Description("The current status of the instance")]
        public string Status => Instance.Status;

        [Category(Category)]
        [Description("The interna IP address of the instance")]
        [DisplayName("Internal IP Address")]
        public string IpAddress => Instance.GetInternalIpAddress();

        [Category(Category)]
        [Description("The public IP address of the instance")]
        [DisplayName("External IP Address")]
        public string PublicIpAddress => Instance.GetPublicIpAddress();

        [Category(Category)]
        [Description("The tags for this instance.")]
        public string Tags => Instance.GetTags();
    }
}