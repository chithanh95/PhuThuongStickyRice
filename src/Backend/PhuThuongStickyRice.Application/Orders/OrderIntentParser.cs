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
    /// Một dòng món đã parse được: {productId, productName, quantity, price}.
    /// </summary>
    public class ParsedOrderItem
    {
        /// <summary>Id sản phẩm trong DB.</summary>
        public Guid ProductId { get; set; }

        /// <summary>Tên sản phẩm như hiển thị trong menu.</summary>
        public string ProductName { get; set; }

        /// <summary>Số lượng (đã kẹp trong khoảng 1–99 ở bước parse).</summary>
        public int Quantity { get; set; }

        /// <summary>Giá đơn vị tại thời điểm parse (dùng để tính thử tổng tiền).</summary>
        public decimal Price { get; set; }
    }

    /// <summary>
    /// Kết quả parse một đoạn chat/giọng nói thành ý định đặt món.
    /// </summary>
    public class ParseOrderResult
    {
        /// <summary>True khi text có từ khóa đặt hàng ("đặt", "mua", "ship"...) hoặc khớp được ít nhất 1 món.</summary>
        public bool IsOrderIntent { get; set; }

        /// <summary>Các dòng món khớp được với catalog.</summary>
        public List<ParsedOrderItem> Items { get; set; } = new List<ParsedOrderItem>();

        /// <summary>Phần text không khớp món nào (để hiển thị lại cho khách sửa/bổ sung).</summary>
        public string UnmatchedText { get; set; }
    }

    /// <summary>
    /// Parse text tiếng Việt tự do (chatbot, voice) thành các dòng món (item 61, 80).
    /// Logic tĩnh thuần túy — caller cung cấp catalog sản phẩm; không tạo đơn ở đây,
    /// frontend xác nhận với khách trước khi gọi API tạo đơn thật.
    /// </summary>
    public static partial class OrderIntentParser
    {
        // codequality-team 2026-10-20 run-1135 Agent3-BestPractices:
        // [GeneratedRegex] biên dịch regex ngay lúc build (source generator),
        // không tốn Regex.Compile ở runtime và không phụ thuộc cache động
        // 15-entry của Regex.IsMatch/Regex.Match static — cùng convention với
        // MoMoPaymentService, MessengerChatService, PhoneNumber.
        // Từ khóa toàn chữ cái thường (không có ký tự regex đặc biệt, đã bỏ
        // dấu) nên gộp 7 pattern "\b<kw>\b" thành 1 alternation là tương đương
        // ngữ nghĩa với vòng Any() cũ (dat, mua, lay, cho, order, ship, giao).
        [GeneratedRegex(@"\b(dat|mua|lay|cho|order|ship|giao)\b")]
        private static partial Regex OrderKeywordRegex();

        // Thu gọn khoảng trắng thừa trong phần text không khớp món nào.
        [GeneratedRegex(@"\s+")]
        private static partial Regex WhitespaceRegex();

        /// <summary>
        /// Vietnamese textual numbers, normalized (no diacritics).
        /// </summary>
        private static readonly Dictionary<string, int> TextNumbers = new Dictionary<string, int>
        {
            { "mot", 1 }, { "hai", 2 }, { "ba", 3 }, { "bon", 4 }, { "nam", 5 },
            { "sau", 6 }, { "bay", 7 }, { "tam", 8 }, { "chin", 9 }, { "muoi", 10 }
        };

        // codequality-team 2026-10-20 run-1135 Agent3-BestPractices:
        // Chuỗi alternation "mot|hai|...|muoi" dựng 1 lần ở static init thay vì
        // string.Join lại cho mỗi ứng viên trong mỗi lần parse (vòng lặp
        // foreach candidate trong Parse). Giá trị giống hệt vì cùng 1 nguồn.
        private static readonly string TextNumberAlternation = string.Join("|", TextNumbers.Keys);

        /// <summary>
        /// Chuẩn hóa text: viết thường + bỏ dấu tiếng Việt để "xôi gấc" khớp với "xoi gac".
        /// </summary>
        /// <param name="text">Text gốc do khách nhập/nói.</param>
        /// <returns>Text đã chuẩn hóa (không dấu, chữ thường).</returns>
        public static string Normalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var normalized = text.ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);
            foreach (var ch in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (category != UnicodeCategory.NonSpacingMark)
                {
                    // 'đ' (U+0111) không có phân rã FormD nên rớt qua vòng lọc dấu.
                    // Không map tay thì "đặt" -> "đat" không khớp keyword "dat",
                    // và tên món chứa đ (vd "Xôi Đậu") không khớp khi khách gõ
                    // không dấu "xoi dau". (bug-hunt 2026-10-20)
                    sb.Append(ch == 'đ' ? 'd' : ch);
                }
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        /// <summary>
        /// Parse text thành các dòng món từ catalog đầy đủ (entity Product).
        /// </summary>
        /// <param name="text">Text tiếng Việt tự do ("2 xôi gấc, 1 xôi xéo").</param>
        /// <param name="products">Catalog sản phẩm để đối chiếu tên món.</param>
        /// <returns>Kết quả parse: ý định đặt hàng, các dòng món, phần text không khớp.</returns>
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
        /// Chiếu gọn cho luồng chatbot: chỉ cần Id/Tên/Giá nên caller có thể
        /// SELECT 3 cột hẹp thay vì nạp toàn bộ dòng Product.
        /// </summary>
        public class ParseProductCandidate
        {
            /// <summary>Id sản phẩm.</summary>
            public Guid Id { get; set; }

            /// <summary>Tên món như trong menu (dùng để đối chiếu tên).</summary>
            public string Name { get; set; }

            /// <summary>Giá đơn vị (để tính thử tổng tiền khi parse).</summary>
            public decimal PriceAmount { get; set; }
        }

        // Số lượng mỗi dòng món bị kẹp trong khoảng này khi parse từ text tự do.
        private const int MinParsedQuantity = 1;
        private const int MaxParsedQuantity = 99;

        /// <summary>
        /// Parse text thành các dòng món từ danh sách ứng viên gọn nhẹ
        /// (chỉ Id/Tên/Giá — dùng cho luồng chatbot để truy vấn DB ít cột).
        /// </summary>
        /// <param name="text">Text tiếng Việt tự do.</param>
        /// <param name="products">Danh sách ứng viên đã chiếu gọn từ catalog.</param>
        /// <returns>Kết quả parse: ý định đặt hàng, các dòng món, phần text không khớp.</returns>
        public static ParseOrderResult Parse(string text, IEnumerable<ParseProductCandidate> products)
        {
            var result = new ParseOrderResult();

            if (string.IsNullOrWhiteSpace(text))
            {
                return result;
            }

            var normalized = Normalize(text);
            // 1 lần match duy nhất thay cho 7 lần Regex.IsMatch (mỗi lần 1
            // pattern build động) — regex đã biên dịch sẵn lúc build.
            result.IsOrderIntent = OrderKeywordRegex().IsMatch(normalized);

            var candidates = BuildCandidates(products);
            var consumed = new StringBuilder(normalized);

            foreach (var candidate in candidates)
            {
                if (TryParseCandidate(candidate, consumed, out var item))
                {
                    result.Items.Add(item);
                    result.IsOrderIntent = true;
                }
            }

            result.UnmatchedText = WhitespaceRegex().Replace(consumed.ToString(), " ").Trim();
            return result;
        }

        /// <summary>
        /// Chuẩn bị danh sách ứng viên: chuẩn hóa tên món, tên dài khớp trước
        /// để "xôi ngũ sắc" thắng "xôi".
        /// </summary>
        private static List<(ParseProductCandidate Product, string Key, Regex Matcher)> BuildCandidates(
            IEnumerable<ParseProductCandidate> products)
        {
            // codequality-team 2026-10-20 run-1400 Agent3-BestPractices:
            // Regex.Match(string, pattern) static chỉ cache tối đa 15 pattern —
            // catalog >15 món làm cache thrash, mỗi lần parse lại re-parse
            // pattern từ đầu cho từng ứng viên. Biên dịch 1 lần mỗi ứng viên
            // ở đây (RegexOptions.Compiled, culture không ảnh hưởng vì pattern
            // không dùng IgnoreCase) rồi tái dùng trong TryParseCandidate —
            // semantics match giữ nguyên.
            return (products ?? Enumerable.Empty<ParseProductCandidate>())
                .Where(p => p != null && !string.IsNullOrWhiteSpace(p.Name))
                .Select(p =>
                {
                    var key = Normalize(p.Name);
                    var pattern = $@"(?<qty>\d+|{TextNumberAlternation})\s*{Regex.Escape(key)}|\b{Regex.Escape(key)}\b";
                    return (Product: p, Key: key, Matcher: new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant));
                })
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
            (ParseProductCandidate Product, string Key, Regex Matcher) candidate,
            StringBuilder consumed,
            out ParsedOrderItem item)
        {
            item = null;

            // Regex của ứng viên đã biên dịch sẵn 1 lần ở BuildCandidates.
            var match = candidate.Matcher.Match(consumed.ToString());
            if (!match.Success)
            {
                return false;
            }

            var parsedQuantity = ParseQuantity(match.Groups["qty"]);
            item = new ParsedOrderItem
            {
                ProductId = candidate.Product.Id,
                ProductName = candidate.Product.Name,
                Quantity = Math.Max(MinParsedQuantity, Math.Min(MaxParsedQuantity, parsedQuantity)),
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
        private static int ParseQuantity(Group quantityGroup)
        {
            if (!quantityGroup.Success)
            {
                return MinParsedQuantity;
            }

            if (int.TryParse(quantityGroup.Value, out var parsedQuantity))
            {
                return parsedQuantity;
            }

            return TextNumbers.TryGetValue(quantityGroup.Value, out var textQuantity) ? textQuantity : MinParsedQuantity;
        }
    }
}
