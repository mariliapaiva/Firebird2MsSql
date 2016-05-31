using System;
using System.Text;

namespace CopyDb.Core.Servicos
{
    public sealed class ConversorArrayByteParaString : IConversor
    {
        public Tuple<Type, Type> TuplaConversao => Tuple.Create(typeof(byte[]), typeof(string));

        public object Converter(object byteArray)
        {
            var bytes = byteArray as byte[];
            return bytes != null ? Encoding.Default.GetString(bytes) : default(string);
        }
    }
}