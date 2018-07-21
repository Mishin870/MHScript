using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mishin870.MHScript.engine.commands;

namespace Mishin870.MHScript.engine.objects {
    /// <summary>
    /// Локальная функция, объявленная в скрипте
    /// </summary>
    public class LocalFunction {
        /// <summary>
        /// Название функции
        /// </summary>
        public string name;
        /// <summary>
        /// Ссылка на блок кода функции
        /// </summary>
        public Script code;
        /// <summary>
        /// Названия аргументов функции
        /// </summary>
        public List<string> args;
    }
}
