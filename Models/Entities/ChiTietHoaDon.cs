using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QL_ShopGiay.Models.Entities
{
    [Table("ChiTietHoaDon")]
    public class ChiTietHoaDon
    {
        [Key]
        public int MaChiTietHoaDon { get; set; }

        public int MaHoaDon { get; set; }

        public int MaBienThe { get; set; }

        public int SoLuong { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal DonGia { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [Column(TypeName = "decimal(29, 2)")]
        public decimal? ThanhTien { get; set; }

        [ForeignKey("MaHoaDon")]
        public virtual HoaDon? HoaDon { get; set; }

        [ForeignKey("MaBienThe")]
        public virtual BienTheSanPham? BienTheSanPham { get; set; }
    }
}
