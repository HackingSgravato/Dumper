using System;
using System.Diagnostics;
using DumperN;

namespace Dumper_Test
{
    internal class Program
    {
        static void Main()
        {
            Dumper d = new Dumper();
            d.addProcess("explorer");
            d.addProcess("6100");
            d.addProcess("3616");
            d.customLoad(); 

            Console.WriteLine("processes dumped in " + d.ElapsedMilliseconds + "ms");
            Console.WriteLine(d.warningMessage);
            foreach (string resultFile in d.resultFiles)
                Process.Start(resultFile);

            Console.ReadKey();
        }
    }
}
