﻿// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.Projects;
using System;
using System.Globalization;
using System.Windows.Data;

namespace GoogleCloudExtension.DeploymentDialog
{
    internal class AspNetRuntimeValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var runtime = (AspNetRuntime)value;
            return DnxRuntime.GetRuntimeDisplayName(runtime);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
