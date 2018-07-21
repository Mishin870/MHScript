using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Mishin870.MHScript.lexems {

    /// <summary>
    /// Рекурсивный парсер сырого исходного текста скрипта в список лексем и их внутренностей.
    /// </summary>
    public class LexemParser {
        //private static readonly Regex COMMENT = new Regex(@"\G\\/\\*.*?\\*\\/", RegexOptions.Compiled | RegexOptions.Singleline);
        private string source;
        private int offset;
        private int sourceLength;
        public List<Lexem> lexems {
            get;
            private set;
        }

        public LexemParser(string source) {
            this.source = source;
            this.sourceLength = this.source.Length;

            List<Lexem> lexems = new List<Lexem>();
            parse(lexems);
            this.lexems = lexems;
        }

        private void parse(List<Lexem> list) {
            LexemKind kind = LexemKind.UNKNOWN;
            LexemKind prevKind = LexemKind.UNKNOWN;
            while (offset < sourceLength) {
                //пропуск лишних символов (пробел, таб, новая строка, перенос каретки)
                char c = source[offset];
                while (offset < sourceLength && LexemDefinitions.isEmptyCharacter(c)) {
                    offset++;
                    if (offset >= sourceLength)
                        break;
                    c = source[offset];
                }

                if (offset >= sourceLength || LexemDefinitions.isCloseBrace(c)) {
                    offset++;
                    break;
                }

                /*Match match = COMMENT.Match(source, offset);
                if (match.Success) {
                    this.offset += match.Length;
                    continue;
                }*/

                Lexem lexem = parseStatic() ?? parseDynamic();
                if (lexem == null)
                    throw new Exception(string.Format("Неизвестная лексема на позиции {0}: {1}", offset, source.Substring(offset, Math.Min(30, sourceLength - offset)).Replace("<", "&lt;").Replace(">", "&gt;")));

                kind = lexem.kind;
                if (kind == LexemKind.BRACE || kind == LexemKind.BLOCK || kind == LexemKind.INDEX) {
                    List<Lexem> childs = new List<Lexem>();
                    parse(childs);
                    lexem.childs = childs;
                } else if (kind == LexemKind.NUMBER && prevKind == LexemKind.MINUS) {
                    if (list.Count >= 2 && (list[list.Count - 2].kind == LexemKind.ASSIGN || list[list.Count - 2].kind == LexemKind.COMMA)) {
                        list.RemoveAt(list.Count - 1);
                    } else {
                        list[list.Count - 1].kind = LexemKind.PLUS;
                    }
                    lexem.value = "-" + lexem.value;
                }

                list.Add(lexem);

                prevKind = kind;
            }
        }

        /// <summary>
        /// Пропарсить возможные статичные лексемы на текущей позиции
        /// </summary>
        private Lexem parseStatic() {
            foreach (var def in LexemDefinitions.statics) {
                string rep = def.representation;
                int len = rep.Length;

                if (offset + len > sourceLength || source.Substring(offset, len) != rep)
                    continue;

                if (offset + len < sourceLength && def.isKeyword) {
                    char nextChar = source[offset + len];
                    if (nextChar == '_' || char.IsLetterOrDigit(nextChar))
                        continue;
                }

                this.offset += len;
                return new Lexem {
                    kind = def.kind,
                    offset = this.offset,
                    length = len
                };
            }
            return null;
        }

        /// <summary>
        /// Пропарсить возможные динамические лексемы на текущей позиции
        /// </summary>
        private Lexem parseDynamic() {
            foreach (var def in LexemDefinitions.dynamics) {
                Match match = def.representation.Match(source, offset);
                if (!match.Success)
                    continue;

                this.offset += match.Length;
                return new Lexem {
                    kind = def.kind,
                    offset = this.offset,
                    length = match.Length,
                    value = match.Value
                };
            }
            return null;
        }

    }
}
