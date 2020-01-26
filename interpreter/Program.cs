using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace interpreter
{
    class Program
    {
        static void Main(string[] args)
        {
            var interpreter = new Interpreter();
            interpreter.LoadScript("script.code");
        }
    }
}
