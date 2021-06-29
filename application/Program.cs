using System;
using SourceGenerator;

namespace App
{
    class Program
    {
        static void Main(string[] args)
        {
            Employee e = new();
            e.Name = "Employee";
            System.Console.WriteLine(e.Name);

            Person p = new();
            p.Name = "Person";
            System.Console.WriteLine(p.Name);
        }
    }
}
