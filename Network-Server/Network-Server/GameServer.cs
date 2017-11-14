namespace Network_Server
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
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

        /// <summary>
        /// The connected clients.
        /// </summary>
        private readonly List<TcpClient> clients;

        /// <summary>
        /// The lobby of connected clients.
        /// </summary>
        private readonly List<TcpClient> lobby;

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
        }

        /// <summary>
        /// Starts the server.
        /// </summary>
        public void Lobby()
        {
            // A list for new connection tasks
            // ReSharper disable once CollectionNeverQueried.Local
            var newConnectionTasks = new List<Task>();
            Console.WriteLine("Lobby open");
            bool runLobby = true;

            // Starts our listener
            this.listener.Start();

            // Executes the lobby
            while (runLobby) 
            {
                // Waits for new connections
                if (this.listener.Pending())
                {
                    // Add new connection and say welcome
                    newConnectionTasks.Add(this.HandleNewConnection());
                }
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
            string msg = "Welcome to the lobby, please wait for the game to start";
            await this.SendPacket(newClient, new GamePacket("message", msg));
        }
    }
}