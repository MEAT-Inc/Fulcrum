﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FulcrumInjector.FulcrumViewSupport.DataContentHelpers
{
    /// <summary>
    /// Class used for casting bools to inverse bool values
    /// This was being done using a second bool value but wasn't true MVVM so I just wrote this little subclass of the
    /// booleanToOutput Converter I built which takes generics and converts them into outputs as defined by the CTOR 
    /// </summary>
    public sealed class InverseBoolConverter : BooleanToOutputConverter<bool>
    {
        /// <summary>
        /// Builds a new converter object class here.
        /// </summary>
        /// <param name="TrueValue">Value for visible</param>
        /// <param name="FalseValue">Value for hidden</param>
        public InverseBoolConverter() : base(false, true) { }
    }
}
