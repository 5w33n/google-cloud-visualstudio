﻿using System.ComponentModel;

namespace GoogleCloudExtension.CloudExplorerSources.Gce
{
    public class GceInstanceItem
    {
        private const string Category = "Instance Properties";

        private readonly GceInstance _instance;

        public GceInstanceItem(GceInstance instance)
        {
            _instance = instance;
        }

        [Category(Category)]
        [Description("The name of the instance")]
        public string Name => _instance.Name;

        [Category(Category)]
        [Description("The zone of the instance")]
        public string Zone => _instance.ZoneName;

        [Category(Category)]
        [Description("The machine type for the instance")]
        public string MachineType => _instance.MachineType;

        [Category(Category)]
        [Description("The current status of the instance")]
        public string Status => _instance.Status;

        [Category(Category)]
        [Description("Whether this is an ASP.NET server")]
        public bool IsAspNet => _instance.Tags?.Items?.Contains("aspnet") ?? false;
    }
}