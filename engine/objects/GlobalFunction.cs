using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Mishin870.MHScript.engine.objects {
    /// <summary>
    /// Функция, описанная при помощи делегата на функцию C#
    /// </summary>
    public class GlobalFunction {
        public delegate object UniversalFunction(StringWriter output, Engine engine, params object[] args);
        public UniversalFunction function;
        public string description;
        public string functionName;
    }
}
