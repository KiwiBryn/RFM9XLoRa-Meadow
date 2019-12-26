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
namespace devMobile.IoT.Rfm9x.ReadAndWrite
{
   using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices.WindowsRuntime;
    using System.Threading.Tasks;

   using Meadow;
   using Meadow.Devices;
   using Meadow.Hardware;

   public sealed class Rfm9XDevice
   {
      private SpiPeripheral Rfm9XLoraModem;
      private IDigitalOutputPort ChipSelectGpioPin;
		private const byte RegisterAddressReadMask = 0X7f;
		private const byte RegisterAddressWriteMask = 0x80;

		public Rfm9XDevice(IIODevice device, ISpiBus spiBus, IPin chipSelectPin, IPin resetPin)
      {
         // Chip select pin configuration
         ChipSelectGpioPin = device.CreateDigitalOutputPort(chipSelectPin, initialState: true);
         if (ChipSelectGpioPin == null)
         {
            Console.WriteLine("ChipSelectGpioPin == null");
         }
			
         // Factory reset pin configuration
         IDigitalOutputPort resetGpioPin = device.CreateDigitalOutputPort(resetPin);
         if (resetGpioPin == null)
         {
            Console.WriteLine("resetGpioPin == null");
         }
         resetGpioPin.State = false;
         Task.Delay(10);
         resetGpioPin.State = true;
         Task.Delay(10);
			
			Rfm9XLoraModem = new SpiPeripheral(spiBus, ChipSelectGpioPin);
         if (Rfm9XLoraModem == null)
         {
            Console.WriteLine("Rfm9XLoraModem == null");
         }
      }

      public Byte RegisterReadByte(byte address)
      {
         byte[] writeBuffer = new byte[] { address &= RegisterAddressReadMask };
			Debug.Assert(Rfm9XLoraModem != null);

			byte[] readBuffer = Rfm9XLoraModem.WriteRead(writeBuffer, 2);

         return readBuffer[1];
      }

		public ushort RegisterReadWord(byte address)
		{
			byte[] writeBuffer = new byte[] { address &= RegisterAddressReadMask };
			Debug.Assert(Rfm9XLoraModem != null);

			byte[] readBuffer = Rfm9XLoraModem.WriteRead(writeBuffer, 3);

			return (ushort)(readBuffer[2] + (readBuffer[1] << 8));
		}

		public byte[] RegisterRead(byte address, int length)
		{
			byte[] writeBuffer = new byte[] { address &= RegisterAddressReadMask };
			byte[] repyBuffer = new byte[length + 1];
			Debug.Assert(Rfm9XLoraModem != null);

			byte[] readBuffer = Rfm9XLoraModem.WriteRead(writeBuffer, (ushort)repyBuffer.Length);

			Array.Copy(readBuffer, 1, repyBuffer, 0, length);

			return repyBuffer;
		}

		public void RegisterWriteByte(byte address, byte value)
		{
			byte[] writeBuffer = new byte[] { address |= RegisterAddressWriteMask, value };
			Debug.Assert(Rfm9XLoraModem != null);

			Rfm9XLoraModem.WriteBytes(writeBuffer);
		}

		public void RegisterWriteWord(byte address, ushort value)
		{
			byte[] valueBytes = BitConverter.GetBytes(value);
			byte[] writeBuffer = new byte[] { address |= RegisterAddressWriteMask, valueBytes[0], valueBytes[1] };
			Debug.Assert(Rfm9XLoraModem != null);

			Rfm9XLoraModem.WriteBytes(writeBuffer);
		}
		public void RegisterWrite(byte address, [ReadOnlyArray()] byte[] bytes)
		{
			byte[] writeBuffer = new byte[1 + bytes.Length];
			Debug.Assert(Rfm9XLoraModem != null);

			Array.Copy(bytes, 0, writeBuffer, 1, bytes.Length);
			writeBuffer[0] = address |= RegisterAddressWriteMask;

			Rfm9XLoraModem.WriteBytes(writeBuffer);
		}

		public void RegisterDump()
		{
			Console.WriteLine("Register dump");
			for (byte registerIndex = 0; registerIndex <= 0x42; registerIndex++)
			{
				byte registerValue = this.RegisterReadByte(registerIndex);

				Console.WriteLine("Register 0x{0:x2} - Value 0X{1:x2} - Bits {2}", registerIndex, registerValue, Convert.ToString(registerValue, 2).PadLeft(8, '0'));
			}
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

			rfm9XDevice = new Rfm9XDevice(Device, spiBus, Device.Pins.D09, Device.Pins.D11);

			while (true)
         {
				rfm9XDevice.RegisterDump();

				Byte regOpMode = rfm9XDevice.RegisterReadByte(0x1);

				Console.WriteLine("Set LoRa mode and sleep mode (write byte)");
				rfm9XDevice.RegisterWriteByte(0x01, 0b10000000); // 

				Console.WriteLine("Read the preamble (read word)");
				ushort preamble = rfm9XDevice.RegisterReadWord(0x20);
				Console.WriteLine("Preamble 0x{0:x2} - Bits {1}", preamble, Convert.ToString(preamble, 2).PadLeft(16, '0'));

				Console.WriteLine("Set the preamble to 0x80 (write word)");
				rfm9XDevice.RegisterWriteWord(0x20, 0x80);

				Console.WriteLine("Read the centre frequency (read byte array)");
				byte[] frequencyReadBytes = rfm9XDevice.RegisterRead(0x06, 3);
				Console.WriteLine("Frequency Msb 0x{0:x2} Mid 0x{1:x2} Lsb 0x{2:x2}", frequencyReadBytes[0], frequencyReadBytes[1], frequencyReadBytes[2]);

				Console.WriteLine("Set the centre frequency to 916MHz ( write byte array)");
				byte[] frequencyWriteBytes = { 0xE4, 0xC0, 0x00 };
				rfm9XDevice.RegisterWrite(0x06, frequencyWriteBytes);

				rfm9XDevice.RegisterDump();

				Task.Delay(30000).Wait();
			}
		}
   }
}
