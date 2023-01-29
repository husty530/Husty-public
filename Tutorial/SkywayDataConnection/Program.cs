﻿using Husty.SkywayGateway;

var apiKey = "API_KEY";         // get from https://webrtc.ecl.ntt.com/
var localId = "Player1";
var remoteId = "Player2";
var mode = "listen";            // listen or call

// create my peer object
await using var peer = await Peer.CreateNewAsync(apiKey, localId);
Console.WriteLine("My ID = " + peer.PeerId);

// create data channel
await using var channel = await peer.CreateDataChannelAsync();
channel.Closed.Subscribe(_ => Console.WriteLine("channel was disconnected!"));

// listen or call
using var stream = mode is "listen"
    ? await channel.ListenAsync()
    : await channel.CallConnectionAsync(remoteId);

// show and confirm connection information
var info = channel.ConnectionInfo;
Console.WriteLine($"Local  : {info.LocalEP.Address}:{info.LocalEP.Port}");
Console.WriteLine($"Remote : {info.RemoteEP.Address}:{info.RemoteEP.Port}");
var alive = await channel.ConfirmAliveAsync();
Console.WriteLine(alive is true ? "connected!" : "failed to connect!");
Console.WriteLine();

// loop
var t = Task.Run(async () =>
{
    var count = 0;
    while (true)
    {
        // send
        var msg = $"Message {count++} from {localId}.";
        await stream.WriteStringAsync(msg);
        Console.WriteLine("---> : " + msg);
        // receive
        var rcv = await stream.ReadStringAsync();
        Console.WriteLine("<--- : " + rcv);
        await Task.Delay(1000);
    }
});

Console.WriteLine("press ESC key to finish ...");
while (Console.ReadKey().Key is not ConsoleKey.Escape)
    Thread.Sleep(50);

Console.WriteLine("completed.");