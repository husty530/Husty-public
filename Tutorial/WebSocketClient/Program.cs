﻿using System.Text;
using Husty.IO;

Console.WriteLine("attempt to connect ...");
var client = WebSocketStream.CreateClient("127.0.0.1", 8000);
Console.WriteLine("connected!");
while (!(Console.KeyAvailable && Console.ReadKey().Key is ConsoleKey.Escape) && client.IsOpened)
{
    var msg = client.Read(Encoding.UTF8);
    Console.WriteLine("<--" + msg);
    client.Write(new byte[] { 1 });
    Console.WriteLine("-->" + 1);
}
client.Close();
Console.WriteLine("completed");
Console.ReadKey();