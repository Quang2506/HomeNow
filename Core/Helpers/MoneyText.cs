using System;
using System.Globalization;
using System.Threading;
using Core.Resources;

namespace Core.Helpers
{
    public static class MoneyText
    {
        /// <summary>
        /// Giá dạng ngắn theo ngôn ngữ: "25 triệu", "2.5 tỷ"
        /// Rent thì thêm suffix: "/tháng" | "/month" | "/月"
        /// </summary>
        public static string ToPriceShort(decimal amount, string listingType)
        {
            var baseText = ToVndShort(amount);

            if (!string.IsNullOrWhiteSpace(listingType) &&
                listingType.Equals("rent", StringComparison.OrdinalIgnoreCase))
            {
                return baseText + MoneyTexts.Suffix_PerMonth;
            }

            return baseText;
        }

        /// <summary>
        /// Chỉ format số + đơn vị (triệu/tỷ) theo UI culture.
        /// </summary>
        public static string ToVndShort(decimal amount)
        {
            var numberCulture = Thread.CurrentThread.CurrentCulture ?? CultureInfo.GetCultureInfo("vi-VN");
            var uiCulture = Thread.CurrentThread.CurrentUICulture ?? CultureInfo.GetCultureInfo("vi-VN");

            bool isZh = uiCulture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase);
            string sep = isZh ? "" : " ";

            // âm (nếu có)
            if (amount < 0) return "-" + ToVndShort(Math.Abs(amount));

            // < 1 triệu: in số đầy đủ
            if (amount < 1_000_000m)
                return amount.ToString("N0", numberCulture);

            // >= 1 tỷ
            if (amount >= 1_000_000_000m)
                return FormatUnit(amount / 1_000_000_000m, MoneyTexts.Unit_Billion, sep, numberCulture);

            // còn lại: triệu
            return FormatUnit(amount / 1_000_000m, MoneyTexts.Unit_Million, sep, numberCulture);
        }

        private static string FormatUnit(decimal value, string unit, string sep, CultureInfo culture)
        {
            // < 10 và có phần lẻ => 1 chữ số thập phân (vd 2.5 tỷ), còn lại làm tròn 0 (vd 25 triệu)
            int decimals = (value < 10m && value != Math.Truncate(value)) ? 1 : 0;

            var rounded = Math.Round(value, decimals, MidpointRounding.AwayFromZero);
            string text = rounded.ToString(decimals == 0 ? "N0" : "N1", culture);

            // bỏ đuôi ".0" hoặc ",0"
            if (decimals == 1)
            {
                var decSep = culture.NumberFormat.NumberDecimalSeparator;
                var tail = decSep + "0";
                if (text.EndsWith(tail, StringComparison.Ordinal))
                    text = text.Substring(0, text.Length - tail.Length);
            }

            return text + sep + unit;
        }
    }
}
