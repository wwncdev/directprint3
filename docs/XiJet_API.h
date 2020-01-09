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
#define XIJET_API_VERSION		9.12

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
#define NUM_RESOLUTIONS_KM512	4		// KM512

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
//	Parameters constants used in calls to XIJET_GetPrinterParameter,
//  XIJET_SetPrinterParameter
//

#define	XIJET_RESOLUTION		0		// USHORT - resolution index as defined below
#define XIJET_QUEUE_DEPTH		1		// USHORT - controls how many print buffers are used; i.e. 2=double buffering
#define XIJET_SUB_SAMPLE		2		// USHORT - 1=print every horizontal line; 2=print every other horizontal line; etc.
#define	XIJET_JET_BLANKING		3		// USHORT - 0=off (print with all jets); 1=on (blank "trailing" jets)
#define	XIJET_AUX_OUTPUT		4		// USHORT - bitmapped control word. Bit 0 = Aux Output. Head specific.
#define	XIJET_TRIGGER_OFFSET	5		// USHORT - photocell offset in mils.  Head specific.
#define	XIJET_HEAD_HEIGHT		6		// USHORT - head height in mils.  Head specific.
#define	XIJET_INK_PROFILE		7		// INK_PROFILE - (same as CARTRIGE_PROFILE) used for updated spec.
#define	XIJET_PEN_WARMING		8		// USHORT - 0=off; 1=pre-warm; 2=continuous
#define XIJET_TRIGGER_MASK		9		// ULONG -	Mask print trigger for distance (in mils)
#define XIJET_RESOLUTION_DIRECT 10		// RESOLUTION_STRUCT
#define XIJET_SKIP_TRIG_DETECT	11		// USHORT - 0=off; 1=on - Web systems option
#define XIJET_PRINT_WINDOW_MAX	12		// USHORT - distance in inches (encoder) or time in milliseconds (fixed speed)
#define XIJET_PRINT_WINDOW_MIN	13		// USHORT - distance in inches (encoder) or time in milliseconds (fixed speed)


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

double WINAPI XIJET_GetVersionAPI();

BOOL WINAPI XIJET_ProbePrinter(USHORT index, CHAR *printerName);

BOOL WINAPI XIJET_ProbePrinterW(USHORT index, WCHAR *printerName);

HANDLE WINAPI XIJET_OpenPrinter( const CHAR *printerName,
								 XIJET_CONFIGURATION *pPrinterConfig);

HANDLE WINAPI XIJET_OpenPrinterW( const WCHAR *printerName,
								  XIJET_CONFIGURATION *pPrinterConfig);

void WINAPI XIJET_Reset(HANDLE printerHandle);

void WINAPI XIJET_ResetPrintData(HANDLE printerHandle);

void WINAPI XIJET_ResetPrintQueue(HANDLE printerHandle);

BOOL WINAPI XIJET_GetPrinterResolution( const CHAR *PrinterName,
										USHORT ResolutionIndex,
										PCHAR ResolutionDescription,
										PUSHORT HorizontalDPI,
										PUSHORT VerticalDPI,
										PUSHORT MaxSpeedIPS);

int WINAPI XIJET_GetPrinterParameter( HANDLE printerHandle,
									  USHORT parameterIndex,
									  PVOID pParameter,
									  USHORT HeadNumber = 0);

int WINAPI XIJET_SetPrinterParameter( HANDLE printerHandle,
									  USHORT parameterIndex,
									  PVOID pParameter,
									  USHORT HeadNumber = 0);

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
// Canvas mode functions...
//
HANDLE WINAPI XIJET_LoadFontXFT( HANDLE printerHandle, const char *filename);

HANDLE WINAPI XIJET_LoadFontXFTwRotation( HANDLE printerHandle,
										 const char *filename,
										 int rotation,
										 int codePages[]);

int WINAPI XIJET_CanvasBegin( HANDLE printerHandle,
							  ULONG VerticalHeight,
							  ULONG HorizontalWidth);

int WINAPI XIJET_CanvasWrite( HANDLE printerHandle,
							  ULONG VerticalOffset,
							  ULONG HorizontalOffset,
							  ULONG VerticalHeight,
							  ULONG HorizontalWidth,
							  UCHAR *pBitmappedBuffer);

int WINAPI XIJET_CanvasWriteRGB( HANDLE printerHandle,
								 ULONG VerticalOffset,
								 ULONG HorizontalOffset,
								 ULONG VerticalHeight,
								 ULONG HorizontalWidth,
								 LPVOID pPixelBuffer);

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
								 ULONG *pTextHeight,
								 ULONG *pTextWidth,
								 ULONG LineToLineOffset = -1,
								 ULONG CharToCharOffset = -1,
								 int skewRotation = 0);

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
// status/misc functions...
//
int WINAPI XIJET_WaitForPrintComplete( HANDLE printerHandle,
									   ULONG timeoutMSecs);

int WINAPI XIJET_WaitForPrintCompleteExt( HANDLE printerHandle,
										  ULONG timeoutMSecs,
										  BOOL *pPrintCompletedFlag,
										  USHORT *pPrintIDcompleted,
										  ULONG *pCompletedPositionMils);

int WINAPI XIJET_GetStatus( HANDLE printerHandle,
							CHAR *pStatusMessage);

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

int WINAPI XIJET_ResetInkCartridge( HANDLE printerHandle,
									USHORT headIndex,
									USHORT penIndex);

void WINAPI XIJET_ClosePrinter(HANDLE printerHandle);

int WINAPI XIJET_ActivatePens( HANDLE printerHandle);

int WINAPI XIJET_DeactivatePens( HANDLE printerHandle);

int WINAPI XIJET_ActivateInkPurge( HANDLE printerHandle,
								   UCHAR  purgeMode,
								   ULONG  purgeParameter1 = 0,
								   ULONG  purgeParameter2 = 0,
								   ULONG  purgeParameter3 = 0,
								   ULONG  purgeParameter4 =0
								   );

int WINAPI XIJET_SelectPens( HANDLE printerHandle, ULONG bmPensSelect);

FILE* WINAPI XIJET_OpenLogFile(const char *filename);

void WINAPI XIJET_CloseLogFile();

void WINAPI XIJET_logMessage(const char *funcName, const char *msg);

double WINAPI XIJET_GetVersionEmbedded(HANDLE printerHandle);

ULONG WINAPI XIJET_TestUSB( HANDLE printerHandle);

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


