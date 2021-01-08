using System;

namespace Cloneable.Sample
{
    [Cloneable]
    public partial class SafeDeepClone
    {
        public string A { get; set; }
        public SafeDeepCloneChild Child { get; set; }

        public override string ToString()
        {
            return $"{nameof(SafeDeepClone)}:{Environment.NewLine}" +
                $"\tA:\t{A}" +
                Environment.NewLine +
                $"\tChild.A:\t{Child?.A}";
        }
    }

    [Cloneable]
    public partial class SafeDeepCloneChild
    {
        public string A { get; set; }
        public SafeDeepClone Parent { get; set; }

        public override string ToString()
        {
            return $"{nameof(SafeDeepCloneChild)}:{Environment.NewLine}" +
                   $"\tA:\t{A}" +
                   Environment.NewLine +
                   $"\tParent.A:\t{Parent?.A}";
        }
    }
}
