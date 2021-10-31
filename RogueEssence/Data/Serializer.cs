using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace RogueEssence.Data
{
    public static class Serializer
    {
        private static readonly JsonSerializerSettings Settings = new()
        {
            ContractResolver = new SerializerContractResolver(),
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
            TypeNameHandling = TypeNameHandling.Auto,
        };
        
        public static object Deserialize(Stream stream, Type type)
        {
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return JsonConvert.DeserializeObject(reader.ReadToEnd(), type, Settings);
        }

        public static void Serialize(Stream stream, object entry)
        {
            using var writer = new StreamWriter(stream, Encoding.UTF8);
            var val = JsonConvert.SerializeObject(entry, Settings);
            writer.Write(val);
        }
        
        private class SerializerContractResolver : DefaultContractResolver
        {
            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                List<MemberInfo> fields = Dev.ReflectionExt.GetEditableMembers(type);
                List<JsonProperty> props = fields.Select(f => CreateProperty(f, memberSerialization))
                    .ToList();
                props.ForEach(p => { p.Writable = true; p.Readable = true; });
                return props;
                
            }
        }
    }
}