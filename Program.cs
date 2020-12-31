using System;
//using SpeechLib;
using System.IO;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Timers;
using System.ComponentModel;
using Cognex.DataMan.SDK;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using EasyModbus;
using XIJET_PrintService;

namespace dp_printer_prod
{

    class Program
    {
        public static int yoffset = 0;
        public static bool dblWidth = false;

        static void Main(string[] args)
        {
            // monitor for job file
            // read in job file
            bool test = false;

            JobSpoolSVC.Start();
            JobSpoolSVC.watchSpool();
            bool doneProcessing = false;
//            int yoffset = 100;
            int imageWidth = 192;
            int imageHeight = 640;
            int lastIndex=0;
            string tagkey;

            string lastBarcode= "UPCA08723321869";

            foreach(string arg in args)
            {
                Console.WriteLine(arg);
                switch (arg.Split("=")[0])
                {
                    case "imageWidth":
                        int val = int.Parse(arg.Split("=")[1]);
                        imageWidth = val;
                        break;
                    case "dblWidth":
                        bool bval = bool.Parse(arg.Split("=")[1]);
                        dblWidth = bval;
                        break;
                    default:
                        Console.WriteLine("Error above argument not recognized");
                        break;
                }
            }

//            JobSpoolSVC.ReadSettings();
            while (!doneProcessing)
            {
                char cmd;
                string preamble = "";
//                Console.Write(".");                
                if (Console.KeyAvailable)
                {
                    System.ConsoleKeyInfo keyInfo = Console.ReadKey(true);
                    cmd = char.ToUpper(keyInfo.KeyChar);
                    preamble = "";
                    switch (cmd)
                    {
                        case 'Q':
                            doneProcessing = true;
                            break;
                        case '8': // eiiiiiii syringa? 
                        case '0': // probably the beginning of a barcode
                            if (cmd == '0') preamble = "0";
                            if (cmd == '8') preamble = "08";

                            tagkey = Console.ReadLine();
                            tagkey = preamble + tagkey;
                            Console.Write("Tagkey Set to: ");Console.WriteLine(tagkey);
                            int index = JobSpoolSVC.getItemIndex("UPCA"+tagkey,false);
                            if (index > 0)
                            {
                                JToken item = JobSpoolSVC.job["incomplete_items"][index];
                                int qty = Int32.Parse((string)item["notprintedqty"]);
                                Console.Write("I need "); Console.WriteLine(qty);
                                BarcodeScanner.handleBarcode(tagkey);
                            }
                            else
                            {
                                Console.WriteLine("Tagkey " + tagkey + " Not in this order");
                            }
                            break;
                        case 'T':
                            if (BarcodeScanner.mode == "print")
                            {
                                BarcodeScanner.SetTestMode();
                                Console.WriteLine("Now in Test Mode");
                            }
                            else
                            {
                                BarcodeScanner.SetPrintMode();
                                Console.WriteLine("Now in Print Mode");
                            }

                            break;
                        case 'L':
                            if (BarcodeScanner.initialized)
                            {
                                BarcodeScanner.DisplayLog();
                            };
                            break;
                        case 'E':
                            // end processing current job
                            JobSpoolSVC.status = "Starting";
                            Console.WriteLine("Processing of Job ended.");
                            break;
                        case 'F':
                            // first in group - do not guess
                            BarcodeScanner.lastBarcode = null;
                            Console.WriteLine("No more guessing for awhile");
                            break;
                        case 'G':
                            string goat = (string)JobSpoolSVC.job["goat"];
                            Console.WriteLine("Goat");
                            Printer.displayHex(goat);
                            Console.WriteLine();
                            break;
                        case 'S':
                            Console.WriteLine("Display Current Status:");
                            JobSpoolSVC.DisplayStatus();
                            Console.WriteLine("yoffset: " + yoffset.ToString());
                            break;
                        case 'W':
                            JobSpoolSVC.SaveJob();
                            break;
                        case 'P':
                            Printer.DisplayParams();
                            break;
                        case '-':
                            yoffset -= 5;
                            if (yoffset < 0) yoffset = 0;
                            Console.WriteLine("yoffset: " + yoffset.ToString());
                            JobSpoolSVC.settings["yoffset"] = yoffset;
                            JobSpoolSVC.SaveSettings();
  //                          System.Threading.Thread.Sleep(500);
                            break;
                        case '+':
                        case '=':
                            yoffset += 5;
                            Console.WriteLine("yoffset: " + yoffset.ToString());
                            JobSpoolSVC.settings["offset"] = yoffset;
                            JobSpoolSVC.SaveSettings();
  //                          System.Threading.Thread.Sleep(500);
                            break;
                        case 'I':
                            Printer.Flush(dblWidth);
                            BarcodeScanner.init();
                            break;
                        case 'M':
                            Printer.Flush(dblWidth, 5500);
                            BarcodeScanner.init();
                            break;
                        case 'A':
                            JobSpoolSVC.AddOneMore(lastBarcode);
                            break;
                        default:
                            displayHelp();
                            break;
                    }
                }
                // if this isn't very performant or takes a high percent of CPU then add some sleep time
                // to allow thread to go process other things.
                if (!BarcodeScanner.initialized)
                {
                    BarcodeScanner.Start();
                    if (test)
                    {
                        BarcodeScanner.handleBarcode("08723311272");
                        BarcodeScanner.handleBarcode("08723311272");
                    }
                }
                if (BarcodeScanner.mode == "print")
                {
                    BarcodeScanner.WatchTagEdge();  // always watch for barcodes or you tick off the machine
                }
                if (JobSpoolSVC.status == "needs_processing")
                {
                    // Probe printer and open for handle
                    if (!Printer.initialized)
                    {
                        if (Printer.Init(Program.dblWidth))
                        {
                            Console.WriteLine("Printer Initialized");
                        }
                        else
                        {
                            Console.WriteLine("Printer Fail");
                            Environment.Exit(0);
                        }
                    }
                    System.Threading.Thread.Sleep(10);
                    if (BarcodeScanner.barcodes.Count > 0)
                    {
                        if (Printer.inPrintBits == 0)
                        {
                            var barcode = BarcodeScanner.barcodes.Dequeue();
                            var index = JobSpoolSVC.getItemIndex("UPCA" + barcode.barcodeData, true);
                            lastIndex = index;
                            lastBarcode = "UPCA" + barcode.barcodeData;
                            //Console.Write("Index: " + index.ToString());
                            //Console.WriteLine("Printing: " + lastBarcode);
                            if (index >= 0)
                            {
                                JToken item = JobSpoolSVC.job["incomplete_items"][index];
                                int qty = Int32.Parse((string)item["notprintedqty"]);
                                int printqty = Int32.Parse((string)item["printqty"]);
                                int printnumber = printqty - qty;
                                int lineno = Int32.Parse((string)item["lineno"]);
                                string item_description = (string)item["item_description"];
                                item_description = item_description.PadRight(40).Remove(35);
                                Console.WriteLine("LineNo:"+lineno.ToString().PadLeft(5)+" "+item_description+" UPC:" + barcode.barcodeData + " " + 
                                    printnumber.ToString().PadLeft(5) + " of "+printqty.ToString());
                                //Printer.PrintBits(JobSpoolSVC.index2image[index], imageWidth * (Program.dblWidth ? 2 : 1), imageHeight, yoffset);
                            }
                            else
                            {
                                 //Printer.PrintBits(JobSpoolSVC.goatImage, imageWidth * (Program.dblWidth ? 2 : 1), imageHeight, yoffset);
                                Console.WriteLine("Goat PRINTED");
                            }
                        }
                    }
                }
                else
                {
                    System.Threading.Thread.Sleep(50);
                    JobSpoolSVC.watchSpool();
                }
            }
            // key press save and exit

           // BarcodeScanner.DisplayLog();
            Printer.Close();
            JobSpoolSVC.SaveJob();
        }

        static void  displayHelp()
        {
            Console.WriteLine("");
            Console.WriteLine("Q - Quit");
            Console.WriteLine("L - Log");
            Console.WriteLine("S - Status");
            Console.WriteLine("");

        }


    }


    public class Tag
    {
        public string barcodeData;
        public string description;
        public long edge1Time;
        public  long edge2Time;
        public long readArrivedTime;
        // add the time that readstring arrived was called.

        public Tag(string barcodeData, long edge1Time, long edge2Time,long readArrivedTime)
        {
            this.barcodeData = barcodeData;
            this.edge1Time = edge1Time;
            this.edge2Time = edge2Time;
            this.readArrivedTime = readArrivedTime;
        }

        public long Elapsed()
        {
            return (edge2Time - edge1Time);
        }
    }

    static class Log
    {
        private static StreamWriter w = File.AppendText("log.txt");

        public static void WriteLine(string msg)
        {
            w.AutoFlush = true;
            w.Write(DateTime.Now.ToLongTimeString());
            w.WriteLine(": "+msg);
            //Console.Write(DateTime.Now.ToLongTimeString());
            //Console.WriteLine(": " + msg);
        }

    }

    static class JobSpoolSVC
    {
        static Timer aTimer;
        public static string status = "starting";
        const string jobfiledir = @"c:/jobfiles/";
        static String spooldir;
        static String processdir;
        static String archivedir;
        static String settingsFile;
        public static JToken job;
        public static JObject settings = new JObject();
        //public static SpVoice voice;
        public static String LastFileName;
        public static int lastIndex = -1;

        //static DPsys dpsys;
        public static Dictionary<string, List<int>> tag2items = new Dictionary<string, List<int>>();  // needs cleaned up at end of job?
        public static Dictionary<int,byte[]> index2image = new Dictionary<int,byte[]>();
        public static byte[] goatImage;

        public static int imageWidth { get; private set; }

        public static void Start()
        {
            spooldir = jobfiledir + "spooled/";
            processdir = jobfiledir + "processing/";
            archivedir = jobfiledir + "archive/";
            settingsFile = jobfiledir + "settings.json";

            uint flag = 0;

            // start by cleaning out the directory
            string[] fileEntries = Directory.GetFiles(spooldir);
            foreach (string fileName in fileEntries) File.Delete(fileName);

            aTimer = new Timer(200);
            aTimer.Elapsed += CheckSpoolDirEvent;
            aTimer.AutoReset = true;
            //            aTimer.Enabled = true;
            status = "started";
        }

        public static void watchSpool()
        {
            aTimer.Enabled = true;
        }

        public static void SaveSettings()
        {
            File.WriteAllText(settingsFile, JsonConvert.SerializeObject(settings));
            using (StreamWriter file = File.CreateText(settingsFile))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, settings);
            }
        }

        public static void ReadSettings()
        {
            using (StreamReader r = new StreamReader(settingsFile))
            {
                var json = r.ReadToEnd();
                JToken settings = JToken.Parse(json);
                Program.yoffset = (int)settings["offset"];
            }
        }
        public static void SaveJob()
        {
            File.WriteAllText(archivedir+LastFileName, JsonConvert.SerializeObject(job));

            // serialize JSON directly to a file
            using (StreamWriter file = File.CreateText(archivedir+LastFileName))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, job);
            }
        }
        private static void CheckSpoolDirEvent(Object source, ElapsedEventArgs e)
        {
            string[] fileEntries = Directory.GetFiles(spooldir, @"*.json");
            if (fileEntries.Length > 0)
            {
                aTimer.Enabled = false;

                // get rid of old processing file if it already exists
                if (File.Exists(processdir + Path.GetFileName(fileEntries[0]))) File.Delete(processdir + Path.GetFileName(fileEntries[0]));

                // move file from spooled to processing
                LastFileName = Path.GetFileName(fileEntries[0]);
                File.Move(spooldir + LastFileName, processdir + LastFileName);
                using (StreamReader r = new StreamReader(processdir + LastFileName))
                {
                    var json = r.ReadToEnd();
                    job = JToken.Parse(json);
                    
                    // now add an lookup from tagkey to item
                    JArray items = (JArray)job["incomplete_items"];
                    tag2items.Clear();  // reuse this dictionary so clear it before building on current job.
                    index2image.Clear();
                    goatImage = ConvertImage((string)job["goat"]);

                    foreach (JToken item in items)
                    {
                        JArray tagkeys = (JArray)item["tagkeys"];
                        // Convert image to go from item to bitmap
                        int index = items.IndexOf(item);
                        Console.Write(index.ToString()+",");
//                        Printer.displayHex((string)item["label"]);
                        index2image.Add(index, ConvertImage((string) item["label"]));
                        //Printer.displayBITS2(ConvertImage((string)item["label"]),192,640);
                        if (tagkeys.Count > 0) {
                            foreach (string tag in item["tagkeys"])
                            {
                                if (!tag2items.ContainsKey(tag)) tag2items[tag] = new List<int>();
                                tag2items[tag].Add(items.IndexOf(item));
                            }
                        }
                    }
                    var index2 = JobSpoolSVC.getItemIndex("UPCA08723305937", true);

               //     Printer.displayBITS2(index2image[index2], 640 * (Program.dblWidth ? 2 : 1),192);
                    //voice.Speak("Job for order number "+job["orderkey"].ToString()+" is ready for processing. Please turn on conveyor belt and load plant tags", SpeechVoiceSpeakFlags.SVSFlagsAsync);
                    Console.WriteLine("Index to Image Contains:" + index2image.Count.ToString());
                    status = "needs_processing";
                }
                Console.WriteLine();
            }
        }

        public static string Aslen(string str,int l)
        {
            if (str == null) return "";
            if ((str!=null) && (str.Length > l)) str = str.Substring(0, l);
            else str = str.PadRight(l);
            return str;
        }
        public static void DisplayStatus()
        {
            JArray items = (JArray)job["incomplete_items"];
            foreach(JToken item in items)
            {
                Console.Write(items.IndexOf(item).ToString().PadLeft(5));
                Console.Write(": "+Aslen(item["item_description"].ToString(),60));
                Console.Write(item["printqty"].ToString().PadLeft(5));
                Console.Write(((int)item["printqty"] - (int)item["notprintedqty"]).ToString().PadLeft(5));
                Console.Write(item["notprintedqty"].ToString().PadLeft(5));
            //    Console.Write("  " + Aslen(item["itemupc"].ToString(), 15));
                Console.WriteLine();
            }
        }

        public static int getItemIndex(string tagkey,bool decrement=false)
        {
            // use barcode to find incomplete item in list
            if (tag2items.ContainsKey(tagkey))
            {
                for(int bc = 0; bc < tag2items[tagkey].Count;bc++)
                {
                    int index = tag2items[tagkey][bc];
                    int qty = Int32.Parse((string)job["incomplete_items"][index]["notprintedqty"]);
                    if (qty > 0)
                    {
                        if (decrement)
                        {
                            qty--;
                            job["incomplete_items"][index]["notprintedqty"] = qty;
                            if (qty <= 0)
                            {
                                JArray completed_items = (JArray)job["completed_items"];
                                completed_items.Add(job["incomplete_items"][index]);
                                JArray incomplete_items = (JArray)job["incomplete_items"];
                                //tag2items[barcode].Remove(index);
                                if (tag2items[tagkey].Count == 0) tag2items.Remove(tagkey);
                            }
                        }
                        return index;
                    }
                }
                return -1;  // we have the barcode but no qty left to print
            }
            else
            {
           //     Console.WriteLine(barcode + " no qty left to print");
                return -1;
            }
        }

        public static void AddOneMore(string lastBarcode)
        {
            if (tag2items.ContainsKey(lastBarcode))
            {
                for (int bc = 0; bc < tag2items[lastBarcode].Count; bc++)
                {
                    Console.WriteLine("Added one to: "+lastBarcode);
                    int index = tag2items[lastBarcode][bc];
                    int qty = Int32.Parse((string)job["incomplete_items"][index]["notprintedqty"]);
                    qty = qty + 1;
                    job["incomplete_items"][index]["notprintedqty"] = qty;
                }
            }
            else
            {
                Console.WriteLine("Barcode " + lastBarcode + " not contained in order");
            }
        }


        public static byte[] DoubleWidth(byte[] image)
        {
            byte[] Bits = new byte[image.Length * 2];
            for (int x = 0; x < image.Length; x++)
            {
                byte v = image[x];
                int r = 0;
                for(int b = 0; b < 8; b++)  // loop through 8 bits
                {
                    if ((v&(1<< b))!=0)
                    {
                        r += (3 << (b * 2));
                    }                    
                }
                Bits[(x * 2)+1] = (byte) (r%256);
                Bits[(x * 2)]   = (byte)(r / 256);
            }
            return Bits;
        }

        public static byte[] ConvertImage(string imageEncoded)
        {
            byte[] bmp = Convert.FromBase64String(imageEncoded);
            // Some Image and Data Metrics 
            int imgLen = bmp.Length;
            int o= bmp[10] + bmp[11] * 256;
            int imageWidth = bmp[18] + bmp[19] * 256;
            int imageHeight = bmp[22] + bmp[23] * 256;
            Console.WriteLine("Width:"+imageWidth.ToString()+"  Height:"+imageHeight.ToString());
            int colorPlanes = bmp[26];
            int bitsPerPixel = bmp[28];
            Console.WriteLine("ColorPlanes:" + colorPlanes.ToString() + "  bitsPerPixel:" + bitsPerPixel.ToString());

            int numBytes = imageWidth * imageHeight;
            int bytesPerRow = imageWidth / 8;  // this must be evenly divisible by 4 *** shit!
            byte[] Bits = new byte[(imageWidth * imageHeight) / 8];
            byte whitey = bmp[o];  // we'll just agree never to write to 0,0

            for (int y = 0; y < imageHeight; y++)
            {
                for (int x = 0; x < imageWidth; x++)
                {
                    if ((x % 8) == 0) Bits[y * bytesPerRow + x / 8] = 0;  // initialize byte
                    byte bmpByte = bmp[o+y * imageWidth + x];
                    byte aBit = (bmpByte != whitey) ?(byte)1:(byte)0;
                    Bits[(y * bytesPerRow) + (x / 8)] += (byte)(aBit << 7 - ((x % 8))); // added the 7- to test msb lsb issue
                }
            }
            if (Program.dblWidth)
            {
                return DoubleWidth(Bits);
            }
            else
            {
                return Bits;
            }
        }
    }


    /**************************************************************
     * if edge detector says there is a tag then "pull the trigger" on the barcode scanner.
     * 
     */
    public static class BarcodeScanner
    {
        volatile static string BarcodeData="INITIAL BARCODE";
        static int BarcodesRead = 0;
        static ModbusClient modbusClient;
        static IPAddress ip_BarcodeReader1= IPAddress.Parse("192.168.8.46");
        static public DataManSystem BarcodeReader1;
        static bool SensorInitialized;
        static bool PreviousSensorState;
        static long edge1Time;
        static long edge2Time;
        static long readArrivedTime;

        static bool triggerIsOn = true;
        public static Queue<Tag> barcodes;
        public static Queue<Tag> edges;
        static List<Tag> Tags;
        public static bool initialized = false;
        public static Timer bcLagTimer = new System.Timers.Timer();
        public static bool allowGuessing = true;
        public static string lastBarcode;
        public static string mode = "print";  // or test


        public static void Start()
        {
            bcLagTimer.Interval = 50;
            bcLagTimer.Elapsed += SeeIfThereIsABarcode;
            bcLagTimer.AutoReset = false;
            bcLagTimer.Enabled = false;

            barcodes = new Queue<Tag>();
            edges = new Queue<Tag>();
            
            EthSystemConnector conn_BarcodeReader1 = new EthSystemConnector(ip_BarcodeReader1);

            BarcodeReader1 = new DataManSystem(conn_BarcodeReader1);
            //subscribe to Cognex barcode reader "string arrived" event

            BarcodeReader1.ReadStringArrived += new ReadStringArrivedHandler(BarcodeReader1_ReadStringArrived);
         //   BarcodeReader1.ReadStringArrived -= new ReadStringArrivedHandler(BarcodeReader1_ReadStringArrived);
            //create Modbus object to monitor edge detection sensor on the Protos X I/O board
            try
            {
                modbusClient = new ModbusClient("192.168.8.45", 502);
                //open connection to Protos X and get current state of sensor
                while (modbusClient.Connected) modbusClient.Disconnect();
                modbusClient.Connect();
                System.Threading.Thread.Sleep(500);
                if (modbusClient.Connected)
                {
                    bool[] readSensorInput = modbusClient.ReadDiscreteInputs(0, 1);
                    //           modbusClient.Disconnect();

                    PreviousSensorState = readSensorInput[0];

                    //sensor is initialized if there is no tag under the sensor when the timer is started
                    PreviousSensorState = (!SensorInitialized);

                }
            }catch(Exception e)
            {
                Console.WriteLine("ERROR - Is everything turned on?");
                return;
            }
            Tags = new List<Tag>();
            //open connection to barcode reader
            BarcodeReader1.Connect();
            BarcodeReader1.SetResultTypes(ResultTypes.ReadString);
            initialized = true;
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
            Console.WriteLine("Barcodes:" + barcodes.Count.ToString());
            Console.WriteLine("Edges:" + edges.Count.ToString());
            Console.WriteLine("Previous Sensor State:" + PreviousSensorState.ToString());
            barcodes.Clear();
            edges.Clear();
            PreviousSensorState = false;
        }
        private static void SeeIfThereIsABarcode(Object source, System.Timers.ElapsedEventArgs e)
        {
            if (mode == "test") return;  // I hate short circuits
            int x = 60;
            while ((x-- > 0) && (BarcodeData == null)) System.Threading.Thread.Sleep(10);
            if (x<59) Console.WriteLine("xtra-time: " + x.ToString());
            //System.Threading.Thread.Sleep(100);
            if (BarcodeData != null)
            {
                handleBarcode(BarcodeData);
                lastBarcode = BarcodeData;
            }
            else
            {
                if (allowGuessing && lastBarcode != null)
                {
                    Console.WriteLine("*******  Took a Guess *******");
                    handleBarcode(lastBarcode);
                }
                else
                {
                    handleBarcode("No Barcode");
                }
            }
        }
        public static void DisplayLog()
        {
            string o;
            o = "barcode,edge1Time,edg2Time,readarrived,e2-e1";
            Log.WriteLine(o);
            foreach (Tag t in Tags.ToArray())
            {
                o = t.barcodeData + ","
                    + t.edge1Time.ToString() + ","
                    + t.edge2Time.ToString() + ","
                    + t.readArrivedTime.ToString() + ","
                    + t.Elapsed().ToString("D4");
                Log.WriteLine(o);
                var index = JobSpoolSVC.getItemIndex("UPCA" + t.barcodeData);
            
                if (index >= 0)
                {
                    JToken item = JobSpoolSVC.job["incomplete_items"][index];
                    Console.WriteLine(item["item_description"].ToString().PadRight(20) + " " + 
                        t.barcodeData.PadRight(15) + " : " + 
                        t.Elapsed().ToString("D10").PadLeft(10));
                }
                else
                {
                    Console.WriteLine("Goat".PadRight(20) + " " + t.barcodeData.PadRight(15) + " : " + t.Elapsed().ToString().PadLeft(15));
                }
            }
        }
        private static void BarcodeReader1_ReadStringArrived(object sender, ReadStringArrivedEventArgs args)
        {
            //Console.Write((DateTime.Now.Ticks/10000).ToString()+":ReadStringArrived: ");
            switch (mode)
            {
                case "test":
                    Console.WriteLine(args.ReadString.ToString());
                    BarcodeReader1.SendCommand("TRIGGER ON");
                    break;
                case "print":
                    readArrivedTime = DateTime.Now.Ticks / 10000;
                    int ResultId = args.ResultId;
                  //  Console.WriteLine("ResultId: " + ResultId.ToString());
                    BarcodeData = args.ReadString.ToString();
                    handleBarcode(BarcodeData);
                    break;
            }
            //System.Threading.Thread.Sleep(50);
        }
        public static void handleBarcode(string aBarcode){

            Tag aTag = new Tag(aBarcode,0,0,0);
            aTag.barcodeData = aBarcode;
            barcodes.Enqueue(aTag);
            Tags.Add(aTag);
            BarcodesRead++;
            return;
        }

        private static void TriggerOff(Object source, ElapsedEventArgs e)
        {
            return;
            DmccResponse response = BarcodeReader1.SendCommand("TRIGGER OFF");
            //  Console.WriteLine("Trigger Off");
            triggerIsOn = false;
            // check to see if barcode in 200ms
            edges.Enqueue(new Tag("", edge1Time, edge2Time, 0));
            bcLagTimer.Enabled = true;
            if (edges.Count > 1) Console.Write("*Too Many Edges?*");
        }

        public static void WatchTagEdge() // you were missing a return type. I've added void for now
        {
            Timer bcTimer;
            bool CurrentSensorState=false;
            bool[] readSensorInput;
            try
            {
                //Console.Write(".");
                if (!modbusClient.Connected) modbusClient.Connect();
                readSensorInput = modbusClient.ReadDiscreteInputs(0, 1);
                CurrentSensorState = readSensorInput[0];
            }
            catch (Exception e)
            {
                Console.WriteLine("{0} Problem reading modbus sensor.", e);
                modbusClient = new ModbusClient("192.168.16.45", 502);
                //open connection to Protos X and get current state of sensor
                while (modbusClient.Connected) modbusClient.Disconnect();
                modbusClient.Connect();
                readSensorInput = modbusClient.ReadDiscreteInputs(0, 1);
                CurrentSensorState = readSensorInput[0];

            }

            //do nothing if the sensor state has not changed
            if (CurrentSensorState != PreviousSensorState)
            {
                if (CurrentSensorState) Console.Write("+");
                else Console.Write("-");

                if (CurrentSensorState) edge1Time = DateTime.Now.Ticks/10000;
                else edge2Time = DateTime.Now.Ticks/10000;
          //      if (!CurrentSensorState) Console.Write(" - " + (edge2Time - edge1Time).ToString() + " - ");
                PreviousSensorState = CurrentSensorState;

                //if the sensor was not initialized when the timer was started (i.e., the sensor was over a tag when the timer was started)
                //then the sensor will be initialized the first time it is over a gap
                if (SensorInitialized == false & CurrentSensorState == false)
                {
                    SensorInitialized = true;
                }
                else if (SensorInitialized == true & CurrentSensorState == true)  // over a tag
                {
                    if (BarcodeData == null) {
                        handleBarcode("BAD DATA");
                        Console.Write("BAD DATA"); 
                    }
                    BarcodeData = null;
                    if ((!triggerIsOn) && (BarcodeReader1.State == ConnectionState.Connected))
                    {
                        Console.WriteLine("This might never be called?");
                        DmccResponse response = BarcodeReader1.SendCommand("TRIGGER ON");
                        //  Console.WriteLine("Trigger ON");
                        triggerIsOn = true;
                    }
                }
                else if (SensorInitialized == true & CurrentSensorState == false)  // wait until tag has passed sensor?? -tw
                {

                }
            }
      //      Console.Write("/");
        }

    }

}
