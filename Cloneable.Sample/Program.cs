using System;

namespace Cloneable.Sample
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var a = new Foo()
            {
                A = "salam"
            };
            var b = a.Clone();
            Console.WriteLine(b.A);
        }
    }
}
