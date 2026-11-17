using QL_ShopGiay.Models.Entities;

namespace QL_ShopGiay.Models.ViewModels
{
    public class HomeViewModel
    {
        public List<DanhMuc> DanhMucCha { get; set; } = new();
        public List<SanPham> SanPhamMoi { get; set; } = new();
        public List<SanPham> SanPhamBanChay { get; set; } = new();
        public List<SanPham> SanPhamGiamGia { get; set; } = new();
        public List<KhuyenMai> KhuyenMaiHienHanh { get; set; } = new();
    }
}
