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
namespace devMobile.IoT.Rfm9x.RegisterRead
{
   using System;
   using System.Threading.Tasks;

   using Meadow;
   using Meadow.Devices;
   using Meadow.Hardware;

   public class MeadowApp : App<F7Micro, MeadowApp>
   {
      const byte RegVersion = 0x42;
      ISpiBus spiBus;
      SpiPeripheral sx127xDevice;
      IDigitalOutputPort spiPeriphChipSelect;

      public MeadowApp()
      {
         ConfigureSpiPort();
         ReadDeviceID();
      }

      public void ConfigureSpiPort()
      {
         try
         {
            spiBus = Device.CreateSpiBus(500);
            if (spiBus == null)
            {
               Console.WriteLine("spiBus == null");
            }

            Console.WriteLine("Creating SPI NSS Port...");
            spiPeriphChipSelect = Device.CreateDigitalOutputPort(Device.Pins.D09, initialState: true);
            if (spiPeriphChipSelect == null)
            {
               Console.WriteLine("spiPeriphChipSelect == null");
            }

            Console.WriteLine("sx127xDevice Device...");
            sx127xDevice = new SpiPeripheral(spiBus, spiPeriphChipSelect);
            if (sx127xDevice == null)
            {
               Console.WriteLine("sx127xDevice == null");
            }

            Console.WriteLine("ConfigureSpiPort Done...");
         }
         catch (Exception ex)
         {
            Console.WriteLine("ConfigureSpiPort " + ex.Message);
         }
      }

      public void ReadDeviceID()
      {

         Task.Delay(500).Wait();

         while (true)
         {
            try
            {
               byte registerValue;

               // Using this device level approach works, without buffer size issues
               byte[] txBuffer = new byte[] { RegVersion };
               Console.WriteLine("spiBus.WriteRead...1");
               byte[] rxBuffer = sx127xDevice.WriteRead(txBuffer, 2);
               Console.WriteLine("spiBus.WriteRead...2");
               registerValue = rxBuffer[1];

               Console.WriteLine("Register 0x{0:x2} - Value 0X{1:x2} - Bits {2}", RegVersion, registerValue, Convert.ToString(registerValue, 2).PadLeft(8, '0'));
            }
            catch (Exception ex)
            {
               Console.WriteLine("ReadDeviceIDDiy " + ex.Message);
            }

            Task.Delay(10000).Wait();
         }
      }
   }
}
