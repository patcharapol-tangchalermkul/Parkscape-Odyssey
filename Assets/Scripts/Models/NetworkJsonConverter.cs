using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using UnityEngine;

public class NetworkJsonConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(MessageInfo);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        JObject jsonObject = JObject.Load(reader);

        Debug.Log("Parsing messageinfo");

        // Determine the type based on the "type" field
        if (jsonObject["messageType"] == null)
        {
            throw new JsonSerializationException("type field is missing");
        }
        string type = jsonObject.Value<string>("messageType");
        
        if (type.Equals(MessageType.TEST.ToString()))
        {
            return jsonObject.ToObject<TestMessageInfo>(serializer);
        }
        else if (type.Equals(MessageType.PINGMESSAGE.ToString()))
        {
            return jsonObject.ToObject<PingMessageInfo>(serializer);
        }
        else if (type.Equals(MessageType.LOBBYMESSAGE.ToString()))
        {
            return jsonObject.ToObject<LobbyMessage>(serializer);
        } 
        else if (type.Equals(MessageType.GAMESTATE.ToString()))
        {
            return jsonObject.ToObject<GameStateMessage>(serializer);
        }
        else if (type.Equals(MessageType.ENCOUNTERMESSAGE.ToString()))
        {
            return jsonObject.ToObject<EncounterMessage>(serializer);
        }
        else if (type.Equals(MessageType.BATTLEMESSAGE.ToString())) 
        {
            return jsonObject.ToObject<BattleMessage>(serializer);
        }
        else if (type.Equals(MessageType.TRADE.ToString())) 
        {
            return jsonObject.ToObject<TradeMessage>(serializer);
        }
        else if (type.Equals(MessageType.MAP.ToString())) 
        {
            return jsonObject.ToObject<MapMessage>(serializer);
        }

        Debug.Log("Parsed messageinfo" + jsonObject.ToString());

        throw new JsonSerializationException("Unknown type");
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}
