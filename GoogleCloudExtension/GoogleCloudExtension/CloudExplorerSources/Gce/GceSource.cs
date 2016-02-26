﻿// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using System;
using System.Collections.Generic;
using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.Utils;
using System.Windows.Media;

namespace GoogleCloudExtension.CloudExplorerSources.Gce
{
    internal class GceSource : CloudExplorerSourceBase
    {
        private const string WindowsOnlyButtonIconPath = "CloudExplorerSources/Gce/Resources/gce_logo.png";
        private static readonly Lazy<ImageSource> s_windowsOnlyButtonIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadResource(WindowsOnlyButtonIconPath));

        private readonly GceSourceRootViewModel _root = new GceSourceRootViewModel();
        private readonly ButtonDefinition _windowsOnlyButton;
        private readonly IEnumerable<ButtonDefinition> _buttons;

        public GceSource()
        {
            _windowsOnlyButton = new ButtonDefinition
            {
                ToolTip = "Only Windows Instances",
                Command = new WeakCommand(OnOnlyWindowsClicked),
                Icon = s_windowsOnlyButtonIcon.Value,
                IsChecked = true,
            };

            _buttons = new List<ButtonDefinition>
            {
                _windowsOnlyButton,
            };
        }

        private void OnOnlyWindowsClicked()
        {
            _windowsOnlyButton.IsChecked = !_windowsOnlyButton.IsChecked;
        }

        public override TreeHierarchy GetRoot()
        {
            return _root;
        }

        public override void Refresh()
        {
            _root.Refresh();
        }

        public override IEnumerable<ButtonDefinition> GetButtons() => _buttons;
    }
}
