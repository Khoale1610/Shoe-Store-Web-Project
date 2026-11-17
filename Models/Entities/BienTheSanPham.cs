using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QL_ShopGiay.Models.Entities
{
    [Table("BienTheSanPham")]
    public class BienTheSanPham
    {
        [Key]
        public int MaBienThe { get; set; }

        public int MaSanPham { get; set; }

        public int MaMau { get; set; }

        public int MaKichCo { get; set; }

        public int SoLuongTon { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal GiaBan { get; set; }

        [StringLength(100)]
        public string? MaSKU { get; set; }

        [ForeignKey("MaSanPham")]
        public virtual SanPham? SanPham { get; set; }

        [ForeignKey("MaMau")]
        public virtual MauSac? MauSac { get; set; }

        [ForeignKey("MaKichCo")]
        public virtual KichCo? KichCo { get; set; }

        public virtual ICollection<ChiTietHoaDon> ChiTietHoaDons { get; set; } = new List<ChiTietHoaDon>();
    }
}
