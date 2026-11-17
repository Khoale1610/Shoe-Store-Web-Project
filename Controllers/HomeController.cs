using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QL_ShopGiay.Data;
using QL_ShopGiay.Models;
using QL_ShopGiay.Models.ViewModels;

namespace QL_ShopGiay.Controllers
{
    public class HomeController : Controller
    {
        private readonly ShopGiayDbContext _context;

        public HomeController(ShopGiayDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var danhMucCha = await _context.DanhMucs
                .Include(d => d.DanhMucCons)
                .Where(d => d.MaDanhMucCha == null)
                .OrderBy(d => d.MaDanhMuc)
                .ToListAsync();

            var allProducts = await _context.SanPhams
                .Include(s => s.DanhMuc)
                .Include(s => s.BienTheSanPhams)
                    .ThenInclude(b => b.MauSac)
                .Include(s => s.BienTheSanPhams)
                    .ThenInclude(b => b.KichCo)
                .OrderByDescending(s => s.MaSanPham)
                .ToListAsync();

            var now = DateTime.Now;
            var khuyenMais = await _context.KhuyenMais
                .Where(k => k.TrangThai && k.NgayBatDau <= now && k.NgayKetThuc >= now)
                .OrderByDescending(k => k.PhanTramGiam)
                .ToListAsync();

            var vm = new HomeViewModel
            {
                DanhMucCha = danhMucCha,
                SanPhamMoi = allProducts
                    .Where(s => s.NhanNoiBat != null && s.NhanNoiBat.Contains("Moi"))
                    .Take(8)
                    .ToList(),
                SanPhamBanChay = allProducts
                    .Where(s => s.NhanNoiBat != null && s.NhanNoiBat.Contains("BanChay"))
                    .Take(8)
                    .ToList(),
                SanPhamGiamGia = allProducts
                    .Where(s => (s.NhanNoiBat != null && s.NhanNoiBat.Contains("GiamGia")) || s.PhanTramGiamGia > 0)
                    .Take(4)
                    .ToList(),
                KhuyenMaiHienHanh = khuyenMais
            };

            return View(vm);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
