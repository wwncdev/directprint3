using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Timers;

namespace dp_printer_prod
{
    static class JobSpoolSVC
    {
        static Timer aTimer;
        public static string status = "starting";
        const string jobfiledir = @"c:\\jobfiles\\printer";
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
        public static Dictionary<int, byte[]> index2image = new Dictionary<int, byte[]>();
        public static byte[] goatImage;

        public static int imageWidth { get; private set; }

        public static void Start(int printerNumber)
        {
            spooldir = jobfiledir + printerNumber.ToString() + "\\spooled\\";
            processdir = jobfiledir + printerNumber.ToString() + "\\processing\\";
            archivedir = jobfiledir + printerNumber.ToString() + "\\archive\\";
            settingsFile = jobfiledir + printerNumber.ToString() + "\\settings.json";


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
  //              Program.yoffset = (int)settings["offset"];
            }
        }
        public static void SaveJob()
        {
            File.WriteAllText(archivedir + LastFileName, JsonConvert.SerializeObject(job));

            // serialize JSON directly to a file
            using (StreamWriter file = File.CreateText(archivedir + LastFileName))
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
                        //Console.Write(index.ToString()+",");
                        //                        Printer.displayHex((string)item["label"]);
                        index2image.Add(index, ConvertImage((string)item["label"]));
                        //Printer.displayBITS2(ConvertImage((string)item["label"]),192,640);
                        if (tagkeys.Count > 0)
                        {   // the item has to have possible tagkeys
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
                //Console.WriteLine();
            }
        }

        public static string Aslen(string str, int l)
        {
            if (str == null) return "";
            if ((str != null) && (str.Length > l)) str = str.Substring(0, l);
            else str = str.PadRight(l);
            return str;
        }

        public static void DisplayItem(JToken item)
        {
            //Console.Write(items.IndexOf(item).ToString().PadLeft(5));
            Console.Write(": " + Aslen(item["item_description"].ToString(), 60));
            Console.Write(item["printqty"].ToString().PadLeft(5));
            Console.Write(((int)item["printqty"] - (int)item["notprintedqty"]).ToString().PadLeft(5));
            Console.Write(item["notprintedqty"].ToString().PadLeft(5));
            //    Console.Write("  " + Aslen(item["itemupc"].ToString(), 15));
            Console.WriteLine();
        }
        public static void DisplayStatus()
        {
            if (job != null)
            {

                JArray items = (JArray)job["incomplete_items"];
                Console.WriteLine("Items Completed");
                foreach (JToken item in items)
                {
                    int notPrintedQty = (int)item["notprintedqty"];
                    int printedQty = (int)item["printqty"] - (int)item["notprintedqty"];
                    if (notPrintedQty == 0) DisplayItem(item);
                }
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine("Items Started But Not Completed");
                foreach (JToken item in items)
                {
                    int notPrintedQty = (int)item["notprintedqty"];
                    int printedQty = (int)item["printqty"] - (int)item["notprintedqty"];
                    if ((printedQty > 0) && (notPrintedQty > 0)) DisplayItem(item);
                }

            }
            //            Console.WriteLine("VF TagWaitingStatus:" + VacuumFeed.TagIsWaitingForBarcodeScanner().ToString());
        }

        public static int getCountItem(string tagkey)
        {
            if (tag2items.ContainsKey(tagkey))
            {
                return tag2items[tagkey].Count;
            }
            return 0;
        }

        public static int getItemIndex(string tagkey, bool decrement = false)
        {
            // use barcode to find incomplete item in list
            if (tag2items.ContainsKey(tagkey))
            {
                for (int bc = 0; bc < tag2items[tagkey].Count; bc++)
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
                    Console.Write(String.Join(',', choices.ToArray()) + "):");
                    char choice = 'Q';
                    while (choices.IndexOf(choice) == -1)
                    {
                        System.ConsoleKeyInfo keyInfo = Console.ReadKey(true);
                        choice = char.ToUpper(keyInfo.KeyChar);
                        if (choices.IndexOf(choice) == -1) Console.Beep();
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
                            //qty = qty + 1;
                            qty = printQty;
                            job["incomplete_items"][index]["notprintedqty"] = qty;
                            //Console.WriteLine(qty + " of " + printQty);
                        }
                        else
                        {  // zero out any non-matching ones
                            int qty = Int32.Parse((string)job["incomplete_items"][index]["notprintedqty"]);
                            int printQty = Int32.Parse((string)job["incomplete_items"][index]["printqty"]);
                            //qty = qty + 1;
                            qty = 0;
                            job["incomplete_items"][index]["notprintedqty"] = qty;

                        }
                    }
                }
                else
                {
                    int index = tag2items[lastBarcode][0];
                    int qty = Int32.Parse((string)job["incomplete_items"][index]["notprintedqty"]);
                    int printQty = Int32.Parse((string)job["incomplete_items"][index]["printqty"]);
                    //qty = qty + 1;
                    qty = printQty;
                    job["incomplete_items"][index]["notprintedqty"] = qty;
                    //Console.WriteLine(qty + " of " + printQty);
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
                for (int b = 0; b < 8; b++)  // loop through 8 bits
                {
                    if ((v & (1 << b)) != 0)
                    {
                        r += (3 << (b * 2));
                    }
                }
                Bits[(x * 2) + 1] = (byte)(r % 256);
                Bits[(x * 2)] = (byte)(r / 256);
            }
            return Bits;
        }

        public static byte[] ConvertImage(string imageEncoded)
        {
            byte[] bmp = Convert.FromBase64String(imageEncoded);
            // Some Image and Data Metrics 
            int imgLen = bmp.Length;
            int o = bmp[10] + bmp[11] * 256;
            int imageWidth = bmp[18] + bmp[19] * 256;
            int imageHeight = bmp[22] + bmp[23] * 256;
            //Console.WriteLine("Width:"+imageWidth.ToString()+"  Height:"+imageHeight.ToString());
            int colorPlanes = bmp[26];
            int bitsPerPixel = bmp[28];
            //            Console.WriteLine("ColorPlanes:" + colorPlanes.ToString() + "  bitsPerPixel:" + bitsPerPixel.ToString());

            int numBytes = imageWidth * imageHeight;
            int bytesPerRow = imageWidth / 8;  // this must be evenly divisible by 4 *** shit!
            byte[] Bits = new byte[(imageWidth * imageHeight) / 8];
            byte whitey = bmp[o];  // we'll just agree never to write to 0,0

            for (int y = 0; y < imageHeight; y++)
            {
                for (int x = 0; x < imageWidth; x++)
                {
                    if ((x % 8) == 0) Bits[y * bytesPerRow + x / 8] = 0;  // initialize byte
                    byte bmpByte = bmp[o + y * imageWidth + x];
                    byte aBit = (bmpByte != whitey) ? (byte)1 : (byte)0;
                    Bits[(y * bytesPerRow) + (x / 8)] += (byte)(aBit << 7 - ((x % 8))); // added the 7- to test msb lsb issue
                }
            }
            return Bits;
        }
    }

}