using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

// https://www.newtonsoft.com/json/help/html/convertingjsonandxml.htm
// convert xml to c# class - https://xmltocsharp.azurewebsites.net/
// https://qna.habr.com/q/477557

namespace ae.lib
{
    internal class XML
    {
        public static T ConvertXMLFileToClass<T>(string filePath)
        {
            T res = default(T);
            try
            {
                if (File.Exists(filePath)) {
                    XmlSerializer formatter = new XmlSerializer(typeof(T));
                    using (FileStream reader = new FileStream(filePath, FileMode.Open))
                    {
                        res = (T)formatter.Deserialize(reader);
                    }
                }
            }
            catch (Exception ex)
            {
                Base.Log("Error in " + ex.GetType().Name + ".ConvertXMLFileToClass(): " + ex.Message);
            }
            return res;
        }

        public static bool ConvertClassToXMLFile(
            string toXmlFilePath, object objectClass, Encoding TextEncoding
        )
        {
            bool res = false;
            if (TextEncoding == null)
                TextEncoding = Encoding.UTF8;
            try
            {
                if (File.Exists(toXmlFilePath)) {
                    File.Delete(toXmlFilePath);
                }

                Type objectType = objectClass.GetType();
                XmlSerializer formatter = new XmlSerializer(objectType);
                using (var fs = new FileStream(toXmlFilePath, FileMode.CreateNew))
                {
                    var streamWriter = new StreamWriter(fs, TextEncoding);
                    formatter.Serialize(streamWriter, objectClass);
                    res = true;
                }
            }
            catch (Exception ex)
            {
                Base.Log("Error in " + ex.GetType().Name + ".ConvertClassToXMLFile(): " + ex.Message);
            }
            return res;
        }


        public static string ConvertClassToXMLText(object objectClass, Encoding enc = null)
        {
            string result = "";
            Encoding TextEncoding = Encoding.UTF8;
            if (enc != null)
                TextEncoding = enc;
            try
            {
                XmlSerializer formatter = new XmlSerializer(objectClass.GetType());
                var settings = new XmlWriterSettings {
                    Encoding = enc,
                    Indent = true
                };
                using (var ms = new MemoryStream())
                {
                    using (var writer = XmlWriter.Create(ms, settings))
                    {
                        formatter.Serialize(writer, objectClass, null);
                        result = TextEncoding.GetString(ms.ToArray());
                    }
                }
            }
            catch (Exception ex) {
                Base.Log("Error in " + ex.GetType().Name + ".ConvertClassToXMLText(): " + ex.Message);
            }
            return result;
        }

        public static T ConvertXMLTextToClass<T>(string objectClassXMLString)
        {
            T result = default(T);
            Encoding TextEncoding = Encoding.UTF8;
            if (objectClassXMLString.Length > 0) {
                try
                {
                    XmlSerializer formatter = new XmlSerializer(typeof(T));
                    byte[] byteArray = TextEncoding.GetBytes(objectClassXMLString);
                    using (var ms = new MemoryStream(byteArray))
                    {
                        result = (T)formatter.Deserialize(ms);
                    }
                }
                catch (Exception ex)
                {
                    Base.Log("Error in " + ex.GetType().Name + ".ConvertXMLTextToClass(): " + ex.Message);
                }
            }
            return result;
        }

    }
}
