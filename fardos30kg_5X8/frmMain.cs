using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace fardos30kg_5X8
    {
    public partial class frmMain : Form
        {

        /// <summary>Posicoes carregadas do arquivo. Vazio ate abrir algo.</summary>
        private Dictionary<string, Coordenada> _posicoes =
        new Dictionary<string, Coordenada>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Caminho do arquivo aberto (usado depois pelo Salvar).</summary>
        private string _caminhoArquivo = string.Empty;

        private readonly string _tituloBase;

        /// <summary>true quando ha ajuste aplicado e ainda nao salvo.</summary>
        private bool _alterado;

        /// <summary>Separador de coluna do arquivo aberto, para gravar igual.</summary>
        private char _separadorArquivo = ';';

        /// <summary>Selecao do palete 1, camadas impares (gbxPal1CamImpar).</summary>
        private PainelSelecao _selPal1Impar;

        /// <summary>Selecao do palete 1, camadas pares (gbxPal1CamPar).</summary>
        private PainelSelecao _selPal1Par;

        /// <summary>Pinta o grid do palete 1 com a uniao dos dois paineis.</summary>
        private MarcadorPalete _marcadorPal1;

        /// <summary>Selecao do palete 2, camadas impares (gbxPal2CamImpar).</summary>
        private PainelSelecao _selPal2Impar;

        /// <summary>Selecao do palete 2, camadas pares (gbxPal2CamPar).</summary>
        private PainelSelecao _selPal2Par;

        /// <summary>Pinta o grid do palete 2.</summary>
        private MarcadorPalete _marcadorPal2;

        public frmMain()
            {
            InitializeComponent();
            _tituloBase = this.Text;                 // "Fardo 30kg - 5X8"

            ConfigurarDialogos();
            ConfigurarSpinboxes();

            // Liga os checkboxes. Os eventos sao registrados dentro do
            // PainelSelecao — nao precisa mexer no Designer.
            _selPal1Impar = new PainelSelecao(
                1,                          // palete 1 (esquerdo)
                ParidadeCamada.Impares,
                chkEsqImpares,              // "Todas Impares"
                cbxCamImpar,                // camada especifica (1,3,5,7,9,11)
                chkEsqImparAll,             // "Todas" as posicoes
                chkEsqImparPos1, chkEsqImparPos2, chkEsqImparPos3,
                chkEsqImparPos4, chkEsqImparPos5, chkEsqImparPos6,
                chkEsqImparPos7);

            _selPal1Par = new PainelSelecao(
                1,
                ParidadeCamada.Pares,
                chkEsqPares,                // "Todas Pares"
                cbxCamPar,                  // camada especifica (2,4,6,8,10)
                chkEsqParAll,               // "Todas" as posicoes
                chkEsqParPos1, chkEsqParPos2, chkEsqParPos3,
                chkEsqParPos4, chkEsqParPos5, chkEsqParPos6,
                chkEsqParPos7);                // pos7 do groupbox PAR (sem "Par" no nome)

            // ---------- palete 2 (direito) ----------
            _selPal2Impar = new PainelSelecao(
                2,                          // palete 2 (direito)
                ParidadeCamada.Impares,
                chkDirImpares,
                cbxCamImparDir,
                chkDirImparAll,
                chkDirImparPos1, chkDirImparPos2, chkDirImparPos3,
                chkDirImparPos4, chkDirImparPos5, chkDirImparPos6,
                chkDirImparPos7);

            _selPal2Par = new PainelSelecao(
                2,
                ParidadeCamada.Pares,
                chkDirPares,
                cbxCamParDir,
                chkDirParAll,
                chkDirParPos1, chkDirParPos2, chkDirParPos3,
                chkDirParPos4, chkDirParPos5, chkDirParPos6,
                chkDirParPos7);

            // Um marcador por grid: os paineis do mesmo palete pintam junto,
            // sem se apagar. Os dois paletes sao independentes.
            _marcadorPal1 = new MarcadorPalete(
                dataGridPalete1, _selPal1Impar, _selPal1Par);
            _marcadorPal2 = new MarcadorPalete(
                dataGridPalete2, _selPal2Impar, _selPal2Par);

            _marcadorPal1.SelecaoAlterada += marcador_SelecaoAlterada;
            _marcadorPal2.SelecaoAlterada += marcador_SelecaoAlterada;

            AtualizarBotoesAplicar();
            }

        public Dictionary<string, Coordenada> Posicoes
            {
            get { return _posicoes; }
            }

        // ------------------------------------------------------------------
        //  Caixas de dialogo
        // ------------------------------------------------------------------

        /// <summary>
        /// Filtros e sugestoes das caixas Abrir/Salvar.
        ///
        /// Feito em codigo, e nao no Designer, de proposito: toda vez que o
        /// Visual Studio regera o frmMain.Designer.cs essas propriedades se
        /// perdem. Aqui elas sobrevivem.
        ///
        /// O Filter e uma unica string com pares separados por "|":
        ///     descricao1|mascara1|descricao2|mascara2|...
        /// O numero de partes tem que ser PAR — se sobrar uma, o dialogo
        /// lanca ArgumentException na hora de abrir, nao na compilacao.
        /// Varias mascaras no mesmo item vao separadas por ";".
        /// </summary>
        private void ConfigurarDialogos()
            {
            // ---------- Abrir ----------
            openDlgArqCsv.Title = "Abrir arquivo de posições";

            openDlgArqCsv.Filter =
                "Arquivos de posições (*.csv;*.txt)|*.csv;*.txt|" +
                "Arquivos CSV (*.csv)|*.csv|" +
                "Texto (*.txt)|*.txt|" +
                "Todos os arquivos (*.*)|*.*";

            openDlgArqCsv.FilterIndex = 1;      // 1 = primeiro item (NAO e base zero)
            openDlgArqCsv.DefaultExt = "csv";   // completa a extensao se o usuario digitar so o nome

            // Nome sugerido na caixa. O Designer tinha deixado "openFileDialog1",
            // que aparecia escrito para o operador.
            openDlgArqCsv.FileName = "posicoes.csv";

            openDlgArqCsv.CheckFileExists = true;   // nao deixa escolher arquivo inexistente
            openDlgArqCsv.CheckPathExists = true;
            openDlgArqCsv.Multiselect = false;
            openDlgArqCsv.RestoreDirectory = true;  // nao muda o diretorio atual do processo

            // ---------- Salvar ----------
            saveDlgArqCsv.Title = "Salvar posições ajustadas";
            saveDlgArqCsv.Filter =
                "Arquivos CSV (*.csv)|*.csv|" +
                "Texto (*.txt)|*.txt|" +
                "Todos os arquivos (*.*)|*.*";

            saveDlgArqCsv.FilterIndex = 1;
            saveDlgArqCsv.DefaultExt = "csv";
            saveDlgArqCsv.AddExtension = true;      // digitou "teste" -> grava "teste.csv"
            saveDlgArqCsv.OverwritePrompt = true;   // avisa antes de sobrescrever
            saveDlgArqCsv.RestoreDirectory = true;
            }

        /// <summary>
        /// Nome sugerido no Salvar, derivado do arquivo aberto:
        /// "posicoes.csv" -> "posicoes_ajustado.csv".
        /// </summary>
        private void SugerirNomeSalvar()
            {
            if (string.IsNullOrEmpty(_caminhoArquivo)) return;

            string pasta = Path.GetDirectoryName(_caminhoArquivo);
            string nome = Path.GetFileNameWithoutExtension(_caminhoArquivo);

            saveDlgArqCsv.InitialDirectory = pasta;
            saveDlgArqCsv.FileName = nome + "_ajustado.csv";
            }

        // ------------------------------------------------------------------
        //  Ajuste de posicoes
        // ------------------------------------------------------------------

        /// <summary>
        /// Alinha os NumericUpDown ao limite real da regra de negocio.
        /// O limite vive em AjustadorPosicoes.LimiteMm — aqui so espelha,
        /// para nao existirem dois numeros diferentes no sistema.
        /// </summary>
        private void ConfigurarSpinboxes()
            {
            decimal mm = (decimal)AjustadorPosicoes.LimiteMm;
            decimal gr = (decimal)AjustadorPosicoes.LimiteGraus;

            AjustarSpin(nUpDownX, mm); AjustarSpin(nUpDownXDir, mm);
            AjustarSpin(nUpDownY, mm); AjustarSpin(nUpDownYDir, mm);
            AjustarSpin(nUpDownZ, mm); AjustarSpin(nUpDownZDir, mm);
            AjustarSpin(nUpDownC, gr); AjustarSpin(nUpDownCDir, gr);

            string titulo = "Ajustar Posições  (max. " +
                AjustadorPosicoes.LimiteMm + " por aplicação)";
            gbxAjustarPosic.Text = titulo;
            gbxAjustarPosicDir.Text = titulo;
            }

        private static void AjustarSpin(NumericUpDown nud, decimal limite)
            {
            nud.Minimum = -limite;
            nud.Maximum = limite;
            nud.Value = 0m;
            nud.TextAlign = HorizontalAlignment.Right;
            }

        private void marcador_SelecaoAlterada(object sender, EventArgs e)
            {
            AtualizarBotoesAplicar();
            }

        private void AtualizarBotoesAplicar()
            {
            AtualizarBotao(btnAplicar, _marcadorPal1);
            AtualizarBotao(btnAplicarDir, _marcadorPal2);
            }

        private void AtualizarBotao(Button botao, MarcadorPalete marcador)
            {
            int quantas = (_posicoes.Count == 0) ? 0 : marcador.Quantidade;

            botao.Enabled = quantas > 0;
            botao.Text = (quantas > 0) ? "Aplicar (" + quantas + ")" : "Aplicar";
            }

        private void btnAplicar_Click(object sender, EventArgs e)
            {
            AplicarPalete(1, _marcadorPal1, dataGridPalete1,
                          nUpDownX, nUpDownY, nUpDownZ, nUpDownC);
            }

        private void btnAplicarDir_Click(object sender, EventArgs e)
            {
            AplicarPalete(2, _marcadorPal2, dataGridPalete2,
                          nUpDownXDir, nUpDownYDir, nUpDownZDir, nUpDownCDir);
            }

        /// <summary>
        /// Aplica os incrementos de UM palete. O dicionario e o mesmo para os
        /// dois: quem separa e o filtro, que ja carrega o numero do palete.
        /// </summary>
        private void AplicarPalete(
            int palete, MarcadorPalete marcador, DataGridView grid,
            NumericUpDown spinX, NumericUpDown spinY,
            NumericUpDown spinZ, NumericUpDown spinC)
            {
            if (_posicoes.Count == 0)
                {
                MessageBox.Show(this, "Abra um arquivo de posicoes primeiro.",
                    "Aplicar", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
                }

            List<string> alvos = marcador.ChavesSelecionadas();
            if (alvos.Count == 0)
                {
                MessageBox.Show(this,
                    "Nenhuma posicao selecionada no palete " + palete + "." +
                    Environment.NewLine + Environment.NewLine +
                    "Marque as posicoes (1 a 7) e escolha as camadas: " +
                    "\"Todas Impares\"/\"Todas Pares\", ou uma camada no combo.",
                    "Aplicar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
                }

            float dx = (float)spinX.Value;
            float dy = (float)spinY.Value;
            float dz = (float)spinZ.Value;
            float dc = (float)spinC.Value;

            if (dx == 0f && dy == 0f && dz == 0f && dc == 0f)
                {
                MessageBox.Show(this, "Todos os incrementos estao zerados.",
                    "Aplicar", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
                }

            try
                {
                int alteradas = AjustadorPosicoes.Aplicar(_posicoes, alvos, dx, dy, dz, dc);

                // Recarrega so o grid deste palete. O MarcadorPalete escuta
                // DataBindingComplete e repinta sozinho, entao o amarelo
                // sobrevive ao Aplicar.
                GridPosicoes.Popular(grid, _posicoes, palete);

                ZerarIncrementos(spinX, spinY, spinZ, spinC);

                _alterado = true;
                AtualizarTitulo();
                AtualizarBotoesAplicar();

                this.Text += "   [palete " + palete + ": " + alteradas + " ajustada(s)" +
                    Fmt(dx, "X") + Fmt(dy, "Y") + Fmt(dz, "Z") + Fmt(dc, "C") + "]";
                }
            catch (ArgumentOutOfRangeException ex)
                {
                // Rede de seguranca: os NumericUpDown ja limitam, mas a regra
                // de negocio vive no AjustadorPosicoes e vale para quem chamar.
                MessageBox.Show(this, ex.Message,
                    "Limite excedido", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

        private static string Fmt(float v, string eixo)
            {
            if (v == 0f) return string.Empty;
            return " " + eixo + (v > 0 ? "+" : "") + v;
            }

        /// <summary>Zera os quatro spinbox do palete que acabou de ser ajustado.</summary>
        private static void ZerarIncrementos(
            NumericUpDown x, NumericUpDown y, NumericUpDown z, NumericUpDown c)
            {
            x.Value = 0m;
            y.Value = 0m;
            z.Value = 0m;
            c.Value = 0m;
            }

        // ------------------------------------------------------------------
        //  Abrir arquivo
        // ------------------------------------------------------------------

        private void menuAbrir_Click(object sender, EventArgs e)
            {
            // Abrir outro arquivo descarta os ajustes em memoria.
            if (!ConfirmarDescarte("abrir outro arquivo")) return;

            // Comeca na pasta do .exe na primeira vez; depois lembra a ultima usada.
            if (string.IsNullOrEmpty(openDlgArqCsv.InitialDirectory))
                openDlgArqCsv.InitialDirectory = Application.StartupPath;

            if (openDlgArqCsv.ShowDialog(this) != DialogResult.OK)
                return;                              // usuario cancelou

            CarregarArquivo(openDlgArqCsv.FileName);
            }

        /// <summary>
        /// Le o arquivo para o dicionario. Nao lanca: qualquer falha vira MessageBox.
        /// </summary>
        private void CarregarArquivo(string caminho)
            {
            Cursor cursorAnterior = this.Cursor;
            this.Cursor = Cursors.WaitCursor;

            try
                {
                Posicoesreader leitor = new Posicoesreader();
                Dictionary<string, Coordenada> lidas = leitor.Ler(caminho);

                if (lidas.Count == 0)
                    {
                    MessageBox.Show(this,
                        "O arquivo nao contem nenhuma posicao valida:" +
                        Environment.NewLine + caminho,
                        "Abrir", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;                          // mantem o que ja estava carregado
                    }

                _posicoes = lidas;
                _caminhoArquivo = caminho;
                _separadorArquivo = leitor.SeparadorDetectado;
                _alterado = false;
                openDlgArqCsv.InitialDirectory = Path.GetDirectoryName(caminho);

                AtualizarTitulo();
                PosicoesCarregadas();

                GridPosicoes.Popular(dataGridPalete1, _posicoes, 1);   // esquerdo
                GridPosicoes.Popular(dataGridPalete2, _posicoes, 2);   // direito
                AtualizarBotoesAplicar();

                if (leitor.Erros.Count > 0)
                    MostrarAvisos(lidas.Count, leitor.Erros);
                else
                    MessageBox.Show(this,
                        lidas.Count + " posicoes carregadas.",
                        "Abrir", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            catch (FileNotFoundException)
                {
                MessageBox.Show(this,
                    "Arquivo nao encontrado:" + Environment.NewLine + caminho,
                    "Abrir", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            catch (IOException ex)
                {
                // Arquivo aberto no Excel/LibreOffice cai aqui.
                MessageBox.Show(this,
                    "Nao foi possivel ler o arquivo. Ele pode estar aberto em outro programa." +
                    Environment.NewLine + Environment.NewLine + ex.Message,
                    "Abrir", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            catch (UnauthorizedAccessException)
                {
                MessageBox.Show(this,
                    "Sem permissao de leitura para:" + Environment.NewLine + caminho,
                    "Abrir", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            finally
                {
                this.Cursor = cursorAnterior;
                }
            }

        /// <summary>
        /// Gancho para o resto da tela reagir ao arquivo novo.
        /// </summary>
        private void PosicoesCarregadas()
            {
            menuSalvar.Enabled = true;
            menuSalvarComo.Enabled = true;
            SugerirNomeSalvar();
            }

        private void MostrarAvisos(int carregadas, IList<string> erros)
            {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(carregadas + " posicoes carregadas.");
            sb.AppendLine();
            sb.AppendLine(erros.Count + " linha(s) ignorada(s):");
            sb.AppendLine();

            int limite = Math.Min(erros.Count, 15);
            for (int i = 0; i < limite; i++) sb.AppendLine(erros[i]);
            if (erros.Count > limite)
                sb.AppendLine("... e mais " + (erros.Count - limite) + ".");

            MessageBox.Show(this, sb.ToString(),
                "Abrir", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

        private void AtualizarTitulo()
            {
            if (string.IsNullOrEmpty(_caminhoArquivo))
                {
                this.Text = _tituloBase;
                return;
                }

            this.Text = _tituloBase + " - " + Path.GetFileName(_caminhoArquivo) +
                (_alterado ? " *" : string.Empty) +
                " (" + _posicoes.Count + " posicoes)";
            }

        // ------------------------------------------------------------------
        //  Salvar
        // ------------------------------------------------------------------

        private void menuSalvar_Click(object sender, EventArgs e)
            {
            Salvar();
            }

        private void menuSalvarComo_Click(object sender, EventArgs e)
            {
            SalvarComo();
            }

        /// <summary>
        /// Grava no arquivo aberto. Se ainda nao ha arquivo, cai no Salvar Como.
        /// </summary>
        /// <returns>true se gravou; false se falhou ou o usuario cancelou.</returns>
        private bool Salvar()
            {
            if (string.IsNullOrEmpty(_caminhoArquivo)) return SalvarComo();

            return Gravar(_caminhoArquivo);
            }

        /// <returns>true se gravou; false se falhou ou o usuario cancelou.</returns>
        private bool SalvarComo()
            {
            if (_posicoes.Count == 0)
                {
                MessageBox.Show(this, "Nao ha posicoes para salvar.",
                    "Salvar", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
                }

            SugerirNomeSalvar();

            if (saveDlgArqCsv.ShowDialog(this) != DialogResult.OK)
                return false;                        // usuario cancelou

            return Gravar(saveDlgArqCsv.FileName);
            }

        /// <summary>
        /// Gravacao propriamente dita. Nenhuma excecao escapa: tudo vira
        /// MessageBox e false.
        /// </summary>
        private bool Gravar(string caminho)
            {
            Cursor cursorAnterior = this.Cursor;
            this.Cursor = Cursors.WaitCursor;

            try
                {
                PosicoesWriter escritor = new PosicoesWriter();
                escritor.Separador = _separadorArquivo;   // devolve no formato que veio
                escritor.Gravar(caminho, _posicoes);

                _caminhoArquivo = caminho;
                _alterado = false;
                AtualizarTitulo();

                this.Text += "   [salvo: " + Path.GetFileName(caminho) + "]";
                return true;
                }
            catch (IOException ex)
                {
                // Arquivo aberto no Excel/LibreOffice cai aqui.
                MessageBox.Show(this,
                    "Nao foi possivel gravar. O arquivo pode estar aberto em outro programa." +
                    Environment.NewLine + Environment.NewLine + ex.Message,
                    "Salvar", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            catch (UnauthorizedAccessException)
                {
                MessageBox.Show(this,
                    "Sem permissao de gravacao para:" + Environment.NewLine + caminho,
                    "Salvar", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            catch (Exception ex)
                {
                MessageBox.Show(this,
                    "Falha ao gravar:" + Environment.NewLine + Environment.NewLine + ex.Message,
                    "Salvar", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            finally
                {
                this.Cursor = cursorAnterior;
                }

            return false;
            }

        // ------------------------------------------------------------------
        //  Descartar alteracoes
        // ------------------------------------------------------------------

        /// <summary>
        /// Pergunta o que fazer com os ajustes nao salvos antes de uma acao
        /// que os perderia (sair, abrir outro arquivo).
        /// </summary>
        /// <returns>true = pode seguir; false = a acao deve ser cancelada.</returns>
        private bool ConfirmarDescarte(string acao)
            {
            if (!_alterado) return true;

            DialogResult r = MessageBox.Show(this,
                "Ha ajustes de posicao que ainda nao foram salvos." +
                Environment.NewLine + Environment.NewLine +
                "Deseja salvar antes de " + acao + "?",
                "Alteracoes nao salvas",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button1);     // Sim e o padrao: Enter salva

            if (r == DialogResult.Cancel) return false;

            // Nao: descarta e segue.
            if (r == DialogResult.No) return true;

            // Sim: so segue se a gravacao der certo. Se o usuario cancelar o
            // dialogo de salvar, ou faltar permissao, a acao e abortada em vez
            // de jogar o trabalho fora em silencio.
            return Salvar();
            }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
            {
            // Vale para o menu Sair, para o X da janela e para Alt+F4.
            if (!ConfirmarDescarte("sair")) e.Cancel = true;
            }

        private void menuSair_Click(object sender, EventArgs e)
            {
            this.Close();          // dispara o FormClosing acima
            }

        }
    }