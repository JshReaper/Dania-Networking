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
    private TcpClient _client;
    [SerializeField]
    InputField _serverAdress;
    private bool _running;
    [SerializeField]
    InputField _port;
    private NetworkStream msgStream = null;
    private Dictionary<string, Func<string, Task>> commandHandlers = new Dictionary<string, Func<string, Task>>();
    [SerializeField]
    private GameObject myClientPlayer;
    [SerializeField]
    private GameObject playerPrefab;
    private List<Player> players;

    private bool playerChanged;

    private Vector3 lastPos = Vector3.zero;
    Quaternion lastRot = Quaternion.identity;
    private int myId;

    // Use this for initialization
    void Start () {
        players = new List<Player>();
        _client = new TcpClient();
        string playerinfo = String.Empty;
        playerinfo += "pos" + transform.position.x.ToString(CultureInfo.InvariantCulture) + "," + transform.position.y.ToString(CultureInfo.InvariantCulture)+ "," + transform.position.z.ToString(CultureInfo.InvariantCulture);
        playerinfo += " rot " + transform.rotation.x.ToString() + "," + transform.rotation.y.ToString() + "," + transform.rotation.z.ToString();
    }
	
	// Update is called once per frame
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
        }
    }

    public void Connect()
    {
        try
        {
            if (this._serverAdress.text == "" && this._port.text == "")
            {
                _client.Connect("dania-jshreaper.northeurope.cloudapp.azure.com", 42424);
            }
            else
            {
                _client.Connect(_serverAdress.text, Convert.ToInt32(_port.text));
            }
        }
        catch (SocketException se)
        {
            Debug.Log("[ERROR] " + se.Message);
        }

        if (_client.Connected)
        {
            Debug.Log("Connected to the server at " + _client.Client.RemoteEndPoint);

            _running = true;
            // Get the message stream
            msgStream = _client.GetStream();
            //// Hook up some packet command handlers
            //commandHandlers["message"] = HandleMessage;
            //// Hook up some packet command handlers
            //commandHandlers["input"] = HandleInput;
            commandHandlers["update"] = cmdUpdate;
            commandHandlers["id"] = HandleId;
            commandHandlers["myId"] = HandleMyId;
            //Run();
            
        }
    }
    private async Task cmdUpdate(string message)
    {
        string[] splitString = message.Split(':');
        if(!(splitString[0] == myClientPlayer.GetComponent<Player>().MyId.ToString()))
            foreach (var player in players)
            {
            if (player.MyId.ToString() == splitString[0])
            {

                Vector3 pos = new Vector3(float.Parse(splitString[1], CultureInfo.InvariantCulture.NumberFormat), float.Parse(splitString[2], CultureInfo.InvariantCulture.NumberFormat), float.Parse(splitString[3], CultureInfo.InvariantCulture.NumberFormat));
                Quaternion rot = new Quaternion(float.Parse(splitString[4], CultureInfo.InvariantCulture.NumberFormat), float.Parse(splitString[5], CultureInfo.InvariantCulture.NumberFormat), float.Parse(splitString[6], CultureInfo.InvariantCulture.NumberFormat), 1);
                bool isShooting = Convert.ToBoolean(splitString[7]);
                int hp = Convert.ToInt32(splitString[8]);
                player.gameObject.transform.position = pos;
                player.gameObject.transform.rotation = rot;
                player.gameObject.GetComponent<Health>().currentHealth = hp;
                if (isShooting)
                {
                    player.gameObject.GetComponent<PlayerController>().CmdFire();
                }
            }
        }
    }
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

    private async Task HandleIncomingPackets()
    {
        try
        {
            // Check for new incomding messages
            if (_client.Available > 0)
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
                try
                {
                    await commandHandlers[packet.Command](packet.Message);
                }
                catch (KeyNotFoundException)
                {
                }
            }
        }
        catch (Exception) { }
    }
    private async Task SendPacket(GamePacket packet)
    {
        try
        {
            // convert JSON to buffer and its length to a 16 bit unsigned integer buffer
            byte[] jsonBuffer = Encoding.UTF8.GetBytes(packet.ToJson());
            byte[] lengthBuffer = BitConverter.GetBytes(Convert.ToUInt16(jsonBuffer.Length));
            // Join the buffers
            byte[] packetBuffer = new byte[lengthBuffer.Length + jsonBuffer.Length];
            lengthBuffer.CopyTo(packetBuffer, 0);
            jsonBuffer.CopyTo(packetBuffer, lengthBuffer.Length);
            // Send the packet
            await msgStream.WriteAsync(packetBuffer, 0, packetBuffer.Length);
        }
        catch (Exception) { }
    }

    async Task SendUpdate()
    {
        CultureInfo cIn = CultureInfo.InvariantCulture;
        string playerinfo = this.myClientPlayer.GetComponent<Player>().MyId.ToString(cIn) + 
            ":" + this.myClientPlayer.transform.position.x.ToString(cIn) + 
            ":" + this.myClientPlayer.transform.position.y.ToString(cIn) + 
            ":" + this.myClientPlayer.transform.position.z.ToString(cIn) +
            ":" + this.myClientPlayer.transform.rotation.x.ToString(cIn) + 
            ":" + this.myClientPlayer.transform.rotation.y.ToString(cIn) + 
            ":" + this.myClientPlayer.transform.rotation.z.ToString(cIn) +
            ":" + this.myClientPlayer.GetComponent<PlayerController>().IsShooting.ToString(cIn) +
            ":" + this.myClientPlayer.GetComponent<Health>().currentHealth.ToString(cIn);
        await this.SendPacket(new GamePacket("update", playerinfo));
    }
}

