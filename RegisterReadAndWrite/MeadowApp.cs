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
		private ISpiBus SpiBus;
      private SpiPeripheral Rfm9XLoraModem;
      private IDigitalOutputPort ChipSelectGpioPin;
		private IDigitalOutputPort ResetGpioPin;
		private const byte RegisterAddressReadMask = 0X7f;
		private const byte RegisterAddressWriteMask = 0x80;

		public Rfm9XDevice(IIODevice device, ISpiBus spiBus, IPin chipSelectPin, IPin resetPin)
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

		public Byte RegisterReadByteB(byte address)
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

		public void RegisterWriteByteB(byte address, byte value)
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

		public void RegisterWriteB(byte address, [ReadOnlyArray()] byte[] bytes)
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

      public MeadowApp()
      {
			Console.WriteLine("Starting devMobile.IoT.Rfm9x.ReadAndWrite");

			ISpiBus spiBus = Device.CreateSpiBus(500);
         if (spiBus == null)
         {
            Console.WriteLine("spiBus == null");
         }

			rfm9XDevice = new Rfm9XDevice(Device, spiBus, Device.Pins.D09, Device.Pins.D10);

			while (true)
         {
				rfm9XDevice.RegisterDump();

				Byte regOpModeA = rfm9XDevice.RegisterReadByteA(0x1);
				Console.WriteLine("RegOpMode A {0}", Convert.ToString(regOpModeA, 2).PadLeft(8, '0'));

				Byte regOpModeB = rfm9XDevice.RegisterReadByteA(0x1);
				Console.WriteLine("RegOpMode B {0}", Convert.ToString(regOpModeB, 2).PadLeft(8, '0'));

				Console.WriteLine("Set LoRa mode and sleep mode (write byte)");
				rfm9XDevice.RegisterWriteByteA(0x01, 0b10000000); // 

				rfm9XDevice.RegisterDump();

				Console.WriteLine("Read the preamble (read word)");
				ushort preambleA = rfm9XDevice.RegisterReadWordA(0x20);
				Console.WriteLine("PreambleA 0x{0:x2} - Bits {1}", preambleA, Convert.ToString(preambleA, 2).PadLeft(16, '0'));

				ushort preambleB = rfm9XDevice.RegisterReadWordB(0x20);
				Console.WriteLine("PreambleB 0x{0:x2} - Bits {1}", preambleB, Convert.ToString(preambleB, 2).PadLeft(16, '0'));

				Console.WriteLine("Set the preamble to 0x80 (write word)");
				rfm9XDevice.RegisterWriteWordB(0x20, 0x8000);

				Console.WriteLine("Read the centre frequency (read byte array)");
				byte[] frequencyReadBytesA = rfm9XDevice.RegisterReadA(0x06, 3);
				Console.WriteLine("Frequency A Msb 0x{0:x2} Mid 0x{1:x2} Lsb 0x{2:x2}", frequencyReadBytesA[0], frequencyReadBytesA[1], frequencyReadBytesA[2]);

				byte[] frequencyReadBytesB = rfm9XDevice.RegisterReadB(0x06, 3);
				Console.WriteLine("Frequency B Msb 0x{0:x2} Mid 0x{1:x2} Lsb 0x{2:x2}", frequencyReadBytesB[0], frequencyReadBytesB[1], frequencyReadBytesB[2]);

				Console.WriteLine("Set the centre frequency to 915MHz ( write byte array)");
				byte[] frequencyWriteBytes = { 0xE4, 0xC0, 0x00 };
				rfm9XDevice.RegisterWriteB(0x06, frequencyWriteBytes);

				rfm9XDevice.RegisterDump();

				Task.Delay(30000).Wait();
			}
		}
   }
}
