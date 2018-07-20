using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Globalization;
using Mishin870.MHScript.engine;
using Mishin870.MHScript.engine.objects;

namespace Mishin870.MHScript.engine {

    public class Engine {
        private static readonly Dictionary<string, VariableFunction> stringFunctions = new Dictionary<string, VariableFunction>();
        private static readonly Dictionary<string, VariableFunction> listFunctions = new Dictionary<string, VariableFunction>();
        private static readonly Dictionary<string, VariableFunction> dictFunctions = new Dictionary<string, VariableFunction>();
        
        private static Dictionary<string, GlobalFunction> functions = new Dictionary<string, GlobalFunction>();

        private Dictionary<string, Variable> variables = new Dictionary<string, Variable>();
        private Dictionary<string, LocalFunction> localFunctions = new Dictionary<string, LocalFunction>();

        private List<FunctionType> callStack = new List<FunctionType>();
        private List<Dictionary<string, Variable>> argsStack = new List<Dictionary<string, Variable>>();
        private int stackPointer = -1;
        private int localFunctionsInCallStack = 0;

        static Engine() {
            stringFunctions.Add("sub", new VariableFunction() {
                function = string_sub,
                functionName = "string sub(start, length)",
                description = "Возвращает подстроку из строки"
            });
            stringFunctions.Add("size", new VariableFunction() {
                function = string_size,
                functionName = "int size()",
                description = "Получить длину строки"
            });
            stringFunctions.Add("reverse", new VariableFunction() {
                function = string_reverse,
                functionName = "string reverse()",
                description = "Инвертировать строку"
            });

            listFunctions.Add("add", new VariableFunction() {
                function = list_add,
                functionName = "List<object> add(object... args)",
                description = "Добавляет элементы args в массив. Возвращает сам массив"
            });
            listFunctions.Add("remove", new VariableFunction() {
                function = list_remove,
                functionName = "List<object> remove(index)",
                description = "Удаляет элемент с индексом index. Возвращает сам массив"
            });
            listFunctions.Add("clear", new VariableFunction() {
                function = list_clear,
                functionName = "List<object> clear()",
                description = "Очистить массив. Возвращает сам массив"
            });
            listFunctions.Add("size", new VariableFunction() {
                function = list_size,
                functionName = "int size()",
                description = "Получить размер массива"
            });
            listFunctions.Add("reverse", new VariableFunction() {
                function = list_reverse,
                functionName = "List<object> reverse()",
                description = "Перевернуть массив. Возвращает сам массив"
            });
            listFunctions.Add("have", new VariableFunction() {
                function = list_have,
                functionName = "List<object> have(obj)",
                description = "Содержит ли данный список элемент obj?"
            });

            dictFunctions.Add("add", new VariableFunction() {
                function = dict_add,
                functionName = "Dictionary<string, object> add((string, object)... args)",
                description = "Добавляет элементы с ключами, перечисленных по очереди в args, в словарь. Возвращает сам словарь"
            });
            dictFunctions.Add("remove", new VariableFunction() {
                function = dict_remove,
                functionName = "Dictionary<string, object> remove(key)",
                description = "Удаляет элемент с ключём key. Возвращает сам словарь"
            });
            dictFunctions.Add("clear", new VariableFunction() {
                function = dict_clear,
                functionName = "Dictionary<string, object> clear()",
                description = "Очистить словарь. Возвращает сам словарь"
            });
            dictFunctions.Add("size", new VariableFunction() {
                function = dict_size,
                functionName = "int size()",
                description = "Получить размер словаря. Количество пар ключ => значение"
            });
            dictFunctions.Add("keys", new VariableFunction() {
                function = dict_keys,
                functionName = "List<object> keys()",
                description = "Получить массив ключей словаря"
            });
            dictFunctions.Add("values", new VariableFunction() {
                function = dict_values,
                functionName = "List<object> values()",
                description = "Получить массив значений словаря"
            });
            dictFunctions.Add("have", new VariableFunction() {
                function = dict_have,
                functionName = "bool have(key)",
                description = "Есть ли ключ key в этом словаре?"
            });
            dictFunctions.Add("toUrlArgs", new VariableFunction() {
                function = dict_to_url_args,
                functionName = "string toUrlArgs()",
                description = "Возвращает строку аргументов, созданную на основе этого словаря"
            });
        }

        #region DOT_FUNCTIONS
        private static object string_sub(object obj, StringWriter output, Engine engine, params object[] args) {
            if (args.Length >= 2 && obj is string && args[0] != null && args[1] != null)
                return ((string) obj).Substring((int) ((float) args[0]), (int) ((float) args[1]));
            return null;
        }
        private static object string_size(object obj, StringWriter output, Engine engine, params object[] args) {
            return (float) ((string) obj).Length;
        }
        private static object string_reverse(object obj, StringWriter output, Engine engine, params object[] args) {
            char[] arr = ((string) obj).ToCharArray();
            Array.Reverse(arr);
            return new string(arr);
        }

        private static object list_add(object obj, StringWriter output, Engine engine, params object[] args) {
            List<object> list = (List<object>) obj;
            foreach (object arg in args)
                list.Add(obj);
            return obj;
        }
        private static object list_remove(object obj, StringWriter output, Engine engine, params object[] args) {
            List<object> list = (List<object>) obj;
            if (args.Length == 1 && args[0] != null)
                list.RemoveAt((int) ((float) args[0]));
            return obj;
        }
        private static object list_clear(object obj, StringWriter output, Engine engine, params object[] args) {
            List<object> list = (List<object>) obj;
            list.Clear();
            return obj;
        }
        private static object list_size(object obj, StringWriter output, Engine engine, params object[] args) {
            return (float) ((List<object>) obj).Count;
        }
        private static object list_reverse(object obj, StringWriter output, Engine engine, params object[] args) {
            ((List<object>) obj).Reverse();
            return obj;
        }
        private static object list_have(object obj, StringWriter output, Engine engine, params object[] args) {
            if (args.Length > 0 && args[0] != null)
                return ((List<object>) obj).Contains(args[0]);
            return false;
        }

        private static object dict_add(object obj, StringWriter output, Engine engine, params object[] args) {
            Dictionary<string, object> dict = (Dictionary<string, object>) obj;
            for (int i = 0; i < args.Length; i += 2) {
                string key = (string) args[i];
                if (key == null || dict.ContainsKey(key))
                    continue;
                dict[key] = args[i + 1];
            }
            return dict;
        }
        private static object dict_remove(object obj, StringWriter output, Engine engine, params object[] args) {
            Dictionary<string, object> dict = (Dictionary<string, object>) obj;
            if (args.Length == 1 && args[0] != null)
                dict.Remove((string) args[0]);
            return obj;
        }
        private static object dict_clear(object obj, StringWriter output, Engine engine, params object[] args) {
            Dictionary<string, object> dict = (Dictionary<string, object>) obj;
            dict.Clear();
            return obj;
        }
        private static object dict_size(object obj, StringWriter output, Engine engine, params object[] args) {
            return (float) ((Dictionary<string, object>) obj).Count;
        }
        private static object dict_keys(object obj, StringWriter output, Engine engine, params object[] args) {
            List<object> keys = new List<object>();
            Dictionary<string, object> dict = (Dictionary<string, object>) obj;
            foreach (string key in dict.Keys)
                keys.Add(key);
            return keys;
        }
        private static object dict_values(object obj, StringWriter output, Engine engine, params object[] args) {
            List<object> values = new List<object>();
            Dictionary<string, object> dict = (Dictionary<string, object>) obj;
            foreach (string value in dict.Values)
                values.Add(value);
            return values;
        }
        private static object dict_have(object obj, StringWriter output, Engine engine, params object[] args) {
            if (args.Length > 0 && args[0] != null)
                return ((Dictionary<string, object>) obj).ContainsKey((string) args[0]);
            return false;
        }
        private static object dict_to_url_args(object obj, StringWriter output, Engine engine, params object[] args) {
            Dictionary<string, object> dict = (Dictionary<string, object>) obj;
            if (dict.Count > 0) {
                string result = "";
                bool first = true;
                foreach (string key in dict.Keys) {
                    result += (first ? "?" : "&") + key + "=" + dict[key];
                    first = false;
                }
                return result;
            } else {
                return "";
            }
        }
        #endregion
        #region VARIABLE
        /// <summary>
        /// Добавить переменную в движок
        /// </summary>
        public void addVariable(string variableName, Variable variable) {
            if (!variables.ContainsKey(variableName)) {
                variables.Add(variableName, variable);
            } else {
                throw new ScriptException(ScriptException.VARIABLE_ALREADY_EXISTS, "Попытка добавить уже существующую переменную: \"" + variableName + "\"!");
            }
        }
        /// <summary>
        /// Удалить переменную из движка
        /// </summary>
        public void removeVariable(string variableName) {
            if (variables.ContainsKey(variableName))
                variables.Remove(variableName);
        }
        /// <summary>
        /// Получить переменную из движка
        /// </summary>
        public Variable getVariable(string variableName) {
            if ((localFunctionsInCallStack > 0 && callStack[stackPointer] == FunctionType.LOCAL) && argsStack[stackPointer].ContainsKey(variableName)) {
                return argsStack[stackPointer][variableName];
            } else {
                if (variables.ContainsKey(variableName)) {
                    return variables[variableName];
                } else {
                    Variable newVariable = new Variable();
                    newVariable.value = null;
                    addVariable(variableName, newVariable);
                    return newVariable;
                }
            }
        }
        /// <summary>
        /// Проверка существования переменной
        /// </summary>
        public bool isVariableSet(string variableName) {
            return variables.ContainsKey(variableName);
        }
        #endregion
        #region FUNCTION
        /// <summary>
        /// Добавить функцию в движок
        /// </summary>
        public void addFunction(string functionName, GlobalFunction function) {
            functions.Add(functionName, function);
        }
        /// <summary>
        /// Получить функцию из движка
        /// </summary>
        public GlobalFunction getFunction(string functionName) {
            if (functions.ContainsKey(functionName)) {
                return functions[functionName];
            } else {
                return null;
            }
        }
        /// <summary>
        /// Добавить все локальные функции в движок из скрипта
        /// </summary>
        public void addLocalFunctions(List<LocalFunction> localFunctions) {
            foreach (LocalFunction localFunction in localFunctions)
                this.localFunctions[localFunction.name] = localFunction;
        }
        #endregion

        #region DEFAULT_FUNCTIONS
        /// <summary>
        /// Добавляет в движок все необходимые стандартные функции
        /// </summary>
        public void addDefaultFunctions() {
            addFunction("array", new GlobalFunction() {
                function = new GlobalFunction.UniversalFunction(array),
                functionName = "List<object> array(object... args)",
                description = "Создаёт массив из объектов args"
            });
            addFunction("dict", new GlobalFunction() {
                function = new GlobalFunction.UniversalFunction(dict),
                functionName = "Dictionary<string, object> dict((string, object)... args)",
                description = "Создаёт словарь из ключей и объектов, перечисленных по очереди"
            });
            addFunction("echo", new GlobalFunction() {
                function = new GlobalFunction.UniversalFunction(echo),
                functionName = "void echo(value)",
                description = "Выводит value на страницу"
            });
            addFunction("isnull", new GlobalFunction() {
                function = new GlobalFunction.UniversalFunction(isnull),
                functionName = "bool isnull(object)",
                description = "Возвращает true, если object равен null. Иначе false"
            });
            addFunction("isset", new GlobalFunction() {
                function = new GlobalFunction.UniversalFunction(isset),
                functionName = "bool isset(variableName)",
                description = "Возвращает true, если переменная с именем variableName существует"
            });
            addFunction("unset", new GlobalFunction() {
                function = new GlobalFunction.UniversalFunction(unset),
                functionName = "void unset(variableName)",
                description = "Удаляет переменную"
            });
            addFunction("file_exists", new GlobalFunction() {
                function = new GlobalFunction.UniversalFunction(file_exists),
                functionName = "bool file_exists(fileName)",
                description = "Возвращает true, если файл fileName существует. Иначе false"
            });
            addFunction("float", new GlobalFunction() {
                function = new GlobalFunction.UniversalFunction(float_val),
                functionName = "float float(value, defaultValue)",
                description = "Конвертирует любой объект в float"
            });
            addFunction("string", new GlobalFunction() {
                function = new GlobalFunction.UniversalFunction(string_val),
                functionName = "string string(value, defaultValue)",
                description = "Конвертирует любой объект в string"
            });
            addFunction("var_dump", new GlobalFunction() {
                function = new GlobalFunction.UniversalFunction(var_dump),
                functionName = "void var_dump(object)",
                description = "Выводит полную информацию о переменной"
            });
            addFunction("limit", new GlobalFunction() {
                function = new GlobalFunction.UniversalFunction(limit),
                functionName = "string limit(source, count)",
                description = "Ограничивает строку source в count символов. Плюс троеточие, если сокращена"
            });
            addFunction("files", new GlobalFunction() {
                function = new GlobalFunction.UniversalFunction(files),
                functionName = "List<object> files(path, [pattern])",
                description = "Получить список файлов"
            });
            addFunction("dirs", new GlobalFunction() {
                function = new GlobalFunction.UniversalFunction(dirs),
                functionName = "List<object> dirs(path, [pattern])",
                description = "Получить список папок"
            });
            addFunction("file_get_contents", new GlobalFunction() {
                function = new GlobalFunction.UniversalFunction(file_get_contents),
                functionName = "string file_get_contents(fileName)",
                description = "Получить содержимое файла"
            });
            addFunction("file_get_lines", new GlobalFunction() {
                function = new GlobalFunction.UniversalFunction(file_get_lines),
                functionName = "List<string> file_get_lines(fileName)",
                description = "Получить содержимое файла по строчкам"
            });
            
        }
        
        private string getErrOneArg(object[] args) {
            if (args.Length == 0) {
                return ErrorHelper.ARGUMENT_FEW;
            } else if (args[0] == null) {
                return ErrorHelper.ARGUMENT_NULL;
            } else {
                return "unknown error";
            }
        }
        private string getErrOneArgString(object[] args) {
            if (args.Length == 0) {
                return ErrorHelper.ARGUMENT_FEW;
            } else if (args[0] == null) {
                return ErrorHelper.ARGUMENT_NULL;
            } else if (!(args[0] is string)) {
                return ErrorHelper.ARGUMENT_STRING;
            } else {
                return "unknown error";
            }
        }
        private object array(StringWriter output, Engine engine, params object[] args) {
            List<object> arr = new List<object>();
            foreach (object obj in args)
                arr.Add(obj);
            return arr;
        }
        private object dict(StringWriter output, Engine engine, params object[] args) {
            Dictionary<string, object> arr = new Dictionary<string, object>();
            for (int i = 0; i < args.Length; i += 2) {
                string key = (string) args[i];
                if (key == null || arr.ContainsKey(key))
                    continue;
                arr[key] = args[i + 1];
            }
            return arr;
        }
        private object echo(StringWriter output, Engine engine, params object[] args) {
            if (args.Length >= 1) {
                if (args[0] != null) {
                    if (args[0] is string) {
                        output.Write((string) args[0]);
                    } else if (args[0] is float) {
                        output.Write((float) args[0]);
                    } else if (args[0] is bool) {
                        output.Write((bool) args[0]);
                    } else if (args[0] is List<object>) {
                        output.Write("array(" + ((List<object>) args[0]).Count + ")");
                    } else if (args[0] is Dictionary<string, object>) {
                        output.Write("dict(" + ((Dictionary<string, object>) args[0]).Count + ")");
                    } else if (args[0] is CustomVariable) {
                        output.Write("custom_object");
                    } else {
                        output.Write("error");
                    }
                } else {
                    output.Write("null");
                }
            } else {
                ErrorHelper.logArg(output, "echo", ErrorHelper.ARGUMENT_FEW);
            }
            return null;
        }
        private object isset(StringWriter output, Engine engine, params object[] args) {
            if (args.Length > 0 && args[0] != null) {
                return isVariableSet(args[0].ToString());
            } else {
                ErrorHelper.logArg(output, "isset", getErrOneArg(args));
                return false;
            }
        }
        private object unset(StringWriter output, Engine engine, params object[] args) {
            if (args.Length > 0 && args[0] != null) {
                removeVariable(args[0].ToString());
            } else {
                ErrorHelper.logArg(output, "unset", getErrOneArg(args));
            }
            return false;
        }
        private object isnull(StringWriter output, Engine engine, params object[] args) {
            if (args.Length > 0) {
                return args[0] == null;
            } else {
                ErrorHelper.logArg(output, "isnull", ErrorHelper.ARGUMENT_FEW);
                return true;
            }
        }
        private object file_exists(StringWriter output, Engine engine, params object[] args) {
            if (args.Length > 0 && args[0] is string) {
                return File.Exists((string) args[0]);
            } else {
                ErrorHelper.logArg(output, "file_exists", getErrOneArgString(args));
                return false;
            }
        }
        private object files(StringWriter output, Engine engine, params object[] args) {
            if (args.Length > 0 && args[0] is string) {
                List<object> result = new List<object>();
                string[] arr;
                if (args.Length >= 2 && args[1] is string) {
                    arr = Directory.GetFiles((string) args[0], (string) args[1]);
                } else {
                    arr = Directory.GetFiles((string) args[0]);
                }
                int baseLen = ((string) args[0]).Length;
                foreach (string dir in arr)
                    result.Add(dir.Substring(baseLen));
                return result;
            } else {
                ErrorHelper.logArg(output, "files", getErrOneArgString(args));
                return null;
            }
        }
        private object dirs(StringWriter output, Engine engine, params object[] args) {
            if (args.Length > 0 && args[0] is string) {
                List<object> result = new List<object>();
                string[] arr;
                if (args.Length >= 2 && args[1] is string) {
                    arr = Directory.GetDirectories((string) args[0], (string) args[1]);
                } else {
                    arr = Directory.GetDirectories((string) args[0]);
                }
                int baseLen = ((string) args[0]).Length;
                foreach (string dir in arr)
                    result.Add(dir.Substring(baseLen));
                return result;
            } else {
                ErrorHelper.logArg(output, "dirs", getErrOneArgString(args));
                return null;
            }
        }
        private object file_get_contents(StringWriter output, Engine engine, params object[] args) {
            if (args.Length > 0 && args[0] is string) {
                return File.ReadAllText((string) args[0]);
            } else {
                ErrorHelper.logArg(output, "file_get_contents", getErrOneArgString(args));
                return null;
            }
        }
        private object file_get_lines(StringWriter output, Engine engine, params object[] args) {
            if (args.Length > 0 && args[0] is string) {
                string[] arr = File.ReadAllLines((string) args[0]);
                List<object> result = new List<object>();
                foreach (string line in arr)
                    result.Add(line);
                return result;
            } else {
                ErrorHelper.logArg(output, "file_get_lines", getErrOneArgString(args));
                return null;
            }
        }
        public object float_val(StringWriter output, Engine engine, params object[] args) {
            if (args.Length == 0) {
                ErrorHelper.logArg(output, "float", ErrorHelper.ARGUMENT_FEW);
                return 0.0f;
            }
            if (args[0] != null) {
                if (args[0] is float) {
                    return args[0];
                } else if (args[0] is string) {
                    try {
                        return float.Parse(args[0].ToString(), CultureInfo.InvariantCulture);
                    } catch (Exception) {
                        if (args.Length >= 2 && args[1] != null) {
                            return (float) args[1];
                        } else {
                            return 0.0f;
                        }
                    }
                } else if (args[0] is bool) {
                    return ((bool) args[0]) ? 1 : 0;
                } else if (args[0] is CustomVariable) {
                    return ((CustomVariable) args[0]).floatVal();
                }
            }
            if (args.Length >= 2 && args[1] != null) {
                return (float) args[1];
            } else {
                return 0.0f;
            }
        }
        public object string_val(StringWriter output, Engine engine, params object[] args) {
            if (args.Length == 0) {
                ErrorHelper.logArg(output, "string", ErrorHelper.ARGUMENT_FEW);
                return null;
            }
            if (args[0] != null) {
                if (args[0] is float) {
                    return ((float) args[0]).ToString();
                } else if (args[0] is string) {
                    return args[0];
                } else if (args[0] is bool) {
                    return ((bool) args[0]) ? "True" : "False";
                } else if (args[0] is CustomVariable) {
                    return ((CustomVariable) args[0]).stringVal();
                }
            }
            if (args.Length >= 2 && args[1] != null) {
                return (string) args[1];
            } else {
                return null;
            }
        }
        public static string objectInfo(object obj, int tabLevel) {
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < tabLevel; i++)
                stringBuilder.Append("&#8194;&#8194;");
            string tabs = stringBuilder.ToString();

            if (obj is float) {
                return stringBuilder.Append("float(").Append((float) obj).Append(")").ToString();
            } else if (obj is string) {
                string str = (string) obj;
                return stringBuilder.Append("string(").Append(str.Length).Append(") \"").Append(str).Append("\"").ToString();
            } else if (obj is bool) {
                return stringBuilder.Append("bool(").Append((bool) obj).Append(")").ToString();
            } else if (obj is List<object>) {
                List<object> list = (List<object>) obj;
                stringBuilder.Append("array(").Append(list.Count).Append(") {<br>");
                for (int i = 0; i < list.Count; i++) {
                    stringBuilder.Append(tabs).Append("&#8194;&#8194;[").Append(i).Append("]=><br>");
                    stringBuilder.Append(objectInfo(list[i], tabLevel + 1)).Append("<br>");
                }
                stringBuilder.Append(tabs).Append("}");
                return stringBuilder.ToString();
            } else if (obj is Dictionary<string, object>) {
                Dictionary<string, object> dict = (Dictionary<string, object>) obj;
                stringBuilder.Append("dict(").Append(dict.Count).Append(") {<br>");
                foreach (string key in dict.Keys) {
                    stringBuilder.Append(tabs).Append("&#8194;&#8194;[").Append(key).Append("]=><br>");
                    stringBuilder.Append(objectInfo(dict[key], tabLevel + 1)).Append("<br>");
                }
                stringBuilder.Append(tabs).Append("}");
                return stringBuilder.ToString();
            } else if (obj is CustomVariable) {
                return ((CustomVariable) obj).dump(stringBuilder, tabLevel, tabs, "&#8194;&#8194;");
            } else if (obj == null) {
                return stringBuilder.Append("null").ToString();
            } else {
                throw new ScriptException(ScriptException.UNKNOWN_VARIABLE_TYPE, "Неизвестный тип переменной: \"" + obj.GetType() + "\"!");
            }
        }
        private object var_dump(StringWriter output, Engine engine, params object[] args) {
            if (args.Length > 0) {
                output.Write(objectInfo(args[0], 0));
            } else {
                ErrorHelper.logArg(output, "var_dump", ErrorHelper.ARGUMENT_FEW);
            }
            return null;
        }
        private object limit(StringWriter output, Engine engine, params object[] args) {
            if (args.Length >= 2 && args[0] is string && args[1] is float) {
                string str = (string) args[0];
                int len = (int) ((float) args[1]);
                if (str.Length > len) {
                    return str.Substring(0, len) + "...";
                } else {
                    return str;
                }
            } else {
                ErrorHelper.logArg(output, "limit", "Использование: limit(строка, макс_количество_символов)");
                return null;
            }
        }
        #endregion

        /// <summary>
        /// Пропарсить файл скрипта. Он состоит из последовательности команд и html-вставок
        /// </summary>
        public MHSScript parseScript(string page) {
            return MHSParser.getScriptChunk(page);
        }

        /// <summary>
        /// Запустить чанк скрипта и получить его вывод в StringWriter
        /// </summary>
        private string getChunkOutput(MHSScript chunk) {
            StringBuilder stringBuilder = new StringBuilder();
            StringWriter stringWriter = new StringWriter(stringBuilder);
            chunk.execute(this, stringWriter);
            stringWriter.Close();
            return stringBuilder.ToString();
        }

        #region CALL_STACK_FUNCTIONS
        private void addToCallStack(FunctionType type, Dictionary<string, Variable> args) {
            callStack.Add(type);
            stackPointer++;
            argsStack.Add(args);
            if (type == FunctionType.LOCAL)
                localFunctionsInCallStack++;
        }
        private void removeFromCallStack() {
            if (stackPointer < 0)
                throw new InvalidOperationException("Попытка удаления функции из пустого call stack'а!");
            if (callStack[stackPointer] == FunctionType.LOCAL)
                localFunctionsInCallStack--;
            callStack.RemoveAt(stackPointer);
            argsStack.RemoveAt(stackPointer);
            stackPointer--;
        }
        #endregion

        /// <summary>
        /// Запустить функцию с заданными аргументами [в основном для вызова из скрипта]
        /// </summary>
        public object executeFunction(string functionName, StringWriter output, Engine engine, object[] args) {
            GlobalFunction function = getFunction(functionName);
            if (function != null) {
                addToCallStack(FunctionType.GLOBAL, null);
                object obj = function.function.Invoke(output, engine, args);
                removeFromCallStack();
                return obj;
            } else {
                if (localFunctions.ContainsKey(functionName)) {
                    LocalFunction localFunction = localFunctions[functionName];

                    Dictionary<string, Variable> localArgs = new Dictionary<string,Variable>();
                    for (int i = 0; i < args.Length; i++) {
                        localArgs.Add(localFunction.args[i], new Variable() {
                            value = args[i]
                        });
                    }

                    addToCallStack(FunctionType.LOCAL, localArgs);
                    object obj = localFunctions[functionName].code.execute(this, output);
                    removeFromCallStack();
                    return obj;
                } else {
                    throw new ScriptException(ScriptException.FUNCTION_NOT_FOUND, "Функция \"" + functionName + "\" не найдена!");
                }
            }
        }

        /// <summary>
        /// Вспомогательная функция. Обрабатывает результат MHSCommand для переменной
        /// </summary>
        public object objectValueHelper(object obj) {
            if (obj is Variable) {
                return ((Variable) obj).value;
            } else {
                return obj;
            }
        }

        /// <summary>
        /// Запустить функцию объекта с заданными аргументами [в основном для вызова из скрипта]
        /// </summary>
        public object executeDotFunction(object obj, string functionName, StringWriter output, Engine engine, object[] args) {
            VariableFunction function = null;
            obj = objectValueHelper(obj);

            if (obj == null) {
                throw new ScriptException(ScriptException.NULL_POINTER, "Попытка вызова функции: \"" + functionName + "\" у null объекта!");
            }

            if (obj is string) {
                if (stringFunctions.ContainsKey(functionName))
                    function = stringFunctions[functionName];
            } else if (obj is List<object>) {
                if (listFunctions.ContainsKey(functionName))
                    function = listFunctions[functionName];
            } else if (obj is Dictionary<string, object>) {
                if (dictFunctions.ContainsKey(functionName))
                    function = dictFunctions[functionName];
            } else if (obj is CustomVariable) {
                addToCallStack(FunctionType.GLOBAL, null);
                object functionResult = ((CustomVariable) obj).executeFunction(functionName, this, output, args);
                removeFromCallStack();
                return functionResult;
            }
            if (function != null) {
                addToCallStack(FunctionType.GLOBAL, null);
                object functionResult = function.function.Invoke(obj, output, engine, args);
                removeFromCallStack();
                return functionResult;
            } else {
                throw new ScriptException(ScriptException.FUNCTION_NOT_FOUND, "Функция \"" + functionName + "\" не найдена в типе \"" + obj.GetType() + "\"!");
            }
        }

        /// <summary>
        /// Получить свойство объекта
        /// </summary>
        public object getDotProperty(object obj, string propertyName) {
            throw new NotImplementedException("Параметры объектов ещё не поддерживаются!");
        }
    }

}
