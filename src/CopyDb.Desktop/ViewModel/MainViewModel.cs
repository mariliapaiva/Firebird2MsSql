using System;
using System.Configuration;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using CopyDb.Core.Servicos;
using CopyDb.Desktop.Services;
using FirebirdSql.Data.FirebirdClient;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using PropertyChanged;
using static System.String;

namespace CopyDb.Desktop.ViewModel
{
    [ImplementPropertyChanged]
    public class MainViewModel : ViewModelBase
    {
        private readonly IDialogService _dialogService;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private static readonly Configuration Configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        private static readonly KeyValueConfigurationCollection Settings = Configuration.AppSettings.Settings;
        private const int AUTENTICACAO_WINDOWS = 0;
        private const int AUTENTICACA_USUARIO_SENHA = 1;
        private Migrador migrador;

        public MainViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService;

            OnTestarConexaoFirebird = new RelayCommand(TestarConexaoFirebird);
            OnTestarConexaoMsSql = new RelayCommand(TestarConexaoMsSql);
            OnIniciarMigracao = new RelayCommand(IniciarMigracao);
            OnReorganizarIndices = new RelayCommand(ReorganizarIndices);
            _cancellationTokenSource = new CancellationTokenSource();
            CarregaConfiguracoes();
        }

        public string DataSourceFirebird { get; set; }
        public string ArquivoFirebird { get; set; }
        public string UsuarioFirebird { get; set; }
        public string SenhaFirebird { get; set; }

        public string NomeBancoMsSql { get; set; }
        public string EnderecoMsSql { get; set; }
        public int AutenticacaoMsSql { get; set; }
        public string UsuarioMsSql { get; set; }
        public string SenhaMsSql { get; set; }

        public string TabelaEmMigracao { get; set; } = "";
        public long TotalRegistrosNaTabelaAtual { get; set; } = 1;
        public long RegistrosMigradosNaTabelaAtual { get; set; }
        public long TotalTabelas { get; set; } = 1;
        public long TabelasMigradas { get; set; }

        public bool MigracaoNaoIniciada { get; set; } = true;

        public RelayCommand OnTestarConexaoFirebird { get; set; }
        public RelayCommand OnTestarConexaoMsSql { get; set; }
        public RelayCommand OnIniciarMigracao { get; set; }
        public RelayCommand OnReorganizarIndices { get; set; }

        public bool TestarConexaoFirebirdHabilitado
            => MigracaoNaoIniciada && !IsNullOrWhiteSpace(DataSourceFirebird) && !IsNullOrWhiteSpace(ArquivoFirebird) &&
               !IsNullOrWhiteSpace(UsuarioFirebird) && !IsNullOrWhiteSpace(SenhaFirebird);

        public bool TestarConexaoMsSqlHabilitado
            => MigracaoNaoIniciada && !IsNullOrWhiteSpace(NomeBancoMsSql) && !IsNullOrWhiteSpace(EnderecoMsSql) &&
               (AutenticacaoMsSql == AUTENTICACAO_WINDOWS ||
                (AutenticacaoMsSql == AUTENTICACA_USUARIO_SENHA && !IsNullOrWhiteSpace(UsuarioFirebird) &&
                 !IsNullOrWhiteSpace(SenhaFirebird)));

        public bool IniciarMigracaoHabilitado => MigracaoNaoIniciada && ConexaoFirebirdSucesso && ConexaoMsSqlSucesso;

        public bool ReorganizarIndicesHabilitado { get; set; } = false;
        public bool ConexaoFirebirdSucesso { get; set; }
        public bool ConexaoMsSqlSucesso { get; set; }

        public bool UsuarioESenhaMsSqlHabilitado => AutenticacaoMsSql == AUTENTICACA_USUARIO_SENHA;

        #region ConnectionStringFonte
        public string ConnectionStringFonte => new FbConnectionStringBuilder
        {
            DataSource = DataSourceFirebird,
            Database = ArquivoFirebird,
            Password = SenhaFirebird,
            UserID = UsuarioFirebird,
            ConnectionTimeout = 10
        }.ConnectionString;

        #endregion

        #region ConnectionStringDestino
        public string ConnectionStringDestino => new SqlConnectionStringBuilder
        {
            InitialCatalog = NomeBancoMsSql,
            DataSource = EnderecoMsSql,
            IntegratedSecurity = AutenticacaoMsSql == 0,
            UserID = UsuarioMsSql ?? "",
            Password = SenhaMsSql ?? "",
            ConnectTimeout = 10
        }.ConnectionString;

        #endregion

        private void IniciarMigracao()
        {
            Task.Factory.StartNew(ExecutaMigracao, TaskCreationOptions.LongRunning);
        }

        private void ExecutaMigracao()
        {
            ReorganizarIndicesHabilitado = MigracaoNaoIniciada = false;
            LogaMensagem?.Invoke("Iniciando migração");
            Stopwatch stopwatch;
            using (var conexaoFonte = new FbConnection(ConnectionStringFonte))
            using (var conexaoDestino = new SqlConnection(ConnectionStringDestino))
            {
                conexaoFonte.Open();
                conexaoDestino.Open();

                migrador = new Migrador(new ExtratorMetadadosTabelaFirebird(conexaoFonte), new ExtratorMetadadosTabelaMsSqlServer(conexaoDestino));

                migrador.OnMessage += LogaMensagem;
                migrador.OnError += message => LogaMensagem?.Invoke($"Erro na migração da tabela {TabelaEmMigracao}: {message}");
                migrador.OnIniciarMigracao += totalTabelas => TotalTabelas = totalTabelas;
                migrador.OnInicioMigracaoTabela += Migrador_OnInicioMigracaoTabela;
                migrador.OnFimMigracaoTabela += Migrador_OnFimMigracaoTabela;
                migrador.OnRegistrosMigrados += Migrador_OnRegistrosMigrados;

                stopwatch = Stopwatch.StartNew();
                Task.Factory.StartNew(() => AtualizaTempoDecorrido(stopwatch), _cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current);
                migrador.Migrar(conexaoFonte, conexaoDestino);
                stopwatch.Stop();
            }
            LogaMensagem?.Invoke($"Migração realizada em {stopwatch.Elapsed}");
            _cancellationTokenSource.Cancel();
            ReorganizarIndicesHabilitado = true;
        }

        private void AtualizaTempoDecorrido(Stopwatch stopwatch)
        {
            while (true)
            {
                TempoDecorrido = stopwatch.Elapsed;
                Thread.Sleep(1000);
            }
        }

        public TimeSpan TempoDecorrido { get; set; } = TimeSpan.Zero;

        private void Migrador_OnFimMigracaoTabela(string tabela, double tabelasMigradas, TimeSpan tempoGasto)
        {
            TabelasMigradas = (long)tabelasMigradas;
            LogaMensagem?.Invoke($"Tabela {tabela} migrada em {tempoGasto}");
            LogaMensagem?.Invoke($"Tabelas migradas: {tabelasMigradas:N0}/{TotalTabelas:N0}({tabelasMigradas / TotalTabelas:P})");
            LogaMensagem?.Invoke("--------------------------------------------------------------------------------------------------------------------");
        }

        private void Migrador_OnRegistrosMigrados(long registrosMigrados)
        {
            RegistrosMigradosNaTabelaAtual = registrosMigrados;
            LogaMensagem?.Invoke($"Migrado {registrosMigrados:N0}/{TotalRegistrosNaTabelaAtual:N0} linhas da tabela {TabelaEmMigracao}");
        }

        private void Migrador_OnInicioMigracaoTabela(string tabela, int totalRegistros)
        {
            TotalRegistrosNaTabelaAtual = totalRegistros;
            RegistrosMigradosNaTabelaAtual = 0;
            TabelaEmMigracao = tabela;
            LogaMensagem?.Invoke($"Migrando dados da tabela {TabelaEmMigracao} com {TotalRegistrosNaTabelaAtual:N0} registros");
        }

        public event Action<string> LogaMensagem;

        private void ReorganizarIndices()
        {
            using (var conexaoDestino = new SqlConnection(ConnectionStringDestino))
            {
                conexaoDestino.Open();
                migrador.ReorganizarIndice(conexaoDestino);
            }
        }

        private void TestarConexaoFirebird()
        {
            using (var connection = new FbConnection(ConnectionStringFonte))
            {
                ConexaoFirebirdSucesso = TestaConexao(connection, "Firebird");
                if (ConexaoFirebirdSucesso)
                {
                    AtualizarConfiguracao(nameof(DataSourceFirebird), DataSourceFirebird);
                    AtualizarConfiguracao(nameof(ArquivoFirebird), ArquivoFirebird);
                    AtualizarConfiguracao(nameof(UsuarioFirebird), UsuarioFirebird);
                    AtualizarConfiguracao(nameof(SenhaFirebird), SenhaFirebird);

                    Configuration.Save();
                }
            }
        }

        private void TestarConexaoMsSql()
        {
            using (var connection = new SqlConnection(ConnectionStringDestino))
            {
                ConexaoMsSqlSucesso = TestaConexao(connection, "MS SQL Server");

                if (ConexaoMsSqlSucesso)
                {
                    AtualizarConfiguracao(nameof(NomeBancoMsSql), NomeBancoMsSql);
                    AtualizarConfiguracao(nameof(EnderecoMsSql), EnderecoMsSql);
                    AtualizarConfiguracao(nameof(AutenticacaoMsSql), AutenticacaoMsSql.ToString());
                    AtualizarConfiguracao(nameof(UsuarioMsSql), UsuarioMsSql);
                    AtualizarConfiguracao(nameof(SenhaMsSql), SenhaMsSql);

                    Configuration.Save();
                }
            }
        }

        private bool TestaConexao(DbConnection connection, string db)
        {
            try
            {
                connection.Open();
                _dialogService.ShowMessage($"Conexão com {db} realizada com sucesso.");
                return true;
            }
            catch
            {
                _dialogService.ShowMessage($"Não foi possível conectar com o {db}.");
            }
            return false;
        }

        private void CarregaConfiguracoes()
        {
            DataSourceFirebird = Settings[nameof(DataSourceFirebird)]?.Value;
            ArquivoFirebird = Settings[nameof(ArquivoFirebird)]?.Value;
            UsuarioFirebird = Settings[nameof(UsuarioFirebird)]?.Value;
            SenhaFirebird = Settings[nameof(SenhaFirebird)]?.Value;

            NomeBancoMsSql = Settings[nameof(NomeBancoMsSql)]?.Value;
            EnderecoMsSql = Settings[nameof(EnderecoMsSql)]?.Value;
            AutenticacaoMsSql = int.Parse(Settings[nameof(AutenticacaoMsSql)]?.Value ?? "0");
            UsuarioMsSql = Settings[nameof(UsuarioMsSql)]?.Value;
            SenhaMsSql = Settings[nameof(SenhaMsSql)]?.Value;
        }

        private static void AtualizarConfiguracao(string key, string value)
        {
            Settings.Remove(key);
            Settings.Add(key, value);
        }
    }
}