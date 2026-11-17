using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QL_ShopGiay.Models.Entities
{
    [Table("KichCo")]
    public class KichCo
    {
        [Key]
        public int MaKichCo { get; set; }

        [Required]
        [StringLength(10)]
        public string SoKichCo { get; set; } = string.Empty;

        public virtual ICollection<BienTheSanPham> BienTheSanPhams { get; set; } = new List<BienTheSanPham>();
    }
}
