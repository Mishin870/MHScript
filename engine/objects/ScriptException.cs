using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mishin870.MHScript.engine.objects {
    /// <summary>
    /// Исключение при выполнении скрипта
    /// </summary>
    public class ScriptException : Exception {
        public static readonly int FUNCTION_NOT_FOUND = 0;
        public static readonly int UNKNOWN_VARIABLE_TYPE = 1;
        public static readonly int NULL_POINTER = 2;
        public static readonly int VARIABLE_ALREADY_EXISTS = 3;
        public static readonly int DEPRECATED = 4;
        public static readonly int ILLEGAL_STATE = 5;
        public static readonly int OUTER_EXCEPTION = 6;
        public int code;

        public ScriptException(int code, string message)
            : base(message) {
            this.code = code;
        }

    }
}
