﻿using System;
using System.IO;
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
        public static bool DumpToFile(string dirPath, string fileName, object SourceObj)
        {
            bool result = false;
            var index = Base.dumpIndex++;
            var path =  Path.Combine(dirPath, "dump" + index + "_" + Base.NumberDateTime(DateTime.Now) + "_" + fileName);

            using (StreamWriter file = File.CreateText(Path.GetFullPath(path)))
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
    }
}