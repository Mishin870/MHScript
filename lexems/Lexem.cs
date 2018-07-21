using System;
using System.Collections.Generic;

namespace Mishin870.MHScript.lexems {

    /// <summary>
    /// Виды возможных лексем в скрипте
    /// </summary>
    public enum LexemKind {
        UNKNOWN,
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
