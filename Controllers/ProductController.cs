using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QL_ShopGiay.Data;
using QL_ShopGiay.Models.ViewModels;

namespace QL_ShopGiay.Controllers
{
    public class ProductController : Controller
    {
        private readonly ShopGiayDbContext _context;

        public ProductController(ShopGiayDbContext context)
        {
            _context = context;
        }

        // Danh sách sản phẩm + Bộ lọc + Tìm kiếm
        public async Task<IActionResult> Index(
            int? maDanhMuc,
            string? mucGia,
            string? thuongHieu,
            string? kichCo,
            string? gioiTinh,
            string? nhanNoiBat,
            string? tuKhoa,
            string? sapXep)
        {
            var danhMucCha = await _context.DanhMucs
                .Include(d => d.DanhMucCons)
                .Where(d => d.MaDanhMucCha == null)
                .OrderBy(d => d.MaDanhMuc)
                .ToListAsync();

            var thuongHieus = await _context.SanPhams
                .Where(s => s.ThuongHieu != null && s.ThuongHieu != "")
                .Select(s => s.ThuongHieu!)
                .Distinct()
                .OrderBy(t => t)
                .ToListAsync();

            var kichCos = await _context.KichCos
                .OrderBy(k => k.MaKichCo)
                .Select(k => k.SoKichCo)
                .ToListAsync();

            var query = _context.SanPhams
                .Include(s => s.DanhMuc)
                    .ThenInclude(d => d!.DanhMucCha)
                .Include(s => s.BienTheSanPhams)
                    .ThenInclude(b => b.MauSac)
                .Include(s => s.BienTheSanPhams)
                    .ThenInclude(b => b.KichCo)
                .AsQueryable();

            string? tenDanhMucHienTai = null;
            string? moTaDanhMucHienTai = null;

            // Lọc theo Danh mục (hỗ trợ cả Danh mục cha lẫn Danh mục con)
            if (maDanhMuc.HasValue && maDanhMuc.Value > 0)
            {
                var dm = await _context.DanhMucs
                    .Include(d => d.DanhMucCons)
                    .FirstOrDefaultAsync(d => d.MaDanhMuc == maDanhMuc.Value);

                if (dm != null)
                {
                    tenDanhMucHienTai = dm.TenDanhMuc;
                    moTaDanhMucHienTai = dm.MoTa;

                    if (dm.MaDanhMucCha == null)
                    {
                        var childIds = dm.DanhMucCons.Select(c => c.MaDanhMuc).ToList();
                        childIds.Add(dm.MaDanhMuc);
                        query = query.Where(s => childIds.Contains(s.MaDanhMuc));
                    }
                    else
                    {
                        query = query.Where(s => s.MaDanhMuc == dm.MaDanhMuc);
                    }
                }
            }

            // Tìm kiếm theo từ khóa
            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                var kw = tuKhoa.Trim();
                query = query.Where(s =>
                    s.TenSanPham.Contains(kw) ||
                    (s.ThuongHieu != null && s.ThuongHieu.Contains(kw)) ||
                    (s.DanhMuc != null && s.DanhMuc.TenDanhMuc.Contains(kw)) ||
                    (s.MoTa != null && s.MoTa.Contains(kw)));
            }

            // Lọc theo Thương hiệu
            if (!string.IsNullOrWhiteSpace(thuongHieu))
            {
                query = query.Where(s => s.ThuongHieu == thuongHieu);
            }

            // Lọc theo Kích cỡ (Size giày)
            if (!string.IsNullOrWhiteSpace(kichCo))
            {
                query = query.Where(s => s.BienTheSanPhams.Any(b => b.KichCo != null && b.KichCo.SoKichCo == kichCo && b.SoLuongTon > 0));
            }

            // Lọc theo Giới tính
            if (!string.IsNullOrWhiteSpace(gioiTinh))
            {
                query = query.Where(s => s.GioiTinh == gioiTinh);
            }

            // Lọc theo Nhãn nổi bật (Mới, Bán chạy, Giảm giá)
            if (!string.IsNullOrWhiteSpace(nhanNoiBat))
            {
                query = query.Where(s => s.NhanNoiBat != null && s.NhanNoiBat.Contains(nhanNoiBat));
            }

            var products = await query.ToListAsync();

            // Lọc theo Mức giá (dựa trên giá bán thấp nhất của biến thể hoặc giá gốc)
            if (!string.IsNullOrWhiteSpace(mucGia))
            {
                products = mucGia switch
                {
                    "duoi-2tr" => products.Where(s => s.GiaThapNhat < 2000000).ToList(),
                    "2tr-3tr" => products.Where(s => s.GiaThapNhat >= 2000000 && s.GiaThapNhat <= 3000000).ToList(),
                    "3tr-4tr" => products.Where(s => s.GiaThapNhat > 3000000 && s.GiaThapNhat <= 4000000).ToList(),
                    "tren-4tr" => products.Where(s => s.GiaThapNhat > 4000000).ToList(),
                    _ => products
                };
            }

            // Sắp xếp
            products = sapXep switch
            {
                "gia-tang" => products.OrderBy(s => s.GiaThapNhat).ToList(),
                "gia-giam" => products.OrderByDescending(s => s.GiaThapNhat).ToList(),
                "ten-az" => products.OrderBy(s => s.TenSanPham).ToList(),
                _ => products.OrderByDescending(s => s.MaSanPham).ToList()
            };

            var vm = new ProductListViewModel
            {
                SanPhams = products,
                DanhMucCha = danhMucCha,
                ThuongHieus = thuongHieus,
                KichCos = kichCos,
                MaDanhMuc = maDanhMuc,
                TenDanhMucHienTai = tenDanhMucHienTai,
                MoTaDanhMucHienTai = moTaDanhMucHienTai,
                MucGia = mucGia,
                ThuongHieu = thuongHieu,
                KichCo = kichCo,
                GioiTinh = gioiTinh,
                NhanNoiBat = nhanNoiBat,
                TuKhoa = tuKhoa,
                SapXep = sapXep,
                TongSoSanPham = products.Count
            };

            return View(vm);
        }

        // Chi tiết sản phẩm + Chọn Size/Màu + Sản phẩm liên quan
        public async Task<IActionResult> Detail(int id)
        {
            var sanPham = await _context.SanPhams
                .Include(s => s.DanhMuc)
                    .ThenInclude(d => d!.DanhMucCha)
                .Include(s => s.BienTheSanPhams)
                    .ThenInclude(b => b.MauSac)
                .Include(s => s.BienTheSanPhams)
                    .ThenInclude(b => b.KichCo)
                .FirstOrDefaultAsync(s => s.MaSanPham == id);

            if (sanPham == null)
            {
                return NotFound();
            }

            var danhSachMau = sanPham.BienTheSanPhams
                .Where(b => b.MauSac != null)
                .Select(b => b.MauSac!)
                .GroupBy(m => m.MaMau)
                .Select(g => g.First())
                .OrderBy(m => m.MaMau)
                .ToList();

            var danhSachKichCo = sanPham.BienTheSanPhams
                .Where(b => b.KichCo != null)
                .Select(b => b.KichCo!)
                .GroupBy(k => k.MaKichCo)
                .Select(g => g.First())
                .OrderBy(k => k.MaKichCo)
                .ToList();

            var bienTheJson = sanPham.BienTheSanPhams
                .Select(b => new VariantDto
                {
                    MaBienThe = b.MaBienThe,
                    MaMau = b.MaMau,
                    TenMau = b.MauSac?.TenMau ?? "",
                    MaHex = b.MauSac?.MaHex ?? "#111827",
                    MaKichCo = b.MaKichCo,
                    SoKichCo = b.KichCo?.SoKichCo ?? "",
                    SoLuongTon = b.SoLuongTon,
                    GiaBan = b.GiaBan,
                    MaSKU = b.MaSKU ?? $"SKU-{b.MaBienThe}"
                })
                .ToList();

            // Lấy sản phẩm liên quan cùng danh mục hoặc cùng danh mục cha
            var parentId = sanPham.DanhMuc?.MaDanhMucCha ?? sanPham.MaDanhMuc;
            var relatedProducts = await _context.SanPhams
                .Include(s => s.DanhMuc)
                .Include(s => s.BienTheSanPhams)
                    .ThenInclude(b => b.MauSac)
                .Include(s => s.BienTheSanPhams)
                    .ThenInclude(b => b.KichCo)
                .Where(s => s.MaSanPham != sanPham.MaSanPham &&
                            (s.MaDanhMuc == sanPham.MaDanhMuc ||
                             (s.DanhMuc != null && s.DanhMuc.MaDanhMucCha == parentId)))
                .OrderByDescending(s => s.MaDanhMuc == sanPham.MaDanhMuc)
                .ThenByDescending(s => s.MaSanPham)
                .Take(4)
                .ToListAsync();

            var vm = new ProductDetailViewModel
            {
                SanPham = sanPham,
                DanhSachMau = danhSachMau,
                DanhSachKichCo = danhSachKichCo,
                BienTheJson = bienTheJson,
                SanPhamLienQuan = relatedProducts
            };

            return View(vm);
        }

        // API AJAX gợi ý tìm kiếm nhanh trên thanh Header
        [HttpGet]
        public async Task<IActionResult> SearchSuggest(string? q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return Json(Array.Empty<object>());
            }

            var kw = q.Trim();
            var matches = await _context.SanPhams
                .Include(s => s.DanhMuc)
                .Include(s => s.BienTheSanPhams)
                .Where(s => s.TenSanPham.Contains(kw) ||
                            (s.ThuongHieu != null && s.ThuongHieu.Contains(kw)) ||
                            (s.DanhMuc != null && s.DanhMuc.TenDanhMuc.Contains(kw)))
                .OrderByDescending(s => s.MaSanPham)
                .Take(6)
                .ToListAsync();

            var result = matches.Select(s => new
            {
                maSanPham = s.MaSanPham,
                tenSanPham = s.TenSanPham,
                thuongHieu = s.ThuongHieu ?? "StrideLab",
                tenDanhMuc = s.DanhMuc?.TenDanhMuc ?? "",
                hinhAnh = s.HinhAnh,
                giaBan = s.GiaThapNhat,
                giaGoc = s.GiaGoc
            });

            return Json(result);
        }
    }
}
