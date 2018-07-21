using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Mishin870.MHScript.engine {

    /// <summary>
    /// Логгер исключений скрипта
    /// </summary>
    public class ExceptionHelper {
        /// <summary>
        /// Ожидался не NULL аргумент
        /// </summary>
        public static readonly string ARGUMENT_NULL = "argument is null";
        /// <summary>
        /// Ожидалось большее количество аргументов
        /// </summary>
        public static readonly string ARGUMENT_FEW = "too few arguments";
        /// <summary>
        /// Аргумент должен быть строковым
        /// </summary>
        public static readonly string ARGUMENT_STRING = "argument is not string";
        /// <summary>
        /// Аргумент должен быть вещественным
        /// </summary>
        public static readonly string ARGUMENT_FLOAT = "argument is not float";
        /// <summary>
        /// Аргумент должен быть логическим
        /// </summary>
        public static readonly string ARGUMENT_BOOL = "argument is not bool";

        /// <summary>
        /// Сообщение о исключении, вызванным аргументами функции
        /// </summary>
        /// <param name="output">поток вывода лога</param>
        /// <param name="functionName">название функции</param>
        /// <param name="message">сообщение исключения</param>
        public static void logArg(string functionName, string message) {
            Console.WriteLine("[Warning] ArgumentException: " + message + " in function " + functionName);
        }

        /// <summary>
        /// Сообщение об общем исключении в процессе работы скрипта
        /// </summary>
        /// <param name="output">поток вывода лога</param>
        /// <param name="message">описание исключения</param>
        public static void logCommon(string message) {
            Console.WriteLine("[Warning] Exception: " + message);
        }

    }
}
