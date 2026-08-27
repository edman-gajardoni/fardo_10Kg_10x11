using System;
using System.Collections.Generic;

namespace fardos30kg_5X8
    {
    /// <summary>Quais camadas os checkboxes de paridade liberam.</summary>
    public enum ParidadeCamada
        {
        /// <summary>Nenhum dos dois marcado: nao pega camada nenhuma.</summary>
        Nenhuma = 0,
        Impares = 1,
        Pares = 2,
        /// <summary>Os dois marcados: todas as camadas.</summary>
        Ambas = 3
        }

    /// <summary>
    /// Regra de selecao montada a partir da UI. Nao conhece Windows Forms —
    /// da para testar no console.
    ///
    /// Ordem de decisao:
    ///   1) Palete: 0 = todos, senao so o palete indicado.
    ///   2) Posicao: tem que estar na lista de posicoes marcadas (1..7).
    ///   3) Camada:  se CamadaEspecifica > 0, so aquela camada (a paridade e
    ///               ignorada, e a UI ja desmarca os checkboxes Par/Impar).
    ///               Senao, vale a paridade.
    /// </summary>
    public class FiltroSelecao
        {
        private readonly HashSet<int> _posicoes = new HashSet<int>();

        public FiltroSelecao()
            {
            Paridade = ParidadeCamada.Nenhuma;
            CamadaEspecifica = 0;
            Palete = 0;
            }

        /// <summary>Posicoes marcadas (1..7).</summary>
        public HashSet<int> Posicoes { get { return _posicoes; } }

        public ParidadeCamada Paridade { get; set; }

        /// <summary>Camada unica selecionada no combo. 0 = usar a paridade.</summary>
        public int CamadaEspecifica { get; set; }

        /// <summary>Palete alvo. 0 = todos.</summary>
        public int Palete { get; set; }

        /// <summary>
        /// true se o filtro nao pode selecionar nada (evita "Aplicar" silencioso).
        /// </summary>
        public bool Vazio
            {
            get
                {
                if (_posicoes.Count == 0) return true;
                if (CamadaEspecifica > 0) return false;
                return Paridade == ParidadeCamada.Nenhuma;
                }
            }

        public bool Corresponde(ChavePosicao ch)
            {
            if (Palete > 0 && ch.Palete != Palete) return false;

            if (!_posicoes.Contains(ch.Posicao)) return false;

            if (CamadaEspecifica > 0)
                return ch.Camada == CamadaEspecifica;

            switch (Paridade)
                {
                case ParidadeCamada.Ambas: return true;
                case ParidadeCamada.Pares: return ch.CamadaPar;
                case ParidadeCamada.Impares: return !ch.CamadaPar;
                default: return false;          // Nenhuma
                }
            }

        /// <summary>Descricao curta para mostrar na tela.</summary>
        public override string ToString()
            {
            if (Vazio) return "nenhuma posicao selecionada";

            List<int> ordenadas = new List<int>(_posicoes);
            ordenadas.Sort();

            string camadas;
            if (CamadaEspecifica > 0) camadas = "camada " + CamadaEspecifica;
            else if (Paridade == ParidadeCamada.Ambas) camadas = "todas as camadas";
            else if (Paridade == ParidadeCamada.Pares) camadas = "camadas pares";
            else camadas = "camadas impares";

            string palete = (Palete > 0) ? "palete " + Palete : "ambos os paletes";

            return "pos " + string.Join(",", ordenadas.ConvertAll(
                delegate(int i) { return i.ToString(); }).ToArray())
                + " / " + camadas + " / " + palete;
            }
        }
    }