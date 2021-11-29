using System;
using System.IO;

namespace CailLomecb.ColorerTake5.Debug
{
    public class Program
    {
        protected class LocalSource : IColorerLineSourceString
        {
            public bool GetLine(int line, out string target, out int length)
            {
                if (line == 0)
                {
                    target = "<root>";
                    length = 6;
                }
                else if (line == 1)
                {
                    target = "</root>";
                    length = 7;
                }
                else
                {
                    target = null;
                    length = 0;
                    return false;
                }

                return true;
            }
        }

        public static void Main()
        {
            if (!File.Exists("../../data/catalog.xml"))
            {
                Console.WriteLine("Copy colorer data from native to bin");
                return ;
            }
            Console.WriteLine("---enter---");
            ParserFactory.DoAllInOneAction();
            Console.WriteLine("---exit---");

            using ParserFactory pf = ParserFactory.Create("../../data/catalog.xml", "console", "xce", 5000);
            using ColorerBaseEditor be = pf.CreateEditor(new LocalSource());

            be.NotifyNameChange("q.xml");
            Console.WriteLine(be.FileType);
            be.NotifyLineCount(2);
            be.NotifyIdle();
            be.ValidateLines(0, 1);
            LineRegion r = be.GetFirstRegionInLine(0);
            if (r == null)
                Console.WriteLine("Didn't start");
            while (r != null)
            {
                Console.WriteLine($"{r.Start}-{r.End}-{r.SyntaxRegion?.Name}");
                r = r.Next;
            }
        }
    }
}
