using System;
using _2;
using _4;
using _5;

namespace _1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            _2.Class1.M();

            // new sdk uses transitive project references by default
            _4.Class1.M();
            _5.Class1.M();
        }
    }
}
