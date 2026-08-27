using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace fardos30kg_5X8
    {
    /// <summary>
    /// Representa uma posicao no espaco lida da planilha: X, Y, Z e C (rotacao/giro).
    /// </summary>
    public class Coordenada
        {
        private readonly float _x;
        private readonly float _y;
        private readonly float _z;
        private readonly float _c;

        public Coordenada(float x, float y, float z, float c)
            {
            _x = x;
            _y = y;
            _z = z;
            _c = c;
            }

        public float X { get { return _x; } }
        public float Y { get { return _y; } }
        public float Z { get { return _z; } }
        public float C { get { return _c; } }

        public override string ToString()
            {
            return string.Format(CultureInfo.InvariantCulture,
                "X={0} Y={1} Z={2} C={3}", _x, _y, _z, _c);
            }

        }
    }