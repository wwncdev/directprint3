using System;
using System.IO;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Timers;
using System.ComponentModel;
using Cognex.DataMan.SDK;
using EasyModbus;
using Newtonsoft.Json.Linq;


namespace dpservice_composer
{
    class Program
    {

        static void Main(string[] args)
        {
            // monitor for job file
            // read in job file

            JobSpoolSVC.Start();
            Console.WriteLine("Hello World!");
            Console.ReadKey();
        }

    }

    static class JobSpoolSVC
    {
        static Timer aTimer;
        const string jobfiledir=@"c:/jobfiles/";
        static String spooldir;
        static String processdir;
        static String archivedir;
        public static JToken job;
        static DPsys dpsys;
        public static Dictionary<string,List <int>> tag2items = new Dictionary<string,List <int>>();
            public static void Start()
        {
            spooldir = jobfiledir + "spooled/";
            processdir = jobfiledir + "processing/";
            archivedir = jobfiledir + "archive/";

            // start by cleaning out the directory
            string[] fileEntries = Directory.GetFiles(spooldir);
            foreach(string fileName in fileEntries) File.Delete(fileName);

            aTimer = new Timer(200);
            aTimer.Elapsed += CheckSpoolDirEvent;
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
            Console.WriteLine("Started...");
        }

        private static void CheckSpoolDirEvent(Object source, ElapsedEventArgs e)
        {
            string[] fileEntries = Directory.GetFiles(spooldir,@"*.json");
            if (fileEntries.Length > 0)
            {
                aTimer.Enabled = false;

                // get rid of old processing file if it already exists
                if (File.Exists(processdir + Path.GetFileName(fileEntries[0]))) File.Delete(processdir + Path.GetFileName(fileEntries[0]));
                
                // move file from spooled to processing
                File.Move(spooldir + Path.GetFileName(fileEntries[0]), processdir + Path.GetFileName(fileEntries[0]));
                using (StreamReader r = new StreamReader(processdir + Path.GetFileName(fileEntries[0])))
                {
                    var json = r.ReadToEnd();
                    job = JToken.Parse(json);

                    // now add an lookup from tagkey to item
                    JArray items = (JArray) job["incomplete_items"];
                    foreach( JToken item in items)
                    {
                        foreach(string tag in item["tagkeys"])
                        {
                            if (!tag2items.ContainsKey(tag))
                            {
                                tag2items[tag] = new List<int>();
                            }
                            tag2items[tag].Add(items.IndexOf(item));
                        }
                    }
                    foreach(var ndexs in tag2items)
                    {
                        Console.Write(ndexs.Key+":");
                        foreach(int ndex in ndexs.Value)
                        {
                            Console.Write(ndex.ToString()+",");
                            Console.Write(items[ndex]["item_description"]);
                        }
                        Console.WriteLine("*");
                    }
                    //dpsys = new DPsys(job);
                    //   incompleteItems = ((JArray)job["incomplete_items"]);
                }

            }
        }
    }


    class DPsys
    {

        bool SensorInitialized;
        bool PreviousSensorState;
        string BarcodeData;
        public DataManSystem BarcodeReader1;
        public ModbusClient modbusClient;
        Timer timerProtosX;
        public BackgroundWorker MonitorEdgeSensor;
        TcpClient tcpClientComposer;
        StreamWriter writerComposer;

       

        public DPsys(JToken job)
        {
            //Console.WriteLine(JobSpoolSVC.job["incomplete_items"]);
            foreach(JToken item in job["incomplete_items"])
            {
                Console.WriteLine(item["tagkeys"]);

            }
            IPAddress ip_BarcodeReader1 = IPAddress.Parse("192.168.16.46");
            EthSystemConnector conn_BarcodeReader1 = new EthSystemConnector(ip_BarcodeReader1);
            BarcodeReader1 = new DataManSystem(conn_BarcodeReader1);
            //subscribe to Cognex barcode reader "string arrived" event
            BarcodeReader1.ReadStringArrived += new ReadStringArrivedHandler(BarcodeReader1_ReadStringArrived);

            //create Modbus object to monitor edge detection sensor on the Protos X I/O board
            modbusClient = new ModbusClient("192.168.16.45", 502);

            //backgroundworker to monitor edge detection sensor on the Protos X I/O board
            MonitorEdgeSensor = new BackgroundWorker();
            MonitorEdgeSensor.DoWork += new DoWorkEventHandler(MonitorEdgeSensor_DoWork);
            MonitorEdgeSensor.RunWorkerCompleted += new RunWorkerCompletedEventHandler(MonitorEdgeSensor_RunWorkerCompleted);

            //timer to monitor edge detection sensor on the Protos X I/O board
            timerProtosX = new Timer();
            timerProtosX.Interval = 200;
            timerProtosX.Elapsed += new ElapsedEventHandler(TimerHandlerProtosX);

            //TCP client to talk to Composer
            tcpClientComposer = new TcpClient();
            tcpClientComposer.NoDelay = true;

            //open connection to Protos X and get current state of sensor
            modbusClient.Connect();
            bool[] readSensorInput = modbusClient.ReadDiscreteInputs(0, 1);
            PreviousSensorState = readSensorInput[0];

            //sensor is initialized if there is no tag under the sensor when the timer is started
            if (PreviousSensorState == false)
            {
                SensorInitialized = true;
            }
            else
            {
                SensorInitialized = false;
            }

            //open connection to barcode reader
            BarcodeReader1.Connect();
            BarcodeReader1.SetResultTypes(ResultTypes.ReadString);

            //open connection to Composer
            tcpClientComposer.Connect("127.0.0.1", 9100);
            NetworkStream nwStreamComposer = tcpClientComposer.GetStream();
            writerComposer = new StreamWriter(nwStreamComposer);
            writerComposer.AutoFlush = true;
            writerComposer.WriteLineAsync("PRT");

            //start timer
            timerProtosX.Start();
        }
        private void BarcodeReader1_ReadStringArrived(object sender, ReadStringArrivedEventArgs args)
        {
            if (BarcodeData == null) 
            {
                BarcodeData = args.ReadString.ToString();
            }
        }

        private void MonitorEdgeSensor_DoWork(object sender, DoWorkEventArgs e)
        {
            //a sensor state of true = over a tag
            bool CurrentSensorState;
            bool[] readSensorInput = modbusClient.ReadDiscreteInputs(0, 1);
            CurrentSensorState = readSensorInput[0];

            //do nothing if the sensor state has not changed
            if (CurrentSensorState == PreviousSensorState)
            {
                return;
            }
            else
            {
                PreviousSensorState = CurrentSensorState;
            }

            //if the sensor was not initialized when the timer was started (i.e., the sensor was over a tag when the timer was started)
            //then the sensor will be initialized the first time it is over a gap
            if (SensorInitialized == false & CurrentSensorState == false)
            {
                SensorInitialized = true;
            }
            else if (SensorInitialized == true & CurrentSensorState == true)
            {
                BarcodeData = null;
                if (BarcodeReader1.State == ConnectionState.Connected)
                    BarcodeReader1.SendCommand("TRIGGER ON");
            }
            else if (SensorInitialized == true & CurrentSensorState == false)
            {
                if (BarcodeReader1.State == ConnectionState.Connected)
                    BarcodeReader1.SendCommand("TRIGGER OFF");

                if (BarcodeData != null)
                {
                    /*                     ProgramStatus.Dispatcher.Invoke(() =>
                                        {
                                            ProgramStatus.Text += BarcodeData.ToString();
                                        }); */
                    Console.WriteLine(BarcodeData.ToString());// emit to console

                    switch (BarcodeData)
                    {
                        case "111":
                            writerComposer.WriteLineAsync("RPD*DATA1=$1.11");
                            break;
                        case "222":
                            writerComposer.WriteLineAsync("RPD*DATA1=$2.22");
                            break;
                        case "333":
                            writerComposer.WriteLineAsync("RPD*DATA1=$3.33");
                            break;
                        case "444":
                            writerComposer.WriteLineAsync("RPD*DATA1=$4.44");
                            break;
                    }
                }
            }
        }


        private void MonitorEdgeSensor_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                //ProgramStatus.Text = e.Error.Message;
                Console.WriteLine(e.Error.Message);
            }
            else
            {

            }
        }

        private void TimerHandlerProtosX(object sender, ElapsedEventArgs e)
        {
            MonitorEdgeSensor.RunWorkerAsync();
            Console.Write(".");
        }


    }
}
