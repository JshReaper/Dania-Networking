using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Sockets;
using System.Net;
using UnityEngine;
using System.Text;
using System.Threading.Tasks;

using UnityEngine.UI;

public class Client : MonoBehaviour {
    private UdpClient _client;
    private TcpClient _tcpClient;
    private NetworkStream msgStream;
    [SerializeField]
    InputField _serverAdress;
    private bool _running;
    [SerializeField]
    InputField _port;
    private Dictionary<string, Func<string, Task>> commandHandlers = new Dictionary<string, Func<string, Task>>();
    [SerializeField]
    private GameObject myClientPlayer;
    [SerializeField]
    private GameObject playerPrefab;
    private List<Player> players;
    private GamePacket gp = null;
    private bool playerChanged;

    private Vector3 lastPos = Vector3.zero;
    Quaternion lastRot = Quaternion.identity;
    private int myId;

    
    /// <summary>
    /// Sætter de standarte værdier og lignende, og initiere vores klienter
    /// </summary>
    void Start () {
        players = new List<Player>();
        _client = new UdpClient();
        _tcpClient = new TcpClient();
        string playerinfo = String.Empty;
        playerinfo += "pos" + transform.position.x.ToString(CultureInfo.InvariantCulture) + "," + transform.position.y.ToString(CultureInfo.InvariantCulture)+ "," + transform.position.z.ToString(CultureInfo.InvariantCulture);
        playerinfo += " rot " + transform.rotation.x.ToString() + "," + transform.rotation.y.ToString() + "," + transform.rotation.z.ToString() + "," + transform.rotation.w.ToString();
    }
	/// <summary>
    /// update sørger for at vores tasks bliver kørt hvert frame, og sørger for at kun hvis spilleren har bevæget sig sender vi nyt data
    /// </summary>
	void Update ()
	{
        if (this._running)
        {
            List<Task> tasks = new List<Task>();
            tasks.Add(this.HandleIncomingPackets());
            if(this.myClientPlayer != null)
            { 
            if (this.lastPos != this.myClientPlayer.transform.position || this.lastRot != this.myClientPlayer.transform.rotation || this.myClientPlayer.GetComponent<PlayerController>().IsShooting)
            {
                this.playerChanged = true;
            }
            else
            {
                this.playerChanged = false;
            }
            if (this.playerChanged)
            {
                tasks.Add(this.SendUpdate());
            }
            this.lastPos = new Vector3(this.myClientPlayer.transform.position.x, this.myClientPlayer.transform.position.y, this.myClientPlayer.transform.position.z);
            this.lastRot = new Quaternion(this.myClientPlayer.transform.rotation.x, this.myClientPlayer.transform.rotation.y, this.myClientPlayer.transform.rotation.z, this.myClientPlayer.transform.rotation.w);
            }
            //This code has been moved to the HandleIncomignMessage method as this calls it to often
            //if (gp != null)
            //    try
            //    {
            //        cmdUpdate(gp.Message);
            //        //await commandHandlers[gp.Command](gp.Message);
            //        // gp = null;
            //    }
            //    catch (KeyNotFoundException)
            //    {
            //    }
        }

    }
    /// <summary>
    /// Denne metode konnekter vores klienter til serveren, endten den standarte som vi har lavet eller en 
    /// "unofficial" server som en spiller har sat op ved at indskrive info i text felterne
    /// </summary>
    public void Connect()
    {
        try
        {
            if (this._serverAdress.text == "" && this._port.text == "")
            {
                _client.Connect("dania-jshreaper.northeurope.cloudapp.azure.com", 42424);
                _tcpClient.Connect("dania-jshreaper.northeurope.cloudapp.azure.com", 42424);
            }
            else
            {
                _client.Connect(_serverAdress.text, Convert.ToInt32(_port.text));
                _tcpClient.Connect(_serverAdress.text, Convert.ToInt32(_port.text));
            }
        }
        catch (Exception e)
        {
            Debug.Log("[ERROR] " + e.Message);
        }

        if (_client.Client.Connected && _tcpClient.Connected)
        {
            Debug.Log("Connected to the server at " + _client.Client.RemoteEndPoint);

            _running = true;
            msgStream = _tcpClient.GetStream();
            //// Hook up some packet command handlers
            //commandHandlers["message"] = HandleMessage;
            //// Hook up some packet command handlers
            //commandHandlers["input"] = HandleInput;
            //commandHandlers["update"] = cmdUpdate;
            commandHandlers["id"] = HandleId;
            commandHandlers["myId"] = HandleMyId;
            //Run();
            
        }
    }
    /// <summary>
    /// cmdUpdate sørger for at de beskeder der kommer fra serveren om andre spilleres handlinger bliver udført clientside også
    /// den sætter deres nye position og roteríng lige med hvad de sidst sendte
    /// </summary>
    /// <param name="message"></param>
    private void cmdUpdate(string message)
    {
        string[] splitString = message.Split(':');
        if(!(splitString[0] == myClientPlayer.GetComponent<Player>().MyId.ToString()))
            foreach (var player in players)
            {
            if (player.MyId.ToString() == splitString[0])
            {

                Vector3 pos = new Vector3(float.Parse(splitString[1], CultureInfo.InvariantCulture.NumberFormat), float.Parse(splitString[2], CultureInfo.InvariantCulture.NumberFormat), float.Parse(splitString[3], CultureInfo.InvariantCulture.NumberFormat));
                Quaternion rot = new Quaternion(float.Parse(splitString[4], CultureInfo.InvariantCulture.NumberFormat), float.Parse(splitString[5], CultureInfo.InvariantCulture.NumberFormat), float.Parse(splitString[6], CultureInfo.InvariantCulture.NumberFormat), float.Parse(splitString[7], CultureInfo.InvariantCulture.NumberFormat));
                bool isShooting = Convert.ToBoolean(splitString[8]);
                int hp = Convert.ToInt32(splitString[9]);
                player.gameObject.transform.position = pos;
                player.gameObject.transform.rotation = rot;
                player.gameObject.GetComponent<Health>().currentHealth = hp;
                if (isShooting)
                {
                    player.gameObject.GetComponent<Player>().CmdFire();
                }
            }
        }
    }
    /// <summary>
    /// Når man første gang konnekter eller andre konnekter til serveren sendes deres id ud til 
    /// alle andre og det hånteres her, ved at lave en ny spiller og indsætte den i verdenen
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    private async Task HandleId(string message)
    {
        if (this.weCanSpawnOthers) { 
        bool foundId = false;
        foreach (var player in players)
        {
            if (player.MyId.ToString() == message)
            {
                foundId = true;
            }
        }
        if (!foundId)
        {
            GameObject go = GameObject.Instantiate(playerPrefab);
                Destroy(go.GetComponent<PlayerController>());
                go.GetComponent<Player>().MyId = Convert.ToInt32(message);
                this.players.Add(go.GetComponent<Player>());
        }
        }
    }

    private bool weCanSpawnOthers;
    /// <summary>
    /// Første gang man konnekter til serveren sendes der et ID tilbage til spilleren, det hånteres her
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    private async Task HandleMyId(string message)
    {
        try
        {

            this.myClientPlayer =  Instantiate(playerPrefab);
            this.myClientPlayer.GetComponent<Player>().MyId = Convert.ToInt32(message);
            this.players.Add(this.myClientPlayer.GetComponent<Player>());
            this.weCanSpawnOthers = true;
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }
    }
    /// <summary>
    /// Håntere indkommende beskeder fra TCP klienten den kan kun gøre brug af 2 metoder myID og Id metoderne.
    /// </summary>
    /// <returns></returns>
    private async Task HandleIncomingPackets()
    {
        try
        {
            // Check for new incomding messages
            if (_tcpClient.Available > 0)
            {

                // There must be some incoming data, the first two bytes are the size of the Packet
                byte[] lengthBuffer = new byte[2];
                await msgStream.ReadAsync(lengthBuffer, 0, 2);
                ushort packetByteSize = BitConverter.ToUInt16(lengthBuffer, 0);
                // Now read that many bytes from what's left in the stream, it must be the Packet
                byte[] jsonBuffer = new byte[packetByteSize];
                await msgStream.ReadAsync(jsonBuffer, 0, jsonBuffer.Length);
                // Convert it into a packet datatype
                string jsonString = Encoding.UTF8.GetString(jsonBuffer);
                GamePacket packet = GamePacket.FromJson(jsonString);
                // Dispatch it
                if(packet.Command != "update")
                try
                {
                    await commandHandlers[packet.Command](packet.Message);
                }
                catch (KeyNotFoundException)
                {
                }
            }
            
            try
            {


                //if (_client.Available > 0)
                //{


                    _client.BeginReceive(DataReceived, _client);
                // There must be some incoming data, the first two bytes are the size of the Packet
                //byte[] lengthBuffer = new byte[2];
                //await msgStream.ReadAsync(lengthBuffer, 0, 2);
                //ushort packetByteSize = BitConverter.ToUInt16(lengthBuffer, 0);
                // Now read that many bytes from what's left in the stream, it must be the Packet
                //byte[] jsonBuffer = new byte[packetByteSize];
                //await msgStream.ReadAsync(jsonBuffer, 0, jsonBuffer.Length);
                // Convert it into a packet datatype
                //string jsonString = Encoding.UTF8.GetString(jsonBuffer);
                //GamePacket packet = GamePacket.FromJson(jsonString);
                // Dispatch it
                if (gp != null)
                    try
                    {
                        cmdUpdate(gp.Message);
                        //await commandHandlers[gp.Command](gp.Message);
                        // gp = null;
                    }
                    catch (KeyNotFoundException)
                    {
                    }
                    //}
            }
            catch (Exception)
            {

            }
        }
        catch (Exception) { }
    }

    /// <summary>
    /// Her tjekker vi om der er kommet nogle beskeder siden vi sidst tjekkede
    /// hvis der er kommet information så kommer vi det ind i en gamepacket gp og den bruges i en anden metode "cmdUpdate" når den er sat lige med noget
    /// </summary>
    /// <param name="ar"></param>
    private void DataReceived(IAsyncResult ar)
    {
        UdpClient c = (UdpClient)ar.AsyncState;
        IPEndPoint receivedIpEndPoint = new IPEndPoint(IPAddress.Any, 0);

        Byte[] receivedBytes = c.EndReceive(ar, ref receivedIpEndPoint);
        Debug.Log(receivedIpEndPoint.ToString());
        // Convert data to UTF8 and print in console
        string receivedText = Encoding.UTF8.GetString(receivedBytes);
        gp = GamePacket.FromJson(receivedText);
        Debug.Log(gp.ToString());
        
        // Restart listening for udp data packages
        c.BeginReceive(DataReceived, ar.AsyncState);
       
    }
    /// <summary>
    /// Sender vores player information som en gamepacket via UDP
    /// information sendes via UDP vi tjekker ikke op på om det kom frem da vi arbejder med time critical data.
    /// </summary>
    /// <param name="packet"></param>
    /// <returns></returns>
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
            _client.Send(jsonBuffer,jsonBuffer.Length);
        }
        catch (Exception se)
        {
            Debug.Log("[ERROR] : Could not send the gamePacket : \n" + se);
        }
    }
    string playerinfo;

    /// <summary>
    /// SendUpdate laver vores player data om til en streng som vi så lavet til en gamepacket og sender via metoden SendPacket
    /// </summary>
    /// <returns></returns>
    async Task SendUpdate()
    {
        CultureInfo cIn = CultureInfo.InvariantCulture;
        playerinfo = this.myClientPlayer.GetComponent<Player>().MyId.ToString(cIn) + 
            ":" + this.myClientPlayer.transform.position.x.ToString(cIn) + 
            ":" + this.myClientPlayer.transform.position.y.ToString(cIn) + 
            ":" + this.myClientPlayer.transform.position.z.ToString(cIn) +
            ":" + this.myClientPlayer.transform.rotation.x.ToString(cIn) + 
            ":" + this.myClientPlayer.transform.rotation.y.ToString(cIn) + 
            ":" + this.myClientPlayer.transform.rotation.z.ToString(cIn) +
            ":" + this.myClientPlayer.transform.rotation.w.ToString(cIn) +
            ":" + this.myClientPlayer.GetComponent<PlayerController>().IsShooting.ToString(cIn) +
            ":" + this.myClientPlayer.GetComponent<Health>().currentHealth.ToString(cIn);
        await this.SendPacket(new GamePacket("update", playerinfo));
    }
}

