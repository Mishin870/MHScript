using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;
using System.Text.RegularExpressions;
using Mishin870.MHScript.engine;
using Mishin870.MHScript.engine.objects;
using Mishin870.MHScript.engine.lexems;

namespace Mishin870.MHScript.engine.commands {

    /// <summary>
    /// Любая команда скрипта. Арифметическая операция, условие, вызов функции и т.д.
    /// Некоторые команды имеют в себе блок для других команд.
    /// </summary>
    public abstract class ICommand {
        public abstract object execute(Engine engine);
        internal virtual void serialize(Stream stream, SerializationInfo info) {
            stream.WriteByte(SerializationHelper.ids[GetType()]);
        }
    }

    #region STATEMENTS
    public class CommandLogicCompound : ICommand {
        private List<ICommand> blocks = new List<ICommand>();
        private LexemKind operation;

        public CommandLogicCompound(Stream stream) {
            operation = (LexemKind) stream.ReadByte();
            SerializationHelper.deserializeBlock(stream, blocks);
        }

        internal override void serialize(Stream stream, SerializationInfo info) {
            base.serialize(stream, info);
            stream.WriteByte((byte) operation);
            SerializationHelper.serializeBlock(stream, info, blocks);
        }

        public CommandLogicCompound(LexemKind operation) {
            this.operation = operation;
        }

        public void addBlock(ICommand block) {
            this.blocks.Add(block);
        }

        public override object execute(Engine engine) {
            if (operation == LexemKind.AND) {
                foreach (ICommand block in blocks)
                    if (!((bool) engine.getRealValue(block.execute(engine))))
                        return false;
                return true;
            } else if (operation == LexemKind.OR) {
                foreach (ICommand block in blocks)
                    if ((bool) engine.getRealValue(block.execute(engine)))
                        return true;
                return false;
            } else {
                return false;
            }
        }

    }
    public class CommandLogic : ICommand {
        private ICommand left, right;
        private LexemKind operation;

        public CommandLogic(Stream stream) {
            operation = (LexemKind) stream.ReadByte();
            left = SerializationHelper.deSerialize(stream);
            right = SerializationHelper.deSerialize(stream);
        }

        internal override void serialize(Stream stream, SerializationInfo info) {
            base.serialize(stream, info);
            stream.WriteByte((byte) operation);
            left.serialize(stream, info);
            right.serialize(stream, info);
        }

        public CommandLogic(LexemKind operation, ICommand left, ICommand right) {
            this.operation = operation;
            this.left = left;
            this.right = right;
        }

        private object equals(object obj1, object obj2) {
            if (obj1 is float && obj2 is float) {
                return ((float) obj1) == ((float) obj2);
            } else if (obj1 is string && obj2 is string) {
                return ((string) obj1).Equals(((string) obj2));
            } else {
                return obj1.Equals(obj2);
            }
        }
        private object nequals(object obj1, object obj2) {
            if (obj1 is float && obj2 is float) {
                return ((float) obj1) != ((float) obj2);
            } else if (obj1 is string && obj2 is string) {
                return !((string) obj1).Equals(((string) obj2));
            } else {
                return !obj1.Equals(obj2);
            }
        }

        public override object execute(Engine engine) {
            object op1 = engine.getRealValue(left.execute(engine));
            object op2 = engine.getRealValue(right.execute(engine));
            if (op1 == null || op2 == null) {
                switch (operation) {
                    case LexemKind.EQUALS:
                        return op1 == null && op2 == null;
                    case LexemKind.NOTEQUALS:
                        return (op1 == null) ? (op2 != null) : (op1 != null);
                    default:
                        return false;
                }
            }

            if (op1 is CustomVariable || op2 is CustomVariable) {
                CustomVariable value = (op1 is CustomVariable) ? ((CustomVariable) op1) : ((CustomVariable) op2);
                switch (operation) {
                    case LexemKind.EQUALS:
                        return value.compare(op1, LexemKind.EQUALS, op2);
                    case LexemKind.NOTEQUALS:
                        return value.compare(op1, LexemKind.NOTEQUALS, op2);
                    case LexemKind.LESSER:
                        return value.compare(op1, LexemKind.LESSER, op2);
                    case LexemKind.GREATER:
                        return value.compare(op1, LexemKind.GREATER, op2);
                    case LexemKind.LESSER_EQUALS:
                        return value.compare(op1, LexemKind.LESSER_EQUALS, op2);
                    case LexemKind.GREATER_EQUALS:
                        return value.compare(op1, LexemKind.GREATER_EQUALS, op2);
                    default:
                        return false;
                }
            } else {
                switch (operation) {
                    case LexemKind.EQUALS:
                        return equals(op1, op2);
                    case LexemKind.NOTEQUALS:
                        return nequals(op1, op2);
                    case LexemKind.LESSER:
                        return ((float) op1) < ((float) op2);
                    case LexemKind.GREATER:
                        return ((float) op1) > ((float) op2);
                    case LexemKind.LESSER_EQUALS:
                        return ((float) op1) <= ((float) op2);
                    case LexemKind.GREATER_EQUALS:
                        return ((float) op1) >= ((float) op2);
                    default:
                        return false;
                }
            }
        }

    }
    public class CommandReturn : ICommand {
        private ICommand command;

        public CommandReturn(Stream stream) {
            command = SerializationHelper.deSerialize(stream);
        }

        internal override void serialize(Stream stream, SerializationInfo info) {
            base.serialize(stream, info);
            command.serialize(stream, info);
        }

        public CommandReturn(ICommand command) {
            this.command = command;
        }

        public override object execute(Engine engine) {
            throw new ScriptInterruptException(
                ScriptInterruptException.CODE_RETURN,
                command.execute(engine)
            );
        }

    }
    public class CommandElse : ICommand {
        private ICommand command;

        public CommandElse(Stream stream) {
            command = SerializationHelper.deSerialize(stream);
        }

        internal override void serialize(Stream stream, SerializationInfo info) {
            base.serialize(stream, info);
            command.serialize(stream, info);
        }

        public CommandElse(ICommand command) {
            this.command = command;
        }

        public override object execute(Engine engine) {
            command.execute(engine);
            return null;
        }

    }
    public class CommandElseIf : ICommand {
        private ICommand condition;
        private ICommand command;

        public CommandElseIf(Stream stream) {
            condition = SerializationHelper.deSerialize(stream);
            command = SerializationHelper.deSerialize(stream);
        }

        internal override void serialize(Stream stream, SerializationInfo info) {
            base.serialize(stream, info);
            condition.serialize(stream, info);
            command.serialize(stream, info);
        }

        public CommandElseIf(ICommand condition, ICommand command) {
            this.condition = condition;
            this.command = command;
        }

        public override object execute(Engine engine) {
            if ((bool) engine.getRealValue(condition.execute(engine)) == true) {
                command.execute(engine);
                return true;
            } else {
                return false;
            }
        }

    }
    public class CommandIf : ICommand {
        private ICommand condition;
        private ICommand command;
        private List<ICommand> elseStatements;

        public CommandIf(Stream stream) {
            condition = SerializationHelper.deSerialize(stream);
            command = SerializationHelper.deSerialize(stream);
            elseStatements = new List<ICommand>();
            SerializationHelper.deserializeBlock(stream, elseStatements);
        }

        internal override void serialize(Stream stream, SerializationInfo info) {
            base.serialize(stream, info);
            condition.serialize(stream, info);
            command.serialize(stream, info);
            SerializationHelper.serializeBlock(stream, info, elseStatements);
        }

        public CommandIf(ICommand condition, ICommand command, List<ICommand> elseStatements) {
            this.condition = condition;
            this.command = command;
            this.elseStatements = elseStatements;
        }

        public override object execute(Engine engine) {
            if ((bool) engine.getRealValue(condition.execute(engine)) == true) {
                command.execute(engine);
            } else {
                foreach (ICommand statement in elseStatements) {
                    if (statement is CommandElse) {
                        ((CommandElse) statement).execute(engine);
                        return null;
                    } else if (statement is CommandElseIf) {
                        if ((bool) ((CommandElseIf) statement).execute(engine) == true)
                            return null;
                    }
                }
            }
            return null;
        }

    }
    public class CommandFor : ICommand {
        private ICommand condition;
        private ICommand pre, iter;
        private ICommand command;

        public CommandFor(Stream stream) {
            condition = SerializationHelper.deSerialize(stream);
            pre = SerializationHelper.deSerialize(stream);
            iter = SerializationHelper.deSerialize(stream);
            command = SerializationHelper.deSerialize(stream);
        }

        internal override void serialize(Stream stream, SerializationInfo info) {
            base.serialize(stream, info);
            condition.serialize(stream, info);
            pre.serialize(stream, info);
            iter.serialize(stream, info);
            command.serialize(stream, info);
        }

        public CommandFor(ICommand pre, ICommand condition, ICommand iter, ICommand command) {
            this.pre = pre;
            this.condition = condition;
            this.iter = iter;
            this.command = command;
        }

        public override object execute(Engine engine) {
            for (pre.execute(engine); (bool) engine.getRealValue(condition.execute(engine)); iter.execute(engine)) {
                command.execute(engine);
            }
            return null;
        }

    }
    public class CommandWhile : ICommand {
        private ICommand condition;
        private ICommand command;

        public CommandWhile(Stream stream) {
            condition = SerializationHelper.deSerialize(stream);
            command = SerializationHelper.deSerialize(stream);
        }

        internal override void serialize(Stream stream, SerializationInfo info) {
            base.serialize(stream, info);
            condition.serialize(stream, info);
            command.serialize(stream, info);
        }

        public CommandWhile(ICommand condition, ICommand command) {
            this.condition = condition;
            this.command = command;
        }

        public override object execute(Engine engine) {
            while ((bool) engine.getRealValue(condition.execute(engine))) {
                command.execute(engine);
            }
            return null;
        }

    }
    #endregion

    #region STRUCTURES
    public class Script : ICommand {
        private List<ICommand> commands = new List<ICommand>();
        private List<LocalFunction> localFunctions = new List<LocalFunction>();
        private bool isRoot = true;

        public Script(Stream stream) {
            SerializationHelper.deserializeBlock(stream, commands);
            isRoot = stream.ReadByte() == 1;

            if (isRoot) {
                int count = SerializationHelper.readInt(stream);
                for (int i = 0; i < count; i++) {
                    localFunctions.Add(new LocalFunction(stream));
                }
            }
        }

        internal override void serialize(Stream stream, SerializationInfo info) {
            if (isRoot && info.optimizeForClient) {
                info.writeIntro(stream);
            }

            base.serialize(stream, info);
            SerializationHelper.serializeBlock(stream, info, commands);
            stream.WriteByte((byte) (isRoot ? 1 : 0));

            if (isRoot) {
                SerializationHelper.writeInt(stream, localFunctions.Count);
                foreach (LocalFunction function in localFunctions) {
                    function.serialize(stream, info);
                }
            }
        }

        public Script() {
        }

        /// <summary>
        /// Сделать этот блок скрипта корневым или не корневым
        /// (например, блок не корневого скрипта внутри функции внутри корневого скрипта)
        /// </summary>
        public Script setRoot(bool isRoot) {
            this.isRoot = isRoot;
            return this;
        }

        /// <summary>
        /// Добавить функцию, определённую в этом блоке скрипта
        /// </summary>
        public void addLocalFunction(LocalFunction localFunction) {
            if (!isRoot) {
                throw new InvalidOperationException("Local functions can only be added to root script blocks!");
            }
            this.localFunctions.Add(localFunction);
        }

        /// <summary>
        /// Добавить команду в цепочку
        /// </summary>
        public void addCommand(ICommand command) {
            this.commands.Add(command);
        }

        /// <summary>
        /// Получить команды блока
        /// </summary>
        public List<ICommand> getCommands() {
            return this.commands;
        }

        /// <summary>
        /// Запустить скрипт
        /// </summary>
        public override object execute(Engine engine) {
            if (isRoot) {
                engine.addLocalFunctions(localFunctions);
                foreach (ICommand command in commands)
                    command.execute(engine);
            } else {
                try {
                    foreach (ICommand command in commands)
                        command.execute(engine);
                } catch (ScriptInterruptException sie) {
                    if (sie.code == ScriptInterruptException.CODE_RETURN) {
                        return sie.data;
                    } else if (sie.code == ScriptInterruptException.CODE_REDIRECT) {
                        throw sie;
                    }
                }
            }
            return null;
        }

        public void serialize(Engine engine, Stream stream, bool optimizeForClient) {
            SerializationInfo info = engine.getSerializationInfo();
            info.optimizeForClient = optimizeForClient;
            foreach (LocalFunction local in localFunctions) {
                info.localFunctions.Add(local.name);
            }
            this.serialize(stream, info);
        }
    }
    #endregion

    #region COMMON
    public class CommandIndex : ICommand {
        private ICommand command;
        private List<ICommand> index;

        public CommandIndex(Stream stream) {
            command = SerializationHelper.deSerialize(stream);
            index = new List<ICommand>();
            SerializationHelper.deserializeBlock(stream, index);
        }

        internal override void serialize(Stream stream, SerializationInfo info) {
            base.serialize(stream, info);
            command.serialize(stream, info);
            SerializationHelper.serializeBlock(stream, info, index);
        }

        public CommandIndex(ICommand command, List<ICommand> index) {
            this.command = command;
            this.index = index;
        }

        public override object execute(Engine engine) {
            object obj = engine.getRealValue(command.execute(engine));
            bool safe = false;
            object defaultValue = null;
            if (index.Count >= 3) {
                safe = (bool) engine.getRealValue(index[1].execute(engine));
                defaultValue = engine.getRealValue(index[2].execute(engine));
            }
            if (obj != null) {
                if (obj is List<object>) {
                    int x = (int) ((float) engine.getRealValue(index[0].execute(engine)));
                    int count = ((List<object>) obj).Count;
                    if (x >= 0 && x < count) {
                        return ((List<object>) obj)[x];
                    } else {
                        if (safe) {
                            return defaultValue;
                        } else {
                            ExceptionHelper.logCommon("Ошибка ндексации: выход за границы списка: " + x + " за пределами [0, " + (count - 1) + "]!");
                            return null;
                        }
                    }
                } else if (obj is string) {
                    int x = (int) ((float) engine.getRealValue(index[0].execute(engine)));
                    int count = ((string) obj).Length;
                    if (x >= 0 && x < count) {
                        return ((string) obj).Substring(x, 1);
                    } else {
                        if (safe) {
                            return defaultValue;
                        } else {
                            ExceptionHelper.logCommon("Ошибка ндексации: выход за границы строки: " + x + " за пределами [0, " + (count - 1) + "]!");
                            return null;
                        }
                    }
                } else if (obj is Dictionary<string, object>) {
                    string x = (string) engine.getRealValue(index[0].execute(engine));
                    if (((Dictionary<string, object>) obj).ContainsKey(x)) {
                        return ((Dictionary<string, object>) obj)[x];
                    } else {
                        if (safe) {
                            return defaultValue;
                        } else {
                            ExceptionHelper.logCommon("Ошибка при индексации: ключ \"" + x + "\" не найден в словаре!");
                            return null;
                        }
                    }
                } else if (obj is CustomVariable) {
                    return ((CustomVariable) obj).indexGet(engine.getRealValue(index[0].execute(engine)), safe, defaultValue);
                } else {
                    if (safe) {
                        return defaultValue;
                    } else {
                        throw new ScriptException(ScriptException.UNKNOWN_VARIABLE_TYPE, "Неизвестный тип индексируемого объекта: " + obj.GetType().ToString() + "!");
                    }
                }
            } else {
                ExceptionHelper.logCommon("Ошибка при безопасной адресации: объект равен null!");
                return "error";
            }
        }

    }
    public class CommandEmpty : ICommand {

        public CommandEmpty() {
        }

        public CommandEmpty(Stream stream) {
        }

        internal override void serialize(Stream stream, SerializationInfo info) {
            base.serialize(stream, info);
        }

        public override object execute(Engine engine) {
            return null;
        }

    }
    public class CommandAssign : ICommand {
        private ICommand left, right;
        
        public CommandAssign(Stream stream) {
            left = SerializationHelper.deSerialize(stream);
            right = SerializationHelper.deSerialize(stream);
        }

        internal override void serialize(Stream stream, SerializationInfo info) {
            base.serialize(stream, info);
            left.serialize(stream, info);
            right.serialize(stream, info);
        }

        public CommandAssign(ICommand left, ICommand right) {
            this.left = left;
            this.right = right;
        }

        public override object execute(Engine engine) {
            if (left is CommandVariable) {
                Variable variable = (Variable) left.execute(engine);
                variable.value = engine.getRealValue(right.execute(engine));
            }
            return null;
        }

    }
    public class CommandAssignIndex : ICommand {
        private ICommand left, right;
        private ICommand index;

        public CommandAssignIndex(Stream stream) {
            left = SerializationHelper.deSerialize(stream);
            right = SerializationHelper.deSerialize(stream);
            index = SerializationHelper.deSerialize(stream);
        }

        internal override void serialize(Stream stream, SerializationInfo info) {
            base.serialize(stream, info);
            left.serialize(stream, info);
            right.serialize(stream, info);
            index.serialize(stream, info);
        }

        public CommandAssignIndex(ICommand left, ICommand index, ICommand right) {
            this.left = left;
            this.index = index;
            this.right = right;
        }

        public override object execute(Engine engine) {
            if (left is CommandVariable) {
                Variable variable = (Variable) left.execute(engine);
                object obj = engine.getRealValue(variable.value);
                if (obj is string) {
                    StringBuilder stringBuilder = new StringBuilder((string) obj);
                    int x = (int) ((float) engine.getRealValue(index.execute(engine)));
                    stringBuilder[x] = ((string) engine.getRealValue(right.execute(engine)))[0];
                    variable.value = stringBuilder.ToString();
                } else if (obj is List<object>) {
                    int x = (int) ((float) engine.getRealValue(index.execute(engine)));
                    ((List<object>) obj)[x] = engine.getRealValue(right.execute(engine));
                } else if (obj is Dictionary<string, object>) {
                    string x = (string) engine.getRealValue(index.execute(engine));
                    ((Dictionary<string, object>) obj)[x] = engine.getRealValue(right.execute(engine));
                } else if (obj is CustomVariable) {
                    ((CustomVariable) obj).indexSet(
                        engine.getRealValue(index.execute(engine)),
                        engine.getRealValue(right.execute(engine))
                    );
                }
            }
            return null;
        }

    }
    public class CommandUnary : ICommand {
        private ICommand command;
        private LexemKind operation;

        public CommandUnary(Stream stream) {
            command = SerializationHelper.deSerialize(stream);
            operation = (LexemKind) stream.ReadByte();
        }

        internal override void serialize(Stream stream, SerializationInfo info) {
            base.serialize(stream, info);
            command.serialize(stream, info);
            stream.WriteByte((byte) operation);
        }

        public CommandUnary(ICommand command, LexemKind operation) {
            this.command = command;
            this.operation = operation;
        }

        public override object execute(Engine engine) {
            if (command is CommandVariable) {
                Variable variable = (Variable) command.execute(engine);
                object value = engine.getRealValue(variable.value);
                if (value is float) {
                    float x = (float) value;
                    switch (operation) {
                        case LexemKind.INCREMENT:
                            variable.value = x + 1;
                            return x;
                        case LexemKind.DECREMENT:
                            variable.value = x - 1;
                            return x;
                        case LexemKind.PREINCREMENT:
                            variable.value = x + 1;
                            return variable.value;
                        case LexemKind.PREDECREMENT:
                            variable.value = x - 1;
                            return variable.value;
                        default:
                            return x;
                    }
                } else if (value is bool) {
                    bool x = (bool) value;
                    switch (operation) {
                        case LexemKind.NOT:
                            return !x;
                        default:
                            return x;
                    }
                } else if (value is string) {
                    string x = (string) value;
                    switch (operation) {
                        case LexemKind.NOT: {
                            char[] arr = x.ToCharArray();
                            Array.Reverse(arr);
                            return new string(arr);
                        }
                    }
                } else if (value is CustomVariable) {
                    return ((CustomVariable) value).unary(operation);
                }
            } else {
                object value = engine.getRealValue(command.execute(engine));
                if (value is bool) {
                    bool x = (bool) value;
                    switch (operation) {
                        case LexemKind.NOT:
                            return !x;
                        default:
                            return x;
                    }
                } else if (value is CustomVariable) {
                    return ((CustomVariable) value).unary(operation);
                }
            }
            return null;
        }

    }
    public class CommandMath : ICommand {
        private LexemKind operation;
        private List<ICommand> blocks = new List<ICommand>();

        public CommandMath(Stream stream) {
            operation = (LexemKind) stream.ReadByte();
            SerializationHelper.deserializeBlock(stream, blocks);
        }

        internal override void serialize(Stream stream, SerializationInfo info) {
            base.serialize(stream, info);
            stream.WriteByte((byte) operation);
            SerializationHelper.serializeBlock(stream, info, blocks);
        }

        public CommandMath(LexemKind operation) {
            this.operation = operation;
        }

        public void addBlock(ICommand block) {
            this.blocks.Add(block);
        }

        private object plus(object[] objects) {
            bool isStr = false;
            bool isFloat = false;
            foreach (object obj in objects) {
                if (!isStr && obj is string) {
                    isStr = true;
                } else if (!isFloat && obj is float) {
                    isFloat = true;
                }
            }
            if (isStr) {
                StringBuilder stringBuilder = new StringBuilder();
                foreach (object obj in objects) {
                    if (obj is CustomVariable) {
                        stringBuilder.Append(((CustomVariable) obj).stringVal());
                    } else {
                        stringBuilder.Append(obj);
                    }
                }
                return stringBuilder.ToString();
            } else if (isFloat) {
                float result = 0;
                foreach (object obj in objects) {
                    if (obj is CustomVariable) {
                        result += ((CustomVariable) obj).floatVal();
                    } else {
                        result += (float) obj;
                    }
                }
                return result;
            } else {
                object result = objects[0];
                foreach (object obj in objects) {
                    if (obj is CustomVariable) {
                        result = ((CustomVariable) obj).math(result, LexemKind.PLUS);
                    } else if (obj is string) {
                        result = result.ToString() + (string) obj;
                    } else if (obj is float) {
                        result = ((float) result) + (float) obj;
                    }
                }
                return result;
            }
        }
        private object minus(object[] objects) {
            float result = (float) objects[0];
            foreach (object obj in objects) {
                if (obj is CustomVariable) {
                    result -= ((CustomVariable) obj).floatVal();
                } else {
                    result -= (float) obj;
                }
            }
            return result;
        }
        private object multiply(object[] objects) {
            float result = (float) objects[0];
            foreach (object obj in objects) {
                if (obj is CustomVariable) {
                    result *= ((CustomVariable) obj).floatVal();
                } else {
                    result *= (float) obj;
                }
            }
            return result;
        }
        private object divide(object[] objects) {
            float result = (float) objects[0];
            foreach (object obj in objects) {
                if (obj is CustomVariable) {
                    result /= ((CustomVariable) obj).floatVal();
                } else {
                    result /= (float) obj;
                }
            }
            return result;
        }

        public override object execute(Engine engine) {
            if (blocks.Count == 0)
                return null;

            object[] objects = new object[blocks.Count];
            for (int i = 0; i < blocks.Count; i++) {
                objects[i] = engine.getRealValue(blocks[i].execute(engine));
                if (objects[i] == null)
                    return null;
            }

            switch (operation) {
                case LexemKind.PLUS:
                    return plus(objects);
                case LexemKind.MINUS:
                    return minus(objects);
                case LexemKind.MULTIPLY:
                    return multiply(objects);
                case LexemKind.DIVIDE:
                    return divide(objects);
                default:
                    return null;
            }
        }

    }
    public class CommandNumeric : ICommand {
        private int value;

        public CommandNumeric(Stream stream) {
            value = SerializationHelper.readInt(stream);
        }

        internal override void serialize(Stream stream, SerializationInfo info) {
            base.serialize(stream, info);
            SerializationHelper.writeInt(stream, value);
        }

        public CommandNumeric(int value) {
            this.value = value;
        }

        public override object execute(Engine engine) {
            return value;
        }

    }
    public class CommandString : ICommand {
        private string value;

        public CommandString(Stream stream) {
            value = SerializationHelper.readString(stream);
        }

        internal override void serialize(Stream stream, SerializationInfo info) {
            base.serialize(stream, info);
            SerializationHelper.writeString(stream, value);
        }

        public CommandString(string value) {
            this.value = value.Substring(1, value.Length - 2);
        }

        public CommandString(string value, bool removeQuotes) {
            if (removeQuotes) {
                this.value = value.Substring(1, value.Length - 2);
            } else {
                this.value = value;
            }
        }

        public override object execute(Engine engine) {
            return value;
        }

    }
    public class CommandStringVariabled : ICommand {
        private List<ICommand> commands;

        public CommandStringVariabled(Stream stream) {
            commands = new List<ICommand>();
            SerializationHelper.deserializeBlock(stream, commands);
        }

        internal override void serialize(Stream stream, SerializationInfo info) {
            base.serialize(stream, info);
            SerializationHelper.serializeBlock(stream, info, commands);
        }

        public CommandStringVariabled(string value) {
            string[] arr = Regex.Split(value.Substring(2, value.Length - 3), "(#[a-zA-Z_][a-zA-Z0-9_]*#)");
            commands = new List<ICommand>();
            foreach (string part in arr) {
                if (part.Length > 0) {
                    if (part[0] == '#' && part[part.Length - 1] == '#') {
                        commands.Add(new CommandVariable(part.Substring(1, part.Length - 2)));
                    } else {
                        commands.Add(new CommandString(part, false));
                    }
                }
            }
        }

        public override object execute(Engine engine) {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (ICommand command in commands)
                stringBuilder.Append(engine.getRealValue(command.execute(engine)));
            return stringBuilder.ToString();
        }

    }
    public class CommandBool : ICommand {
        private bool value;

        public CommandBool(Stream stream) {
            value = stream.ReadByte() == 1;
        }

        internal override void serialize(Stream stream, SerializationInfo info) {
            base.serialize(stream, info);
            stream.WriteByte((byte) (value ? 1 : 0));
        }

        public CommandBool(bool value) {
            this.value = value;
        }

        public override object execute(Engine engine) {
            return value;
        }

    }
    public class CommandVariable : ICommand {
        private string variableName;

        public CommandVariable(Stream stream) {
            variableName = SerializationHelper.readString(stream);
        }

        internal override void serialize(Stream stream, SerializationInfo info) {
            base.serialize(stream, info);
            SerializationHelper.writeString(stream, variableName);
        }

        public CommandVariable(string variableName) {
            this.variableName = variableName;
        }

        public override object execute(Engine engine) {
            return engine.getVariable(this.variableName);
        }

    }
    public class CommandDotVariable : ICommand {
        private ICommand obj;
        private string variableName;

        public CommandDotVariable(Stream stream) {
            obj = SerializationHelper.deSerialize(stream);
            variableName = SerializationHelper.readString(stream);
        }

        internal override void serialize(Stream stream, SerializationInfo info) {
            base.serialize(stream, info);
            obj.serialize(stream, info);
            SerializationHelper.writeString(stream, variableName);
        }

        public CommandDotVariable(ICommand obj, string variableName) {
            this.obj = obj;
            this.variableName = variableName;
        }

        public override object execute(Engine engine) {
            return engine.getDotProperty(obj.execute(engine), this.variableName);
        }

    }
    public class CommandGlobalFunction : ICommand {
        private string functionName;
        private List<ICommand> args;

        public CommandGlobalFunction(Stream stream) {
            functionName = SerializationHelper.readString(stream);
            args = new List<ICommand>();
            SerializationHelper.deserializeBlock(stream, args);
        }

        internal override void serialize(Stream stream, SerializationInfo info) {
            base.serialize(stream, info);

            if (info.optimizeForClient) {
                int x = info.localFunctions.IndexOf(functionName);
                if (x == -1) {
                    x = info.globalFunctions.IndexOf(functionName);
                    if (x == -1) {
                        throw new InvalidOperationException("Call to undefined function name. Function must be exist in optimizeForClient mode!");
                    }
                }
                SerializationHelper.writeInt(stream, x);
            } else {
                SerializationHelper.writeString(stream, functionName);
            }
            SerializationHelper.serializeBlock(stream, info, args);
        }

        public CommandGlobalFunction(string functionName, List<ICommand> args) {
            this.functionName = functionName;
            this.args = args;
        }

        public override object execute(Engine engine) {
            object[] resultArgs = new object[args.Count];
            for (int i = 0; i < args.Count; i++)
                resultArgs[i] = args[i] == null ? null : engine.getRealValue(args[i].execute(engine));

            return engine.executeFunction(this.functionName, resultArgs);
        }

    }
    public class CommandDotFunction : ICommand {
        private ICommand obj;
        private string functionName;
        private List<ICommand> args;

        public CommandDotFunction(Stream stream) {
            obj = SerializationHelper.deSerialize(stream);
            functionName = SerializationHelper.readString(stream);
            args = new List<ICommand>();
            SerializationHelper.deserializeBlock(stream, args);
        }

        internal override void serialize(Stream stream, SerializationInfo info) {
            base.serialize(stream, info);
            obj.serialize(stream, info);
            SerializationHelper.writeString(stream, functionName);
            SerializationHelper.serializeBlock(stream, info, args);
        }

        public CommandDotFunction(ICommand obj, string functionName, List<ICommand> args) {
            this.obj = obj;
            this.functionName = functionName;
            this.args = args;
        }

        public override object execute(Engine engine) {
            object[] resultArgs = new object[args.Count];
            for (int i = 0; i < args.Count; i++)
                resultArgs[i] = args[i] == null ? null : engine.getRealValue(args[i].execute(engine));

            return engine.executeDotFunction(obj.execute(engine), this.functionName, engine, resultArgs);
        }

    }
    #endregion

}
