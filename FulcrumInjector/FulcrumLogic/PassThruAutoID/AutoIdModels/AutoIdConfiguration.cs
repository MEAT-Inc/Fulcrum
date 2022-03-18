using SharpWrap2534.PassThruTypes;

namespace FulcrumInjector.FulcrumLogic.PassThruAutoID.AutoIdModels
{
    /// <summary>
    /// Class object used to declare types for auto id routines
    /// </summary>
    public class AutoIdConfiguration
    {
        // Class values for pulling in new information about an AutoID routine
        public BaudRate ConnectBaud { get; set; }
        public TxFlags ConnectFlags { get; set; }
        public ProtocolId AutoIdType { get; set; }
        public FilterObject[] RoutineFilters { get; set; }
        public MessageObject[] RoutineCommands { get; set; }
    }
}
