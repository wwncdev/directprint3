using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using Cognex.DataMan.SDK;
using Tags;


namespace dp_printer_prod
{
    /**************************************************************
      * if edge detector says there is a tag then "pull the trigger" on the barcode scanner.
      * 
      */
    public static class BarcodeScanner
    {
        public volatile static string BarcodeData = "INITIAL BARCODE";
        static int BarcodesRead = 0;
        static IPAddress ip_BarcodeReader1; 
        static public DataManSystem BarcodeReader1;

#pragma warning disable CA2211 // Non-constant fields should not be visible
        public static Queue<Tag> barcodes;
#pragma warning restore CA2211 // Non-constant fields should not be visible
        public static Queue<Tag> edges;
        static List<Tag> Tags;
        public static bool initialized = false;
        // public static Timer bcLagTimer = new System.Timers.Timer();
        public static bool allowGuessing = true;
        public static string lastBarcode;
        public static string mode = "print";  // or test

        static int BarcodeReaderTimeout = 5500;
        static bool TagAutoMode = true;

        public static long triggerBeginTime = 0;
        public static bool simulateTagIsWaiting = false;


        public static void Start(string ipAddress)
        {
            ip_BarcodeReader1 = IPAddress.Parse(ipAddress);
            barcodes = new Queue<Tag>();
            edges = new Queue<Tag>();

            EthSystemConnector conn_BarcodeReader1 = new EthSystemConnector(ip_BarcodeReader1);

            BarcodeReader1 = new DataManSystem(conn_BarcodeReader1);
            //subscribe to Cognex barcode reader "string arrived" event

            BarcodeReader1.ReadStringArrived += new ReadStringArrivedHandler(BarcodeReader1_ReadStringArrived);
            Tags = new List<Tag>();
            //open connection to barcode reader
            try
            {
                BarcodeReader1.Connect();
                BarcodeReader1.SetResultTypes(ResultTypes.ReadString);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + " ERROR - No Cognex barcode scanner found");
            }
            initialized = true;
            if (BarcodeReader1.State == ConnectionState.Connected)
            {
                //                            Console.WriteLine("TRIGGER OFF");
                //BarcodeReader1.SendCommand("SET CAMERA.EXPOSURE 162");
            }

        }

        public static bool IsInitialized()
        {
            return initialized;
        }
        public static bool GetTagAutoMode()
        {
            return TagAutoMode;
        }
        public static void SetTagAutoMode(bool val)
        {
            TagAutoMode = val;
        }
        public static void SetTestMode()
        {
            mode = "test";
            BarcodeReader1.SendCommand("TRIGGER ON");
        }

        public static void SetPrintMode()
        {
            mode = "print";
        }
        public static void init()
        {
            barcodes.Clear();
            edges.Clear();
        }
        private static void BarcodeReader1_ReadStringArrived(object sender, ReadStringArrivedEventArgs args)
        {
            BarcodeData = args.ReadString.ToString();
            //Console.WriteLine("ReadStringArrived:" + BarcodeData);
        }

        public static string Scan(int wait)
        {
            BarcodeData = null;
            int retry = 6;
            do
            {
                BarcodeReader1.SendCommand("TRIGGER ON");
                int x = wait * 10;
                while (BarcodeData == null)
                {
                    System.Threading.Thread.Sleep(10);
                    x--; if (x < 0) BarcodeData = "NO READ";
                }
                BarcodeReader1.SendCommand("TRIGGER OFF");
                retry--;
                if (BarcodeData == "NO READ")
                {
                    Console.WriteLine("Barcode Scanner: No Read");
                }
            }
            while ((BarcodeData == "NO READ") && (retry > 0));
            return BarcodeData;
        }

        public static bool handleBarcode(string aBarcode)
        {

            var index = JobSpoolSVC.getItemIndex("UPCA" + aBarcode, false);
            if (index >= 0)
            {
                //                Console.WriteLine("Handle Barcode Data: " + aBarcode + "  Index:" + index.ToString());
                Tag aTag = new Tag(aBarcode, 0, 0, 0);
                aTag.barcodeData = aBarcode;
                barcodes.Enqueue(aTag);
                Tags.Add(aTag);
                BarcodesRead++;
                return true;
            }
            else
            {
                return false;
            }
        }




        
    } 
}