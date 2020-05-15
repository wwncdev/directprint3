// XIJET_API.h : main header file for the XIJET API DLL
//

#pragma once

#include <windows.h>

//
// *** NOTE ***
// XIJET_API_VERSION must be in floating format... x.y only... x.y.z is not allowed..
//
// for "dot releases" e.g. 7.4.2 set number to 7.42
//
#define XIJET_API_VERSION		10.0

///////////////////////////////////////////////
//
//	PRINTER TYPES
//
#define XIJET_TYPE_HP			0
#define XIJET_TYPE_LEXMARK		1
#define XIJET_TYPE_KM512		2
#define XIJET_TYPE_XAAR500		3
#define XIJET_TYPE_NEUFLOR		4
#define XIJET_TYPE_KM1024I		5
#define XIJET_TYPE_KYO_KJ4		6
#define XIJET_TYPE_JUNO			7
#define XIJET_TYPE_RICOH		8

//  in the new API specification, "CARTRIGE" was replaced with the more generic "INK"
//  these defines allow for backward compatibility...

#define XIJET_CARTRIDGE_PROFILE_STRUCT	XIJET_INK_PROFILE_STRUCT
#define	XIJET_CARTRIDGE_PROFILE			XIJET_INK_PROFILE

///////////////////////////////////////////////
//
//	PrintFlags (in call to XIJET_PrintDocumentPageExt, CanvasPrintExt)
//
#define PRT_NO_BUFFER_CLEAR		0x01	// disables clear of buffered images on first print (use for read-and-print mode)
#define PRT_PRINT_WINDOW_SYNC	0x02	// detects sync errors in read-and-print mode (by auto-detecting a print window)

///////////////////////////////////////////////
//
//	Bulk Tank Status (in call to XIJET_GetInkRemainingExt)
//
#define BULK_INK_STATUS_NONE	0x00	// no tank
#define BULK_INK_STATUS_GREEN	0x01
#define BULK_INK_STATUS_YELLOW	0x02
#define BULK_INK_STATUS_RED		0x03
#define BULK_INK_STATUS_UNKNOWN	0x04

///////////////////////////////////////////////

#define MAX_PRINTERS			4
#define MAX_HEADS				16
#define MAX_PENS				4
#define MAX_USB_DEVICES			32

#define MAX_QUEUE_DEPTH			15		// embedded print queue depth 

#define NUM_RESOLUTIONS_NP45	15		// HP resolutions supported
#define NUM_RESOLUTIONS_LX		15		// Lexmark resolutions supported
#define NUM_RESOLUTIONS_KM512	2		// KM512
#define NUM_RESOLUTIONS_KYO_KJ4 6		// Kyocera

#define MAX_CUSTOM_RESOLUTIONS	50		// customer special...


#define NOMINAL_PEN_HEIGHT		150		
#define NOMINAL_PEN_OVERLAP		6
#define NOMINAL_RESOLUTION_DPI	300

///////////////////////////////////////////////////////////////////
//
//	XIJET_CONFIGURATION structure
//
//	Used in calls to XIJET_OpenPrinter
//

typedef struct {
	int PrinterType;					// XIJET_TYPE_HP, XIJET_TYPE_LX
	USHORT VerticalResolutionDPI;		// printhead resolution
	USHORT HorizontalResolutionDPI;		// transport resolution
	USHORT NumberOfPrintheads;			// up to 4 logical printheads
	USHORT Head1Height;					// height of logical printhead in pixels
	USHORT Head2Height;
	USHORT Head3Height;
	USHORT Head4Height;
} XIJET_CONFIGURATION;

///////////////////////////////////////////////////////////////////
//
//	XIJET_CARTRIDGE_PROFILE_STRUCT structure
//
//	Used in calls to XIJET_GetPrinterParameter/
//	XIJET_SetPrinterParameter - XIJET_CARTRIDGE_PROFILE
//

typedef struct {
	USHORT	voltage;				// HP only cartridge voltage, 0.1 volt units, Range 70 to 112 (7.0 to 11.2)
	USHORT  preFirePulseWidth;		// Lexmark only, nSec units
	USHORT	gapWidth;				// Lexmark only, nSec units
	USHORT	pulseWidth;				// pulse width, HP range = 10 to 25 (1.0 to 2.5 uSecs) / LX in nSec units
	USHORT	temperature;			// degrees C, Range 30 to 70
} XIJET_INK_PROFILE_STRUCT;


///////////////////////////////////////////////////////////////////
//
//	RESOLUTION_STRUCT structure
//
//	Used in calls to XIJET_SetPrinterParameter - XIJET_RESOLUTION_DIRECT
//
typedef struct {
	ULONG resVerticalDPI;			//
	ULONG resHorizontalDPI;			//
	USHORT fastModeFactor;
	USHORT bitsPerPixel;
} RESOLUTION_STRUCT;

///////////////////////////////////////////////////////////////////
//
//  error return codes for XIJET_GetStatus 
//
#define XIJET_API_SUCCESS						0
#define XIJET_API_INVALID_PARAMETER				1	// bad parameter passed to API function
#define XIJET_API_INVALID_HANDLE				2	// bad handle passed to API function
#define XIJET_API_LOST_COMM_ERROR				3	// ioctl (read or write) failed
#define XIJET_API_FILE_OPEN_ERROR				4	// error returns from download functions
#define XIJET_API_FILE_RANGE_ERROR				5	//			"
#define XIJET_API_FILE_INVALID_RECORD			6	//			"
#define XIJET_API_LCAFILE_OPEN_ERROR			7	// error returns from download LCA
#define XIJET_API_LCAFILE_READ_ERROR			8	//			"
#define XIJET_API_PRINT_SPEED_TOO_HIGH			9	// PRINT errors - Encoder saturation
#define XIJET_API_PRINT_TIMEOUT_PAGE_JAM		10	// PRINT timeout / Page jam
#define XIJET_API_VERSION_ERROR					11	// embedded needs version upgrade
#define XIJET_API_PRINT_DOC_SYNC_ERR_UNDERRUN	12	// document sync - data underrun
#define XIJET_API_PRINT_DOC_SYNC_ERR_OVERLAP	13	// document sync - data overlap
#define XIJET_API_NON_SUPPORTED_CARTRIDGE		14	// invalid cartridge
#define XIJET_API_CARTRIDGE_UNDER_VOLTAGE		15	// detected under voltage condition
#define XIJET_API_CARTRIDGE_OVER_VOLTAGE		16	// detected over voltage condition
#define XIJET_API_CARTRIDGE_OVER_TEMPERATURE	17	// detected over temperature condition
#define XIJET_API_NO_CARTRIDGE_PRESENT			18	// no cartridge inserted
#define XIJET_API_CARTRIDGE_BLOWN_FET			19	// detected blown power FET
#define XIJET_API_CARTRIDGE_OVER_CURRENT		20	// detected excess current condition
#define XIJET_API_HEAD_SYNC_ERROR				21	// detected potential print out-of-sync condition


///////////////////////////////////////////////////////////////////
//
//	Parameter constants used in calls to XIJET_GetPrinterParameter,
//  XIJET_SetPrinterParameter
//

#define	XIJET_RESOLUTION		0		// USHORT - resolution index as defined below
#define XIJET_QUEUE_DEPTH		1		// USHORT - controls how many print buffers are used; i.e. 2=double buffering
#define XIJET_SPP_NSP_2			2		// NON-SUPPORTED
#define	XIJET_JET_BLANKING		3		// USHORT - 0=off (print with all jets); 1=on (blank "trailing" jets)
#define	XIJET_AUX_OUTPUT		4		// USHORT - bitmapped control word. Bit 0 = Aux Output. Head specific.
#define	XIJET_SPP_NSP_5			5		// RETIRED - was XIJET_TRIGGER_OFFSET which has new constant below (old constant will still work)
#define	XIJET_HEAD_HEIGHT		6		// USHORT - READ-ONLY - head height in mils.  Head specific - head index must be specified
#define	XIJET_INK_PROFILE		7		// INK_PROFILE - (same as CARTRIGE_PROFILE) used for updated spec.
#define	XIJET_PEN_WARMING		8		// USHORT - 0=off; 1=pre-warm; 2=continuous
#define XIJET_TRIGGER_MASK		9		// ULONG -	Mask print trigger for distance (in mils)
#define XIJET_SPP_NSP_10		10		// NON-SUPPORTED
#define XIJET_SKIP_TRIG_DETECT	11		// USHORT - 0=off; 1=on - Web systems option
#define XIJET_PRINT_WINDOW_MAX	12		// USHORT - distance in inches (encoder) or time in milliseconds (fixed speed) - Read-and-Print systems only
#define XIJET_PRINT_WINDOW_MIN	13		// USHORT - distance in inches (encoder) or time in milliseconds (fixed speed) - Read-and-Print systems only
#define XIJET_PRINT_DELAY		14		// USHORT - global horizontal offset, aka print delay - distance in mils

										// ONLY NON-VOL parameters beyond this point...
#define XIJET_HEAD_ORIENTATION	100		// USHORT - NON-VOL - (see constants below)
#define XIJET_ENCODER_RES		101		// USHORT - NON-VOL - counts/inch
#define XIJET_TRIGGER_TYPE		102		// USHORT - NON-VOL - (see constants below)
#define	XIJET_TRIGGER_OFFSET	103		// USHORT - NON-VOL config setting - photocell offset in mils.  Head specific - head index must be specified
#define XIJET_VERT_JETS_P1		104		// USHORT - NON-VOL - active jets pen 1 - vertical stitching
#define XIJET_VERT_JETS_P2		105		// USHORT - NON-VOL - active jets pen 2
#define XIJET_VERT_JETS_P3		106		// USHORT - NON-VOL - active jets pen 3
#define XIJET_VERT_JETS_P4		107		// USHORT - NON-VOL - active jets pen 4
#define XIJET_HORZ_OFF_P1		108		// USHORT - NON-VOL - horizontal offset pen 1 - horizontal stitching (horizontal = running direction)
#define XIJET_HORZ_OFF_P2		109		// USHORT - NON-VOL - horizontal offset pen 2
#define XIJET_HORZ_OFF_P3		110		// USHORT - NON-VOL - horizontal offset pen 3
#define XIJET_HORZ_OFF_P4		111		// USHORT - NON-VOL - horizontal offset pen 4

// Head Index constants (used for HeadIndex parameter of GetPrinterParameter/SetPrinterParameter)
#define XIJET_HEAD_1_INDEX		0
#define XIJET_HEAD_2_INDEX		1
#define XIJET_HEAD_3_INDEX		2
#define XIJET_HEAD_4_INDEX		3
#define XIJET_ALL_HEADS			0xffff	// get/set parameter to all heads

// Head orientation constants (used with GetPrinterParameter/SetPrinterParameter/XIJET_HEAD_ORIENTATION)
#define XIJET_HEAD_ORIENT_A		0
#define XIJET_HEAD_ORIENT_B		1
#define XIJET_HEAD_ORIENT_C		2
#define XIJET_HEAD_ORIENT_D		3

// Trigger type constants (used with GetPrinterParameter/SetPrinterParameter/XIJET_TRIGGER_TYPE)
#define TRIGGER_TYPE_AUTO_GEN		0	// no physical input, print triggers automatically when print request received
#define TRIGGER_TYPE_RISING			1	// rising edge of input
#define TRIGGER_TYPE_FALLING		2	// falling edge of input
#define TRIGGER_TYPE_SOFTWARE		3	// no physical input, print trigger comes via API call XIJET_SoftPrintTrigger

//
//	Warming modes
//
#define XIJET_WARMING_OFF		 0		// Warming off
#define XIJET_WARMING_PRE_WARM	 1		// Pre-Warm only
#define XIJET_WARMING_CONTINUOUS 2		// Continuous


//
//	Parameters constants for use in SetPrinterParameter - XIJET_RESOLUTION - NP45 only
//
//  These have been left in for backward combatibility reason only.
//  They are the resolutions supported by the HP (NP45) printers
//
//	The correct way to determine resolutions is by calling XIJET_GetPrinterResolution
//
//										 Vert x Horiz 

#define	NP45_RES_600_600		0		// 600x600 25"/sec max
#define	NP45_RES_600_300		1		// 600x300 50"/sec max
#define	NP45_RES_600_250		2		// 600x250 60"/sec max
#define	NP45_RES_600_200		3		// 600x200 75"/sec max
#define	NP45_RES_600_150		4		// 600x150 100"/sec max
#define	NP45_RES_600_100		5		// 600x100 150"/sec max
#define	NP45_RES_300_300f		6		// 300x300 100"/sec max
#define	NP45_RES_300_300d		7		// 300x300 dark 50"/sec max
#define	NP45_RES_300_150f		8		// 300x150 200"/sec max
#define	NP45_RES_300_200f		9		// 300x200 150"/sec max
#define	NP45_RES_300_250f		10		// 300x250 120"/sec max
#define	NP45_RES_300_100f		11		// 300x100 300"/sec max
#define	NP45_RES_300_300		12		// 300x300 50"/sec max
#define	NP45_RES_300_600		13		// 300x600 25"/sec max

//
//	Parameters constants for use in XIJET_QueueOutputToggle - syncOption parameter
//
#define SYNC_DELAY_NONE			0
#define SYNC_DELAY_TIME			1
#define SYNC_DELAY_DISTANCE		2

//
// Parameter used in call to XIJET_ActivateInkPurge (purgeMode)
//
#define PMT_MODE_OFF			0		// OFF
#define PMT_MODE_SPIT			1		// spit m drops (purgeParameter1) every n secs (purgeParameter2)
#define PMT_MODE_RANDOM_SPIT	2		// random spitting mode

///////////////////////////////////////////////////////////////////
//
//	Function defintions
//

//
// Housekeeping Functions
// 

BOOL WINAPI XIJET_ProbePrinter(USHORT index, CHAR *printerName);

BOOL WINAPI XIJET_ProbePrinterW(USHORT index, WCHAR *printerName);

HANDLE WINAPI XIJET_OpenPrinter( const CHAR *printerName,
								 XIJET_CONFIGURATION *pPrinterConfig);

HANDLE WINAPI XIJET_OpenPrinterW( const WCHAR *printerName,
								  XIJET_CONFIGURATION *pPrinterConfig);

void WINAPI XIJET_ClosePrinter(HANDLE printerHandle);

BOOL WINAPI XIJET_GetPrinterResolution( const CHAR *PrinterName,
										USHORT ResolutionIndex,
										PCHAR ResolutionDescription,
										PUSHORT HorizontalDPI,
										PUSHORT VerticalDPI,
										PUSHORT MaxSpeedIPS);

int WINAPI XIJET_GetStatus( HANDLE printerHandle,
							CHAR *pStatusMessage);

//
// Adjusting Settings

int WINAPI XIJET_GetPrinterParameter( HANDLE printerHandle,
									  USHORT parameterIndex,
									  PVOID pParameter,
									  USHORT HeadIndex);

int WINAPI XIJET_SetPrinterParameter( HANDLE printerHandle,
									  USHORT parameterIndex,
									  PVOID pParameter,
									  USHORT HeadIndex);
//
// Printing functions
//
//
// Canvas Mode Print Functions
//
HANDLE WINAPI XIJET_LoadFontXFT( HANDLE printerHandle, const char *filename);

HANDLE WINAPI XIJET_LoadFontXFTwRotation( HANDLE printerHandle,
										 const char *filename,
										 int rotation,
										 int codePages[] = NULL);

int WINAPI XIJET_CanvasBegin( HANDLE printerHandle,
							  ULONG VerticalHeight,
							  ULONG HorizontalWidth);

int WINAPI XIJET_CanvasWrite( HANDLE printerHandle,
							  ULONG VerticalOffset,
							  ULONG HorizontalOffset,
							  ULONG VerticalHeight,
							  ULONG HorizontalWidth,
							  UCHAR *pBitmappedBuffer);

int WINAPI XIJET_CanvasWriteStr( HANDLE printerHandle,
								 HANDLE fontHandle,
								 const char *asciiStr,
								 ULONG VerticalOffset,
								 ULONG HorizontalOffset,
								 ULONG LineToLineOffset = -1);

int WINAPI XIJET_CanvasWriteStrRotation( HANDLE printerHandle,
								 HANDLE fontHandle,
								 const WCHAR *asciiStr,
								 ULONG Vertical_Top,
								 ULONG Horizontal_Left,
								 ULONG Vertical_Bot,
								 ULONG Horizontal_Right,
								 int rotation,				// primary rotation 0, 90, 180, 270
								 int align,
								 int reverseVideo,
								 int wordWrap,
								 int mirror,
								 int bottomJustify,
								 int isVariableData,
								 ULONG *pTextHeight,	// ok to pass NULL if not interested in resultant height or width
								 ULONG *pTextWidth,
								 ULONG LineToLineOffset = -1,
								 ULONG CharToCharOffset = -1,
								 int skewRotation = 0,
								 int inkDensity = 0);	// 0 = full; else 8 (dark) down to 1 (light)

int WINAPI XIJET_CanvasSave(HANDLE printerHandle);

int WINAPI XIJET_CanvasPrint( HANDLE printerHandle,
							  ULONG Head1Offset,
							  ULONG Head2Offset,
							  ULONG Head3Offset,
							  ULONG Head4Offset,
							  ULONG Timeout);

int WINAPI XIJET_CanvasPrintExt( HANDLE printerHandle,
								 USHORT PrintID,
								 ULONG *HeadVertOffsets,
								 ULONG Timeout,
								 USHORT PrintFlags,
								 int *pPercentComplete,
								 float *pTransportSpeedIPS,
								 BOOL *pPrintCompletedFlag,
								 USHORT *pPrintIDcompleted,
								 ULONG *pCompletedPositionMils);

//
// Page Mode Print Functions
//
int WINAPI XIJET_PrintDocumentPage( HANDLE printerHandle,
								    ULONG VerticalHeight,
									ULONG HorizontalWidth,
									UCHAR *pBitmappedBuffer,
									ULONG Head1Offset,
									ULONG Head2Offset,
									ULONG Head3Offset,
									ULONG Head4Offset,
									ULONG Timeout);

int WINAPI XIJET_PrintDocumentPageExt( HANDLE printerHandle,
									   USHORT PrintID,
									   ULONG VerticalHeight,
									   ULONG HorizontalWidth,
									   UCHAR *pBitmappedBuffer,
									   ULONG *HeadVertOffsets,
									   ULONG Timeout,
									   USHORT PrintFlags,
									   float *pTransportSpeedIPS);

int WINAPI XIJET_QueueOutputToggle( HANDLE printerHandle,
									USHORT outputIndex,
									USHORT outputBitmask,
									USHORT toggleTimeMS,
									UCHAR syncOption = SYNC_DELAY_NONE,
									USHORT syncDelay = 0);

int WINAPI XIJET_SoftPrintTrigger( HANDLE printerHandle);


//
// status/misc functions...
//

double WINAPI XIJET_GetVersionAPI();

int WINAPI XIJET_WaitForPrintComplete( HANDLE printerHandle,
									   ULONG timeoutMSecs);

int WINAPI XIJET_WaitForPrintCompleteExt( HANDLE printerHandle,
										  ULONG timeoutMSecs,
										  BOOL *pPrintCompletedFlag,
										  USHORT *pPrintIDcompleted,
										  ULONG *pCompletedPositionMils);

void WINAPI XIJET_Reset(HANDLE printerHandle);

void WINAPI XIJET_ResetPrintData(HANDLE printerHandle);

void WINAPI XIJET_ResetPrintQueue(HANDLE printerHandle);



//
//	XIJET_ResetInkCartridge
//
//	This call is now only applicable to HP based systems.
//	In order to be of any use, the cartridge based ink tracking, the
//	cartridge must remain in the same stall for its life.
//	When a new cartridge is installed, this function will reset
//	the level being maintained by the firmware.

int WINAPI XIJET_ResetInkCartridge( HANDLE printerHandle,
									USHORT headIndex,
									USHORT penIndex);
//
//	XIJET_GetInkRemaining
//
//	For Funai/Lexmark based cartridge systems, ink levels is being maintained
//  in the file \XiJet\Common\InkTrackLX.bin.  Based on a 16-bit cartridge ID
//	this allows for cartridges to be swapped in and out amonst different stalls
//	For HP based systems the cartridge must remain in the same stall 
//	for its 'life'

int WINAPI XIJET_GetInkRemaining( HANDLE printerHandle,
								  USHORT headIndex,
								  SHORT  *remainingPen1,
								  SHORT  *remainingPen2,
								  SHORT  *remainingPen3,
								  SHORT  *remainingPen4);

int WINAPI XIJET_GetInkRemainingExt( HANDLE printerHandle,
									 USHORT headIndex,
									 USHORT	*bulkTankStatus,
									 SHORT  *remainingPen1,
									 SHORT  *remainingPen2,
									 SHORT  *remainingPen3,
									 SHORT  *remainingPen4);


int WINAPI XIJET_SetDateAndTime( HANDLE printerHandle,
								 USHORT year, USHORT month, USHORT day,
								 USHORT hour, USHORT min, USHORT sec, USHORT dayOfWeek,
								 LONG rolloverAdjust);

int WINAPI XIJET_GetTransportSpeed( HANDLE printerHandle,
									float *transportSpeedIPS);

int WINAPI XIJET_GetTransportSpeedExt( const CHAR *printerName,
									   float *transportSpeedIPS);

double WINAPI XIJET_GetVersionEmbedded(HANDLE printerHandle);

ULONG WINAPI XIJET_TestUSB( HANDLE printerHandle);

//
//	XIJET_ActivatePens, XIJET_DeactivatePens
//
//	Power-up and Power-down of the cartridges are normally handled
//	automatically by the firmware.  Under special circumstances
//  cartridges may be powered 'manually' via these calls.

int WINAPI XIJET_ActivatePens( HANDLE printerHandle);

int WINAPI XIJET_DeactivatePens( HANDLE printerHandle);

//
//	XIJET_ActivateInkPurge
//
//	the only call still supported is a 1-time ejection of drops
//
//	To expel 200 drops per nozzle, the call is:
//
//  XIJET_ActivateInkPurge( printerHandle, PMT_MODE_SPIT, 200, 0, 0, 0);
//
int WINAPI XIJET_ActivateInkPurge( HANDLE printerHandle,
								   UCHAR  purgeMode,
								   ULONG  purgeParameter1 = 0,
								   ULONG  purgeParameter2 = 0,
								   ULONG  purgeParameter3 = 0,
								   ULONG  purgeParameter4 =0
								   );

//
//	XIJET_OpenLogFile, XIJET_CloseLogFile
//
//	will direct diagnostic output to a log file
//
FILE* WINAPI XIJET_OpenLogFile(const char *filename);

void WINAPI XIJET_CloseLogFile();

void WINAPI XIJET_logMessage(const char *funcName, const char *msg);

//
// 'board' level calls to comunicate with a specific controller.
//  must use XIJET_GetBoardHandle for the
// 
HANDLE WINAPI XIJET_GetBoardHandle( HANDLE printerHandle,
									USHORT headIndex);

int WINAPI XIJET_GetStatusExt2( HANDLE boardHandle,
								ULONG *printerStatus,
								LONG *printCount,
								ULONG *printerUptime,
								BOOL *printComplete,
								BOOL *cartGraceWarningFlag,
								USHORT	*bulkTankStatus,
								SHORT  *remainingPen1,	// OK to send "NULL" pointers for remainder of parameters
								SHORT  *remainingPen2,
								SHORT  *remainingPen3,
								SHORT  *remainingPen4,
								USHORT *cartID1,
								USHORT *cartID2,
								USHORT *cartID3,
								USHORT *cartID4,
								LONG *diffDropCount1,
								LONG *diffDropCount2,
								LONG *diffDropCount3,
								LONG *diffDropCount4
								);


