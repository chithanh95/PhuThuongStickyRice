using PhuThuongStickyRice.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PhuThuongStickyRice.Application.Orders
{
    /// <summary>
    /// One parsed dish line: {productId, productName, quantity, price}.
    /// </summary>
    public class ParsedOrderItem
    {
        public Guid ProductId { get; set; }

        public string ProductName { get; set; }

        public int Quantity { get; set; }

        public decimal Price { get; set; }
    }

    public class ParseOrderResult
    {
        public bool IsOrderIntent { get; set; }

        public List<ParsedOrderItem> Items { get; set; } = new List<ParsedOrderItem>();

        public string UnmatchedText { get; set; }
    }

    /// <summary>
    /// Parses Vietnamese free text (chatbot, voice) into dish lines (items 61, 80).
    /// Pure static logic — the caller supplies the product catalog; no order is
    /// created here, the frontend confirms before calling the real order API.
    /// </summary>
    public static class OrderIntentParser
    {
        /// <summary>Normalized (no diacritics) order keywords.</summary>
        private static readonly string[] OrderKeywords = { "dat", "mua", "lay", "cho", "order", "ship", "giao" };

        /// <summary>
        /// Vietnamese textual numbers, normalized (no diacritics).
        /// </summary>
        private static readonly Dictionary<string, int> TextNumbers = new Dictionary<string, int>
        {
            { "mot", 1 }, { "hai", 2 }, { "ba", 3 }, { "bon", 4 }, { "nam", 5 },
            { "sau", 6 }, { "bay", 7 }, { "tam", 8 }, { "chin", 9 }, { "muoi", 10 }
        };

        /// <summary>Lowercase + strip Vietnamese diacritics so "xôi gấc" matches "xoi gac".</summary>
        public static string Normalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var normalized = text.ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);
            foreach (var c in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(c);
                if (category != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        public static ParseOrderResult Parse(string text, IEnumerable<Product> products)
        {
            var result = new ParseOrderResult();

            if (string.IsNullOrWhiteSpace(text))
            {
                return result;
            }

            var normalized = Normalize(text);
            result.IsOrderIntent = OrderKeywords.Any(k =>
                Regex.IsMatch(normalized, @"\b" + Regex.Escape(k) + @"\b"));

            var candidates = (products ?? Enumerable.Empty<Product>())
                .Where(p => p != null && !string.IsNullOrWhiteSpace(p.Name))
                .Select(p => new { Product = p, Key = Normalize(p.Name) })
                .Where(x => !string.IsNullOrWhiteSpace(x.Key))
                // Longest names first so "xôi ngũ sắc" wins over "xôi".
                .OrderByDescending(x => x.Key.Length)
                .ToList();

            var consumed = new StringBuilder(normalized);

            foreach (var c in candidates)
            {
                var pattern = $@"(?<qty>\d+|{string.Join("|", TextNumbers.Keys)})\s*{Regex.Escape(c.Key)}|\b{Regex.Escape(c.Key)}\b";
                var match = Regex.Match(consumed.ToString(), pattern);
                if (!match.Success)
                {
                    continue;
                }

                var qty = 1;
                var qtyGroup = match.Groups["qty"];
                if (qtyGroup.Success)
                {
                    if (int.TryParse(qtyGroup.Value, out var n))
                    {
                        qty = n;
                    }
                    else if (TextNumbers.TryGetValue(qtyGroup.Value, out var t))
                    {
                        qty = t;
                    }
                }

                result.Items.Add(new ParsedOrderItem
                {
                    ProductId = c.Product.Id,
                    ProductName = c.Product.Name,
                    Quantity = Math.Max(1, Math.Min(99, qty)),
                    Price = c.Product.Price.Amount
                });

                // Blank out the matched span so shorter names can't re-match it.
                for (var i = match.Index; i < match.Index + match.Length && i < consumed.Length; i++)
                {
                    consumed[i] = ' ';
                }

                result.IsOrderIntent = true;
            }

            result.UnmatchedText = Regex.Replace(consumed.ToString(), @"\s+", " ").Trim();
            return result;
        }
    }
}
