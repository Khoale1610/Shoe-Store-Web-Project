using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QL_ShopGiay.Models.Entities
{
    [Table("MauSac")]
    public class MauSac
    {
        [Key]
        public int MaMau { get; set; }

        [Required]
        [StringLength(50)]
        public string TenMau { get; set; } = string.Empty;

        [StringLength(10)]
        public string? MaHex { get; set; }

        public virtual ICollection<BienTheSanPham> BienTheSanPhams { get; set; } = new List<BienTheSanPham>();
    }
}
