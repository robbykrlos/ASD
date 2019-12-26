using System;

namespace ASDStarter
{
    class Program
    {
        static void Main(string[] args)
        {
            var output = AutoSubtitleDownloader.ASD.Start(args);
            Console.WriteLine(output);
            
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
    }
}
