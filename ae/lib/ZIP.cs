﻿//using System;
using System.IO;
//using System.Threading.Tasks;
//using Aspose.Zip;
//using Aspose.Zip.Saving;
using Ionic.Zip;

//Aspose.Zip
//http s://kb.aspose.com/ru/zip/net/how-to-create-7z-archive-in-csharp/#
//http s://products.aspose.com/zip/net/

//Ionic.ZIP:
//http s://documentation.help/DotNetZip/CSharp.htm



namespace ae.lib
{
    /*
        internal class ZIP
        {
            public static bool Create(string SourcePath, string ArchiveNamePath,  bool recursive=false)
            {
                bool result = false;
                using (FileStream zipFile = File.Open(ArchiveNamePath, FileMode.Create))
                {
                    using (Archive archive = new Archive())
                    {
                        archive.CreateEntry(Path.GetFileName(SourcePath), SourcePath);
                        archive.Save(zipFile, new ArchiveSaveOptions()
                        {
                            ParallelOptions = new ParallelOptions() { ParallelCompressInMemory = ParallelCompressionMode.Auto }
                        });
                        result = true;
                    }
                }
                //Base.Log1("Ошибка: упаковки файлов [" + SourcePath + "]: " + ex.Message + "!");
                return result;
            }


            public static bool Extract(string DestinationPath, string ArchivePath)
            {
                bool result = false;
                try
                {
                    using (var archive = new Archive(ArchivePath))
                    {
                        archive.ExtractToDirectory(DestinationPath);
                    }
                    result = true;
                }
                catch (Exception ex)
                {
                    Base.Log1("Ошибка: распоковки архива [" + ArchivePath + "] в папку [" + DestinationPath + "]: " + ex.Message + "!");
                }
                return result;
            }
        }
    */

    internal class ZIP
    {
        public static bool Create(string SourcePath, string ArchiveNamePath, bool recursive = false)
        {
            bool result = false;

            if (File.Exists(SourcePath)) {
                using (FileStream zipFile = File.Open(ArchiveNamePath, FileMode.Create))
                {
                    using (ZipFile loanZip = new ZipFile())
                    {
                        loanZip.AddFile(SourcePath);
                        loanZip.Save(zipFile); //(string.Format("{0}{1}.zip", zipDestinationPath, documentIdentifier.ToString()));
                        result = true;
                    }
                }
            }
            return result;
        }

        public static bool ListCreate(string SourcePath, string ArchiveNamePath, bool recursive = false)
        {
            bool result = false;

            if (Directory.Exists(SourcePath))
            {
                string[] list = Directory.GetFiles(SourcePath);
                if (list.Length > 0)
                {
                    using (FileStream zipFile = File.Open(ArchiveNamePath, FileMode.Create))
                    {
                        using (ZipFile loanZip = new ZipFile())
                        {
                            foreach (var l in list)
                            {
                                loanZip.AddFile(l);
                            }
                            loanZip.Save(zipFile); //(string.Format("{0}{1}.zip", zipDestinationPath, documentIdentifier.ToString()));
                            result = true;
                        }
                    }
                }
            }
            return result;
        }

    }
}
