using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FulcrumInjector.FulcrumLogic.PassThruRegex
{
    /// <summary>
    /// Result Attribute for a Regex command operation
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    sealed class PtRegexResult : Attribute
    {
        // Name of result to set
        public readonly bool FailOnMatch;
        public readonly string ResultName;
        public readonly string ResultValue;

        // Private values for setting true/false here.
        private readonly string _failedState;
        private readonly string _passedState;
        
        // ------------------------------------------------------------------------------

        /// <summary>
        /// This makes a result tag for the name given and sets to passed when the value matches the given result.
        /// </summary>
        /// <param name="Name">Name of result</param>
        /// <param name="DesiredResult">Value to equal.</param>
        public PtRegexResult(string Name, string ResultValue = "", string[] ResultStates = null, bool OnFail = false)
        {
            // Store values for attribute
            this.ResultName = Name;
            this.FailOnMatch = OnFail;
            this.ResultValue = ResultValue;

            // Store result values.
            ResultStates ??= new[] { "Result Valid", "Result Invalid" };
            this._passedState = ResultStates[0];
            this._failedState = ResultStates[1];
        }
        /// <summary>
        /// This makes a result tag for the name given and sets to passed when the value matches the given result.
        /// </summary>
        /// <param name="Name">Name of result</param>
        /// <param name="DesiredResult">Value to equal.</param>
        public PtRegexResult(string Name)
        {
            // Store values for attribute
            this.ResultName = Name;
            this._passedState = "Value Validated";
            this._failedState = "Value Was Invalid!";
        }

        // ------------------------------------------------------------------------------

        /// <summary>
        /// Returns true if the value matches the passed value or doesn't match the false value.
        /// </summary>
        /// <param name="InputValue"></param>
        /// <returns>True/False based on input values.</returns>
        public string ResultState(string InputValue)
        {
            // If no value set to compare, return true every time
            if (string.IsNullOrWhiteSpace(FailOnMatch ? _failedState : _passedState))
                return FailOnMatch ? _failedState : _passedState;

            // If this fails when matched, then return the inverse of the result.
            if (FailOnMatch) return InputValue.Contains(this.ResultValue) ? _failedState : _passedState;
            return InputValue.Contains(this.ResultValue) ? _passedState : _failedState;
        }
    }
}
