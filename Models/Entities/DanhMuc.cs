using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QL_ShopGiay.Models.Entities
{
    [Table("DanhMuc")]
    public class DanhMuc
    {
        [Key]
        public int MaDanhMuc { get; set; }

        [Required]
        [StringLength(100)]
        public string TenDanhMuc { get; set; } = string.Empty;

        public int? MaDanhMucCha { get; set; }

        [StringLength(255)]
        public string? MoTa { get; set; }

        [ForeignKey("MaDanhMucCha")]
        public virtual DanhMuc? DanhMucCha { get; set; }

        public virtual ICollection<DanhMuc> DanhMucCons { get; set; } = new List<DanhMuc>();

        public virtual ICollection<SanPham> SanPhams { get; set; } = new List<SanPham>();
    }
}
