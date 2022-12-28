using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using FulcrumInjector.FulcrumViewContent.Models.SettingsModels;
using FulcrumInjector.FulcrumViewContent.Models.SimulationModels;
using Newtonsoft.Json;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumViewSupport.DataContentHelpers
{
    /// <summary>
    /// Template Selector for simulation events. Picks a channel or message event data template based on type provided
    /// </summary>
    public class SimEventDataTemplateSelector : DataTemplateSelector
    {
        // Logger object.
        private static SubServiceLogger TemplateLogger => (SubServiceLogger)LoggerQueue.SpawnLogger("SimEventDataTemplateLogger", LoggerActions.SubServiceLogger);

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Data Template location routine
        /// Pulls in the type of the item in the template and then finds a style based on the output
        /// </summary>
        /// <param name="InputItem"></param>
        /// <param name="ObjectContainer"></param>
        /// <returns></returns>
        public override DataTemplate SelectTemplate(object InputItem, DependencyObject ObjectContainer)
        {
            // Check if we can use this selector object or not.
            if (ObjectContainer is FrameworkElement InputElement && InputItem is SimulationEventObject SimEventObject)
            {
                // Now find the type of control to use
                if (SimEventObject is SimChannelEventObject) return InputElement.FindResource("ChannelContentTemplate") as DataTemplate;
                if (SimEventObject is SimMessageEventObject) return InputElement.FindResource("MessageContentTemplate") as DataTemplate;

                // If failed, log the failure and exit out
                TemplateLogger.WriteLog($"FAILED TO FIND NEW CONTROL TYPE FOR OBJECT TYPE {SimEventObject.GetType().Name}!", LogType.ErrorLog);
                return null;
            }

            // Failed to find control template output
            TemplateLogger.WriteLog("ERROR! INVALID CONTROL TYPE WAS PROCESSED! NOT RETURNING A DATATEMPLATE FOR IT", LogType.ErrorLog);
            TemplateLogger.WriteLog($"CONTROL PASSED CONTENT: {JsonConvert.SerializeObject(InputItem, Formatting.None)}", LogType.TraceLog);
            return null;
        }
    }
}