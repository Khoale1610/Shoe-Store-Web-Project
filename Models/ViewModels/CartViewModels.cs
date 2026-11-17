using System.ComponentModel.DataAnnotations;
using QL_ShopGiay.Models.Entities;

namespace QL_ShopGiay.Models.ViewModels
{
    public class CartItemViewModel
    {
        public int MaBienThe { get; set; }
        public int MaSanPham { get; set; }
        public string TenSanPham { get; set; } = string.Empty;
        public string ThuongHieu { get; set; } = string.Empty;
        public string HinhAnh { get; set; } = string.Empty;
        public int MaMau { get; set; }
        public string TenMau { get; set; } = string.Empty;
        public string MaHex { get; set; } = string.Empty;
        public int MaKichCo { get; set; }
        public string SoKichCo { get; set; } = string.Empty;
        public string MaSKU { get; set; } = string.Empty;
        public decimal DonGia { get; set; }
        public int SoLuong { get; set; }
        public int SoLuongTon { get; set; }
        public decimal ThanhTien => DonGia * SoLuong;
    }

    public class CartPageViewModel
    {
        public List<CartItemViewModel> Items { get; set; } = new();
        public decimal TamTinh => Items.Sum(i => i.ThanhTien);
        public string? MaCodeKhuyenMai { get; set; }
        public string? TenChuongTrinhKM { get; set; }
        public int PhanTramGiam { get; set; }
        public decimal TienGiam => Math.Round(TamTinh * PhanTramGiam / 100m, 0);
        public decimal TongCong => Math.Max(0, TamTinh - TienGiam);
        public List<KhuyenMai> DanhSachKhuyenMai { get; set; } = new();
    }

    public class CheckoutViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên người nhận hàng")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Họ tên phải từ 2 đến 100 ký tự")]
        public string TenNguoiNhan { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại liên hệ")]
        [RegularExpression(@"^(0|\+84)[0-9]{9,10}$", ErrorMessage = "Số điện thoại không hợp lệ (gồm 10-11 chữ số, bắt đầu bằng 0 hoặc +84)")]
        public string SDTNguoiNhan { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng chi tiết")]
        [StringLength(255, MinimumLength = 5, ErrorMessage = "Địa chỉ giao hàng phải từ 5 đến 255 ký tự")]
        public string DiaChiGiaoHang { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán")]
        public string PhuongThucThanhToan { get; set; } = "COD";

        public string? GhiChu { get; set; }

        public CartPageViewModel Cart { get; set; } = new();
    }
}
