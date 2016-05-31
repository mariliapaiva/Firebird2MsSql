using System;
using System.Collections.Generic;
using System.Linq;
using CopyDb.Core.Servicos;

namespace CopyDb.Core
{
    public class MapeamentoTabela
    {
        public MapeamentoTabela(Tabela tabelaDaFonte, Tabela tabelaDoDestino)
        {
            Nome = tabelaDaFonte.Nome;

            Colunas = tabelaDaFonte.Colunas.Join(tabelaDoDestino.Colunas, c => c.Nome, c => c.Nome,
                (colunaFonte, colunaDestino) => Tuple.Create(colunaFonte.Nome, colunaDestino.DataType, Conversores.RecuperarConversor(colunaFonte.DataType, colunaDestino.DataType))).ToList();

            Customizada = Colunas.Any(c => c.Item3 != null);
        }

        public string Nome { get; }
        public bool Customizada { get; }

        public IList<Tuple<string, Type, IConversor>> Colunas { get; }

        public override string ToString() => $"{Nome} => Customizada: {Customizada}";
    }
}