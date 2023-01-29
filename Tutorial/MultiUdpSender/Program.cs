﻿using Husty.Extensions;
using Husty.IO;

var sender = new UdpSender(3000, 3001);
var data = "DATA";
Task.Run(async () =>
{
    while (true)
    {
        await sender.SendAsync(null, data);
        Console.WriteLine("!");
        Thread.Sleep(1000);
    }
});
Console.WriteLine("Press Enter key to exit...");
ConsoleEx.WaitKey(ConsoleKey.Enter);