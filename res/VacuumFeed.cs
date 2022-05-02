using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasyModbus;


namespace dp_printer_prod
{ 
       public static class VacuumFeed
       {
        const int TagIsWaitingForReadCoil = 0;  // bit 0 ?
        const int ContinueCoil = 1;
        const int AbortCoil = 2;
        const int HaltPlacerCoil = 3;  // false = medium tags, true = small tags defaults to false;

        static ModbusClient modBusClient;
        static bool sensorInitialized = false;
        static bool previousSensorState = false;
        static bool simulateTagIsWaiting = false;

        public static void Start(string vfIPAddress = "192.168.8.45")
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
                Console.WriteLine("ERROR - Is everything turned on?\nVacuum feed not responding. "+e.Message);
                return;
            }
        }

        public static void haltPlacer()
        {
            modBusClient.WriteSingleCoil(HaltPlacerCoil, true);  // set to true 
        }
        public static void SetSimulateTagIsWaiting(bool waiting)
        {
            simulateTagIsWaiting = waiting;
        }
        public static void Continue()
        {
            //Program.tagsOnBelt++;
            //if ((Program.tagsPerBang > 0) && (Program.tagsOnBelt % Program.tagsPerBang == 0)) Printer.WaitForPrintComplete(5000);
            modBusClient.WriteSingleCoil(ContinueCoil, true);
            //previousSensorState = false;
        }
        public static void Abort()
        {
            modBusClient.WriteSingleCoil(AbortCoil, true);
            ClearTagWaiting();
            //previousSensorState=false;
        }

        // clears the previous sensor state
        // call after abort or timeout
        public static void ClearTagWaiting()
        {
            previousSensorState = false;
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
}
