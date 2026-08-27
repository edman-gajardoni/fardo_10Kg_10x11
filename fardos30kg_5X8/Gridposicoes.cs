using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace fardos30kg_5X8
    {
    /// <summary>
    /// Enche um DataGridView com as posicoes de um palete.
    ///
    /// Respeita as colunas que voce criou no Designer: em vez de apagar e
    /// recriar, amarra cada coluna existente a uma propriedade de LinhaPosicao
    /// pelo NOME da coluna (chave, xcoord, ycoord, zcoord, Giro).
    /// So monta colunas do zero se o grid vier sem nenhuma.
    ///
    /// AutoGenerateColumns fica FALSE de proposito. Com ele em true, as colunas
    /// so nascem quando o grid ganha BindingContext — ou seja, DEPOIS do
    /// construtor do form — e qualquer formatacao aplicada antes disso some
    /// sem dar erro.
    /// </summary>
    public static class GridPosicoes
        {
        /// <summary>
        /// Nome da coluna no Designer -> propriedade de LinhaPosicao.
        /// Acrescente aqui se criar colunas novas (ex.: "camada" -> "Camada").
        /// </summary>
        private static readonly Dictionary<string, string> _mapaColunas =
            NovoMapa();

        private static Dictionary<string, string> NovoMapa()
            {
            Dictionary<string, string> m =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Nomes usados no seu frmMain.Designer.cs
            m["chave"] = "Chave";
            m["xcoord"] = "X";
            m["ycoord"] = "Y";
            m["zcoord"] = "Z";
            m["Giro"] = "C";

            // Nomes alternativos, caso voce crie colunas extras
            m["camada"] = "Camada";
            m["posicao"] = "Posicao";
            m["palete"] = "Palete";

            return m;
            }

        /// <summary>
        /// Popula o grid com as posicoes do palete indicado.
        /// </summary>
        /// <param name="grid">DataGridView de destino (dataGridPalete1/2).</param>
        /// <param name="posicoes">Dicionario lido do CSV.</param>
        /// <param name="palete">Numero do palete (1 ou 2). 0 = todos.</param>
        /// <returns>Quantas linhas foram carregadas.</returns>
        public static int Popular(
            DataGridView grid,
            Dictionary<string, Coordenada> posicoes,
            int palete)
            {
            if (grid == null) throw new ArgumentNullException("grid");
            if (posicoes == null) throw new ArgumentNullException("posicoes");

            List<LinhaPosicao> linhas = new List<LinhaPosicao>();

            foreach (KeyValuePair<string, Coordenada> par in posicoes)
                {
                ChavePosicao ch;

                // Chave fora do padrao "palXcamYposZ" simplesmente nao entra.
                if (!ChavePosicao.TryParse(par.Key, out ch)) continue;

                if (palete > 0 && ch.Palete != palete) continue;

                linhas.Add(new LinhaPosicao(par.Key, ch, par.Value));
                }

            // Dictionary NAO tem ordem garantida — sem este Sort as linhas
            // aparecem embaralhadas e podem trocar de lugar entre execucoes.
            linhas.Sort(delegate(LinhaPosicao a, LinhaPosicao b)
            {
                int r = a.Camada.CompareTo(b.Camada);
                if (r != 0) return r;
                return a.Posicao.CompareTo(b.Posicao);
            });

            grid.SuspendLayout();
            try
                {
                grid.DataSource = null;
                grid.AutoGenerateColumns = false;

                if (grid.Columns.Count == 0)
                    MontarColunasPadrao(grid, palete == 0);
                else
                    AmarrarColunasExistentes(grid);

                AjustarAparencia(grid);

                grid.DataSource = linhas;
                }
            finally
                {
                grid.ResumeLayout();
                }

            return linhas.Count;
            }

        /// <summary>
        /// Converte o nome da coluna do Designer no nome da propriedade.
        /// Tolera sufixo de clonagem: "xcoordDir" e "xcoord2" caem em "xcoord".
        /// Devolve null se nao souber.
        /// </summary>
        private static string PropriedadeDaColuna(string nomeColuna)
            {
            if (string.IsNullOrEmpty(nomeColuna)) return null;

            string n = nomeColuna;

            // sufixo do palete 2: chaveDir, xcoordDir, ... e tambem chave2, xcoord2
            if (n.Length > 3 && n.EndsWith("Dir", StringComparison.OrdinalIgnoreCase))
                n = n.Substring(0, n.Length - 3);

            n = n.TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');

            string propriedade;
            return _mapaColunas.TryGetValue(n, out propriedade) ? propriedade : null;
            }

        /// <summary>
        /// Liga as colunas do Designer as propriedades, pelo nome da coluna.
        /// </summary>
        private static void AmarrarColunasExistentes(DataGridView grid)
            {
            int ligadas = 0;
            string desconhecidas = string.Empty;

            foreach (DataGridViewColumn col in grid.Columns)
                {
                string propriedade = PropriedadeDaColuna(col.Name);

                if (propriedade == null)
                    {
                    // Coluna que nao conheco. Sem DataPropertyName ela fica
                    // VAZIA na tela — o grid enche de linhas e nao mostra nada.
                    desconhecidas += (desconhecidas.Length > 0 ? ", " : "") + col.Name;
                    continue;
                    }

                ligadas++;

                col.DataPropertyName = propriedade;
                col.ReadOnly = true;
                col.SortMode = DataGridViewColumnSortMode.NotSortable;

                if (propriedade == "X" || propriedade == "Y" ||
                    propriedade == "Z" || propriedade == "C")
                    {
                    col.DefaultCellStyle.Format = "0.##";
                    col.DefaultCellStyle.Alignment =
                        DataGridViewContentAlignment.MiddleRight;
                    }
                else if (propriedade == "Camada" || propriedade == "Posicao" ||
                         propriedade == "Palete")
                    {
                    col.DefaultCellStyle.Alignment =
                        DataGridViewContentAlignment.MiddleCenter;
                    }
                }

            // Falhar em silencio aqui custa caro: o grid aparece com as 77
            // linhas certas e todas as celulas em branco, e nao ha nada na
            // tela que aponte a causa. Melhor estourar na hora.
            if (ligadas == 0)
                {
                throw new InvalidOperationException(
                    "Nenhuma coluna do grid \"" + grid.Name + "\" pode ser ligada a uma " +
                    "propriedade de LinhaPosicao. Colunas encontradas: " + desconhecidas +
                    ". Acrescente o nome em GridPosicoes._mapaColunas.");
                }
            }

        private static void MontarColunasPadrao(DataGridView grid, bool mostrarPalete)
            {
            grid.Columns.Clear();

            grid.Columns.Add(Texto("chave", "Chave", "Posicao", 150,
                DataGridViewContentAlignment.MiddleLeft, null));

            if (mostrarPalete)
                grid.Columns.Add(Texto("palete", "Palete", "Palete", 55,
                    DataGridViewContentAlignment.MiddleCenter, null));

            grid.Columns.Add(Texto("xcoord", "X", "x [mm]", 80,
                DataGridViewContentAlignment.MiddleRight, "0.##"));
            grid.Columns.Add(Texto("ycoord", "Y", "y [mm]", 80,
                DataGridViewContentAlignment.MiddleRight, "0.##"));
            grid.Columns.Add(Texto("zcoord", "Z", "z [mm]", 80,
                DataGridViewContentAlignment.MiddleRight, "0.##"));
            grid.Columns.Add(Texto("Giro", "C", "Giro [graus]", 90,
                DataGridViewContentAlignment.MiddleRight, "0.##"));
            }

        private static DataGridViewTextBoxColumn Texto(
            string nome, string propriedade, string cabecalho, int largura,
            DataGridViewContentAlignment alinhamento, string formato)
            {
            DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn();
            col.Name = nome;
            col.DataPropertyName = propriedade;
            col.HeaderText = cabecalho;
            col.Width = largura;
            col.FillWeight = largura;
            col.ReadOnly = true;
            col.SortMode = DataGridViewColumnSortMode.NotSortable;
            col.DefaultCellStyle.Alignment = alinhamento;

            if (!string.IsNullOrEmpty(formato))
                col.DefaultCellStyle.Format = formato;

            return col;
            }

        private static void AjustarAparencia(DataGridView grid)
            {
            grid.ReadOnly = true;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.RowHeadersVisible = false;
            grid.MultiSelect = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.ColumnHeadersHeightSizeMode =
                DataGridViewColumnHeadersHeightSizeMode.AutoSize;

            grid.AlternatingRowsDefaultCellStyle.BackColor =
                Color.FromArgb(245, 245, 245);
            }

        /// <summary>
        /// Devolve a chave da linha sob o cursor, ou string vazia.
        /// </summary>
        public static string ChaveSelecionada(DataGridView grid)
            {
            if (grid == null || grid.CurrentRow == null) return string.Empty;

            LinhaPosicao linha = grid.CurrentRow.DataBoundItem as LinhaPosicao;
            return (linha == null) ? string.Empty : linha.Chave;
            }
        }
    }