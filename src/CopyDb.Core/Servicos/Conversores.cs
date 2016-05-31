using System;
using System.Collections.Generic;

namespace CopyDb.Core.Servicos
{
    public static class Conversores
    {
        private static readonly Dictionary<Tuple<Type, Type>, IConversor> conversores = new Dictionary<Tuple<Type, Type>, IConversor>();

        static Conversores()
        {
            var conversor = new ConversorArrayByteParaString();
            conversores.Add(conversor.TuplaConversao, conversor);
            var conversor1 = new ConversorDateTimeParaDateTime();
            conversores.Add(conversor1.TuplaConversao, conversor1);
        }

        public static IConversor RecuperarConversor(Type source, Type to)
        {
            IConversor conversor;
            conversores.TryGetValue(Tuple.Create(source, to), out conversor);
            return conversor;
        }
    }
}