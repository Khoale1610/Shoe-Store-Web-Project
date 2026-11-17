using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QL_ShopGiay.Data;
using QL_ShopGiay.Helpers;
using QL_ShopGiay.Models.Entities;
using QL_ShopGiay.Models.ViewModels;

namespace QL_ShopGiay.Controllers
{
    public class CartController : Controller
    {
        private readonly ShopGiayDbContext _context;
        public const string CART_SESSION_KEY = "CART_ITEMS";
        public const string PROMO_SESSION_KEY = "CART_PROMO_CODE";

        public CartController(ShopGiayDbContext context)
        {
            _context = context;
        }

        private List<CartItemViewModel> GetCartItems()
        {
            return HttpContext.Session.GetObject<List<CartItemViewModel>>(CART_SESSION_KEY) ?? new List<CartItemViewModel>();
        }

        private void SaveCartItems(List<CartItemViewModel> items)
        {
            HttpContext.Session.SetObject(CART_SESSION_KEY, items);
        }

        private async Task<CartPageViewModel> BuildCartPageViewModelAsync()
        {
            var items = GetCartItems();

            // Đồng bộ lại tồn kho và giá bán mới nhất từ Database
            if (items.Any())
            {
                var variantIds = items.Select(i => i.MaBienThe).ToList();
                var dbVariants = await _context.BienTheSanPhams
                    .Include(b => b.SanPham)
                    .Include(b => b.MauSac)
                    .Include(b => b.KichCo)
                    .Where(b => variantIds.Contains(b.MaBienThe))
                    .ToDictionaryAsync(b => b.MaBienThe);

                foreach (var item in items.ToList())
                {
                    if (dbVariants.TryGetValue(item.MaBienThe, out var dbVar))
                    {
                        item.DonGia = dbVar.GiaBan;
                        item.SoLuongTon = dbVar.SoLuongTon;
                        if (item.SoLuong > dbVar.SoLuongTon && dbVar.SoLuongTon > 0)
                        {
                            item.SoLuong = dbVar.SoLuongTon;
                        }
                    }
                }
                SaveCartItems(items);
            }

            var now = DateTime.Now;
            var activePromos = await _context.KhuyenMais
                .Where(k => k.TrangThai && k.NgayBatDau <= now && k.NgayKetThuc >= now)
                .OrderBy(k => k.PhanTramGiam)
                .ToListAsync();

            var vm = new CartPageViewModel
            {
                Items = items,
                DanhSachKhuyenMai = activePromos
            };

            var savedPromoCode = HttpContext.Session.GetString(PROMO_SESSION_KEY);
            if (!string.IsNullOrWhiteSpace(savedPromoCode) && items.Any())
            {
                var promo = activePromos.FirstOrDefault(k => k.MaCode.Equals(savedPromoCode, StringComparison.OrdinalIgnoreCase));
                if (promo != null)
                {
                    vm.MaCodeKhuyenMai = promo.MaCode;
                    vm.TenChuongTrinhKM = promo.TenChuongTrinh;
                    vm.PhanTramGiam = promo.PhanTramGiam;
                }
                else
                {
                    HttpContext.Session.Remove(PROMO_SESSION_KEY);
                }
            }

            return vm;
        }

        // Trang Giỏ hàng
        public async Task<IActionResult> Index()
        {
            var vm = await BuildCartPageViewModelAsync();
            return View(vm);
        }

        // API AJAX Thêm vào giỏ hàng
        [HttpPost]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest req)
        {
            if (req == null || (req.MaBienThe <= 0 && req.MaSanPham <= 0))
            {
                return Json(new { success = false, message = "Dữ liệu sản phẩm không hợp lệ." });
            }

            BienTheSanPham? variant = null;

            if (req.MaBienThe > 0)
            {
                variant = await _context.BienTheSanPhams
                    .Include(b => b.SanPham)
                    .Include(b => b.MauSac)
                    .Include(b => b.KichCo)
                    .FirstOrDefaultAsync(b => b.MaBienThe == req.MaBienThe);
            }
            else if (req.MaSanPham > 0)
            {
                variant = await _context.BienTheSanPhams
                    .Include(b => b.SanPham)
                    .Include(b => b.MauSac)
                    .Include(b => b.KichCo)
                    .Where(b => b.MaSanPham == req.MaSanPham && b.SoLuongTon > 0)
                    .OrderBy(b => b.GiaBan)
                    .FirstOrDefaultAsync();
            }

            if (variant == null || variant.SanPham == null)
            {
                return Json(new { success = false, message = "Không tìm thấy biến thể sản phẩm phù hợp hoặc đã hết hàng." });
            }

            if (variant.SoLuongTon <= 0)
            {
                return Json(new { success = false, message = "Phiên bản Màu/Size này hiện đang tạm hết hàng." });
            }

            int qtyToAdd = Math.Max(1, req.SoLuong);
            var cart = GetCartItems();
            var existing = cart.FirstOrDefault(i => i.MaBienThe == variant.MaBienThe);

            if (existing != null)
            {
                if (existing.SoLuong + qtyToAdd > variant.SoLuongTon)
                {
                    existing.SoLuong = variant.SoLuongTon;
                    SaveCartItems(cart);
                    return Json(new
                    {
                        success = true,
                        message = $"Đã cập nhật số lượng tối đa trong kho ({variant.SoLuongTon} đôi) cho {variant.SanPham.TenSanPham}.",
                        cartCount = cart.Sum(i => i.SoLuong),
                        cartTotal = cart.Sum(i => i.ThanhTien)
                    });
                }
                existing.SoLuong += qtyToAdd;
            }
            else
            {
                int finalQty = Math.Min(qtyToAdd, variant.SoLuongTon);
                cart.Add(new CartItemViewModel
                {
                    MaBienThe = variant.MaBienThe,
                    MaSanPham = variant.MaSanPham,
                    TenSanPham = variant.SanPham.TenSanPham,
                    ThuongHieu = variant.SanPham.ThuongHieu ?? "StrideLab",
                    HinhAnh = variant.SanPham.HinhAnh ?? "",
                    MaMau = variant.MaMau,
                    TenMau = variant.MauSac?.TenMau ?? "",
                    MaHex = variant.MauSac?.MaHex ?? "#111827",
                    MaKichCo = variant.MaKichCo,
                    SoKichCo = variant.KichCo?.SoKichCo ?? "",
                    MaSKU = variant.MaSKU ?? $"SKU-{variant.MaBienThe}",
                    DonGia = variant.GiaBan,
                    SoLuong = finalQty,
                    SoLuongTon = variant.SoLuongTon
                });
            }

            SaveCartItems(cart);

            return Json(new
            {
                success = true,
                message = $"Đã thêm {variant.SanPham.TenSanPham} (Màu {variant.MauSac?.TenMau} - Size {variant.KichCo?.SoKichCo}) vào giỏ hàng!",
                cartCount = cart.Sum(i => i.SoLuong),
                cartTotal = cart.Sum(i => i.ThanhTien)
            });
        }

        // API AJAX Cập nhật số lượng
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity([FromBody] UpdateQuantityRequest req)
        {
            var cart = GetCartItems();
            var item = cart.FirstOrDefault(i => i.MaBienThe == req.MaBienThe);
            if (item == null)
            {
                return Json(new { success = false, message = "Sản phẩm không có trong giỏ hàng." });
            }

            var dbVariant = await _context.BienTheSanPhams.FirstOrDefaultAsync(b => b.MaBienThe == req.MaBienThe);
            int maxStock = dbVariant?.SoLuongTon ?? item.SoLuongTon;

            if (req.SoLuong <= 0)
            {
                cart.Remove(item);
            }
            else if (req.SoLuong > maxStock)
            {
                item.SoLuong = maxStock;
                SaveCartItems(cart);
                var vmLimit = await BuildCartPageViewModelAsync();
                return Json(new
                {
                    success = false,
                    message = $"Kho chỉ còn tối đa {maxStock} sản phẩm cho Size/Màu này.",
                    soLuong = item.SoLuong,
                    thanhTien = item.ThanhTien,
                    tamTinh = vmLimit.TamTinh,
                    tienGiam = vmLimit.TienGiam,
                    tongCong = vmLimit.TongCong,
                    cartCount = vmLimit.Items.Sum(i => i.SoLuong)
                });
            }
            else
            {
                item.SoLuong = req.SoLuong;
            }

            SaveCartItems(cart);
            var vm = await BuildCartPageViewModelAsync();
            var updatedItem = vm.Items.FirstOrDefault(i => i.MaBienThe == req.MaBienThe);

            return Json(new
            {
                success = true,
                soLuong = updatedItem?.SoLuong ?? 0,
                thanhTien = updatedItem?.ThanhTien ?? 0,
                tamTinh = vm.TamTinh,
                tienGiam = vm.TienGiam,
                tongCong = vm.TongCong,
                cartCount = vm.Items.Sum(i => i.SoLuong)
            });
        }

        // API AJAX Xóa 1 sản phẩm khỏi giỏ
        [HttpPost]
        public async Task<IActionResult> RemoveItem([FromBody] RemoveItemRequest req)
        {
            var cart = GetCartItems();
            var item = cart.FirstOrDefault(i => i.MaBienThe == req.MaBienThe);
            if (item != null)
            {
                cart.Remove(item);
                SaveCartItems(cart);
            }

            var vm = await BuildCartPageViewModelAsync();
            return Json(new
            {
                success = true,
                message = "Đã xóa sản phẩm khỏi giỏ hàng.",
                tamTinh = vm.TamTinh,
                tienGiam = vm.TienGiam,
                tongCong = vm.TongCong,
                cartCount = vm.Items.Sum(i => i.SoLuong)
            });
        }

        // API AJAX Áp mã khuyến mãi
        [HttpPost]
        public async Task<IActionResult> ApplyVoucher([FromBody] ApplyVoucherRequest req)
        {
            if (string.IsNullOrWhiteSpace(req?.MaCode))
            {
                return Json(new { success = false, message = "Vui lòng nhập mã khuyến mãi." });
            }

            var code = req.MaCode.Trim().ToUpper();
            var now = DateTime.Now;

            var promo = await _context.KhuyenMais
                .FirstOrDefaultAsync(k => k.MaCode.ToUpper() == code && k.TrangThai);

            if (promo == null)
            {
                return Json(new { success = false, message = $"Mã khuyến mãi '{code}' không tồn tại hoặc đã ngừng áp dụng." });
            }

            if (now < promo.NgayBatDau || now > promo.NgayKetThuc)
            {
                return Json(new { success = false, message = $"Mã khuyến mãi '{code}' đã hết hạn sử dụng." });
            }

            HttpContext.Session.SetString(PROMO_SESSION_KEY, promo.MaCode);
            var vm = await BuildCartPageViewModelAsync();

            return Json(new
            {
                success = true,
                message = $"Áp dụng thành công mã {promo.MaCode} (-{promo.PhanTramGiam}%): {promo.TenChuongTrinh}",
                maCode = promo.MaCode,
                tenChuongTrinh = promo.TenChuongTrinh,
                phanTramGiam = promo.PhanTramGiam,
                tamTinh = vm.TamTinh,
                tienGiam = vm.TienGiam,
                tongCong = vm.TongCong
            });
        }

        // API AJAX Hủy mã khuyến mãi
        [HttpPost]
        public async Task<IActionResult> RemoveVoucher()
        {
            HttpContext.Session.Remove(PROMO_SESSION_KEY);
            var vm = await BuildCartPageViewModelAsync();

            return Json(new
            {
                success = true,
                message = "Đã gỡ mã khuyến mãi.",
                tamTinh = vm.TamTinh,
                tienGiam = vm.TienGiam,
                tongCong = vm.TongCong
            });
        }

        // Trang Đặt hàng (Checkout) - GET
        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var cartVm = await BuildCartPageViewModelAsync();
            if (!cartVm.Items.Any())
            {
                TempData["ToastWarning"] = "Giỏ hàng của bạn đang trống. Vui lòng chọn giày trước khi thanh toán!";
                return RedirectToAction("Index", "Product");
            }

            var checkoutVm = new CheckoutViewModel
            {
                Cart = cartVm,
                PhuongThucThanhToan = "COD"
            };

            // Nếu khách hàng đã đăng nhập thì tự động điền sẵn thông tin nhận hàng
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId.HasValue)
            {
                var kh = await _context.KhachHangs.FirstOrDefaultAsync(k => k.MaKhachHang == customerId.Value);
                if (kh != null)
                {
                    checkoutVm.TenNguoiNhan = kh.HoTen;
                    checkoutVm.SDTNguoiNhan = kh.SoDienThoai;
                    checkoutVm.DiaChiGiaoHang = kh.DiaChi ?? "";
                }
            }

            return View(checkoutVm);
        }

        // Xử lý Đặt hàng & Lưu vào Database (Checkout) - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            var cartVm = await BuildCartPageViewModelAsync();
            model.Cart = cartVm;

            if (!cartVm.Items.Any())
            {
                TempData["ToastWarning"] = "Giỏ hàng của bạn đang trống.";
                return RedirectToAction("Index", "Cart");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                int? customerId = HttpContext.Session.GetInt32("CustomerId");
                int? maKM = null;

                if (!string.IsNullOrWhiteSpace(cartVm.MaCodeKhuyenMai))
                {
                    var promo = await _context.KhuyenMais
                        .FirstOrDefaultAsync(k => k.MaCode == cartVm.MaCodeKhuyenMai && k.TrangThai);
                    if (promo != null)
                    {
                        maKM = promo.MaKM;
                    }
                }

                // Kiểm tra và trừ tồn kho từng biến thể
                foreach (var item in cartVm.Items)
                {
                    var variant = await _context.BienTheSanPhams
                        .Include(b => b.SanPham)
                        .FirstOrDefaultAsync(b => b.MaBienThe == item.MaBienThe);

                    if (variant == null || variant.SoLuongTon < item.SoLuong)
                    {
                        await transaction.RollbackAsync();
                        ModelState.AddModelError("", $"Sản phẩm '{item.TenSanPham}' (Size {item.SoKichCo} - {item.TenMau}) hiện không đủ số lượng tồn kho.");
                        return View(model);
                    }

                    variant.SoLuongTon -= item.SoLuong;
                }

                // Tạo bản ghi Hóa Đơn (HoaDon)
                var hoaDon = new HoaDon
                {
                    MaKhachHang = customerId,
                    MaAdmin = null,
                    MaKM = maKM,
                    TenNguoiNhan = model.TenNguoiNhan.Trim(),
                    SDTNguoiNhan = model.SDTNguoiNhan.Trim(),
                    DiaChiGiaoHang = model.DiaChiGiaoHang.Trim(),
                    PhuongThucThanhToan = model.PhuongThucThanhToan,
                    TienGiam = cartVm.TienGiam,
                    TongTien = cartVm.TongCong,
                    TrangThai = "Chờ xác nhận",
                    NgayLap = DateTime.Now
                };

                _context.HoaDons.Add(hoaDon);
                await _context.SaveChangesAsync();

                // Tạo các bản ghi Chi Tiết Hóa Đơn (ChiTietHoaDon)
                foreach (var item in cartVm.Items)
                {
                    var ct = new ChiTietHoaDon
                    {
                        MaHoaDon = hoaDon.MaHoaDon,
                        MaBienThe = item.MaBienThe,
                        SoLuong = item.SoLuong,
                        DonGia = item.DonGia
                    };
                    _context.ChiTietHoaDons.Add(ct);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Xóa giỏ hàng và mã khuyến mãi sau khi đặt thành công
                HttpContext.Session.Remove(CART_SESSION_KEY);
                HttpContext.Session.Remove(PROMO_SESSION_KEY);

                TempData["ToastSuccess"] = $"Đặt hàng thành công! Mã đơn hàng của bạn là #{hoaDon.MaHoaDon}.";
                return RedirectToAction("OrderSuccess", new { id = hoaDon.MaHoaDon });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "Có lỗi xảy ra khi lưu đơn hàng: " + ex.Message);
                return View(model);
            }
        }

        // Trang Xác nhận đơn hàng thành công
        [HttpGet]
        public async Task<IActionResult> OrderSuccess(int id)
        {
            var hoaDon = await _context.HoaDons
                .Include(h => h.KhuyenMai)
                .Include(h => h.ChiTietHoaDons)
                    .ThenInclude(ct => ct.BienTheSanPham)
                        .ThenInclude(b => b!.SanPham)
                .Include(h => h.ChiTietHoaDons)
                    .ThenInclude(ct => ct.BienTheSanPham)
                        .ThenInclude(b => b!.MauSac)
                .Include(h => h.ChiTietHoaDons)
                    .ThenInclude(ct => ct.BienTheSanPham)
                        .ThenInclude(b => b!.KichCo)
                .FirstOrDefaultAsync(h => h.MaHoaDon == id);

            if (hoaDon == null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(hoaDon);
        }
    }

    public class AddToCartRequest
    {
        public int MaBienThe { get; set; }
        public int MaSanPham { get; set; }
        public int SoLuong { get; set; } = 1;
    }

    public class UpdateQuantityRequest
    {
        public int MaBienThe { get; set; }
        public int SoLuong { get; set; }
    }

    public class RemoveItemRequest
    {
        public int MaBienThe { get; set; }
    }

    public class ApplyVoucherRequest
    {
        public string? MaCode { get; set; }
    }
}
