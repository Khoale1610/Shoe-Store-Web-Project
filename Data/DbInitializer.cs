using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace QL_ShopGiay.Data
{
    public static class DbInitializer
    {
        /// <summary>
        /// Tự động kiểm tra và khởi tạo Database QL_ShopGiay + nạp toàn bộ dữ liệu mẫu từ file database.sql
        /// khi người khác clone dự án từ GitHub về máy và chạy lần đầu.
        /// </summary>
        public static void Initialize(IServiceProvider serviceProvider, IWebHostEnvironment env)
        {
            using var scope = serviceProvider.CreateScope();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");
            var context = scope.ServiceProvider.GetRequiredService<ShopGiayDbContext>();

            try
            {
                bool needsSeed = false;

                if (!context.Database.CanConnect())
                {
                    logger.LogInformation("Chưa tìm thấy CSDL QL_ShopGiay. Đang tự động khởi tạo từ database.sql...");
                    needsSeed = true;
                }
                else
                {
                    try
                    {
                        // Kiểm tra bảng SanPham đã có dữ liệu chưa hoặc có bị lỗi font không
                        bool hasProducts = context.SanPhams.Any();
                        bool hasGarbledText = context.DanhMucs.Any(d => d.TenDanhMuc.Contains("Ã") || d.TenDanhMuc.Contains("á»"));
                        if (!hasProducts || hasGarbledText)
                        {
                            needsSeed = true;
                        }
                    }
                    catch
                    {
                        // Bảng chưa tồn tại trong DB
                        needsSeed = true;
                    }
                }

                if (!needsSeed)
                {
                    return;
                }

                // Tìm file database.sql tại thư mục gốc của dự án
                var sqlFilePath = Path.Combine(env.ContentRootPath, "database.sql");
                if (!File.Exists(sqlFilePath))
                {
                    logger.LogWarning("Không tìm thấy file database.sql tại: {Path}", sqlFilePath);
                    return;
                }

                var connString = config.GetConnectionString("DefaultConnection");
                if (string.IsNullOrWhiteSpace(connString))
                {
                    return;
                }

                // Kết nối vào nhánh 'master' để có thể chạy lệnh CREATE DATABASE QL_ShopGiay
                var builder = new SqlConnectionStringBuilder(connString)
                {
                    InitialCatalog = "master"
                };

                var script = File.ReadAllText(sqlFilePath, Encoding.UTF8);

                // Tách các câu lệnh theo từ khóa GO (chuẩn SQL Server Management Studio)
                var batches = Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);

                using var connection = new SqlConnection(builder.ConnectionString);
                connection.Open();

                foreach (var batch in batches)
                {
                    var trimmed = batch.Trim();
                    if (string.IsNullOrWhiteSpace(trimmed))
                    {
                        continue;
                    }

                    using var cmd = new SqlCommand(trimmed, connection)
                    {
                        CommandTimeout = 120
                    };
                    cmd.ExecuteNonQuery();
                }

                logger.LogInformation("Đã tự động tạo Database QL_ShopGiay và nạp đầy đủ 14 đôi giày, 75 biến thể, 14 danh mục, 4 mã giảm giá thành công!");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Lỗi khi tự động khởi tạo CSDL QL_ShopGiay từ database.sql.");
            }
        }
    }
}
