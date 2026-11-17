using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QL_ShopGiay.Models.Entities
{
    [Table("HoaDon")]
    public class HoaDon
    {
        [Key]
        public int MaHoaDon { get; set; }

        public int? MaKhachHang { get; set; }

        public int? MaAdmin { get; set; }

        public int? MaKM { get; set; }

        [Required]
        [StringLength(100)]
        public string TenNguoiNhan { get; set; } = string.Empty;

        [Required]
        [StringLength(15)]
        public string SDTNguoiNhan { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string DiaChiGiaoHang { get; set; } = string.Empty;

        [StringLength(50)]
        public string PhuongThucThanhToan { get; set; } = "COD";

        [Column(TypeName = "decimal(18, 2)")]
        public decimal TienGiam { get; set; } = 0;

        [Column(TypeName = "decimal(18, 2)")]
        public decimal TongTien { get; set; }

        [StringLength(50)]
        public string TrangThai { get; set; } = "Chờ xác nhận";

        public DateTime NgayLap { get; set; } = DateTime.Now;

        [ForeignKey("MaKhachHang")]
        public virtual KhachHang? KhachHang { get; set; }

        [ForeignKey("MaAdmin")]
        public virtual QuanTriVien? QuanTriVien { get; set; }

        [ForeignKey("MaKM")]
        public virtual KhuyenMai? KhuyenMai { get; set; }

        public virtual ICollection<ChiTietHoaDon> ChiTietHoaDons { get; set; } = new List<ChiTietHoaDon>();
    }
}
