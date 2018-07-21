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

    public class CommandsParser {

        /// <summary>
        /// Парсит аргументы функции. Как отдельные parseCommand, разделённые запятой
        /// </summary>
        private static List<ICommand> parseFunctionArgs(Lexem brace) {
            List<Lexem> lexems = brace.childs;
            List<Lexem> currentLexems = new List<Lexem>();
            List<ICommand> commands = new List<ICommand>();
            while (lexems.Count > 0) {
                if (lexems[0].kind == LexemKind.COMMA) {
                    commands.Add(parseCommand(currentLexems));
                    currentLexems.Clear();
                    lexems.RemoveAt(0);
                } else {
                    currentLexems.Add(lexems[0]);
                    lexems.RemoveAt(0);
                }
            }
            if (currentLexems.Count > 0) {
                commands.Add(parseCommand(currentLexems));
                currentLexems.Clear();
            }
            return commands;
        }

        /// <summary>
        /// Часть примитива (между точками)
        /// </summary>
        private static ICommand parseNamedPrimitivePart(ICommand prevCommand, List<Lexem> lexems) {
            if (lexems.Count < 1 || lexems[0].kind != LexemKind.IDENTIFIER)
                return null;

            string value = lexems[0].value;
            lexems.RemoveAt(0);
            if (lexems.Count == 1) {
                if ((lexems[0].kind == LexemKind.INCREMENT || lexems[0].kind == LexemKind.DECREMENT)) {
                    if (prevCommand == null) {
                        return new CommandUnary(new CommandVariable(value), lexems[0].kind);
                    } else {
                        return new CommandUnary(new CommandDotVariable(prevCommand, value), lexems[0].kind);
                    }
                } else if (lexems[0].kind == LexemKind.BRACE) {
                    if (prevCommand == null) {
                        return new CommandGlobalFunction(value, parseFunctionArgs(lexems[0]));
                    } else {
                        return new CommandDotFunction(prevCommand, value, parseFunctionArgs(lexems[0]));
                    }
                } else if (lexems[0].kind == LexemKind.INDEX) {
                    if (prevCommand == null) {
                        return new CommandIndex(new CommandVariable(value), parseFunctionArgs(lexems[0]));
                    } else {
                        return new CommandIndex(new CommandDotVariable(prevCommand, value), parseFunctionArgs(lexems[0]));
                    }
                }
            } else if (lexems.Count == 2) {
                if (lexems[0].kind == LexemKind.BRACE && lexems[1].kind == LexemKind.INDEX) {
                    if (prevCommand == null) {
                        return new CommandIndex(
                            new CommandGlobalFunction(value, parseFunctionArgs(lexems[0])),
                            parseFunctionArgs(lexems[1])
                        );
                    } else {
                        return new CommandIndex(
                            new CommandDotFunction(prevCommand, value, parseFunctionArgs(lexems[0])),
                            parseFunctionArgs(lexems[1])
                        );
                    }
                }
            } else if (lexems.Count == 0) {
                if (prevCommand == null) {
                    return new CommandVariable(value);
                } else {
                    return new CommandDotVariable(prevCommand, value);
                }
            }

            return null;
        }

        /// <summary>
        /// Примитив, содержащий в себе переменную или функцию
        /// </summary>
        private static ICommand parseNamedPrimitive(List<Lexem> full) {
            List<List<Lexem>> list = new List<List<Lexem>>();
            List<Lexem> currentList = new List<Lexem>();
            foreach (Lexem lexem in full) {
                if (lexem.kind == LexemKind.DOT) {
                    list.Add(currentList);
                    currentList = new List<Lexem>();
                } else {
                    currentList.Add(lexem);
                }
            }
            list.Add(currentList);

            if (list.Count == 1) {
                return parseNamedPrimitivePart(null, currentList);
            } else {
                ICommand prevCommand = null;
                foreach (List<Lexem> lexems in list)
                    prevCommand = parseNamedPrimitivePart(prevCommand, lexems);
                return prevCommand;
            }
            
        }

        private static ICommand parsePrimitive(List<Lexem> lexems) {
            if (lexems.Count == 0)
                return null;

            LexemKind kind = lexems[0].kind;
            if (kind == LexemKind.NOT) {
                lexems.RemoveAt(0);
                return new CommandUnary(parsePrimitive(lexems), LexemKind.NOT);
            } else if (kind == LexemKind.BRACE && lexems.Count == 1) {
                return parseCommand(lexems[0].childs);
            } else if (kind == LexemKind.NUMBER && lexems.Count == 1) {
                return new CommandNumeric(float.Parse(lexems[0].value, CultureInfo.InvariantCulture));
            } else if (kind == LexemKind.STRING && lexems.Count == 1) {
                return new CommandString(lexems[0].value);
            } else if (kind == LexemKind.STRING_VARIABLED && lexems.Count == 1) {
                return new CommandStringVariabled(lexems[0].value);
            } else if (kind == LexemKind.TRUE && lexems.Count == 1) {
                return new CommandBool(true);
            } else if (kind == LexemKind.FALSE && lexems.Count == 1) {
                return new CommandBool(false);
            } else if (kind == LexemKind.IDENTIFIER) {
                return parseNamedPrimitive(lexems);
            } else if (kind == LexemKind.INCREMENT || kind == LexemKind.DECREMENT) {
                lexems.RemoveAt(0);
                if (lexems.Count == 1 && lexems[0].kind == LexemKind.IDENTIFIER) {
                    return new CommandUnary(new CommandVariable(lexems[0].value), kind);
                }
            }
            throw new InvalidOperationException("Неизвестный синтаксический примитив! " + string.Join(", ", lexems.Select(s => s.kind.ToString()).ToArray()));
        }

        private static ICommand parseCommandPart(List<Lexem> lexems, int level) {
            if (level >= 7)
                return parsePrimitive(lexems);

            int count = 1;
            List<List<Lexem>> list = new List<List<Lexem>>();
            LexemKind compareKind = LexemKind.UNKNOWN;
            List<Lexem> buffer = new List<Lexem>();
            foreach (Lexem lexem in lexems) {
                if (level == 0) {
                    if (lexem.kind == LexemKind.AND) {
                        count++;
                        list.Add(buffer);
                        buffer = new List<Lexem>();
                        continue;
                    }
                } else if (level == 1) {
                    if (lexem.kind == LexemKind.OR) {
                        count++;
                        list.Add(buffer);
                        buffer = new List<Lexem>();
                        continue;
                    }
                } else if (level == 2) {
                    if (LexemDefinitions.isLogicCompare(lexem.kind)) {
                        count++;
                        list.Add(buffer);
                        buffer = new List<Lexem>();
                        compareKind = lexem.kind;
                        continue;
                    }
                } else if (level == 3) {
                    if (lexem.kind == LexemKind.PLUS) {
                        count++;
                        list.Add(buffer);
                        buffer = new List<Lexem>();
                        continue;
                    }
                } else if (level == 4) {
                    if (lexem.kind == LexemKind.MINUS) {
                        count++;
                        list.Add(buffer);
                        buffer = new List<Lexem>();
                        continue;
                    }
                } else if (level == 5) {
                    if (lexem.kind == LexemKind.MULTIPLY) {
                        count++;
                        list.Add(buffer);
                        buffer = new List<Lexem>();
                        continue;
                    }
                } else if (level == 6) {
                    if (lexem.kind == LexemKind.DIVIDE) {
                        count++;
                        list.Add(buffer);
                        buffer = new List<Lexem>();
                        continue;
                    }
                }
                buffer.Add(lexem);
            }

            if (count == 1) {
                return parseCommandPart(buffer, level + 1);
            } else {
                list.Add(buffer);
                if (level == 0) {
                    CommandLogicCompound compound = new CommandLogicCompound(LexemKind.AND);
                    foreach (List<Lexem> sub in list)
                        compound.addBlock(parseCommandPart(sub, level + 1));
                    return compound;
                } else if (level == 1) {
                    CommandLogicCompound compound = new CommandLogicCompound(LexemKind.OR);
                    foreach (List<Lexem> sub in list)
                        compound.addBlock(parseCommandPart(sub, level + 1));
                    return compound;
                } else if (level == 2) {
                    if (compareKind == LexemKind.UNKNOWN || count > 2)
                        return null;
                    CommandLogic compound = new CommandLogic(
                        compareKind,
                        parseCommandPart(list[0], level + 1),
                        parseCommandPart(list[1], level + 1)
                    );
                    return compound;
                } else if (level == 3) {
                    CommandMath compound = new CommandMath(LexemKind.PLUS);
                    foreach (List<Lexem> sub in list)
                        compound.addBlock(parseCommandPart(sub, level + 1));
                    return compound;
                } else if (level == 4) {
                    CommandMath compound = new CommandMath(LexemKind.MINUS);
                    foreach (List<Lexem> sub in list)
                        compound.addBlock(parseCommandPart(sub, level + 1));
                    return compound;
                } else if (level == 5) {
                    CommandMath compound = new CommandMath(LexemKind.MULTIPLY);
                    foreach (List<Lexem> sub in list)
                        compound.addBlock(parseCommandPart(sub, level + 1));
                    return compound;
                } else if (level == 6) {
                    CommandMath compound = new CommandMath(LexemKind.DIVIDE);
                    foreach (List<Lexem> sub in list)
                        compound.addBlock(parseCommandPart(sub, level + 1));
                    return compound;
                }
            }

            return null;
        }

        /// <summary>
        /// Пропарсить целую команду, отделённую точкой с запятой (или же выражение в скобках)
        /// </summary>
        private static ICommand parseCommand(List<Lexem> lexems) {
            if (lexems.Count == 0)
                return null;

            if (lexems.Count >= 2 && lexems[0].kind == LexemKind.IDENTIFIER) {
                string value = lexems[0].value;
                if (lexems[1].kind == LexemKind.ASSIGN) {
                    lexems.RemoveRange(0, 2);
                    return new CommandAssign(new CommandVariable(value), parseCommandPart(lexems, 0));
                } else if (lexems.Count >= 3 && lexems[1].kind == LexemKind.INDEX && lexems[2].kind == LexemKind.ASSIGN) {
                    ICommand index = parseCommandPart(lexems[1].childs, 0);
                    lexems.RemoveRange(0, 3);
                    return new CommandAssignIndex(
                        new CommandVariable(value),
                        index,
                        parseCommandPart(lexems, 0)
                    );
                }
            }

            return parseCommandPart(lexems, 0);
        }

        private static CommandElse parseElse(Lexem block) {
            Script commandBlock = parseBlock(block);
            if (commandBlock == null)
                throw new Exception("Ошибка в блоке кода конструкции else!");
            return new CommandElse(commandBlock);
        }
        private static CommandElseIf parseElseIf(Lexem brace, Lexem block) {
            ICommand condition = parseCommand(brace.childs); //parseLogic(brace.childs);
            Script commandBlock = parseBlock(block);
            if (commandBlock == null)
                throw new Exception("Ошибка в блоке кода конструкции else if!");
            if (condition == null)
                throw new Exception("Ошибка в блоке условия конструкции else if!");
            return new CommandElseIf(condition, commandBlock);
        }

        /// <summary>
        /// Блок из команд: {block}
        /// </summary>
        private static Script parseBlock(Lexem lexemBlock) {
            List<Lexem> lexems = lexemBlock.childs;
            Script block = new Script();
            int lexemsCount = lexems.Count;
            List<Lexem> oneCommand = new List<Lexem>();
            bool isOneCommandReturn = false;

            for (int i = 0; i < lexemsCount; i++) {
                Lexem lexem = lexems[i];
                if (lexem.kind == LexemKind.SEMICOLON) {
                    ICommand command = parseCommand(oneCommand);
                    oneCommand.Clear();
                    if (command == null)
                        throw new Exception("Неизвестная операция в скрипте!");
                    if (isOneCommandReturn) {
                        block.addCommand(new CommandReturn(command));
                    } else {
                        block.addCommand(command);
                    }
                } else if (lexem.kind == LexemKind.IF) {
                    if (i + 2 < lexemsCount && lexems[i + 1].kind == LexemKind.BRACE && lexems[i + 2].kind == LexemKind.BLOCK) {
                        ICommand condition = parseCommand(lexems[i + 1].childs); //parseLogic(lexems[i + 1].childs);
                        ICommand ifBlock = parseBlock(lexems[i + 2]);
                        //теперь проверяем else блоки
                        List<ICommand> elseStatements = new List<ICommand>();
                        int n = i + 3;
                        while (n < lexemsCount && lexems[n].kind == LexemKind.ELSE) {
                            if (lexems[n + 1].kind == LexemKind.IF) {
                                //else if statement
                                CommandElseIf command = parseElseIf(lexems[n + 2], lexems[n + 3]);
                                if (command == null)
                                    throw new Exception("Ошибка в конструкции else if!");
                                elseStatements.Add(command);
                                n += 4;
                            } else if (lexems[n + 1].kind == LexemKind.BLOCK) {
                                //else statement
                                elseStatements.Add(parseElse(lexems[n + 1]));
                                n += 2;
                            } else {
                                throw new Exception("Неверная конструкция else! Необходимо: else <if (условие)> {блок кода}");
                            }
                        }
                        i = n - 1;
                        block.addCommand(new CommandIf(condition, ifBlock, elseStatements));
                    } else {
                        throw new Exception("Неверная конструкция if! Необходимо: if (условие) {блок кода}");
                    }
                    oneCommand.Clear();
                } else if (lexem.kind == LexemKind.FOR) {
                    if (i + 2 < lexemsCount && lexems[i + 1].kind == LexemKind.BRACE && lexems[i + 2].kind == LexemKind.BLOCK) {
                        //заголовок цикла (начало; условие; шаг)
                        Script head = parseBlock(lexems[i + 1]);
                        List<ICommand> headCommands = head.getCommands();
                        if (headCommands.Count != 3)
                            throw new Exception("Неверная конструкция заголовка for! Необходимо: for (начало; условие; шаг)");
                        ICommand begin = headCommands[0] == null ? new CommandEmpty() : headCommands[0];
                        ICommand condition = headCommands[1] == null ? new CommandEmpty() : headCommands[1];
                        ICommand step = headCommands[2] == null ? new CommandEmpty() : headCommands[2];
                        ICommand command = parseBlock(lexems[i + 2]);
                        block.addCommand(new CommandFor(begin, condition, step, command));
                        i += 3 - 1;
                    } else {
                        throw new Exception("Неверная конструкция for! Необходимо: for (начало; условие; шаг) {блок кода}");
                    }
                    oneCommand.Clear();
                } else if (lexem.kind == LexemKind.WHILE) {
                    if (i + 2 < lexemsCount && lexems[i + 1].kind == LexemKind.BRACE && lexems[i + 2].kind == LexemKind.BLOCK) {
                        ICommand condition = parseCommand(lexems[i + 1].childs);
                        ICommand command = parseBlock(lexems[i + 2]);
                        block.addCommand(new CommandWhile(condition, command));
                        i += 3 - 1;
                    } else {
                        throw new Exception("Неверная конструкция while! Необходимо: while (условие) {блок кода}");
                    }
                    oneCommand.Clear();
                } else if (lexem.kind == LexemKind.FUNCTION) {
                    if (i + 3 < lexemsCount && lexems[i + 1].kind == LexemKind.IDENTIFIER && lexems[i + 2].kind == LexemKind.BRACE && lexems[i + 3].kind == LexemKind.BLOCK) {
                        string fname = lexems[i + 1].value;
                        List<string> fargs = parseLocalFunctionArgs(lexems[i + 2].childs);
                        Script fcode = parseBlock(lexems[i + 3]);
                        fcode.isLocalFunctionBlock = true;
                        block.localFunctions.Add(new LocalFunction() {
                            name = fname,
                            code = fcode,
                            args = fargs
                        });
                        i += 4 - 1;
                    } else {
                        throw new Exception("Неверная конструкция function! Необходимо: function (arg0, arg1, arg2, ...) {блок кода}");
                    }
                    oneCommand.Clear();
                } else if (lexem.kind == LexemKind.RETURN) {
                    isOneCommandReturn = true;
                    oneCommand.Clear();
                } else {
                    oneCommand.Add(lexem);
                }
            }

            if (oneCommand.Count > 0) {
                ICommand command = parseCommand(oneCommand);
                if (command == null)
                    throw new Exception("Неизвестная операция в скрипте!");
                oneCommand.Clear();
                block.addCommand(command);
            }

            return block;
        }

        /// <summary>
        /// Пропарсить названия аргументов функции. Пример: (arg1, arg2, arg3, ...)
        /// </summary>
        private static List<string> parseLocalFunctionArgs(List<Lexem> lexems) {
            List<string> result = new List<string>();
            bool comma = false;
            while (lexems.Count > 0) {
                if (comma) {
                    if (lexems[0].kind == LexemKind.COMMA)
                        comma = false;
                } else {
                    if (lexems[0].kind == LexemKind.IDENTIFIER) {
                        result.Add(lexems[0].value);
                        comma = true;
                    }
                }
                lexems.RemoveAt(0);
            }
            return result;
        }

        /// <summary>
        /// Главная функция. Парсит скрипт в запускаемые команды
        /// </summary>
        public static Script getScriptChunk(string script) {
            LexemParser lexemParser = new LexemParser(script);
            List<Lexem> lexems = lexemParser.lexems;
            Lexem lexemBlock = new Lexem() {
                childs = lexems,
                kind = LexemKind.BLOCK,
            };
            return parseBlock(lexemBlock);
        }

    }

    /// <summary>
    /// Любая команда скрипта. Арифметическая операция, условие, вызов функции и т.д.
    /// Некоторые команды имеют в себе блок для других команд.
    /// </summary>
    public interface ICommand {
        object execute(Engine engine);
    }

    #region STATEMENTS
    public class CommandLogicCompound : ICommand {
        private List<ICommand> blocks = new List<ICommand>();
        private LexemKind operation;

        public CommandLogicCompound(LexemKind operation) {
            this.operation = operation;
        }

        public void addBlock(ICommand block) {
            this.blocks.Add(block);
        }

        public object execute(Engine engine) {
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

        public object execute(Engine engine) {
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

        public CommandReturn(ICommand command) {
            this.command = command;
        }

        public object execute(Engine engine) {
            throw new ScriptInterruptException(
                ScriptInterruptException.CODE_RETURN,
                command.execute(engine)
            );
        }

    }
    public class CommandElse : ICommand {
        private ICommand command;

        public CommandElse(ICommand command) {
            this.command = command;
        }

        public object execute(Engine engine) {
            command.execute(engine);
            return null;
        }

    }
    public class CommandElseIf : ICommand {
        private ICommand condition;
        private ICommand command;

        public CommandElseIf(ICommand condition, ICommand command) {
            this.condition = condition;
            this.command = command;
        }

        public object execute(Engine engine) {
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

        public CommandIf(ICommand condition, ICommand command, List<ICommand> elseStatements) {
            this.condition = condition;
            this.command = command;
            this.elseStatements = elseStatements;
        }

        public object execute(Engine engine) {
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

        public CommandFor(ICommand pre, ICommand condition, ICommand iter, ICommand command) {
            this.pre = pre;
            this.condition = condition;
            this.iter = iter;
            this.command = command;
        }

        public object execute(Engine engine) {
            for (pre.execute(engine); (bool) engine.getRealValue(condition.execute(engine)); iter.execute(engine)) {
                command.execute(engine);
            }
            return null;
        }

    }
    public class CommandWhile : ICommand {
        private ICommand condition;
        private ICommand command;

        public CommandWhile(ICommand condition, ICommand command) {
            this.condition = condition;
            this.command = command;
        }

        public object execute(Engine engine) {
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
        public bool isLocalFunctionBlock;
        public List<LocalFunction> localFunctions = new List<LocalFunction>();

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

        public string run(Engine engine) {
            StringBuilder stringBuilder = new StringBuilder();
            StringWriter stringWriter = new StringWriter(stringBuilder);
            this.execute(engine);
            stringWriter.Close();
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Запустить все команды в чанке и получить вывод
        /// </summary>
        public object execute(Engine engine) {
            engine.addLocalFunctions(localFunctions);
            if (isLocalFunctionBlock) {
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
                return null;
            } else {
                foreach (ICommand command in commands)
                    command.execute(engine);
                return null;
            }
        }

    }
    #endregion

    #region COMMON
    public class CommandIndex : ICommand {
        private ICommand command;
        private List<ICommand> index;

        public CommandIndex(ICommand command, List<ICommand> index) {
            this.command = command;
            this.index = index;
        }

        public object execute(Engine engine) {
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
                        //MHSErrorHelper.logCommon("Ошибка при безопасной индексации: выход за границы списка: " + x + " за пределами [0, " + (count - 1) + "]!");
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
                        //MHSErrorHelper.logCommon("Ошибка при безопасной индексации: выход за границы строки: " + x + " за пределами [0, " + (count - 1) + "]!");
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
                        //MHSErrorHelper.logCommon("Ошибка при безопасной индексации: ключ не найден в словаре!");
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
        
        public object execute(Engine engine) {
            return null;
        }

    }
    public class CommandAssign : ICommand {
        private ICommand left, right;

        public CommandAssign(ICommand left, ICommand right) {
            this.left = left;
            this.right = right;
        }

        public object execute(Engine engine) {
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

        public CommandAssignIndex(ICommand left, ICommand index, ICommand right) {
            this.left = left;
            this.index = index;
            this.right = right;
        }

        public object execute(Engine engine) {
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

        public CommandUnary(ICommand command, LexemKind operation) {
            this.command = command;
            this.operation = operation;
        }

        public object execute(Engine engine) {
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

        public object execute(Engine engine) {
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
        private float value;

        public CommandNumeric(float value) {
            this.value = value;
        }

        public object execute(Engine engine) {
            return value;
        }

    }
    public class CommandString : ICommand {
        private string value;

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

        public object execute(Engine engine) {
            return value;
        }

    }
    public class CommandStringVariabled : ICommand {
        private List<ICommand> commands;

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

        public object execute(Engine engine) {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (ICommand command in commands)
                stringBuilder.Append(engine.getRealValue(command.execute(engine)));
            return stringBuilder.ToString();
        }

    }
    public class CommandBool : ICommand {
        private bool value;

        public CommandBool(bool value) {
            this.value = value;
        }

        public object execute(Engine engine) {
            return value;
        }

    }
    public class CommandVariable : ICommand {
        private string variableName;

        public CommandVariable(string variableName) {
            this.variableName = variableName;
        }

        public object execute(Engine engine) {
            return engine.getVariable(this.variableName);
        }

    }
    public class CommandDotVariable : ICommand {
        private ICommand obj;
        private string variableName;

        public CommandDotVariable(ICommand obj, string variableName) {
            this.obj = obj;
            this.variableName = variableName;
        }

        public object execute(Engine engine) {
            return engine.getDotProperty(obj.execute(engine), this.variableName);
        }

    }
    public class CommandGlobalFunction : ICommand {
        private string functionName;
        private List<ICommand> args;

        public CommandGlobalFunction(string functionName, List<ICommand> args) {
            this.functionName = functionName;
            this.args = args;
        }

        public object execute(Engine engine) {
            object[] resultArgs = new object[args.Count];
            for (int i = 0; i < args.Count; i++)
                resultArgs[i] = args[i] == null ? null : engine.getRealValue(args[i].execute(engine));

            return engine.executeFunction(this.functionName, engine, resultArgs);
        }

    }
    public class CommandDotFunction : ICommand {
        private ICommand obj;
        private string functionName;
        private List<ICommand> args;

        public CommandDotFunction(ICommand obj, string functionName, List<ICommand> args) {
            this.obj = obj;
            this.functionName = functionName;
            this.args = args;
        }

        public object execute(Engine engine) {
            object[] resultArgs = new object[args.Count];
            for (int i = 0; i < args.Count; i++)
                resultArgs[i] = args[i] == null ? null : engine.getRealValue(args[i].execute(engine));

            return engine.executeDotFunction(obj.execute(engine), this.functionName, engine, resultArgs);
        }

    }
    #endregion

}
