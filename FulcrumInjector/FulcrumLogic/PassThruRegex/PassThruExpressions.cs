using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace FulcrumInjector.FulcrumLogic.PassThruRegex
{
    /// <summary>
    /// This class instance is used to help configure the Regex tools and commands needed to perform highlighting on output from
    /// the shim DLL.
    /// </summary>
    public class PassThruExpressions
    {
        // Static list of all results from regex operations.
        public static int ExpressionsChecked => ExpressionResults?.Count ?? 0;
        public static ObservableCollection<PtRegexResult> ExpressionResults { get; }
        
        // --------------------------------------------------------------------------------------------------------------

        // Static regex control values. These are used to control the PT Command reading routines.


    }
}
