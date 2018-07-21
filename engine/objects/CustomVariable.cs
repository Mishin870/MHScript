using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Mishin870.MHScript.lexems;

namespace Mishin870.MHScript.engine.objects {
    /// <summary>
    /// Интерфейс пользовательского типа переменной в скрипте.
    /// Определяет ответ объектов этого типа на применение к ним различных операторов языка.
    /// </summary>
    public interface CustomVariable {
        /// <summary>
        /// Вызывается когда переменная с данным типом участвует в логическом выражении
        /// Переменная может стоять как слева, так и справа оператора. Операторы:
        /// равно, не равно, больше, меньше, больше или равно, меньше или равно
        /// (EQUALS, NOT_EQUALS, GREATER, LESSER, GREATER_EQUALS, LESSER_EQUALS)
        /// </summary>
        bool compare(object left, LexemKind operation, object right);
        /// <summary>
        /// Вызывается когда переменная с данным типом участвует в математическом выражении
        /// Только когда эта переменная стоит справа оператора. Левая переменная дана (left). Операторы:
        /// сложение, вычитание, деление, умножение
        /// (PLUS, MINUS, DIVIDE, MULTIPLY)
        /// </summary>
        object math(object left, LexemKind operation);
        /// <summary>
        /// Вызывается при попытке получения значения данной переменной по индексу. Пример: x["abc"], x[10], ...
        /// </summary>
        /// <param name="safe">определяет безопасную индексацию. возвращает defaultValue в случае ошибки</param>
        object indexGet(object index, bool safe, object defaultValue);
        /// <summary>
        /// Вызывается при попытке установки значения данной переменной по индексу. Пример: x[index] = value
        /// </summary>
        void indexSet(object index, object value);
        /// <summary>
        /// Вызывается при использовании одиночного оператора на объекте. Операторы:
        /// отрицание, инкремент, декремент, преинкремент, предекремент
        /// (NOT, INCREMENT, DECREMENT, PREINCREMENT, PREDECREMENT)
        /// </summary>
        object unary(LexemKind operation);
        /// <summary>
        /// Вызывается, когда на основании данной переменной вызывается функция
        /// </summary>
        object executeFunction(string functionName, Engine engine, StringWriter output, object[] args);
        /// <summary>
        /// Вызывается при необходимости сложить этот объект с числом. Либо при вызове функции float
        /// </summary>
        float floatVal();
        /// <summary>
        /// Вызывается при необходимости сложить этот объект со строкой. Либо при вызове функции str
        /// </summary>
        string stringVal();
        /// <summary>
        /// Вызывается функцией var_dump при наличии этой переменной в дереве вывода
        /// </summary>
        /// <param name="stringBuilder">компоновщик строки</param>
        /// <param name="tabLeve">текущий уровень</param>
        /// <param name="tabs">строка, содержащая необходимое количество табов для текущего уровня</param>
        /// <param name="twoTabs">строка, содержащая два таба</param>
        /// <returns></returns>
        string dump(StringBuilder stringBuilder, int tabLevel, string tabs, string twoTabs);
    }
}
