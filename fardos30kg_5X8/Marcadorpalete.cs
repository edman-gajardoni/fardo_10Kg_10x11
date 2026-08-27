using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace fardos30kg_5X8
    {
    /// <summary>
    /// Junta os paineis de selecao de UM palete (impares + pares) e cuida do
    /// grid: pinta de amarelo a UNIAO das duas selecoes e diz quais chaves
    /// serao alteradas.
    ///
    /// Existe por um motivo concreto: se cada PainelSelecao pintasse o grid
    /// por conta propria, o painel das pares apagaria o amarelo do painel das
    /// impares (ele limparia toda linha que o SEU filtro nao pega). A pintura
    /// precisa de um lugar so, que enxergue os dois filtros juntos.
    /// </summary>
    public class MarcadorPalete
        {
        /// <summary>Fundo das linhas marcadas para alteracao.</summary>
        public static readonly Color CorMarcada = Color.FromArgb(255, 240, 130);

        /// <summary>Fundo da linha marcada que tambem esta sob o cursor.</summary>
        public static readonly Color CorMarcadaSelecionada = Color.FromArgb(235, 195, 40);

        private readonly DataGridView _grid;
        private readonly List<PainelSelecao> _paineis = new List<PainelSelecao>();

        /// <summary>Disparado quando qualquer painel muda (para atualizar botao/label).</summary>
        public event EventHandler SelecaoAlterada;

        public MarcadorPalete(DataGridView grid, params PainelSelecao[] paineis)
            {
            if (grid == null) throw new ArgumentNullException("grid");

            _grid = grid;

            if (paineis != null)
                {
                foreach (PainelSelecao p in paineis)
                    {
                    if (p == null) continue;
                    _paineis.Add(p);
                    p.SelecaoAlterada += delegate { Repintar(); };
                    }
                }

            // Repopular o grid recria as linhas: sem isto o amarelo sumiria
            // depois de cada Aplicar.
            _grid.DataBindingComplete += delegate { Repintar(); };
            }

        /// <summary>
        /// Chaves que serao alteradas: uniao dos filtros de todos os paineis,
        /// sem repetir, na ordem em que aparecem no grid.
        /// </summary>
        public List<string> ChavesSelecionadas()
            {
            List<string> chaves = new List<string>();

            List<FiltroSelecao> filtros = FiltrosAtivos();
            if (filtros.Count == 0) return chaves;

            foreach (DataGridViewRow linha in _grid.Rows)
                {
                LinhaPosicao lp = linha.DataBoundItem as LinhaPosicao;
                if (lp == null) continue;

                if (Marcada(lp, filtros)) chaves.Add(lp.Chave);
                }

            return chaves;
            }

        /// <summary>Quantas linhas o Aplicar vai pegar.</summary>
        public int Quantidade
            {
            get { return ChavesSelecionadas().Count; }
            }

        /// <summary>Descricao curta da selecao, para status bar / tooltip.</summary>
        public string Descricao()
            {
            List<FiltroSelecao> filtros = FiltrosAtivos();
            if (filtros.Count == 0) return "nenhuma posicao selecionada";

            string texto = string.Empty;
            foreach (FiltroSelecao f in filtros)
                {
                if (texto.Length > 0) texto += "  +  ";
                texto += f.ToString();
                }

            return texto;
            }

        /// <summary>
        /// Pinta o grid. Pode ser chamado a qualquer momento — inclusive
        /// logo depois de GridPosicoes.Popular.
        /// </summary>
        public void Repintar()
            {
            if (_grid.Rows.Count > 0)
                {
                List<FiltroSelecao> filtros = FiltrosAtivos();

                foreach (DataGridViewRow linha in _grid.Rows)
                    {
                    LinhaPosicao lp = linha.DataBoundItem as LinhaPosicao;
                    bool marcada = (lp != null) && Marcada(lp, filtros);

                    if (marcada)
                        {
                        linha.DefaultCellStyle.BackColor = CorMarcada;
                        // Sem isto, clicar numa linha amarela pinta ela de azul
                        // e a marca some justo na linha que o operador olha.
                        linha.DefaultCellStyle.SelectionBackColor = CorMarcadaSelecionada;
                        linha.DefaultCellStyle.SelectionForeColor = Color.Black;
                        }
                    else
                        {
                        // Color.Empty devolve a linha a heranca do grid — e so
                        // assim o zebrado das linhas alternadas volta.
                        linha.DefaultCellStyle.BackColor = Color.Empty;
                        linha.DefaultCellStyle.SelectionBackColor = Color.Empty;
                        linha.DefaultCellStyle.SelectionForeColor = Color.Empty;
                        }
                    }
                }

            if (SelecaoAlterada != null) SelecaoAlterada(this, EventArgs.Empty);
            }

        /// <summary>Desmarca todos os paineis deste palete.</summary>
        public void Limpar()
            {
            foreach (PainelSelecao p in _paineis) p.Limpar();
            Repintar();
            }

        // ------------------------------------------------------------------

        private List<FiltroSelecao> FiltrosAtivos()
            {
            List<FiltroSelecao> ativos = new List<FiltroSelecao>();

            foreach (PainelSelecao p in _paineis)
                {
                FiltroSelecao f = p.Filtro;
                if (!f.Vazio) ativos.Add(f);
                }

            return ativos;
            }

        private static bool Marcada(LinhaPosicao lp, List<FiltroSelecao> filtros)
            {
            if (filtros.Count == 0) return false;

            ChavePosicao ch;
            if (!ChavePosicao.TryParse(lp.Chave, out ch)) return false;

            foreach (FiltroSelecao f in filtros)
                {
                if (f.Corresponde(ch)) return true;    // uniao: basta um pegar
                }

            return false;
            }
        }
    }