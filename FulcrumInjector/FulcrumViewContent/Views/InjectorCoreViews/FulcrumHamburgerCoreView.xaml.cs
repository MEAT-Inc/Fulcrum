﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using FulcrumInjector.FulcrumLogic.JsonHelpers;
using FulcrumInjector.FulcrumViewContent.Models;
using FulcrumInjector.FulcrumViewContent.ViewModels.InjectorCoreViewModels;
using FulcrumInjector.FulcrumViewSupport;
using FulcrumInjector.FulcrumViewSupport.AppStyleSupport.AvalonEditHelpers;
using MahApps.Metro.Controls;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;
using Svg;

namespace FulcrumInjector.FulcrumViewContent.Views.InjectorCoreViews
{
    /// <summary>
    /// Interaction logic for FulcrumHamburgerCoreView.xaml
    /// </summary>
    public partial class FulcrumHamburgerCoreView : UserControl
    {
        // Logger object.
        private SubServiceLogger ViewLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("InjectorHamburgerViewLogger")) ?? new SubServiceLogger("InjectorHamburgerViewLogger");

        // ViewModel object to bind onto
        public FulcrumHamburgerCoreViewModel ViewModel { get; set; }

        // Navigation Helpers. Used to control moving around the menu
        private readonly HamburgerNavService NavService;

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds new logic for Hamburger Core output view
        /// </summary>
        public FulcrumHamburgerCoreView()
        {
            InitializeComponent();

            // Build new ViewModel
            this.ViewModel = new FulcrumHamburgerCoreViewModel();

            // Configure new Naviagation Service helper
            this.NavService = new HamburgerNavService();
            this.InjectorHamburgerMenu.Content = this.NavService.Frame;
            this.NavService.Navigated += this.NavigationServiceEx_OnNavigated;
        }

        /// <summary>
        /// On loaded, we want to setup our new viewmodel object and populate values
        /// </summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">Events attached to it.</param>
        private void FulcrumHamburgerCoreView_OnLoaded(object sender, RoutedEventArgs e)
        {
            // Setup a new ViewModel
            this.ViewModel.SetupViewControl(this);
            this.DataContext = this.ViewModel;

            // Setup Menu icon objects here.
            InjectorHamburgerMenu.ItemsSource = this.ViewModel.SetupHamburgerMenuItems();
            this.ViewLogger.WriteLog("SETUP AND STORED NEW MENU ENTRIES ON THE VIEW OK!", LogType.InfoLog);

            // Log built view contents ok
            InjectorHamburgerMenu.SelectedIndex = 0;
            this.ViewLogger.WriteLog("CONFIGURED VIEW CONTROL VALUES FOR FULCRUM HAMBURGER CORE OK!", LogType.InfoLog);
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// When a view is updated, run this method. Check the type of the menu objct, and then if possible, invoke changed view
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InjectorHamburgerMenu_OnItemInvoked(object sender, HamburgerMenuItemInvokedEventArgs e)
        {
            // Navigate assuming it's a type of nav menu item and the menu item can navigate
            if (e.InvokedItem is HamburgerNavMenuItem menuItemBuilt && menuItemBuilt.IsNavigation)
                this.NavService.Navigate(menuItemBuilt.NavigationDestination);
        }

        /// <summary>
        /// Event when view content is updated
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NavigationServiceEx_OnNavigated(object sender, NavigationEventArgs e)
        {
            // Select the menu item
            this.InjectorHamburgerMenu.SelectedItem = this.InjectorHamburgerMenu
              .Items
              .OfType<HamburgerNavMenuItem>()
              .FirstOrDefault(x => x.NavigationType == e.Content?.GetType());
        }

        // --------------------------------------------------------------------------------------------------------------------------

    }
}