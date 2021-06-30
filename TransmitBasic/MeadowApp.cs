//---------------------------------------------------------------------------------
// Copyright (c) August 2018, devMobile Software
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
namespace devMobile.IoT.Rfm9x.TransmitBasic
{
   using System;
   using System.Diagnostics;
   using System.Runtime.InteropServices.WindowsRuntime;
   using System.Text;
   using System.Threading.Tasks;

   using Meadow;
   using Meadow.Devices;
   using Meadow.Hardware;

   public sealed class Rfm9XDevice
   {
      private ISpiBus SpiBus;
      private SpiPeripheral Rfm9XLoraModem;
      private IDigitalOutputPort ChipSelectGpioPin;
      private IDigitalOutputPort ResetGpioPin;
      private const byte RegisterAddressReadMask = 0X7f;
      private const byte RegisterAddressWriteMask = 0x80;

      public Rfm9XDevice(IMeadowDevice device, ISpiBus spiBus, IPin chipSelectPin, IPin resetPin)
      {
         SpiBus = spiBus;

         // Chip select pin configuration
         ChipSelectGpioPin = device.CreateDigitalOutputPort(chipSelectPin, initialState: true);
         if (ChipSelectGpioPin == null)
         {
            Console.WriteLine("ChipSelectGpioPin == null");
         }

         Rfm9XLoraModem = new SpiPeripheral(spiBus, ChipSelectGpioPin);
         if (Rfm9XLoraModem == null)
         {
            Console.WriteLine("Rfm9XLoraModem == null");
         }

         // Factory reset pin configuration
         ResetGpioPin = device.CreateDigitalOutputPort(resetPin);
         if (ResetGpioPin == null)
         {
            Console.WriteLine("ResetGpioPin == null");
         }
         ResetGpioPin.State = false;
         Task.Delay(10);
         ResetGpioPin.State = true;
         Task.Delay(10);
      }

      public Byte RegisterReadByteA(byte address)
      {
         Debug.Assert(Rfm9XLoraModem != null);

         return Rfm9XLoraModem.ReadRegister(address);
      }

      public Byte RegisterReadByte(byte address)
      {
         byte[] writeBuffer = new byte[] { address &= RegisterAddressReadMask, 0x0 };
         byte[] readBuffer = new byte[writeBuffer.Length];
         Debug.Assert(Rfm9XLoraModem != null);

         SpiBus.ExchangeData(this.ChipSelectGpioPin, ChipSelectMode.ActiveLow, writeBuffer, readBuffer);

         return readBuffer[1];
      }

      public ushort RegisterReadWordA(byte address)
      {
         Debug.Assert(Rfm9XLoraModem != null);

         return Rfm9XLoraModem.ReadUShort(address);
      }

      public ushort RegisterReadWordB(byte address)
      {
         byte[] writeBuffer = new byte[] { address &= RegisterAddressReadMask, 0x0, 0x0 };
         byte[] readBuffer = new byte[writeBuffer.Length];
         Debug.Assert(Rfm9XLoraModem != null);

         SpiBus.ExchangeData(this.ChipSelectGpioPin, ChipSelectMode.ActiveLow, writeBuffer, readBuffer);

         return (ushort)(readBuffer[2] + (readBuffer[1] << 8));
      }

      public byte[] RegisterReadA(byte address, int length)
      {
         Debug.Assert(Rfm9XLoraModem != null);

         return Rfm9XLoraModem.ReadRegisters(address, (ushort)length);
      }

      public byte[] RegisterReadB(byte address, int length)
      {
         byte[] writeBuffer = new byte[length + 1];
         byte[] readBuffer = new byte[writeBuffer.Length];
         byte[] replyBuffer = new byte[length];
         Debug.Assert(Rfm9XLoraModem != null);

         writeBuffer[0] = address &= RegisterAddressReadMask;

         SpiBus.ExchangeData(this.ChipSelectGpioPin, ChipSelectMode.ActiveLow, writeBuffer, readBuffer);

         Array.Copy(readBuffer, 1, replyBuffer, 0, length);

         return replyBuffer;
      }

      public void RegisterWriteByteA(byte address, byte value)
      {
         Debug.Assert(Rfm9XLoraModem != null);

         Rfm9XLoraModem.WriteRegister(address |= RegisterAddressWriteMask, value);
      }

      public void RegisterWriteByte(byte address, byte value)
      {
         byte[] writeBuffer = new byte[] { address |= RegisterAddressWriteMask, value };
         byte[] readBuffer = new byte[writeBuffer.Length];
         Debug.Assert(Rfm9XLoraModem != null);

         SpiBus.ExchangeData(this.ChipSelectGpioPin, ChipSelectMode.ActiveLow, writeBuffer, readBuffer);
      }

      public void RegisterWriteWordA(byte address, ushort value)
      {
         Debug.Assert(Rfm9XLoraModem != null);

         Rfm9XLoraModem.WriteUShort(address |= RegisterAddressWriteMask, value);
      }

      public void RegisterWriteWordB(byte address, ushort value)
      {
         byte[] valueBytes = BitConverter.GetBytes(value);
         byte[] writeBuffer = new byte[] { address |= RegisterAddressWriteMask, valueBytes[0], valueBytes[1] };
         byte[] readBuffer = new byte[writeBuffer.Length];
         Debug.Assert(Rfm9XLoraModem != null);

         SpiBus.ExchangeData(this.ChipSelectGpioPin, ChipSelectMode.ActiveLow, writeBuffer, readBuffer);
      }

      public void RegisterWriteA(byte address, [ReadOnlyArray()] byte[] bytes)
      {
         Debug.Assert(Rfm9XLoraModem != null);

         Rfm9XLoraModem.WriteRegisters(address |= RegisterAddressWriteMask, bytes);
      }

      public void RegisterWrite(byte address, [ReadOnlyArray()] byte[] bytes)
      {
         byte[] writeBuffer = new byte[1 + bytes.Length];
         byte[] readBuffer = new byte[writeBuffer.Length];
         Debug.Assert(Rfm9XLoraModem != null);

         Array.Copy(bytes, 0, writeBuffer, 1, bytes.Length);
         writeBuffer[0] = address |= RegisterAddressWriteMask;

         SpiBus.ExchangeData(this.ChipSelectGpioPin, ChipSelectMode.ActiveLow, writeBuffer, readBuffer);
      }

      public void RegisterDump()
      {
         Console.WriteLine("Register dump");
         for (byte registerIndex = 0; registerIndex <= 0x42; registerIndex++)
         {
            byte registerValue = this.RegisterReadByteA(registerIndex);

            Console.WriteLine("Register 0x{0:x2} - Value 0X{1:x2} - Bits {2}", registerIndex, registerValue, Convert.ToString(registerValue, 2).PadLeft(8, '0'));
         }
      }
   }

   public class MeadowApp : App<F7Micro, MeadowApp>
   {
      private Rfm9XDevice rfm9XDevice;
      private byte MessageCount = Byte.MaxValue;

      public MeadowApp()
      {
         Console.WriteLine("Starting devMobile.IoT.Rfm9x.TransmitBasic");

         ISpiBus spiBus = Device.CreateSpiBus(500);
         if (spiBus == null)
         {
            Console.WriteLine("spiBus == null");
         }

         rfm9XDevice = new Rfm9XDevice(Device, spiBus, Device.Pins.D09, Device.Pins.D10);

         // Put device into LoRa + Sleep mode
         rfm9XDevice.RegisterWriteByte(0x01, 0b10000000); // RegOpMode 

         // Set the frequency to 915MHz
         byte[] frequencyWriteBytes = { 0xE4, 0xC0, 0x00 }; // RegFrMsb, RegFrMid, RegFrLsb
         rfm9XDevice.RegisterWrite(0x06, frequencyWriteBytes);

         // More power PA Boost
         rfm9XDevice.RegisterWriteByte(0x09, 0b10000000); // RegPaConfig

         while (true)
         {
            rfm9XDevice.RegisterWriteByte(0x0E, 0x0); // RegFifoTxBaseAddress 

            // Set the Register Fifo address pointer
            rfm9XDevice.RegisterWriteByte(0x0D, 0x0); // RegFifoAddrPtr 

            string messageText = string.Format("Hello from {0} ! {1}", Environment.MachineName, MessageCount);
            MessageCount -= 1;

            // load the message into the fifo
            byte[] messageBytes = UTF8Encoding.UTF8.GetBytes(messageText);
            rfm9XDevice.RegisterWrite(0x0, messageBytes);

            // Set the length of the message in the fifo
            rfm9XDevice.RegisterWriteByte(0x22, (byte)messageBytes.Length); // RegPayloadLength

            Console.WriteLine("Sending {0} bytes message {1}", messageBytes.Length, messageText);
            /// Set the mode to LoRa + Transmit
            rfm9XDevice.RegisterWriteByte(0x01, 0b10000011); // RegOpMode 

            // Wait until send done, no timeouts in PoC
            Console.WriteLine("Send-wait");
            byte IrqFlags = rfm9XDevice.RegisterReadByte(0x12); // RegIrqFlags
            while ((IrqFlags & 0b00001000) == 0)  // wait until TxDone cleared
            {
               Task.Delay(10).Wait();
               IrqFlags = rfm9XDevice.RegisterReadByte(0x12); // RegIrqFlags
               Console.Write(".");
            }
            Console.WriteLine("");
            rfm9XDevice.RegisterWriteByte(0x12, 0b00001000); // clear TxDone bit
            Console.WriteLine("Send-Done");

            Task.Delay(30000).Wait();
         }
      }
   }
}
