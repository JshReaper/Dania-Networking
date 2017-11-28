namespace Network_Server
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// The game server.
    /// </summary>
    internal class GameServer
    {
        /// <summary>
        /// The Listener.
        /// </summary>
        private readonly TcpListener listener;

        private int idToAssign;
        /// <summary>
        /// The connected clients.
        /// </summary>
        private readonly List<TcpClient> clients;

        /// <summary>
        /// The lobby of connected clients.
        /// </summary>
        private readonly List<TcpClient> lobby;

        private bool newDataAvailable;

        /// <summary>
        /// Initializes a new instance of the <see cref="GameServer"/> class.
        /// </summary>
        /// <param name="serverPort">
        /// The server port.
        /// </param>
        public GameServer(int serverPort)
        {
            this.clients = new List<TcpClient>();
            this.lobby = new List<TcpClient>();

            // Creates a Listener
            this.listener = new TcpListener(IPAddress.Any, serverPort);
            //  this.listener.AllowNatTraversal(true);
        }

        /// <summary>
        /// Starts the server.
        /// </summary>
        public void Lobby()
        {
            // A list for new connection tasks
            // ReSharper disable once CollectionNeverQueried.Local
            var ConnectionTasks = new List<Task>();
            Console.WriteLine("Lobby open");
            bool runLobby = true;
            var gamePackets = new List<GamePacket>();
            // Starts our listener
            this.listener.Start();
            // Executes the lobby
            while (runLobby)
            {
                // Waits for new connections
                if (this.listener.Pending())
                {
                    // Add new connection and say welcome
                    ConnectionTasks.Add(this.HandleNewConnection());
                }
                try
                {
                    List<TcpClient> clientsToRemove = new List<TcpClient>();
                    foreach (var client in this.lobby)
                    {
                        bool part1 = client.Client.Poll(1000, SelectMode.SelectRead);
                        bool part2 = client.Client.Available == 0;
                        bool disconnected = part2 && part1;
                        if (disconnected)
                        {
                            clientsToRemove.Add(client);
                            Console.WriteLine(this.lobby.IndexOf(client).ToString());
                            foreach (var tcpClient in this.lobby)
                            {
                                ConnectionTasks.Add(
                                    this.SendPacket(
                                        tcpClient,
                                        new GamePacket("disconnect", this.lobby.IndexOf(client).ToString())));
                            }
                        }
                    }
                    if (clientsToRemove.Count > 0)
                    {
                        foreach (var tcpClient in clientsToRemove)
                        {

                            this.clients.Remove(tcpClient);
                            this.lobby.Remove(tcpClient);
                        }
                        clientsToRemove.Clear();
                    }
                }
                catch
                {
                }

                //foreach (var client in this.lobby)
                //{
                //    if (client.Available > 0)
                //    {
                //        gamePackets.Add(this.ReceivePacket(client).Result);
                //        this.newDataAvailable = true;
                //    }
                //}
                //if (this.newDataAvailable)
                //{
                //    foreach (var client in this.lobby)
                //    {
                //        ConnectionTasks.Add(this.UpdateClient(client, gamePackets));
                //    }
                //    this.newDataAvailable = false;
                //    gamePackets.Clear();
                //}
            }
        }

        

        /// <summary>
        /// send a packet to a client.
        /// </summary>
        /// <param name="client">
        /// The client.
        /// </param>
        /// <param name="packet">
        /// The packet.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        public async Task SendPacket(TcpClient client, GamePacket packet)
        {
            try
            {
                // convert JSON to buffer and its length to a 16 bit unsigned integer buffer
                byte[] jsonBuffer = Encoding.UTF8.GetBytes(packet.ToJson());
                byte[] lengthBuffer = BitConverter.GetBytes(Convert.ToUInt16(jsonBuffer.Length));

                // Join the buffers
                byte[] msgBuffer = new byte[lengthBuffer.Length + jsonBuffer.Length];
                lengthBuffer.CopyTo(msgBuffer, 0);
                jsonBuffer.CopyTo(msgBuffer, lengthBuffer.Length);

                // Send the packet
                await client.GetStream().WriteAsync(msgBuffer, 0, msgBuffer.Length);
            }
            catch (Exception e)
            {
                // There was an issue is sending
                Console.WriteLine("There was an issue receiving a packet.");
                Console.WriteLine("Reason: {0}", e.Message);
            }
        }

        /// <summary>
        /// The receive packet.
        /// </summary>
        /// <param name="client">
        /// The client.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        public async Task<GamePacket> ReceivePacket(TcpClient client)
        {
            GamePacket packet = null;
            try
            {
                // First check there is data available
                if (client.Available == 0)
                    return null;
                NetworkStream msgStream = client.GetStream();

                // There must be some incoming data, the first two bytes are the size of the Packet
                byte[] lengthBuffer = new byte[2];
                await msgStream.ReadAsync(lengthBuffer, 0, 2);
                ushort packetByteSize = BitConverter.ToUInt16(lengthBuffer, 0);

                // Now read that many bytes from what's left in the stream, it must be the Packet
                byte[] jsonBuffer = new byte[packetByteSize];
                await msgStream.ReadAsync(jsonBuffer, 0, jsonBuffer.Length);

                // Convert it into a packet datatype
                string jsonString = Encoding.UTF8.GetString(jsonBuffer);
                packet = GamePacket.FromJson(jsonString);
            }
            catch (Exception e)
            {
                // There was an issue in receiving
                Console.WriteLine("There was an issue sending a packet to {0}.", client.Client.RemoteEndPoint);
                Console.WriteLine("Reason: {0}", e.Message);
            }
            return packet;
        }


        /// <summary>
        /// Handles new connections.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        private async Task HandleNewConnection()
        {
            // Creates a reference to the new client
            TcpClient newClient = await this.listener.AcceptTcpClientAsync();
            Console.WriteLine("New connection from {0}.", newClient.Client.RemoteEndPoint);

            // Puts the clients in the lobby
            this.clients.Add(newClient);
            this.lobby.Add(newClient);
         // idToAssign++;
            await this.SendPacket(newClient, new GamePacket("myId", this.lobby.IndexOf(newClient).ToString()));
            Thread.Sleep(10);
            // broadcast to all players there is a new player
            foreach (var client in this.lobby)
            {
                if (client != newClient)
                {
                    await this.SendPacket(client, new GamePacket("id", this.lobby.IndexOf(newClient).ToString()));
                }
            }
            foreach (var client in this.lobby)
            {
                if (client != newClient)
                {
                    await this.SendPacket(newClient, new GamePacket("id", this.lobby.IndexOf(client).ToString()));
                }
            }
        }

        /// <summary>
        /// updates a client.
        /// </summary>
        /// <param name="client">
        /// The client.
        /// </param>
        /// <param name="gamePackets">
        /// The game packets.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        private async Task UpdateClient(TcpClient client, List<GamePacket> gamePackets)
        {
            try
            {
                foreach (var gamePacket in gamePackets)
                {
                    await this.SendPacket(client, gamePacket);
                }
                Thread.Sleep(10);
            }
            catch
            {
                // ignored
            }
        }
    }

}