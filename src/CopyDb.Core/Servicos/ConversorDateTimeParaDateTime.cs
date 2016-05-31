using System;

namespace CopyDb.Core.Servicos
{
    public sealed class ConversorDateTimeParaDateTime : IConversor
    {
        private static readonly Type Type = typeof(DateTime);
        private readonly DateTime _minAllowedDate = new DateTime(2000, 1, 1);

        public Tuple<Type, Type> TuplaConversao => Tuple.Create(Type, Type);

        public object Converter(object source)
        {
            if (!(source is DateTime)) return source;
            var dateTime = (DateTime)source;
            return dateTime.Year < 1900 ? _minAllowedDate : dateTime;
        }
    }
}