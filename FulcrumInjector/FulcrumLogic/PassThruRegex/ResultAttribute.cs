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
    sealed class ResultAttribute : Attribute
    {
        // Name of result to set
        public readonly string ResultName;
        public readonly string PassedValue;
        public readonly string FailedValue;

        // ------------------------------------------------------------------------------

        /// <summary>
        /// This makes a result tag for the name given and sets to passed when the value matches the given result.
        /// </summary>
        /// <param name="Name">Name of result</param>
        /// <param name="DesiredResult">Value to equal.</param>
        public ResultAttribute(string Name, string PassedResult = "", string FailedResult = "REGEX_FAILED")
        {
            // Store values for attribute
            this.ResultName = Name;
            this.FailedValue = FailedResult;
            this.PassedValue = PassedResult;
        }

        // ------------------------------------------------------------------------------

        /// <summary>
        /// Returns true if the value matches the passed value or doesn't match the false value.
        /// </summary>
        /// <param name="InputValue"></param>
        /// <returns>True/False based on input values.</returns>
        public bool CheckValue(string InputValue)
        {
            // If anything but the given passes check and return.
            if (this.PassedValue == "") return this.FailedValue != InputValue;
            return InputValue.Contains(this.PassedValue) && !InputValue.Contains(this.FailedValue);
        }
    }
}
