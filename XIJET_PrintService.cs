using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using XIJET_API;

namespace XIJET_PrintService
{
    public unsafe static class Printer
    {
        public static bool initialized = false;
        public static IntPtr PrinterHandle;
        public static IntPtr PrinterName;
        private static byte[][] Bits = new byte[4][];
        private static int LastBitBufferUsed = 0;
        const ushort XIJET_TRIGGER_TYPE = 102;// USHORT - NON-VOL - (see constants below)
        const ushort XIJET_TRIGGER_MASK =   9;		// ULONG -	Mask print trigger for distance (in mils)
        static short trigger_mask_value = 2600; // 5.5 inches
        public static short inPrintBits = 0;


        public static bool Init(int printerNumber, bool dblWidth=false)
        {
            // set up printer name buffer for extern function to modify (gross)
            PrinterName = Marshal.AllocHGlobal(8);
            PrinterHandle = IntPtr.Zero;
            IntPtr pPrinterConfig = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(XIJET.CONFIGURATION)));

            Marshal.PrelinkAll( typeof(XIJET)); // Marshal Everything (probably will change to more specific Marshaling)

            XIJET.OpenLogFile(new StringBuilder("xijet"+printerNumber.ToString()+".log"));

            /////////////////
            // PROBE PRINTER
            /////////////////
            // extern probe printer function, modifies printer name buffer and returns boolean
            if (XIJET.ProbePrinter(printerNumber-1, PrinterName ) == false)
            {
                Console.WriteLine("No printers found");
                return false;
            }
            Console.WriteLine("Found Printer: " + Marshal.PtrToStringAnsi(PrinterName));

            /////////////////
            // Open Printer and Write Config to console
            /////////////////
            PrinterHandle = XIJET.OpenPrinter(PrinterName, pPrinterConfig);
            Console.WriteLine("Printer Type: " + Marshal.PtrToStructure<XIJET.CONFIGURATION>(pPrinterConfig).PrinterType);
            Console.WriteLine("Vertical Res: " + Marshal.PtrToStructure<XIJET.CONFIGURATION>(pPrinterConfig).VerticalResolutionDPI);
            Console.WriteLine("Horizontal Res: " + Marshal.PtrToStructure<XIJET.CONFIGURATION>(pPrinterConfig).HorizontalResolutionDPI);
            Console.WriteLine("Number of Print Heads: " + Marshal.PtrToStructure<XIJET.CONFIGURATION>(pPrinterConfig).NumberOfPrintheads);
            Marshal.FreeHGlobal(pPrinterConfig);
            if (PrinterHandle.ToInt32() == XIJET.INVALID_HANDLE_VALUE)
            {
                Console.WriteLine("Error opening printer");
                return false;
            }
            Flush(dblWidth);
            //DisplayParams();
            inPrintBits = 0;
            initialized = true;
            //IntPtr inkProfile = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(XIJET.INK_PROFILE)));  // *** guessing this needs updating

            return true;
        }

        public static short GetTriggerOffset()
        {
            IntPtr triggerOffsetptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(ushort)));
            XIJET.GetPrinterParameter(PrinterHandle, (ushort)XIJET.Params.TRIGGER_OFFSET, triggerOffsetptr);
            Console.WriteLine("PARAM - TRIGGER_OFFSET: " + Marshal.ReadInt16(triggerOffsetptr));
            short triggerOffset = Marshal.ReadInt16(triggerOffsetptr); 
            return triggerOffset;

        }


        public static void DisplayParams()
        {
            Console.WriteLine("Size of Ushort: "+Marshal.SizeOf(typeof(ushort)).ToString());
            IntPtr resolution = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(ushort)));
            IntPtr queueDepth = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(ushort)));
            IntPtr subSample = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(ushort)));
            IntPtr jetBlanking = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(ushort)));
            IntPtr auxOutput = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(ushort)));
            IntPtr triggerOffsetptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(ushort)));
            IntPtr headHeight = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(ushort)));
            IntPtr inkProfile = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(XIJET.INK_PROFILE)));  // *** guessing this needs updating
            IntPtr penWarming = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(ushort)));
            IntPtr triggerMask = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(UInt32)));
            IntPtr resolutionDirect = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(XIJET.RESOLUTION_STRUCT)));
            IntPtr skipTrigDetect = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(ushort)));

            XIJET.GetPrinterParameter(PrinterHandle, (ushort)XIJET.Params.RESOLUTION, resolution);
            XIJET.GetPrinterParameter(PrinterHandle, (ushort)XIJET.Params.QUEUE_DEPTH, queueDepth);
        //    XIJET.GetPrinterParameter(PrinterHandle, (ushort)XIJET.Params.SUB_SAMPLE, subSample);
            XIJET.GetPrinterParameter(PrinterHandle, (ushort)XIJET.Params.JET_BLANKING, jetBlanking);
            XIJET.GetPrinterParameter(PrinterHandle, (ushort)XIJET.Params.AUX_OUTPUT, auxOutput);
            XIJET.GetPrinterParameter(PrinterHandle, (ushort)XIJET.Params.TRIGGER_OFFSET, triggerOffsetptr);
            XIJET.GetPrinterParameter(PrinterHandle, (ushort)XIJET.Params.HEAD_HEIGHT, headHeight);
            XIJET.GetPrinterParameter(PrinterHandle, (ushort)XIJET.Params.INK_PROFILE, inkProfile);
            XIJET.GetPrinterParameter(PrinterHandle, (ushort)XIJET.Params.PEN_WARMING, penWarming);
            XIJET.GetPrinterParameter(PrinterHandle, (ushort)XIJET.Params.TRIGGER_MASK, triggerMask);
//            XIJET.GetPrinterParameter(PrinterHandle, (ushort)XIJET.Params.RESOLUTION_DIRECT, resolutionDirect);
            XIJET.GetPrinterParameter(PrinterHandle, (ushort)XIJET.Params.SKIP_TRIG_DETECT, skipTrigDetect);

            Console.WriteLine("PARAM - RESOLUTION: " + Marshal.ReadInt16(resolution));
            Console.WriteLine("PARAM - QUEUE_DEPTH: " + Marshal.ReadInt16(queueDepth));
            Console.WriteLine("PARAM - SUB_SAMPLE: " + Marshal.ReadInt16(subSample));
            Console.WriteLine("PARAM - JET_BLANKING: " + Marshal.ReadInt16(jetBlanking));
            Console.WriteLine("PARAM - AUX_OUTPUT: " + Marshal.ReadInt16(auxOutput));
            Console.WriteLine("PARAM - TRIGGER_OFFSET: " + Marshal.ReadInt16(triggerOffsetptr));
            Console.WriteLine("PARAM - HEAD_HEIGHT: " + Marshal.ReadInt16(headHeight));
            Console.WriteLine("PARAM - INK_PROFILE - preFirePulseWidth: " + Marshal.PtrToStructure<XIJET.INK_PROFILE>(inkProfile).preFirePulseWidth);
            Console.WriteLine("PARAM - INK_PROFILE - gapWidth: " + Marshal.PtrToStructure<XIJET.INK_PROFILE>(inkProfile).gapWidth);
            Console.WriteLine("PARAM - INK_PROFILE - pulseWidth: " + Marshal.PtrToStructure<XIJET.INK_PROFILE>(inkProfile).pulseWidth);
            Console.WriteLine("PARAM - INK_PROFILE - temperature: " + Marshal.PtrToStructure<XIJET.INK_PROFILE>(inkProfile).temperature);
            Console.WriteLine("PARAM - INK_PROFILE - voltage: " + Marshal.PtrToStructure<XIJET.INK_PROFILE>(inkProfile).voltage);

            Console.WriteLine("PARAM - PEN_WARMING: " + Marshal.ReadInt16(penWarming));
            Console.WriteLine("PARAM - TRIGGER_MASK: " + Marshal.ReadIntPtr(triggerMask));
            Console.WriteLine("inPrintBits" + inPrintBits.ToString());
            System.Threading.Thread.Sleep(500);

            Marshal.FreeHGlobal(resolution);
            Marshal.FreeHGlobal(queueDepth);
            Marshal.FreeHGlobal(subSample);
            Marshal.FreeHGlobal(jetBlanking);
            Marshal.FreeHGlobal(auxOutput);
            Marshal.FreeHGlobal(triggerOffsetptr);
            Marshal.FreeHGlobal(headHeight);
            Marshal.FreeHGlobal(inkProfile);
            Marshal.FreeHGlobal(penWarming);
            Marshal.FreeHGlobal(triggerMask);
            Marshal.FreeHGlobal(resolutionDirect);
            Marshal.FreeHGlobal(skipTrigDetect);
        }

        public static void Flush(bool dblWidth, short triggerOffset = 6750)
        {
            IntPtr _param = IntPtr.Zero;
            int XiJetStatus;
            IntPtr trigger_mask_value_ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(ushort)));

            XIJET.Reset(PrinterHandle);
            short resParameter = 12; // 300x300 normal
         //   short resParameter = 7; // 300x300 dark
                                     //           resParameter = 0; // 300x300 Fast
            if (dblWidth) resParameter = 1; // 600x300 dark
                                            //            short resParameter = 0; // 600x600
            _param = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(UInt32))); //unmanaged intptr
            Marshal.WriteIntPtr(_param, new IntPtr(resParameter));

            XiJetStatus = XIJET.SetPrinterParameter(PrinterHandle, 0, _param);
            //short headOrientation = 1;  // probably 3 now
            //XiJetStatus = XIJET.SetPrinterParameter(PrinterHandle, 100, &headOrientation);
            //short PrinterQueueDepthValue = 20;
            //int success = XIJET.SetPrinterParameter(PrinterHandle, (ushort)XIJET.Params.QUEUE_DEPTH, &PrinterQueueDepthValue);
            //            short printDelay = 2000;
            //short trigger_type_value = 1;

            IntPtr tmask = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(UInt32)));
            Marshal.WriteIntPtr(tmask, new IntPtr(trigger_mask_value));

            XiJetStatus = XIJET.SetPrinterParameter(PrinterHandle, XIJET_TRIGGER_MASK, tmask);  // new say to make this work?
            Marshal.FreeHGlobal(tmask);
            Marshal.FreeHGlobal(_param);
        }

        
        public static void SetInkVoltage(short newVoltage)
        {
            int result;

            // get ink profile into unmanaged memory
            IntPtr inkProfile = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(XIJET.INK_PROFILE)));  // *** guessing this needs updating
            result = XIJET.GetPrinterParameter(PrinterHandle, (ushort)XIJET.Params.INK_PROFILE, inkProfile);

            if (result == 1)
            {
                // copy ink profile to managed memory
                XIJET.INK_PROFILE inkp;
                inkp = (XIJET.INK_PROFILE)Marshal.PtrToStructure(inkProfile, typeof(XIJET.INK_PROFILE));
                
                // modify the ink profile
                inkp.voltage = (ushort)(inkp.voltage + newVoltage);
                  
                // copy the modified ink profile back to unmanaged memory and set it
                Marshal.StructureToPtr(inkp, inkProfile, false);
                result = XIJET.SetPrinterParameter(PrinterHandle, (ushort)XIJET.Params.INK_PROFILE, inkProfile);
                
                // test if that worked.
                result = XIJET.GetPrinterParameter(PrinterHandle, (ushort)XIJET.Params.INK_PROFILE, inkProfile);

                //Console.WriteLine("PARAM - INK_PROFILE - preFirePulseWidth: " + Marshal.PtrToStructure<XIJET.INK_PROFILE>(inkProfile).preFirePulseWidth);
                //Console.WriteLine("PARAM - INK_PROFILE - gapWidth: " + Marshal.PtrToStructure<XIJET.INK_PROFILE>(inkProfile).gapWidth);
                //Console.WriteLine("PARAM - INK_PROFILE - pulseWidth: " + Marshal.PtrToStructure<XIJET.INK_PROFILE>(inkProfile).pulseWidth);
                Console.WriteLine("PARAM - INK_PROFILE - temperature: " + Marshal.PtrToStructure<XIJET.INK_PROFILE>(inkProfile).temperature);
                Console.WriteLine("PARAM - INK_PROFILE - voltage: " + Marshal.PtrToStructure<XIJET.INK_PROFILE>(inkProfile).voltage);
                Marshal.FreeHGlobal(inkProfile);
            }
        }

        public static void Close()
        {
            XIJET.ClosePrinter(PrinterHandle);
            Marshal.FreeHGlobal(PrinterHandle);
            Marshal.FreeHGlobal(PrinterName);
            initialized = false;
        }

        public static int WaitForPrintComplete(uint timeout=5000)
        {
            int success = XIJET.WaitForPrintComplete(PrinterHandle, timeout);
            if (success < 1) Console.WriteLine("WaitForPrintComplete error");
            return success;
        }

        public static int ActivateInkPurge()
        {
            byte mode = (byte) XIJET.PURGE_MODE.SPIT;
            int success = XIJET.ActivateInkPurge(PrinterHandle,mode,300,0,0,0);
            return success;
        }


        public static bool PrintBits(byte[] Bits,int imageWidth=640,int imageHeight=192,int yoffset=0)
        {
            inPrintBits++;
            if (inPrintBits == 1)
            {
                XIJET.CanvasBegin(PrinterHandle, (uint)(imageWidth), (uint)(imageHeight));  //hast to be long enough
//            XIJET.CanvasBegin(PrinterHandle, (uint)(imageWidth), 1725);  //hast to be long enough
                XIJET.CanvasWrite(PrinterHandle, 0, (uint)yoffset, (uint)imageWidth, (uint)imageHeight, Bits);
                //            XIJET.CanvasWrite(PrinterHandle, 0, 50, (uint)imageWidth, (uint)imageHeight, Bits);
                IntPtr pStatusMessage = Marshal.AllocHGlobal(4);
                int XiJetStatus = 0;
                while (XiJetStatus == 0) // keep printing from internal queue until status changes
                {
                    //Console.Write("*");
                    XiJetStatus = XIJET.CanvasPrint(PrinterHandle, 0, 1200, 0, 0, 100);
                    //Console.WriteLine("XiJetStatus:" + XiJetStatus.ToString());
                    
                }
                // Console.WriteLine("After Canvas Print Loop");
                if (XiJetStatus != 1) // status indicates error, retrieve and print
                {
                    Console.WriteLine("Status Code: " + XiJetStatus.ToString());
                    XIJET.GetStatus(PrinterHandle, pStatusMessage);
                    Console.WriteLine($"XiJet status: {0}", Marshal.PtrToStringAnsi(pStatusMessage));
                    Console.WriteLine("return");
                }
                if (XiJetStatus < 0) // terminated with an error, issue a reset
                {
                    Console.WriteLine("Reset Printer");
                    XIJET.Reset(PrinterHandle);
                    inPrintBits--;
                    return false;
                }
                else
                {
                    //Console.WriteLine("Terminated without an error");
                    //Console.WriteLine(Marshal.PtrToStringAnsi(pStatusMessage));
                    XiJetStatus = 0; // reset status
                   /* 
                    while (XiJetStatus == 0)
                    {
                        XiJetStatus = XIJET.WaitForPrintComplete(PrinterHandle, 100);
                       // Console.WriteLine("wait for print complete");
                    }
                    //Console.WriteLine("Print Completed: " + XiJetStatus.ToString());
                    */
                }
                XiJetStatus = 0;
                Marshal.FreeHGlobal(pStatusMessage);
                inPrintBits--;
                return true;
            }
            else
            {
                Console.WriteLine("Tried to re-enter PrintBits!!!");
                return false;
            }
        }

        public static void displayHex(string imageEncoded)
        {
            byte[] decoded;
            decoded = Convert.FromBase64String(imageEncoded);
            for (int x = 0; x < 512; x++)
            {
                if (x % 16 == 0)
                {
                    System.Threading.Thread.Sleep(50);
                    Console.WriteLine();
                    Console.Write(x.ToString("X4")+":");
                }
                Console.Write(decoded[x].ToString("X2") + " ");
            }
        }
        public static void displayBITS2(byte[] bits, int imageWidth, int imageHeight)
        {
            int numBytes = imageWidth * imageHeight;
            int bytesPerRow = imageWidth / 8;  
       //     Console.WriteLine($"Byte per row: {0} imageWidth:{1} imageHeight:{2}", bytesPerRow, imageWidth, imageHeight);

            for (int y = imageHeight - 1; y > 0; y--) 
            {
                for (uint x = 0; x < bytesPerRow; x++) 
                {
                    int aByte = bits[y *bytesPerRow+x];
                    for (int b = 0; b < 8; b++) 
                    {
                        if ( (aByte & (1 << 7-b)) !=0) // bitwise not first class citizen
                        {
                            Console.Write("X");
                        }
                        else 
                        {
                            Console.Write(" ");
                        }
                    }
                }
                Console.Write("\n");
            }
        }

        public static int convertBMPtoBITS(byte[] bmp, int imageWidth,int imageHeight) 
        {
            // Some Image and Data Metrics 
            int numBytes = imageWidth * imageHeight;
            int bytesPerRow = imageWidth / 8;  // this must be evenly divisible by 4
            int currentBitBuffer = (LastBitBufferUsed+1)%4; // code is not reentrant and no protected either... 
            // declare byte array bits  to copy into
//            byte[] bits = new byte[numBytes / 8 + 1]; // declare destination byte array

            for (int y = 0; y < imageHeight; y++) 
            {
                for (int x = 0; x < imageWidth; x++) 
                {
                    if ((x%8)==0)Bits[currentBitBuffer][y*bytesPerRow+x/8]=0;  // initialize byte
                    byte bmpByte = bmp[y * imageWidth + x];
                    byte aBit = 0;
                    if (bmpByte != 1U)
                    {
                        aBit = 1;
                    } 
                    Bits[currentBitBuffer][(y * bytesPerRow) + (x / 8)] += (byte)(aBit<< 7-((x % 8))); // added the 7- to test msb lsb issue
                }
            }
            LastBitBufferUsed=currentBitBuffer;  // number of bit buffers = 4
            return currentBitBuffer;
        }

        [DllImport("msvcrt.dll", EntryPoint = "memset", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public static extern IntPtr MemSet(IntPtr dest, int c, int byteCount);

    }

}
