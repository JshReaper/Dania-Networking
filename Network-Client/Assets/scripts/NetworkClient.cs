using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine;

public class NetworkClient
{
    private UdpClient UdpClient;
    private TcpClient tcpClient;
    private NetworkStream msgStream;
    private Dictionary<string, Func<string, Task>> commandHandlers = new Dictionary<string, Func<string, Task>>();
    private bool running;

    private GamePacket gp;

    private List<GamePacket> gpTosend;

    private bool playerChanged;

    private string playerinfo;

    public NetworkClient()
    {
        this.UdpClient = new UdpClient();
        this.tcpClient = new TcpClient();
        Thread t = new Thread(this.HandleUpdates);
        t.IsBackground = true;
        t.Start();
    }


    public void Connect(string serverAdress, string port)
    {
        try
        {

            this.UdpClient.Connect(serverAdress, Convert.ToInt32(port));
            this.tcpClient.Connect(serverAdress, Convert.ToInt32(port));
        }
        catch (Exception e)
        {
            //  Debug.Log("[ERROR] " + e.Message);
        }

        if (this.UdpClient.Client.Connected && this.tcpClient.Connected)
        {
            // Debug.Log("Connected to the server at " + UdpClient.Client.RemoteEndPoint);

            this.running = true;
            this.msgStream = this.tcpClient.GetStream();
            //// Hook up some packet command handlers
            //commandHandlers["message"] = HandleMessage;
            //// Hook up some packet command handlers
            //commandHandlers["input"] = HandleInput;
            //commandHandlers["update"] = cmdUpdate;
            this.commandHandlers["id"] = this.HandleId;
            this.commandHandlers["myId"] = this.HandleMyId;
            this.commandHandlers["disconnect"] = this.HandleDisconnect;
            //Run();

        }
    }


    async Task SendUpdate()
    {
        CultureInfo cIn = CultureInfo.InvariantCulture;
        /* playerinfo = this.myClientPlayer.GetComponent<Player>().MyId.ToString(cIn) +
                      ":" + this.myClientPlayer.transform.position.x.ToString(cIn) +
                      ":" + this.myClientPlayer.transform.position.y.ToString(cIn) +
                      ":" + this.myClientPlayer.transform.position.z.ToString(cIn) +
                      ":" + this.myClientPlayer.transform.rotation.x.ToString(cIn) +
                      ":" + this.myClientPlayer.transform.rotation.y.ToString(cIn) +
                      ":" + this.myClientPlayer.transform.rotation.z.ToString(cIn) +
                      ":" + this.myClientPlayer.transform.rotation.w.ToString(cIn) +
                      ":" + this.myClientPlayer.GetComponent<PlayerController>().IsShooting.ToString(cIn) +
                      ":" + this.myClientPlayer.GetComponent<Health>().currentHealth.ToString(cIn);*/
        await this.SendPacket(new GamePacket("update", this.playerinfo));
    }

    private async Task SendPacket(GamePacket packet)
    {
        try
        {
            // convert JSON to buffer and its length to a 16 bit unsigned integer buffer
            string str = packet.ToJson();
            byte[] jsonBuffer = Encoding.UTF8.GetBytes(str);
            byte[] lengthBuffer = BitConverter.GetBytes(Convert.ToUInt16(jsonBuffer.Length));
            // Join the buffers
            byte[] packetBuffer = new byte[jsonBuffer.Length];
            lengthBuffer.CopyTo(packetBuffer, 0);
            //jsonBuffer.CopyTo(packetBuffer, lengthBuffer.Length);
            // Send the packet
            //await msgStream.WriteAsync(packetBuffer, 0, packetBuffer.Length);
            //_client.Send(packetBuffer, packetBuffer.Length);
            this.UdpClient.Send(jsonBuffer, jsonBuffer.Length);
        }
        catch (Exception se)
        {
            // Debug.Log("[ERROR] : Could not send the gamePacket : \n" + se);
        }
    }

    private void DataReceived(IAsyncResult ar)
    {
        UdpClient c = (UdpClient)ar.AsyncState;
        IPEndPoint receivedIpEndPoint = new IPEndPoint(IPAddress.Any, 0);

        Byte[] receivedBytes = c.EndReceive(ar, ref receivedIpEndPoint);
        Debug.Log(receivedIpEndPoint.ToString());
        // Convert data to UTF8 and print in console
        string receivedText = Encoding.UTF8.GetString(receivedBytes);
        gp = GamePacket.FromJson(receivedText);

        // Restart listening for udp data packages
        c.BeginReceive(DataReceived, ar.AsyncState);

    }
    private async Task HandleIncomingPackets()
    {
        try
        {
            // Check for new incomding messages
            if (this.tcpClient.Available > 0)
            {

                // There must be some incoming data, the first two bytes are the size of the Packet
                byte[] lengthBuffer = new byte[2];
                await this.msgStream.ReadAsync(lengthBuffer, 0, 2);
                ushort packetByteSize = BitConverter.ToUInt16(lengthBuffer, 0);
                // Now read that many bytes from what's left in the stream, it must be the Packet
                byte[] jsonBuffer = new byte[packetByteSize];
                await this.msgStream.ReadAsync(jsonBuffer, 0, jsonBuffer.Length);
                // Convert it into a packet datatype
                string jsonString = Encoding.UTF8.GetString(jsonBuffer);
                GamePacket packet = GamePacket.FromJson(jsonString);
                // Dispatch it
                if (packet.Command != "update")
                    try
                    {
                        await this.commandHandlers[packet.Command](packet.Message);
                    }
                    catch (KeyNotFoundException)
                    {
                    }
            }

            try
            {
                this.UdpClient.BeginReceive(DataReceived, this.UdpClient);

                // Dispatch it
                if (gp != null)
                    try
                    {
                        this.gpTosend.Add(this.gp);
                        //await commandHandlers[gp.Command](gp.Message);
                        // gp = null;
                    }
                    catch (Exception e)
                    {
                    }

            }
            catch (Exception)
            {

            }
        }
        catch (Exception) { }
    }
    private Task HandleDisconnect(string arg)
    {
        throw new NotImplementedException();
    }

    private Task HandleMyId(string arg)
    {
        throw new NotImplementedException();
    }

    private Task HandleId(string arg)
    {
        throw new NotImplementedException();
    }
    void HandleUpdates()
    {
        while (true)
        {

            if (this.running)
            {
                List<Task> tasks = new List<Task>();
                tasks.Add(this.HandleIncomingPackets());
                if (this.playerChanged)
                {
                    tasks.Add(this.SendUpdate());
                }
            }
        }
    }
}