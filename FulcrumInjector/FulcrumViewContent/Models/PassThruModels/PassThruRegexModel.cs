using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FulcrumInjector.FulcrumLogic.PassThruLogic.PassThruExpressions;
using FulcrumInjector.FulcrumLogic.PassThruLogic.PassThruExpressions.ExpressionObjects;
using Newtonsoft.Json;

namespace FulcrumInjector.FulcrumViewContent.Models.PassThruModels
{
    /// <summary>
    ///  Model object for imported regular expression values for a command part
    /// </summary>
    public class PassThruRegexModel
    {
        // Values for the regex object.
        public string ExpressionName { get; set; }
        public string ExpressionPattern { get; set; }
        public int[] ExpressionValueGroups { get; set; }
        public PassThruCommandType ExpressionType { get; set; }

        // ------------------------------------------------------------------------------------------------

        /// <summary>
        /// Json constuctor for this object type
        /// </summary>
        [JsonConstructor]
        public PassThruRegexModel() {  }
        /// <summary>
        /// Makes a new regex model object from the input values given
        /// </summary>
        /// <param name="ExpressionName"></param>
        /// <param name="ExpressionPattern"></param>
        /// <param name="ExpressionGroups"></param>
        public PassThruRegexModel(string ExpressionName, string ExpressionPattern, PassThruCommandType ExpressionType = PassThruCommandType.NONE, int ExpressionGroup = 0)
        {
            // Store model object values here.
            this.ExpressionName = ExpressionName;
            this.ExpressionType = ExpressionType;
            this.ExpressionPattern = ExpressionPattern;
            this.ExpressionValueGroups = new int[] { ExpressionGroup };
        }
        /// <summary>
        /// Makes a new regex model object from the input values given
        /// </summary>
        /// <param name="ExpressionName"></param>
        /// <param name="ExpressionPattern"></param>
        /// <param name="ExpressionGroups"></param>
        public PassThruRegexModel(string ExpressionName, string ExpressionPattern, PassThruCommandType ExpressionType = PassThruCommandType.NONE, int[] ExpressionGroups = null)
        {
            // Store model object values here.
            this.ExpressionName = ExpressionName;
            this.ExpressionType = ExpressionType;
            this.ExpressionPattern = ExpressionPattern;
            this.ExpressionValueGroups = ExpressionGroups ?? new int[] { 0 };
        }

        // ------------------------------------------------------------------------------------------------

        /// <summary>
        /// Processes the input line content and parses it for the regex we passed in.
        /// </summary>
        /// <param name="InputLines">Lines to check</param>
        /// <returns>Value matched.</returns>
        public bool Evaluate(string InputLines, out string[] ResultStrings)
        {
            // Build a regex, find our results.
            var MatchResults = new Regex(this.ExpressionPattern).Match(InputLines);

            // If failed, return an empty string. If all groups, return here too.
            if (!MatchResults.Success) {
                ResultStrings = new[] { "REGEX_FAILED" };
                return false;
            }

            // If no groups given, return full match
            if (this.ExpressionValueGroups.All(IndexObj => IndexObj == 0)) {
                ResultStrings = new[] { MatchResults.Value }; return true;
            }

            // Loop our pulled values out and store them
            List<string> PulledValues = new List<string>();
            for (int GroupIndex = 0; GroupIndex < MatchResults.Groups.Count; GroupIndex++) { 
                PulledValues.Add(MatchResults.Groups[GroupIndex].Value.Trim());
            }

            // Build output and return it.
            ResultStrings = PulledValues.ToArray();
            return true;
        }
    }
}
