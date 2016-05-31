using System.Data.Common;

namespace CopyDb.Core.Servicos
{
    public class ExtratorMetadadosTabelaFirebird : ExtratorMetadadosTabelaBase
    {
        public ExtratorMetadadosTabelaFirebird(DbConnection connection)
        {
            Connection = connection;
        }

        protected override string Sql { get; } = @"SELECT tables.RDB$RELATION_NAME
            FROM RDB$RELATIONS as tables
            WHERE tables.RDB$SYSTEM_FLAG = 0 AND tables.RDB$VIEW_BLR IS NULL
            ORDER BY tables.RDB$RELATION_NAME";
    }
}