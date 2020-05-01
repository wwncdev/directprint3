﻿using System;
//using SpeechLib;
using System.IO;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Timers;
using System.ComponentModel;
using Cognex.DataMan.SDK;
using EasyModbus;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using XIJET_PrintService;

namespace dpservice_composer
{

    class Program
    {
        public static int yoffset = 100;
        static void Main(string[] args)
        {
            // monitor for job file
            // read in job file
            bool test = false;

            JobSpoolSVC.Start();
            JobSpoolSVC.watchSpool();
            bool doneProcessing = false;
//            int yoffset = 100;
            int imageWidth = 640;
            int imageHeight = 192;
            int lastIndex=0;
            string lastBarcode= "UPCA08723321869";

//            JobSpoolSVC.ReadSettings();
            while (!doneProcessing)
            {
                char cmd;
                
                if (Console.KeyAvailable)
                {
                    System.ConsoleKeyInfo keyInfo = Console.ReadKey(true);
                    cmd = char.ToUpper(keyInfo.KeyChar);
                    switch (cmd)
                    {
                        case 'Q':
                            doneProcessing = true;
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
                            Printer.Flush();
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
                    BarcodeScanner.WatchForBarcode();  // always watch for barcodes or you tick off the machine
                }
                if (JobSpoolSVC.status == "needs_processing")
                {
                    // Probe printer and open for handle
                    if (!Printer.initialized)
                    {
                        if (Printer.Init())
                        {
                            Console.WriteLine("Printer Initialized");
                        }
                        else
                        {
                            Console.WriteLine("Printer Fail");
                            Environment.Exit(0);
                        }
                    }
                    System.Threading.Thread.Sleep(25);
                    if (BarcodeScanner.barcodes.Count > 0)
                    {
                        var barcode = BarcodeScanner.barcodes.Dequeue();
                        var index = JobSpoolSVC.getItemIndex("UPCA"+barcode.barcodeData,true);
                        lastIndex = index;
                        lastBarcode = "UPCA"+barcode.barcodeData;
                        if (index >= 0)
                        {
                            Printer.PrintBits(JobSpoolSVC.index2image[index],imageWidth,imageHeight,yoffset);
                        }
                        else
                        {
                            Printer.PrintBits(JobSpoolSVC.goatImage,imageWidth,imageHeight,yoffset);
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
                        index2image.Add(index, ConvertImage((string) item["label"]));

                        if (tagkeys.Count > 0) {
                            foreach (string tag in item["tagkeys"])
                            {
                                if (!tag2items.ContainsKey(tag)) tag2items[tag] = new List<int>();
                                tag2items[tag].Add(items.IndexOf(item));
                            }
                        }
                    }
                    //voice.Speak("Job for order number "+job["orderkey"].ToString()+" is ready for processing. Please turn on conveyor belt and load plant tags", SpeechVoiceSpeakFlags.SVSFlagsAsync);
                    Console.WriteLine("Index to Image Contains:" + index2image.Count.ToString());
                    status = "needs_processing";
                }
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

        public static int getItemIndex(string barcode,bool decrement=false)
        {
            // use barcode to find incomplete item in list
            if (tag2items.ContainsKey(barcode))
            {
                for(int bc = 0; bc < tag2items[barcode].Count;bc++)
                {
                    int index = tag2items[barcode][bc];
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
                                if (tag2items[barcode].Count == 0) tag2items.Remove(barcode);
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
        public static byte[] ConvertImage(string imageEncoded)
        {
            byte[] bmp = Convert.FromBase64String(imageEncoded);
            // Some Image and Data Metrics 
            int imgLen = bmp.Length;
            int o= bmp[10] + bmp[11] * 256;
            int imageWidth = bmp[18] + bmp[19] * 256;
            int imageHeight = bmp[22] + bmp[23] * 256;
            int colorPlanes = bmp[26];
            int bitsPerPixel = bmp[28];

            int numBytes = imageWidth * imageHeight;
            int bytesPerRow = imageWidth / 8;  // this must be evenly divisible by 4
            byte[] Bits = new byte[(imageWidth * imageHeight) / 8];

            for (int y = 0; y < imageHeight; y++)
            {
                for (int x = 0; x < imageWidth; x++)
                {
                    if ((x % 8) == 0) Bits[y * bytesPerRow + x / 8] = 0;  // initialize byte
                    byte bmpByte = bmp[o+y * imageWidth + x];
                    byte aBit = 0;
                    if (bmpByte != 1U)
                    {
                        aBit = 1;
                    }
                    Bits[(y * bytesPerRow) + (x / 8)] += (byte)(aBit << 7 - ((x % 8))); // added the 7- to test msb lsb issue
                }
            }
            return Bits;
        }
    }


    /**************************************************************
     * if edge detector says there is a tag then "pull the trigger" on the barcode scanner.
     * 
     */
    public static class BarcodeScanner
    {
        static string BarcodeData;
        static int BarcodesRead = 0;
        static ModbusClient modbusClient;
        static IPAddress ip_BarcodeReader1= IPAddress.Parse("192.168.16.46");
        static public DataManSystem BarcodeReader1;
        static bool SensorInitialized;
        static bool PreviousSensorState;
        static long edge1Time;
        static long edge2Time;
        static long readArrivedTime;

        static bool triggerIsOn = false;
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
                modbusClient = new ModbusClient("192.168.16.45", 502);
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
                    if (BarcodeData == null)
                    {
                        BarcodeData = args.ReadString.ToString();
                    }
                    break;
            }
            //System.Threading.Thread.Sleep(50);
        }
        public static void handleBarcode(string aBarcode){
            if (edges.Count > 0)
            {
                //    Tag aTag = new Tag(aBarcode, edge1Time, edge2Time, readArrivedTime);
                Tag aTag = edges.Dequeue();
                if ((readArrivedTime > aTag.edge1Time))
                {
                    aTag.readArrivedTime = readArrivedTime;
                    aTag.barcodeData = aBarcode;
                    barcodes.Enqueue(aTag);
                    Tags.Add(aTag);
                    BarcodesRead++;
                    Console.Write(BarcodesRead.ToString() + ": " + JobSpoolSVC.Aslen(aBarcode, 15));
                    int index = JobSpoolSVC.getItemIndex("UPCA" + aBarcode);
                    if (index >= 0)
                    {
                        JToken item = (JToken)JobSpoolSVC.job["incomplete_items"][index];
                        int howManyPrinted = (int)item["printqty"] - (int)item["notprintedqty"] + 1;
                        Console.Write(howManyPrinted.ToString("D3") + "  " + aTag.Elapsed().ToString("D5"));
                        if ((int)item["notprintedqty"] <= 1) Console.Write("  ** Completed  ");
                        Console.WriteLine();
                    }
                    else
                    {
                        Console.WriteLine(" Overage");
                    }
                    Log.WriteLine(aBarcode + ": queulen" + barcodes.Count.ToString());
                }
                else
                {
                    aTag.barcodeData = "No Barcode";
                    barcodes.Enqueue(aTag);
                    Console.WriteLine("Ignored out of sync barcode: " + edge1Time.ToString() +
                        ":" + edge2Time.ToString() + ":" + readArrivedTime.ToString() +
                        ":" + (edge2Time - edge1Time).ToString());
                }

            }
        }

        public static void WatchForBarcode() // you were missing a return type. I've added void for now
        {
            //a sensor state of true = over a tag
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
                    if ((!triggerIsOn) && (BarcodeReader1.State == ConnectionState.Connected))
                    {
                        BarcodeReader1.SendCommand("TRIGGER ON");
                      //  Console.WriteLine("Trigger ON");
                        triggerIsOn = true;
                        BarcodeData = null;
                    }
                }
                else if (SensorInitialized == true & CurrentSensorState == false)  // wait until tag has passed sensor?? -tw
                {
//                    System.Threading.Thread.Sleep(200);  // give a little time past bottom edge for barcode to be sent -tw

                    if (BarcodeReader1.State == ConnectionState.Connected)
                    {
                        BarcodeReader1.SendCommand("TRIGGER OFF");
                      //  Console.WriteLine("Trigger Off");
                        triggerIsOn = false;
                        // check to see if barcode in 200ms
                        edges.Enqueue(new Tag("", edge1Time, edge2Time, 0));
                        bcLagTimer.Enabled = true;
                        if (edges.Count > 1) Console.Write("*Too Many Edges?*");
                    }
                }
            }
      //      Console.Write("/");
        }
 
        private static void MonitorEdgeSensor_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                //ProgramStatus.Text = e.Error.Message;
                Console.WriteLine(e.Error.Message);
            }
        }


    }

}
