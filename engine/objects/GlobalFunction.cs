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
        public delegate object UniversalFunction(Engine engine, params object[] args);
        /// <summary>
        /// Делегат на саму функцию
        /// </summary>
        public UniversalFunction function;
        /// <summary>
        /// Описание функции (для документации)
        /// </summary>
        public string functionDocsDescription;
        /// <summary>
        /// Название функции (для документации)
        /// </summary>
        public string functionDocsName;
    }
}
