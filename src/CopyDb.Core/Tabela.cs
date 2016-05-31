using System.Collections.Generic;

namespace CopyDb.Core
{
    public class Tabela
    {
        private readonly List<Coluna> _colunas;

        public Tabela(string nome)
        {
            Nome = nome;
            _colunas = new List<Coluna>();
        }

        public string Nome { get; }

        public ICollection<Coluna> Colunas => _colunas;

        public void AdicionarColunas(IEnumerable<Coluna> colunas) => _colunas.AddRange(colunas);

        public override string ToString() => Nome;
    }
}
