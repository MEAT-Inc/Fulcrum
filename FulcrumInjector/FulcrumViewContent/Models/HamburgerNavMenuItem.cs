using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using MahApps.Metro.Controls;

namespace FulcrumInjector.FulcrumViewContent.Models
{
    /// <summary>
    /// Dependency property object for navigation menu types
    /// </summary>
    public class HamburgerNavMenuItem : HamburgerMenuGlyphItem
    {
        public static readonly DependencyProperty NavigationDestinationProperty = DependencyProperty.Register(
                nameof(NavigationDestination), typeof(Uri), typeof(HamburgerNavMenuItem), new PropertyMetadata(default(Uri)));

        public Uri NavigationDestination
        {
            get => (Uri)this.GetValue(NavigationDestinationProperty);
            set => this.SetValue(NavigationDestinationProperty, value);
        }

        public static readonly DependencyProperty NavigationTypeProperty = DependencyProperty.Register(
            nameof(NavigationType), typeof(Type), typeof(HamburgerNavMenuItem), new PropertyMetadata(default(Type)));

        public Type NavigationType
        {
            get => (Type)this.GetValue(NavigationTypeProperty);
            set => this.SetValue(NavigationTypeProperty, value);
        }

        public bool IsNavigation => this.NavigationDestination != null;
    }
}
