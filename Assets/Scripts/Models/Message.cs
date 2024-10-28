using Newtonsoft.Json;
using UnityEngine;
public class Message {
    private string messageID;
    public string sentFrom;
    [JsonProperty("messageInfo")]
    public MessageInfo messageInfo{get; set;}
    public string toJson() {
        return JsonConvert.SerializeObject(this);
    }
}