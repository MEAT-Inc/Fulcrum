using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using FulcrumInjector.ViewControl.Models;

namespace FulcrumInjector.AppLogic
{
    /// <summary>
    /// Builds a new template selection object which helps us find templates
    /// </summary>
    public class UserSettingsDataTemplateSelector : DataTemplateSelector
    {
        /// <summary>
        /// Data Template location routine
        /// Pulls in the type of the item in the template and then finds a style based on the output
        /// </summary>
        /// <param name="InputItem"></param>
        /// <param name="ObjectContainer"></param>
        /// <returns></returns>
        public override DataTemplate SelectTemplate(object InputItem, DependencyObject ObjectContainer)
        {
            FrameworkElement InputElement = ObjectContainer as FrameworkElement;
            if (InputElement != null && InputItem != null && InputItem is SettingsEntryModel)
            {
                // Cast into object model here
                SettingsEntryModel SettingModelObject = InputItem as SettingsEntryModel;
                Window window = Application.Current.MainWindow;

                // Now find the type of control to use
                switch (SettingModelObject.TypeOfControl)
                {
                    case SpecialFeatures.None:
                        return
                            InputElement.FindResource("AuctionItem_None")
                                as DataTemplate;
                    case SpecialFeatures.Color:
                        return
                            InputElement.FindResource("AuctionItem_Color")
                                as DataTemplate;
                }
            }

            return null;
        }
    }
}
