using System.Data.Common;

namespace CopyDb.Core.Servicos
{
    public class ExtratorMetadadosTabelaMsSqlServer : ExtratorMetadadosTabelaBase
    {
        public ExtratorMetadadosTabelaMsSqlServer(DbConnection connection)
        {
            Connection = connection;
        }

        protected override string Sql { get; } = @"select name from sys.tables order by name";
    }
}