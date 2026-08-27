using System;
using System.Collections.Generic;
using System.Linq;

namespace fardos30kg_5X8
    {
    /// <summary>
    /// Aplica incrementos (deltas) nas posicoes que atendem ao filtro.
    /// Sem dependencia de UI.
    /// </summary>
    public static class AjustadorPosicoes
        {
        /// <summary>Limite de deslocamento por aplicacao, em mm.</summary>
        public const float LimiteMm = 15f;

        /// <summary>Limite de giro por aplicacao, em graus.</summary>
        public const float LimiteGraus = 15f;

        /// <summary>
        /// Devolve as chaves que o filtro seleciona, em ordem estavel
        /// (palete, camada, posicao). Nao altera nada — serve para o
        /// contador da tela e para destacar as linhas no grid.
        /// </summary>
        public static List<string> Selecionar(
            IEnumerable<string> chaves, FiltroSelecao filtro)
            {
            if (chaves == null) throw new ArgumentNullException("chaves");
            if (filtro == null) throw new ArgumentNullException("filtro");

            // A expressao LINQ faz o trabalho de filtrar e ordenar.
            // O 'let' guarda o parse para nao rodar o Regex duas vezes.
            return (from chave in chaves
                    let par = ParseOuNulo(chave)
                    where par.HasValue && filtro.Corresponde(par.Value)
                    orderby par.Value.Palete, par.Value.Camada, par.Value.Posicao
                    select chave).ToList();
            }

        /// <summary>
        /// Soma os deltas nas posicoes selecionadas. Devolve quantas foram alteradas.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Se algum delta passar do limite permitido por aplicacao.
        /// </exception>
        public static int Aplicar(
            Dictionary<string, Coordenada> posicoes,
            FiltroSelecao filtro,
            float dx, float dy, float dz, float dc)
            {
            if (posicoes == null) throw new ArgumentNullException("posicoes");
            if (filtro == null) throw new ArgumentNullException("filtro");

            ValidarDelta(dx, "X", LimiteMm, "mm");
            ValidarDelta(dy, "Y", LimiteMm, "mm");
            ValidarDelta(dz, "Z", LimiteMm, "mm");
            ValidarDelta(dc, "C", LimiteGraus, "graus");

            if (dx == 0f && dy == 0f && dz == 0f && dc == 0f) return 0;

            // IMPORTANTE: materializa com ToList() ANTES do foreach.
            // Alterar o dicionario enquanto se percorre posicoes.Keys
            // lanca InvalidOperationException ("Collection was modified").
            List<string> alvos = Selecionar(posicoes.Keys, filtro);

            foreach (string chave in alvos)
                {
                Coordenada atual = posicoes[chave];

                // Coordenada e imutavel: cria uma nova em vez de mutar.
                posicoes[chave] = new Coordenada(
                    atual.X + dx,
                    atual.Y + dy,
                    atual.Z + dz,
                    atual.C + dc);
                }

            return alvos.Count;
            }

        /// <summary>
        /// Soma os deltas numa lista de chaves ja escolhida (uniao de varios
        /// paineis, por exemplo). Devolve quantas foram alteradas.
        /// </summary>
        public static int Aplicar(
            Dictionary<string, Coordenada> posicoes,
            IEnumerable<string> chaves,
            float dx, float dy, float dz, float dc)
            {
            if (posicoes == null) throw new ArgumentNullException("posicoes");
            if (chaves == null) throw new ArgumentNullException("chaves");

            ValidarDelta(dx, "X", LimiteMm, "mm");
            ValidarDelta(dy, "Y", LimiteMm, "mm");
            ValidarDelta(dz, "Z", LimiteMm, "mm");
            ValidarDelta(dc, "C", LimiteGraus, "graus");

            if (dx == 0f && dy == 0f && dz == 0f && dc == 0f) return 0;

            // IMPORTANTE: materializa ANTES do foreach. Alterar o dicionario
            // enquanto se percorre posicoes.Keys lanca InvalidOperationException.
            List<string> alvos = new List<string>(chaves);
            int alteradas = 0;

            foreach (string chave in alvos)
                {
                Coordenada atual;
                if (!posicoes.TryGetValue(chave, out atual)) continue;

                // Coordenada e imutavel: cria uma nova em vez de mutar.
                posicoes[chave] = new Coordenada(
                    atual.X + dx, atual.Y + dy, atual.Z + dz, atual.C + dc);

                alteradas++;
                }

            return alteradas;
            }

        private static void ValidarDelta(float valor, string eixo, float limite, string unidade)
            {
            if (Math.Abs(valor) > limite)
                {
                throw new ArgumentOutOfRangeException(
                    "delta" + eixo,
                    "O incremento em " + eixo + " (" + valor + ") passa do limite de " +
                    limite + " " + unidade + " por aplicacao.");
                }
            }

        private static ChavePosicao? ParseOuNulo(string chave)
            {
            ChavePosicao ch;
            return ChavePosicao.TryParse(chave, out ch) ? (ChavePosicao?)ch : null;
            }
        }
    }