using System;
using System.Data.SqlClient;
using System.Diagnostics;
using CopyDb.Core.Servicos;
using FirebirdSql.Data.FirebirdClient;

namespace CopyDb
{
    internal class Program
    {
        private static int _totalTabelas;
        private static string _tabelaEmMigracao;
        private static int _totalRegistrosNaTabelaAtual;

        private static void Main(string[] args)
        {
            var sourceConString = new FbConnectionStringBuilder
            {
                DataSource = @"localhost",
                Database = @"C:\temp\CARGAS32_BOMFIM.FDB",
                Password = "masterkey",
                UserID = "SYSDBA"
            };

            var destConString = new SqlConnectionStringBuilder
            {
                InitialCatalog = "CARGAS32",
                DataSource = ".",
                IntegratedSecurity = true
            };

            Stopwatch stopwatch;
            using (var conexaoFonte = new FbConnection(sourceConString.ConnectionString))
            using (var conexaoDestino = new SqlConnection(destConString.ConnectionString))
            {
                var extratorMetadadosTabelaMsSqlServer = new ExtratorMetadadosTabelaMsSqlServer(conexaoDestino);
                var extratorMetadadosTabelaFirebird = new ExtratorMetadadosTabelaFirebird(conexaoFonte);

                conexaoFonte.Open();
                conexaoDestino.Open();

                stopwatch = Stopwatch.StartNew();

                var migrador = new Migrador(extratorMetadadosTabelaFirebird, extratorMetadadosTabelaMsSqlServer);
                migrador.Migrar(conexaoFonte, conexaoDestino);

                migrador.OnMessage += Console.WriteLine;
                migrador.OnError += Migrador_OnError;
                migrador.OnIniciarMigracao += Migrador_OnIniciarMigracao;
                migrador.OnInicioMigracaoTabela += Migrador_OnInicioMigracaoTabela;
                migrador.OnFimMigracaoTabela += Migrador_OnFimMigracaoTabela;
                migrador.OnRegistrosMigrados += Migrador_OnRegistrosMigrados;


                stopwatch.Stop();
            }
            Console.WriteLine($"Cópia realizada em {stopwatch.Elapsed}");
            Console.ReadLine();
        }

        private static void Migrador_OnIniciarMigracao(int totalTabelas) => _totalTabelas = totalTabelas;

        private static void Migrador_OnFimMigracaoTabela(string tabela, double tabelasMigradas, TimeSpan tempoGasto)
        {
            WriteLineColored(ConsoleColor.Green, $"Tabela {tabela} migrada em {tempoGasto}");
            WriteLineColored(ConsoleColor.Green, $"Tabelas migradas: {tabelasMigradas:N0}/{_totalTabelas:N0}({tabelasMigradas / _totalTabelas:P})");
        }

        private static void Migrador_OnError(string message)
            => WriteLineColored(ConsoleColor.Red, $"Erro na migração da tabela {_tabelaEmMigracao}: {message}");

        private static void Migrador_OnRegistrosMigrados(long registrosMigrados)
            => WriteLineColored(ConsoleColor.Yellow, $"Copiado {registrosMigrados:N0}/{_totalRegistrosNaTabelaAtual:N0} linhas da tabela {_tabelaEmMigracao}");

        private static void Migrador_OnInicioMigracaoTabela(string tabela, int totalRegistros)
        {
            _totalRegistrosNaTabelaAtual = totalRegistros;
            _tabelaEmMigracao = tabela;
            WriteLineColored(ConsoleColor.White, $"Migrando dados da tabela {_tabelaEmMigracao} com {_totalRegistrosNaTabelaAtual:N0} registros");
        }

        private static void WriteLineColored(ConsoleColor foregroundColor, string formattableString)
        {
            Console.ForegroundColor = foregroundColor;
            Console.WriteLine(formattableString);
            Console.ResetColor();
        }
    }
}


//var tabelasMsSql = extratorMetadadosTabelaMsSqlServer.Extrair();
//var tabelasFb = extratorMetadadosTabelaFirebird.Extrair();
//foreach (var tuple in from tabelaFb in tabelasFb
//                      join tabelaTempMsSql in tabelasMsSql on tabelaFb.Nome equals tabelaTempMsSql.Nome into msSqlTabelas
//                      from tabelaMsSql in msSqlTabelas.DefaultIfEmpty()
//                      select Tuple.Create(tabelaFb, tabelaMsSql))
//{
//    Console.WriteLine($"Tabela no Fb: {tuple.Item1.Nome} e Tabela no MsSql: {tuple.Item2?.Nome ?? "Não existe"}");

//    foreach (var tuple1 in from colunaFb in tuple.Item1.Colunas
//                           join colunaTempMsSql in tuple.Item2?.Colunas on colunaFb.Nome equals colunaTempMsSql.Nome into msSqlColunas
//                           from colunaMsSql in msSqlColunas.DefaultIfEmpty()
//                           select Tuple.Create(colunaFb, colunaMsSql))
//    {
//        Console.WriteLine($"\tFb   : {tuple1.Item1}");
//        Console.WriteLine($"\tMsSql: {tuple1.Item2?.ToString() ?? "Não existe"}");
//        Console.WriteLine("\t=============");
//    }
//    Console.ReadLine();
//}