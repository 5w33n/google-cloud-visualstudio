﻿// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.GCloud.Dnx;
using GoogleCloudExtension.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Utils
{
    internal static class CommandUtils
    {
        public static bool ValidateEnvironment()
        {
            var validDNXInstallation = DnxEnvironment.ValidateDnxInstallationForRuntime(DnxRuntime.DnxCore50) ||
                DnxEnvironment.ValidateDnxInstallationForRuntime(DnxRuntime.Dnx451);
            var validGCloudInstallation = GCloudWrapper.Instance.ValidateGCloudInstallation();
            return validDNXInstallation && validGCloudInstallation;
        }
    }
}
