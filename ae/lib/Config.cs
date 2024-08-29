using System;
using System.IO;
using ae.lib.classes.AE;


namespace ae.lib
{
    internal class Config
    {
        private string FileName = "ae.jsonconfig";
        private string FilePath = "";
        public ConfigClass ConfigSettings = null;

        public bool Init()
        {
            bool result = false;
            if (File.Exists(this.FilePath)) {
                string buff = File.ReadAllText(this.FilePath);
                try {
                    this.ConfigSettings = JSON.fromJSON<ConfigClass>(buff);
                    result = true;
                }
                catch (Exception ex) {
                    Base.LogError("Error in Config.Init(): " + ex.Message);
                }
            }
            return result;
        }

        public void Save(object Obj)
        {
            File.WriteAllText(this.FilePath+"__save", JSON.toJSON(Obj));
        }
        public Config()
        {
            this.FilePath = Path.Combine(Base.RunDir, FileName);
        }


/*
        public static object GetValName(object obj, string variableName)
        {
            // Get the type of the object
            Type type =  obj.GetType();

            // Get the field or property by name
            FieldInfo field = type.GetField(variableName);
            PropertyInfo property = type.GetProperty(variableName);

            if (field != null)
            {
                // If it's a field, return its value
                return field.GetValue(obj);
            }
            else if (property != null)
            {
                // If it's a property, return its value
                return property.GetValue(obj);
            }
            else
            {
                // Variable with the given name not found
                throw new ArgumentException("Variable "+variableName+" not found in class.");
            }
        }
*/
    }


}
