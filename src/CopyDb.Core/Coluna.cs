using System;

namespace CopyDb.Core
{
    public class Coluna
    {
        public Coluna(string nome, int posicao, Type dataType, string dataTypeName)
        {
            Nome = nome;
            Posicao = posicao;
            DataType = dataType;
            DataTypeName = dataTypeName;
        }

        public string Nome { get; }
        public int Posicao { get; }
        public Type DataType { get; }
        public string DataTypeName { get; }

        public override string ToString() => $"{Nome,20} - {Posicao,2} - {DataTypeName,10} - {DataType.Name,10}";
    }
}