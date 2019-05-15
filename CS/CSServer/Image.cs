using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace CSNamedPipe
{
    class Image
    {
        public string filedata;
        public static FileInfo fileinfo;
        public static string directoryPath = @".\tmp";

        public Image(string filename)
        {
            Console.WriteLine("Filename :  " + filename);
            fileinfo = new FileInfo(filename);
            Compress();
            filedata = File.ReadAllText(fileinfo.FullName + ".gz");
            Console.WriteLine("Filename :  " + filedata);
        }

        public static void Compress()
        {
            using (FileStream originalFileStream = fileinfo.OpenRead())
            {
                if ((File.GetAttributes(fileinfo.FullName) & FileAttributes.Hidden) != FileAttributes.Hidden & fileinfo.Extension != ".gz")
                {
                    using (FileStream compressedFileStream = File.Create(fileinfo.FullName + ".gz"))
                    {
                        if (compressedFileStream == null)
                        {
                            Console.Write("Error: file didn't create correctly");
                            return;
                        }
                        using (GZipStream compressionStream = new GZipStream(compressedFileStream, CompressionMode.Compress))
                        {
                            originalFileStream.CopyTo(compressionStream);
                        }
                    }
                    FileInfo info = new FileInfo(directoryPath + Path.DirectorySeparatorChar + fileinfo.Name + ".gz");
                }
            }
        }

        public string getFile()
        {
            return (filedata);
        }
    }
}
