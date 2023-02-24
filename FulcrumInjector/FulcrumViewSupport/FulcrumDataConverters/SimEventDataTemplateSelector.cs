using System;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;
using SharpLogging;
using SharpSimulator;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumDataConverters
{
    /// <summary>
    /// Template Selector for simulation events. Picks a channel or message event data template based on type provided
    /// </summary>
    internal class SimEventDataTemplateSelector : DataTemplateSelector
    {
        #region Custom Events
        #endregion //Custom Events

        #region Fields

        // Logger object to log failures from our template instance
        private static SharpLogger _templateLogger;

        #endregion //Fields

        #region Properties
        #endregion //Properties

        #region Structs and Classes
        #endregion //Structs and Classes
        
        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Data Template location routine
        /// Pulls in the type of the item in the template and then finds a style based on the output
        /// </summary>
        /// <param name="InputItem"></param>
        /// <param name="ObjectContainer"></param>
        /// <returns></returns>
        public override DataTemplate SelectTemplate(object InputItem, DependencyObject ObjectContainer)
        {
            // Configure the logger instance if needed
            _templateLogger ??= new SharpLogger(LoggerActions.UniversalLogger);

            // Check if we can use this selector object or not.
            if (ObjectContainer is FrameworkElement InputElement && InputItem is EventArgs SimEventObject)
            {
                // Now find the type of control to use
                if (SimEventObject is PassThruSimulationPlayer.SimChannelEventArgs) 
                    return InputElement.FindResource("ChannelContentTemplate") as DataTemplate;
                if (SimEventObject is PassThruSimulationPlayer.SimMessageEventArgs)
                    return InputElement.FindResource("MessageContentTemplate") as DataTemplate;

                // If failed, log the failure and exit out
                _templateLogger.WriteLog($"FAILED TO FIND NEW CONTROL TYPE FOR OBJECT TYPE {SimEventObject.GetType().Name}!", LogType.ErrorLog);
                return null;
            }

            // Failed to find control template output
            _templateLogger.WriteLog("ERROR! INVALID CONTROL TYPE WAS PROCESSED! NOT RETURNING A DATATEMPLATE FOR IT", LogType.ErrorLog);
            _templateLogger.WriteLog($"CONTROL PASSED CONTENT: {JsonConvert.SerializeObject(InputItem, Formatting.None)}", LogType.TraceLog);
            return null;
        }
    }
}