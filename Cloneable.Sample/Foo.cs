using System;

namespace Cloneable.Sample
{
    [Cloneable]
    public partial class Foo
    {
        public string A { get; set; }
        public int B { get; set; }

        public Foo()
        {

        }

        public override string ToString()
        {
            return $"Foo:{Environment.NewLine}" +
                $"\tA:\t{A}" +
                Environment.NewLine +
                $"\tB:\t{B}";
        }
    }
}
