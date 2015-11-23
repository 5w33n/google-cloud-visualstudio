﻿// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.GCloud.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleCloudExtension.GCloud
{
    /// <summary>
    /// This class wraps the gcloud command and offers up some of its services in a
    /// as async methods. It also manages the its own notion of "current user" and 
    /// "current project".
    /// This class is a singleton.
    /// </summary>
    public sealed class GCloudWrapper
    {
        // Environment variables to specify the credentials to the Gcloud CLI.
        private const string CloudSdkCoreAccountVar = "CLOUDSDK_CORE_ACCOUNT";
        private const string CloudSdkCoreProjectVar = "CLOUDSDK_CORE_PROJECT";

        /// <summary>
        /// Maintains the currently selected account and project for the instance.
        /// </summary>
        private Credentials _currentAccountAndProject;

        /// <summary>
        /// Singleton for the class.
        /// </summary>
        public static GCloudWrapper Instance { get; } = new GCloudWrapper();

        /// <summary>
        /// This event is raised whenever the current account or project has changed;
        /// there's no notification of what the new values are so the caller has to call
        /// to find out what the new values are.
        /// </summary>
        public event EventHandler AccountOrProjectChanged;

        /// <summary>
        /// Private to enforce the singleton.
        /// </summary>
        private GCloudWrapper()
        { }

        private void RaiseAccountOrProjectChanged()
        {
            AccountOrProjectChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Calculates what the current project and account is, it might return what "gcloud"
        /// things is the current account and project or what this instance override is. This is
        /// hidden from the caller.
        /// </summary>
        /// <returns>The current AccountAndProjectId.</returns>
        public async Task<Credentials> GetCurrentCredentialsAsync()
        {
            if (_currentAccountAndProject == null)
            {
                // Fetching the current account and project, for gcloud, does not need to use the current
                // account.
                var settings = await GetJsonOutputAsync<Settings>("config list", credentials: null);
                _currentAccountAndProject = new Credentials(
                    account: settings.CoreSettings.Account,
                    projectId: settings.CoreSettings.Project);
            }
            return _currentAccountAndProject;
        }

        /// <summary>
        /// Updates the local concet of "current account and project" in the instance *only* it does not
        /// affect what gcloud thinks is the current account and project.
        /// </summary>
        /// <param name="accountAndProject">The new accountAndProject to use</param>
        public void UpdateUserAndProject(Credentials accountAndProject)
        {
            _currentAccountAndProject = accountAndProject;
            RaiseAccountOrProjectChanged();
        }

        public void UpdateProject(string projectId)
        {
            var newAccountAndProject = new Credentials(_currentAccountAndProject.Account, projectId);
            UpdateUserAndProject(newAccountAndProject);
        }

        /// <summary>
        /// Finds the location of gcloud.cmd by following all of the directories in the PATH environment
        /// variable until it finds it. With this we assume that in order to run the extension gcloud.cmd is
        /// in the PATH.
        /// </summary>
        /// <returns></returns>
        private static string GetGCloudPath()
        {
            return Environment.GetEnvironmentVariable("PATH")
                .Split(';')
                .Select(x => Path.Combine(x, "gcloud.cmd"))
                .Where(x => File.Exists(x))
                .FirstOrDefault();
        }

        private IDictionary<string, string> GetEnvironmentForCredentials(Credentials credentials)
        {
            if (credentials != null)
            {
                return new Dictionary<string, string>
                {
                    { CloudSdkCoreAccountVar, credentials.Account },
                    { CloudSdkCoreProjectVar, credentials.ProjectId }
                };
            }
            return null;
        }

        private string FormatCommand(string command, bool useJson)
        {
            var jsonFormatParam = useJson ? "--format=json" : "";
            return $"/c gcloud {jsonFormatParam} {command}";
        }

        public async Task RunCommandAsync(string command, Action<string> callback, Credentials credentials)
        {
            var actualCommand = FormatCommand(command, useJson: false);
            var envVars = GetEnvironmentForCredentials(credentials);
            Debug.WriteLine($"Executing gcloud command: {actualCommand}");
            var result = await ProcessUtils.RunCommandAsync("cmd.exe", actualCommand, (s, e) => callback(e.Line), envVars);
            if (!result)
            {
                throw new GCloudException($"Failed to execute: {actualCommand}");
            }
        }

        public async Task RunCommandAsync(string command, Action<string> callback)
        {
            await RunCommandAsync(command, callback, await GetCurrentCredentialsAsync());
        }

        public async Task<string> GetCommandOutputAsync(string command, Credentials credentials)
        {
            var actualCommand = FormatCommand(command, useJson: false);
            var envVars = GetEnvironmentForCredentials(credentials);
            Debug.WriteLine($"Executing gcloud command: {actualCommand}");
            var output = await ProcessUtils.GetCommandOutputAsync("cmd.exe", actualCommand, envVars);
            if (!output.Succeeded)
            {
                throw new GCloudException($"Failed with message: {output.Error}");
            }
            return output.Output;
        }

        public async Task<string> GetCommandOutputAsync(string command)
        {
            return await GetCommandOutputAsync(command, await GetCurrentCredentialsAsync());
        }

        public async Task<T> GetJsonOutputAsync<T>(string command, Credentials credentials)
        {
            var actualCommand = FormatCommand(command, useJson: true);
            var envVars = GetEnvironmentForCredentials(credentials);
            try
            {
                Debug.Write($"Executing gcloud command: {actualCommand}");
                return await ProcessUtils.GetJsonOutputAsync<T>("cmd.exe", actualCommand, envVars);
            }
            catch (JsonOutputException ex)
            {
                throw new GCloudException($"Failed to execute command {actualCommand}\nInner message:\n{ex.Message}", ex);
            }
        }

        public async Task<T> GetJsonOutputAsync<T>(string command)
        {
            return await GetJsonOutputAsync<T>(command, await GetCurrentCredentialsAsync());
        }

        /// <summary>
        /// Returns the access token for this class' notion of current user.
        /// </summary>
        /// <returns>The string representation of the access token.</returns>
        public Task<string> GetAccessTokenAsync()
        {
            return GetCommandOutputAsync("auth print-access-token");
        }

        /// <summary>
        /// Returns the accounts registered with gcloud.
        /// </summary>
        /// <returns>The accounts.</returns>
        public async Task<IEnumerable<string>> GetAccountsAsync()
        {
            // Getting the list of accounts needs to not filter down by the current account
            // being used or nothing will be shown, so we don't need to use the current account.
            var settings = await GetJsonOutputAsync<AccountSettings>("auth list", credentials: null);
            return settings.Accounts;
        }

        /// <summary>
        /// Returns the list of projects accessible by the current account.
        /// </summary>
        /// <returns>List of projects.</returns>
        public Task<IList<CloudProject>> GetProjectsAsync()
        {
            return GetJsonOutputAsync<IList<CloudProject>>("alpha projects list");
        }

        public async Task<IList<CloudProject>> GetProjectsAsync(Credentials credentials)
        {
            return await GetJsonOutputAsync<IList<CloudProject>>("alpha projects list", credentials);
        }

        public bool ValidateGCloudInstallation()
        {
            Debug.WriteLine("Validating GCloud installation.");
            var gcloudPath = GetGCloudPath();
            Debug.WriteLineIf(gcloudPath == null, "Cannot find gcloud.cmd in the system.");
            Debug.WriteLineIf(gcloudPath != null, $"Found gcloud.cmd at {gcloudPath}");
            return gcloudPath != null;
        }
    }
}
