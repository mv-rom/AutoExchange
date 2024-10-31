using System;
using System.IO;
using System.Text;
//using System.Runtime.Remoting.Messaging;
//using System.Xml;
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


        public static string ConvertClassToXMLText(object objectClass)
        {
            string result = "";
            Encoding TextEncoding = Encoding.Default;
            try
            {
                Type objectType = objectClass.GetType();
                XmlSerializer formatter = new XmlSerializer(objectType);
                using (var mStream = new MemoryStream())
                {
                    formatter.Serialize(mStream, objectClass);
                    result = TextEncoding.GetString(mStream.ToArray());
                }
            }
            catch (Exception ex)
            {
                Base.Log("Error in " + ex.GetType().Name + ".ConvertClassToXMLText(): " + ex.Message);
            }
            return result;
        }

        public static T ConvertXMLTextToClass<T>(string objectClassXMLString)
        {
            T result = default(T);
            Encoding TextEncoding = Encoding.Default;

            if (objectClassXMLString.Length > 0) {
                try
                {
                    XmlSerializer formatter = new XmlSerializer(typeof(T));
                    byte[] byteArray = TextEncoding.GetBytes(objectClassXMLString);
                    using (var mStream = new MemoryStream(byteArray))
                    {
                        result = (T)formatter.Deserialize(mStream);
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

    /*
    public object Create(object parent, object configContext, XmlNode section)
    {
        XmlSerializer xmlSerializer = new XmlSerializer(typeof(RewriterConfiguration));
        return xmlSerializer.Deserialize(new XmlNodeReader(section));
    }

    public override void FromXml(string xml)
    {
        XmlDataDocument xmlDataDocument = new XmlDataDocument();
        xmlDataDocument.LoadXml(xml);
        if (xmlDataDocument.ChildNodes.Count == 0)
        {
            ArgumentException ex = new ArgumentException("Invalid xml. No GpsGate.License node");
            ServerLicenseNoDB.m_nlog.ErrorException(ex.Message, ex);
            throw ex;
        }
        foreach (object obj in xmlDataDocument.ChildNodes)
        {
            XmlElement xmlElement = (XmlElement)obj;
            if (xmlElement.Name == "GpsGate.License")
            {
                foreach (object obj2 in xmlElement)
                {
                    XmlElement xmlElement2 = (XmlElement)obj2;
                    if (xmlElement2.Name == "Key")
                    {
                        this.PublicKey = xmlElement2.InnerText;
                    }
                    if (xmlElement2.Name == "LicenseID")
                    {
                        this.LicenseID = new Guid(xmlElement2.InnerText);
                    }
                    if (xmlElement2.Name == "CustomerID")
                    {
                        this.CustomerID = xmlElement2.InnerText;
                    }
                    if (xmlElement2.Name == "Description")
                    {
                        this.Description = xmlElement2.InnerText;
                    }
                    if (xmlElement2.Name == "Email")
                    {
                        this.Email = xmlElement2.InnerText;
                    }
                    if (xmlElement2.Name == "LicensedUsers")
                    {
                        try
                        {
                            this.LicensedUsers = int.Parse(xmlElement2.InnerText);
                        }
                        catch (Exception)
                        {
                            throw new ParseErrorException("Unable to parse LicensedUsrers.");
                        }
                    }
                    if (xmlElement2.Name == "Signature")
                    {
                        this.Signature = xmlElement2.InnerText;
                    }
                }
            }
        }
    */
}
