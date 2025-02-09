﻿using System;
using System.Windows;
using System.Windows.Controls;

namespace Onbox.Mvc.VDev
{
    /// <summary>
    /// Interaction logic for ErrorIcon.xaml
    /// </summary>
    [Obsolete("Use component 'Error' instead")]
    public partial class ErrorIcon : UserControl
    {
        public ErrorIcon()
        {
            InitializeComponent();
        }

        private void OnRetryClicked(object sender, RoutedEventArgs e)
        {
            var component = VisualTreeHelpers.GetParentMvcComponent(this);
            if (component != null)
            {
                component.OnErrorRetry();
            }
        }
    }
}