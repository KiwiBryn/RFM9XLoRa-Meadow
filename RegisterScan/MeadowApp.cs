//---------------------------------------------------------------------------------
// Copyright (c) December 2019, devMobile Software
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
namespace devMobile.IoT.Rfm9x.RegisterScan
{
   using System;
   using System.Threading.Tasks;

   using Meadow;
   using Meadow.Devices;
   using Meadow.Hardware;

   public sealed class Rfm9XDevice
   {
      private SpiPeripheral sx127xDevice;
      private IDigitalOutputPort spiPeriphChipSelect;

      public Rfm9XDevice(IIODevice device, ISpiBus spiBus, IPin chipSelectPin)
      {
         spiPeriphChipSelect = device.CreateDigitalOutputPort(chipSelectPin, initialState: true);
         if (spiPeriphChipSelect == null)
         {
            Console.WriteLine("spiPeriphChipSelect == null");
         }

         sx127xDevice = new SpiPeripheral(spiBus, spiPeriphChipSelect);
         if (sx127xDevice == null)
         {
            Console.WriteLine("sx127xDevice == null");
         }
      }

      public Byte RegisterReadByte(byte registerAddress)
      {
         byte[] txBuffer = new byte[] { registerAddress };

         byte[] rxBuffer = sx127xDevice.WriteRead(txBuffer, 2);

         return rxBuffer[1];
      }
   }

   public class MeadowApp : App<F7Micro, MeadowApp>
   {
      private Rfm9XDevice rfm9XDevice;

      public MeadowApp()
      {
         ISpiBus spiBus = Device.CreateSpiBus(500);
         if (spiBus == null)
         {
            Console.WriteLine("spiBus == null");
         }

         rfm9XDevice = new Rfm9XDevice(Device, spiBus, Device.Pins.D09);

         while (true)
         {
            for (byte registerIndex = 0; registerIndex <= 0x42; registerIndex++)
            {
               byte registerValue = rfm9XDevice.RegisterReadByte(registerIndex);

               Console.WriteLine("Register 0x{0:x2} - Value 0X{1:x2} - Bits {2}", registerIndex, registerValue, Convert.ToString(registerValue, 2).PadLeft(8, '0'));
            }

            Task.Delay(10000).Wait();
         }
      }
   }
}
