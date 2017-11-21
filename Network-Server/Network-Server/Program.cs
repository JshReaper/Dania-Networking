using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Network_Server
{
    using System.Net;
    using System.Net.Sockets;

    class Program
    {
        /// <summary>
        /// The server.
        /// </summary>
        private static UdpGameServer server;

        /// <summary>
        /// The main entry point of the application.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        private static void Main(string[] args)
        {
            server = new UdpGameServer(42424);
            server.Lobby();
        }
    }

    class UdpGameServer
    {
        private UdpClient server;

        private bool running = true;

        List<IPEndPoint> connectedPlayers = new List<IPEndPoint>();

        List<IPEndPoint> playersToConnect = new List<IPEndPoint>();

        private List<GamePacket> packetsToSend = new List<GamePacket>();
        public UdpGameServer(int serverPort)
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, serverPort);
            this.server = new UdpClient(endPoint);
        }

        public void Lobby()
        {
            Console.WriteLine("Server booted");
            while (this.running)
            {
                if (this.server.Available > 0)
                {
                    this.server.BeginReceive(this.DataReceived, this.server);
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
                foreach (var connectedPlayer in this.connectedPlayers)
                {
                    foreach (var gamePacket in this.packetsToSend)
                    {
                        connectedPlayer.Serialize();
                        this.server.Send(Encoding.ASCII.GetBytes(gamePacket.ToJson()), 2, connectedPlayer);
                    }
                }
            }
        }
        private void DataReceived(IAsyncResult ar)
        {
            UdpClient c = (UdpClient)ar.AsyncState;
            IPEndPoint receivedIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
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

            Byte[] receivedBytes = c.EndReceive(ar, ref receivedIpEndPoint);
            
            // Convert data to ASCII and print in console
            string receivedText = ASCIIEncoding.ASCII.GetString(receivedBytes);
            this.packetsToSend.Add(GamePacket.FromJson(receivedText));
            Console.Write(receivedIpEndPoint + ": " + receivedText + Environment.NewLine);

            // Restart listening for udp data packages
            c.BeginReceive(DataReceived, ar.AsyncState);
        }
    }
}
