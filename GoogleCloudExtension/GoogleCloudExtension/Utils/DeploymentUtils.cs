﻿// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.Analytics;
using GoogleCloudExtension.DeploymentDialog;
using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.GCloud.Dnx;
using GoogleCloudExtension.Projects;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Utils
{
    public static class DeploymentUtils
    {
        /// <summary>
        /// Starts the deployment process for the given project, if the project doesn't target the
        /// right runtime or the environment is not set then this becomes a NOOP.
        /// </summary>
        /// <param name="startupProject"></param>
        /// <param name="serviceProvider"></param>
        public static void StartProjectDeployment(Project startupProject, IServiceProvider serviceProvider)
        {
            ActivityLogUtils.LogInfo($"Starting the deployment process for project {startupProject.Name}.");

            // Validate the environment before attempting to start the deployment process.
            if (!CommandUtils.ValidateEnvironment())
            {
                ActivityLogUtils.LogError("Deployment invoked when the environment is not valid.");
                VsShellUtilities.ShowMessageBox(
                    serviceProvider,
                    "Please ensure that the Google Cloud SDK is installed and available in the path and that the preview, app and alpha components are installed.",
                    "The Google Cloud SDK command-line tool (gcloud) could not be found",
                    OLEMSGICON.OLEMSGICON_CRITICAL,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return;
            }

            // We only support the CoreCLR runtime.
            if (startupProject.Runtime == DnxRuntime.DnxCore50)
            {
                var window = new DeploymentDialogWindow(new DeploymentDialogWindowOptions
                {
                    Project = startupProject,
                    ProjectsToRestore = SolutionHelper.CurrentSolution.Projects,
                });
                window.ShowModal();
            }
            else
            {
                var runtime = DnxRuntimeInfo.GetRuntimeInfo(startupProject.Runtime);
                ExtensionAnalytics.ReportEvent("RuntimeNotSupportedError", startupProject.Runtime.ToString());
                GcpOutputWindow.OutputLine($"Runtime {runtime.DisplayName} is not supported for project {startupProject.Name}");
                VsShellUtilities.ShowMessageBox(
                    serviceProvider,
                    $"Runtime {runtime.DisplayName} is not supported. Project {startupProject.Name} needs to target {DnxRuntimeInfo.DnxCore50DisplayString}.",
                    "Runtime not supported",
                    OLEMSGICON.OLEMSGICON_INFO,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }

        public static async Task<bool> DeployProjectAsync(
            Project startupProject,
            IList<Project> projects,
            string versionName,
            bool makeDefault,
            bool preserveOutput,
            Credentials accountAndProject)
        {
            bool result = false;

            try
            {
                ActivityLogUtils.LogInfo("AppEngine deployment started.");
                StatusbarHelper.SetText("Deployment to AppEngine started...");
                GcpOutputWindow.Activate();
                GcpOutputWindow.Clear();
                GcpOutputWindow.OutputLine("Deployment to AppEngine started...");
                GcpOutputWindow.OutputLine($"Deploying project {startupProject}.");
                GcpOutputWindow.OutputLine($"Deploying to cloud project id {accountAndProject.ProjectId} for acccount {accountAndProject.Account}");
                StatusbarHelper.Freeze();
                GoogleCloudExtensionPackage.IsDeploying = true;

                StatusbarHelper.ShowDeployAnimation();
                await AppEngineClient.DeployApplicationAsync(
                    startupProjectPath: startupProject.Root,
                    projectPaths: projects.Select(x => x.Root).ToList(),
                    versionName: versionName,
                    promoteVersion: makeDefault,
                    callback: GcpOutputWindow.OutputLine,
                    preserveOutput: preserveOutput,
                    accountAndProject: accountAndProject);
                StatusbarHelper.UnFreeze();

                StatusbarHelper.SetText("Deployment Succeeded");
                GcpOutputWindow.OutputLine("Deployment Succeeded.");

                ActivityLogUtils.LogInfo("AppEngine deployment finished.");

                result = true;
            }
            catch (GCloudException ex)
            {
                GcpOutputWindow.OutputLine("Deployment Failed.");
                GcpOutputWindow.OutputLine(ex.Message);

                ActivityLogUtils.LogError("AppEngine deployment failed.");
            }
            catch (Exception)
            {
                GcpOutputWindow.OutputLine("Deployment Failed!!!");
                StatusbarHelper.SetText("Deployment Failed");

                ActivityLogUtils.LogError("AppEngine deployment failed.");
            }
            finally
            {
                StatusbarHelper.UnFreeze();
                StatusbarHelper.HideDeployAnimation();
                GoogleCloudExtensionPackage.IsDeploying = false;
                ExtensionEvents.RaiseAppEngineDeployed();
            }

            return result;
        }
    }
}
