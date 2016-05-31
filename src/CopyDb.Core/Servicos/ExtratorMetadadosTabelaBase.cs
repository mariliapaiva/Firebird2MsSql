using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace CopyDb.Core.Servicos
{
    public abstract class ExtratorMetadadosTabelaBase : IExtratorMetadadosTabela
    {
        protected abstract string Sql { get; }
        protected DbConnection Connection { get; set; }

        public IList<Tabela> Extrair()
        {
            var tabelas = new List<Tabela>();

            using (var command = Connection.CreateCommand())
            {
                command.CommandText = Sql;

                using (var dataReader = command.ExecuteReader())
                    while (dataReader.Read())
                        tabelas.Add(new Tabela(dataReader.GetString(0).Trim()));
            }

            tabelas.ForEach(tabela =>
            {
                using (var command = Connection.CreateCommand())
                {
                    command.CommandText = $"SELECT * FROM {tabela.Nome} WHERE 1=0";
                    using (var dataReader = command.ExecuteReader(CommandBehavior.SchemaOnly))
                        tabela.AdicionarColunas(Enumerable.Range(0, dataReader.FieldCount).Select(i => DataReaderParaColuna(dataReader, i)));
                }
            });

            return tabelas;

        }

        protected static Coluna DataReaderParaColuna(DbDataReader dataReader, int i)
            => new Coluna(dataReader.GetName(i), i, dataReader.GetFieldType(i), dataReader.GetDataTypeName(i));
    }
}