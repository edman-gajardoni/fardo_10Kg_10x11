using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace fardos30kg_5X8
    {
    /// <summary>
    /// Traduz UM groupbox de selecao (os 7 checkboxes de posicao + "Todas",
    /// o checkbox "Todas Pares/Impares" e o combo de camada) em um
    /// FiltroSelecao. Nao conhece o grid — quem pinta e o MarcadorPalete.
    ///
    /// Uma instancia por groupbox:
    ///   pal1/impar -> gbxPal1CamImpar
    ///   pal1/par   -> gbxPal1CamPar
    ///   e o mesmo par de instancias para o palete 2, quando a aba existir.
    ///
    /// Os eventos sao ligados aqui no construtor — nao precisa mexer no Designer.
    /// </summary>
    public class PainelSelecao
        {
        /// <summary>
        /// Aceita SO "pos" seguido de numero, no fim do texto: "pos3", "Pos 3".
        /// Isto e proposital. Sem a exigencia do prefixo, um checkbox chamado
        /// "checkBox1" seria lido como "posicao 1" — foi exatamente o que
        /// aconteceu com o "Todas" do groupbox de camadas pares.
        /// </summary>
        private static readonly Regex _padraoPosicao = new Regex(
            @"pos\s*(?<n>\d+)\s*$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private readonly int _palete;
        private readonly ParidadeCamada _paridade;
        private readonly CheckBox _chkTodasCamadas;
        private readonly ComboBox _cmbCamada;
        private readonly CheckBox _chkTodasPosicoes;
        private readonly List<CheckBox> _chkPosicoes = new List<CheckBox>();

        private bool _atualizandoUi;

        /// <summary>Disparado sempre que a selecao deste painel muda.</summary>
        public event EventHandler SelecaoAlterada;

        /// <param name="palete">1 = esquerdo, 2 = direito.</param>
        /// <param name="paridade">Que camadas este groupbox controla.</param>
        /// <param name="chkTodasCamadas">"Todas Impares" / "Todas Pares". Pode ser null.</param>
        /// <param name="cmbCamada">Combo de camada especifica. Pode ser null.</param>
        /// <param name="chkTodasPosicoes">Checkbox "Todas" das posicoes. Pode ser null.</param>
        /// <param name="checkboxesPosicao">Os 7 checkboxes de posicao (Tag = "pos1".."pos7").</param>
        public PainelSelecao(
            int palete,
            ParidadeCamada paridade,
            CheckBox chkTodasCamadas,
            ComboBox cmbCamada,
            CheckBox chkTodasPosicoes,
            params CheckBox[] checkboxesPosicao)
            {
            _palete = palete;
            _paridade = paridade;
            _chkTodasCamadas = chkTodasCamadas;
            _cmbCamada = cmbCamada;
            _chkTodasPosicoes = chkTodasPosicoes;

            if (checkboxesPosicao != null)
                {
                foreach (CheckBox chk in checkboxesPosicao)
                    {
                    if (chk == null) continue;

                    int numero = NumeroDaPosicao(chk);
                    if (numero <= 0)
                        {
                        throw new ArgumentException(
                            "O checkbox \"" + chk.Name + "\" nao tem numero de posicao. " +
                            "Defina o Tag como \"pos1\"..\"pos7\" (ou renomeie para ...Pos1..Pos7).",
                            "checkboxesPosicao");
                        }

                    _chkPosicoes.Add(chk);
                    chk.CheckedChanged += Posicao_Changed;

                    }
                }

            if (_chkTodasPosicoes != null)
                _chkTodasPosicoes.CheckedChanged += TodasPosicoes_Changed;

            if (_chkTodasCamadas != null)
                _chkTodasCamadas.CheckedChanged += TodasCamadas_Changed;

            if (_cmbCamada != null)
                {
                // DropDownList impede o operador digitar uma camada inexistente.
                _cmbCamada.DropDownStyle = ComboBoxStyle.DropDownList;
                _cmbCamada.SelectedIndexChanged += CamadaEspecifica_Changed;
                }
            }

        // ------------------------------------------------------------------
        //  Filtro
        // ------------------------------------------------------------------

        /// <summary>Monta o filtro a partir do estado atual dos controles.</summary>
        public FiltroSelecao Filtro
            {
            get
                {
                FiltroSelecao f = new FiltroSelecao();
                f.Palete = _palete;

                foreach (CheckBox chk in _chkPosicoes)
                    {
                    if (chk.Checked) f.Posicoes.Add(NumeroDaPosicao(chk));
                    }

                f.CamadaEspecifica = CamadaEscolhida();

                // Camada especifica manda; senao vale a paridade do painel,
                // e so quando "Todas Pares/Impares" estiver marcado.
                f.Paridade = (_chkTodasCamadas != null && _chkTodasCamadas.Checked)
                    ? _paridade
                    : ParidadeCamada.Nenhuma;

                return f;
                }
            }

        /// <summary>
        /// Numero da posicao, lido do Tag ("pos3" -> 3) e, se o Tag nao
        /// servir, do fim do Name ("chkEsqImparPos3" -> 3).
        /// Devolve 0 quando nao encontra — e ai o checkbox e recusado.
        /// </summary>
        private static int NumeroDaPosicao(CheckBox chk)
            {
            // Tag ("esq_par_pos3") e, se nao servir, o fim do Name
            // ("chkEsqParPos3"). A APARENCIA nao e mexida aqui: ela vem
            // inteira do Designer.
            int n = ExtrairPosicao(Convert.ToString(chk.Tag));
            return (n > 0) ? n : ExtrairPosicao(chk.Name);
            }

        private static int ExtrairPosicao(string texto)
            {
            if (string.IsNullOrEmpty(texto)) return 0;

            Match m = _padraoPosicao.Match(texto);
            if (!m.Success) return 0;

            int valor;
            return int.TryParse(m.Groups["n"].Value, NumberStyles.None,
                CultureInfo.InvariantCulture, out valor) ? valor : 0;
            }

        private int CamadaEscolhida()
            {
            if (_cmbCamada == null || _cmbCamada.SelectedIndex < 0) return 0;

            int camada;
            return int.TryParse(Convert.ToString(_cmbCamada.SelectedItem),
                NumberStyles.None, CultureInfo.InvariantCulture, out camada)
                ? camada : 0;
            }

        // ------------------------------------------------------------------
        //  Eventos
        // ------------------------------------------------------------------

        private void Posicao_Changed(object sender, EventArgs e)
            {
            if (_atualizandoUi) return;

            SincronizarTodasPosicoes();
            Notificar();
            }

        /// <summary>"Todas" marca/desmarca as 7 posicoes de uma vez.</summary>
        private void TodasPosicoes_Changed(object sender, EventArgs e)
            {
            if (_atualizandoUi) return;

            _atualizandoUi = true;
            try
                {
                bool marcar = _chkTodasPosicoes.Checked;
                foreach (CheckBox chk in _chkPosicoes) chk.Checked = marcar;
                }
            finally
                {
                _atualizandoUi = false;
                }

            Notificar();
            }

        /// <summary>
        /// Deixa o "Todas" refletindo o estado real: marcado so quando as 7
        /// posicoes estao marcadas.
        /// </summary>
        private void SincronizarTodasPosicoes()
            {
            if (_chkTodasPosicoes == null || _chkPosicoes.Count == 0) return;

            bool todas = true;
            foreach (CheckBox chk in _chkPosicoes)
                {
                if (!chk.Checked) { todas = false; break; }
                }

            if (_chkTodasPosicoes.Checked == todas) return;

            _atualizandoUi = true;
            try { _chkTodasPosicoes.Checked = todas; }
            finally { _atualizandoUi = false; }
            }

        /// <summary>Marcar "Todas Pares/Impares" limpa a camada especifica.</summary>
        private void TodasCamadas_Changed(object sender, EventArgs e)
            {
            if (_atualizandoUi) return;

            if (_chkTodasCamadas.Checked && _cmbCamada != null &&
                _cmbCamada.SelectedIndex >= 0)
                {
                _atualizandoUi = true;
                try { _cmbCamada.SelectedIndex = -1; }
                finally { _atualizandoUi = false; }
                }

            Notificar();
            }

        /// <summary>Escolher uma camada especifica desmarca "Todas Pares/Impares".</summary>
        private void CamadaEspecifica_Changed(object sender, EventArgs e)
            {
            if (_atualizandoUi) return;

            if (_cmbCamada.SelectedIndex >= 0 &&
                _chkTodasCamadas != null && _chkTodasCamadas.Checked)
                {
                _atualizandoUi = true;
                try { _chkTodasCamadas.Checked = false; }
                finally { _atualizandoUi = false; }
                }

            Notificar();
            }

        private void Notificar()
            {
            if (SelecaoAlterada != null) SelecaoAlterada(this, EventArgs.Empty);
            }

        /// <summary>Desmarca tudo neste painel.</summary>
        public void Limpar()
            {
            _atualizandoUi = true;
            try
                {
                foreach (CheckBox chk in _chkPosicoes) chk.Checked = false;
                if (_chkTodasPosicoes != null) _chkTodasPosicoes.Checked = false;
                if (_chkTodasCamadas != null) _chkTodasCamadas.Checked = false;
                if (_cmbCamada != null) _cmbCamada.SelectedIndex = -1;
                }
            finally
                {
                _atualizandoUi = false;
                }

            Notificar();
            }
        }
    }