using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mishin870.MHScript.engine.commands;
using System.IO;
using Mishin870.MHScript.engine.documentation;

namespace Mishin870.MHScript.engine.objects {
    /// <summary>
    /// Локальная функция, объявленная в скрипте
    /// </summary>
    public class LocalFunction : IDocumentationProvider {
        /// <summary>
        /// Название функции
        /// </summary>
        public string name;
        /// <summary>
        /// Ссылка на блок кода функции
        /// </summary>
        public Script code;
        /// <summary>
        /// Названия аргументов функции
        /// </summary>
        public List<string> args;

        public LocalFunction() {
        }

        public LocalFunction(Stream stream) {
            name = SerializationHelper.readString(stream);
            code = (Script) SerializationHelper.deSerialize(stream);

            int count = SerializationHelper.readInt(stream);
            args = new List<string>();
            for (int i = 0; i < count; i++) {
                args.Add(SerializationHelper.readString(stream));
            }
        }

        public IDocumentationEntry getDocumentation() {
            return new FunctionEntry(
                "object " + name + "(" + string.Join(", ", args.ToArray()) + ")",
                "Local function (no description)",
                true
            );
        }

        public void serialize(Stream stream) {
            SerializationHelper.writeString(stream, name);
            code.serialize(stream);

            SerializationHelper.writeInt(stream, args.Count);
            foreach (string arg in args) {
                SerializationHelper.writeString(stream, arg);
            }
        }
    }
}
