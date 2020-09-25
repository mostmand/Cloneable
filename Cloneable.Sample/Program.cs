using System;

namespace Cloneable.Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            var a = new Foo()
            {
                A = "salam"
            };
            var b = a.Clone();
            System.Console.WriteLine(b.A);
        }
    }
}
