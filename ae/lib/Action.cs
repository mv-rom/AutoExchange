using System;
//using System.Collections.Generic;
using System.IO;
using System.Linq;
//using System.Linq;
using System.Reflection;
//using System.Text;
//using System.Threading.Tasks;
//using System.Xml.Linq;


//http s://stackoverflow.com/questions/18362368/loading-dlls-at-runtime-in-c-sharp
namespace ae.lib
{
    internal class Action
    {
        private string FileName;
        private string ActionsDir;
        private string FilePath;

        public Action(string FileName) {
            this.FileName = FileName;
            this.ActionsDir = Path.Combine(Base.RunDir, @"actions");
            this.FilePath = Path.Combine(ActionsDir, FileName + ".dll");
        }

        public bool Init()
        {
            bool result = false;
            if (Base.MakeFolder(ActionsDir)) {
                Base.Log("Каталог активности - [" + this.ActionsDir + "].");
                result = true;
            }
            else
                Base.Log("Каталог активности не найден!");
            return result;
        }

        public bool Run()
        {
            bool result = false;

            // проверка на то, есть ли загружаемая сборка среди наших статических ссылок
            // взята из https://support.microsoft.com/ru-ru/kb/837908
            // но вам может быть и не нужна, подумайте сами
            if (!Assembly.GetExecutingAssembly().GetReferencedAssemblies().Any(
                asmName => asmName.FullName.Substring(0, asmName.FullName.IndexOf(",")) == this.FileName)
            ) {
                return false;
            }


            if (File.Exists(FilePath)) {
                var DLL = Assembly.LoadFile(FilePath);
                /* 
                    foreach (Type type in DLL.GetExportedTypes())
                    {
                        var c = Activator.CreateInstance(type);
                        type.InvokeMember("Output", BindingFlags.InvokeMethod, null, c, new object[] { @"Hello" });
                    }
               */
                /*
                    foreach (Type type in DLL.GetExportedTypes())
                    {
                        dynamic c = Activator.CreateInstance(type);
                        c.Output(@"Hello");
                    }
                */
                var theType = DLL.GetType("DLL.ActionReleaseClass");
                var c = Activator.CreateInstance(theType);
                var method = theType.GetMethod("Start");
                method.Invoke(c, new object[] { @"" }); //instance to Base
                result = true;
            } else
                Base.Log("Файл активности не найден в каталоге [" + FilePath + "]");
            return result;
        }
    }
}
