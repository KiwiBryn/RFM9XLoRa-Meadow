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
      IDigitalOutputPort chipSelectGpioPin;
      IDigitalOutputPort resetGpioPin;

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
            chipSelectGpioPin = Device.CreateDigitalOutputPort(Device.Pins.D09, initialState:true);
            if (chipSelectGpioPin == null)
            {
               Console.WriteLine("chipSelectGpioPin == null");
            }

            Console.WriteLine("sx127xDevice Device...");
            sx127xDevice = new SpiPeripheral(spiBus, chipSelectGpioPin);
            if (sx127xDevice == null)
            {
               Console.WriteLine("sx127xDevice == null");
            }

            // Factory reset pin configuration
            resetGpioPin = Device.CreateDigitalOutputPort(Device.Pins.D10, true);
            if (sx127xDevice == null)
            {
               Console.WriteLine("resetPin == null");
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

               // Works May 2020
               registerValue = sx127xDevice.ReadRegister(RegVersion);

               // Works May 2020
               /*
               var txBuffer = new byte[] { RegVersion, 0x0 };
               var rxBuffer = new byte[txBuffer.Length];
               Console.WriteLine("spiBus.ExchangeData...1");
               spiBus.ExchangeData(chipSelectGpioPin, ChipSelectMode.ActiveLow, txBuffer, rxBuffer);
               Console.WriteLine("spiBus.ExchangeData...2");
               registerValue = rxBuffer[1];
               */

               // Doesn't work May 2020 returns  Register 0x42 - Value 0X2d - Bits 00101101 
               /*
               byte[] txBuffer = new byte[] { RegVersion, 0x0 };
               Console.WriteLine("spiBus.WriteRead...1");
               byte[] rxBuffer = sx127xDevice.WriteRead(txBuffer, (ushort)txBuffer.Length);
               Console.WriteLine("spiBus.WriteRead...2");
               registerValue = rxBuffer[1];
               */

               Console.WriteLine("Register 0x{0:x2} - Value 0X{1:x2} - Bits {2}", RegVersion, registerValue, Convert.ToString(registerValue, 2).PadLeft(8, '0'));
            }
            catch (Exception ex)
            {
               Console.WriteLine("ReadDeviceID " + ex.Message);
            }

            Task.Delay(10000).Wait();
         }
      }
   }
}
