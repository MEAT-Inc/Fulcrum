using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media;
using FulcrumInjector.FulcrumLogic.PassThruExpressions.ExpressionObjects;
using FulcrumInjector.FulcrumViewContent.Models.PassThruModels;
using FulcrumInjector.FulcrumViewSupport.DataConverters;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumViewSupport.AvalonEditHelpers.InjectorSyntaxFormatters
{
    /// <summary>
    /// Colors the stacktrace on the log line
    /// </summary>
    public class TypeAndTimeColorFormatter : InjectorDocFormatterBase
    {
        /// <summary>
        /// Builds a new color format helping object.
        /// </summary>
        public TypeAndTimeColorFormatter(OutputFormatHelperBase FormatBase) : base(FormatBase) 
        {
            // Log the type of object built on our helper instance and then return out.
            this.FormatLogger.WriteLog($"BUILT NEW {this.GetType().Name} FORMAT HELPER!", LogType.TraceLog);
        }

        // --------------------------------------------------------------------------------------------------

        /// <summary>
        /// Color our line output for the stack trace
        /// </summary>
        /// <param name="InputLine"></param>
        protected override void ColorizeLine(DocumentLine InputLine)
        {
            // Find the command type for our input object here. If none, drop out
            Regex TimeMatchRegex = new(PassThruRegexModelShare.PassThruTime.ExpressionPattern);
            MatchCollection MatchesFound = TimeMatchRegex.Matches(CurrentContext.Document.GetText(InputLine));

            // Now run our coloring definitions and return out.
            this.ColorNewMatches(InputLine, MatchesFound);
            this.FormatLogger.WriteLog($"FORMATTED NEW OUTPUT FOR TYPE {this.GetType().Name} CORRECTLY!", LogType.TraceLog);
        }
    }
}
