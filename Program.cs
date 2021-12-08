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
                            int index = JobSpoolSVC.getItemIndex("UPCA"+tagkey,false); 
                            JobSpoolSVC.AddOneMore("UPCA"+tagkey);
                            Printer.Flush(dblWidth);
                            BarcodeScanner.init();
                            break;
                        case 'T':
//                            BarcodeScanner.simulateTagIsWaiting = true;
//                            BarcodeScanner.triggerBeginTime = DateTime.Now.Ticks;
                            VacuumFeed.SetTagIsWaiting();
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
                            BarcodeScanner.SetTagAutoMode(!BarcodeScanner.GetTagAutoMode());
                            Console.WriteLine("TagAutoMode: "+BarcodeScanner.GetTagAutoMode().ToString());
                            break;
                        case 'V':
                            // vacuum feed advance
                            VacuumFeed.Continue();
                            Console.WriteLine("Vacuum Feed Advance");
                            break;
                        case 'A':
                            // vacuum feed advance
                            VacuumFeed.Abort();
                            Console.WriteLine("Vacuum Feed Abort");
                            break;
                        case 'Z':
                            VacuumFeed.setSmallTagMode(true);
                            Console.WriteLine("Small Tag Mode Enabled");
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
                    VacuumFeed.Start("192.168.8.45");
                    BarcodeScanner.Start();
                    if (test)
                    {
                        BarcodeScanner.handleBarcode("08723311272");
                        BarcodeScanner.handleBarcode("08723311272");
                    }
                }
                if (BarcodeScanner.mode == "print")
                {
                    BarcodeScanner.WatchTagIsReadyToBeScanned();  // always watch for barcodes or you tick off the machine
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
                                string  lineno =(string)item["lineno"];
                                string item_description = (string)item["item_description"];
                                item_description = item_description.PadRight(40).Remove(35);
                                Console.WriteLine("LineNo:"+lineno.PadLeft(5)+" "+item_description+" UPC:" + barcode.barcodeData + " " + 
                                    printnumber.ToString().PadLeft(5) + " of "+printqty.ToString());
                                Printer.PrintBits(JobSpoolSVC.index2image[index], imageWidth * (Program.dblWidth ? 2 : 1), imageHeight, yoffset);
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
            Console.WriteLine("I - Initialize Printer");
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
        const string jobfiledir = @"c:\\jobfiles\\";
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
            spooldir = jobfiledir + "spooled\\";
            processdir = jobfiledir + "processing\\";
            archivedir = jobfiledir + "archive\\";
            settingsFile = jobfiledir + "settings.json";


            // start by cleaning out the directory
            string[] fileEntries = Directory.GetFiles(spooldir);
            foreach (string fileName in fileEntries) File.Delete(fileName);

            aTimer = new Timer(200);
            aTimer.Elapsed += CheckSpoolDirEvent;
            aTimer.AutoReset = false;  // I think this is the reason this code was almost impossible to debug
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
                    string mode = (string)job["mode"];
                    if (mode == "MPL") VacuumFeed.setSmallTagMode(true);
                    else VacuumFeed.setSmallTagMode(false);
                    Console.WriteLine("Job Mode:" + mode);
                    // now add an lookup from tagkey to item
                    JArray items = (JArray)job["incomplete_items"];
                    tag2items.Clear();  // reuse this dictionary so clear it before building on current job.
                    index2image.Clear();
                    goatImage = ConvertImage((string)job["goat"]);

                    foreach (JToken item in items)   // run through all incomplete items
                    {
                        JArray tagkeys = (JArray)item["tagkeys"];  // possible tagkeys for each item
                        // Convert image to go from item to bitmap
                        int index = items.IndexOf(item);  
                        Console.Write(index.ToString()+",");
//                        Printer.displayHex((string)item["label"]);
                        index2image.Add(index, ConvertImage((string) item["label"]));
                        //Printer.displayBITS2(ConvertImage((string)item["label"]),192,640);
                        if (tagkeys.Count > 0) {   // the item has to have possible tagkeys
                            foreach (string tag in item["tagkeys"])
                            {
                                if (!tag2items.ContainsKey(tag)) tag2items[tag] = new List<int>();  // if no line item currently uses this tag
                                tag2items[tag].Add(items.IndexOf(item));   // add this line item 
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
            if (job != null)
            {

                JArray items = (JArray)job["incomplete_items"];
                foreach (JToken item in items)
                {
                    Console.Write(items.IndexOf(item).ToString().PadLeft(5));
                    Console.Write(": " + Aslen(item["item_description"].ToString(), 60));
                    Console.Write(item["printqty"].ToString().PadLeft(5));
                    Console.Write(((int)item["printqty"] - (int)item["notprintedqty"]).ToString().PadLeft(5));
                    Console.Write(item["notprintedqty"].ToString().PadLeft(5));
                    //    Console.Write("  " + Aslen(item["itemupc"].ToString(), 15));
                    Console.WriteLine();
                }
            }
            Console.WriteLine("VF TagWaitingStatus:" + VacuumFeed.TagIsWaitingForBarcodeScanner().ToString());
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
                                //JArray incomplete_items = (JArray)job["incomplete_items"];
                                //tag2items[barcode].Remove(index);
                                /* since we are now looping through and returning if qty >0 / no need to remote from tag2items
                                if (tag2items[tagkey].Count == 0) tag2items.Remove(tagkey);
                                */
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
                if (tag2items[lastBarcode].Count > 1)
                {
                    Console.Write("Size Code to Add One More (");
                    var choices = new List<char>();
                    for (int bc = 0; bc < tag2items[lastBarcode].Count; bc++)
                    {
                        int index = tag2items[lastBarcode][bc];
                        int sizeCode = Int32.Parse((string)job["incomplete_items"][index]["sizecode"]);
                        choices.Add(sizeCode.ToString()[0]);
                    }
                    Console.Write(String.Join(',', choices.ToArray())+"):");
                    System.ConsoleKeyInfo keyInfo = Console.ReadKey(true);
                    char choice='Q';
                    while (choices.IndexOf(choice) == -1)
                    {
                        choice = char.ToUpper(keyInfo.KeyChar);
                    }
                    Console.WriteLine(choice);
                    for (int bc = 0; bc < tag2items[lastBarcode].Count; bc++)
                    {
                        //Console.WriteLine("Added one to: "+lastBarcode);
                        int index = tag2items[lastBarcode][bc];
                        if (((string)job["incomplete_items"][index]["sizecode"])[0] == choice)
                        {
                            int qty = Int32.Parse((string)job["incomplete_items"][index]["notprintedqty"]);
                            int printQty = Int32.Parse((string)job["incomplete_items"][index]["printqty"]);
                            qty = qty + 1;
                            job["incomplete_items"][index]["notprintedqty"] = qty;
                            Console.WriteLine(qty + " of " + printQty);
                        }
                    }
                }
                else
                {
                    int index = tag2items[lastBarcode][0];
                    int qty = Int32.Parse((string)job["incomplete_items"][index]["notprintedqty"]);
                    int printQty = Int32.Parse((string)job["incomplete_items"][index]["printqty"]);
                    qty = qty + 1;
                    job["incomplete_items"][index]["notprintedqty"] = qty;
                    Console.WriteLine(qty + " of " + printQty);
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


    public static class VacuumFeed
    {
        const int TagIsWaitingForReadCoil = 0;  // bit 0 ?
        const int ContinueCoil = 1;
        const int AbortCoil = 2;
        const int SmallTagCoil = 3;  // false = medium tags, true = small tags defaults to false;

        static ModbusClient modBusClient;
        static bool sensorInitialized=false;
        static bool previousSensorState=false;
        static bool simulateTagIsWaiting = false;

        public static void Start(string vfIPAddress="192.168.8.45")
        {
            try
            {
                modBusClient = new ModbusClient(vfIPAddress, 502);
                //open connection to Protos X and get current state of sensor
                while (modBusClient.Connected) modBusClient.Disconnect();
                modBusClient.Connect();
                System.Threading.Thread.Sleep(500);
                if (modBusClient.Connected)
                {
                    bool[] readSensorInput = modBusClient.ReadDiscreteInputs(TagIsWaitingForReadCoil, 1);

                    previousSensorState = readSensorInput[0];

                    //sensor is initialized if there is no tag under the sensor when the timer is started
                    previousSensorState = !sensorInitialized;

                }
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR - Is everything turned on?\nVacuum feed not responding.");
                return;
            }
        }

        public static void setSmallTagMode(bool small = false)
        {
            modBusClient.WriteSingleCoil(SmallTagCoil, small);
        }
        public static void SetSimulateTagIsWaiting(bool waiting)
        {
            simulateTagIsWaiting = waiting;
        }
        public static void Continue()
        {
            modBusClient.WriteSingleCoil(ContinueCoil, true);
            //previousSensorState = false;
        }
        public static void Abort()
        {
            modBusClient.WriteSingleCoil(AbortCoil, true);
            //previousSensorState=false;
        }

        public static bool TagWaitingHasChanged()
        {
            if (modBusClient.Connected)
            {
                bool[] readSensorInput = modBusClient.ReadCoils(TagIsWaitingForReadCoil, 1);
                bool sensorState = readSensorInput[0] || simulateTagIsWaiting;
                if (sensorState != previousSensorState)
                {
//                    Console.WriteLine("Sensor State Changed from: " + previousSensorState.ToString() + " To: " + sensorState.ToString());
                    previousSensorState = sensorState;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                //Console.WriteLine("Vacuum Feeder Disconnected?");
                return false;
            }
        }

        public static void SetTagIsWaiting()
        {
            if (modBusClient.Connected)
            {
//                modBusClient.WriteSingleCoil(TagIsWaitingForReadCoil, true);
            }
            SetSimulateTagIsWaiting(true);
        }

        public static bool TagIsWaitingForBarcodeScanner()
        {
            if (modBusClient.Connected)
            {
                bool[] readSensorInput = modBusClient.ReadCoils(TagIsWaitingForReadCoil, 1);
                bool sensorState = (readSensorInput[0] || simulateTagIsWaiting);
//                SetSimulateTagIsWaiting(false);
                return sensorState;
            }
            else
            {
                //Console.WriteLine("Vacuum Feeder Disconnected?");
                return false;
            }
        }
    }


    /**************************************************************
     * if edge detector says there is a tag then "pull the trigger" on the barcode scanner.
     * 
     */
    public static class BarcodeScanner
    {
        public volatile static string BarcodeData="INITIAL BARCODE";
        static int BarcodesRead = 0;
        static IPAddress ip_BarcodeReader1= IPAddress.Parse("192.168.8.46");
        static public DataManSystem BarcodeReader1;

        public static Queue<Tag> barcodes;
        public static Queue<Tag> edges;
        static List<Tag> Tags;
        public static bool initialized = false;
       // public static Timer bcLagTimer = new System.Timers.Timer();
        public static bool allowGuessing = true;
        public static string lastBarcode;
        public static string mode = "print";  // or test

        static int BarcodeReaderTimeout = 3500;
        static bool TagAutoMode = true;

        public static long triggerBeginTime = 0;
        public static bool simulateTagIsWaiting = false;


        public static void Start()
        {

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
            }catch(Exception e)
            {
                Console.WriteLine("ERROR - No Cognex barcode scanner");
            }
            initialized = true;
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
            Console.WriteLine("Barcodes:" + barcodes.Count.ToString());
            Console.WriteLine("Edges:" + edges.Count.ToString());
            barcodes.Clear();
            edges.Clear();
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
            BarcodeData = args.ReadString.ToString();
            //Console.WriteLine("ReadStringArrived:" + BarcodeData);
        }

        public static bool handleBarcode(string aBarcode){

            var index = JobSpoolSVC.getItemIndex("UPCA" + aBarcode, false);
            if (index >= 0)
            {
                Console.WriteLine("Handle Barcode Data: " + aBarcode + "  Index:" + index.ToString());
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


 

        
        public static void WatchTagIsReadyToBeScanned()
        {
            //get the current state of the 'Tag Raedy' flag from the vacuum feeder's PLC
            if (!VacuumFeed.TagWaitingHasChanged())
            //no change in state of 'Tag Ready' flag; when current and previous states are both false, we don't need to do anything
            {
                if (VacuumFeed.TagIsWaitingForBarcodeScanner())
                /*even though the state of the flag did not change, it was previously true and is still true
                this means that one timer interval passed while the tag was waiting to be read
                we add an interval amount to the counter in order to determine when the timeout has been reached */
                {
                    //TimeoutCounter += timerInterval;  // rewrite as timeElapsed in ms
                    long timeElapsed = (triggerBeginTime>0)?(System.DateTime.Now.Ticks - triggerBeginTime) / 10000:0; // tickspermilliseconds = 10,000?
                    if (timeElapsed >= BarcodeReaderTimeout || BarcodeData != null)  // changed == to >=
                    {
                        //Console.WriteLine("End Waiting For Barcode:" + BarcodeData);
                        //modbusClient.WriteSingleCoil(0, false);
                        triggerBeginTime = 0;
                        if (BarcodeReader1.State == ConnectionState.Connected)
                        {
//                            Console.WriteLine("TRIGGER OFF");
                            BarcodeReader1.SendCommand("TRIGGER OFF");
                        }
                        if (BarcodeData != null)
                        {
                            //the barcode was successfully read
                            //perform 'good tag' business logic and send print data to inkjet controller

                            //set the 'Continue' flag on the vacuum feeder's PLC

                            //BarcodeNumber.Content = BarcodeData;
                            //handleBarcode(BarcodeData);  oops - already handled in readstringarrived.
                            //Console.WriteLine(BarcodeData);

                            Console.WriteLine("timeElapsed:" + timeElapsed.ToString());
                            if (handleBarcode(BarcodeData))
                            {
                                if (TagAutoMode) 
                                VacuumFeed.Continue();
                            }
                            else
                            {
                                Console.WriteLine("TAG NOT FOUND IN ORDER OR TOO MANY\nPress V to advance");
                            }
                            BarcodeData = null;  // mission accomplished
                        }
                        else
                        {
                            //the timeout for reading a barcode has been reached and there is still no value returned by the reader
                            //perform desired 'bad tag' actions

                            //set the 'Abort' flag on the vacuum feeder's PLC
                            //if (TagAutoMode)
                            //    VacuumFeed.Abort();

                            // BarcodeNumber.Content = "No Read";
                            simulateTagIsWaiting = false;
                            Console.WriteLine("No Read:"+timeElapsed.ToString());
                           // currentTagIsWaiting = false;
                        }
                    }
                }
            }
            else
            //state of 'Tag Ready' flag has changed; when flag state changes from true to false, we don't need to do anything
            {
                //Console.WriteLine("Tag Waiting has Changed");
                if (VacuumFeed.TagIsWaitingForBarcodeScanner()) 
                //flag was previously false and changed to true; a tag is in position and ready to be read; turn on the reader
                {
                    BarcodeData = null;
                    if (BarcodeReader1.State == ConnectionState.Connected)
                    {
//                        Console.WriteLine("TRIGGER ON");
                        BarcodeReader1.SendCommand("TRIGGER ON");
                    }
                    triggerBeginTime = System.DateTime.Now.Ticks;
                    //Console.WriteLine("Begin Waiting for Barcode");
                    //BarcodeNumber.Content = String.Empty;
                }
            }
        }
        
    }

}
