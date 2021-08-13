using System;

namespace Cloneable.Sample
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            DoSimpleClone();
            DoSimpleExplicitClone();
            DoDeepClone();
            DoSafeDeepClone();
            DoBaseClassClone();
        }

        static void DoSimpleClone()
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

        static void DoSimpleExplicitClone()
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

        static void DoDeepClone()
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

        static void DoSafeDeepClone()
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

        static void DoBaseClassClone()
        {
            var classWithInheritance = new ClassWithInheritance();

            classWithInheritance.A = 2;

            var clone = classWithInheritance.Clone();

            Console.WriteLine(clone);
            Console.WriteLine($"Clone A of Inherited Class:{clone.A}");
        }
    }
}
