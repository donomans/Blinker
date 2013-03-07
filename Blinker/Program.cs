using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blink1Lib;
using System.Threading;
using Topshelf;
using System.IO;

namespace Blinker
{
    class Program
    {
        static void Main(string[] args)
        {
            HostFactory.Run(x =>
            {
                x.Service<Blinky>(s =>
                {
                    s.ConstructUsing(name => new Blinky());
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });
                x.RunAsLocalSystem();
                x.StartAutomatically();
                x.SetDescription("Directory watch blinker");
                x.SetDisplayName("Blinker");
                x.SetServiceName("Blinker");
            });     
                
        }
    }

    public class Blinky
    {
        ///2) set a datetime var as some super early date
        private DateTime checkdate = new DateTime();
        private Boolean ok = false;
        private Blink1 blink1 = new Blink1();
        public Blinky()
        {
            ///0) ensure blink1 is connected
            ///1) check if folder exists
            ok = blink1.open() & Directory.Exists(@"Z:\Unsorted\TV\Complete");
            if(ok)
                blink1.setRGB(0, 0, 0);
            Console.WriteLine("status: " + ok);
        }

        public void Start()
        {
            while (ok)
            {
                ///2) loop every 10 minutes
                ///3) check for folders newer than recorded current time
                ///4) enumerate folders to see if there are files and blink red for 1 minute 
                List<DirectoryInfo> dirs = new List<DirectoryInfo>(
                    Directory.GetDirectories(@"Z:\Unsorted\TV\Complete").
                    Select(d => new DirectoryInfo(d)));

                DirectoryInfo newestdir = dirs
                    .OrderByDescending(di => di.LastWriteTime)
                    .FirstOrDefault();
                if (newestdir.LastWriteTime > checkdate && (newestdir.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                {
                    ///need to blink red for ~10 minutes and then glow dark red
                    for (int x = 0; x < 300; x++)
                    {
                        blink1.fadeToRGB(1500, 255, 255, 0);
                        Thread.Sleep(1500);
                        blink1.fadeToRGB(1500, 0, 0, 0);
                        Thread.Sleep(1500);
                    }
                    blink1.setRGB(100, 100, 0);
                }
                else if (dirs.TrueForAll(p => (p.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden))
                {
                    ///if it's only the junk hidden folders then turn off the light
                    blink1.setRGB(0, 0, 0);
                }
                ///5) record current time over previous datetime
                checkdate = DateTime.Now;
                Thread.Sleep(60 * 10 * 1000);
            }
        }

        public void Stop()
        {
            ok = false;
        }
    }
}
