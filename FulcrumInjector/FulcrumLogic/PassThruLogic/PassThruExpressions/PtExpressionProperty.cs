using System;
using System.Runtime.CompilerServices;

namespace FulcrumInjector.FulcrumLogic.PassThruLogic.PassThruExpressions
{
    /// <summary>
    /// Result Attribute for a Regex command operation
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    sealed class PtExpressionProperty : Attribute
    {
        // Name of result to set
        public readonly int LineNumber;
        public readonly bool FailOnMatch;
        public readonly string ResultName;
        public readonly string ResultValue;

        // Private values for setting true/false here.
        private readonly string _failedState;
        private readonly string _passedState;

        // ------------------------------------------------------------------------------

        /// <summary>
        /// This makes a result tag for the name given and sets to passed when the value matches the given result.
        /// Set OnFail to true to make the value fail when it matches the input
        /// </summary>
        /// <param name="Name">Name of result</param>
        /// <param name="DesiredResult">Value to equal.</param>
        public PtExpressionProperty(string Name, string ResultValue = "", string[] ResultStates = null, bool OnFail = false, [CallerLineNumber]int LineNumber = 0)
        {
            // Store values for attribute
            this.ResultName = Name;
            this.FailOnMatch = OnFail;
            this.LineNumber = LineNumber;
            this.ResultValue = ResultValue;

            // Store result values.
            ResultStates ??= new[] { "Value Validated", "Value Was Invalid!" };
            this._passedState = ResultStates[0];
            this._failedState = ResultStates[1];
        }
        
        // ------------------------------------------------------------------------------

        /// <summary>
        /// Returns true if the value matches the passed value or doesn't match the false value.
        /// </summary>
        /// <param name="InputValue"></param>
        /// <returns>True/False based on input values.</returns>
        public string ResultState(string InputValue)
        {
            // If no value set to compare, return true every time. Check input as well
            if (string.IsNullOrWhiteSpace(FailOnMatch ? _failedState : _passedState))
                return FailOnMatch ? _failedState : _passedState;
            if (string.IsNullOrWhiteSpace(InputValue)) 
                return FailOnMatch ? _failedState : _passedState;

            // If this fails when matched, then return the inverse of the result.
            if (FailOnMatch) return InputValue.Contains(this.ResultValue) ? _failedState : _passedState;
            return InputValue.Contains(this.ResultValue) ? _passedState : _failedState;
        }
    }
}
