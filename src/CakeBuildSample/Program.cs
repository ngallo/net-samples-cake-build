using CakeBuildLib;
using System;

namespace CakeBuildSample
{
    class Program
    {
        static void Main(string[] args)
        {
            var calculator = new Calculator();
            Console.WriteLine("1 + 5 = {0}", calculator.Sum(1,5));
            Console.ReadLine();
        }
    }
}
