using System;
using AhBearStudios.Core.Messaging.Interfaces;
using UnityEngine;

namespace AhBearStudios.Core.Messaging.Serializers
{
    /// <summary>
    /// A JSON serializer implementation
    /// </summary>
    public class JsonMessageSerializer : IMessageSerializer
    {
        public string Serialize(object obj)
        {
            return JsonUtility.ToJson(obj);
        }
    
        public string Serialize<T>(T obj)
        {
            return JsonUtility.ToJson(obj);
        }

        public object Deserialize(string json, Type type)
        {
            return JsonUtility.FromJson(json, type);
        }
    
        public T Deserialize<T>(string json)
        {
            return JsonUtility.FromJson<T>(json);
        }
    }
}