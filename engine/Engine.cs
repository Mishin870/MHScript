using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Globalization;
using Mishin870.MHScript.engine;
using Mishin870.MHScript.engine.objects;
using Mishin870.MHScript.engine.commands;

namespace Mishin870.MHScript.engine {

    public class Engine {
        private static readonly Dictionary<string, VariableFunction> stringFunctions = new Dictionary<string, VariableFunction>();
        private static readonly Dictionary<string, VariableFunction> listFunctions = new Dictionary<string, VariableFunction>();
        private static readonly Dictionary<string, VariableFunction> dictFunctions = new Dictionary<string, VariableFunction>();
        private static Dictionary<string, GlobalFunction> globalFunctions = new Dictionary<string, GlobalFunction>();

        private Dictionary<string, Variable> variables = new Dictionary<string, Variable>();
        private Dictionary<string, LocalFunction> localFunctions = new Dictionary<string, LocalFunction>();

        private List<FunctionType> callStack = new List<FunctionType>();
        private List<Dictionary<string, Variable>> argsStack = new List<Dictionary<string, Variable>>();
        private int stackPointer = -1;
        private int localFunctionsInCallStack = 0;

        public delegate void WarningFunction(string message);
        public WarningFunction warning;

        public Engine(WarningFunction warning) {
            this.warning = warning;
        }

        /// <summary>
        /// Заполнение функций над стандартными типами
        /// </summary>
        static Engine() {
            stringFunctions.Add("sub", new VariableFunction() {
                function = string_sub,
                functionName = "string sub(start, length)",
                description = "Возвращает подстроку из строки"
            });
            stringFunctions.Add("size", new VariableFunction() {
                function = string_size,
                functionName = "int size()",
                description = "Возвращает длину строки"
            });
            stringFunctions.Add("reverse", new VariableFunction() {
                function = string_reverse,
                functionName = "string reverse()",
                description = "Инвертирует строку"
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
                description = "Очищает массив. Возвращает сам массив"
            });
            listFunctions.Add("size", new VariableFunction() {
                function = list_size,
                functionName = "int size()",
                description = "Возвращает размер массива"
            });
            listFunctions.Add("reverse", new VariableFunction() {
                function = list_reverse,
                functionName = "List<object> reverse()",
                description = "Переворачивает массив. Возвращает сам массив"
            });
            listFunctions.Add("have", new VariableFunction() {
                function = list_have,
                functionName = "bool have(obj)",
                description = "Проверяет содержание объекта obj в массиве"
            });

            dictFunctions.Add("add", new VariableFunction() {
                function = dict_add,
                functionName = "Dictionary<string, object> add((string, object)... args)",
                description = "Добавляет ключи и соответствующие им элементы, переисленные по очереди, в словарь. Возвращает сам словарь"
            });
            dictFunctions.Add("remove", new VariableFunction() {
                function = dict_remove,
                functionName = "Dictionary<string, object> remove(key)",
                description = "Удаляет элемент с ключём key. Возвращает сам словарь"
            });
            dictFunctions.Add("clear", new VariableFunction() {
                function = dict_clear,
                functionName = "Dictionary<string, object> clear()",
                description = "Очищает словарь. Возвращает сам словарь"
            });
            dictFunctions.Add("size", new VariableFunction() {
                function = dict_size,
                functionName = "int size()",
                description = "Возвращает размер словаря (количество пар ключ => значение)"
            });
            dictFunctions.Add("keys", new VariableFunction() {
                function = dict_keys,
                functionName = "List<object> keys()",
                description = "Возвращает массив ключей словаря"
            });
            dictFunctions.Add("values", new VariableFunction() {
                function = dict_values,
                functionName = "List<object> values()",
                description = "Возвращает массив значений словаря"
            });
            dictFunctions.Add("have", new VariableFunction() {
                function = dict_have,
                functionName = "bool have(key)",
                description = "Проверяет содержание ключа key в словаре"
            });
            dictFunctions.Add("toUrlArgs", new VariableFunction() {
                function = dict_to_url_args,
                functionName = "string toUrlArgs()",
                description = "Возвращает строку URL-параметров, созданную на основе этого словаря"
            });
        }

        #region DEFAULT_OBJECT_FUNCTIONS
        private static object string_sub(object obj, Engine engine, params object[] args) {
            if (args.Length >= 2 && obj is string && args[0] != null && args[1] != null)
                return ((string) obj).Substring((int) ((float) args[0]), (int) ((float) args[1]));
            return null;
        }
        private static object string_size(object obj, Engine engine, params object[] args) {
            return (float) ((string) obj).Length;
        }
        private static object string_reverse(object obj, Engine engine, params object[] args) {
            char[] arr = ((string) obj).ToCharArray();
            Array.Reverse(arr);
            return new string(arr);
        }

        private static object list_add(object obj, Engine engine, params object[] args) {
            List<object> list = (List<object>) obj;
            foreach (object arg in args)
                list.Add(obj);
            return obj;
        }
        private static object list_remove(object obj, Engine engine, params object[] args) {
            List<object> list = (List<object>) obj;
            if (args.Length == 1 && args[0] != null)
                list.RemoveAt((int) ((float) args[0]));
            return obj;
        }
        private static object list_clear(object obj, Engine engine, params object[] args) {
            List<object> list = (List<object>) obj;
            list.Clear();
            return obj;
        }
        private static object list_size(object obj, Engine engine, params object[] args) {
            return (float) ((List<object>) obj).Count;
        }
        private static object list_reverse(object obj, Engine engine, params object[] args) {
            ((List<object>) obj).Reverse();
            return obj;
        }
        private static object list_have(object obj, Engine engine, params object[] args) {
            if (args.Length > 0 && args[0] != null)
                return ((List<object>) obj).Contains(args[0]);
            return false;
        }

        private static object dict_add(object obj, Engine engine, params object[] args) {
            Dictionary<string, object> dict = (Dictionary<string, object>) obj;
            for (int i = 0; i < args.Length; i += 2) {
                string key = (string) args[i];
                if (key == null || dict.ContainsKey(key))
                    continue;
                dict[key] = args[i + 1];
            }
            return dict;
        }
        private static object dict_remove(object obj, Engine engine, params object[] args) {
            Dictionary<string, object> dict = (Dictionary<string, object>) obj;
            if (args.Length == 1 && args[0] != null)
                dict.Remove((string) args[0]);
            return obj;
        }
        private static object dict_clear(object obj, Engine engine, params object[] args) {
            Dictionary<string, object> dict = (Dictionary<string, object>) obj;
            dict.Clear();
            return obj;
        }
        private static object dict_size(object obj, Engine engine, params object[] args) {
            return (float) ((Dictionary<string, object>) obj).Count;
        }
        private static object dict_keys(object obj, Engine engine, params object[] args) {
            List<object> keys = new List<object>();
            Dictionary<string, object> dict = (Dictionary<string, object>) obj;
            foreach (string key in dict.Keys)
                keys.Add(key);
            return keys;
        }
        private static object dict_values(object obj, Engine engine, params object[] args) {
            List<object> values = new List<object>();
            Dictionary<string, object> dict = (Dictionary<string, object>) obj;
            foreach (string value in dict.Values)
                values.Add(value);
            return values;
        }
        private static object dict_have(object obj, Engine engine, params object[] args) {
            if (args.Length > 0 && args[0] != null)
                return ((Dictionary<string, object>) obj).ContainsKey((string) args[0]);
            return false;
        }
        private static object dict_to_url_args(object obj, Engine engine, params object[] args) {
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
        #region VARIABLES
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
        /// Проверка существования переменной в движке
        /// </summary>
        public bool isVariableSet(string variableName) {
            return variables.ContainsKey(variableName);
        }
        #endregion
        #region FUNCTIONS
        /// <summary>
        /// Добавить глобальную функцию в движок
        /// </summary>
        public void addGlobalFunction(string functionName, GlobalFunction function) {
            globalFunctions.Add(functionName, function);
        }
        /// <summary>
        /// Получить глобальную функцию из движка
        /// </summary>
        public GlobalFunction getGlobalFunction(string functionName) {
            if (globalFunctions.ContainsKey(functionName)) {
                return globalFunctions[functionName];
            } else {
                return null;
            }
        }
        /// <summary>
        /// Добавить набор локальных функций (которые загружаются из скрипта)
        /// </summary>
        public void addLocalFunctions(List<LocalFunction> localFunctions) {
            foreach (LocalFunction localFunction in localFunctions)
                this.localFunctions[localFunction.name] = localFunction;
        }
        #endregion
        #region DEFAULT_GLOBAL_FUNCTIONS
        /// <summary>
        /// Добавляет в движок все необходимые стандартные функции
        /// </summary>
        public void addDefaultGlobalFunctions() {
            addGlobalFunction("array", new GlobalFunction() {
                function = new GlobalFunction.UniversalFunction(array),
                functionDocsName = "List<object> array(object... args)",
                functionDocsDescription = "Создаёт массив из объектов args"
            });
            addGlobalFunction("dict", new GlobalFunction() {
                function = new GlobalFunction.UniversalFunction(dict),
                functionDocsName = "Dictionary<string, object> dict((string, object)... args)",
                functionDocsDescription = "Создаёт словарь из ключей и объектов, перечисленных по очереди в args"
            });
            addGlobalFunction("isnull", new GlobalFunction() {
                function = new GlobalFunction.UniversalFunction(isnull),
                functionDocsName = "bool isnull(object)",
                functionDocsDescription = "Возвращает true, если object равен null. Иначе false"
            });
            addGlobalFunction("isset", new GlobalFunction() {
                function = new GlobalFunction.UniversalFunction(isset),
                functionDocsName = "bool isset(variableName)",
                functionDocsDescription = "Возвращает true, если переменная с именем variableName существует"
            });
            addGlobalFunction("unset", new GlobalFunction() {
                function = new GlobalFunction.UniversalFunction(unset),
                functionDocsName = "void unset(variableName)",
                functionDocsDescription = "Удаляет переменную"
            });
            addGlobalFunction("file_exists", new GlobalFunction() {
                function = new GlobalFunction.UniversalFunction(file_exists),
                functionDocsName = "bool file_exists(fileName)",
                functionDocsDescription = "Возвращает true, если файл fileName существует. Иначе false"
            });
            addGlobalFunction("float", new GlobalFunction() {
                function = new GlobalFunction.UniversalFunction(float_val),
                functionDocsName = "float float(value, defaultValue)",
                functionDocsDescription = "Конвертирует любой объект в float"
            });
            addGlobalFunction("string", new GlobalFunction() {
                function = new GlobalFunction.UniversalFunction(string_val),
                functionDocsName = "string string(value, defaultValue)",
                functionDocsDescription = "Конвертирует любой объект в string"
            });
            addGlobalFunction("var_dump", new GlobalFunction() {
                function = new GlobalFunction.UniversalFunction(var_dump),
                functionDocsName = "string var_dump(object)",
                functionDocsDescription = "Возвращает полную информацию о переменной"
            });
            addGlobalFunction("limit", new GlobalFunction() {
                function = new GlobalFunction.UniversalFunction(limit),
                functionDocsName = "string limit(source, count)",
                functionDocsDescription = "Ограничивает строку source в count символов. Плюс троеточие, если сокращена"
            });
            addGlobalFunction("files", new GlobalFunction() {
                function = new GlobalFunction.UniversalFunction(files),
                functionDocsName = "List<object> files(path, [pattern])",
                functionDocsDescription = "Получить список файлов"
            });
            addGlobalFunction("dirs", new GlobalFunction() {
                function = new GlobalFunction.UniversalFunction(dirs),
                functionDocsName = "List<object> dirs(path, [pattern])",
                functionDocsDescription = "Получить список папок"
            });
            addGlobalFunction("file_get_contents", new GlobalFunction() {
                function = new GlobalFunction.UniversalFunction(file_get_contents),
                functionDocsName = "string file_get_contents(fileName)",
                functionDocsDescription = "Получить содержимое файла"
            });
            addGlobalFunction("file_get_lines", new GlobalFunction() {
                function = new GlobalFunction.UniversalFunction(file_get_lines),
                functionDocsName = "List<string> file_get_lines(fileName)",
                functionDocsDescription = "Получить содержимое файла по строчкам"
            });
            
        }
        
        private object array(Engine engine, params object[] args) {
            List<object> arr = new List<object>();
            foreach (object obj in args)
                arr.Add(obj);
            return arr;
        }
        private object dict(Engine engine, params object[] args) {
            Dictionary<string, object> arr = new Dictionary<string, object>();
            for (int i = 0; i < args.Length; i += 2) {
                string key = (string) args[i];
                if (key == null || arr.ContainsKey(key))
                    continue;
                arr[key] = args[i + 1];
            }
            return arr;
        }
        private object isset(Engine engine, params object[] args) {
            if (args.Length > 0 && args[0] != null) {
                return isVariableSet(args[0].ToString());
            } else {
                string functionName = "isset";
                if (args.Length == 0) {
                    ExceptionHelper.logArg(functionName, ExceptionHelper.ARGUMENT_FEW);
                } else if (args[0] == null) {
                    ExceptionHelper.logArg(functionName, ExceptionHelper.ARGUMENT_NULL);
                } else {
                    ExceptionHelper.logArg(functionName, "unknown error");
                }
                return false;
            }
        }
        private object unset(Engine engine, params object[] args) {
            if (args.Length > 0 && args[0] != null) {
                removeVariable(args[0].ToString());
            } else {
                string functionName = "unset";
                if (args.Length == 0) {
                    ExceptionHelper.logArg(functionName, ExceptionHelper.ARGUMENT_FEW);
                } else if (args[0] == null) {
                    ExceptionHelper.logArg(functionName, ExceptionHelper.ARGUMENT_NULL);
                } else {
                    ExceptionHelper.logArg(functionName, "unknown error");
                }
            }
            return false;
        }
        private object isnull(Engine engine, params object[] args) {
            if (args.Length > 0) {
                return args[0] == null;
            } else {
                ExceptionHelper.logArg("isnull", ExceptionHelper.ARGUMENT_FEW);
                return true;
            }
        }
        private object file_exists(Engine engine, params object[] args) {
            if (args.Length > 0 && args[0] is string) {
                return File.Exists((string) args[0]);
            } else {
                string functionName = "file_exists";
                if (args.Length == 0) {
                    ExceptionHelper.logArg(functionName, ExceptionHelper.ARGUMENT_FEW);
                } else if (args[0] == null) {
                    ExceptionHelper.logArg(functionName, ExceptionHelper.ARGUMENT_NULL);
                } else if (!(args[0] is string)) {
                    ExceptionHelper.logArg(functionName, ExceptionHelper.ARGUMENT_STRING);
                } else {
                    return "unknown error";
                }
                return false;
            }
        }
        private object files(Engine engine, params object[] args) {
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
                string functionName = "files";
                if (args.Length == 0) {
                    ExceptionHelper.logArg(functionName, ExceptionHelper.ARGUMENT_FEW);
                } else if (args[0] == null) {
                    ExceptionHelper.logArg(functionName, ExceptionHelper.ARGUMENT_NULL);
                } else if (!(args[0] is string)) {
                    ExceptionHelper.logArg(functionName, ExceptionHelper.ARGUMENT_STRING);
                } else {
                    return "unknown error";
                }
                return null;
            }
        }
        private object dirs(Engine engine, params object[] args) {
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
                string functionName = "dirs";
                if (args.Length == 0) {
                    ExceptionHelper.logArg(functionName, ExceptionHelper.ARGUMENT_FEW);
                } else if (args[0] == null) {
                    ExceptionHelper.logArg(functionName, ExceptionHelper.ARGUMENT_NULL);
                } else if (!(args[0] is string)) {
                    ExceptionHelper.logArg(functionName, ExceptionHelper.ARGUMENT_STRING);
                } else {
                    return "unknown error";
                }
                return null;
            }
        }
        private object file_get_contents(Engine engine, params object[] args) {
            if (args.Length > 0 && args[0] is string) {
                return File.ReadAllText((string) args[0]);
            } else {
                string functionName = "file_get_contents";
                if (args.Length == 0) {
                    ExceptionHelper.logArg(functionName, ExceptionHelper.ARGUMENT_FEW);
                } else if (args[0] == null) {
                    ExceptionHelper.logArg(functionName, ExceptionHelper.ARGUMENT_NULL);
                } else if (!(args[0] is string)) {
                    ExceptionHelper.logArg(functionName, ExceptionHelper.ARGUMENT_STRING);
                } else {
                    return "unknown error";
                }
                return null;
            }
        }
        private object file_get_lines(Engine engine, params object[] args) {
            if (args.Length > 0 && args[0] is string) {
                string[] arr = File.ReadAllLines((string) args[0]);
                List<object> result = new List<object>();
                foreach (string line in arr)
                    result.Add(line);
                return result;
            } else {
                string functionName = "file_get_lines";
                if (args.Length == 0) {
                    ExceptionHelper.logArg(functionName, ExceptionHelper.ARGUMENT_FEW);
                } else if (args[0] == null) {
                    ExceptionHelper.logArg(functionName, ExceptionHelper.ARGUMENT_NULL);
                } else if (!(args[0] is string)) {
                    ExceptionHelper.logArg(functionName, ExceptionHelper.ARGUMENT_STRING);
                } else {
                    return "unknown error";
                }
                return null;
            }
        }
        public object float_val(Engine engine, params object[] args) {
            if (args.Length == 0) {
                ExceptionHelper.logArg("float", ExceptionHelper.ARGUMENT_FEW);
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
        public object string_val(Engine engine, params object[] args) {
            if (args.Length == 0) {
                ExceptionHelper.logArg("string", ExceptionHelper.ARGUMENT_FEW);
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
        private object var_dump(Engine engine, params object[] args) {
            if (args.Length > 0) {
                return objectInfo(args[0], 0);
            } else {
                ExceptionHelper.logArg("var_dump", ExceptionHelper.ARGUMENT_FEW);
            }
            return null;
        }
        private object limit(Engine engine, params object[] args) {
            if (args.Length >= 2 && args[0] is string && args[1] is float) {
                string str = (string) args[0];
                int len = (int) ((float) args[1]);
                if (str.Length > len) {
                    return str.Substring(0, len) + "...";
                } else {
                    return str;
                }
            } else {
                ExceptionHelper.logArg("limit", "Использование: limit(строка, макс_количество_символов)");
                return null;
            }
        }
        #endregion

        /// <summary>
        /// Пропарсить текст скрипта.
        /// </summary>
        public Script parseScript(string page) {
            return CommandsParser.parseScript(page);
        }

        /// <summary>
        /// Загрузить сериализованный скрипт из потока.
        /// </summary>
        public Script loadScript(Stream stream) {
            return (Script) SerializationHelper.deSerialize(stream);
        }

        /// <summary>
        /// Запустить чанк скрипта и получить его вывод в StringWriter
        /// </summary>
        private string getChunkOutput(Script chunk) {
            StringBuilder stringBuilder = new StringBuilder();
            StringWriter stringWriter = new StringWriter(stringBuilder);
            chunk.execute(this);
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
        public object executeFunction(string functionName, params object[] args) {
            GlobalFunction function = getGlobalFunction(functionName);
            if (function != null) {
                addToCallStack(FunctionType.GLOBAL, null);
                object obj = function.function.Invoke(this, args);
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
                    object obj = localFunctions[functionName].code.execute(this);
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
        public object getRealValue(object obj) {
            if (obj is Variable) {
                return ((Variable) obj).value;
            } else {
                return obj;
            }
        }

        /// <summary>
        /// Запустить функцию объекта с заданными аргументами [в основном для вызова из скрипта]
        /// </summary>
        public object executeDotFunction(object obj, string functionName, Engine engine, object[] args) {
            VariableFunction function = null;
            obj = getRealValue(obj);

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
                object functionResult = ((CustomVariable) obj).executeFunction(functionName, this, args);
                removeFromCallStack();
                return functionResult;
            }
            if (function != null) {
                addToCallStack(FunctionType.GLOBAL, null);
                object functionResult = function.function.Invoke(obj, engine, args);
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
