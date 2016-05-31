using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;

namespace CopyDb.Core.Servicos
{
    public class Migrador : IMigrador
    {
        private const int TAKE = 10000;
        private readonly IList<Tabela> _tabelasFonte;
        private readonly IList<Tabela> _tabelasDestino;

        public event Action<string> OnMessage;
        public event Action<string> OnError;
        public event Action<int> OnIniciarMigracao;
        public event Action<string, int> OnInicioMigracaoTabela;
        public event Action<string, double, TimeSpan> OnFimMigracaoTabela;
        public event Action<long> OnRegistrosMigrados;

        public Migrador(IExtratorMetadadosTabela source, IExtratorMetadadosTabela dest)
        {
            _tabelasFonte = source.Extrair();
            _tabelasDestino = dest.Extrair();
        }

        public void Migrar(DbConnection conexaoFonte, SqlConnection conexaoDestino)
        {
            Stopwatch stopwatch = null;
            try
            {
                var mapeamentoTabelas = CriaMapeamentoDeTabela();

                var command = conexaoDestino.CreateCommand();

                OnMessage?.Invoke("Desabilitando triggers");
                command.CommandText = "EXEC sp_msforeachtable 'ALTER TABLE ? DISABLE TRIGGER all'";
                command.ExecuteNonQuery();

                OnIniciarMigracao?.Invoke(mapeamentoTabelas.Count);
                var migradas = 0d;

                foreach (var mapeamentoTabela in mapeamentoTabelas)
                {
                    stopwatch = Stopwatch.StartNew();
                    MigraTabela(conexaoFonte, conexaoDestino, mapeamentoTabela);
                    stopwatch.Stop();

                    OnFimMigracaoTabela?.Invoke(mapeamentoTabela.Nome, ++migradas, stopwatch.Elapsed);
                }

                OnMessage?.Invoke("Habilitando triggers");
                command.CommandText = "EXEC sp_msforeachtable 'ALTER TABLE ? ENABLE TRIGGER all'";
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                stopwatch?.Stop();
                OnError?.Invoke(ex.Message);
            }
        }

        private void MigraTabela(DbConnection conexaoFonte, SqlConnection conexaoDestino, MapeamentoTabela mapeamentoTabela)
        {
            var sqlCommand = conexaoFonte.CreateCommand();
            sqlCommand.CommandText = $"SELECT COUNT({mapeamentoTabela.Colunas.First().Item1}) FROM {mapeamentoTabela.Nome}";

            var totalRegistros = (int)sqlCommand.ExecuteScalar();
            OnInicioMigracaoTabela?.Invoke(mapeamentoTabela.Nome, totalRegistros);

            sqlCommand.CommandText = $"SELECT {string.Join(",", mapeamentoTabela.Colunas.Select(c => c.Item1))} FROM {mapeamentoTabela.Nome}";
            var dataReader = sqlCommand.ExecuteReader();

            using (dataReader)
            using (var sqlBulkCopy = new SqlBulkCopy(conexaoDestino))
            {
                mapeamentoTabela.Colunas.Select(c => new SqlBulkCopyColumnMapping(c.Item1, c.Item1))
                    .ToList()
                    .ForEach(sqlBulkCopyColumnMapping => sqlBulkCopy.ColumnMappings.Add(sqlBulkCopyColumnMapping));

                if (mapeamentoTabela.Customizada)
                    CopiarDadosComTratamentoDeBlob(mapeamentoTabela, sqlBulkCopy, dataReader, totalRegistros);
                else
                    CopiarDados(mapeamentoTabela, sqlBulkCopy, dataReader, totalRegistros);
            }
        }

        private List<MapeamentoTabela> CriaMapeamentoDeTabela()
        {
            return _tabelasFonte
                .Join(_tabelasDestino, t => t.Nome, t => t.Nome, (tabelaDaFonte, tabelaDoDestino) => new MapeamentoTabela(tabelaDaFonte, tabelaDoDestino))
                .ToList();
        }

        private void CopiarDados(MapeamentoTabela tabela, SqlBulkCopy sqlBulkCopy, DbDataReader sqlDataReader, long totalRegistros)
        {
            try
            {
                sqlBulkCopy.BulkCopyTimeout = 1800/*30 minutos*/;
                sqlBulkCopy.BatchSize = TAKE;
                sqlBulkCopy.NotifyAfter = TAKE;
                sqlBulkCopy.SqlRowsCopied += (sender, eventArgs) => OnRegistrosMigrados?.Invoke(Math.Min(eventArgs.RowsCopied, totalRegistros));
                sqlBulkCopy.DestinationTableName = tabela.Nome;
                sqlBulkCopy.WriteToServer(sqlDataReader);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Erro de conversão na {tabela} => {ex.Message}");
                Console.ResetColor();
            }
        }

        private void CopiarDadosComTratamentoDeBlob(MapeamentoTabela tabela, SqlBulkCopy sqlBulkCopy, DbDataReader sqlDataReader, long totalRegistros)
        {
            var inserirDados = true;

            var fieldCount = sqlDataReader.FieldCount;
            var inseridos = 0L;
            do
            {
                var contador = 0;
                var datatable = MontarTabelaTemporaria(tabela);
                while (contador < TAKE && (inserirDados = sqlDataReader.Read()))
                {
                    var valores = new object[fieldCount];
                    for (int i = 0; i < fieldCount; i++)
                    {
                        var value = sqlDataReader.GetValue(i);
                        valores[i] = tabela.Colunas[i].Item3 != null ? tabela.Colunas[i].Item3.Converter(value) : value;
                    }
                    datatable.Rows.Add(valores);
                    contador++;
                }
                inseridos += TAKE;
                try
                {
                    if (contador > 0)
                    {
                        sqlBulkCopy.BulkCopyTimeout = 1800/*30 minutos*/;
                        sqlBulkCopy.DestinationTableName = tabela.Nome;
                        sqlBulkCopy.WriteToServer(datatable);
                        OnRegistrosMigrados?.Invoke(Math.Min(inseridos, totalRegistros));
                    }
                }
                catch (Exception ex)
                {
                    OnError?.Invoke(ex.Message);
                }
            } while (inserirDados);
        }

        private static DataTable MontarTabelaTemporaria(MapeamentoTabela mapeamentoTabela)
        {
            var datatable = new DataTable(mapeamentoTabela.Nome);

            foreach (var column in mapeamentoTabela.Colunas)
                datatable.Columns.Add(column.Item1, column.Item2);

            return datatable;
        }
    }
}