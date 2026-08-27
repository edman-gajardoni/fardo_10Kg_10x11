using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace fardos30kg_5X8
    {
    /// <summary>
    /// Quebra a chave do dicionario "pal1cam3pos5" em palete=1, camada=3, posicao=5.
    /// </summary>
    public struct ChavePosicao
        {
        private static readonly Regex _padrao = new Regex(
            @"^\s*pal(?<pal>\d+)cam(?<cam>\d+)pos(?<pos>\d+)\s*$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private readonly int _palete;
        private readonly int _camada;
        private readonly int _posicao;

        public ChavePosicao(int palete, int camada, int posicao)
            {
            _palete = palete;
            _camada = camada;
            _posicao = posicao;
            }

        public int Palete { get { return _palete; } }
        public int Camada { get { return _camada; } }
        public int Posicao { get { return _posicao; } }

        /// <summary>true se a camada for par (2, 4, 6, 8, 10).</summary>
        public bool CamadaPar { get { return (_camada % 2) == 0; } }

        public static bool TryParse(string chave, out ChavePosicao resultado)
            {
            resultado = new ChavePosicao();
            if (string.IsNullOrEmpty(chave)) return false;

            Match m = _padrao.Match(chave);
            if (!m.Success) return false;

            int pal, cam, pos;
            if (!int.TryParse(m.Groups["pal"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out pal)) return false;
            if (!int.TryParse(m.Groups["cam"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out cam)) return false;
            if (!int.TryParse(m.Groups["pos"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out pos)) return false;

            resultado = new ChavePosicao(pal, cam, pos);
            return true;
            }

        /// <summary>Remonta a chave no formato canonico.</summary>
        public override string ToString()
            {
            return string.Format(CultureInfo.InvariantCulture,
                "pal{0}cam{1}pos{2}", _palete, _camada, _posicao);
            }
        }
    }