//---------------------------------------------------------------------------------
// Copyright (c) January 2020, devMobile Software
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
//---------------------------------------------------------------------------------
namespace devMobile.IoT.Rfm9x.LoRaDeviceClient
{
   using System;
   using System.Diagnostics;
   using System.Text;
   using System.Threading.Tasks;

   using Meadow;
   using Meadow.Devices;
   using Meadow.Hardware;

   public class MeadowApp : App<F7Micro, MeadowApp>
   {
      private const double Frequency = 915000000.0;
      private byte MessageCount = Byte.MaxValue;
      private Rfm9XDevice rfm9XDevice;

      public MeadowApp()
      {
         try
         {
            ISpiBus spiBus = Device.CreateSpiBus(500);

            rfm9XDevice = new Rfm9XDevice(Device, spiBus, Device.Pins.D09, Device.Pins.D10, Device.Pins.D12);

            rfm9XDevice.Initialise(Frequency, paBoost: true, rxPayloadCrcOn: true);
#if DEBUG
            rfm9XDevice.RegisterDump();
#endif

            rfm9XDevice.OnReceive += Rfm9XDevice_OnReceive;
#if ADDRESSED_MESSAGES_PAYLOAD
            rfm9XDevice.Receive(UTF8Encoding.UTF8.GetBytes("LoRaIoT1"));
#else
            rfm9XDevice.Receive();
#endif
            rfm9XDevice.OnTransmit += Rfm9XDevice_OnTransmit;
         }
         catch (Exception ex)
         {
            Console.WriteLine(ex.Message);
         }

         Task.Delay(10000).Wait();

         while (true)
         {
            string messageText = string.Format("Hello from {0} ! {1}", Environment.MachineName, MessageCount);
            MessageCount -= 1;

            byte[] messageBytes = UTF8Encoding.UTF8.GetBytes(messageText);
            Console.WriteLine("{0:HH:mm:ss}-TX {1} byte message {2}", DateTime.Now, messageBytes.Length, messageText);
#if ADDRESSED_MESSAGES_PAYLOAD
            this.rfm9XDevice.Send(UTF8Encoding.UTF8.GetBytes("AddressHere"), messageBytes);
#else
            this.rfm9XDevice.Send(messageBytes);
#endif
            Task.Delay(30000).Wait();
         }
      }

      private void Rfm9XDevice_OnReceive(object sender, Rfm9XDevice.OnDataReceivedEventArgs e)
      {
         try
         {
            string messageText = UTF8Encoding.UTF8.GetString(e.Data);

#if ADDRESSED_MESSAGES_PAYLOAD
            string addressText = UTF8Encoding.UTF8.GetString(e.Address);
            string addressHex = BitConverter.ToString(e.Address);

            Console.WriteLine(@"{0:HH:mm:ss}-RX From {1} ({2}) PacketSnr {3:0.0} Packet RSSI {4}dBm RSSI {5}dBm = {6} byte message ""{7}""", DateTime.Now, addressText, addressHex, e.PacketSnr, e.PacketRssi, e.Rssi, e.Data.Length, messageText);
#else
            Console.WriteLine(@"{0:HH:mm:ss}-RX PacketSnr {1:0.0} Packet RSSI {2}dBm RSSI {3}dBm = {4} byte message ""{5}""", DateTime.Now, e.PacketSnr, e.PacketRssi, e.Rssi, e.Data.Length, messageText);
#endif
         }
         catch (Exception ex)
         {
            Console.WriteLine(ex.Message);
         }
      }

      private void Rfm9XDevice_OnTransmit(object sender, Rfm9XDevice.OnDataTransmitedEventArgs e)
      {
         Console.WriteLine("{0:HH:mm:ss}-TX Done", DateTime.Now);
      }
   }
}
