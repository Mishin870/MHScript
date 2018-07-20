using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Mishin870.MHScript {
    
    public enum LexemKind {
        UNKNOWN, HTML_LITERAL,
        AND, OR,
        TRUE, FALSE,
        IF, ELSE, FOR, WHILE, FUNCTION, RETURN,
        LESSER, GREATER, LESSER_EQUALS, GREATER_EQUALS, EQUALS, NOTEQUALS, NOT,
        INCREMENT, DECREMENT, PREINCREMENT, PREDECREMENT,
        COMMA, DOT,
        BRACE, BLOCK, INDEX,
	    PLUS,
	    MINUS,
	    MULTIPLY,
	    DIVIDE,
	    ASSIGN,
	    SEMICOLON,
	    IDENTIFIER,
	    NUMBER,
        STRING, STRING_VARIABLED,
    }

    public class LocationEntity {
	    public int offset;
	    public int length;
    }

    public class Lexem : LocationEntity {
	    public LexemKind kind;
	    public string value;
        public List<Lexem> childs;
    }

    class LexemDefinition<T> {
	    public LexemKind kind { get; protected set; }
	    public T representation  { get; protected set; }
    }

    class StaticLexemDefinition : LexemDefinition<string> {
	    public bool isKeyword;

        public StaticLexemDefinition(string representation, LexemKind kind, bool isKeyword) {
            this.representation = representation;
            this.kind = kind;
            this.isKeyword = isKeyword;
        }

        public StaticLexemDefinition(string representation, LexemKind kind) {
            this.representation = representation;
            this.kind = kind;
            this.isKeyword = false;
        }

    }

    class DynamicLexemDefinition : LexemDefinition<Regex> {

        public DynamicLexemDefinition(string representation, LexemKind kind) {
            this.representation = new Regex(@"\G" + representation, RegexOptions.Compiled);
		    this.kind = kind;
	    }

        public DynamicLexemDefinition(string representation, LexemKind kind, string flags) {
            this.representation = new Regex(@"\G" + flags + representation, RegexOptions.Compiled);
            this.kind = kind;
        }

    }

    static class LexemDefinitions {

	    public static StaticLexemDefinition[] statics = new StaticLexemDefinition[] {
            new StaticLexemDefinition("if", LexemKind.IF),
            new StaticLexemDefinition("else", LexemKind.ELSE),
            new StaticLexemDefinition("for", LexemKind.FOR),
            new StaticLexemDefinition("while", LexemKind.WHILE),
            new StaticLexemDefinition("function", LexemKind.FUNCTION),
            new StaticLexemDefinition("return", LexemKind.RETURN),
            new StaticLexemDefinition("true", LexemKind.TRUE),
            new StaticLexemDefinition("false", LexemKind.FALSE),

            new StaticLexemDefinition("(", LexemKind.BRACE),
            new StaticLexemDefinition("{", LexemKind.BLOCK),
            new StaticLexemDefinition("[", LexemKind.INDEX),
            new StaticLexemDefinition(";", LexemKind.SEMICOLON),
            new StaticLexemDefinition(",", LexemKind.COMMA),
            new StaticLexemDefinition(".", LexemKind.DOT),
            
            new StaticLexemDefinition("?>", LexemKind.HTML_LITERAL),

            new StaticLexemDefinition("&&", LexemKind.AND),
            new StaticLexemDefinition("||", LexemKind.OR),
            new StaticLexemDefinition("<=", LexemKind.LESSER_EQUALS),
            new StaticLexemDefinition(">=", LexemKind.GREATER_EQUALS),
            new StaticLexemDefinition("<", LexemKind.LESSER),
            new StaticLexemDefinition(">", LexemKind.GREATER),
            new StaticLexemDefinition("==", LexemKind.EQUALS),
            new StaticLexemDefinition("!=", LexemKind.NOTEQUALS),
            new StaticLexemDefinition("!", LexemKind.NOT),

		    new StaticLexemDefinition("=", LexemKind.ASSIGN),
            new StaticLexemDefinition("++", LexemKind.INCREMENT),
            new StaticLexemDefinition("--", LexemKind.DECREMENT),
		    new StaticLexemDefinition("+", LexemKind.PLUS),
		    new StaticLexemDefinition("-", LexemKind.MINUS),
		    new StaticLexemDefinition("*", LexemKind.MULTIPLY),
		    new StaticLexemDefinition("/", LexemKind.DIVIDE),
	    };
    	
	    public static DynamicLexemDefinition[] dynamics = new DynamicLexemDefinition[] {
		    new DynamicLexemDefinition("[a-zA-Z_][a-zA-Z0-9_]*", LexemKind.IDENTIFIER),
		    new DynamicLexemDefinition("(0|[1-9][0-9]*)", LexemKind.NUMBER),
            new DynamicLexemDefinition("@\".*?\"", LexemKind.STRING_VARIABLED),
            new DynamicLexemDefinition("\".*?\"", LexemKind.STRING),
            new DynamicLexemDefinition("'.*?'", LexemKind.STRING),
	    };

        /// <summary>
        /// Считается ли данная лексема логическим сравнением?
        /// <, >, <=, >=, ==, !=
        /// </summary>
        public static bool isLogicCompare(LexemKind kind) {
            return kind == LexemKind.LESSER ||
                kind == LexemKind.GREATER ||
                kind == LexemKind.LESSER_EQUALS ||
                kind == LexemKind.GREATER_EQUALS ||
                kind == LexemKind.EQUALS ||
                kind == LexemKind.NOTEQUALS;
        }

    }

    public class LexemParser {
        //private static readonly Regex COMMENT = new Regex(@"\G\\/\\*.*?\\*\\/", RegexOptions.Compiled | RegexOptions.Singleline);
	    private string source;
	    private int offset;
        private int sourceLength;
	    public List<Lexem> lexems {
            get; private set;
        }

        private Lexem htmlLexem;
        private int htmlInlineOpen;
    	
	    public LexemParser(string source) {
            if (source.StartsWith("?>")) {
                this.source = source;
            } else {
                this.source = "?>" + source;
            }
            this.sourceLength = this.source.Length;

            List<Lexem> lexems = new List<Lexem>();
            parse(lexems, true);
            this.lexems = lexems;
	    }

        private void parse(List<Lexem> list, bool isCode) {
            LexemKind kind = LexemKind.UNKNOWN;
            LexemKind prevKind = LexemKind.UNKNOWN;
            while (offset < sourceLength) {
                //пропуск лишних символов (пробел, таб, новая строка, перенос каретки)
                char c = source[offset];
                while (offset < sourceLength && (c == ' ' || c == '\t' || c == '\r' || c == '\n')) {
                    offset++;
                    if (offset >= sourceLength)
                        break;
                    c = source[offset];
                }

                if (isCode) {
                    if (offset >= sourceLength || c == ')' || c == '}' || c == ']') {
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
                    if (kind == LexemKind.BRACE || kind == LexemKind.BLOCK || kind == LexemKind.INDEX || kind == LexemKind.HTML_LITERAL) {
                        if (kind == LexemKind.HTML_LITERAL) {
                            this.htmlLexem = lexem;
                            this.htmlInlineOpen = offset;
                        }

                        List<Lexem> childs = new List<Lexem>();
                        parse(childs, kind != LexemKind.HTML_LITERAL);
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
                } else {
                    if (offset + 3 < sourceLength && source[offset] == '<' && source[offset + 1] == '?' && source[offset + 2] == 'm' && source[offset + 3] == 'h') {
                        htmlLexem.value = source.Substring(htmlInlineOpen, offset - htmlInlineOpen);
                        offset += 4;
                        break;
                    }
                    offset++;
                    //доп проверка после прибавления индекса
                    if (offset >= sourceLength) {
                        htmlLexem.value = source.Substring(htmlInlineOpen, sourceLength - htmlInlineOpen);
                        offset++;
                        break;
                    }
                }
                prevKind = kind;
            }
        }
    	
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
