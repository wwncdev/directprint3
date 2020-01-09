
///////////////////////////////////////////////////////////////
//
// XiJet_Test()
//
// Illustrate the use XIJET_API
//
void XiJet_Test()
{
    int XiJetStatus;
    XIJET_CONFIGURATION PrinterConfig;
    CHAR printerName[20];
    CHAR statusMessage[120];
    HANDLE printerHandle;
    HANDLE textFont1;
    HANDLE uspsFont1;

    //
    // example use of XIJET_ProbePrinter attached printer
    //
    if (XIJET_ProbePrinter(0, printerName) == FALSE)
    {
        printf("No printers found! \n");
        return;
    }
    printf("Found printer: %s \n", printerName);

    //
    // open the printer for communication...
    //
    printerHandle = XIJET_OpenPrinter(printerName, &PrinterConfig);
    if (printerHandle == INVALID_HANDLE_VALUE)
    {
        printf("Error openning printer! \n");
        return;
    }

    // query and display the list of supported resolutions…
    USHORT resolutionIndex = 0;
    CHAR resolutionDescripton[64];
    USHORT horizontalDPI;
    USHORT verticalDPI;
    USHORT maxSpeedIPS;
    USHORT resIndexSave = -1;
    while (XIJET_GetPrinterResolution(printerName, resolutionIndex, resolutionDescripton, &horizontalDPI, &verticalDPI, &maxSpeedIPS))
    {
        printf("Resolution Index %d = %s \n", resolutionIndex, resolutionDescripton);
        // if this is 600 vertical X 300 horizontal, save the index for later...
        // (that's the resolution of the sample fonts, and the resolution that
        // the sample is designed for...)
        if ((verticalDPI == VERTICAL_RES) && (horizontalDPI == HORZONTAL_RES))
        {
            resIndexSave = resolutionIndex;
            resolutionIndex++;
        }
    }
    // if for some reason we didn't find 600 x 300 resolution then stop
    if (resIndexSave == -1)
    {
        printf("Resolution 600 vertical X 300 horizontal not found! \n");
        return;
    }

    // use XIJET_SetPrinterParameter to select resolution
    XiJetStatus = XIJET_SetPrinterParameter(printerHandle, XIJET_RESOLUTION, &resIndexSave);
    if (XiJetStatus == 0)
    {
        printf("Error return from XIJET_SetPrinterParameter!");
        // use XIJET_GetStatus to retrieve a message
        XIJET_GetStatus(printerHandle, statusMessage);
        printf("XiJet status: %s \n", statusMessage);
        XIJET_ClosePrinter(printerHandle);
        return;
    }

    //
    // load some fonts... Arial text font and USPS barcode font
    //
    textFont1 = XIJET_LoadFontXFT(printerHandle, "../XIJET_API/Arial_600x300_12_400_0.XFT");
    uspsFont1 = XIJET_LoadFontXFT(printerHandle, "./XIJET_API/USPSIMBStandard_600x300_16_500_0.XFT");
    if ((textFont1 == INVALID_HANDLE_VALUE) || (uspsFont1 == INVALID_HANDLE_VALUE))
    {
        printf("Error openning font file! \n");
        return;
    }

    //
    // sample print loop
    //
    ULONG canvasHeight = 4 * VERTICAL_RES; // 4" (2400 pixels)
    ULONG canvasWidth = 6 * VERTICAL_RES;  // 6" (1800 pixels)
    CHAR printMsg[256];
    int i;
    //
    // print 10 documents...
    //
    for (i = 0; i < 10; i++)
    {
        XIJET_CanvasBegin(printerHandle, canvasHeight, canvasWidth);
        //
        // write a block of text at vertOffset = 0 (at top)
        //
        sprintf(printMsg, "Sample Print %d", (int)i);
        XIJET_CanvasWriteStr(printerHandle,
                             textFont1,
                             printMsg,
                             0, // vertOffset = 0
                             30 // horizOffset = 30 pixels (0.1" from left)
        );

        //
        // write a barcode (using barcode font)
        //
        XIJET_CanvasWriteStr(printerHandle,
                             uspsFont1,
                             "FFADTFFAAFFFADTFFAAFFFADTFFAAF", // (not really valid)
                             120,                              // vertOffset = 120 pixels (0.2" from top)
                             30                                // horizOffset == 30 (0.1" from left)
        );
        // print the document...
        //
        XiJetStatus = 0;
        // note that we keep re-trying as long as
        // the status indicates a simple "timeout"
        // timeout status of 0 is OK... it just means that
        // the printers image buffer is full
        while (XiJetStatus == 0)
        {
            XiJetStatus = XIJET_CanvasPrint(printerHandle,
                                            0,    // head 1 vert offset
                                            1200, // head 2 vert offset 2" or 1200 pixels
                                            0,    // head 3 vertical offset don’t care ..
                                            0,    // head 4 .. assuming a 2x2" system
                                            100   // timeout in msecs
            );
        }
        // if the status indicates an error, go retrieve the error message
        if (XiJetStatus != 1)
        {
            XIJET_GetStatus(printerHandle, statusMessage);
            printf("XiJet status: %s \n", statusMessage);
            break; // and break out of print loop
        }
    }

    // if we terminated with an error, issue a reset..
    if (XiJetStatus < 0)
    {
        XIJET_Reset(printerHandle);
    }
    else
    {
        //
        // wait for last print to complete
        //
        // similar to the canvas print above, we just wait
        // if the status comes back as timeout
        XiJetStatus = 0;
        while (XiJetStatus == 0)
        {
            XiJetStatus = XIJET_WaitForPrintComplete(printerHandle, 100);
        }
    }
    // close the printer...
    XIJET_ClosePrinter(printerHandle);
}