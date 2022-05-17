

/*
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
                    if (lastIndex != index)
                    {
                        lastIndex = index;

                        cpos = Console.GetCursorPosition();
                    }
                    else
                    {
                        Console.SetCursorPosition(cpos.Item1, cpos.Item2);
                    }
                    Console.WriteLine("LineNo:"+lineno.PadLeft(5)+" "+item_description+" UPC:" + barcode.barcodeData + " " + 
                        printnumber.ToString().PadLeft(5) + " of "+printqty.ToString());
                    bool success;
                    do{
                        success =Printer.PrintBits(JobSpoolSVC.index2image[index], imageWidth * (Program.dblWidth ? 2 : 1), imageHeight, yoffset);
                        if (!success)
                        {
                            Console.WriteLine("Printer Error");
                        }
                    } while(!success);
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
*/


// from barcode scanner
/*
public static void WatchTagIsReadyToBeScanned()
{
    //get the current state of the 'Tag Raedy' flag from the vacuum feeder's PLC
    if (!VacuumFeed.TagWaitingHasChanged())
    //no change in state of 'Tag Ready' flag; when current and previous states are both false, we don't need to do anything
    {
        if (VacuumFeed.TagIsWaitingForBarcodeScanner())
        //even though the state of the flag did not change, it was previously true and is still true
        //this means that one timer interval passed while the tag was waiting to be read
        //we add an interval amount to the counter in order to determine when the timeout has been reached 
        {
            //TimeoutCounter += timerInterval;  // rewrite as timeElapsed in ms
            long timeElapsed = (triggerBeginTime > 0) ? (System.DateTime.Now.Ticks - triggerBeginTime) / 10000 : 0; // tickspermilliseconds = 10,000?
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

                    //                            Console.WriteLine("timeElapsed:" + timeElapsed.ToString());
                    if (handleBarcode(BarcodeData))
                    {
                        if (TagAutoMode)
                        {
                            VacuumFeed.Continue();
                        }
                        else
                        {
                            Console.WriteLine("TagAutoMode is currently OFF!!!");
                        }
                    }
                    else
                    {
                        if (BarcodeData == "8127STOP")
                        {
                            Console.Beep(); Console.Beep();
                            VacuumFeed.Abort();
                            VacuumFeed.haltPlacer();
                            Console.WriteLine("END OF JOB " + BarcodeData + " Placer Halted");
                        }
                        else
                        {
                            int index = JobSpoolSVC.getItemIndex("UPCA" + BarcodeData, false);
                            if (index < 0)
                            {
                                VacuumFeed.haltPlacer();
                                Console.WriteLine("TAG " + BarcodeData + " NOT FOUND IN ORDER.");
                                Console.Beep(); Console.Beep(); Console.Beep();
                            }
                            else
                            {
                                VacuumFeed.Abort();
                            }
                        }
                    }
                    BarcodeData = null;  // mission accomplished
                }
                else  // barcodedata is null
                {
                    //the timeout for reading a barcode has been reached and there is still no value returned by the reader
                    //perform desired 'bad tag' actions

                    //set the 'Abort' flag on the vacuum feeder's PLC
                    //if (TagAutoMode)
                    //    VacuumFeed.Abort();

                    // BarcodeNumber.Content = "No Read";
                    //simulateTagIsWaiting = false;
                    Console.WriteLine("No Read - Waited for:" + (timeElapsed / 1000).ToString() + "Seconds");
                    VacuumFeed.Abort();
                    VacuumFeed.haltPlacer();
                    Console.Beep();
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

*/