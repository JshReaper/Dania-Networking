using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Network_Server
{
    class Program
    {
        /// <summary>
        /// The server.
        /// </summary>
        private static GameServer server;

        /// <summary>
        /// The main entry point of the application.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        private static void Main(string[] args)
        {
            server = new GameServer(42424);
            server.Lobby();
        }
    }
}
