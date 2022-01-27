using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using FulcrumInjector.FulcrumLogic.ExtensionClasses;
using FulcrumInjector.FulcrumLogic.JsonHelpers;
using FulcrumInjector.FulcrumLogic.PassThruExpressions;
using FulcrumInjector.FulcrumLogic.PassThruExpressions.ExpressionObjects;
using FulcrumInjector.FulcrumViewContent.Models.SettingsModels;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumViewContent.Models.PassThruModels
{
    /// <summary>
    /// Static class object for the PT Regex object we've built from the settings page.
    /// </summary>
    public static class PassThruRegexModelShare
    {
        // Logger Object 

        private static SubServiceLogger RegexStoreLogger = (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("PassThruRegexModelShareLogger")) ?? new SubServiceLogger("PassThruRegexModelShareLogger");

        // --------------------------------------------------------------------------------------------------------------------------

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

        // --------------------------------------------------------------------------------------------------------------------------

        // Main PassThru command objects here.
        public static PassThruRegexModel PassThruOpen => PassThruExpressionObjects.GetRegexByName("PTOpen");
        public static PassThruRegexModel PassThruClose => PassThruExpressionObjects.GetRegexByName("PTClose");
        public static PassThruRegexModel PassThruConnect => PassThruExpressionObjects.GetRegexByName("PTConnect");
        public static PassThruRegexModel PassThruDisconnect => PassThruExpressionObjects.GetRegexByName("PTDisconnect");
        public static PassThruRegexModel PassThruReadMessages => PassThruExpressionObjects.GetRegexByName("PTReadMsgs");
        public static PassThruRegexModel PassThruWriteMessages => PassThruExpressionObjects.GetRegexByName("PTWriteMsgs");
        public static PassThruRegexModel PassThruStartMsgFilter => PassThruExpressionObjects.GetRegexByName("PTStartMsgFilter");
        public static PassThruRegexModel PassThruStopMsgFilter => PassThruExpressionObjects.GetRegexByName("PTStopMsgFilter");
        public static PassThruRegexModel PassThruIoctl => PassThruExpressionObjects.GetRegexByName("PTIoctl");

        // --------------------------------------------------------------------------------------------------------------------------

        // Helper Regex objects.
        public static PassThruRegexModel PassThruTime => PassThruExpressionObjects.GetRegexByName("CommandTime");
        public static PassThruRegexModel PassThruStatus => PassThruExpressionObjects.GetRegexByName("CommandStatus");
        public static PassThruRegexModel DeviceIdReturned => PassThruExpressionObjects.GetRegexByName("DeviceID");
        public static PassThruRegexModel ChannelIdReturned => PassThruExpressionObjects.GetRegexByName("ChannelID");
        public static PassThruRegexModel FilterIdReturned => PassThruExpressionObjects.GetRegexByName("FilterID");
        public static PassThruRegexModel NumberOfMessages => PassThruExpressionObjects.GetRegexByName("MessageCount");
        public static PassThruRegexModel MessageDataContent => PassThruExpressionObjects.GetRegexByName("MessageData");
        public static PassThruRegexModel MessageSentInfo => PassThruExpressionObjects.GetRegexByName("MessageSentInfo");
        public static PassThruRegexModel MessageReadInfo => PassThruExpressionObjects.GetRegexByName("MessageReadInfo");
        public static PassThruRegexModel MessageFilterInfo => PassThruExpressionObjects.GetRegexByName("MessageFilterInfo");
        public static PassThruRegexModel IoctlParameterValue => PassThruExpressionObjects.GetRegexByName("IoctlParameterValue");

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Configures a new set of PTregex objects.
        /// </summary>
        /// <returns></returns>
        public static ObservableCollection<PassThruRegexModel> GeneratePassThruRegexModels()
        {
            // Pull the objects from the settings store that relate to our expressions and then build an object from them.
            RegexStoreLogger.WriteLog($"REBUILDING STORE VALUES FOR INJECTOR REGEX COMMAND OBJECTS NOW...", LogType.WarnLog);
            var RegexModelArray = FulcrumSettingsShare.InjectorRegexSettings.SettingsEntries.Select(SettingObj =>
            {
                // Find our group binding values here.
                var GroupValueMatch = Regex.Match(SettingObj.SettingValue.ToString(), @"\*GROUPS_\(((\d+|\d,)+)\)\*");
                var SettingGroups = GroupValueMatch.Success ? GroupValueMatch.Groups[1].Value : "0";

                // Now build an array of int values for groups.
                int[] ArrayOfGroups;
                try
                {
                    // Try to parse out values here. If Failed default to all
                    if (!SettingGroups.Contains(",")) { ArrayOfGroups = new[] { int.Parse(SettingGroups) }; }
                    else
                    {
                        // Split content out, parse values, and return.
                        var SplitGroupValues = SettingGroups.Split(',');
                        ArrayOfGroups = SplitGroupValues.Select(int.Parse).ToArray();
                    }
                }
                catch { ArrayOfGroups = new[] { 0 }; }

                // Build our new object for the model of regex now.
                var SettingNameSplit = SettingObj.SettingName.Split(' ').ToArray();
                var RegexName = string.Join("", SettingNameSplit.Where(StringObj => !StringObj.Contains("Regex")))
                    .Trim();
                Enum.TryParse(RegexName.Replace("PassThru", "PT"), out PassThruCommandType ExpressionType);
                var RegexPattern = SettingObj.SettingValue.ToString().Replace(GroupValueMatch.Value, string.Empty)
                    .Trim();

                // Return our new output object here.
                return new PassThruRegexModel(
                    RegexName,          // Name of command. Just the input setting with no spaces
                    RegexPattern,       // Pattern used during regex operations (No group value)
                    ExpressionType,     // Type of expression. Defined for PTCommands or none for base
                    ArrayOfGroups       // Index set of groups to use
                );
            }).ToArray();

            // Store new values and move onto selection
            RegexStoreLogger.WriteLog($"BUILT A TOTAL OF {RegexModelArray.Length} OUTPUT OBJECTS!", LogType.InfoLog);
            _passThruExpressionObjects = new ObservableCollection<PassThruRegexModel>(RegexModelArray);
            return PassThruExpressionObjects;
        }
        /// <summary>
        /// Build a new regex model object from a given name value for a regex.
        /// </summary>
        /// <param name="RegexName"></param>
        /// <returns></returns>
        private static PassThruRegexModel GetRegexByName(this ObservableCollection<PassThruRegexModel> RegexModelSet, string RegexName)
        {
            // Finds the first Regex object matching the current name provided from a collection instance.
            return RegexModelSet.FirstOrDefault(RegexObj => RegexObj.ExpressionName.ToUpper().Contains(RegexName.ToUpper()));
        }
    }
}