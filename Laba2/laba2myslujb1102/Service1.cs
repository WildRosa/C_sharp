using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using System.IO.Compression;

namespace laba2myslujb1102
{
    public partial class Service1 : ServiceBase
    {    //Читайте со строчки 40
        Logger logger;
        public Service1()
        {
            InitializeComponent();
            this.CanStop = true;
            this.AutoLog = true;
            this.CanPauseAndContinue = true;
        }

        protected override void OnStart(string[] args)
        {
            logger = new Logger();
            Thread loggerThread = new Thread(new ThreadStart(logger.Start));
            loggerThread.Start();
        }

        protected override void OnStop()
        {
            logger.Stop();
            Thread.Sleep(5000);
        }
    }
    //Мы создаем логгер для просмотра того что происходит в папке
    class Logger
    {
        FileSystemWatcher watcher;
        object obj = new object();
        bool enabled = true;
        bool create = false;
        bool rename = false;
        string forRenaming = "";
        //делаем конструктор логгера в котором описываем наш путь source и events
        public Logger() 
        {
            watcher = new FileSystemWatcher("C:\\source");
            watcher.Deleted += Watcher_Deleted;
            watcher.Created += Watcher_Created;
            watcher.Renamed += Watcher_Renamed;

        }
        //устанавливаем условия работы нашей программы
        public void Start()
        {
            watcher.EnableRaisingEvents = true;
            while (enabled)
            {
                Thread.Sleep(2000);
            }
        }
        //метод остановки нашей программы
        public void Stop()
        {
            watcher.EnableRaisingEvents = false;
            enabled = false;
        }
        //здесь идут 3 eventa связанных с нашем FileSystemWatcher  
        public void Watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            string FileEvent = "удален";
            string filePath = e.FullPath;
            Recording(FileEvent, filePath);
        }

        public void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            string FileEvent = "создан";
            string filePath = e.FullPath;
            Archiving(FileEvent, filePath);
            Recording(FileEvent, filePath);
        }

        public void Watcher_Renamed(object sender, RenamedEventArgs e)
        {
            string FileEvent = "переименован";
            forRenaming = e.FullPath;
            string filePath = e.OldFullPath;
            Archiving(FileEvent, forRenaming);
            Recording(FileEvent, filePath);
        }
        //Наш метод копирования,архивации,деархивации в архив данных
        public void Archiving(string FileEvent, string filePath)
        {
            string path = Path.Combine("C:\\archive2", DateTime.Now.ToString("dd HH mm ss"));
            Directory.CreateDirectory(path);
            string dearchivingPath = Path.Combine(path, "decompress");
            Directory.CreateDirectory(dearchivingPath);
            string gzipPath = Path.Combine(path, "gzips");
            Directory.CreateDirectory(gzipPath);
            try
            {
                FileInfo original = new FileInfo(filePath);
                string name = filePath.Substring(filePath.LastIndexOf("\\") + 1);
                FileStream fs = File.Create(Path.Combine(path, name));
                fs.Close();
                using (StreamReader read = original.OpenText())
                {
                    using (StreamWriter writer = new StreamWriter(Path.Combine(path, name), true))
                    {
                        string buffer = "";
                        while ((buffer = read.ReadLine()) != null)
                        {
                            writer.WriteLine(buffer);
                        }
                    }
                }
                name += ".gz";
                FileStream fsforgzip = File.Create(Path.Combine(gzipPath, name));
                fsforgzip.Close();
                using (FileStream file = new FileStream(filePath, FileMode.Open))
                {
                    using (FileStream zipfile = new FileStream(Path.Combine(gzipPath, name), FileMode.Open))
                    {
                        using (GZipStream gzip = new GZipStream(zipfile, CompressionMode.Compress))
                        {
                            file.CopyTo(gzip);
                        }
                    }
                }
                 name = filePath.Substring(filePath.LastIndexOf("\\") + 1);
                 using (FileStream DecFile = new FileStream(gzipPath, FileMode.Open))
                 {
                     using (FileStream decompressedFileStream = File.Create(Path.Combine(dearchivingPath, name)))
                     {
                         using (GZipStream decompressionStream = new GZipStream(DecFile, CompressionMode.Decompress))
                         {
                             decompressionStream.CopyTo(decompressedFileStream);

                         }
                     }
                 }
                 
                if (FileEvent == "создан")
                {
                    create = true;
                }
                else if (FileEvent == "переименован")
                {
                    rename = true;
                }


            }
            catch (Exception e)
            {
                using (StreamWriter writer = new StreamWriter("C:\\Vika\\logforsecondlab.txt", true))
                {
                    writer.WriteLine($"{DateTime.Now.ToString("dd.MM HH:mm:ss")} произошла ошибка {e.Message} ");


                    writer.Flush();
                }
                throw;
            }

        }
        //запись информации в наш файл лога
        public void Recording(string FileEvent, string filePath)
        {


            lock (obj)
            {
                using (StreamWriter writer = new StreamWriter("C:\\Vika\\logforsecondlab.txt", true))
                {
                    writer.WriteLine($"{DateTime.Now.ToString("dd.MM HH:mm:ss")} Файл {filePath} был {FileEvent}");

                    switch (FileEvent)
                    {
                        case "создан":
                            if (create == true)
                            {
                                writer.WriteLine($"Файл {filePath} также был отправлен в архив");
                            }

                            break;
                        case "переименован":
                            writer.WriteLine($" в {forRenaming}");
                            if (rename == true)
                            {
                                writer.WriteLine($"Файл {forRenaming} также был отправлен в архив");
                            }
                            break;

                    }
                    writer.Flush();
                }
            }
        }
    }
}
