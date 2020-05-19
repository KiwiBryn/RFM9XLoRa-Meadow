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
namespace devMobile.IoT.Rfm9x.ReceiveTransmitInterrupt
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
		private IDigitalInterruptPort InterruptGpioPin;
		private IDigitalOutputPort ResetGpioPin;
		private const byte RegisterAddressReadMask = 0X7f;
		private const byte RegisterAddressWriteMask = 0x80;

		public Rfm9XDevice(IIODevice device, ISpiBus spiBus, IPin chipSelectPin, IPin resetPin, IPin interruptPin)
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

			// Interrupt pin for RX message & TX done notification 
			InterruptGpioPin = device.CreateDigitalInputPort(interruptPin, InterruptMode.EdgeRising);
			InterruptGpioPin.Changed += InterruptGpioPin_ValueChanged;

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

		private void InterruptGpioPin_ValueChanged(object sender, DigitalInputPortEventArgs args)
		{
			byte irqFlags = this.RegisterReadByte(0x12); // RegIrqFlags
			byte numberOfBytes = 0;
			string messageText = "";
			bool transmitDone = false;

			//Console.WriteLine(string.Format("RegIrqFlags:{0}", Convert.ToString(irqFlags, 2).PadLeft(8, '0')));
			if ((irqFlags & 0b01000000) == 0b01000000)
			{
				//Console.WriteLine("Receive-Message");
				byte currentFifoAddress = this.RegisterReadByte(0x10); // RegFifiRxCurrent
				this.RegisterWriteByte(0x0d, currentFifoAddress); // RegFifoAddrPtr

				numberOfBytes = this.RegisterReadByte(0x13); // RegRxNbBytes
				byte[] messageBytes = this.RegisterRead(0x00, numberOfBytes); // RegFifo
				messageText = UTF8Encoding.UTF8.GetString(messageBytes);
			}

			if ((irqFlags & 0b00001000) == 0b00001000)  // TxDone
			{
				this.RegisterWriteByte(0x01, 0b10000101); // RegOpMode set LoRa & RxContinuous
				transmitDone = true;
			}

			this.RegisterWriteByte(0x40, 0b00000000); // RegDioMapping1 0b00000000 DI0 RxReady & TxReady
			this.RegisterWriteByte(0x12, 0xff);// RegIrqFlags
			if (numberOfBytes > 0)
			{
				Console.WriteLine("Received {0} byte message {1}", numberOfBytes, messageText);
			}
			if(transmitDone)
			{
				Console.WriteLine("Transmit-Done");
			}
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

		public byte[] RegisterRead(byte address, int length)
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
				byte registerValue = this.RegisterReadByte(registerIndex);

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
			Console.WriteLine("Starting devMobile.IoT.Rfm9x.ReceiveTransmitInterrupt");

			ISpiBus spiBus = Device.CreateSpiBus(500);
			if (spiBus == null)
			{
				Console.WriteLine("spiBus == null");
			}

			rfm9XDevice = new Rfm9XDevice(Device, spiBus, Device.Pins.D09, Device.Pins.D10, Device.Pins.D12);

			// Put device into LoRa + Sleep mode
			rfm9XDevice.RegisterWriteByte(0x01, 0b10000000); // RegOpMode 

			// Set the frequency to 915MHz
			byte[] frequencyWriteBytes = { 0xE4, 0xC0, 0x00 }; // RegFrMsb, RegFrMid, RegFrLsb
			rfm9XDevice.RegisterWrite(0x06, frequencyWriteBytes);

			rfm9XDevice.RegisterWriteByte(0x0F, 0x0); // RegFifoRxBaseAddress 

			rfm9XDevice.RegisterWriteByte(0x09, 0b10000000); // RegPaConfig

			rfm9XDevice.RegisterWriteByte(0x01, 0b10000101); // RegOpMode set LoRa & RxContinuous

			while (true)
			{
				rfm9XDevice.RegisterWriteByte(0x0E, 0x0); // RegFifoTxBaseAddress 

				// Set the Register Fifo address pointer
				rfm9XDevice.RegisterWriteByte(0x0D, 0x0); // RegFifoAddrPtr 

				string messageText = string.Format("Hello from {0} ! {1}", Environment.MachineName, MessageCount);
				MessageCount -= 1;

				// load the message into the fifo
				byte[] messageBytes = UTF8Encoding.UTF8.GetBytes(messageText);
				rfm9XDevice.RegisterWrite(0x0, messageBytes); // RegFifo 

				// Set the length of the message in the fifo
				rfm9XDevice.RegisterWriteByte(0x22, (byte)messageBytes.Length); // RegPayloadLength
				rfm9XDevice.RegisterWriteByte(0x40, 0b01000000); // RegDioMapping1 0b00000000 DI0 RxReady & TxReady
				rfm9XDevice.RegisterWriteByte(0x01, 0b10000011); // RegOpMode 

				Console.WriteLine("Sending {0} bytes message {1}", messageBytes.Length, messageText);

				Task.Delay(10000).Wait();
			}
		}
	}
}