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
            var host = HostFactory.New(x =>
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
            host.Run();       
        }
    }

    public class Blinky
    {
        ///2) set a datetime var as some super early date
        private DateTime checkdate = new DateTime();
        private Int32 foldercount = 0;
        private Boolean ok = false;
        private Blink1 blink1 = new Blink1();
        public Blinky()
        {
            ///TODO:
            ///1) Create a format for configuration:
            /// - Directories
            /// - Colors
            /// - Some basic check types
            /// - Check frequency
            ///2) 



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
                ///2) loop every 3 minutes
                ///3) check for folders newer than recorded current time
                ///4) enumerate folders to see if there are new folders and blink for 10 minute 
                List<DirectoryInfo> dirs = new List<DirectoryInfo>(
                    Directory.GetDirectories(@"Z:\Unsorted\TV\Complete").
                    Select(d => new DirectoryInfo(d)));

                ///use this to determine color - from blue to green to yellow to orange to red based on how many folders are present
                Int32 newfoldercount = dirs.Count(f => (f.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden);
                
                if (newfoldercount > 0)
                {
                    DirectoryInfo newestdir = dirs
                        .OrderByDescending(di => di.LastWriteTime)
                        .FirstOrDefault();

                    //Console.WriteLine(newestdir.Name);
                    //Console.WriteLine("newestdir datetime: " + newestdir.LastWriteTime.ToString());
                    //Console.WriteLine("checkdate datetime: " + checkdate.ToString());
                    //Console.WriteLine("newfoldercount: " + newfoldercount.ToString());
                    //Console.WriteLine("foldercount: " + foldercount.ToString());
                    //Console.WriteLine(newestdir.Attributes + " : " + ((newestdir.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden).ToString());

                    if ((newestdir.LastWriteTime > checkdate || newfoldercount >= foldercount)
                        && (newestdir.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden)
                    {
                        Rgb colors = ColorPicker(newfoldercount, 20);

                        ///need to blink a color for ~5 minutes and then glow 
                        for (int x = 0; x < 100; x++)
                        {
                            blink1.fadeToRGB(1500, colors.Red, colors.Green, colors.Blue);
                            Thread.Sleep(1500);
                            blink1.fadeToRGB(1500, 0, 0, 0);
                            Thread.Sleep(1500);
                        }
                        blink1.setRGB(colors.Red, colors.Green, colors.Blue);
                    }
                    else if (dirs.Count < 1 || dirs.TrueForAll(p => (p.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden))
                    {
                        ///if it's only the junk hidden folders then turn off the light
                        blink1.setRGB(0, 0, 0);
                    }
                }
                else 
                    blink1.setRGB(0, 0, 0);

                ///5) record current time over previous datetime
                checkdate = DateTime.Now;
                foldercount = newfoldercount;
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
            ///gosh this is bad.  need to redo it when it's 
            byte color = 0;

            Single div = (CurrentCount / (Single)Threshold);
            Console.WriteLine("div-pre: " + div);
            div *= 10;
            Console.WriteLine("div: " + div);
            try
            {
                color = checked((byte)((div / 10) * 255));
                Console.WriteLine("color: " + color);
            }
            catch (OverflowException)
            {
                color = 255;
            }
            if (div < 2)
                return new Rgb { Red = (byte)(color + 64), Green = 0, Blue = (byte)(color + 127) };
            else if (div >= 2 && div < 3.999)
                return new Rgb { Blue = 255, Green = (byte)(color * 2), Red = 0 };
            else if (div >= 4 && div < 5.999)
                return new Rgb { Blue = (byte)(color * 9 / 5), Green = 255, Red = 0 };
            else if (div >= 6 && div < 7.999)
                return new Rgb { Blue = 0, Green = 255, Red = (byte)(color * 9 / 5) };
            else if (div >= 8 && div < 9.999)
                return new Rgb { Blue = 0, Green = (byte)(1 + (255 - -((255 - color) * 5))), Red = 255 };
            else
                return new Rgb { Blue = 0, Green = 0, Red = 255 };

        }
    }
}
