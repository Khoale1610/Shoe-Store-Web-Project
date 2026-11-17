using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QL_ShopGiay.Models.Entities
{
    [Table("SanPham")]
    public class SanPham
    {
        [Key]
        public int MaSanPham { get; set; }

        [Required]
        [StringLength(150)]
        public string TenSanPham { get; set; } = string.Empty;

        public int MaDanhMuc { get; set; }

        [StringLength(50)]
        public string? ThuongHieu { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal GiaGoc { get; set; }

        [StringLength(255)]
        public string? HinhAnh { get; set; }

        [StringLength(20)]
        public string? GioiTinh { get; set; } = "Unisex";

        [StringLength(100)]
        public string? NhanNoiBat { get; set; }

        public string? MoTa { get; set; }

        public DateTime NgayTao { get; set; } = DateTime.Now;

        [ForeignKey("MaDanhMuc")]
        public virtual DanhMuc? DanhMuc { get; set; }

        public virtual ICollection<BienTheSanPham> BienTheSanPhams { get; set; } = new List<BienTheSanPham>();

        [NotMapped]
        public decimal GiaThapNhat => BienTheSanPhams != null && BienTheSanPhams.Any()
            ? BienTheSanPhams.Min(b => b.GiaBan)
            : GiaGoc;

        [NotMapped]
        public int TongTonKho => BienTheSanPhams != null && BienTheSanPhams.Any()
            ? BienTheSanPhams.Sum(b => b.SoLuongTon)
            : 0;

        [NotMapped]
        public int PhanTramGiamGia => GiaGoc > 0 && GiaThapNhat < GiaGoc
            ? (int)Math.Round((1 - (GiaThapNhat / GiaGoc)) * 100)
            : 0;
    }
}
