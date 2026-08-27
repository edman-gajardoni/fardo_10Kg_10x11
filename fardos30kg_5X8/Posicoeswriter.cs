using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace fardos30kg_5X8
    {
    /// <summary>
    /// Grava o dicionario de posicoes de volta em CSV, no mesmo formato que o
    /// PosicoesReader le — ida e volta sem perda.
    ///
    /// A gravacao e ATOMICA: escreve num arquivo temporario e so no fim troca
    /// pelo definitivo. Se faltar energia ou o programa morrer no meio, o
    /// arquivo bom continua intacto em vez de virar meio arquivo.
    /// </summary>
    public class PosicoesWriter
        {
        private char _separador = ';';
        private bool _escreverCabecalho = true;

        /// <summary>Separador de coluna. Use o mesmo que o leitor detectou.</summary>
        public char Separador
            {
            get { return _separador; }
            set { _separador = value; }
            }

        /// <summary>Se grava a linha "nome;x;y;z;c" no topo.</summary>
        public bool EscreverCabecalho
            {
            get { return _escreverCabecalho; }
            set { _escreverCabecalho = value; }
            }

        public void Gravar(string caminho, Dictionary<string, Coordenada> posicoes)
            {
            if (string.IsNullOrEmpty(caminho)) throw new ArgumentNullException("caminho");
            if (posicoes == null) throw new ArgumentNullException("posicoes");

            string temporario = caminho + ".tmp";

            // UTF8 sem BOM: como chaves e numeros sao todos ASCII, o arquivo sai
            // byte a byte igual a um texto puro — nao incomoda nenhum programa
            // que leia isso depois (Excel, PMAC, script).
            UTF8Encoding semBom = new UTF8Encoding(false);

            using (StreamWriter sw = new StreamWriter(temporario, false, semBom))
                {
                if (_escreverCabecalho)
                    {
                    sw.WriteLine("nome" + _separador + "x" + _separador + "y" +
                                 _separador + "z" + _separador + "c");
                    }

                foreach (string chave in Ordenar(posicoes.Keys))
                    {
                    Coordenada c = posicoes[chave];

                    sw.WriteLine(
                        chave + _separador +
                        Num(c.X) + _separador +
                        Num(c.Y) + _separador +
                        Num(c.Z) + _separador +
                        Num(c.C));
                    }
                }

            TrocarArquivo(temporario, caminho);
            }

        /// <summary>
        /// Substitui o arquivo final pelo temporario. So aqui o arquivo bom
        /// deixa de existir, e por um instante minimo.
        /// </summary>
        private static void TrocarArquivo(string temporario, string destino)
            {
            try
                {
                if (File.Exists(destino))
                    {
                    // Troca em um passo so. O terceiro argumento null diz
                    // "nao quero copia de backup".
                    File.Replace(temporario, destino, null, true);
                    }
                else
                    {
                    File.Move(temporario, destino);
                    }
                }
            catch (PlatformNotSupportedException)
                {
                // File.Replace nao funciona em alguns sistemas de arquivo
                // (rede, FAT). Cai para o caminho simples.
                if (File.Exists(destino)) File.Delete(destino);
                File.Move(temporario, destino);
                }
            catch
                {
                // Nao deixa lixo .tmp para tras se a troca falhar.
                try { if (File.Exists(temporario)) File.Delete(temporario); }
                catch { }
                throw;
                }
            }

        /// <summary>
        /// Ordena por palete, camada, posicao. Sem isto a ordem do Dictionary
        /// e arbitraria e o arquivo sai embaralhado a cada gravacao — o que
        /// torna impossivel comparar duas versoes.
        /// </summary>
        private static List<string> Ordenar(IEnumerable<string> chaves)
            {
            List<string> lista = new List<string>(chaves);

            lista.Sort(delegate(string a, string b)
            {
                ChavePosicao ca, cb;
                bool okA = ChavePosicao.TryParse(a, out ca);
                bool okB = ChavePosicao.TryParse(b, out cb);

                // Chaves fora do padrao vao para o fim, em ordem alfabetica.
                if (!okA && !okB) return string.CompareOrdinal(a, b);
                if (!okA) return 1;
                if (!okB) return -1;

                int r = ca.Palete.CompareTo(cb.Palete);
                if (r != 0) return r;

                r = ca.Camada.CompareTo(cb.Camada);
                if (r != 0) return r;

                return ca.Posicao.CompareTo(cb.Posicao);
            });

            return lista;
            }

        /// <summary>
        /// Sempre InvariantCulture: grava "1182.5", nunca "1182,5", em
        /// qualquer maquina. "0.###" evita casas decimais inuteis — 1180
        /// sai como "1180" e nao "1180.000".
        /// </summary>
        private static string Num(float v)
            {
            return v.ToString("0.###", CultureInfo.InvariantCulture);
            }
        }
    }