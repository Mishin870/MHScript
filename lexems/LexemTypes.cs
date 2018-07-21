using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Mishin870.MHScript.lexems {

    /// <summary>
    /// Представление любой лексемы в движке
    /// </summary>
    /// <typeparam name="T">с помощью чего она представляется</typeparam>
    class LexemDefinition<T> {
        public LexemKind kind {
            get;
            protected set;
        }
        public T representation {
            get;
            protected set;
        }
    }

    /// <summary>
    /// Статичная лексема.
    /// Все ключевые слова, математические/логические операции и т.д.
    /// Представляется статичной строкой.
    /// </summary>
    class StaticLexemDefinition : LexemDefinition<string> {
        /// <summary>
        /// true, если лексема представляет ключевое слово
        /// также это может быть математический или логический оператор
        /// </summary>
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

    /// <summary>
    /// Динамическая лексема.
    /// Числа, строки, идентификаторы переменных и функций. Всё, что динамично.
    /// Представляется регулярным выражением.
    /// </summary>
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
}
