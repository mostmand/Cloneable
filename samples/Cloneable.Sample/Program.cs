using System;

namespace Cloneable.Sample
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            DoSimpleClone();
            DoSimpleExplicitClone();
            DoDeepClone();
            DoSafeDeepClone();
        }

        private static void DoSimpleClone()
        {
            // Uses the Clone method on a class with no circular references
            var obj = new SimpleClone()
            {
                A = "salam",
                B = 100
            };
            var clone = obj.Clone();
            Console.WriteLine(clone);
            Console.WriteLine("Clone equals original: " + (clone == obj));
            Console.WriteLine();
        }

        private static void DoSimpleExplicitClone()
        {
            // Uses the Clone method on a class with no circular references
            var obj = new SimpleCloneExplicit()
            {
                A = "salam",
                B = 100
            };
            var clone = obj.Clone();
            Console.WriteLine(clone);
            Console.WriteLine("Clone equals original: " + (clone == obj));
            Console.WriteLine();
        }

        private static void DoDeepClone()
        {
            // Uses the Clone method on a class with no circular references
            var obj = new SimpleClone()
            {
                A = "salam",
                B = 100
            };
            var deep = new DeepClone()
            {
                A = "first",
                Simple = obj
            };
            var clone = deep.Clone();
            Console.WriteLine(clone);
            Console.WriteLine("Clone equals original: " + (clone == deep));
            Console.WriteLine();
        }

        private static void DoSafeDeepClone()
        {
            // Uses the Clone method on a class with no circular references
            var child = new SafeDeepCloneChild()
            {
                A = "child"
            };
            var parent = new SafeDeepClone()
            {
                A = "parent",
                Child = child
            };
            child.Parent = parent;
            var clone = parent.CloneSafe();
            Console.WriteLine(clone);
            Console.WriteLine("Clone equals original: " + (clone == parent));
            Console.WriteLine("Is parents child copied: " + (clone.Child != parent.Child));
            Console.WriteLine();
        }
    }
}
