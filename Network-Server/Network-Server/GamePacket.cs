namespace Network_Server
{
    using Newtonsoft.Json;

    internal class GamePacket
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="GamePacket"/> class.
        /// </summary>
        /// <param name="command">
        /// The command.
        /// </param>
        /// <param name="message">
        /// The message.
        /// </param>
        public GamePacket(string command = "", string message = "")
        {
            this.Command = command;
            this.Message = message;
        }

        /// <summary>
        /// Gets or sets the command.
        /// </summary>
        [JsonProperty("command")]
        public string Command { get; set; }

        /// <summary>
        /// Gets or sets the message.
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; set; }

        /// <summary>
        /// Override of the ToString method.
        /// </summary>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public override string ToString()
        {
            return "[Packet:\n" + $" Command=`{this.Command}`\n" + $" Message=`{this.Message}`]";
        }

        /// <summary>
        /// Serialize to Json.
        /// </summary>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }

        /// <summary>
        /// Deserialize the json string.
        /// </summary>
        /// <param name="jsonData">
        /// The json data.
        /// </param>
        /// <returns>
        /// The <see cref="GamePacket"/>.
        /// </returns>
        public static GamePacket FromJson(string jsonData)
        {
            return JsonConvert.DeserializeObject<GamePacket>(jsonData);
        }
    }
}