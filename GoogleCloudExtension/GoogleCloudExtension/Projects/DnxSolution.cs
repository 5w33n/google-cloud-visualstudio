﻿// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace GoogleCloudExtension.Projects
{
    internal class DnxSolution
    {
        private readonly Solution _solution;

        internal DnxSolution(Solution solution)
        {
            _solution = solution;
        }

        public static DnxSolution CurrentSolution
        {
            get
            {
                return GetCurrentSolution();
            }
        }

        public DnxProject StartupProject
        {
            get
            {
                return GetStartupProject();
            }
        }

        public IList<DnxProject> Projects
        {
            get
            {
                return GetSolutionProjects();
            }
        }

        private SolutionBuild2 SolutionBuild
        {
            get
            {
                return _solution.SolutionBuild as SolutionBuild2;
            }
        }

        private static DnxSolution GetCurrentSolution()
        {
            var dte = Package.GetGlobalService(typeof(DTE)) as DTE;
            return new DnxSolution(dte.Solution);
        }

        private DnxProject GetStartupProject()
        {
            var sb = this.SolutionBuild;
            if (sb == null)
            {
                Debug.WriteLine("No startup project, no solution loaded yet.");
                return null;
            }

            var startupProjects = sb.StartupProjects as Array;
            if (startupProjects == null)
            {
                Debug.WriteLine("No startup projects in solution.");
                return null;
            }

            string startupProjectName = startupProjects.GetValue(0) as string;
            if (startupProjectName == null)
            {
                Debug.WriteLine("No startup project name.");
                return null;
            }

            try
            {
                return GetProjectFromName(startupProjectName);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private DnxProject GetProjectFromName(string name)
        {
            var solutionDirectory = Path.GetDirectoryName(_solution.FullName);
            var projectDirectory = Path.GetDirectoryName(name);
            var projectPath = Path.Combine(solutionDirectory, projectDirectory);

            // Only return a DnxProject if the project is indeed a DnxProject otherwise just return
            // null. This is for the case when a Non-Dnx project is opened in VS.
            if (DnxProject.IsDnxProject(projectPath))
            {
                return new DnxProject(projectPath);
            }
            else
            {
                return null;
            }
        }

        private IList<DnxProject> GetSolutionProjects()
        {
            List<DnxProject> result = new List<DnxProject>();

            var solutionDirectory = Path.GetDirectoryName(_solution.FullName);
            var globalJsonPath = Path.Combine(solutionDirectory, "global.json");
            if (File.Exists(globalJsonPath))
            {
                try
                {
                    var globalJsonContents = File.ReadAllText(globalJsonPath);
                    var globalJson = JsonConvert.DeserializeObject<DnxGlobalJson>(globalJsonContents);
                    foreach (var dir in globalJson.Projects)
                    {
                        FindProjects(Path.Combine(solutionDirectory, dir), result);
                    }
                }
                catch (JsonException)
                {
                    return result;
                }
            }
            else
            {
                // If no global.json file is found then we attempt at looking for projects in the
                // solution directory, this can happen if a solution was created from loose project.json files.
                Debug.WriteLine("No global.json found.");
                FindProjects(solutionDirectory, result);
            }
            return result;
        }

        private void FindProjects(string projectContainer, List<DnxProject> result)
        {
            if (!Directory.Exists(projectContainer))
            {
                Debug.WriteLine($"The project container {projectContainer} doesn't exist.");
                return;
            }

            var possibleProjects = Directory.GetDirectories(projectContainer);
            foreach (var project in possibleProjects)
            {
                if (!DnxProject.IsDnxProject(project))
                {
                    continue;
                }
                Debug.WriteLine($"Found project: {project}");
                result.Add(new DnxProject(project));
            }
        }
    }
}
