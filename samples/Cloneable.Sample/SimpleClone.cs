using System;

namespace Cloneable.Sample
{
    [Cloneable]
    public partial class SimpleClone
    {
        public string A { get; init; }
        
        [IgnoreClone]
        public int B { get; init; }

        public override string ToString()
        {
            return $"{nameof(SimpleClone)}:{Environment.NewLine}" +
                $"\tA:\t{A}" +
                Environment.NewLine +
                $"\tB:\t{B}";
        }
    }
}
