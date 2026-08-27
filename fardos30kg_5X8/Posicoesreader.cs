using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace fardos30kg_5X8
    {
    /// <summary>
    /// Le o arquivo de posicoes (CSV) e devolve Dictionary&lt;string, Coordenada&gt;.
    ///
    /// Formato esperado por linha:  nome ; X ; Y ; Z ; C
    /// O separador de coluna e detectado automaticamente (; , TAB ou |).
    /// Linhas em branco e linhas comecando com # sao ignoradas.
    /// Uma linha de cabecalho ("nome;x;y;z;c") e detectada e pulada.
    /// </summary>
    public class Posicoesreader
        {
        private readonly List<string> _erros = new List<string>();

        /// <summary>Linhas que nao puderam ser lidas (numero da linha + motivo).</summary>
        public IList<string> Erros { get { return _erros; } }

        /// <summary>
        /// Separador de coluna detectado na ultima leitura. Passe para o
        /// PosicoesWriter para o arquivo voltar no mesmo formato que veio.
        /// </summary>
        public char SeparadorDetectado { get; private set; }

        /// <summary>
        /// Se true, uma linha invalida lanca excecao.
        /// Se false (padrao), a linha e pulada e registrada em Erros.
        /// </summary>
        public bool LancarExcecaoEmLinhaInvalida { get; set; }

        public Dictionary<string, Coordenada> Ler(string caminhoArquivo)
            {
            if (string.IsNullOrEmpty(caminhoArquivo))
                throw new ArgumentNullException("caminhoArquivo");

            if (!File.Exists(caminhoArquivo))
                throw new FileNotFoundException("Arquivo nao encontrado.", caminhoArquivo);

            // Encoding.Default cobre arquivos salvos em ANSI/Windows-1252 pelo Excel;
            // detectEncodingFromByteOrderMarks=true respeita BOM de UTF-8/UTF-16.
            using (StreamReader sr = new StreamReader(caminhoArquivo, Encoding.Default, true))
                {
                return Ler(sr);
                }
            }

        public Dictionary<string, Coordenada> Ler(TextReader leitor)
            {
            _erros.Clear();

            // StringComparer.OrdinalIgnoreCase: "PAL1CAM1POS1" acha "pal1cam1pos1".
            // Troque para StringComparer.Ordinal se quiser diferenciar maiuscula/minuscula.
            Dictionary<string, Coordenada> mapa =
                new Dictionary<string, Coordenada>(StringComparer.OrdinalIgnoreCase);

            string linha;
            int numeroLinha = 0;
            char separador = '\0';
            SeparadorDetectado = ';';

            while ((linha = leitor.ReadLine()) != null)
                {
                numeroLinha++;

                if (linha.Length > 0 && numeroLinha == 1 && linha[0] == '﻿')
                    linha = linha.Substring(1);                  // BOM sobrando

                string limpa = linha.Trim();
                if (limpa.Length == 0) continue;                 // linha vazia
                if (limpa[0] == '#' || limpa.StartsWith("//")) continue;   // comentario

                if (separador == '\0')
                    {
                    separador = DetectarSeparador(limpa);
                    SeparadorDetectado = separador;
                    }

                string[] campos = limpa.Split(separador);
                if (campos.Length < 5)
                    {
                    Registrar(numeroLinha, "esperava 5 colunas, veio " + campos.Length, linha);
                    continue;
                    }

                string chave = campos[0].Trim().Trim('"');
                if (chave.Length == 0)
                    {
                    Registrar(numeroLinha, "chave vazia", linha);
                    continue;
                    }

                float x, y, z, c;
                if (!NumeroParser.TryParseFloat(campos[1], out x) ||
                    !NumeroParser.TryParseFloat(campos[2], out y) ||
                    !NumeroParser.TryParseFloat(campos[3], out z) ||
                    !NumeroParser.TryParseFloat(campos[4], out c))
                    {
                    // Cabecalho: primeira linha nao numerica e simplesmente pulada.
                    if (numeroLinha == 1) continue;

                    Registrar(numeroLinha, "valor numerico invalido", linha);
                    continue;
                    }

                Coordenada coord = new Coordenada(x, y, z, c);

                if (mapa.ContainsKey(chave))
                    {
                    Registrar(numeroLinha, "chave duplicada \"" + chave + "\" (sobrescrevendo)", linha);
                    mapa[chave] = coord;      // ultima ocorrencia vence
                    }
                else
                    {
                    mapa.Add(chave, coord);
                    }
                }

            return mapa;
            }

        private void Registrar(int numeroLinha, string motivo, string conteudo)
            {
            string msg = string.Format(CultureInfo.InvariantCulture,
                "Linha {0}: {1} -> {2}", numeroLinha, motivo, conteudo);

            if (LancarExcecaoEmLinhaInvalida)
                throw new FormatException(msg);

            _erros.Add(msg);
            }

        private static char DetectarSeparador(string linha)
            {
            char[] candidatos = new char[] { ';', '\t', '|', ',' };
            char melhor = ';';
            int maior = -1;

            foreach (char cand in candidatos)
                {
                int n = 0;
                for (int i = 0; i < linha.Length; i++)
                    {
                    if (linha[i] == cand) n++;
                    }
                if (n > maior)
                    {
                    maior = n;
                    melhor = cand;
                    }
                }

            return melhor;
            }
        }
    }