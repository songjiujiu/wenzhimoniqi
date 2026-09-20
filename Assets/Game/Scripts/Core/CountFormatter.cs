using System;
using System.Globalization;
using System.Numerics;

namespace Mosquito.Core
{
    public static class CountFormatter
    {
        private static readonly string[] Suffix = { "", "K", "M", "B", "T" };
        public static string Format(BigInteger number)
        {
            if (number < 0) return "-" + Format(-number);
            string digits = number.ToString(CultureInfo.InvariantCulture);
            if (digits.Length <= 3) return digits;
            int exponent = digits.Length - 1;
            int significant = int.Parse(digits.Substring(0, 3), CultureInfo.InvariantCulture);
            if (digits[3] >= '5') significant++;
            if (significant == 1000) { significant = 100; exponent++; }
            if (exponent >= 15) return (significant / 100d).ToString("F2", CultureInfo.InvariantCulture) + "e" + exponent;
            int group = exponent / 3;
            int places = 2 - exponent % 3;
            double value = significant * Math.Pow(10, exponent % 3 - 2);
            return value.ToString("F" + places, CultureInfo.InvariantCulture) + Suffix[group];
        }
    }
}
