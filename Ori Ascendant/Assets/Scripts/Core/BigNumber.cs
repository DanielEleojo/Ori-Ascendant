using System;
using System.Globalization;

namespace OriAscendant.Core
{
    /// <summary>
    /// Immutable large-number value type used for every Àṣẹ quantity in the game.
    /// A value is stored as <c>mantissa * 10^exponent</c> in engineering form:
    /// the exponent is always a multiple of 3 and the magnitude of the mantissa is
    /// in the range [1, 1000) for non-zero values. Zero is the canonical (0, 0).
    /// Values are signed so subtraction is always well defined, even though
    /// gameplay Àṣẹ never drops below zero.
    ///
    /// This type has NO UnityEngine dependency and is unit-tested independently.
    /// Persistence goes through the <see cref="Mantissa"/> / <see cref="Exponent"/>
    /// pair (see SaveData's split fields) rather than serializing the struct directly.
    /// </summary>
    [Serializable]
    public readonly struct BigNumber : IComparable<BigNumber>, IEquatable<BigNumber>
    {
        // Engineering-notation constants. These are intrinsic to the number
        // representation (not game-balance values), so they live here as named
        // constants rather than in a ScriptableObject.
        private const double Base = 1000.0;        // 10^3 grouping
        private const int ExponentStep = 3;        // exponent is always a multiple of this
        private const double MantissaEpsilon = 1e-9; // equality tolerance on the mantissa

        /// <summary>Signed mantissa, magnitude in [1, 1000) for non-zero values; 0 for zero.</summary>
        public double Mantissa { get; }

        /// <summary>Power-of-ten exponent, always a multiple of 3 (0 for zero).</summary>
        public int Exponent { get; }

        public static readonly BigNumber Zero = new BigNumber(0.0, 0);
        public static readonly BigNumber One = new BigNumber(1.0, 0);

        /// <summary>Constructs and normalizes the value <c>mantissa * 10^exponent</c>.</summary>
        public BigNumber(double mantissa, int exponent)
        {
            if (mantissa == 0.0 || double.IsNaN(mantissa) || double.IsInfinity(mantissa))
            {
                Mantissa = 0.0;
                Exponent = 0;
                return;
            }

            double sign = mantissa < 0.0 ? -1.0 : 1.0;
            double m = Math.Abs(mantissa);
            int e = exponent;

            // Fold the exponent's remainder mod 3 into the mantissa so the
            // exponent becomes a multiple of ExponentStep.
            int r = ((e % ExponentStep) + ExponentStep) % ExponentStep; // always non-negative
            if (r != 0)
            {
                m *= Pow10(r);
                e -= r;
            }

            // Bring the mantissa into [1, Base) in steps of ExponentStep.
            while (m >= Base)
            {
                m /= Base;
                e += ExponentStep;
            }
            while (m < 1.0)
            {
                m *= Base;
                e -= ExponentStep;
            }

            Mantissa = sign * m;
            Exponent = e;
        }

        /// <summary>Builds a BigNumber from a plain double (limited to double's range).</summary>
        public static BigNumber FromDouble(double value) => new BigNumber(value, 0);

        /// <summary>
        /// Converts to double for UI ratios and percent displays ONLY — game math
        /// stays in BigNumber. Values beyond double's range saturate to
        /// ±double.MaxValue instead of overflowing to infinity.
        /// </summary>
        public double ToDouble()
        {
            if (IsZero) return 0.0;
            if (Exponent > 300) return Mantissa > 0.0 ? double.MaxValue : double.MinValue;
            return Mantissa * Pow10(Exponent);
        }

        public bool IsZero => Mantissa == 0.0;

        // ---- arithmetic ----

        public static BigNumber operator +(BigNumber a, BigNumber b)
        {
            if (a.IsZero) return b;
            if (b.IsZero) return a;

            // Work against the larger exponent; the smaller addend is scaled down
            // and naturally underflows to ~0 when the magnitude gap is large.
            if (a.Exponent < b.Exponent)
            {
                BigNumber tmp = a; a = b; b = tmp;
            }
            int diff = a.Exponent - b.Exponent;
            double alignedB = b.Mantissa * Pow10(-diff);
            return new BigNumber(a.Mantissa + alignedB, a.Exponent);
        }

        public static BigNumber operator -(BigNumber a, BigNumber b) => a + (-b);

        public static BigNumber operator -(BigNumber a) => new BigNumber(-a.Mantissa, a.Exponent);

        public static BigNumber operator *(BigNumber a, BigNumber b)
        {
            if (a.IsZero || b.IsZero) return Zero;
            return new BigNumber(a.Mantissa * b.Mantissa, a.Exponent + b.Exponent);
        }

        public static BigNumber operator /(BigNumber a, BigNumber b)
        {
            if (b.IsZero) throw new DivideByZeroException("BigNumber division by zero.");
            if (a.IsZero) return Zero;
            return new BigNumber(a.Mantissa / b.Mantissa, a.Exponent - b.Exponent);
        }

        // Scalar helpers for plain multipliers and elapsed-second counts.
        public static BigNumber operator *(BigNumber a, double scalar) => a * FromDouble(scalar);
        public static BigNumber operator *(double scalar, BigNumber a) => a * FromDouble(scalar);

        // ---- comparison ----

        public int CompareTo(BigNumber other)
        {
            int sa = Math.Sign(Mantissa);
            int sb = Math.Sign(other.Mantissa);
            if (sa != sb) return sa.CompareTo(sb);
            if (sa == 0) return 0;

            if (Exponent != other.Exponent)
            {
                int byExponent = Exponent.CompareTo(other.Exponent);
                return sa > 0 ? byExponent : -byExponent; // larger exponent = larger magnitude
            }
            return Mantissa.CompareTo(other.Mantissa);
        }

        public static bool operator <(BigNumber a, BigNumber b) => a.CompareTo(b) < 0;
        public static bool operator >(BigNumber a, BigNumber b) => a.CompareTo(b) > 0;
        public static bool operator <=(BigNumber a, BigNumber b) => a.CompareTo(b) <= 0;
        public static bool operator >=(BigNumber a, BigNumber b) => a.CompareTo(b) >= 0;
        public static bool operator ==(BigNumber a, BigNumber b) => a.Equals(b);
        public static bool operator !=(BigNumber a, BigNumber b) => !a.Equals(b);

        public bool Equals(BigNumber other)
        {
            if (Exponent != other.Exponent) return false;
            return Math.Abs(Mantissa - other.Mantissa) <= MantissaEpsilon;
        }

        public override bool Equals(object obj) => obj is BigNumber other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Math.Round(Mantissa, 6), Exponent);

        // ---- formatting ----

        public override string ToString()
        {
            if (IsZero) return "0";

            string sign = Mantissa < 0.0 ? "-" : string.Empty;
            double am = Math.Abs(Mantissa);

            // Sub-unit values: print the plain decimal.
            if (Exponent < 0)
            {
                double val = am * Pow10(Exponent);
                return sign + val.ToString("0.####", CultureInfo.InvariantCulture);
            }

            // Suffixed range: plain, K, M, B, T.
            string suffix = SuffixFor(Exponent);
            if (suffix != null)
            {
                return sign + am.ToString("0.##", CultureInfo.InvariantCulture) + suffix;
            }

            // Beyond T: scientific. Convert engineering mantissa [1,1000) to [1,10).
            double d = am;
            int sci = Exponent;
            if (d >= 100.0) { d /= 100.0; sci += 2; }
            else if (d >= 10.0) { d /= 10.0; sci += 1; }
            return sign + d.ToString("0.00", CultureInfo.InvariantCulture) + "e" + sci;
        }

        private static string SuffixFor(int exponent)
        {
            switch (exponent)
            {
                case 0: return string.Empty;
                case 3: return "K";
                case 6: return "M";
                case 9: return "B";
                case 12: return "T";
                default: return null; // beyond T -> caller uses scientific
            }
        }

        private static double Pow10(int n) => Math.Pow(10.0, n);
    }
}
