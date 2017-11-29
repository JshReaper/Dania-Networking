using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;

public class Client : MonoBehaviour
{

    string playerinfo;

    private NetworkClient networkClient;
    private NetworkStream msgStream;
    [SerializeField]
    InputField _serverAdress;
    [SerializeField]
    InputField _port;
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
    void Start()
    {
        this.networkClient = new NetworkClient();
        this.players = new List<Player>();

    }

    /// <summary>
    /// update sørger for at vores tasks bliver kørt hvert frame, og sørger for at kun hvis spilleren har bevæget sig sender vi nyt data
    /// </summary>
    void Update()
    {
        if (networkClient != null)
        {
            if (this.networkClient.Running)
            {
                if (this.networkClient.DisconnectMsgAvailable)
                {
                    this.HandleDisconnect(this.networkClient.DisconnectMsg);
                }

                if (this.networkClient.MyIdMsgAvailable)
                {
                    this.HandleMyId(this.networkClient.MyIdMsg);
                }

                if (this.networkClient.IdMsgAvailable)
                {
                    this.HandleId(this.networkClient.IdMsg);
                }
                if (this.networkClient.Gp != null)
                {
                    cmdUpdate(this.networkClient.Gp.Message);
                }

                if (this.myClientPlayer != null)
                {
                    if (this.lastPos != this.myClientPlayer.transform.position || this.lastRot != this.myClientPlayer.transform.rotation || this.myClientPlayer.GetComponent<PlayerController>().IsShooting)
                    {
                        this.playerChanged = true;
                    }
                    else
                    {
                        this.playerChanged = false;
                    }

                    CultureInfo cIn = CultureInfo.InvariantCulture;
                    this.playerinfo = this.myClientPlayer.GetComponent<Player>().MyId.ToString(cIn) +
                                 ":" + this.myClientPlayer.transform.position.x.ToString(cIn) +
                                 ":" + this.myClientPlayer.transform.position.y.ToString(cIn) +
                                 ":" + this.myClientPlayer.transform.position.z.ToString(cIn) +
                                 ":" + this.myClientPlayer.transform.rotation.x.ToString(cIn) +
                                 ":" + this.myClientPlayer.transform.rotation.y.ToString(cIn) +
                                 ":" + this.myClientPlayer.transform.rotation.z.ToString(cIn) +
                                 ":" + this.myClientPlayer.transform.rotation.w.ToString(cIn) +
                                 ":" + this.myClientPlayer.GetComponent<PlayerController>().IsShooting.ToString(cIn) +
                                 ":" + this.myClientPlayer.GetComponent<Health>().currentHealth.ToString(cIn);
                    this.networkClient.Playerinfo = this.playerinfo;
                    this.networkClient.PlayerChanged = this.playerChanged;
                    this.lastPos = new Vector3(this.myClientPlayer.transform.position.x, this.myClientPlayer.transform.position.y, this.myClientPlayer.transform.position.z);
                    this.lastRot = new Quaternion(this.myClientPlayer.transform.rotation.x, this.myClientPlayer.transform.rotation.y, this.myClientPlayer.transform.rotation.z, this.myClientPlayer.transform.rotation.w);
                }
            }
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
            if (this._serverAdress.text == string.Empty && this._port.text == string.Empty)
            {
                this.networkClient.Connect("62.116.202.203", "42424");
                this.networkClient.Connect("62.116.202.203", "42424");
            }
            else
            {
                this.networkClient.Connect(this._serverAdress.text, this._port.text);
                this.networkClient.Connect(this._serverAdress.text, this._port.text);
            }
            Debug.Log("connected to server");
        }
        catch (Exception e)
        {
            Debug.Log("[ERROR] " + e.Message);
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
        if (!(splitString[0] == this.myClientPlayer.GetComponent<Player>().MyId.ToString()))
            foreach (var player in this.players)
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
    private void HandleId(string message)
    {
        Debug.Log("Entered HandleId method");
        if (this.weCanSpawnOthers)
        {
            bool foundId = false;
            foreach (var player in this.players)
            {
                if (player.MyId.ToString() == message)
                {
                    foundId = true;
                }
            }

            if (!foundId)
            {
                Debug.Log("Found new player on id");
                GameObject go = Instantiate(this.playerPrefab);
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
    private void HandleMyId(string message)
    {
        Debug.Log("Entered HandleMyId method");
        try
        {
            Debug.Log("Entered Try method");
            this.myClientPlayer = Instantiate(this.playerPrefab);
            Debug.Log("Spawned player");
            this.myClientPlayer.GetComponent<Player>().MyId = Convert.ToInt32(message);
            this.players.Add(this.myClientPlayer.GetComponent<Player>());
            this.weCanSpawnOthers = true;
            networkClient.MyIdMsgAvailable = false;
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    private void HandleDisconnect(string message)
    {
        GameObject goToDelete = null;
        foreach (Player p in this.players)
        {
            if (p.MyId.ToString() == message)
            {
                goToDelete = p.gameObject;

            }
        }

        if (goToDelete != null)
        {

            this.players.Remove(goToDelete.GetComponent<Player>());
            Destroy(goToDelete);
        }
        foreach (Player p in this.players)
        {
            if (p.MyId > Convert.ToInt32(message))
            {
                p.MyId -= 1;
            }
        }
        this.networkClient.DisconnectMsgAvailable = false;
    }
}

