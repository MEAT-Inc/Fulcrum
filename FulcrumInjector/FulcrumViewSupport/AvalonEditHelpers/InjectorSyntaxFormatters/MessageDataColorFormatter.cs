using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumViewContent.Models.PassThruModels;
using ICSharpCode.AvalonEdit.Document;

namespace FulcrumInjector.FulcrumViewSupport.AvalonEditHelpers.InjectorSyntaxFormatters
{
    /// <summary>
    /// Used to color and format message data contents out inside our injector log file outputs
    /// </summary>
    public class MessageDataColorFormatter : InjectorDocFormatterBase
    {
        /// <summary>
        /// Builds a new color format helping object.
        /// </summary>
        public MessageDataColorFormatter(OutputFormatHelperBase FormatBase) : base(FormatBase) { }

        // --------------------------------------------------------------------------------------------------

        /// <summary>
        /// Color our line output for the stack trace
        /// </summary>
        /// <param name="InputLine"></param>
        protected override void ColorizeLine(DocumentLine InputLine)
        {
            // Find the message data values here
            Regex MessageDataRegex = new(PassThruRegexModelShare.MessageDataContent.ExpressionPattern);
            Match FoundMatch = MessageDataRegex.Match(CurrentContext.Document.GetText(InputLine));
            if (!FoundMatch.Success) return;

            // Now run our coloring definitions and return out
            this.ColorNewMatches(InputLine, FoundMatch);
        }
    }
}
