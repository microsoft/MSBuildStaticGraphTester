using System;
using B;
using C;

namespace A
{
    public class Class1
    {
        public static int A = B.Class1.B + C.Class1.C;
    }
}
