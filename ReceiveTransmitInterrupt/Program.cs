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
   using System.Threading;

   using Meadow;

   class Program
   {
      static IApp app;
      public static void Main(string[] args)
      {
         if (args.Length > 0 && args[0] == "--exitOnDebug") return;

         // instantiate and run new meadow app
         app = new MeadowApp();

         Thread.Sleep(Timeout.Infinite);
      }
   }
}
