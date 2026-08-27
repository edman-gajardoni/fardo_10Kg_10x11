using System;
using System.Globalization;
using System.Text;
 
namespace fardos30kg_5X8
    {
    /// <summary>
    /// Conversao de texto para float imune a diferenca de separador decimal
    /// (ponto x virgula) e a cultura da maquina onde o programa roda.
    ///
    /// Regras aplicadas:
    ///  - Remove espacos, espaco nao-separavel (NBSP/U+00A0) e apostrofo (separador
    ///    de milhar usado em algumas culturas).
    ///  - Aceita sinal de menos unicode (U+2212, U+2013) e "+" na frente.
    ///  - Se o texto tem ponto E virgula: o ultimo que aparece e o separador
    ///    decimal, o outro e separador de milhar e e removido.
    ///      "1.234,56" -> 1234.56      "1,234.56" -> 1234.56
    ///  - Se tem so um tipo de separador e ele aparece mais de uma vez, e milhar:
    ///      "1.234.567" -> 1234567     "1,234,567" -> 1234567
    ///  - Se aparece uma unica vez, e tratado como decimal:
    ///      "1182,5" -> 1182.5         "1182.5" -> 1182.5
    ///  - Notacao cientifica ("1,5e3") tambem funciona.
    ///  - Sempre converte com CultureInfo.InvariantCulture, entao o
    ///    resultado nao muda se a maquina estiver em pt-BR ou en-US.
    /// </summary>
    public static class NumeroParser
        {
        /// <summary>
        /// Tenta converter. Retorna false em vez de lancar excecao.
        /// </summary>
        public static bool TryParseFloat(string texto, out float valor)
            {
            valor = 0f;

            string normalizado = Normalizar(texto);
            if (normalizado.Length == 0) return false;

            return float.TryParse(
                normalizado,
                NumberStyles.Float,              // sinal, decimal e expoente
                CultureInfo.InvariantCulture,
                out valor);
            }

        /// <summary>
        /// Converte ou lanca FormatException com a mensagem apontando o texto original.
        /// </summary>
        public static float ParseFloat(string texto)
            {
            float v;
            if (!TryParseFloat(texto, out v))
                {
                throw new FormatException(
                    "Nao foi possivel converter para float: \"" + (texto ?? "<null>") + "\"");
                }
            return v;
            }

        /// <summary>
        /// Converte, e devolve o valor padrao quando o texto for invalido ou vazio.
        /// Util para colunas opcionais (ex.: C ausente na linha).
        /// </summary>
        public static float ParseFloatOuPadrao(string texto, float padrao)
            {
            float v;
            return TryParseFloat(texto, out v) ? v : padrao;
            }

        /// <summary>
        /// Deixa a string em formato invariante: "-1234.56".
        /// </summary>
        public static string Normalizar(string texto)
            {
            if (string.IsNullOrEmpty(texto)) return string.Empty;

            // 1) Limpeza de caracteres que nao interessam para o numero.
            StringBuilder sb = new StringBuilder(texto.Length);
            foreach (char ch in texto)
                {
                if (ch == '\u2212' || ch == '\u2013' || ch == '\u2014')
                    {
                    sb.Append('-');                 // menos unicode -> hifen
                    }
                else if (ch == '\u00A0' || ch == '\u202F' || ch == '\'' ||
                         ch == '_' || char.IsWhiteSpace(ch))
                    {
                    // separador de milhar "invisivel" ou espaco: descarta
                    }
                else
                    {
                    sb.Append(ch);
                    }
                }

            string s = sb.ToString();
            if (s.Length == 0) return string.Empty;

            // 2) Guarda o sinal e trabalha so com a parte numerica.
            string sinal = string.Empty;
            if (s[0] == '-' || s[0] == '+')
                {
                sinal = (s[0] == '-') ? "-" : string.Empty;
                s = s.Substring(1);
                }
            if (s.Length == 0) return string.Empty;

            // 3) Separa o expoente (e/E) antes de mexer nos separadores.
            string expoente = string.Empty;
            int posE = s.IndexOfAny(new char[] { 'e', 'E' });
            if (posE >= 0)
                {
                expoente = s.Substring(posE);       // inclui o "e" e o sinal do expoente
                s = s.Substring(0, posE);
                }

            // 4) Resolve ponto x virgula na mantissa.
            int ultimoPonto = s.LastIndexOf('.');
            int ultimaVirgula = s.LastIndexOf(',');

            if (ultimoPonto >= 0 && ultimaVirgula >= 0)
                {
                // Tem os dois: o que vier por ultimo e o decimal.
                if (ultimaVirgula > ultimoPonto)
                    {
                    s = s.Replace(".", string.Empty);   // ponto = milhar
                    s = s.Replace(',', '.');            // virgula = decimal
                    }
                else
                    {
                    s = s.Replace(",", string.Empty);   // virgula = milhar
                    }
                }
            else if (ultimaVirgula >= 0)
                {
                s = ContarOcorrencias(s, ',') > 1
                    ? s.Replace(",", string.Empty)      // "1,234,567" -> milhar
                    : s.Replace(',', '.');              // "1182,5"    -> decimal
                }
            else if (ultimoPonto >= 0)
                {
                if (ContarOcorrencias(s, '.') > 1)
                    s = s.Replace(".", string.Empty);   // "1.234.567" -> milhar
                // um ponto so ja esta no formato invariante
                }

            // 5) ".5" -> "0.5"  e  "5." -> "5"
            if (s.Length > 0 && s[0] == '.') s = "0" + s;
            if (s.Length > 0 && s[s.Length - 1] == '.') s = s.Substring(0, s.Length - 1);

            if (s.Length == 0) return string.Empty;

            return sinal + s + expoente;
            }

        private static int ContarOcorrencias(string s, char c)
            {
            int n = 0;
            for (int i = 0; i < s.Length; i++)
                {
                if (s[i] == c) n++;
                }
            return n;
            }
        }
    }
