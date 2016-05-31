using System;

namespace CopyDb.Core.Servicos
{
    public interface IConversor
    {
        Tuple<Type, Type> TuplaConversao { get; }
        object Converter(object source);
    }
}