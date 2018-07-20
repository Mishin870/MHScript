using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mishin870.MHScript.engine {
    /// <summary>
    /// Указатель на пространство функции
    /// Используется для отеделения локальных и глобальных переменных
    /// </summary>
    public enum FunctionType {
        LOCAL,
        GLOBAL
    }
}
