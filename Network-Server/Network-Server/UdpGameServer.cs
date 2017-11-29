namespace Network_Server
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;

    class UdpGameServer
    {
        private UdpClient server;

        private bool running = true;

        private GameServer tcpGameServer;

        List<IPEndPoint> connectedPlayers = new List<IPEndPoint>();

        List<IPEndPoint> playersToConnect = new List<IPEndPoint>();

        List<IPEndPoint> playersToRemove = new List<IPEndPoint>();

        private List<GamePacket> packetsToSend = new List<GamePacket>();
        public UdpGameServer(int serverPort)
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, serverPort);
            this.server = new UdpClient(endPoint);
            this.tcpGameServer = new GameServer(serverPort);
            Thread t = new Thread(this.HandleConnections);
            t.Start();
            
        }

        private void HandleConnections()
        {
            this.tcpGameServer.Lobby();
        }
        

        public void Lobby()
        {
            Console.WriteLine("Server booted");
            while (this.running)
            {
                if (this.server.Available > 0)
                {
                    try
                    {
                        this.server.BeginReceive(this.DataReceived, this.server);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                    Thread.Sleep(10);
                }
                if (this.playersToConnect.Count > 0)
                {
                    try
                    {
                        foreach (var ipEndPoint in this.playersToConnect)
                        {
                            // broadcast the new player id and pos to all previous connected players and vica versa 
                            this.connectedPlayers.Add(ipEndPoint);
                        }
                        this.playersToConnect.Clear();

                    }
                    catch
                    {
                        // ignored
                    }
                }
                try
                {
                    bool doneSending = false;
                    if (this.connectedPlayers.Count != 0)
                    {
                        foreach (var connectedPlayer in this.connectedPlayers)
                        {
                            List<GamePacket> toremove = new List<GamePacket>();
                            foreach (var gamePacket in this.packetsToSend)
                            {
                                string[] split1 = gamePacket.Message.Split(':');
                                foreach (var packet in this.packetsToSend)
                                {
                                    if (packet != gamePacket)
                                    {
                                        string[] split2 = packet.Message.Split(':');
                                        if (split2[0] == split1[0])
                                        {
                                            toremove.Add(gamePacket);
                                            break;
                                        }
                                    }
                                }
                            }
                            foreach (var gamePacket in toremove)
                            {
                                this.packetsToSend.Remove(gamePacket);
                            }

                            foreach (var gamePacket in this.packetsToSend)
                            {
                                try
                                {
                                    connectedPlayer.Serialize();
                                    byte[] toSend = Encoding.UTF8.GetBytes(gamePacket.ToJson());
                                    this.server.SendAsync(toSend, toSend.Length, connectedPlayer);
                                    
                                }
                                catch (Exception e)
                                {
                                    this.playersToRemove.Add(connectedPlayer);
                                    Console.WriteLine(e);
                                }
                            }
                        }

                        doneSending = true;
                    }

                    if (doneSending)
                    {
                        this.packetsToSend.Clear();
                    }
                }
                catch
                {
                    // ignored
                }
                Thread.Sleep(10);
                if (this.playersToRemove.Count > 0)
                {
                    foreach (var ipEndPoint in this.playersToRemove)
                    {
                        this.connectedPlayers.Remove(ipEndPoint);
                    }
                    this.playersToRemove.Clear();
                }
            }
        }

        private void DataReceived(IAsyncResult ar)
        {
            try
            {
                UdpClient c = (UdpClient)ar.AsyncState;

                IPEndPoint receivedIpEndPoint = new IPEndPoint(IPAddress.Any, 0);


                Byte[] receivedBytes = c.EndReceive(ar, ref receivedIpEndPoint);

                // Convert data to UTF8 and print in console
                string receivedText = Encoding.UTF8.GetString(receivedBytes);
                this.packetsToSend.Add(GamePacket.FromJson(receivedText));

                Console.Write(receivedIpEndPoint + ": " + receivedText + Environment.NewLine);
                bool alreadyConnected = false;
                try
                {
                    foreach (var connectedPlayer in this.connectedPlayers)
                    {
                        if (receivedIpEndPoint.ToString() == connectedPlayer.ToString())
                        {
                            alreadyConnected = true;
                            break;
                        }
                    }
                    if (!alreadyConnected)
                    {
                        this.playersToConnect.Add(receivedIpEndPoint);
                    }
                }
                catch
                {
                    // ignored
                }

                // Restart listening for udp data packages
                c.BeginReceive(this.DataReceived, ar.AsyncState);

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}