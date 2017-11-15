﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using UnityEngine;
using System.Text;
using System.Threading.Tasks;


public class Client : MonoBehaviour {
    private TcpClient _client;
    private string _serverAdress;
    private bool _running;
    private int _port;
    private NetworkStream msgStream = null;
    private Dictionary<string, Func<string, Task>> commandHandlers = new Dictionary<string, Func<string, Task>>();

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void Connect()
    {
        try
        {
            _client.Connect(_serverAdress, _port);
        }
        catch (SocketException se)
        {
            Debug.Log("[ERROR] " + se.Message);
        }

        if (_client.Connected)
        {
            Debug.Log("Connected to the server at " + _client.Client.RemoteEndPoint);

            //_running = true;
            //// Get the message stream
            //msgStream = _client.GetStream();
            //// Hook up some packet command handlers
            //commandHandlers["message"] = HandleMessage;
            //// Hook up some packet command handlers
            //commandHandlers["input"] = HandleInput;
            //Run();


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
}
