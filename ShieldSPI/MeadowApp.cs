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
namespace devMobile.IoT.Rfm9x.ShieldSPI
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
            var spiClockConfiguration = new SpiClockConfiguration(500, SpiClockConfiguration.Mode.Mode0); // From SemTech docs pg 80 CPOL=0, CPHA=0

            spiBus = Device.CreateSpiBus(Device.Pins.SCK,
                                         Device.Pins.MOSI,
                                         Device.Pins.MISO,
                                         spiClockConfiguration);
            if (spiBus == null)
            {
               Console.WriteLine("spiBus == null");
            }

            Console.WriteLine("Creating SPI NSS Port...");
            spiPeriphChipSelect = Device.CreateDigitalOutputPort(Device.Pins.D09, initialState:true);
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

               // Doesn't work Dec 2019 always returns 0
               //registerValue = sx127xDevice.ReadRegister(RegVersion);

               // Using low level approach works
               /*
               var txBuffer = new byte[] { RegVersion, 0x0 };
               var rxBuffer = new byte[] { 0x0, 0x0 };
               Console.WriteLine("spiBus.ExchangeData...1");
               spiBus.ExchangeData(spiPeriphChipSelect, ChipSelectMode.ActiveLow, txBuffer, rxBuffer);
               Console.WriteLine("spiBus.ExchangeData...2");
               registerValue = rxBuffer[1];
               */

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
