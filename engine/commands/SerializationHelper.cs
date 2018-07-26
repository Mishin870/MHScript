using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Mishin870.MHScript.engine.commands {
    internal static class SerializationHelper {
        internal static Dictionary<byte, Type> types = new Dictionary<byte, Type>();
        internal static Dictionary<Type, byte> ids = new Dictionary<Type, byte>();
        static SerializationHelper() {
            types.Add(0, typeof(CommandLogicCompound));
            types.Add(1, typeof(CommandLogic));
            types.Add(2, typeof(CommandReturn));
            types.Add(3, typeof(CommandIf));
            types.Add(4, typeof(CommandElse));
            types.Add(5, typeof(CommandElseIf));
            types.Add(6, typeof(CommandFor));
            types.Add(7, typeof(CommandWhile));
            types.Add(8, typeof(Script));
            types.Add(9, typeof(CommandIndex));
            types.Add(10, typeof(CommandEmpty));
            types.Add(11, typeof(CommandAssign));
            types.Add(12, typeof(CommandAssignIndex));
            types.Add(13, typeof(CommandUnary));
            types.Add(14, typeof(CommandMath));
            types.Add(15, typeof(CommandNumeric));
            types.Add(16, typeof(CommandString));
            types.Add(17, typeof(CommandStringVariabled));
            types.Add(18, typeof(CommandBool));
            types.Add(19, typeof(CommandVariable));
            types.Add(20, typeof(CommandDotVariable));
            types.Add(21, typeof(CommandGlobalFunction));
            types.Add(22, typeof(CommandDotFunction));

            foreach (KeyValuePair<byte, Type> entry in types) {
                ids.Add(entry.Value, entry.Key);
            }
        }

        internal static ICommand deSerialize(Stream stream) {
            int typeId = stream.ReadByte();
            if (typeId == -1) {
                throw new Exception("Ошибка чтения скрипта: конец файла!");
            }
            Type type = types[(byte) typeId];
            return (ICommand) Activator.CreateInstance(type, stream);
        }

        internal static void serializeBlock(Stream stream, SerializationInfo info, List<ICommand> block) {
            writeInt(stream, block.Count);
            foreach (ICommand command in block) {
                command.serialize(stream, info);
            }
        }

        internal static void deserializeBlock(Stream stream, List<ICommand> block) {
            int count = readInt(stream);
            block.Clear();
            for (int i = 0; i < count; i++) {
                block.Add(deSerialize(stream));
            }
        }

        internal static void writeInt(Stream stream, int value) {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian) {
                Array.Reverse(bytes);
            }
            stream.Write(bytes, 0, 4);
        }

        internal static int readInt(Stream stream) {
            byte[] bytes = new byte[4];
            stream.Read(bytes, 0, 4);
            if (BitConverter.IsLittleEndian) {
                Array.Reverse(bytes);
            }
            return BitConverter.ToInt32(bytes, 0);
        }

        internal static void writeString(Stream stream, string value) {
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            writeInt(stream, bytes.Length);
            stream.Write(bytes, 0, bytes.Length);
        }

        internal static string readString(Stream stream) {
            int count = readInt(stream);
            byte[] bytes = new byte[count];
            stream.Read(bytes, 0, count);
            return Encoding.UTF8.GetString(bytes);
        }
    }

    /// <summary>
    /// Вспомогательный класс, хранящий всю необходимую информацию, передаваемую
    /// композицией от верхов к низам. Например, маппинг имён функций.
    /// </summary>
    internal class SerializationInfo {
        /// <summary>
        /// Имена глобальных функций. Вызовы всех функций будут заменены на число-индекс в этом массиве.
        /// </summary>
        internal List<string> globalFunctions = new List<string>();

        /// <summary>
        /// Имена локальных функций. Вызовы и сами функции в объявлении будут заменены на число-индекс в этом массиве.
        /// </summary>
        internal List<string> localFunctions = new List<string>();

        /// <summary>
        /// Все вышеперечисленные правила будут работать только при установке этого флага в true
        /// </summary>
        internal bool optimizeForClient;
    }
}
