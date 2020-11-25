using System;
using System.Text;
using System.Runtime.InteropServices;

namespace XIJET_API
{
    public unsafe class XIJET
    {
        public static readonly int INVALID_HANDLE_VALUE = -1;

        public static readonly int BULK_INK_STATUS_NONE = 0x00;
        public static readonly int BULK_INK_STATUS_GREEN = 0x01;
        public static readonly int BULK_INK_STATUS_YELLOW = 0x02;
        public static readonly int BULK_INK_STATUS_RED = 0x03;
        public static readonly int BULK_INK_STATUS_UNKNOWN = 0x04;

        public static readonly int MAX_PRINTERS = 4;
        public static readonly int MAX_HEADS = 16;
        public static readonly int MAX_PENS = 4;
        public static readonly int MAX_USB_DEVICES = 32;

        public static readonly int MAX_QUEUE_DEPTH = 15;

        public static readonly int NUM_RESOLUTIONS_NP45 = 15; 

        /////////////////
        // error Return Codes for GETSTATUS
        /////////////////
        public enum Codes
        {
            API_SUCCESS,
            API_PARAMETER,
            API_INVALID_HANDLE,
            API_LOST_COMM_ERROR,
            API_FILE_OPEN_ERROR,
            API_FILE_RANGE_ERROR,
            API_FILE_INVALID_RECORD,
            API_LCAFILE_OPEN_ERROR,
            API_LCAFILE_READ_ERROR,
            API_PRINT_SPEED_TOO_HIGH,
            API_PRINT_TIMEOUT_PAGE_JAM,
            API_VERSION_ERROR,
            API_PRINT_DOC_SYNC_ERR_UNDERRUN,
            API_PRINT_DOC_SYNC_ERR_OVERLAP,
            API_NON_SUPPORTED_CARTRIDGE,
            API_CARTRIDGE_UNDER_VOLTAGE,
            API_CARTRIDGE_OVER_VOLTAGE,
            API_CARTRIDGE_OVER_TEMPERATURE,
            API_NO_CARTRIDGE_PRESENT,
            API_CARTRIDGE_BLOWN_FET,
            API_CARTRIDGE_OVER_CURRENT,
            API_HEAD_SYNC_ERROR
        }

        /////////////////
        // Parameters constants used in calls to GetPrinterParameter,
        // SetPrinterParameter
        /////////////////
        public enum Params
        {
            RESOLUTION,
            TRIGGER_OFFSET,
            QUEUE_DEPTH,
            SUB_SAMPLE,
            JET_BLANKING,
            AUX_OUTPUT,
            HEAD_HEIGHT,
            INK_PROFILE,
            PEN_WARMING,
            TRIGGER_MASK,
            RESOLUTION_DIRECT,
            SKIP_TRIG_DETECT,
            PRINT_WINDOW_MAX,
            PRINT_WINDOW_MIN
        }

        /////////////////
        // Warming Modes
        /////////////////
        public enum Warm_mode
        {
            WARMING_OFF,
            WARMING_PRE_WARM,
            WARMING_CONTINUOUS
        }

        /////////////////
        // Sync Options
        /////////////////
        public enum Sync_option
        {
            SYNC_DELAY_NONE,
            SYNC_DELAY_TIME,
            SYNC_DELAY_DISTANCE
        }

        /////////////////
        // Purge Modes
        /////////////////
        public enum Purge_mode
        {
            PMT_MODE_OFF,
            PMT_MODE_SPIT,
            PMT_MODE_RANDOM_SPIT
        }

        /////////////////
        // Printhead Configuration Struct
        /////////////////
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct CONFIGURATION
        {
            public int PrinterType;
            public ushort VerticalResolutionDPI;
            public ushort HorizontalResolutionDPI;
            public ushort NumberOfPrintheads;
            public ushort Head1Height;
            public ushort Head2Height;
            public ushort Head3Height;
            public ushort Head4Height;
        }

        //////////////////
        // INK PROFILE STRUCT
        //////////////////
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct INK_PROFILE
        {
            public ushort voltage;
            public ushort preFirePulseWidth;
            public ushort gapWidth;
            public ushort pulseWidth;
            public ushort temperature;
        }

        //////////////////
        // RES STRUCT
        //////////////////
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct RESOLUTION_STRUCT
        {
            public UInt32 resVerticalDPI;
            public UInt32 resHorizontalDPI;
            public ushort fastModeFactor;
            public ushort bitsPerPixel;
        }

        //////////////////
        // Function Definitions
        //////////////////
        [DllImport("res/XIJET_API.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "XIJET_GetVersionAPI")]
        public static extern double GetVersionAPI();

        [DllImport("res/XIJET_API.dll", CharSet = CharSet.Ansi, SetLastError = true, EntryPoint = "XIJET_ProbePrinter")]
        public static extern bool ProbePrinter(int index, IntPtr printerName);

        [DllImport("res/XIJET_API.dll", CharSet = CharSet.Ansi, SetLastError = true, EntryPoint = "XIJET_OpenPrinter")]
        public static extern IntPtr OpenPrinter(IntPtr printerName, IntPtr pPrinterConfig);

        [DllImport("res/XIJET_API.dll", CharSet = CharSet.Ansi, SetLastError = true, EntryPoint = "XIJET_Reset")]
        public static extern void Reset(IntPtr printerHandle);

        [DllImport("res/XIJET_API.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "XIJET_ResetPrintData")]
        public static extern void ResetPrintData(IntPtr printerHandle);

        [DllImport("res/XIJET_API.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "XIJET_ResetPrintQueue")]
        public static extern void ResetPrintQueue(IntPtr printerHandle);

        [DllImport("res/XIJET_API.dll", CharSet = CharSet.Ansi, SetLastError = true, EntryPoint = "XIJET_OpenLogFile")]
        public static extern IntPtr OpenLogFile(StringBuilder filename);

        [DllImport("res/XIJET_API.dll", CharSet = CharSet.Ansi, SetLastError = true, EntryPoint = "XIJET_GetPrinterResolution")]
        public static extern bool GetPrinterResolution(IntPtr printerName,
                                                             ushort resolutionIndex,
                                                             IntPtr ResolutionDescription,
                                                             short* HorizontalDPI,
                                                             short* VerticalDPI,
                                                             short* MaxSpeedIPS
                                                             );

        [DllImport("res/XIJET_API.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "XIJET_GetPrinterParameter")]
        public static extern int GetPrinterParameter(IntPtr printerHandle,
                                                            ushort parameterIndex,
                                                            IntPtr pParameter,
                                                            ushort HeadNumber = 0);

        [DllImport("res/XIJET_API.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "XIJET_SetPrinterParameter")]
        public static extern int SetPrinterParameter(IntPtr printerHandle,
                                                        ushort parameterIndex,
                                                        short * pParameter,
                                                        ushort HeadNumber = 0);

        [DllImport("res/XIJET_API.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "XIJET_SoftPrintTrigger")]
        public static extern int SoftPrintTrigger(IntPtr printerHandle);

        /////////////////
        // Canvas mode functions
        /////////////////
        [DllImport("res/XIJET_API.dll", CharSet = CharSet.Ansi, SetLastError = true, EntryPoint = "XIJET_LoadFontXFT")]
        public static extern IntPtr LoadFontXFT(IntPtr printerHandle, [MarshalAs(UnmanagedType.LPStr)] string fileName);

        [DllImport("res/XIJET_API.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "XIJET_CanvasBegin")]
        public static extern int CanvasBegin(IntPtr printerHandle,
                                                      UInt32 VerticalHeight,
                                                      UInt32 HorizontalWidth);

        [DllImport("res/XIJET_API.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "XIJET_CanvasWriteStr")]
        public static extern int CanvasWriteStr(IntPtr printerHandle,
                                                      IntPtr fontHandle,
                                                      [MarshalAs(UnmanagedType.LPStr)] string asciiStr,
                                                      UInt32 VerticalOffset,
                                                      UInt32 HorizontalOffset,
                                                      long LineToLineOffset);

        [DllImport("res/XIJET_API.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "XIJET_CanvasWrite")]
        public static extern int CanvasWrite(IntPtr printerHandle,
                                                UInt32 VerticalOffset,
                                                UInt32 HorizontalOffset,
                                                UInt32 VerticalHeight,
                                                UInt32 HorizontalHeight,
                                                byte[] pPixelBuffer);

        [DllImport("res/XIJET_API.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "XIJET_CanvasWriteRGB")]
        public static extern int CanvasWriteRGB(IntPtr printerHandle,
                                                UInt32 VerticalOffset,
                                                UInt32 HorizontalOffset,
                                                UInt32 VerticalHeight,
                                                UInt32 HorizontalHeight,
                                                IntPtr pPixelBuffer);

        [DllImport("res/XIJET_API.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "XIJET_CanvasPrint")]
        public static extern int CanvasPrint(IntPtr printerHandle,
                                                   UInt32 Head1Offset,
                                                   UInt32 Head2Offset,
                                                   UInt32 Head3Offset,
                                                   UInt32 Head4Offset,
                                                   UInt32 Timeout);

        //////////////////
        // Status/misc functions
        //////////////////
        [DllImport("res/XIJET_API.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "XIJET_WaitForPrintComplete")]
        public static extern int WaitForPrintComplete( IntPtr printerHandle, UInt32 timeoutMSecs);

        [DllImport("res/XIJET_API.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "XIJET_GetStatus")]
        public static extern int GetStatus(IntPtr printerHandle, IntPtr pStatusMessage);

        [DllImport("res/XIJET_API.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "XIJET_GetInkRemaining")]
        public static extern int GetInkRemaining(IntPtr printerHandle,
                                                 ushort headIndex,
                                                 short* remainingPen1,
                                                 short* remainingPen2,
                                                 short* remainingPen3,
                                                 short* remainingPen4
                                                 );

        [DllImport("res/XIJET_API.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "XIJET_GetTransportSpeed")]
        public static extern int GetTransportSpeed(IntPtr printerHandle, float* transportSpeedIPS);

        [DllImport("res/XIJET_API.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "XIJET_ResetInkCartridge")]
        public static extern int ResetInkCartridge(IntPtr printerHandle, ushort headIndex, ushort penIndex);

        [DllImport("res/XIJET_API.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "XIJET_ClosePrinter")]
        public static extern void ClosePrinter(IntPtr printerHandle);
    }
}