using System;

namespace ASDStarter
{
    class Program
    {
        static async System.Threading.Tasks.Task Main(string[] args)
        {
            var output = await AutoSubtitleDownloader.ASD.StartAsync(args);
            Console.WriteLine(output);
            
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
    }
}
