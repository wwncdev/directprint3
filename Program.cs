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
using System.Speech.Synthesis;

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
        public static int printerNumber = 1;
        public static int feednscan = 1;
        public static int spooldir = 1;
        public static int tagsPerBang = 0;
        public static int tagsOnBelt = 0;
        public static int extraTags = 0;
        public static bool doneProcessing = false;
        public static int imageWidth = 192;
        public static int imageHeight = 640;
        static ValueTuple<Int32, Int32> cpos = (0, 0);
        static int lastIndex = 0;

        static void Main(string[] args)
        {
            // monitor for job file
            // read in job file
            ProcessCommandLine(args);
            var synthesizer = new SpeechSynthesizer();
            synthesizer.SetOutputToDefaultAudioDevice();


            JobSpoolSVC.Start(Program.spooldir);
            JobSpoolSVC.watchSpool();



            Console.WriteLine("Printer Number:" + printerNumber.ToString());
            Console.WriteLine("Feed-n-scan :" + feednscan.ToString());
            switch (feednscan)
            {
                case 1:
                    VacuumFeed.Start("192.168.8.45");
                    BarcodeScanner.Start("192.168.8.46");
                    break;
                default:
                    VacuumFeed.Start("192.168.8.47");
                    BarcodeScanner.Start("192.168.8.48");
                    break;
            }
            Printer.Init(printerNumber, Program.dblWidth);

            //            JobSpoolSVC.ReadSettings();
            while (!doneProcessing)
            {
                CheckKeyCommands();
                // if this isn't very performant or takes a high percent of CPU then add some sleep time
                // to allow thread to go process other things.
                if (VacuumFeed.TagIsWaitingForBarcodeScanner())
                {
                    string barcode = BarcodeScanner.Scan(5);

                    if (barcode != "NO READ")
                    {
                        var index = JobSpoolSVC.getItemIndex("UPCA" + barcode, true);
                        if (index >= 0) // is barcode in the order
                        {
                            if (index != lastIndex)
                            {
                                Console.Write("Pause while tag clears printhead....");
                                System.Threading.Thread.Sleep(1500);
                                Console.WriteLine("buffer flushed");
                                Printer.Flush(dblWidth);
                            }
                            VacuumFeed.Continue();
                            // need to print the tag
                            bool success;
                            do
                            {
                              UpdateConsole(index,barcode);  // this updates lastIndex
                              success = Printer.PrintBits(JobSpoolSVC.index2image[index], imageWidth * (Program.dblWidth ? 2 : 1), imageHeight, yoffset);
                            } while (!success);
                        }
                        else  // either NO READ of barcode not in order
                        {
                            VacuumFeed.Abort();
                            VacuumFeed.haltPlacer();
                            Console.WriteLine("Barcode: " + barcode + " - Not in list or too many?");
                        }
                    }
                    else
                    {
                        VacuumFeed.haltPlacer();
                        VacuumFeed.Abort();
                    }
                }else
                {
                    System.Threading.Thread.Sleep(50);
                    JobSpoolSVC.watchSpool();
                }
            }
            Printer.Close();
        }

        private static void UpdateConsole(int index,string barcode)
        {
            JToken item = JobSpoolSVC.job["incomplete_items"][index];
            int qty = Int32.Parse((string)item["notprintedqty"]);
            int printqty = Int32.Parse((string)item["printqty"]);
            int printnumber = printqty - qty;
            string lineno = (string)item["lineno"];
            string item_description = (string)item["item_description"];
            item_description = item_description.PadRight(40).Remove(35);
            if (lastIndex != index)
            {
                lastIndex = index;

                cpos = Console.GetCursorPosition();
            }
            else
            {
                Console.SetCursorPosition(cpos.Item1, cpos.Item2);
            }
            Console.WriteLine("LineNo:" + lineno.PadLeft(5) + " " + item_description + " UPC:" + barcode + " " +
                printnumber.ToString().PadLeft(5) + " of " + printqty.ToString());

        }
        private static void ProcessCommandLine(string[] args)
        {
            foreach (string arg in args)
            {
                Console.WriteLine(arg);
                switch (arg.Split("=")[0])
                {
                    case "tagsPerBang":
                        tagsPerBang = int.Parse(arg.Split("=")[1]);
                        Console.WriteLine("tagsPerBang = " + tagsPerBang.ToString());
                        break;
                    case "imageWidth":
                        int val = int.Parse(arg.Split("=")[1]);
                        imageWidth = val;
                        break;
                    case "dblWidth":
                        bool bval = bool.Parse(arg.Split("=")[1]);
                        dblWidth = bval;
                        break;
                    case "printerNumber":
                        printerNumber = int.Parse(arg.Split("=")[1]);
                        break;
                    case "feednscan":
                        feednscan = int.Parse(arg.Split("=")[1]);
                        break;
                    case "spooldir":
                        spooldir = int.Parse(arg.Split("=")[1]);
                        break;
                    default:
                        Console.WriteLine("Error above argument not recognized");
                        break;
                }
            }   // process arguements
        }

        private static void CheckKeyCommands()
        {
            char cmd;
            string tagkey;

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
                    case 'C':
                        int success = Printer.ActivateInkPurge();
                        Console.Write("Wait....");
                        System.Threading.Thread.Sleep(5000);
                        Printer.Flush(dblWidth);
                        Printer.Flush(dblWidth);
                        Console.WriteLine("ActivateInkPurge returned: " + success.ToString());
                        break;
                    case '8': // eiiiiiii syringa? 
                    case '0': // probably the beginning of a barcode
                        if (cmd == '0') preamble = "0";
                        if (cmd == '8') preamble = "08";

                        tagkey = Console.ReadLine();
                        tagkey = preamble + tagkey;
                        int index = JobSpoolSVC.getItemIndex("UPCA" + tagkey, false);
                        JobSpoolSVC.AddOneMore("UPCA" + tagkey);
                        Printer.Flush(dblWidth);
                        BarcodeScanner.init();
                        break;
                    case 'T':
                        VacuumFeed.SetTagIsWaiting();
                        break;
                    case 'E':
                        // end processing current job
                        JobSpoolSVC.status = "Starting";
                        JobSpoolSVC.StopIgnoringJobs();
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
                        //Console.WriteLine("yoffset: " + yoffset.ToString());
                        break;
                    case 'W':
                        JobSpoolSVC.SaveJob();
                        break;
                    case 'P':
                        Printer.DisplayParams();
                        break;
                    case '-':
                        Printer.SetInkVoltage(-2);
                        break;
                    case '+':
                    case '=':
                        Printer.SetInkVoltage(2);
                        break;
                    case 'I':
                        Console.WriteLine("Init Printer and Barcode Scanner");
                        Printer.Flush(dblWidth);
                        BarcodeScanner.init();
                        break;
                    case 'M':
                        BarcodeScanner.SetTagAutoMode(!BarcodeScanner.GetTagAutoMode());
                        Console.WriteLine("TagAutoMode: " + BarcodeScanner.GetTagAutoMode().ToString());
                        break;
                    case 'A':
                    case 'V':
                        // vacuum feed advance
                        VacuumFeed.Abort();
                        Console.WriteLine("Vacuum Feed Abort");
                        break;
                    case 'Z':
                        VacuumFeed.haltPlacer();
                        Console.WriteLine("Placer will Stop");
                        break;

                    default:
                        displayHelp();
                        break;
                }
            }
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
}
