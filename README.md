#STRIDELAB — Website Bán & Quản Lý Giày (ASP.NET Core MVC + SQL Server)

Dự án đồ án môn **Lập trình Web**: Hệ thống Thương mại Điện tử & Quản lý Cửa hàng Giày phong cách **High-Contrast Streetwear Retail**.

---

## 1. Tính năng tự động tạo Database khi Clone từ GitHub
Dự án đã được tích hợp bộ khởi tạo tự động **`DbInitializer`** (trong file `Data/DbInitializer.cs` và `Program.cs`) kết hợp cùng file kịch bản gốc **`database.sql`**:
* Khi **clone dự án từ GitHub về máy mới** và bấm chạy ứng dụng (`dotnet run` hoặc `F5` trên Visual Studio), hệ thống sẽ **tự động kiểm tra SQL Server**:
  * Nếu máy chưa có CSDL `QL_ShopGiay` hoặc bảng `SanPham` đang trống, ứng dụng sẽ tự động đọc file `database.sql` và nạp đầy đủ **9 bảng**, **14 đôi giày thực tế**, **75 biến thể Size/Màu**, **14 danh mục 2 cấp**, **4 mã giảm giá** và **các tài khoản/hóa đơn mẫu** mà không cần thao tác tay.

---

## 2. Hướng dẫn cài đặt cho người Clone dự án về máy

### Bước 1: Kiểm tra tên SQL Server trong `appsettings.json`
Mở file `appsettings.json`, kiểm tra chuỗi kết nối `DefaultConnection`:
* Nếu máy dùng **SQL Server Express** (mặc định): giữ nguyên `Server=.\SQLEXPRESS;...`
* Nếu máy dùng **SQL Server bản đầy đủ (MSSQLSERVER)**: đổi thành `Server=localhost;...` hoặc `Server=.;...`

### Bước 2: Chạy dự án
Mở Terminal / PowerShell tại thư mục dự án và chạy:
```powershell
dotnet run
```
*(Hoặc nếu muốn tạo Database thủ công bằng SSMS, chỉ cần mở file `database.sql` trong SQL Server Management Studio và nhấn **Execute / F5**).*

---

## 3. Tài khoản đăng nhập mẫu
* **Quản trị viên (SuperAdmin - Chuyển hướng vào `/Admin`):**
  * Tài khoản: `admin`
  * Mật khẩu: `123456`
* **Nhân viên bán hàng:**
  * Tài khoản: `nhanvien`
  * Mật khẩu: `123456`
* **Khách hàng:**
  * Tài khoản: `khachhang@gmail.com`
  * Mật khẩu: `123456`
