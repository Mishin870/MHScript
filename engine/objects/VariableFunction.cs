using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Mishin870.MHScript.engine.objects {
    /// <summary>
    /// Функция, вызываемая на объекте
    /// </summary>
    public class VariableFunction {
        public delegate object UniversalFunction(object obj, Engine engine, params object[] args);
        public UniversalFunction function;
        public string description;
        public string functionName;
    }
}
