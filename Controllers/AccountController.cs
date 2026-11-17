using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QL_ShopGiay.Data;
using QL_ShopGiay.Models.Entities;
using QL_ShopGiay.Models.ViewModels;

namespace QL_ShopGiay.Controllers
{
    public class AccountController : Controller
    {
        private readonly ShopGiayDbContext _context;

        public AccountController(ShopGiayDbContext context)
        {
            _context = context;
        }

        // Đăng ký tài khoản Khách hàng - GET
        [HttpGet]
        public IActionResult Register()
        {
            if (HttpContext.Session.GetInt32("CustomerId").HasValue)
            {
                return RedirectToAction("Index", "Home");
            }
            return View(new RegisterViewModel());
        }

        // Đăng ký tài khoản Khách hàng - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var normalizedEmail = model.Email.Trim().ToLower();
            var emailExists = await _context.KhachHangs.AnyAsync(k => k.Email.ToLower() == normalizedEmail);
            if (emailExists)
            {
                ModelState.AddModelError("Email", "Địa chỉ Email này đã được đăng ký. Vui lòng sử dụng Email khác hoặc Đăng nhập.");
                return View(model);
            }

            var phoneExists = await _context.KhachHangs.AnyAsync(k => k.SoDienThoai == model.SoDienThoai.Trim());
            if (phoneExists)
            {
                ModelState.AddModelError("SoDienThoai", "Số điện thoại này đã được liên kết với một tài khoản khác.");
                return View(model);
            }

            var khachHang = new KhachHang
            {
                HoTen = model.HoTen.Trim(),
                Email = normalizedEmail,
                MatKhau = model.MatKhau,
                SoDienThoai = model.SoDienThoai.Trim(),
                DiaChi = model.DiaChi.Trim(),
                NgayTao = DateTime.Now
            };

            _context.KhachHangs.Add(khachHang);
            await _context.SaveChangesAsync();

            // Tự động đăng nhập cho khách hàng vừa tạo tài khoản
            HttpContext.Session.SetInt32("CustomerId", khachHang.MaKhachHang);
            HttpContext.Session.SetString("CustomerName", khachHang.HoTen);
            HttpContext.Session.SetString("CustomerEmail", khachHang.Email);
            HttpContext.Session.SetString("UserRole", "KhachHang");

            TempData["ToastSuccess"] = $"Chào mừng {khachHang.HoTen}! Đăng ký tài khoản thành công.";
            return RedirectToAction("Index", "Home");
        }

        // Đăng nhập (Phân quyền chuyển hướng Admin / Khách hàng) - GET
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        // Đăng nhập (Phân quyền chuyển hướng Admin / Khách hàng) - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var input = model.TaiKhoan.Trim();
            var password = model.MatKhau;

            // 1. Kiểm tra bảng QuanTriVien (Admin / Nhân viên)
            var admin = await _context.QuanTriViens
                .FirstOrDefaultAsync(a =>
                    (a.TenDangNhap == input || (a.Email != null && a.Email == input)) &&
                    a.MatKhau == password);

            if (admin != null)
            {
                if (!admin.TrangThai)
                {
                    ModelState.AddModelError("", "Tài khoản Quản trị viên này hiện đang bị khóa.");
                    return View(model);
                }

                HttpContext.Session.Remove("CustomerId");
                HttpContext.Session.Remove("CustomerName");
                HttpContext.Session.SetInt32("AdminId", admin.MaAdmin);
                HttpContext.Session.SetString("AdminName", admin.HoTen);
                HttpContext.Session.SetString("AdminUsername", admin.TenDangNhap);
                HttpContext.Session.SetString("UserRole", admin.VaiTro); // SuperAdmin hoặc NhanVien
                HttpContext.Session.SetString("Role", "admin");

                TempData["ToastSuccess"] = $"Đăng nhập thành công vào Hệ thống Quản trị ({admin.VaiTro}): {admin.HoTen}";
                return RedirectToAction("Index", "Admin");
            }

            // 2. Kiểm tra bảng KhachHang
            var khachHang = await _context.KhachHangs
                .FirstOrDefaultAsync(k =>
                    (k.Email.ToLower() == input.ToLower() || k.SoDienThoai == input) &&
                    k.MatKhau == password);

            if (khachHang != null)
            {
                HttpContext.Session.Remove("AdminId");
                HttpContext.Session.Remove("AdminName");
                HttpContext.Session.SetInt32("CustomerId", khachHang.MaKhachHang);
                HttpContext.Session.SetString("CustomerName", khachHang.HoTen);
                HttpContext.Session.SetString("CustomerEmail", khachHang.Email);
                HttpContext.Session.SetString("UserRole", "KhachHang");
                HttpContext.Session.SetString("Role", "customer");

                TempData["ToastSuccess"] = $"Xin chào {khachHang.HoTen}! Chúc bạn mua sắm vui vẻ.";

                if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Tài khoản hoặc mật khẩu không chính xác. Vui lòng kiểm tra lại!");
            return View(model);
        }

        // Đăng xuất
        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("CustomerId");
            HttpContext.Session.Remove("CustomerName");
            HttpContext.Session.Remove("CustomerEmail");
            HttpContext.Session.Remove("AdminId");
            HttpContext.Session.Remove("AdminName");
            HttpContext.Session.Remove("AdminUsername");
            HttpContext.Session.Remove("UserRole");
            HttpContext.Session.Remove("Role");

            TempData["ToastSuccess"] = "Bạn đã đăng xuất khỏi hệ thống.";
            return RedirectToAction("Index", "Home");
        }

        // Lịch sử đơn hàng của Khách hàng
        [HttpGet]
        public async Task<IActionResult> Orders()
        {
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (!customerId.HasValue)
            {
                TempData["ToastWarning"] = "Vui lòng đăng nhập để xem lịch sử đơn hàng của bạn.";
                return RedirectToAction("Login", new { returnUrl = "/Account/Orders" });
            }

            var orders = await _context.HoaDons
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
                .Where(h => h.MaKhachHang == customerId.Value)
                .OrderByDescending(h => h.NgayLap)
                .ToListAsync();

            return View(orders);
        }
    }
}
