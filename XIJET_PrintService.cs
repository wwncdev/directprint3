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
        private static readonly int BitsSize = 25600;
//        private static byte[,] Bits = new byte[4,25600];
        private static int LastBitBufferUsed = 0;

        public static bool Init()
        {
            for(int i = 0; i < 4; i++)
            {
                Bits[i] = new byte[BitsSize];
            }
            // set up printer name buffer for extern function to modify (gross)
            PrinterName = Marshal.AllocHGlobal(4);
            PrinterHandle = IntPtr.Zero;
            IntPtr pPrinterConfig = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(XIJET.CONFIGURATION)));
            int XiJetStatus;

            Marshal.PrelinkAll( typeof(XIJET)); // Marshal Everything (probably will change to more specific Marshaling)

            XIJET.OpenLogFile(new StringBuilder("xijet.log"));

            /////////////////
            // PROBE PRINTER
            /////////////////
            // extern probe printer function, modifies printer name buffer and returns boolean
            if (XIJET.ProbePrinter(0, PrinterName ) == false)
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

            IntPtr pStatusMessage = Marshal.AllocHGlobal(4);

            ///////////////
            //Set Parameter
            ///////////////
            short resParameter = 6;
            XiJetStatus = XIJET.SetPrinterParameter(PrinterHandle, 0, &resParameter);
            Console.WriteLine("Status: " + Marshal.PtrToStringAnsi(pStatusMessage));
            if (XiJetStatus == 0)
            {
                Console.WriteLine("Error return from SetPrinterParameter!");
                // use XIJET_GetStatus to retrieve a message
                XIJET.GetStatus(PrinterHandle, pStatusMessage);
                Console.WriteLine("Status: " + Marshal.PtrToStringAnsi(pStatusMessage));
                return false;
            }
            Marshal.FreeHGlobal(pStatusMessage);
            initialized = true;
            return true;
        }

        public static void Close()
        {
            XIJET.ClosePrinter(PrinterHandle);
            Marshal.FreeHGlobal(PrinterHandle);
            Marshal.FreeHGlobal(PrinterName);
            initialized = false;
        }

        public static bool Print(string imageEncoded)
        {
            long beforePrint = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            byte[] decoded;
            decoded = Convert.FromBase64String(imageEncoded);
            long timeToDecode = DateTimeOffset.Now.ToUnixTimeMilliseconds() - beforePrint;
           // Console.WriteLine("Time To Decode: " + timeToDecode);

            int imgLen = decoded.Length;

            int pixelArrayOffset = decoded[10]+decoded[11]*256;
            int imageWidth = decoded[18]+decoded[19]*256;
            int imageHeight= decoded[22]+decoded[23]*256;
            int colorPlanes = decoded[26];
            int bitsPerPixel = decoded[28];

            byte[] pixelArrayAsBytes = new byte[imageWidth * imageHeight]; // declare byte array for pixels
            Buffer.BlockCopy(decoded, pixelArrayOffset, pixelArrayAsBytes, 0, imageWidth * imageHeight); // copy pixel array off decoded bmp
            int bitBufferUsed = Printer.convertBMPtoBITS(pixelArrayAsBytes, imageWidth, imageHeight);
            //Printer.displayBITS2(pixelArrayAsBits, imageWidth, imageHeight);

            XIJET.CanvasBegin(PrinterHandle, (uint)(imageWidth * 2), (uint)(imageHeight * 4));
            XIJET.CanvasWrite(PrinterHandle, 0, 0, (uint)imageWidth, (uint)imageHeight, Bits[bitBufferUsed]);

            IntPtr pStatusMessage = Marshal.AllocHGlobal(4);
            int XiJetStatus = 0;
            while (XiJetStatus == 0) // keep printing from internal queue until status changes
            {
                XiJetStatus = XIJET.CanvasPrint(PrinterHandle, 0, 1200, 0, 0, 100);
            }
           // Console.WriteLine("After Canvas Print Loop");
            if (XiJetStatus != 1) // status indicates error, retrieve and print
            {
                XIJET.GetStatus(PrinterHandle, pStatusMessage);
             //   Console.WriteLine($"XiJet status: {0}", Marshal.PtrToStringAnsi(pStatusMessage));
             //   Console.WriteLine("return");
            }
            if (XiJetStatus < 0) // terminated with an error, issue a reset
            {
            //    Console.WriteLine("Reset Printer");
                XIJET.Reset(PrinterHandle);
                return false;
            }
            else
            {
                /*Console.WriteLine("Terminated without an error");
                Console.WriteLine(Marshal.PtrToStringAnsi(pStatusMessage));
                XiJetStatus = 0; // reset status
                while (XiJetStatus == 0)
                {
                    XiJetStatus = XIJET.WaitForPrintComplete(PrinterHandle, 100);
                    Console.WriteLine("wait for print complete");
                }*/
            }
            XiJetStatus = 0;
            Marshal.FreeHGlobal(pStatusMessage);
            return true;

/*             Console.WriteLine("Offset of Pixel Array: " + pixelArrayOffset);
            Console.WriteLine("Image Width: " + imageWidth);
            Console.WriteLine("Image Height: " + imageHeight);
            Console.WriteLine("Color Planes: " + colorPlanes);
            Console.WriteLine("Bits Per Pixel: " + bitsPerPixel); */
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
            int currentBitBuffer = (LastBitBufferUsed+1)%4;
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
