using SharpWrap2534.PassThruTypes;

namespace FulcrumInjector.FulcrumLogic.PassThruAutoID.AutoIdModels
{
    /// <summary>
    /// Message object pulled from the app settings json file for Auto ID routines
    /// </summary>
    public struct MessageObject
    {
        // Flags and message information
        public string MessageData;
        public TxFlags MessageFlags;
        public ProtocolId MessageProtocol;
    }
    /// <summary>
    /// Filter object pulled from the app settings json file for auto ID routines
    /// </summary>
    public struct FilterObject
    {
        // Flags and Type
        public TxFlags FilterFlags;
        public FilterDef FilterType;
        public ProtocolId FilterProtocol;

        // Filter content values
        public MessageObject FilterMask;
        public MessageObject FilterPattern;
        public MessageObject FilterFlowControl;
    }
}