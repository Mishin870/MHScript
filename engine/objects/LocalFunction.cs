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

        internal LocalFunction(Stream stream, SerializationInfo info) {
            if (info.optimizeForClient) {
                name = info.localFunctions[SerializationHelper.readInt(stream)];
            } else {
                name = SerializationHelper.readString(stream);
            }
            code = (Script) SerializationHelper.deSerialize(stream, info);

            int count = SerializationHelper.readInt(stream);
            args = new List<string>();
            for (int i = 0; i < count; i++) {
                if (info.optimizeForClient) {
                    args.Add(info.variables[SerializationHelper.readInt(stream)]);
                } else {
                    args.Add(SerializationHelper.readString(stream));
                }
            }
        }

        public IDocumentationEntry getDocumentation() {
            return new FunctionEntry(
                "object " + name + "(" + string.Join(", ", args.ToArray()) + ")",
                "Local function (no description)",
                true
            );
        }

        internal void serialize(Stream stream, SerializationInfo info) {
            if (info.optimizeForClient) {
                int x = info.localFunctions.IndexOf(this.name);
                if (x == -1) {
                    throw new InvalidOperationException("Can't find that local function name in SerializationInfo!");
                }
                SerializationHelper.writeInt(stream, x);
            } else {
                SerializationHelper.writeString(stream, name);
            }

            code.serialize(stream, info);

            SerializationHelper.writeInt(stream, args.Count);
            foreach (string arg in args) {
                if (info.optimizeForClient) {
                    int x = info.variables.IndexOf(arg);
                    if (x == -1) {
                        throw new InvalidOperationException("Can't find that local function argument name in SerializationInfo!");
                    }
                    SerializationHelper.writeInt(stream, x);
                } else {
                    SerializationHelper.writeString(stream, arg);
                }
            }
        }
    }
}
