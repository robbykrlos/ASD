using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASDStarter
{
    class Program
    {
        static void Main(string[] args)
        {
            var output = AutoDownloadSubtitle.ASD.Start(args);
            Console.WriteLine(output);
            
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
    }
}
