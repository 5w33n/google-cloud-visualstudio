﻿// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.GCloud.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// Contains the result of running detection for the gcloud SDK.
    /// </summary>
    public class GCloudValidationResult
    {
        /// <summary>
        /// Whether gcloud SDK is installed.
        /// </summary>
        public bool IsGCloudInstalled { get; }

        /// <summary>
        /// What missing components needed from the extension are there.
        /// </summary>
        public IReadOnlyCollection<Component> MissingComponents { get; }

        public GCloudValidationResult(
            bool gcloudInstalled,
            IEnumerable<Component> missingComponents)
        {
            IsGCloudInstalled = gcloudInstalled;
            MissingComponents = new ReadOnlyCollection<Component>(missingComponents.ToList());
        }

        /// <summary>
        /// Returns if everything is ok with the gcloud SDK installation.
        /// </summary>
        /// <returns>True if the installation is fine, false otherwise.</returns>
        public bool IsValidGCloudInstallation() => IsGCloudInstalled && MissingComponents.Count == 0;

        public string GetDisplayString()
        {
            if (!IsGCloudInstalled)
            {
                return "Please install GCloud SDK.";
            }
            else
            {
                return "Please install the missing gcloud components.";
            }
        }

        /// <summary>
        /// Returns a string representation of the result.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder resultBuilder = new StringBuilder();

            if (!IsGCloudInstalled)
            {
                resultBuilder.AppendLine("Need to install gcloud");
            }

            if (MissingComponents.Count != 0)
            {
                resultBuilder.AppendLine("Missing components:");
                foreach (var component in MissingComponents)
                {
                    resultBuilder.AppendLine($"  Component: {component.Id}");
                }
            }

            return resultBuilder.ToString();
        }
    }
}