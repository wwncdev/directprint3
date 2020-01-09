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
using EasyModbus;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using XIJET_PrintService;

namespace dpservice_composer
{
    class Program
    {
        static void Main(string[] args)
        {
            // monitor for job file
            // read in job file
            bool test = false;

            JobSpoolSVC.Start();
            JobSpoolSVC.watchSpool();
            while (!(Console.KeyAvailable))
            {
                // if this isn't very performant or takes a high percent of CPU then add some sleep time
                // to allow thread to go process other things.
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
                    System.Threading.Thread.Sleep(10);
                    if (!BarcodeScanner.initialized)
                    {
                        BarcodeScanner.Start();
                        if (test){
                           BarcodeScanner.handleBarcode("08723311272");
                           BarcodeScanner.handleBarcode("08723311272");
                        }
                    }
                    BarcodeScanner.WatchForBarcode();
                    if (BarcodeScanner.barcodes.Count > 0)
                    {
                        var barcode = BarcodeScanner.barcodes.Dequeue();
                        var index = JobSpoolSVC.getItemIndex("UPCA"+barcode.barcodeData);
                        if (index >= 0)
                        {
                            JToken item = JobSpoolSVC.job["incomplete_items"][index];
                            Printer.Print((string)item["label"]);
                            Console.WriteLine("After Print");
                        }
                        else
                        {
                            // print nothing  **** To Be Completed
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

            //BarcodeScanner.DisplayLog();
            Printer.Close();
            JobSpoolSVC.SaveJob();
        }

    }

    public class Tag
    {
        public string barcodeData;
        private long edge1Time;
        private long edge2Time;
        

        public Tag(string barcodeData, long edge1Time, long edge2Time)
        {
            this.barcodeData = barcodeData;
            this.edge1Time = edge1Time;
            this.edge2Time = edge2Time;
        }

        public long Elapsed()
        {
            return (edge2Time - edge2Time);
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
        public static JToken job;
        //public static SpVoice voice;
        public static String LastFileName;

        //static DPsys dpsys;
        public static Dictionary<string, List<int>> tag2items = new Dictionary<string, List<int>>();
        public static void Start()
        {
            spooldir = jobfiledir + "spooled/";
            processdir = jobfiledir + "processing/";
            archivedir = jobfiledir + "archive/";

            //voice = new SpVoice();
            //voice.Volume = 100;

//            voice.Speak("Welcome to the direct print project", SpeechVoiceSpeakFlags.SVSFlagsAsync);



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
                    foreach (JToken item in items)
                    {
                        foreach (string tag in item["tagkeys"])
                        {
                            if (!tag2items.ContainsKey(tag)) tag2items[tag] = new List<int>();
                            tag2items[tag].Add(items.IndexOf(item));
                        }
                    }
                    //voice.Speak("Job for order number "+job["orderkey"].ToString()+" is ready for processing. Please turn on conveyor belt and load plant tags", SpeechVoiceSpeakFlags.SVSFlagsAsync);
                    status = "needs_processing";
                }
            }
        }

        public static int getItemIndex(string barcode)
        {
            // use barcode to find incomplete item in list
            if (tag2items.ContainsKey(barcode))
            {
                var index = tag2items[barcode][0];
                Console.WriteLine(job["incomplete_items"][index]["item_description"]);
                int qty = Int32.Parse((string)job["incomplete_items"][index]["notprintedqty"]);
                qty--;
                job["incomplete_items"][index]["notprintedqty"] = qty;
                if (qty <= 0)
                {
                    JArray completed_items = (JArray)job["completed_items"];
                    completed_items.Add(job["incomplete_items"][index]);
                    JArray incomplete_items = (JArray)job["incomplete_items"];
                    tag2items[barcode].Remove(index);
                    if (tag2items[barcode].Count == 0) tag2items.Remove(barcode);
                    //                    incomplete_items[index].Remove();
                    Console.WriteLine("Line Item " + index.ToString() + " completed");
                }
                return index;
            }
            else
            {
                Console.WriteLine(barcode + " no qty left to print");
                return -1;
            }
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
        static bool triggerIsOn = false;
        public static Queue<Tag> barcodes;
        static List<Tag> Tags;
        public static bool initialized = false;
        public static void Start()
        {
            barcodes = new Queue<Tag>();
//            IPAddress ip_BarcodeReader1 = IPAddress.Parse("192.168.16.46");
            EthSystemConnector conn_BarcodeReader1 = new EthSystemConnector(ip_BarcodeReader1);

            BarcodeReader1 = new DataManSystem(conn_BarcodeReader1);
            //subscribe to Cognex barcode reader "string arrived" event

            BarcodeReader1.ReadStringArrived += new ReadStringArrivedHandler(BarcodeReader1_ReadStringArrived);
            //create Modbus object to monitor edge detection sensor on the Protos X I/O board
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

            Tags = new List<Tag>();
            //open connection to barcode reader
            BarcodeReader1.Connect();
            BarcodeReader1.SetResultTypes(ResultTypes.ReadString);
            initialized = true;
        }

        public static void DisplayLog()
        {
            foreach (Tag t in Tags.ToArray())
            {
                Console.WriteLine(t.barcodeData + t.Elapsed().ToString());
            }
        }
        private static void BarcodeReader1_ReadStringArrived(object sender, ReadStringArrivedEventArgs args)
        {
            if (BarcodeData == null)
            {
                BarcodeData = args.ReadString.ToString();
            }
        }
        public static void handleBarcode(string aBarcode){
            Tag aTag = new Tag(aBarcode, edge1Time, edge2Time);
            barcodes.Enqueue(aTag);
            Tags.Add(aTag);
            BarcodesRead++;
            Console.WriteLine(BarcodesRead.ToString() + ":" + aBarcode);            
        }

        public static void WatchForBarcode() // you were missing a return type. I've added void for now
        {
            //a sensor state of true = over a tag
            bool CurrentSensorState;
            if (!modbusClient.Connected) modbusClient.Connect();
            bool[] readSensorInput = modbusClient.ReadDiscreteInputs(0, 1);
            CurrentSensorState = readSensorInput[0];

            //do nothing if the sensor state has not changed
            if (CurrentSensorState != PreviousSensorState)
            {
                if (CurrentSensorState) edge1Time = DateTime.Now.Ticks;
                else edge2Time = DateTime.Now.Ticks;
                if (!CurrentSensorState) Console.Write(" - " + (edge2Time - edge1Time).ToString() + " - ");
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
                        triggerIsOn = true;
                        BarcodeData = null;
                    }
                }
                else if (SensorInitialized == true & CurrentSensorState == false)  // wait until tag has passed sensor?? -tw
                {
                    if (BarcodeReader1.State == ConnectionState.Connected)
                    {
                        BarcodeReader1.SendCommand("TRIGGER OFF");
                        triggerIsOn = false;
                    }
                    if (BarcodeData != null)
                    {
                        handleBarcode(BarcodeData);
                        //Console.WriteLine(BarcodeData.ToString());// emit to console
/*                        Tag aTag = new Tag(BarcodeData, edge1Time, edge2Time);
                        barcodes.Enqueue(aTag);
                        Tags.Add(aTag);
                        BarcodesRead++;
                        Console.WriteLine(BarcodesRead.ToString() + ":" + BarcodeData);
*/                        // JobSpoolSVC.voice.Speak("Good...");
                    }
                    else
                    {
                        handleBarcode("No Barcode");
/*                        Tag aTag = new Tag("No Barcode", edge1Time, edge2Time);
                        barcodes.Enqueue(aTag);
                        Tags.Add(aTag);
                        Console.WriteLine("No Barcode!");
 */                       // some piece of crap went through and we couldn't read a barcode.
                        // JobSpoolSVC.voice.Speak("Crap!");
                    }
                }

            }
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
