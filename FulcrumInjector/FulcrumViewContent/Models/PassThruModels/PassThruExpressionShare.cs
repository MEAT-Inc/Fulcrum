using System.Collections.ObjectModel;
using System.Linq;
using FulcrumInjector.FulcrumLogic.ExtensionClasses;
using FulcrumInjector.FulcrumLogic.JsonHelpers;
using FulcrumInjector.FulcrumLogic.PassThruRegex;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumViewContent.Models.PassThruModels
{
    /// <summary>
    /// Static class object for the PT Regex object we've built from the settings page.
    /// </summary>
    public static class PassThruExpressionShare
    {
        // PassThru Regex options.
        private static ObservableCollection<PassThruRegexModel> _passThruExpressionObjects;
        public static ObservableCollection<PassThruRegexModel> PassThruExpressionObjects
        {
            get
            {
                // Check if this value has been configured yet or not.
                _passThruExpressionObjects ??= GeneratePassThruRegexModels();
                return _passThruExpressionObjects;
            }
        }

        // Regex objects defined by names here.
        public static PassThruRegexModel PassThruTime => PassThruExpressionObjects.GetRegexByName("PassThruTime");
        public static PassThruRegexModel PassThruStatus => PassThruExpressionObjects.GetRegexByName("PassThruStatus");

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Configures a new set of PTregex objects.
        /// </summary>
        /// <returns></returns>
        public static ObservableCollection<PassThruRegexModel> GeneratePassThruRegexModels()
        {
            // Get Logger instance 
            var RegexStoreLogger = (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
                .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("PassThruRegexModelShareLogger")) ?? new SubServiceLogger("PassThruRegexModelShareLogger");

            // Find a regex object on our static share of them to determine the objet to build.
            RegexStoreLogger.WriteLog("IMPORTING PASSTHRU REGEX OBJECT CONTENTS NOW...", LogType.WarnLog);
            var LoadedValues = ValueLoaders.GetConfigValue<PassThruRegexModel[]>("FulcrumRegularExpressions");

            // Store new values and move onto selection
            RegexStoreLogger.WriteLog($"BUILT A TOTAL OF {LoadedValues.Length} OUTPUT OBJECTS!", LogType.InfoLog);
            _passThruExpressionObjects = new ObservableCollection<PassThruRegexModel>(LoadedValues);
            return PassThruExpressionObjects;
        }
    }
}
