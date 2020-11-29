using System;

namespace Cloneable.Sample
{
    [Cloneable(ExplicitDeclaration = true)]
    public partial class SimpleCloneExplicit
    {
        public string A { get; set; }
     
        [Clone]
        public int B { get; set; }

        public override string ToString()
        {
            return $"{nameof(SimpleCloneExplicit)}:{Environment.NewLine}" +
                $"\tA:\t{A}" +
                Environment.NewLine +
                $"\tB:\t{B}";
        }
    }
}
