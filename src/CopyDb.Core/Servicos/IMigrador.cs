using System.Data.Common;
using System.Data.SqlClient;

namespace CopyDb.Core.Servicos
{
    public interface IMigrador
    {
        void Migrar(DbConnection conexaoFonte, SqlConnection conexaoDestino);
        void ReorganizarIndice(SqlConnection conexaoDestino);
    }
}