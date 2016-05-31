using System.Collections.Generic;

namespace CopyDb.Core.Servicos
{
    public interface IExtratorMetadadosTabela
    {
        IList<Tabela> Extrair();
    }
}