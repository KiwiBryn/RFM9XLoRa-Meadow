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
namespace devMobile.IoT.Rfm9x
{
	using System;
	using System.Diagnostics;
	using System.Runtime.InteropServices.WindowsRuntime;

	using Meadow.Hardware;

	public sealed class RegisterManager
	{
		ISpiBus SpiBus = null;
		private IDigitalOutputPort ChipSelectGpioPin = null;
		private SpiPeripheral Rfm9XLoraModem = null;
		private const byte RegisterAddressReadMask = 0X7f;
		private const byte RegisterAddressWriteMask = 0x80;

		public RegisterManager(IIODevice device, ISpiBus spiBus, IPin chipSelectPin)
		{
			this.SpiBus = spiBus;

			// Chip select pin configuration
			ChipSelectGpioPin = device.CreateDigitalOutputPort(chipSelectPin, initialState: true);

			Rfm9XLoraModem = new SpiPeripheral(spiBus, ChipSelectGpioPin);
		}

		public Byte ReadByte(byte address)
		{
			byte[] writeBuffer = new byte[] { address &= RegisterAddressReadMask, 0x0 };
			byte[] readBuffer = new byte[writeBuffer.Length];
			Debug.Assert(Rfm9XLoraModem != null);

			SpiBus.ExchangeData(this.ChipSelectGpioPin, ChipSelectMode.ActiveLow, writeBuffer, readBuffer);

			return readBuffer[1];
		}

		public byte[] Read(byte address, int length)
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

		public void WriteByte(byte address, byte value)
		{
			byte[] writeBuffer = new byte[] { address |= RegisterAddressWriteMask, value };
			byte[] readBuffer = new byte[writeBuffer.Length];
			Debug.Assert(Rfm9XLoraModem != null);

			SpiBus.ExchangeData(this.ChipSelectGpioPin, ChipSelectMode.ActiveLow, writeBuffer, readBuffer);
		}

		public void Write(byte address, [ReadOnlyArray()] byte[] bytes)
		{
			byte[] writeBuffer = new byte[1 + bytes.Length];
			byte[] readBuffer = new byte[writeBuffer.Length];
			Debug.Assert(Rfm9XLoraModem != null);

			Array.Copy(bytes, 0, writeBuffer, 1, bytes.Length);
			writeBuffer[0] = address |= RegisterAddressWriteMask;

			SpiBus.ExchangeData(this.ChipSelectGpioPin, ChipSelectMode.ActiveLow, writeBuffer, readBuffer);
		}

		public void Dump(byte start, byte finish)
		{
			Console.WriteLine("Register dump");
			for (byte registerIndex = start; registerIndex <= finish; registerIndex++)
			{
				byte registerValue = this.ReadByte(registerIndex);

				Console.WriteLine("Register 0x{0:x2} - Value 0X{1:x2} - Bits {2}", registerIndex, registerValue, Convert.ToString(registerValue, 2).PadLeft(8, '0'));
			}
		}
	}
}
