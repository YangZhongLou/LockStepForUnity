using System;
using System.Threading;
using LockStepLib.Transport;

namespace Tests
{
    public static class TcpTransportTests
    {
        public static void Run()
        {
            Console.WriteLine("--- TcpTransport Tests ---");

            ConnectDisconnect();
            SendReceive();
        }

        static void ConnectDisconnect()
        {
            TestRunner.StartSection("Connect/Disconnect");

            var server = new TcpTransport();
            var client = new TcpTransport();

            IConnection serverConn = null;
            IConnection clientConn = null;
            string disconnectReason = null;

            server.OnConnected += conn => { serverConn = conn; };
            server.OnDisconnected += (conn, reason) => { disconnectReason = reason; };
            client.OnConnected += conn => { clientConn = conn; };

            server.StartServer(19550, 10);
            Thread.Sleep(50);

            client.Connect("127.0.0.1", 19550);

            // 轮询等待连接
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (serverConn == null && sw.ElapsedMilliseconds < 3000)
            {
                server.Update();
                client.Update();
                Thread.Sleep(10);
            }

            TestRunner.Assert(serverConn != null, "server got connection");
            TestRunner.Assert(clientConn != null, "client got connection");
            TestRunner.AssertEqual(true, server.ConnectionCount > 0, "server has connections");

            // 断开
            if (serverConn != null)
            {
                disconnectReason = "not disconnected";
                server.Disconnect(serverConn);
                Thread.Sleep(50);
                server.Update();
                client.Update();
            }

            server.Shutdown();
            client.Shutdown();
        }

        static void SendReceive()
        {
            TestRunner.StartSection("Send/Receive");

            var server = new TcpTransport();
            var client = new TcpTransport();

            IConnection serverConn = null;
            byte[] receivedData = null;
            int receivedFromId = -1;

            server.OnConnected += conn => serverConn = conn;
            server.OnDataReceived += (conn, data) =>
            {
                receivedFromId = conn.Id;
                receivedData = new byte[data.Count];
                Array.Copy(data.Array, data.Offset, receivedData, 0, data.Count);
            };

            client.OnConnected += conn =>
            {
                var payload = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD };
                client.Send(conn, payload, 0, payload.Length, SendOptions.Reliable);
            };

            server.StartServer(19551, 10);
            Thread.Sleep(50);

            client.Connect("127.0.0.1", 19551);

            // 轮询等待数据到达
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (receivedData == null && sw.ElapsedMilliseconds < 3000)
            {
                server.Update();
                client.Update();
                Thread.Sleep(10);
            }

            TestRunner.Assert(receivedData != null, "server received data");
            if (receivedData != null)
            {
                TestRunner.AssertEqual(4, receivedData.Length, "received 4 bytes");
                TestRunner.AssertEqual(0xAA, (int)receivedData[0], "byte 0 = 0xAA");
                TestRunner.AssertEqual(0xDD, (int)receivedData[3], "byte 3 = 0xDD");
            }

            server.Shutdown();
            client.Shutdown();
        }
    }
}
