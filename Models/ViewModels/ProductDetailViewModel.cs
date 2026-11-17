using QL_ShopGiay.Models.Entities;

namespace QL_ShopGiay.Models.ViewModels
{
    public class ProductDetailViewModel
    {
        public SanPham SanPham { get; set; } = null!;
        public List<MauSac> DanhSachMau { get; set; } = new();
        public List<KichCo> DanhSachKichCo { get; set; } = new();
        public List<VariantDto> BienTheJson { get; set; } = new();
        public List<SanPham> SanPhamLienQuan { get; set; } = new();
    }

    public class VariantDto
    {
        public int MaBienThe { get; set; }
        public int MaMau { get; set; }
        public string TenMau { get; set; } = string.Empty;
        public string MaHex { get; set; } = string.Empty;
        public int MaKichCo { get; set; }
        public string SoKichCo { get; set; } = string.Empty;
        public int SoLuongTon { get; set; }
        public decimal GiaBan { get; set; }
        public string MaSKU { get; set; } = string.Empty;
    }
}
