using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class GamePacket {

    [JsonProperty("command")]
    public string Command { get; set; }
    [JsonProperty("message")]
    public string Message { get; set; }
    public GamePacket(string command = "", string message = "")
    {
        this.Command = command;

        this.Message = message;
    }
    public override string ToString()
    {
        return string.Format("[Packet:\n" + " Command=`{0}`\n" + " Message=`{1}`]", Command, Message);
    }
    // Serialize to Json
    public string ToJson()
    {
        return JsonConvert.SerializeObject(this);
    }
    // Deserialize
    public static GamePacket FromJson(string jsonData)
    {
        return JsonConvert.DeserializeObject<GamePacket>(jsonData);
    }
}
