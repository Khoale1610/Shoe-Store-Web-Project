using Microsoft.EntityFrameworkCore;
using QL_ShopGiay.Models.Entities;

namespace QL_ShopGiay.Data
{
    public class ShopGiayDbContext : DbContext
    {
        public ShopGiayDbContext(DbContextOptions<ShopGiayDbContext> options) : base(options)
        {
        }

        public DbSet<QuanTriVien> QuanTriViens { get; set; }
        public DbSet<DanhMuc> DanhMucs { get; set; }
        public DbSet<SanPham> SanPhams { get; set; }
        public DbSet<MauSac> MauSacs { get; set; }
        public DbSet<KichCo> KichCos { get; set; }
        public DbSet<BienTheSanPham> BienTheSanPhams { get; set; }
        public DbSet<KhachHang> KhachHangs { get; set; }
        public DbSet<KhuyenMai> KhuyenMais { get; set; }
        public DbSet<HoaDon> HoaDons { get; set; }
        public DbSet<ChiTietHoaDon> ChiTietHoaDons { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<DanhMuc>(entity =>
            {
                entity.HasOne(d => d.DanhMucCha)
                      .WithMany(p => p.DanhMucCons)
                      .HasForeignKey(d => d.MaDanhMucCha)
                      .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<BienTheSanPham>(entity =>
            {
                entity.HasIndex(e => new { e.MaSanPham, e.MaMau, e.MaKichCo })
                      .IsUnique()
                      .HasDatabaseName("UQ_BienThe");
            });

            modelBuilder.Entity<ChiTietHoaDon>(entity =>
            {
                entity.Property(e => e.ThanhTien)
                      .HasComputedColumnSql("([SoLuong]*[DonGia])", stored: false);
            });
        }
    }
}
