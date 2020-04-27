using System;
using System.IO;
using System.Threading.Tasks;

namespace Consumer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // violates CA2000
            new FileStream("", default);

            // violates CA2007
            await Console.Out.WriteLineAsync("hi");
        }
    }
}
