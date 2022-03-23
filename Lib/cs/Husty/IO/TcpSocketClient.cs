﻿using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Husty.IO
{
    /// <summary>
    /// Tcp socket client class
    /// </summary>
    public sealed class TcpSocketClient : ICommunicator
    {

        // ------ fields ------ //

        private TcpClient _client1;
        private TcpClient _client2;
        private readonly Task _connectionTask;


        // ------ properties ------ //

        public int ReadTimeout { set; get; } = -1;

        public int WriteTimeout { set; get; } = -1;


        // ------ constructors ------ //

        public TcpSocketClient(string ip, int port)
        {
            _client1 = new();
            _connectionTask = Task.Run(() =>
            {
                try
                {
                    _client1 = new(ip, port);
                }
                catch
                {
                    throw new Exception("failed to connect!");
                }
            });
        }

        public TcpSocketClient(string recvIp, int recvPort, string sendIp, int sendPort)
        {
            _client1 = new();
            _client2 = new();
            _connectionTask = Task.Run(() =>
            {
                try
                {
                    _client1 = new(sendIp, sendPort);
                    _client2 = new(recvIp, recvPort);
                }
                catch
                {
                    throw new Exception("failed to connect!");
                }
            });
        }


        // ------ public methods ------ //

        public BidirectionalDataStream GetStream()
        {
            _connectionTask.Wait();
            if (_client1 is not null && _client2 is null)
            {
                var stream = _client1.GetStream();
                return new BidirectionalDataStream(stream, stream, WriteTimeout, ReadTimeout);
            }
            else if (_client1 is not null && _client2 is not null)
            {
                var stream1 = _client1.GetStream();
                var stream2 = _client2.GetStream();
                return new BidirectionalDataStream(stream1, stream2, WriteTimeout, ReadTimeout);
            }
            else
            {
                throw new Exception("failed to get stream!");
            }
        }

        public async Task<BidirectionalDataStream> GetStreamAsync()
        {
            return await Task.FromResult(GetStream()).ConfigureAwait(false);
        }

        public void Dispose()
        {
            _client1?.Dispose();
            _client2?.Dispose();
        }

    }
}
