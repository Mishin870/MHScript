using System;
using System.Collections.Generic;

namespace Mishin870.MHScript.engine.lexems {

    /// <summary>
    /// Виды возможных лексем в скрипте
    /// </summary>
    public enum LexemKind : byte {
        UNKNOWN = 0,
        AND = 1, OR = 2,
        TRUE = 3, FALSE = 4,
        IF = 5, ELSE = 6, FOR = 7, WHILE = 8, FUNCTION = 9, RETURN = 10,
        LESSER = 11, GREATER = 12, LESSER_EQUALS = 13, GREATER_EQUALS = 14, EQUALS = 15, NOTEQUALS = 16, NOT = 17,
        INCREMENT = 18, DECREMENT = 19, PREINCREMENT = 20, PREDECREMENT = 21,
        COMMA = 22, DOT = 23,
        BRACE = 24, BLOCK = 25, INDEX = 26,
        PLUS = 27,
        MINUS = 28,
        MULTIPLY = 29,
        DIVIDE = 30,
        ASSIGN = 31,
        SEMICOLON = 32,
        IDENTIFIER = 33,
        NUMBER = 34,
        STRING = 35, STRING_VARIABLED = 36,
    }

    /// <summary>
    /// Позиция чего-либо в тексте
    /// </summary>
    public class LocationEntity {
	    public int offset;
	    public int length;
    }

    /// <summary>
    /// Лексема (минимальная сущность языка)
    /// </summary>
    public class Lexem : LocationEntity {
        public LexemKind kind;
        public string value;
        /// <summary>
        /// Внутренние лексемы (если это лексема BLOCK)
        /// </summary>
        public List<Lexem> childs;
    }

}
