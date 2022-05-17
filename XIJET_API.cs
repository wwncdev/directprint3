using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace XIJET_API
{
    public unsafe class XIJET
    {
        public static readonly double XIJET_API_VERSION = 10.0;

        public static readonly int INVALID_HANDLE_VALUE = -1;

        public enum BULK_INK_STATUS
        {
            NONE = 0x00,
            GREEN = 0x01,
            YELLOW = 0x02,
            RED = 0x03,
            UNKNOWN = 0x04
        }

        public static readonly int MAX_PRINTERS = 4;
        public static readonly int MAX_HEADS = 16;
        public static readonly int MAX_PENS = 4;
        public static readonly int MAX_USB_DEVICES = 32;

        public static readonly int MAX_QUEUE_DEPTH = 15;

        public static readonly int NUM_RESOLUTIONS_NP45 = 15;

        public static readonly int NOMINAL_PEN_HEIGHT = 150;
        public static readonly int NOMINAL_PEN_OVERLAP = 6;
        public static readonly int NOMINAL_RESOLUTION_DPI = 300;

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
        public struct INK_PROFILE : IEquatable<INK_PROFILE>
        {
            public ushort voltage;
            public ushort preFirePulseWidth; //lexmark only (ignore?)
            public ushort gapWidth;          //lexmark only (ignore?)
            public ushort pulseWidth;
            public ushort temperature;

            public static bool operator ==(INK_PROFILE left, INK_PROFILE right) =>
                Equals(left, right);

            public static bool operator !=(INK_PROFILE left, INK_PROFILE right) =>
                !Equals(left, right);

            public override bool Equals(object obj)
            {
                return (obj is INK_PROFILE INK_PROFILE) && Equals(INK_PROFILE);
            }

            public bool Equals(INK_PROFILE Ink_Profile)
            {
                return Ink_Profile.voltage == voltage &&
                Ink_Profile.preFirePulseWidth == preFirePulseWidth &&
                Ink_Profile.gapWidth == gapWidth &&
                Ink_Profile.pulseWidth == pulseWidth &&
                Ink_Profile.temperature == temperature;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(voltage, preFirePulseWidth, gapWidth, pulseWidth, temperature);
            }
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

        /////////////////
        // error Return Codes for GETSTATUS
        /////////////////
        public enum Codes
        {
            API_SUCCESS = 0,
            API_INVALID_PARAMETER = 1,
            API_INVALID_HANDLE = 2,
            API_LOST_COMM_ERROR = 3,
            API_FILE_OPEN_ERROR = 4,
            API_FILE_RANGE_ERROR = 5,
            API_FILE_INVALID_RECORD = 6,
            API_LCAFILE_OPEN_ERROR = 7,
            API_LCAFILE_READ_ERROR = 8,
            API_PRINT_SPEED_TOO_HIGH = 9,
            API_PRINT_TIMEOUT_PAGE_JAM = 10,
            API_VERSION_ERROR = 11,
            API_PRINT_DOC_SYNC_ERR_UNDERRUN = 12,
            API_PRINT_DOC_SYNC_ERR_OVERLAP = 13,
            API_NON_SUPPORTED_CARTRIDGE = 14,
            API_CARTRIDGE_UNDER_VOLTAGE = 15,
            API_CARTRIDGE_OVER_VOLTAGE = 16,
            API_CARTRIDGE_OVER_TEMPERATURE = 17,
            API_NO_CARTRIDGE_PRESENT = 18,
            API_CARTRIDGE_BLOWN_FET = 19,
            API_CARTRIDGE_OVER_CURRENT = 20,
            API_HEAD_SYNC_ERROR = 21
        }

        /////////////////
        // Parameters constants used in calls to GetPrinterParameter,
        // SetPrinterParameter
        /////////////////
        public enum Params
        {
            RESOLUTION = 0,
            QUEUE_DEPTH = 1,
            SSP_NSP_2 = 2,
            JET_BLANKING = 3,
            AUX_OUTPUT = 4,
            SSP_NSP_5 = 5,
            HEAD_HEIGHT = 6,
            INK_PROFILE = 7,
            PEN_WARMING = 8,
            TRIGGER_MASK = 9,
            SSP_NSP_10 = 10,
            SKIP_TRIG_DETECT = 11,
            PRINT_WINDOW_MAX = 12,
            PRINT_WINDOW_MIN = 13,
            PRINT_DELAY = 14,
            HEAD_ORIENTATION = 100,
            ENCODER_RES = 101,
            TRIGGER_TYPE = 102,
            TRIGGER_OFFSET = 103,
            VERT_JETS_P1 = 104,
            VERT_JETS_P2 = 105,
            VERT_JETS_P3 = 106,
            VERT_JETS_P4 = 107,
            HORZ_OFF_P1 = 108,
            HORZ_OFF_P2 = 109,
            HORZ_OFF_P3 = 110,
            HORZ_OFF_P4 = 111
        }

        public struct ParameterSet : IEquatable<ParameterSet>
        {
            public XIJET.RES RESOLUTION;
            public int QUEUE_DEPTH;
            public int SSP_NSP_2;
            public int JET_BLANKING;
            public int AUX_OUTPUT;
            public int SSP_NSP_5;
            public int HEAD_HEIGHT;
            public XIJET.INK_PROFILE INK_PROFILE;
            public int TRIGGER_MASK;
            public int SSP_NSP_10;
            public int SKIP_TRIG_DETECT;
            public int PRINT_WINDOW_MAX;
            public int PRINT_WINDOW_MIN;
            public int PRINT_DELAY;
            public XIJET.HEAD_ORIENTATION HEAD_ORIENTATION;
            public int ENCODER_RES;
            public XIJET.TRIGGER_TYPE TRIGGER_TYPE;
            public int TRIGGER_OFFSET;
            public int VERT_JETS_P1;
            public int VERT_JETS_P2;
            public int VERT_JETS_P3;
            public int VERT_JETS_P4;
            public int HORZ_OFF_P1;
            public int HORZ_OFF_P2;
            public int HORZ_OFF_P3;
            public int HORZ_OFF_P4;

            public static bool operator ==(ParameterSet left, ParameterSet right) =>
                Equals(left, right);

            public static bool operator !=(ParameterSet left, ParameterSet right) =>
                !Equals(left, right);

            public override bool Equals(object obj) =>
                (obj is ParameterSet parameterSet) && Equals(parameterSet);

            public bool Equals(ParameterSet p) =>
                (p.RESOLUTION, p.QUEUE_DEPTH, p.SSP_NSP_2, p.JET_BLANKING, p.AUX_OUTPUT, p.SSP_NSP_5, p.HEAD_HEIGHT, p.INK_PROFILE, p.TRIGGER_MASK, p.SSP_NSP_10, p.SKIP_TRIG_DETECT, p.PRINT_WINDOW_MAX, p.PRINT_WINDOW_MIN, p.PRINT_DELAY, p.HEAD_ORIENTATION, p.ENCODER_RES, p.TRIGGER_TYPE, p.VERT_JETS_P1, p.VERT_JETS_P2, p.VERT_JETS_P3, p.VERT_JETS_P4, p.HORZ_OFF_P1, p.HORZ_OFF_P2, p.HORZ_OFF_P3, p.HORZ_OFF_P4)
                == (RESOLUTION, QUEUE_DEPTH, SSP_NSP_2, JET_BLANKING, AUX_OUTPUT, SSP_NSP_5, HEAD_HEIGHT, INK_PROFILE, TRIGGER_MASK, SSP_NSP_10, SKIP_TRIG_DETECT, PRINT_WINDOW_MAX, PRINT_WINDOW_MIN, PRINT_DELAY, HEAD_ORIENTATION, ENCODER_RES, TRIGGER_TYPE, VERT_JETS_P1, VERT_JETS_P2, VERT_JETS_P3, VERT_JETS_P4, HORZ_OFF_P1, HORZ_OFF_P2, HORZ_OFF_P3, HORZ_OFF_P4);

            public override int GetHashCode() =>
                (RESOLUTION, QUEUE_DEPTH, SSP_NSP_2, JET_BLANKING, AUX_OUTPUT, SSP_NSP_5, HEAD_HEIGHT, INK_PROFILE, TRIGGER_MASK, SSP_NSP_10, SKIP_TRIG_DETECT, PRINT_WINDOW_MAX, PRINT_WINDOW_MIN, PRINT_DELAY, HEAD_ORIENTATION, ENCODER_RES, TRIGGER_TYPE, VERT_JETS_P1, VERT_JETS_P2, VERT_JETS_P3, VERT_JETS_P4, HORZ_OFF_P1, HORZ_OFF_P2, HORZ_OFF_P3, HORZ_OFF_P4).GetHashCode();
        }

        public enum HEAD_INDEX
        {
            HEAD_1 = 0,
            HEAD_2 = 1,
            HEAD_3 = 2,
            HEAD_4 = 3,
            ALL_HEADS = 0xffff
        }

        /////////////////
        // TRIGGER TYPES 
        /////////////////
        public enum HEAD_ORIENTATION
        {
            A = 0,
            B = 1,
            C = 2,
            D = 3
        }

        /////////////////
        // TRIGGER TYPES 
        /////////////////
        public enum TRIGGER_TYPE
        {
            AUTO_GEN = 0,
            RISING = 1,
            FALLING = 2,
            SOFTWARE = 3
        }

        /////////////////
        // Warming Modes
        /////////////////
        public enum WARMING
        {
            OFF = 0,
            PRE_WARM = 1,
            CONTINUOUS = 2
        }

        public enum RES
        {
            RES_600_600 = 0,
            RES_600_300 = 1,
            RES_600_250 = 2,
            RES_600_200 = 3,
            RES_600_150 = 4,
            RES_600_100 = 5,
            RES_300_300fast = 6,
            RES_300_300dark = 7,
            RES_300_150fast = 8,
            RES_300_200fast = 9,
            RES_300_250fast = 10,
            RES_300_100fast = 11,
            RES_300_300 = 12,
            RES_300_600 = 13
        }

        /////////////////
        // Sync Options
        /////////////////
        public enum SYNC_DELAY
        {
            NONE = 0,
            TIME = 1,
            DISTANCE = 2
        }

        /////////////////
        // Purge Modes
        /////////////////
        public enum PURGE_MODE
        {
            OFF = 0,
            SPIT = 1,
            RANDOM_SPIT = 2
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

        [DllImport("res/XIJET_API.dll", CharSet = CharSet.Ansi, SetLastError = true, EntryPoint = "XIJET_CloseLogFile")]
        public static extern void CloseLogFile();

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
                                                            ushort headIndex = (ushort)XIJET.HEAD_INDEX.HEAD_1);

        [DllImport("res/XIJET_API.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "XIJET_SetPrinterParameter")]
        public static extern int SetPrinterParameter(IntPtr printerHandle,
                                                        ushort parameterIndex,
                                                        IntPtr pParameter,
                                                        ushort headIndex = (ushort)XIJET.HEAD_INDEX.HEAD_1);

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
        public static extern int WaitForPrintComplete(IntPtr printerHandle, UInt32 timeoutMSecs);

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

        [DllImport("res/XIJET_API.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "XIJET_ActivateInkPurge")]
        public static extern int ActivateInkPurge(IntPtr printerHandle,
                                           byte purgeMode,
                                           UInt32 purgeParameter1 = 0,
                                           UInt32 purgeParameter2 = 0,
                                           UInt32 purgeParameter3 = 0,
                                           UInt32 purgeParameter4 = 0);

    }
}