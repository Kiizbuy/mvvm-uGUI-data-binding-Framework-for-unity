using System;
using System.Globalization;
using UnityEngine;

namespace MVVM.Runtime.Converters
{
    [CreateAssetMenu(menuName = "MVVM/Converters/DateTime To String Converter")]
    public class DateTimeToStringConverter : ValueConverterT<DateTime, string>
    {
        public string Format = "G";
        public bool UseInvariantCulture = false;
        public bool ThrowOnParseFailure = false;

        private IFormatProvider Culture => UseInvariantCulture ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture;

        public override string Convert(DateTime source)
        {
            try
            {
                return source.ToString(Format, Culture);
            }
            catch
            {
                return source.ToString(Culture);
            }
        }

        public override DateTime ConvertBack(string target)
        {
            if (string.IsNullOrWhiteSpace(target))
            {
                if (ThrowOnParseFailure)
                    throw new FormatException("Cannot parse empty string to DateTime.");
                return default;
            }

            var culture = (CultureInfo)Culture;

            if (!string.IsNullOrEmpty(Format))
            {
                if (DateTime.TryParseExact(target, Format, culture, DateTimeStyles.None, out var exact))
                    return exact;
                if (DateTime.TryParseExact(target, Format, culture, DateTimeStyles.AllowWhiteSpaces, out exact))
                    return exact;
            }

            if (DateTime.TryParse(target, culture, DateTimeStyles.None, out var parsed))
                return parsed;

            if (DateTime.TryParse(target, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
                return parsed;

            if (ThrowOnParseFailure)
                throw new FormatException($"Cannot parse '{target}' to DateTime with format '{Format}' and cultures.");

            return default;
        }
    }
}
