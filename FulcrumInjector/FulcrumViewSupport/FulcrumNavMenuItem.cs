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
        #region Custom Events
        #endregion //Custom Events

        #region Fields
        #endregion //Fields

        #region Properties

        // Public type definitions used to configure our dependency properties for navigation events
        public Type NavUserControlType
        {
            get => (Type)this.GetValue(NavUserControlTypeProperty);
            set => this.SetValue(NavUserControlTypeProperty, value);
        }
        public Type NavViewModelType
        {
            get => (Type)this.GetValue(NavViewModelTypeProperty);
            set => this.SetValue(NavViewModelTypeProperty, value);
        }

        // Public facing property that tells us if we can navigate on this object or not.
        public bool IsNavigation => this.NavUserControlType != null && this.NavViewModelType != null;

        // Public dependency properties holding the types for the ViewModel and UserControl to navigate to
        public static readonly DependencyProperty NavUserControlTypeProperty = DependencyProperty.Register(
            nameof(NavUserControlType), typeof(Type), typeof(FulcrumNavMenuItem), new PropertyMetadata(default(Type)));
        public static readonly DependencyProperty NavViewModelTypeProperty = DependencyProperty.Register(
            nameof(NavViewModelType), typeof(Type), typeof(FulcrumNavMenuItem), new PropertyMetadata(default(Type)));

        #endregion //Properties

        #region Structs and Classes
        #endregion //Structs and Classes
    }
}
