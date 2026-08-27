using System;
using System.ComponentModel;
using System.Globalization;

namespace fardos30kg_5X8
    {
    /// <summary>
    /// Uma linha do grid. O DataGridView so sabe ligar colunas a PROPRIEDADES
    /// PUBLICAS — por isso o dicionario nao pode ser ligado direto:
    /// KeyValuePair.Value e uma Coordenada, e a chave ficaria de fora.
    ///
    /// O [DisplayName] vira o texto do cabecalho quando
    /// AutoGenerateColumns = true.
    /// </summary>
    public class LinhaPosicao
        {
        public LinhaPosicao(string chave, ChavePosicao ch, Coordenada c)
            {
            Chave = chave;
            Palete = ch.Palete;
            Camada = ch.Camada;
            Posicao = ch.Posicao;
            X = c.X;
            Y = c.Y;
            Z = c.Z;
            C = c.C;
            }

        [DisplayName("Chave")]
        public string Chave { get; set; }

        [DisplayName("Palete")]
        public int Palete { get; set; }

        [DisplayName("Camada")]
        public int Camada { get; set; }

        [DisplayName("Pos")]
        public int Posicao { get; set; }

        [DisplayName("X (mm)")]
        public float X { get; set; }

        [DisplayName("Y (mm)")]
        public float Y { get; set; }

        [DisplayName("Z (mm)")]
        public float Z { get; set; }

        [DisplayName("C (graus)")]
        public float C { get; set; }

        public override string ToString()
            {
            return string.Format(CultureInfo.InvariantCulture,
                "{0}: X={1} Y={2} Z={3} C={4}", Chave, X, Y, Z, C);
            }
        }
    }