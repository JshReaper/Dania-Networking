using System.Linq;
using System.Threading.Tasks;

namespace Network_Server
{
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
}
