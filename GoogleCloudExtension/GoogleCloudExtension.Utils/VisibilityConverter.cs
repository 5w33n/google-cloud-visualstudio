﻿// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// Converts a boolean value into a Visibility enum value. True will mean Visible and False Collapsed.
    /// Note: Only Convert is implemented, so this is not a bidirectional converter, do not use on TwoWay bindings.
    /// </summary>
    public class VisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Determine if the value to convert should be negated before the conversion
        /// takes place, that is, if <c>IsNegated</c> is <c>True</c> then when converting
        /// <c>False</c> will be visible and <c>True</c> will be collapsed.
        /// </summary>
        public bool IsNegated { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool)
            {
                bool toConvert = (!IsNegated && ((bool)value)) || (IsNegated && !((bool)value));
                return toConvert ? Visibility.Visible : Visibility.Collapsed;
            }
            Debug.WriteLine($"Value should be a boolean: {value}");
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
