using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mishin870.MHScript.engine.objects {
    /// <summary>
    /// Исключение, завершающее работу скрипта программно
    /// </summary>
    public class ScriptInterruptException : Exception {
        public static readonly int CODE_RETURN = 0;
        public static readonly int CODE_REDIRECT = 302;
        public object data;
        public int code;

        public ScriptInterruptException(int code, object data) {
            this.code = code;
            this.data = data;
        }

    }
}
