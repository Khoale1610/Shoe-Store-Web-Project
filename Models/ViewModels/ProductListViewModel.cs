using QL_ShopGiay.Models.Entities;

namespace QL_ShopGiay.Models.ViewModels
{
    public class ProductListViewModel
    {
        public List<SanPham> SanPhams { get; set; } = new();
        public List<DanhMuc> DanhMucCha { get; set; } = new();
        public List<string> ThuongHieus { get; set; } = new();
        public List<string> KichCos { get; set; } = new();

        // Filter parameters
        public int? MaDanhMuc { get; set; }
        public string? TenDanhMucHienTai { get; set; }
        public string? MoTaDanhMucHienTai { get; set; }
        public string? MucGia { get; set; }
        public string? ThuongHieu { get; set; }
        public string? KichCo { get; set; }
        public string? GioiTinh { get; set; }
        public string? NhanNoiBat { get; set; }
        public string? TuKhoa { get; set; }
        public string? SapXep { get; set; }

        public int TongSoSanPham { get; set; }
    }
}
