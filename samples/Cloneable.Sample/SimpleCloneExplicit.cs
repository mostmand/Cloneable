using System;

namespace Cloneable.Sample
{
    [Cloneable(ExplicitDeclaration = true)]
    public partial class SimpleCloneExplicit
    {
        public string A { get; init; }
     
        [Clone]
        public int B { get; init; }

        public override string ToString()
        {
            return $"{nameof(SimpleCloneExplicit)}:{Environment.NewLine}" +
                $"\tA:\t{A}" +
                Environment.NewLine +
                $"\tB:\t{B}";
        }
    }
}
