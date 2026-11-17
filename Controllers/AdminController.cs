using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QL_ShopGiay.Data;
using QL_ShopGiay.Helpers;
using QL_ShopGiay.Models.Entities;
using QL_ShopGiay.Models.ViewModels;

namespace QL_ShopGiay.Controllers
{
    [AdminAuthorize]
    public class AdminController : Controller
    {
        private readonly ShopGiayDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AdminController(ShopGiayDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // =====================================================================
        // 5. THỐNG KÊ DOANH THU & TỔNG QUAN (DASHBOARD)
        // =====================================================================
        public async Task<IActionResult> Index()
        {
            var vm = await BuildDashboardViewModelAsync();
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Revenue()
        {
            var vm = await BuildDashboardViewModelAsync();
            return View("Index", vm);
        }

        private async Task<AdminDashboardViewModel> BuildDashboardViewModelAsync()
        {
            var allOrders = await _context.HoaDons
                .Include(h => h.KhachHang)
                .Include(h => h.KhuyenMai)
                .Include(h => h.QuanTriVien)
                .Include(h => h.ChiTietHoaDons)
                .OrderByDescending(h => h.NgayLap)
                .ToListAsync();

            var validOrders = allOrders.Where(h => h.TrangThai != "Đã hủy").ToList();

            var lowStockVariants = await _context.BienTheSanPhams
                .Include(b => b.SanPham)
                .Include(b => b.MauSac)
                .Include(b => b.KichCo)
                .Where(b => b.SoLuongTon <= 8)
                .OrderBy(b => b.SoLuongTon)
                .Take(10)
                .ToListAsync();

            var totalLowStockCount = await _context.BienTheSanPhams.CountAsync(b => b.SoLuongTon <= 8);

            // Tính doanh thu 7 ngày gần nhất (phục vụ biểu đồ theo ngày)
            var today = DateTime.Today;
            var nhanNgay = new List<string>();
            var doanhThuNgay = new List<decimal>();

            for (int i = 6; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                nhanNgay.Add(date.ToString("dd/MM"));
                var rev = validOrders
                    .Where(h => h.NgayLap.Date == date)
                    .Sum(h => h.TongTien);
                doanhThuNgay.Add(rev);
            }

            // Tính doanh thu 12 tháng trong năm hiện tại (phục vụ biểu đồ theo tháng)
            int currentYear = today.Year;
            var nhanThang = new List<string>();
            var doanhThuThang = new List<decimal>();

            for (int m = 1; m <= 12; m++)
            {
                nhanThang.Add($"Tháng {m}");
                var revMonth = validOrders
                    .Where(h => h.NgayLap.Year == currentYear && h.NgayLap.Month == m)
                    .Sum(h => h.TongTien);
                doanhThuThang.Add(revMonth);
            }

            // Tính Top sản phẩm bán chạy nhất từ ChiTietHoaDon
            var orderDetails = await _context.ChiTietHoaDons
                .Include(ct => ct.HoaDon)
                .Include(ct => ct.BienTheSanPham)
                    .ThenInclude(b => b!.SanPham)
                        .ThenInclude(s => s!.DanhMuc)
                .Where(ct => ct.HoaDon != null && ct.HoaDon.TrangThai != "Đã hủy" && ct.BienTheSanPham != null && ct.BienTheSanPham.SanPham != null)
                .ToListAsync();

            var topProducts = orderDetails
                .GroupBy(ct => ct.BienTheSanPham!.SanPham!)
                .Select(g => new TopSellingProductDto
                {
                    MaSanPham = g.Key.MaSanPham,
                    TenSanPham = g.Key.TenSanPham,
                    ThuongHieu = g.Key.ThuongHieu ?? "StrideLab",
                    HinhAnh = g.Key.HinhAnh ?? "",
                    TenDanhMuc = g.Key.DanhMuc?.TenDanhMuc ?? "",
                    TongSoLuongBan = g.Sum(x => x.SoLuong),
                    TongDoanhThu = g.Sum(x => x.SoLuong * x.DonGia)
                })
                .OrderByDescending(x => x.TongSoLuongBan)
                .ThenByDescending(x => x.TongDoanhThu)
                .Take(6)
                .ToList();

            return new AdminDashboardViewModel
            {
                TongDoanhThu = validOrders.Sum(h => h.TongTien),
                DoanhThuHoanThanh = allOrders.Where(h => h.TrangThai == "Hoàn thành").Sum(h => h.TongTien),
                TongDonHang = allOrders.Count,
                DonChoXacNhan = allOrders.Count(h => h.TrangThai == "Chờ xác nhận"),
                DonDangGiao = allOrders.Count(h => h.TrangThai == "Đang giao"),
                DonHoanThanh = allOrders.Count(h => h.TrangThai == "Hoàn thành"),
                DonDaHuy = allOrders.Count(h => h.TrangThai == "Đã hủy"),
                TongSanPham = await _context.SanPhams.CountAsync(),
                TongKhachHang = await _context.KhachHangs.CountAsync(),
                SoBienTheSapHetHang = totalLowStockCount,
                NhanNgay = nhanNgay,
                DoanhThuTheoNgay = doanhThuNgay,
                NhanThang = nhanThang,
                DoanhThuTheoThang = doanhThuThang,
                TopSanPhamBanChay = topProducts,
                BienTheSapHetHang = lowStockVariants,
                DonHangMoiNhat = allOrders.Take(8).ToList()
            };
        }

        // =====================================================================
        // 1. QUẢN LÝ LOẠI SẢN PHẨM (CATEGORIES)
        // =====================================================================
        [HttpGet]
        public async Task<IActionResult> Categories()
        {
            var categories = await _context.DanhMucs
                .Include(d => d.DanhMucCha)
                .Include(d => d.DanhMucCons)
                .Include(d => d.SanPhams)
                .OrderBy(d => d.MaDanhMucCha ?? d.MaDanhMuc)
                .ThenBy(d => d.MaDanhMucCha.HasValue ? 1 : 0)
                .ThenBy(d => d.MaDanhMuc)
                .ToListAsync();

            return View(categories);
        }

        [HttpGet]
        public async Task<IActionResult> CreateCategory()
        {
            var vm = new CategoryFormViewModel
            {
                DanhSachDanhMucCha = await _context.DanhMucs
                    .Where(d => d.MaDanhMucCha == null)
                    .OrderBy(d => d.MaDanhMuc)
                    .ToListAsync()
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(CategoryFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.DanhSachDanhMucCha = await _context.DanhMucs
                    .Where(d => d.MaDanhMucCha == null)
                    .OrderBy(d => d.MaDanhMuc)
                    .ToListAsync();
                return View(model);
            }

            var dm = new DanhMuc
            {
                TenDanhMuc = model.TenDanhMuc.Trim(),
                MaDanhMucCha = model.MaDanhMucCha,
                MoTa = model.MoTa?.Trim()
            };

            _context.DanhMucs.Add(dm);
            await _context.SaveChangesAsync();

            TempData["ToastSuccess"] = $"Đã thêm loại sản phẩm mới: {dm.TenDanhMuc}";
            return RedirectToAction(nameof(Categories));
        }

        [HttpGet]
        public async Task<IActionResult> EditCategory(int id)
        {
            var dm = await _context.DanhMucs.FindAsync(id);
            if (dm == null)
            {
                return NotFound();
            }

            var vm = new CategoryFormViewModel
            {
                MaDanhMuc = dm.MaDanhMuc,
                TenDanhMuc = dm.TenDanhMuc,
                MaDanhMucCha = dm.MaDanhMucCha,
                MoTa = dm.MoTa,
                DanhSachDanhMucCha = await _context.DanhMucs
                    .Where(d => d.MaDanhMucCha == null && d.MaDanhMuc != id)
                    .OrderBy(d => d.MaDanhMuc)
                    .ToListAsync()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(CategoryFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.DanhSachDanhMucCha = await _context.DanhMucs
                    .Where(d => d.MaDanhMucCha == null && d.MaDanhMuc != model.MaDanhMuc)
                    .OrderBy(d => d.MaDanhMuc)
                    .ToListAsync();
                return View(model);
            }

            var dm = await _context.DanhMucs.FindAsync(model.MaDanhMuc);
            if (dm == null)
            {
                return NotFound();
            }

            if (model.MaDanhMucCha == dm.MaDanhMuc)
            {
                ModelState.AddModelError("MaDanhMucCha", "Không thể chọn chính danh mục này làm danh mục cha.");
                model.DanhSachDanhMucCha = await _context.DanhMucs
                    .Where(d => d.MaDanhMucCha == null && d.MaDanhMuc != model.MaDanhMuc)
                    .OrderBy(d => d.MaDanhMuc)
                    .ToListAsync();
                return View(model);
            }

            dm.TenDanhMuc = model.TenDanhMuc.Trim();
            dm.MaDanhMucCha = model.MaDanhMucCha;
            dm.MoTa = model.MoTa?.Trim();

            await _context.SaveChangesAsync();
            TempData["ToastSuccess"] = $"Đã cập nhật loại sản phẩm: {dm.TenDanhMuc}";
            return RedirectToAction(nameof(Categories));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var dm = await _context.DanhMucs
                .Include(d => d.SanPhams)
                .Include(d => d.DanhMucCons)
                .FirstOrDefaultAsync(d => d.MaDanhMuc == id);

            if (dm == null)
            {
                TempData["ToastWarning"] = "Không tìm thấy loại sản phẩm cần xóa.";
                return RedirectToAction(nameof(Categories));
            }

            // Kiểm tra ràng buộc 1: Loại sản phẩm đang chứa sản phẩm
            if (dm.SanPhams.Any())
            {
                TempData["ToastWarning"] = $"Không thể xóa loại '{dm.TenDanhMuc}' vì đang chứa {dm.SanPhams.Count} sản phẩm! Vui lòng chuyển hoặc xóa các sản phẩm thuộc loại này trước.";
                return RedirectToAction(nameof(Categories));
            }

            // Kiểm tra ràng buộc 2: Danh mục cha đang chứa danh mục con
            if (dm.DanhMucCons.Any())
            {
                TempData["ToastWarning"] = $"Không thể xóa danh mục cha '{dm.TenDanhMuc}' vì đang chứa {dm.DanhMucCons.Count} danh mục con trực thuộc!";
                return RedirectToAction(nameof(Categories));
            }

            _context.DanhMucs.Remove(dm);
            await _context.SaveChangesAsync();

            TempData["ToastSuccess"] = $"Đã xóa loại sản phẩm '{dm.TenDanhMuc}' thành công.";
            return RedirectToAction(nameof(Categories));
        }

        // =====================================================================
        // 2. QUẢN LÝ SẢN PHẨM (PRODUCTS)
        // =====================================================================
        [HttpGet]
        public async Task<IActionResult> Products(string? tuKhoa, int? maDanhMuc)
        {
            var query = _context.SanPhams
                .Include(s => s.DanhMuc)
                .Include(s => s.BienTheSanPhams)
                    .ThenInclude(b => b.MauSac)
                .Include(s => s.BienTheSanPhams)
                    .ThenInclude(b => b.KichCo)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                var kw = tuKhoa.Trim();
                query = query.Where(s => s.TenSanPham.Contains(kw) || (s.ThuongHieu != null && s.ThuongHieu.Contains(kw)));
            }

            if (maDanhMuc.HasValue && maDanhMuc.Value > 0)
            {
                query = query.Where(s => s.MaDanhMuc == maDanhMuc.Value || (s.DanhMuc != null && s.DanhMuc.MaDanhMucCha == maDanhMuc.Value));
            }

            ViewBag.DanhSachDanhMuc = await _context.DanhMucs.OrderBy(d => d.MaDanhMuc).ToListAsync();
            ViewBag.TuKhoa = tuKhoa;
            ViewBag.MaDanhMuc = maDanhMuc;

            var products = await query.OrderByDescending(s => s.MaSanPham).ToListAsync();
            return View(products);
        }

        private async Task PopulateProductFormLookupsAsync(ProductFormViewModel vm)
        {
            vm.DanhSachDanhMuc = await _context.DanhMucs
                .Include(d => d.DanhMucCha)
                .OrderBy(d => d.MaDanhMucCha ?? d.MaDanhMuc)
                .ThenBy(d => d.MaDanhMuc)
                .ToListAsync();

            vm.TatCaKichCo = await _context.KichCos.OrderBy(k => k.MaKichCo).ToListAsync();
            vm.TatCaMauSac = await _context.MauSacs.OrderBy(m => m.MaMau).ToListAsync();
        }

        [HttpGet]
        public async Task<IActionResult> CreateProduct()
        {
            var vm = new ProductFormViewModel
            {
                GioiTinh = "Unisex",
                TrangThaiHang = "ConHang",
                NhanNoiBatList = new List<string> { "Moi" },
                SelectedSizeIds = new List<int> { 4, 5, 6, 7 }, // Mặc định chọn sẵn Size 39, 40, 41, 42
                SelectedColorIds = new List<int> { 1, 2 }       // Mặc định chọn sẵn Đen, Trắng
            };
            await PopulateProductFormLookupsAsync(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(ProductFormViewModel model)
        {
            if (model.HinhAnhFile == null && string.IsNullOrWhiteSpace(model.HinhAnhHienTai))
            {
                ModelState.AddModelError("HinhAnhFile", "Vui lòng chọn file hình ảnh sản phẩm để tải lên.");
            }

            if (model.SelectedSizeIds == null || !model.SelectedSizeIds.Any())
            {
                ModelState.AddModelError("SelectedSizeIds", "Vui lòng tích chọn ít nhất 1 Kích cỡ (Size giày).");
            }

            if (model.SelectedColorIds == null || !model.SelectedColorIds.Any())
            {
                ModelState.AddModelError("SelectedColorIds", "Vui lòng tích chọn ít nhất 1 Màu sắc cho sản phẩm.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateProductFormLookupsAsync(model);
                return View(model);
            }

            string imagePath = model.HinhAnhHienTai ?? "/uploads/products/default-shoe.jpg";

            // Xử lý Upload file ảnh vào wwwroot/uploads/products
            if (model.HinhAnhFile != null && model.HinhAnhFile.Length > 0)
            {
                imagePath = await SaveUploadedImageAsync(model.HinhAnhFile);
            }

            var nhanNoiBat = model.NhanNoiBatList != null && model.NhanNoiBatList.Any()
                ? string.Join(",", model.NhanNoiBatList)
                : null;

            var sanPham = new SanPham
            {
                TenSanPham = model.TenSanPham.Trim(),
                MaDanhMuc = model.MaDanhMuc,
                ThuongHieu = model.ThuongHieu.Trim(),
                GiaGoc = model.GiaGoc,
                HinhAnh = imagePath,
                GioiTinh = model.GioiTinh,
                NhanNoiBat = nhanNoiBat,
                MoTa = model.MoTa?.Trim(),
                NgayTao = DateTime.Now
            };

            _context.SanPhams.Add(sanPham);
            await _context.SaveChangesAsync();

            // Tạo các biến thể (BienTheSanPham) từ Checkbox Kích cỡ & Màu sắc
            int stockPerVariant = model.TrangThaiHang == "HetHang" ? 0 : Math.Max(0, model.SoLuongTonMoiBienThe);
            foreach (var colorId in model.SelectedColorIds!.Distinct())
            {
                foreach (var sizeId in model.SelectedSizeIds!.Distinct())
                {
                    var bt = new BienTheSanPham
                    {
                        MaSanPham = sanPham.MaSanPham,
                        MaMau = colorId,
                        MaKichCo = sizeId,
                        SoLuongTon = stockPerVariant,
                        GiaBan = model.GiaBanMacDinh,
                        MaSKU = $"SP{sanPham.MaSanPham}-M{colorId}-S{sizeId}"
                    };
                    _context.BienTheSanPhams.Add(bt);
                }
            }

            await _context.SaveChangesAsync();

            TempData["ToastSuccess"] = $"Đã thêm sản phẩm '{sanPham.TenSanPham}' cùng các biến thể Size/Màu thành công!";
            return RedirectToAction(nameof(Products));
        }

        [HttpGet]
        public async Task<IActionResult> EditProduct(int id)
        {
            var sp = await _context.SanPhams
                .Include(s => s.BienTheSanPhams)
                .FirstOrDefaultAsync(s => s.MaSanPham == id);

            if (sp == null)
            {
                return NotFound();
            }

            var existingSizes = sp.BienTheSanPhams.Select(b => b.MaKichCo).Distinct().ToList();
            var existingColors = sp.BienTheSanPhams.Select(b => b.MaMau).Distinct().ToList();
            var avgStock = sp.BienTheSanPhams.Any() ? (int)Math.Round(sp.BienTheSanPhams.Average(b => b.SoLuongTon)) : 10;
            var minPrice = sp.BienTheSanPhams.Any() ? sp.BienTheSanPhams.Min(b => b.GiaBan) : sp.GiaGoc;

            var vm = new ProductFormViewModel
            {
                MaSanPham = sp.MaSanPham,
                TenSanPham = sp.TenSanPham,
                MaDanhMuc = sp.MaDanhMuc,
                ThuongHieu = sp.ThuongHieu ?? "Nike",
                GiaGoc = sp.GiaGoc,
                GiaBanMacDinh = minPrice,
                SoLuongTonMoiBienThe = avgStock,
                HinhAnhHienTai = sp.HinhAnh,
                GioiTinh = sp.GioiTinh ?? "Unisex",
                TrangThaiHang = sp.TongTonKho > 0 ? "ConHang" : "HetHang",
                NhanNoiBatList = !string.IsNullOrWhiteSpace(sp.NhanNoiBat)
                    ? sp.NhanNoiBat.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList()
                    : new List<string>(),
                SelectedSizeIds = existingSizes,
                SelectedColorIds = existingColors,
                MoTa = sp.MoTa
            };

            await PopulateProductFormLookupsAsync(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(ProductFormViewModel model)
        {
            if (model.SelectedSizeIds == null || !model.SelectedSizeIds.Any())
            {
                ModelState.AddModelError("SelectedSizeIds", "Vui lòng tích chọn ít nhất 1 Kích cỡ (Size giày).");
            }

            if (model.SelectedColorIds == null || !model.SelectedColorIds.Any())
            {
                ModelState.AddModelError("SelectedColorIds", "Vui lòng tích chọn ít nhất 1 Màu sắc cho sản phẩm.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateProductFormLookupsAsync(model);
                return View(model);
            }

            var sp = await _context.SanPhams
                .Include(s => s.BienTheSanPhams)
                    .ThenInclude(b => b.ChiTietHoaDons)
                .FirstOrDefaultAsync(s => s.MaSanPham == model.MaSanPham);

            if (sp == null)
            {
                return NotFound();
            }

            // Nếu có upload ảnh mới thì lưu file mới vào wwwroot/uploads/products
            if (model.HinhAnhFile != null && model.HinhAnhFile.Length > 0)
            {
                sp.HinhAnh = await SaveUploadedImageAsync(model.HinhAnhFile);
            }
            else if (!string.IsNullOrWhiteSpace(model.HinhAnhHienTai))
            {
                sp.HinhAnh = model.HinhAnhHienTai.Trim();
            }

            sp.TenSanPham = model.TenSanPham.Trim();
            sp.MaDanhMuc = model.MaDanhMuc;
            sp.ThuongHieu = model.ThuongHieu.Trim();
            sp.GiaGoc = model.GiaGoc;
            sp.GioiTinh = model.GioiTinh;
            sp.NhanNoiBat = model.NhanNoiBatList != null && model.NhanNoiBatList.Any()
                ? string.Join(",", model.NhanNoiBatList)
                : null;
            sp.MoTa = model.MoTa?.Trim();

            int targetStock = model.TrangThaiHang == "HetHang" ? 0 : Math.Max(0, model.SoLuongTonMoiBienThe);
            var selectedSizes = model.SelectedSizeIds!.Distinct().ToHashSet();
            var selectedColors = model.SelectedColorIds!.Distinct().ToHashSet();

            // Cập nhật hoặc xóa các biến thể hiện có
            foreach (var existingBt in sp.BienTheSanPhams.ToList())
            {
                bool keep = selectedColors.Contains(existingBt.MaMau) && selectedSizes.Contains(existingBt.MaKichCo);
                if (keep)
                {
                    existingBt.GiaBan = model.GiaBanMacDinh;
                    existingBt.SoLuongTon = targetStock;
                }
                else
                {
                    // Nếu biến thể chưa từng phát sinh trong ChiTietHoaDon thì xóa, nếu đã có trong hóa đơn thì đặt tồn kho = 0
                    if (existingBt.ChiTietHoaDons == null || !existingBt.ChiTietHoaDons.Any())
                    {
                        _context.BienTheSanPhams.Remove(existingBt);
                    }
                    else
                    {
                        existingBt.SoLuongTon = 0;
                    }
                }
            }

            // Thêm mới các tổ hợp Màu x Size chưa có
            foreach (var colorId in selectedColors)
            {
                foreach (var sizeId in selectedSizes)
                {
                    bool exists = sp.BienTheSanPhams.Any(b => b.MaMau == colorId && b.MaKichCo == sizeId);
                    if (!exists)
                    {
                        _context.BienTheSanPhams.Add(new BienTheSanPham
                        {
                            MaSanPham = sp.MaSanPham,
                            MaMau = colorId,
                            MaKichCo = sizeId,
                            GiaBan = model.GiaBanMacDinh,
                            SoLuongTon = targetStock,
                            MaSKU = $"SP{sp.MaSanPham}-M{colorId}-S{sizeId}"
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();
            TempData["ToastSuccess"] = $"Đã cập nhật sản phẩm '{sp.TenSanPham}' thành công!";
            return RedirectToAction(nameof(Products));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var sp = await _context.SanPhams
                .Include(s => s.BienTheSanPhams)
                    .ThenInclude(b => b.ChiTietHoaDons)
                .FirstOrDefaultAsync(s => s.MaSanPham == id);

            if (sp == null)
            {
                TempData["ToastWarning"] = "Không tìm thấy sản phẩm cần xóa.";
                return RedirectToAction(nameof(Products));
            }

            bool hasOrderHistory = sp.BienTheSanPhams.Any(b => b.ChiTietHoaDons != null && b.ChiTietHoaDons.Any());
            if (hasOrderHistory)
            {
                TempData["ToastWarning"] = $"Không thể xóa sản phẩm '{sp.TenSanPham}' vì đã có lịch sử giao dịch trong Hóa đơn khách hàng! Bạn có thể Sửa trạng thái thành 'Tạm hết hàng'.";
                return RedirectToAction(nameof(Products));
            }

            _context.SanPhams.Remove(sp);
            await _context.SaveChangesAsync();

            TempData["ToastSuccess"] = $"Đã xóa sản phẩm '{sp.TenSanPham}' khỏi hệ thống.";
            return RedirectToAction(nameof(Products));
        }

        private async Task<string> SaveUploadedImageAsync(IFormFile file)
        {
            var uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "products");
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            var ext = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(ext))
            {
                ext = ".jpg";
            }

            var uniqueName = $"shoe_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid():N}"[..28] + ext.ToLowerInvariant();
            var filePath = Path.Combine(uploadFolder, uniqueName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/uploads/products/{uniqueName}";
        }

        // =====================================================================
        // 3. QUẢN LÝ ĐƠN HÀNG (ORDERS)
        // =====================================================================
        [HttpGet]
        public async Task<IActionResult> Orders(string? trangThai, string? tuKhoa)
        {
            var query = _context.HoaDons
                .Include(h => h.KhachHang)
                .Include(h => h.QuanTriVien)
                .Include(h => h.KhuyenMai)
                .Include(h => h.ChiTietHoaDons)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(trangThai))
            {
                query = query.Where(h => h.TrangThai == trangThai);
            }

            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                var kw = tuKhoa.Trim();
                query = query.Where(h =>
                    h.TenNguoiNhan.Contains(kw) ||
                    h.SDTNguoiNhan.Contains(kw) ||
                    h.MaHoaDon.ToString() == kw);
            }

            ViewBag.TrangThai = trangThai;
            ViewBag.TuKhoa = tuKhoa;

            var orders = await query.OrderByDescending(h => h.NgayLap).ToListAsync();
            return View(orders);
        }

        [HttpGet]
        public async Task<IActionResult> OrderDetail(int id)
        {
            var hoaDon = await _context.HoaDons
                .Include(h => h.KhachHang)
                .Include(h => h.QuanTriVien)
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
                return NotFound();
            }

            return View(hoaDon);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(int maHoaDon, string trangThaiMoi, string? returnToDetail)
        {
            var hoaDon = await _context.HoaDons
                .Include(h => h.ChiTietHoaDons)
                    .ThenInclude(ct => ct.BienTheSanPham)
                .FirstOrDefaultAsync(h => h.MaHoaDon == maHoaDon);

            if (hoaDon == null)
            {
                TempData["ToastWarning"] = "Không tìm thấy đơn hàng cần cập nhật.";
                return RedirectToAction(nameof(Orders));
            }

            var trangThaiCu = hoaDon.TrangThai;

            // Nếu chuyển sang "Đã hủy" từ trạng thái khác, hoàn trả lại số lượng tồn kho cho biến thể
            if (trangThaiMoi == "Đã hủy" && trangThaiCu != "Đã hủy")
            {
                foreach (var ct in hoaDon.ChiTietHoaDons)
                {
                    if (ct.BienTheSanPham != null)
                    {
                        ct.BienTheSanPham.SoLuongTon += ct.SoLuong;
                    }
                }
            }
            // Nếu khôi phục đơn từ "Đã hủy" sang trạng thái hoạt động, trừ lại tồn kho
            else if (trangThaiCu == "Đã hủy" && trangThaiMoi != "Đã hủy")
            {
                foreach (var ct in hoaDon.ChiTietHoaDons)
                {
                    if (ct.BienTheSanPham != null)
                    {
                        ct.BienTheSanPham.SoLuongTon = Math.Max(0, ct.BienTheSanPham.SoLuongTon - ct.SoLuong);
                    }
                }
            }

            hoaDon.TrangThai = trangThaiMoi;
            hoaDon.MaAdmin = HttpContext.Session.GetInt32("AdminId");

            await _context.SaveChangesAsync();
            TempData["ToastSuccess"] = $"Đã cập nhật trạng thái đơn hàng #{hoaDon.MaHoaDon} thành '{trangThaiMoi}'!";

            if (!string.IsNullOrEmpty(returnToDetail))
            {
                return RedirectToAction(nameof(OrderDetail), new { id = maHoaDon });
            }
            return RedirectToAction(nameof(Orders));
        }

        // =====================================================================
        // 4. QUẢN LÝ KHUYẾN MÃI (PROMOTIONS)
        // =====================================================================
        [HttpGet]
        public async Task<IActionResult> Promotions()
        {
            var promos = await _context.KhuyenMais
                .Include(k => k.HoaDons)
                .OrderByDescending(k => k.MaKM)
                .ToListAsync();

            return View(promos);
        }

        [HttpGet]
        public IActionResult CreatePromotion()
        {
            return View(new PromotionFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePromotion(PromotionFormViewModel model)
        {
            if (model.NgayKetThuc < model.NgayBatDau)
            {
                ModelState.AddModelError("NgayKetThuc", "Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu.");
            }

            var normalizedCode = model.MaCode?.Trim().ToUpperInvariant() ?? "";
            if (await _context.KhuyenMais.AnyAsync(k => k.MaCode.ToUpper() == normalizedCode))
            {
                ModelState.AddModelError("MaCode", $"Mã khuyến mãi '{normalizedCode}' đã tồn tại trong hệ thống.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var km = new KhuyenMai
            {
                MaCode = normalizedCode,
                TenChuongTrinh = model.TenChuongTrinh.Trim(),
                PhanTramGiam = model.PhanTramGiam,
                NgayBatDau = model.NgayBatDau,
                NgayKetThuc = model.NgayKetThuc,
                TrangThai = model.TrangThai
            };

            _context.KhuyenMais.Add(km);
            await _context.SaveChangesAsync();

            TempData["ToastSuccess"] = $"Đã tạo mã khuyến mãi '{km.MaCode}' (-{km.PhanTramGiam}%) thành công!";
            return RedirectToAction(nameof(Promotions));
        }

        [HttpGet]
        public async Task<IActionResult> EditPromotion(int id)
        {
            var km = await _context.KhuyenMais.FindAsync(id);
            if (km == null)
            {
                return NotFound();
            }

            var vm = new PromotionFormViewModel
            {
                MaKM = km.MaKM,
                MaCode = km.MaCode,
                TenChuongTrinh = km.TenChuongTrinh,
                PhanTramGiam = km.PhanTramGiam,
                NgayBatDau = km.NgayBatDau,
                NgayKetThuc = km.NgayKetThuc,
                TrangThai = km.TrangThai
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPromotion(PromotionFormViewModel model)
        {
            if (model.NgayKetThuc < model.NgayBatDau)
            {
                ModelState.AddModelError("NgayKetThuc", "Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu.");
            }

            var normalizedCode = model.MaCode?.Trim().ToUpperInvariant() ?? "";
            if (await _context.KhuyenMais.AnyAsync(k => k.MaCode.ToUpper() == normalizedCode && k.MaKM != model.MaKM))
            {
                ModelState.AddModelError("MaCode", $"Mã khuyến mãi '{normalizedCode}' đã được sử dụng bởi chương trình khác.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var km = await _context.KhuyenMais.FindAsync(model.MaKM);
            if (km == null)
            {
                return NotFound();
            }

            km.MaCode = normalizedCode;
            km.TenChuongTrinh = model.TenChuongTrinh.Trim();
            km.PhanTramGiam = model.PhanTramGiam;
            km.NgayBatDau = model.NgayBatDau;
            km.NgayKetThuc = model.NgayKetThuc;
            km.TrangThai = model.TrangThai;

            await _context.SaveChangesAsync();
            TempData["ToastSuccess"] = $"Đã cập nhật mã khuyến mãi '{km.MaCode}' thành công!";
            return RedirectToAction(nameof(Promotions));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePromotion(int id)
        {
            var km = await _context.KhuyenMais
                .Include(k => k.HoaDons)
                .FirstOrDefaultAsync(k => k.MaKM == id);

            if (km == null)
            {
                TempData["ToastWarning"] = "Không tìm thấy mã khuyến mãi cần xóa.";
                return RedirectToAction(nameof(Promotions));
            }

            if (km.HoaDons.Any())
            {
                km.TrangThai = false;
                await _context.SaveChangesAsync();
                TempData["ToastWarning"] = $"Mã '{km.MaCode}' đã được áp dụng trong {km.HoaDons.Count} hóa đơn nên không thể xóa vĩnh viễn. Hệ thống đã chuyển sang trạng thái Ngừng kích hoạt.";
                return RedirectToAction(nameof(Promotions));
            }

            _context.KhuyenMais.Remove(km);
            await _context.SaveChangesAsync();

            TempData["ToastSuccess"] = $"Đã xóa mã khuyến mãi '{km.MaCode}' thành công!";
            return RedirectToAction(nameof(Promotions));
        }
    }
}
