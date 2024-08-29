//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
using System.IO;
//using System.Security.Principal;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
using Newtonsoft.Json;
//using Newtonsoft.Json.Linq;
//using Newtonsoft.Json.Schema;


// http s://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/how-to
// http s://learn.microsoft.com/en-us/dotnet/api/system.runtime.serialization.json.datacontractjsonserializer?view=net-8.0&redirectedfrom=MSDN
// http s://stackoverflow.com/questions/3275863/does-net-4-have-a-built-in-json-serializer-deserializer

// .Net 4 - Newtonsoft.JsonSystem.Text.Json
//     http s://www.newtonsoft.com/json
//     http s://www.newtonsoft.com/json/help/html/Introduction.htm
//     http s://learn.microsoft.com/ru-ru/dotnet/standard/serialization/system-text-json/migrate-from-newtonsoft?source=recommendations&pivots=dotnet-9-0

namespace ae.lib
{
    internal class JSON
    {
        public static bool DumpToFile(object SourceObj, string FileNamePath)
        {
            bool result = false;
            using (StreamWriter file = File.CreateText(Path.GetFullPath(FileNamePath)))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, SourceObj);
                result = true;
            }
            return result;
        }

        public static string toJSON(object rawData, Formatting Formatting=Formatting.None)
        {
            //serialize JSON without any whitespace
            var jsonSerializerSettings = new JsonSerializerSettings
            {
                Formatting = Formatting,
                NullValueHandling = NullValueHandling.Ignore,
                Culture = System.Globalization.CultureInfo.InvariantCulture
            };
            return JsonConvert.SerializeObject(rawData, Formatting, jsonSerializerSettings);
        }

        public static Object fromJSON<Object>(string jsonData)
        {
            return JsonConvert.DeserializeObject<Object>(jsonData);
        }


        /*
            public string Serialize<T>(T Obj)
            {
                using (var ms = new MemoryStream())
                {
                    DataContractJsonSerializer serialiser = new DataContractJsonSerializer(typeof(T));
                    serialiser.WriteObject(ms, Obj);
                    //byte[] json = ms.ToArray();
                    //return Encoding.UTF8.GetString(json, 0, json.Length);
                }
            }

            public T Deserialize<T>(string Json)
            {
                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(Json)))
                {
                    DataContractJsonSerializer serialiser = new DataContractJsonSerializer(typeof(T));
                    var deserializedObj = (T)serialiser.ReadObject(ms);
                    return deserializedObj;
                }
            }
        */
    }
}
