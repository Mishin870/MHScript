using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mishin870.MHScript.engine.lexems;
using System.Globalization;
using Mishin870.MHScript.engine.objects;

namespace Mishin870.MHScript.engine.commands {
    public class CommandsParser {

        /// <summary>
        /// Парсит аргументы функции (как отдельные команды, разделённые запятой)
        /// </summary>
        private static List<ICommand> parseFunctionCallArgs(Lexem brace) {
            List<Lexem> lexems = brace.childs;
            List<Lexem> currentLexems = new List<Lexem>();
            List<ICommand> commands = new List<ICommand>();
            while (lexems.Count > 0) {
                if (lexems[0].kind == LexemKind.COMMA) {
                    commands.Add(parseStatement(currentLexems));
                    currentLexems.Clear();
                    lexems.RemoveAt(0);
                } else {
                    currentLexems.Add(lexems[0]);
                    lexems.RemoveAt(0);
                }
            }
            if (currentLexems.Count > 0) {
                commands.Add(parseStatement(currentLexems));
                currentLexems.Clear();
            }
            return commands;
        }

        /// <summary>
        /// Пропарсить названия аргументов локальной функции. Пример: (arg1, arg2, arg3, ...)
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
        /// Часть именованнного примитива (между точками)
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
                        return new CommandGlobalFunction(value, parseFunctionCallArgs(lexems[0]));
                    } else {
                        return new CommandDotFunction(prevCommand, value, parseFunctionCallArgs(lexems[0]));
                    }
                } else if (lexems[0].kind == LexemKind.INDEX) {
                    if (prevCommand == null) {
                        return new CommandIndex(new CommandVariable(value), parseFunctionCallArgs(lexems[0]));
                    } else {
                        return new CommandIndex(new CommandDotVariable(prevCommand, value), parseFunctionCallArgs(lexems[0]));
                    }
                }
            } else if (lexems.Count == 2) {
                if (lexems[0].kind == LexemKind.BRACE && lexems[1].kind == LexemKind.INDEX) {
                    if (prevCommand == null) {
                        return new CommandIndex(
                            new CommandGlobalFunction(value, parseFunctionCallArgs(lexems[0])),
                            parseFunctionCallArgs(lexems[1])
                        );
                    } else {
                        return new CommandIndex(
                            new CommandDotFunction(prevCommand, value, parseFunctionCallArgs(lexems[0])),
                            parseFunctionCallArgs(lexems[1])
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

        /// <summary>
        /// Пропарсить синтаксический примитив
        /// (любой литерал, унарные операции и просто переменные или вызовы функций)
        /// </summary>
        /// <param name="lexems">список лексем примитива</param>
        private static ICommand parsePrimitive(List<Lexem> lexems) {
            if (lexems.Count == 0)
                return null;

            LexemKind kind = lexems[0].kind;
            if (kind == LexemKind.NOT) {
                lexems.RemoveAt(0);
                return new CommandUnary(parsePrimitive(lexems), LexemKind.NOT);
            } else if (kind == LexemKind.BRACE && lexems.Count == 1) {
                return parseStatement(lexems[0].childs);
            } else if (kind == LexemKind.NUMBER && lexems.Count == 1) {
                return new CommandNumeric(int.Parse(lexems[0].value));
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

        /// <summary>
        /// Парсит математическое или логическое выражение, разделённое
        /// при помощи +, -, *, /, логическое сравнение, &&, ||.
        /// Делит выражение рекурсивно по этапам, компонуя разделённые части на каждом этапе
        /// определённой командой (например, CommandMath)
        /// </summary>
        /// <param name="lexems">список лексем, который нужно разделить</param>
        /// <param name="level">уровень деления</param>
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
        /// Пропарсить целую команду, отделённую точкой с запятой (или выражение в скобках)
        /// </summary>
        private static ICommand parseStatement(List<Lexem> lexems) {
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

        /// <summary>
        /// Пропарсить содержимое блочной лексемы
        /// </summary>
        private static Script parseCommandsBlock(Lexem lexemBlock) {
            List<Lexem> lexems = lexemBlock.childs;
            Script block = new Script();
            int lexemsCount = lexems.Count;
            List<Lexem> oneCommand = new List<Lexem>();
            bool isOneCommandReturn = false;

            for (int i = 0; i < lexemsCount; i++) {
                Lexem lexem = lexems[i];
                if (lexem.kind == LexemKind.SEMICOLON) {
                    ICommand command = parseStatement(oneCommand);
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
                        ICommand condition = parseStatement(lexems[i + 1].childs);
                        ICommand ifBlock = parseCommandsBlock(lexems[i + 2]);
                        //теперь проверяем else блоки
                        List<ICommand> elseStatements = new List<ICommand>();
                        int n = i + 3;
                        while (n < lexemsCount && lexems[n].kind == LexemKind.ELSE) {
                            if (lexems[n + 1].kind == LexemKind.IF) {
                                //else if statement
                                ICommand elseIfCondition = parseStatement(lexems[n + 2].childs);
                                Script commandBlock = parseCommandsBlock(lexems[n + 3]);
                                if (commandBlock == null)
                                    throw new Exception("Ошибка в блоке кода конструкции else if!");
                                if (elseIfCondition == null)
                                    throw new Exception("Ошибка в блоке условия конструкции else if!");
                                CommandElseIf command = new CommandElseIf(elseIfCondition, commandBlock);
                                if (command == null)
                                    throw new Exception("Ошибка в конструкции else if!");
                                elseStatements.Add(command);
                                n += 4;
                            } else if (lexems[n + 1].kind == LexemKind.BLOCK) {
                                //else statement
                                Script elseBlock = parseCommandsBlock(lexems[n + 1]);
                                if (elseBlock == null)
                                    throw new Exception("Ошибка в блоке кода конструкции else!");
                                elseStatements.Add(new CommandElse(elseBlock));
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
                        Script head = parseCommandsBlock(lexems[i + 1]);
                        List<ICommand> headCommands = head.getCommands();
                        if (headCommands.Count != 3)
                            throw new Exception("Неверная конструкция заголовка for! Необходимо: for (начало; условие; шаг)");
                        ICommand begin = headCommands[0] == null ? new CommandEmpty() : headCommands[0];
                        ICommand condition = headCommands[1] == null ? new CommandEmpty() : headCommands[1];
                        ICommand step = headCommands[2] == null ? new CommandEmpty() : headCommands[2];
                        ICommand command = parseCommandsBlock(lexems[i + 2]);
                        block.addCommand(new CommandFor(begin, condition, step, command));
                        i += 3 - 1;
                    } else {
                        throw new Exception("Неверная конструкция for! Необходимо: for (начало; условие; шаг) {блок кода}");
                    }
                    oneCommand.Clear();
                } else if (lexem.kind == LexemKind.WHILE) {
                    if (i + 2 < lexemsCount && lexems[i + 1].kind == LexemKind.BRACE && lexems[i + 2].kind == LexemKind.BLOCK) {
                        ICommand condition = parseStatement(lexems[i + 1].childs);
                        ICommand command = parseCommandsBlock(lexems[i + 2]);
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
                        Script fcode = parseCommandsBlock(lexems[i + 3]);
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
                ICommand command = parseStatement(oneCommand);
                if (command == null)
                    throw new Exception("Неизвестная операция в скрипте!");
                oneCommand.Clear();
                block.addCommand(command);
            }

            return block;
        }

        /// <summary>
        /// Главная функция. Парсит скрипт в запускаемые команды
        /// </summary>
        public static Script parseScript(string script) {
            LexemParser lexemParser = new LexemParser(script);
            List<Lexem> lexems = lexemParser.lexems;
            Lexem lexemBlock = new Lexem() {
                childs = lexems,
                kind = LexemKind.BLOCK,
            };
            return parseCommandsBlock(lexemBlock);
        }

    }
}
