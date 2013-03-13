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

                ///use this to determine color - from blue to green to yellow to orange to red based on how many folders are present
                Int32 newfoldercount = dirs.Count(f => (f.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden);
                Rgb colors = ColorPicker(newfoldercount, 15);

                DirectoryInfo newestdir = dirs
                    .OrderByDescending(di => di.LastWriteTime)
                    .FirstOrDefault();
                Console.WriteLine(newestdir.Name);
                Console.WriteLine("newestdir datetime: " + newestdir.LastWriteTime.ToString());
                Console.WriteLine("checkdate datetime: " + checkdate.ToString());
                Console.WriteLine(newestdir.Attributes + " : " + ((newestdir.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden).ToString());
                    
                if (newestdir.LastWriteTime > checkdate && (newestdir.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden)
                {
                    ///need to blink red for ~10 minutes and then glow 
                    for (int x = 0; x < 200; x++)
                    {
                        blink1.fadeToRGB(1500, colors.Red, colors.Green, colors.Blue);
                        Thread.Sleep(1500);
                        blink1.fadeToRGB(1500, colors.Red, colors.Green, colors.Blue);
                        Thread.Sleep(1500);
                    }
                    blink1.setRGB(colors.Red, colors.Green, colors.Blue);
                }
                else if (dirs.TrueForAll(p => (p.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden))
                {
                    ///if it's only the junk hidden folders then turn off the light
                    blink1.setRGB(0, 0, 0);
                }
                ///5) record current time over previous datetime
                checkdate = DateTime.Now;
                Thread.Sleep(60 * 3 * 1000);
            }
        }

        public void Stop()
        {
            ok = false;
        }

        struct Rgb 
        {
            public short Red;
            public short Green;
            public short Blue;
        }
        private Rgb ColorPicker(Int32 CurrentCount, Int32 Threshold)
        {
            byte color = 0;
            
            Single div = (CurrentCount / Threshold);
            div *= 5;

            try{
                color = checked((byte)((div / 5) * 255));
            }
            catch(OverflowException)
            {
                color = 255;
            }
            if (CurrentCount < 1)
                return new Rgb { Red = 0, Green = 0, Blue = 0 };
            else if (div > 1 && div < 1.999)
                return new Rgb { Blue = 255, Green = color, Red = 0 };
            else if (div > 2 && div < 2.999)
                return new Rgb { Blue = color, Green = 255, Red = 0 };
            else if (div > 3 && div < 3.999)
                return new Rgb { Blue = 0, Green = 255, Red = color };
            else 
                return new Rgb { Blue = 0, Green = 0, Red = 255 };
            
        }
    }
}
