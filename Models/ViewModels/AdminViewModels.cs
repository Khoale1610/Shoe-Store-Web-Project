using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using QL_ShopGiay.Models.Entities;

namespace QL_ShopGiay.Models.ViewModels
{
    // 1. ViewModel Thống kê & Dashboard Admin
    public class AdminDashboardViewModel
    {
        public decimal TongDoanhThu { get; set; }
        public decimal DoanhThuHoanThanh { get; set; }
        public int TongDonHang { get; set; }
        public int DonChoXacNhan { get; set; }
        public int DonDangGiao { get; set; }
        public int DonHoanThanh { get; set; }
        public int DonDaHuy { get; set; }
        public int TongSanPham { get; set; }
        public int TongKhachHang { get; set; }
        public int SoBienTheSapHetHang { get; set; }

        // Dữ liệu biểu đồ Chart.js theo ngày (7 ngày gần nhất)
        public List<string> NhanNgay { get; set; } = new();
        public List<decimal> DoanhThuTheoNgay { get; set; } = new();

        // Dữ liệu biểu đồ Chart.js theo tháng (12 tháng)
        public List<string> NhanThang { get; set; } = new();
        public List<decimal> DoanhThuTheoThang { get; set; } = new();

        // Top sản phẩm bán chạy nhất
        public List<TopSellingProductDto> TopSanPhamBanChay { get; set; } = new();

        // Danh sách biến thể sắp hết hàng (SoLuongTon <= 8)
        public List<BienTheSanPham> BienTheSapHetHang { get; set; } = new();

        // Đơn hàng mới nhất
        public List<HoaDon> DonHangMoiNhat { get; set; } = new();
    }

    public class TopSellingProductDto
    {
        public int MaSanPham { get; set; }
        public string TenSanPham { get; set; } = string.Empty;
        public string ThuongHieu { get; set; } = string.Empty;
        public string HinhAnh { get; set; } = string.Empty;
        public string TenDanhMuc { get; set; } = string.Empty;
        public int TongSoLuongBan { get; set; }
        public decimal TongDoanhThu { get; set; }
    }

    // 2. ViewModel Thêm/Sửa Loại sản phẩm (DanhMuc)
    public class CategoryFormViewModel
    {
        public int MaDanhMuc { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên loại sản phẩm (danh mục)")]
        [StringLength(100, ErrorMessage = "Tên loại sản phẩm không vượt quá 100 ký tự")]
        public string TenDanhMuc { get; set; } = string.Empty;

        public int? MaDanhMucCha { get; set; }

        [StringLength(255, ErrorMessage = "Mô tả không vượt quá 255 ký tự")]
        public string? MoTa { get; set; }

        public List<DanhMuc> DanhSachDanhMucCha { get; set; } = new();
    }

    // 3. ViewModel Thêm/Sửa Sản phẩm (SanPham + BienTheSanPham)
    public class ProductFormViewModel
    {
        public int MaSanPham { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên giày")]
        [StringLength(150, ErrorMessage = "Tên giày không vượt quá 150 ký tự")]
        public string TenSanPham { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn loại sản phẩm (danh mục)")]
        public int MaDanhMuc { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên thương hiệu")]
        [StringLength(50)]
        public string ThuongHieu { get; set; } = "Nike";

        [Required(ErrorMessage = "Vui lòng nhập giá gốc niêm yết")]
        [Range(10000, 100000000, ErrorMessage = "Giá gốc phải từ 10,000₫ trở lên")]
        public decimal GiaGoc { get; set; } = 2500000;

        [Required(ErrorMessage = "Vui lòng nhập giá bán thực tế cho các biến thể")]
        [Range(10000, 100000000, ErrorMessage = "Giá bán phải từ 10,000₫ trở lên")]
        public decimal GiaBanMacDinh { get; set; } = 2290000;

        [Range(0, 10000, ErrorMessage = "Số lượng tồn kho phải từ 0 đến 10,000")]
        public int SoLuongTonMoiBienThe { get; set; } = 15;

        // Upload file ảnh hoặc giữ đường dẫn ảnh hiện tại
        public IFormFile? HinhAnhFile { get; set; }
        public string? HinhAnhHienTai { get; set; }

        // Radio Button 1: Giới tính (Nam / Nữ / Unisex)
        [Required(ErrorMessage = "Vui lòng chọn giới tính")]
        public string GioiTinh { get; set; } = "Unisex";

        // Radio Button 2: Trạng thái hàng hóa (ConHang / HetHang)
        public string TrangThaiHang { get; set; } = "ConHang";

        // Checkbox 1: Nhãn nổi bật (Moi, BanChay, GiamGia)
        public List<string> NhanNoiBatList { get; set; } = new();

        // Checkbox 2: Chọn nhiều Kích cỡ Size giày (36..44)
        public List<int> SelectedSizeIds { get; set; } = new();

        // Checkbox 3: Chọn nhiều Màu sắc giày
        public List<int> SelectedColorIds { get; set; } = new();

        public string? MoTa { get; set; }

        // Dữ liệu nạp cho Dropdown & Checkbox trên form
        public List<DanhMuc> DanhSachDanhMuc { get; set; } = new();
        public List<KichCo> TatCaKichCo { get; set; } = new();
        public List<MauSac> TatCaMauSac { get; set; } = new();
    }

    // 4. ViewModel Thêm/Sửa Khuyến mãi (KhuyenMai)
    public class PromotionFormViewModel
    {
        public int MaKM { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã Code giảm giá (VD: SUMMER25)")]
        [StringLength(30, MinimumLength = 3, ErrorMessage = "Mã Code phải từ 3 đến 30 ký tự")]
        [RegularExpression(@"^[A-Za-z0-9_]+$", ErrorMessage = "Mã Code chỉ gồm chữ cái không dấu và chữ số, không chứa khoảng trắng")]
        public string MaCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên chương trình khuyến mãi")]
        [StringLength(100)]
        public string TenChuongTrinh { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập phần trăm giảm giá")]
        [Range(1, 100, ErrorMessage = "Phần trăm giảm giá phải nằm trong khoảng từ 1% đến 100%")]
        public int PhanTramGiam { get; set; } = 10;

        [Required(ErrorMessage = "Vui lòng chọn ngày bắt đầu")]
        public DateTime NgayBatDau { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Vui lòng chọn ngày kết thúc")]
        public DateTime NgayKetThuc { get; set; } = DateTime.Today.AddMonths(3);

        public bool TrangThai { get; set; } = true;
    }
}
