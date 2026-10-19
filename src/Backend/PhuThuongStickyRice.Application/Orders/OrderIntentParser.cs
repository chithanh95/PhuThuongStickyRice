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
            var candidates = (products ?? Enumerable.Empty<Product>())
                .Where(p => p != null)
                .Select(p => new ParseProductCandidate
                {
                    Id = p.Id,
                    Name = p.Name,
                    PriceAmount = p.Price.Amount
                });
            return Parse(text, candidates);
        }

        /// <summary>
        /// Slim projection for chatbot parse: only Id/Name/Price are needed,
        /// so callers can SELECT 3 narrow columns instead of full Product rows.
        /// </summary>
        public class ParseProductCandidate
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
            public decimal PriceAmount { get; set; }
        }

        // Số lượng mỗi dòng món bị kẹp trong khoảng này khi parse từ text tự do.
        private const int MinParsedQuantity = 1;
        private const int MaxParsedQuantity = 99;

        public static ParseOrderResult Parse(string text, IEnumerable<ParseProductCandidate> products)
        {
            var result = new ParseOrderResult();

            if (string.IsNullOrWhiteSpace(text))
            {
                return result;
            }

            var normalized = Normalize(text);
            result.IsOrderIntent = OrderKeywords.Any(k =>
                Regex.IsMatch(normalized, @"\b" + Regex.Escape(k) + @"\b"));

            var candidates = BuildCandidates(products);
            var consumed = new StringBuilder(normalized);

            foreach (var c in candidates)
            {
                if (TryParseCandidate(c, consumed, out var item))
                {
                    result.Items.Add(item);
                    result.IsOrderIntent = true;
                }
            }

            result.UnmatchedText = Regex.Replace(consumed.ToString(), @"\s+", " ").Trim();
            return result;
        }

        /// <summary>
        /// Chuẩn bị danh sách ứng viên: chuẩn hóa tên món, tên dài khớp trước
        /// để "xôi ngũ sắc" thắng "xôi".
        /// </summary>
        private static List<(ParseProductCandidate Product, string Key)> BuildCandidates(
            IEnumerable<ParseProductCandidate> products)
        {
            return (products ?? Enumerable.Empty<ParseProductCandidate>())
                .Where(p => p != null && !string.IsNullOrWhiteSpace(p.Name))
                .Select(p => (Product: p, Key: Normalize(p.Name)))
                .Where(x => !string.IsNullOrWhiteSpace(x.Key))
                // Tên dài khớp trước để "xôi ngũ sắc" thắng "xôi".
                .OrderByDescending(x => x.Key.Length)
                .ToList();
        }

        /// <summary>
        /// Thử khớp một món trong text: parse số lượng, thêm dòng món, rồi xóa
        /// vùng đã khớp khỏi text để tên ngắn hơn không khớp lại nó.
        /// </summary>
        private static bool TryParseCandidate(
            (ParseProductCandidate Product, string Key) candidate,
            StringBuilder consumed,
            out ParsedOrderItem item)
        {
            item = null;

            var pattern = $@"(?<qty>\d+|{string.Join("|", TextNumbers.Keys)})\s*{Regex.Escape(candidate.Key)}|\b{Regex.Escape(candidate.Key)}\b";
            var match = Regex.Match(consumed.ToString(), pattern);
            if (!match.Success)
            {
                return false;
            }

            var qty = ParseQuantity(match.Groups["qty"]);
            item = new ParsedOrderItem
            {
                ProductId = candidate.Product.Id,
                ProductName = candidate.Product.Name,
                Quantity = Math.Max(MinParsedQuantity, Math.Min(MaxParsedQuantity, qty)),
                Price = candidate.Product.PriceAmount
            };

            // Xóa vùng đã khớp (thay bằng khoảng trắng) để tên món ngắn hơn không khớp lại nó.
            for (var i = match.Index; i < match.Index + match.Length && i < consumed.Length; i++)
            {
                consumed[i] = ' ';
            }

            return true;
        }

        /// <summary>Đọc số lượng từ nhóm regex: số ("3") hoặc chữ ("ba"); mặc định 1.</summary>
        private static int ParseQuantity(Group qtyGroup)
        {
            if (!qtyGroup.Success)
            {
                return MinParsedQuantity;
            }

            if (int.TryParse(qtyGroup.Value, out var parsedQuantity))
            {
                return parsedQuantity;
            }

            return TextNumbers.TryGetValue(qtyGroup.Value, out var textQuantity) ? textQuantity : MinParsedQuantity;
        }
    }
}
