using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FulcrumService
{
    /// <summary>
    /// Base class object holding our configuration for a service setup
    /// </summary>
    public class FulcrumServiceSettings
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields
        #endregion // Fields

        #region Properties
       
        // Public facing properties holding our service configuration
        public bool ServiceEnabled { get; set; }
        public string ServiceName { get; set; }

        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes
    }
}
