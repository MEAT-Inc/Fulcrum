using System;
using System.Windows;
using MahApps.Metro.Controls;

namespace FulcrumInjector.FulcrumViewSupport
{
    /// <summary>
    /// Dependency property object for navigation menu types
    /// </summary>
    public class FulcrumNavMenuItem : HamburgerMenuGlyphItem
    {
        // Sets if we can navigate on this object or not.
        public bool IsNavigation => this.NavUserControlType != null && this.NavViewModelType != null;

        // Destination property based on Type
        public static readonly DependencyProperty NavUserControlTypeProperty = DependencyProperty.Register(
            nameof(NavUserControlType), typeof(Type), typeof(FulcrumNavMenuItem), new PropertyMetadata(default(Type)));

        // Destination property based on Type
        public static readonly DependencyProperty NavViewModelTypeProperty = DependencyProperty.Register(
            nameof(NavViewModelType), typeof(Type), typeof(FulcrumNavMenuItem), new PropertyMetadata(default(Type)));

        // --------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Publicly controlled type for destination
        /// </summary>
        public Type NavUserControlType
        {
            get => (Type)this.GetValue(NavUserControlTypeProperty);
            set => this.SetValue(NavUserControlTypeProperty, value);
        }
        /// <summary>
        /// Publicly controlled type for destination view model
        /// </summary>
        public Type NavViewModelType
        {
            get => (Type)this.GetValue(NavViewModelTypeProperty);
            set => this.SetValue(NavViewModelTypeProperty, value);
        }
    }
}
