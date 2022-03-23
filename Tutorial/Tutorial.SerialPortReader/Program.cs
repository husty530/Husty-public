﻿using System;
using Husty.IO;

namespace Tutorial.SerialPortReader
{
    internal class Program
    {
        static void Main(string[] args)
        {

            // get ports
            var names = SerialPort.GetPortNames();
            if (names.Length is 0) throw new Exception("find no port!");

            // access first found port
            using var port = new SerialPort(names[0], 115250);

            // read messages until key interrupt
            while(true)
            {
                Console.WriteLine(port.ReadLine());
                if (Console.KeyAvailable)
                    if (Console.ReadKey().Key is ConsoleKey.Q)
                        break;
            }
            Console.WriteLine("completed.");

        }
    }
}
