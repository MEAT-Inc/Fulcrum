#pragma once

#include <stdint.h>

/**************************/
/* ProtocolID definitions */
/**************************/

enum e_protocol_id {
	// J2534-1
	J1850VPW = 1,
	J1850PWM = 2,
	ISO9141 = 3,
	ISO14230 = 4,
	CAN = 5,
	ISO15765 = 6,
	SCI_A_ENGINE = 7,
	SCI_A_TRANS = 8,
	SCI_B_ENGINE = 9,
	SCI_B_TRANS = 10,

	// J2534-2 Pin Switched ProtocolIDs
	J1850VPW_PS = 0x8000,
	J1850PWM_PS = 0x8001,
	ISO9141_PS = 0x8002,
	ISO14230_PS = 0x8003,
	CAN_PS = 0x8004,
	ISO15765_PS = 0x8005,
	J2610_PS = 0x8006,
	SW_ISO15765_PS = 0x8007,
	SW_CAN_PS = 0x8008,
	GM_UART_PS = 0x8009,

	// J2534-2 Vendor extensions
	UART_ECHO_BYTE_PS = 0x800A,
	TP1_6_PS = 0x800A,			// 6H,14L CAN
	HONDA_DIAGH_PS = 0x800B,		// Switched K L Line
	J1939_PS = 0x800C,			// 6H,14L CAN
	J1708_PS = 0x800D,
	TP2_0_PS = 0x800E,			// 6H,14L CAN
	FT_CAN_PS = 0x800F,
	FT_ISO15765_PS = 0x8010,
	KW1281_PS = 0x8011,
	KW1281_TP1_6_PS = 0x8012,
	ISO5_PS = 0x8013,			// Switched K Line
	ISO8_PS = 0x8014,			// Switched K Line
	DS2_PS = 0x8015,			// Switched K Line
	DAIHATSU_PS = 0x8016,			// 7K Line
	ISOFORD_PS = 0x8017,			// Switched K Line
	KW81_PS = 0x8018,			// 7K Line
	KW82_PS = 0x8019,			// 7K Line
	MB_PS = 0x801A,				// Switched K Line
	MBHFM_PS = 0x801B,			// 7K Line
	NISSAN_PS = 0x801C,			// 12K 13L Line
	MITSUBISHI_PS = 0x801D,			// 7K Line
	PEUGEOT_PS = 0x801E,			// Switched K Line
	TOYOTA_PS = 0x801F,			// 7K Line
	KIA_PS = 0x8020,			// Switched K Line
	WABKO_PS = 0x8021,			// Switched K Line
	J4_PS = 0x8022,				// 7K Line
	ALDL160_PS = 0x8023,			// Switched K Line
	CUMMINS = 0x8024,			// 6H, 14L CAN

	CAN_CH1 = 0x9000,
	CAN_CH2 = CAN_CH1 + 1,
	CAN_CH3 = CAN_CH1 + 2,
	CAN_CH4 = CAN_CH1 + 3,
	CAN_CH5 = CAN_CH1 + 4,
	CAN_CH6 = CAN_CH1 + 5,
	CAN_CH7 = CAN_CH1 + 6,
	CAN_CH128 = CAN_CH1 + 127,

	J1850VPW_CH1 = 0x9080,
	J1850VPW_CH128 = J1850VPW_CH1 + 127,

	J1850PWM_CH1 = 0x9100,
	J1850PWM_CH128 = J1850PWM_CH1 + 127,

	ISO9141_CH1 = 0x9180,
	ISO9141_CH128 = ISO9141_CH1 + 127,

	ISO14230_CH1 = 0x9200,
	ISO14230_CH128 = ISO14230_CH1 + 127,

	ISO15765_CH1 = 0x9280,
	ISO15765_CH2 = ISO15765_CH1 + 1,
	ISO15765_CH3 = ISO15765_CH1 + 2,
	ISO15765_CH4 = ISO15765_CH1 + 3,
	ISO15765_CH5 = ISO15765_CH1 + 4,
	ISO15765_CH6 = ISO15765_CH1 + 5,
	ISO15765_CH7 = ISO15765_CH1 + 6,

	SW_CAN_CH1 = 0x9300,
	SW_CAN_CH128 = SW_CAN_CH1 + 127,

	SW_ISO15765_CH1 = 0x9380,
	SW_ISO15765_CH128 = SW_ISO15765_CH1 + 127,

	J2610_CH1 = 0x9400,
	J2610_CH128 = J2610_CH1 + 127,

	FT_CAN_CH1 = 0x9480,
	FT_CAN_CH128 = FT_CAN_CH1 + 127,

	FT_ISO15765_CH1 = 0x9500,
	FT_ISO15765_CH128 = FT_ISO15765_CH1 + 127,

	GM_UART_CH1 = 0x9580,
	GM_UART_CH128 = GM_UART_CH1 + 127,

	UART_ECHO_BYTE_CH1 = 0x9600,
	UART_ECHO_BYTE_CH128 = UART_ECHO_BYTE_CH1 + 127,

	HONDA_DIAGH_CH1 = 0x9680,
	HONDA_DIAGH_CH128 = HONDA_DIAGH_CH1 + 127,

	J1939_CH1 = 0x9700,
	J1939_CH128 = J1939_CH1 + 127,

	J1708_CH1 = 0x9780,
	J1708_CH128 = J1708_CH1 + 127,

	TP2_0_CH1 = 0x9800,
	TP2_0_CH128 = TP2_0_CH1 + 127,

	ANALOG_IN_1 = 0xC000,
	ANALOG_IN_32 = ANALOG_IN_1 + 31
};
typedef uint32_t protocol_id_t;

/*************/
/* IOCTL IDs */
/*************/

enum e_ioctl_id {
	GET_CONFIG = 0x01,
	SET_CONFIG = 0x02,
	READ_VBATT = 0x03,
	FIVE_BAUD_INIT = 0x04,
	FAST_INIT = 0x05,
	// IOCTL_ID_UNUSED_06 = 0x06,
	CLEAR_TX_BUFFER = 0x07,
	CLEAR_RX_BUFFER = 0x08,
	CLEAR_PERIODIC_MSGS = 0x09,
	CLEAR_MSG_FILTERS = 0x0A,
	CLEAR_FUNCT_MSG_LOOKUP_TABLE = 0x0B,
	ADD_TO_FUNCT_MSG_LOOKUP_TABLE = 0x0C,
	DELETE_FROM_FUNCT_MSG_LOOKUP_TABLE = 0x0D,
	READ_PROG_VOLTAGE = 0x0E,

	// J2534-2 SW_CAN
	SW_CAN_HS = 0x8000,
	SW_CAN_NS = 0x8001,
	SET_POLL_RESPONSE = 0x8002,
	BECOME_MASTER = 0x8003,

	GET_DEVICE_CONFIG = 0x800C

};
typedef uint32_t ioctl_id_t;

/*******************************/
/* Configuration Parameter IDs */
/*******************************/

enum e_ioctl_config {
	DATA_RATE = 0x01,
	// IOCTL_CONFIG_UNUSED_02 = 0x02,
	LOOPBACK = 0x03,
	NODE_ADDRESS = 0x04,
	NETWORK_LINE = 0x05,
	P1_MIN = 0x06,	// Don't use
	P1_MAX = 0x07,
	P2_MIN = 0x08,	// Don't use
	P2_MAX = 0x09,	// Don't use
	P3_MIN = 0x0A,
	P3_MAX = 0x0B,	// Don't use
	P4_MIN = 0x0C,
	P4_MAX = 0x0D,	// Don't use
	W1 = 0x0E,
	W2 = 0x0F,
	W3 = 0x10,
	W4 = 0x11,
	W5 = 0x12,
	TIDLE = 0x13,
	TINIL = 0x14,
	TWUP = 0x15,
	PARITY = 0x16,
	BIT_SAMPLE_POINT = 0x17,
	SYNC_JUMP_WIDTH = 0x18,
	W0 = 0x19,
	T1_MAX = 0x1A,
	T2_MAX = 0x1B,
	T4_MAX = 0x1C,
	T5_MAX = 0x1D,
	ISO15765_BS = 0x1E,
	ISO15765_STMIN = 0x1F,
	DATA_BITS = 0x20,
	FIVE_BAUD_MOD = 0x21,
	BS_TX = 0x22,
	STMIN_TX = 0x23,
	T3_MAX = 0x24,
	ISO15765_WFT_MAX = 0x25,

	// J2534-2
	CAN_MIXED_FORMAT = 0x8000,
	J1962_PINS = 0x8001,
	SW_CAN_HS_DATA_RATE = 0x8010,
	SW_CAN_SPEEDCHANGE_ENABLE = 0x8011,
	SW_CAN_RES_SWITCH = 0x8012,
	ACTIVE_CHANNELS = 0x8020,	// Bitmask of channels being sampled
	SAMPLE_RATE = 0x8021,		// Samples/second or Seconds/sample
	SAMPLES_PER_READING = 0x8022,	// Samples to average into a single reading
	READINGS_PER_MSG = 0x8023,	// Number of readings for each active channel per PASSTHRU_MSG structure
	AVERAGING_METHOD = 0x8024,	// The way in which the samples will be averaged.
	SAMPLE_RESOLUTION = 0x8025,	// The number of bits of resolution for each channel in the subsystem. Read Only.
	INPUT_RANGE_LOW = 0x8026,	// Lower limit in millivolts of A/D input. Read Only.
	INPUT_RANGE_HIGH = 0x8027	// Upper limit in millivolts of A/D input. Read Only.
};
typedef uint32_t ioctl_device_info_t;

/*************/
/* Error IDs */
/*************/

enum e_retval {
	// Function call successful
	STATUS_NOERROR = 0x00,

	// Device cannot support requested functionality mandated in this
	// document. Device is not fully SAE J2534 compliant
	ERR_NOT_SUPPORTED = 0x01,

	// Invalid ChannelID value
	ERR_INVALID_CHANNEL_ID = 0x02,

	// Invalid ProtocolID value, unsupported ProtocolID, or there is a
	// resource conflict (i.e. trying to connect to multiple protocols that
	// are mutually exclusive such as J1850PWM and J1850VPW, or CAN and SCI
	// A, etc.)
	ERR_INVALID_PROTOCOL_ID = 0x03,

	// NULL pointer supplied where a valid pointer is required
	ERR_NULL_PARAMETER = 0x04,

	// Invalid value for Ioctl parameter
	ERR_INVALID_IOCTL_VALUE = 0x05,

	// Invalid flag values
	ERR_INVALID_FLAGS = 0x06,

	// Undefined error, use PassThruGetLastError for text description
	ERR_FAILED = 0x07,

	// Device ID invalid
	ERR_DEVICE_NOT_CONNECTED = 0x08,

	// Timeout.
	// PassThruReadMsg: No message available to read or could not read the
	//  specified number of messages. The actual number of messages read is
	//  placed in <NumMsgs>
	// PassThruWriteMsg: Device could not write the specified number of
	//  messages. The actual number of messages sent on the vehicle network
	//  is placed in <NumMsgs>.
	ERR_TIMEOUT = 0x09,

	// Invalid message structure pointed to by pMsg (Reference Section 8
	// – Message Structure)
	ERR_INVALID_MSG = 0x0A,

	// Invalid TimeInterval value
	ERR_INVALID_TIME_INTERVAL = 0x0B,

	// Exceeded maximum number of message IDs or allocated space
	ERR_EXCEEDED_LIMIT = 0x0C,

	// Invalid MsgID value
	ERR_INVALID_MSG_ID = 0x0D,

	// Device is currently open
	ERR_DEVICE_IN_USE = 0x0E,

	// Invalid IoctlID value
	ERR_INVALID_IOCTL_ID = 0x0F,

	// Protocol message buffer empty, no messages available to read
	ERR_BUFFER_EMPTY = 0x10,

	// Protocol message buffer full. All the messages specified may not
	// have been transmitted
	ERR_BUFFER_FULL = 0x11,

	// Indicates a buffer overflow occurred and messages were lost
	ERR_BUFFER_OVERFLOW = 0x12,

	// Invalid pin number, pin number already in use, or voltage already
	// applied to a different pin
	ERR_PIN_INVALID = 0x13,

	// Channel number is currently connected
	ERR_CHANNEL_IN_USE = 0x14,

	// Protocol type in the message does not match the protocol associated
	// with the Channel ID
	ERR_MSG_PROTOCOL_ID = 0x15,

	// Invalid Filter ID value
	ERR_INVALID_FILTER_ID = 0x16,

	// No flow control filter set or matched (for protocolID ISO15765 only)
	ERR_NO_FLOW_CONTROL = 0x17,

	// A CAN ID in pPatternMsg or pFlowControlMsg matches either ID in an
	// existing FLOW_CONTROL_FILTER
	ERR_NOT_UNIQUE = 0x18,

	// The desired baud rate cannot be achieved within the tolerance
	// specified in Section 6.5
	ERR_INVALID_BAUDRATE = 0x19,

	// Unable to communicate with device
	ERR_INVALID_DEVICE_ID = 0x1A,

	ERR_NULLPARAMETER = ERR_NULL_PARAMETER	/*v2*/
};
typedef long retval_t;


/*****************************/
/* Miscellaneous definitions */
/*****************************/
#define SHORT_TO_GROUND				0xFFFFFFFE
#define VOLTAGE_OFF				0xFFFFFFFF

enum e_parity {
	NO_PARITY = 0,
	ODD_PARITY = 1,
	EVEN_PARITY = 2
};
typedef uint32_t parity_t;

// SWCAN - J2534-2
enum e_swcan {
	DISBLE_SPDCHANGE = 0,
	ENABLE_SPDCHANGE = 1,
	DISCONNECT_RESISTOR = 0,
	CONNECT_RESISTOR = 1,
	AUTO_RESISTOR = 2
};

// Mixed Mode
enum e_can_mixed_format {
	CAN_MIXED_FORMAT_OFF = 0,
	CAN_MIXED_FORMAT_ON = 1,
	CAN_MIXED_FORMAT_ALL_FRAMES = 2
};
typedef uint32_t can_mixed_format_t;


/*******************************/
/* PassThruConnect definitions */
/*******************************/

enum e_connect_flag {
	// 0 = Receive standard CAN ID (11 bit)
	// 1 = Receive extended CAN ID (29 bit)
	CAN_29BIT_ID = 0x00000100,

	// 0 = The interface will generate and append the checksum as defined
	//  in ISO 9141-2 and ISO 14230-2 for transmitted messages, and verify
	//  the checksum for received messages.
	// 1 = The interface will not generate and verify the checksum-the
	//  entire message will be treated as data by the interface
	ISO9141_NO_CHECKSUM = 0x00000200,

	// 0 = either standard or extended CAN ID types used – CAN ID type
	//  defined by bit 8
	// 1 = both standard and extended CAN ID types used – if the CAN
	//  controller allows prioritizing either standard (11 bit) or extended
	//  (29 bit) CAN ID's then bit 8 will determine the higher priority ID
	//  type
	CAN_ID_BOTH = 0x00000800,

	// 0 = use L-line and K-line for initialization address
	// 1 = use K-line only line for initialization address
	ISO9141_K_LINE_ONLY = 0x00001000
};
typedef uint32_t connect_flag_t;

/************************/
/* RxStatus definitions */
/************************/

enum e_rx_status {
	// 0 = received i.e. this message was transmitted on the bus by another
	//  node
	// 1 = transmitted i.e. this is the echo of the message transmitted by
	//  the PassThru device
	TX_MSG_TYPE = 0x00000001,

	// 0 = Not a start of message indication
	// 1 = First byte or frame received
	START_OF_MESSAGE = 0x00000002,
	ISO15765_FIRST_FRAME = 0x00000002,	/*v2 compat from v0202*/

	ISO15765_EXT_ADDR = 0x00000080,		/*DT Accidentally refered to in spec*/

	// 0 = No break received
	// 1 = Break received
	RX_BREAK = 0x00000004,

	// 0 = No TxDone
	// 1 = TxDone
	TX_INDICATION = 0x00000008,		// Preferred name
	TX_DONE = 0x00000008,

	// 0 = No Error
	// 1 = Padding Error
	ISO15765_PADDING_ERROR = 0x00000010,

	// 0 = no extended address,
	// 1 = extended address is first byte after the CAN ID
	ISO15765_ADDR_TYPE = 0x00000080,

	// CAN_29BIT_ID = 0x00000100,		// defined above

	SW_CAN_NS_RX = 0x00040000,		/*-2*/
	SW_CAN_HS_RX = 0x00020000,		/*-2*/
	SW_CAN_HV_RX = 0x00010000		/*-2*/
};
typedef uint32_t rx_status_t;

/***********************/
/* TxFlags definitions */
/***********************/

enum e_tx_flag {
	// 0 = no padding
	// 1 = pad all flow controlled messages to a full CAN frame using zeroes
	ISO15765_FRAME_PAD = 0x00000040,

	// ISO15765_ADDR_TYPE = 0x00000080,	// defined above
	// CAN_29BIT_ID = 0x00000100,		// defined above

	// 0 = Interface message timing as specified in ISO 14230
	// 1 = After a response is received for a physical request, the wait
	//  time shall be reduced to P3_MIN
	// Does not affect timing on responses to functional requests
	WAIT_P3_MIN_ONLY = 0x00000200,

	SW_CAN_HV_TX = 0x00000400,		/*-2*/

	// 0 = Transmit using SCI Full duplex mode
	// 1 = Transmit using SCI Half duplex mode
	SCI_MODE = 0x00400000,

	// 0 = no voltage after message transmit
	// 1 = apply 20V after message transmit
	SCI_TX_VOLTAGE = 0x00800000,

	DT_PERIODIC_UPDATE = 0x10000000		/*DT*/
};
typedef uint32_t tx_flag_t;

/**********************/
/* Filter definitions */
/**********************/

enum e_filter {
	// Allows matching messages into the receive queue. This filter type is
	// only valid on non-ISO 15765 channels
	PASS_FILTER = 0x00000001,

	// Keeps matching messages out of the receive queue. This filter type
	// is only valid on non-ISO 15765 channels
	BLOCK_FILTER = 0x00000002,

	// Allows matching messages into the receive queue and defines an
	// outgoing flow control message to support the ISO 15765-2 flow
	// control mechanism. This filter type is only valid on ISO 15765
	// channels.
	FLOW_CONTROL_FILTER = 0x00000003
};
typedef uint32_t filter_t;

#pragma pack(push,1)
typedef struct _PASSTHRU_MSG
{
	// Protocol type
	unsigned long ProtocolID;

	// Receive message status – See RxStatus in "Message Flags and Status Definition" section
	unsigned long RxStatus;

	// Transmit message flags
	unsigned long TxFlags;

	// Received message timestamp (microseconds): For the START_OF_FRAME
	// indication, the timestamp is for the start of the first bit of the message. For all other
	// indications and transmit and receive messages, the timestamp is the end of the last
	// bit of the message. For all other error indications, the timestamp is the time the error
	// is detected.
	unsigned long Timestamp;

	// Data size in bytes, including header bytes, ID bytes, message data bytes, and extra
	// data, if any.
	unsigned long DataSize;

	// Start position of extra data in received message (for example, IFR). The extra data
	// bytes follow the body bytes in the Data array. The index is zero-based. When no
	// extra data bytes are present in the message, ExtraDataIndex shall be set equal to
	// DataSize. Therefore, if DataSize equals ExtraDataIndex, there are no extra data
	// bytes. If ExtraDataIndex=0, then all bytes in the data array are extra bytes.
	unsigned long ExtraDataIndex;

	// Start position of extra data in received message (for example, IFR). The extra data
	// bytes follow the body bytes in the Data array. The index is zero-based. When no
	// extra data bytes are present in the message, ExtraDataIndex shall be set equal to
	// DataSize. Therefore, if DataSize equals ExtraDataIndex, there are no extra data
	// bytes. If ExtraDataIndex=0, then all bytes in the data array are extra bytes.
	unsigned char Data[4128];
} PASSTHRU_MSG;

typedef struct _SCONFIG
{
	unsigned long Parameter;		// Name of parameter
	unsigned long Value;			// Value of the parameter
} SCONFIG;

typedef struct _SCONFIG_LIST
{
	unsigned long NumOfParams;		// Number of SCONFIG elements
	SCONFIG *ConfigPtr;			// Array of SCONFIG
} SCONFIG_LIST;

typedef struct _SBYTE_ARRAY
{
	unsigned long NumOfBytes;		// Number of bytes in the array
	unsigned char *BytePtr;			// Array of bytes
} SBYTE_ARRAY;

struct SPARAM
{
	uint32_t Parameter;		// parameter, either ioctl_device_info_t or ioctl_protocol_info_t
	uint32_t Value;			// value of the parameter
	uint32_t Supported;		// support for parameter
};

struct SPARAM_LIST
{
	uint32_t NumOfParams;		// number of SPARAM elements
	SPARAM* ParamPtr;				// array of SPARAM structures
};

#define J1962_CONNECTOR		0x00000001
struct RESOURCE_STRUCT
{
	uint32_t Connector;			// conector identifier
	uint32_t NumOfResources;	// number of resources pointer to by ResourceListPtr
	uint32_t* ResourceListPtr;	// pointer to list of resources
};

#pragma pack(pop)

#ifdef _WIN32
#define J2534_API __stdcall
#else
#define J2534_API
#endif

typedef long(J2534_API *PTOPEN)(void *pName, unsigned long *pDeviceID);
typedef long(J2534_API *PTCLOSE)(unsigned long DeviceID);
typedef long(J2534_API *PTCONNECT)(unsigned long DeviceID, unsigned long ProtocolID, unsigned long Flags, unsigned long BaudRate, unsigned long *pChannelID);
typedef long(J2534_API *PTDISCONNECT)(unsigned long ChannelID);
typedef long(J2534_API *PTREADMSGS)(unsigned long ChannelID, PASSTHRU_MSG *pMsg, unsigned long *pNumMsgs, unsigned long Timeout);
typedef long(J2534_API *PTWRITEMSGS)(unsigned long ChannelID, PASSTHRU_MSG *pMsg, unsigned long *pNumMsgs, unsigned long Timeout);
typedef long(J2534_API *PTSTARTPERIODICMSG)(unsigned long ChannelID, PASSTHRU_MSG *pMsg, unsigned long *pMsgID, unsigned long TimeInterval);
typedef long(J2534_API *PTSTOPPERIODICMSG)(unsigned long ChannelID, unsigned long MsgID);
typedef long(J2534_API *PTSTARTMSGFILTER)(unsigned long ChannelID, unsigned long FilterType, PASSTHRU_MSG *pMaskMsg, PASSTHRU_MSG *pPatternMsg,
    PASSTHRU_MSG *pFlowControlMsg, unsigned long *pFilterID);
typedef long(J2534_API *PTSTOPMSGFILTER)(unsigned long ChannelID, unsigned long FilterID);
typedef long(J2534_API *PTSETPROGRAMMINGVOLTAGE)(unsigned long DeviceID, unsigned long PinNumber, unsigned long Voltage);
typedef long(J2534_API *PTREADVERSION)(unsigned long DeviceID, char *pFirmwareVersion, char *pDllVersion, char *pApiVersion);
typedef long(J2534_API *PTGETLASTERROR)(char *pErrorDescription);
typedef long(J2534_API *PTIOCTL)(unsigned long ChannelID, unsigned long IoctlID, void *pInput, void *pOutput);
