using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mishin870.MHScript.engine.lexems {
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

        /// <summary>
        /// Является ли данный символ закрывающим к какой-либо блочной лексеме?
        /// </summary>
        public static bool isCloseBrace(char c) {
            return c == ')' || c == '}' || c == ']';
        }

        /// <summary>
        /// Является ли данный символ бесполезным?
        /// </summary>
        public static bool isEmptyCharacter(char c) {
            return c == ' ' || c == '\t' || c == '\r' || c == '\n';
        }

    }
}
