using PhuThuongStickyRice.Application.Common;
using PhuThuongStickyRice.Application.Shop;
using PhuThuongStickyRice.Application.Shop.DTOs;
using PhuThuongStickyRice.Application.Subscriptions.DTOs;
using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Repositories;
using PhuThuongStickyRice.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;

namespace PhuThuongStickyRice.Application.Subscriptions
{
    /// <summary>
    /// Hợp đồng dịch vụ gói đăng ký (subscription): khách tạo gói món ăn
    /// định kỳ, giá chốt theo snapshot catalog lúc tạo (không bị ảnh hưởng
    /// khi giá món đổi sau), tra cứu và quản lý gói của mình.
    /// </summary>
    public interface ISubscriptionAppService : IApplicationService
    {
        /// <summary>Public: customer creates a daily subscription.</summary>
        Task<SubscriptionDto> CreateAsync(CreateSubscriptionDto dto);

        /// <summary>Admin: subscription list.</summary>
        Task<List<SubscriptionDto>> GetListAsync(bool? activeOnly);

        /// <summary>Admin: pause / resume / cancel a subscription.</summary>
        Task<SubscriptionDto> SetActiveAsync(Guid id, bool active);

        /// <summary>
        /// Internal: called by the morning worker — creates a Pending COD
        /// order for every active subscription due today. Returns the number
        /// of orders created.
        /// </summary>
        Task<int> CreateDueOrdersAsync();
    }

    /// <summary>
    /// Gói đăng ký xôi theo ngày: khách chọn món cố định, mỗi sáng worker gọi
    /// <see cref="CreateDueOrdersAsync"/> để sinh đơn COD Pending cho mọi gói
    /// đang hoạt động đến hạn hôm nay. Đơn lưu snapshot giá/tên món tại thời
    /// điểm tạo — giá bán thay đổi sau đó không ảnh hưởng các đơn đã sinh.
    /// </summary>
    public class SubscriptionAppService : ApplicationService, ISubscriptionAppService
    {
        private readonly ISubscriptionRepository _subscriptions;
        private readonly IOrderRepository _orders;
        private readonly IProductRepository _products;
        private readonly Volo.Abp.Domain.Repositories.IRepository<ProductSizeOption, Guid> _sizeOptions;
        private readonly Volo.Abp.Domain.Repositories.IRepository<Topping, Guid> _toppings;
        private readonly IAsyncQueryableExecuter _asyncExecuter;
        private readonly IShopSettingAppService _shopSettings;

        /// <summary>
        /// Số dòng tối đa khi admin xem danh sách gói — không materialize
        /// toàn bộ lịch sử gói đăng ký về memory.
        /// </summary>
        private const int MaxAdminListRows = 200;

        /// <summary>Khởi tạo dịch vụ gói đăng ký với các repository và service liên quan.</summary>
        public SubscriptionAppService(
            ISubscriptionRepository subscriptions,
            IOrderRepository orders,
            IProductRepository products,
            Volo.Abp.Domain.Repositories.IRepository<ProductSizeOption, Guid> sizeOptions,
            Volo.Abp.Domain.Repositories.IRepository<Topping, Guid> toppings,
            IAsyncQueryableExecuter asyncExecuter,
            IShopSettingAppService shopSettings)
        {
            _subscriptions = subscriptions;
            _orders = orders;
            _products = products;
            _sizeOptions = sizeOptions;
            _toppings = toppings;
            _asyncExecuter = asyncExecuter;
            _shopSettings = shopSettings;
        }

        /// <summary>
        /// Public: khách tạo gói đăng ký xôi theo ngày. Snapshot giá/tên món
        /// từ catalog hiện tại (size thay giá gốc, topping cộng thêm tiền) —
        /// vì gói cố định giá lúc đăng ký, các đơn sau này dùng giá này chứ
        /// không cập nhật theo giá catalog mới.
        /// Bảo mật: gói tạo từ kênh công khai vào trạng thái CHỜ DUYỆT
        /// (<c>Active = false</c>) — trước đây Active=true ngay khiến bất kỳ
        /// ai biết SĐT người khác cũng đăng ký hộ được và hệ thống tự sinh
        /// đơn COD giao tới địa chỉ lạ mỗi ngày. Admin kiểm tra rồi kích
        /// hoạt (SetActiveAsync) thì worker mới sinh đơn.
        /// </summary>
        /// <param name="dto">Thông tin gói: SĐT, món, size, topping, ngày bắt đầu...</param>
        /// <returns>Gói đăng ký vừa tạo.</returns>
        /// <exception cref="Domain.Exceptions.DomainException">Ném khi món/size/topping không khả dụng hoặc ngày bắt đầu trong quá khứ.</exception>
        [AllowAnonymous]
        public virtual async Task<SubscriptionDto> CreateAsync(CreateSubscriptionDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            var (productName, unitPrice, sizeName, toppingNames) =
                await ResolveSnapshotAsync(dto.ProductId, dto.SizeOptionId, dto.ToppingIds);

            var start = dto.StartDate.Date;
            // So sánh với ngày hôm nay theo giờ Việt Nam (UTC+7), không phải
            // UTC: từ 00:00 đến 06:59 giờ VN, UtcNow.Date vẫn là ngày hôm qua
            // nên gói đăng ký cho "hôm nay" bị từ chối sai là "trong quá khứ".
            if (start < VietnamTime.Today())
            {
                throw new Domain.Exceptions.DomainException("Ngày bắt đầu không thể trong quá khứ.");
            }

            var entity = new Subscription
            {
                Phone = dto.Phone.Trim(),
                CustomerName = dto.CustomerName.Trim(),
                ProductId = dto.ProductId,
                ProductName = productName,
                SizeName = sizeName,
                ToppingNames = toppingNames,
                UnitPrice = unitPrice,
                Quantity = dto.Quantity,
                DeliveryAddress = dto.DeliveryAddress.Trim(),
                StartDate = start,
                // Chờ duyệt: gói công khai KHÔNG tự kích hoạt (xem doc của
                // method) — admin kích hoạt mới bắt đầu sinh đơn tự động.
                Active = false,
                Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
                CreatedAt = DateTime.UtcNow
            };
            await _subscriptions.InsertAsync(entity, autoSave: true);
            return MapToDto(entity);
        }

        /// <summary>
        /// Admin: danh sách gói đăng ký, mới nhất trước (tối đa
        /// <see cref="MaxAdminListRows"/> dòng cho màn hình admin; không
        /// materialize toàn bộ lịch sử).
        /// </summary>
        /// <param name="activeOnly">true: chỉ gói đang hoạt động; false: chỉ gói đã tắt; null: tất cả.</param>
        /// <returns>Danh sách gói đăng ký.</returns>
        [Authorize(Roles = "Admin")]
        public virtual async Task<List<SubscriptionDto>> GetListAsync(bool? activeOnly)
        {
            var queryable = await _subscriptions.GetQueryableAsync();
            IQueryable<Subscription> query = queryable.OrderByDescending(x => x.CreatedAt);

            if (activeOnly.HasValue)
            {
                query = query.Where(x => x.Active == activeOnly.Value);
            }

            var items = await _asyncExecuter.ToListAsync(query.Take(MaxAdminListRows));
            return items.Select(MapToDto).ToList();
        }

        /// <summary>
        /// Admin: tạm ngưng / kích hoạt lại gói đăng ký (soft toggle, giữ
        /// nguyên lịch sử — xóa hẳn chỉ khi khách hủy tài khoản).
        /// </summary>
        /// <param name="id">Id gói đăng ký.</param>
        /// <param name="active">true: kích hoạt; false: tạm ngưng.</param>
        /// <returns>Gói sau khi đổi trạng thái.</returns>
        [Authorize(Roles = "Admin")]
        public virtual async Task<SubscriptionDto> SetActiveAsync(Guid id, bool active)
        {
            var entity = await _subscriptions.GetAsync(id);
            entity.Active = active;
            await _subscriptions.UpdateAsync(entity, autoSave: true);
            return MapToDto(entity);
        }

        /// <summary>
        /// Internal: worker buổi sáng gọi — sinh 1 đơn COD Pending cho mỗi gói
        /// đang hoạt động đến hạn hôm nay (StartDate ≤ hôm nay và chưa sinh đơn
        /// trong ngày). Điều kiện LastOrderDate loại đơn trùng nếu worker chạy
        /// lặp, nên method này idempotent trong cùng một ngày.
        /// </summary>
        /// <returns>Số đơn đã sinh thành công.</returns>
        public virtual async Task<int> CreateDueOrdersAsync()
        {
            // Worker chạy buổi sáng theo giờ VN — dùng ngày VN để không bỏ
            // sót gói bắt đầu "hôm nay" trong khung 00:00-06:59 giờ VN.
            var today = VietnamTime.Today();
            var settings = await _shopSettings.GetAsync();

            var queryable = await _subscriptions.GetQueryableAsync();
            var due = await _asyncExecuter.ToListAsync(
                queryable.Where(x =>
                    x.Active
                    && x.StartDate <= today
                    && (x.LastOrderDate == null || x.LastOrderDate < today)));

            var created = 0;
            // perf-hunt 2026-10-19: generate all order numbers with a single
            // batched existence check (also dedupes within the batch, which
            // the old per-subscription check missed) instead of one DB
            // round-trip per subscription.
            var orderNumbers = await GenerateUniqueOrderNumbersAsync(due.Count);
            var numberIndex = 0;
            foreach (var sub in due)
            {
                // Claim nguyên tử TRƯỚC khi sinh đơn: 2 lần chạy đồng thời
                // (worker 6h sáng + admin bấm tay) cùng thấy gói "đến hạn" —
                // chỉ bên claim thành công được sinh đơn, chống trùng 2 đơn
                // COD cùng ngày cho 1 gói (khách bị giao 2 lần/quán đối soát
                // sai). Claim tham gia UoW nên nếu InsertAsync phía dưới lỗi,
                // claim cũng rollback và lần chạy sau làm lại.
                if (!await _subscriptions.TryClaimDueAsync(sub.Id, today))
                {
                    continue;
                }

                var order = BuildDueOrder(sub, orderNumbers[numberIndex++], settings);

                await _orders.InsertAsync(order);
                created++;
            }

            return created;
        }

        /// <summary>
        /// Dựng một đơn COD định kỳ từ gói đăng ký (snapshot giá/tên tại
        /// thời điểm tạo; phí ship theo cài đặt hiện tại). Tách ra để
        /// CreateDueOrdersAsync ngắn gọn hơn (logic giữ nguyên).
        /// </summary>
        private static Order BuildDueOrder(Subscription sub, string orderNumber, ShopSettingDto settings)
        {
            var lines = new List<(Guid ProductId, string ProductName, Money UnitPrice, int Quantity,
                string SizeName, string ToppingNames, decimal ToppingsExtra, bool IsCombo, string Note,
                Guid? SizeOptionId, List<Guid> ToppingIds)>
            {
                // Gói đăng ký chỉ snapshot TÊN size/topping (entity
                // Subscription không lưu id) nên 2 id để trống như đơn cũ.
                (sub.ProductId, sub.ProductName, Money.Of(sub.UnitPrice, Money.DefaultCurrency),
                    sub.Quantity, sub.SizeName, sub.ToppingNames, 0, false, null, null, null)
            };

            var subtotal = sub.UnitPrice * sub.Quantity;
            // Miễn ship cho đơn định kỳ khi đủ ngưỡng như đơn đặt tay trên
            // checkout — khách đăng ký gói phải được hưởng chính sách freeship
            // chung, không tính riêng.
            var fee = settings.DeliveryFee > 0
                && !(settings.FreeShipThreshold > 0 && subtotal >= settings.FreeShipThreshold)
                ? settings.DeliveryFee
                : 0;

            return Order.Create(
                orderNumber,
                sub.CustomerName,
                sub.Phone,
                null,
                sub.DeliveryAddress,
                "Đơn định kỳ tự động" + (string.IsNullOrWhiteSpace(sub.Notes) ? "" : " — " + sub.Notes),
                PaymentMethod.Cod,
                lines,
                Money.Of(fee, Money.DefaultCurrency));
        }

        /// <summary>
        /// Resolves the product/price snapshot for a new subscription from
        /// the current catalog (size replaces base price, toppings add extra).
        /// </summary>
        private async Task<(string ProductName, decimal UnitPrice, string SizeName, string ToppingNames)>
            ResolveSnapshotAsync(Guid productId, Guid? sizeOptionId, List<Guid> toppingIds)
        {
            var productQueryable = await _products.GetQueryableAsync();
            // Best practice: truyền predicate thẳng vào FirstOrDefaultAsync
            // thay vì bọc queryable.Where(...) thừa (tránh 1 wrapper LINQ).
            var product = await _asyncExecuter.FirstOrDefaultAsync(
                productQueryable, x => x.Id == productId);
            if (product == null || product.Status != ProductStatus.Active)
            {
                throw new Domain.Exceptions.DomainException("Món ăn không tồn tại hoặc đã ngừng bán.");
            }

            string sizeName = null;
            var unitPrice = product.Price.Amount;
            if (sizeOptionId.HasValue)
            {
                var sizeQueryable = await _sizeOptions.GetQueryableAsync();
                var size = await _asyncExecuter.FirstOrDefaultAsync(
                    sizeQueryable, x => x.Id == sizeOptionId.Value);
                if (size == null || !size.IsActive || size.ProductId != product.Id)
                {
                    throw new Domain.Exceptions.DomainException("Size không khả dụng.");
                }

                sizeName = size.Name;
                unitPrice = size.Price;
            }

            string toppingNames = null;
            var ids = (toppingIds ?? new List<Guid>()).Distinct().ToList();
            if (ids.Count > 0)
            {
                var toppingQueryable = await _toppings.GetQueryableAsync();
                var resolved = await _asyncExecuter.ToListAsync(
                    toppingQueryable.Where(x => ids.Contains(x.Id) && x.IsActive));
                if (resolved.Count != ids.Count)
                {
                    throw new Domain.Exceptions.DomainException("Một topping đã chọn không còn khả dụng.");
                }

                toppingNames = string.Join(", ", resolved.Select(x => x.Name));
                unitPrice += resolved.Sum(x => x.ExtraPrice);
            }

            return (product.Name, unitPrice, sizeName, toppingNames);
        }

        // Mã đơn dạng PX-yyyyMMdd-NNNN: 4 chữ số ngẫu nhiên, chống trùng trong
        // batch bằng HashSet, chống trùng DB bằng 1 query theo chunk
        // (chunk 1000 để dưới giới hạn tham số của SQL Server).
        private const int OrderNumberQueryChunkSize = 1000;

        /// <summary>Số lần thử sinh lại tối đa khi mã ứng viên trùng với DB.</summary>
        private const int MaxOrderNumberAttempts = 10;

        /// <summary>
        /// Generates <paramref name="count"/> unique order numbers of the
        /// form PX-yyyyMMdd-NNNN. Numbers are deduped within the batch via a
        /// HashSet and DB collisions are detected with a single batched query
        /// per attempt (chunked at 1000 to stay under SQL Server's parameter
        /// limit); only colliding numbers are regenerated.
        /// </summary>
        private async Task<List<string>> GenerateUniqueOrderNumbersAsync(int count)
        {
            // Mã đơn mang ngày VN (khớp VietnamTime.Today() dùng ở
            // CreateDueOrdersAsync); dùng UtcNow sẽ ghi nhầm ngày hôm trước
            // khi worker chạy trong khung 00:00-06:59 giờ VN. (bug-hunt 2026-10-20)
            var day = VietnamTime.Today().ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var numbers = BuildCandidateNumbers(count, day, used);

            for (var attempt = 0; attempt < MaxOrderNumberAttempts; attempt++)
            {
                var existing = await FindExistingOrderNumbersAsync(numbers);
                if (existing.Count == 0)
                {
                    return numbers;
                }

                RegenerateCollidingNumbers(numbers, used, existing, day);
            }

            throw new Domain.Exceptions.DomainException("Could not generate a unique order number.");
        }

        /// <summary>Sinh <paramref name="count"/> mã ứng viên, đảm bảo không trùng nhau trong batch.</summary>
        private static List<string> BuildCandidateNumbers(int count, string day, HashSet<string> used)
        {
            var numbers = new List<string>(count);
            for (var i = 0; i < count; i++)
            {
                numbers.Add(NewUniqueOrderNumber(day, used));
            }

            return numbers;
        }

        /// <summary>Sinh 1 mã PX-yyyyMMdd-NNNN chưa có trong <paramref name="used"/>.</summary>
        private static string NewUniqueOrderNumber(string day, HashSet<string> used)
        {
            // CA5394: Random là RNG không an toàn mật mã — mã phiếu xuất là
            // chứng từ nên dùng RandomNumberGenerator (CSPRNG, thread-safe).
            // Vòng do/while bên dưới vẫn đảm bảo duy nhất như cũ.
            string number;
            do
            {
                number = $"PX-{day}-{RandomNumberGenerator.GetInt32(1000, 9999)}";
            } while (!used.Add(number));

            return number;
        }

        /// <summary>Tìm trong DB các mã ứng viên đã tồn tại (query theo chunk để dưới giới hạn tham số của SQL Server).</summary>
        private async Task<HashSet<string>> FindExistingOrderNumbersAsync(List<string> numbers)
        {
            var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var queryable = await _orders.GetQueryableAsync();
            foreach (var chunk in numbers.Chunk(OrderNumberQueryChunkSize))
            {
                var found = await _asyncExecuter.ToListAsync(
                    queryable.Where(x => chunk.Contains(x.OrderNumber)).Select(x => x.OrderNumber));
                foreach (var number in found)
                {
                    existing.Add(number);
                }
            }

            return existing;
        }

        /// <summary>Sinh lại các mã bị trùng DB, giữ nguyên các mã đã OK.</summary>
        private static void RegenerateCollidingNumbers(
            List<string> numbers, HashSet<string> used, HashSet<string> existing, string day)
        {
            for (var i = 0; i < numbers.Count; i++)
            {
                if (existing.Contains(numbers[i]))
                {
                    used.Remove(numbers[i]);
                    numbers[i] = NewUniqueOrderNumber(day, used);
                }
            }
        }

        private static SubscriptionDto MapToDto(Subscription entity)
        {
            return new SubscriptionDto
            {
                Id = entity.Id,
                Phone = entity.Phone,
                CustomerName = entity.CustomerName,
                ProductId = entity.ProductId,
                ProductName = entity.ProductName,
                SizeName = entity.SizeName,
                ToppingNames = entity.ToppingNames,
                UnitPrice = entity.UnitPrice,
                Quantity = entity.Quantity,
                DeliveryAddress = entity.DeliveryAddress,
                StartDate = entity.StartDate,
                Active = entity.Active,
                Notes = entity.Notes,
                LastOrderDate = entity.LastOrderDate,
                CreatedAt = entity.CreatedAt
            };
        }
    }
}
