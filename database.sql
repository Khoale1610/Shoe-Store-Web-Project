-- =================================================================================
-- DỰ ÁN: WEBSITE BÁN & QUẢN LÝ GIÀY (HỆ THỐNG CỬA HÀNG GIÀY STRIDELAB)
-- CƠ SỞ DỮ LIỆU: QL_ShopGiay (SQL Server)
-- =================================================================================

USE master;
GO

IF EXISTS (SELECT name FROM sys.databases WHERE name = N'QL_ShopGiay')
BEGIN
    ALTER DATABASE QL_ShopGiay SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE QL_ShopGiay;
END
GO

CREATE DATABASE QL_ShopGiay;
GO
USE QL_ShopGiay;
GO

-- 1. Bảng Quản trị viên (Gộp chung Admin và Nhân viên)
CREATE TABLE QuanTriVien (
    MaAdmin INT IDENTITY(1,1) PRIMARY KEY,
    TenDangNhap VARCHAR(50) UNIQUE NOT NULL,
    MatKhau VARCHAR(255) NOT NULL,
    HoTen NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100),
    SoDienThoai VARCHAR(15),
    VaiTro NVARCHAR(50) DEFAULT N'NhanVien', -- SuperAdmin, NhanVien
    TrangThai BIT DEFAULT 1,
    NgayTao DATETIME DEFAULT GETDATE()
);

-- 2. Bảng Danh mục (Hỗ trợ Menu 2 cấp qua MaDanhMucCha)
CREATE TABLE DanhMuc (
    MaDanhMuc INT IDENTITY(1,1) PRIMARY KEY,
    TenDanhMuc NVARCHAR(100) NOT NULL,
    MaDanhMucCha INT NULL,
    MoTa NVARCHAR(255),
    FOREIGN KEY (MaDanhMucCha) REFERENCES DanhMuc(MaDanhMuc)
);

-- 3. Bảng Sản phẩm (Bổ sung HinhAnh, GioiTinh cho Radio, NhanNoiBat cho Checkbox)
CREATE TABLE SanPham (
    MaSanPham INT IDENTITY(1,1) PRIMARY KEY,
    TenSanPham NVARCHAR(150) NOT NULL,
    MaDanhMuc INT NOT NULL,
    ThuongHieu NVARCHAR(50),
    GiaGoc DECIMAL(18, 2) NOT NULL,
    HinhAnh NVARCHAR(255), -- Lưu đường dẫn ảnh đại diện
    GioiTinh NVARCHAR(20) DEFAULT N'Unisex', -- Dùng cho Radio: Nam, Nữ, Unisex
    NhanNoiBat NVARCHAR(100), -- Dùng cho Checkbox: VD "Moi,BanChay,GiamGia"
    MoTa NVARCHAR(MAX),
    NgayTao DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (MaDanhMuc) REFERENCES DanhMuc(MaDanhMuc)
);

-- 4. Bảng Màu sắc & Kích cỡ
CREATE TABLE MauSac (
    MaMau INT IDENTITY(1,1) PRIMARY KEY,
    TenMau NVARCHAR(50) NOT NULL,
    MaHex VARCHAR(10) -- VD: #000000 để hiển thị nút chọn màu đẹp hơn
);

CREATE TABLE KichCo (
    MaKichCo INT IDENTITY(1,1) PRIMARY KEY,
    SoKichCo VARCHAR(10) NOT NULL -- Hỗ trợ cả 38, 39.5, 40...
);

-- 5. Bảng Biến thể sản phẩm
CREATE TABLE BienTheSanPham (
    MaBienThe INT IDENTITY(1,1) PRIMARY KEY,
    MaSanPham INT NOT NULL,
    MaMau INT NOT NULL,
    MaKichCo INT NOT NULL,
    SoLuongTon INT NOT NULL DEFAULT 0,
    GiaBan DECIMAL(18, 2) NOT NULL,
    MaSKU NVARCHAR(100) UNIQUE,
    CONSTRAINT UQ_BienThe UNIQUE (MaSanPham, MaMau, MaKichCo),
    FOREIGN KEY (MaSanPham) REFERENCES SanPham(MaSanPham) ON DELETE CASCADE,
    FOREIGN KEY (MaMau) REFERENCES MauSac(MaMau),
    FOREIGN KEY (MaKichCo) REFERENCES KichCo(MaKichCo)
);

-- 6. Bảng Khách hàng (Bổ sung MatKhau để Đăng ký / Đăng nhập)
CREATE TABLE KhachHang (
    MaKhachHang INT IDENTITY(1,1) PRIMARY KEY,
    HoTen NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100) UNIQUE NOT NULL,
    MatKhau VARCHAR(255) NOT NULL,
    SoDienThoai VARCHAR(15) NOT NULL,
    DiaChi NVARCHAR(255),
    NgayTao DATETIME DEFAULT GETDATE()
);

-- 7. Bảng Khuyến mãi (Phục vụ tiêu chí Quản lý khuyến mãi)
CREATE TABLE KhuyenMai (
    MaKM INT IDENTITY(1,1) PRIMARY KEY,
    MaCode VARCHAR(30) UNIQUE NOT NULL,
    TenChuongTrinh NVARCHAR(100) NOT NULL,
    PhanTramGiam INT NOT NULL CHECK (PhanTramGiam BETWEEN 1 AND 100),
    NgayBatDau DATETIME NOT NULL,
    NgayKetThuc DATETIME NOT NULL,
    TrangThai BIT DEFAULT 1
);

-- 8. Bảng Hóa đơn (Bổ sung thông tin giao hàng, thanh toán & khuyến mãi)
CREATE TABLE HoaDon (
    MaHoaDon INT IDENTITY(1,1) PRIMARY KEY,
    MaKhachHang INT NULL, -- Cho phép NULL nếu khách mua không cần đăng nhập
    MaAdmin INT NULL,     -- Nhân viên duyệt đơn
    MaKM INT NULL,
    TenNguoiNhan NVARCHAR(100) NOT NULL,
    SDTNguoiNhan VARCHAR(15) NOT NULL,
    DiaChiGiaoHang NVARCHAR(255) NOT NULL,
    PhuongThucThanhToan NVARCHAR(50) DEFAULT N'COD', -- COD, Chuyển khoản
    TienGiam DECIMAL(18, 2) DEFAULT 0,
    TongTien DECIMAL(18, 2) NOT NULL,
    TrangThai NVARCHAR(50) DEFAULT N'Chờ xác nhận', -- Chờ xác nhận, Đang giao, Hoàn thành, Đã hủy
    NgayLap DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (MaKhachHang) REFERENCES KhachHang(MaKhachHang),
    FOREIGN KEY (MaAdmin) REFERENCES QuanTriVien(MaAdmin),
    FOREIGN KEY (MaKM) REFERENCES KhuyenMai(MaKM)
);

-- 9. Bảng Chi tiết hóa đơn
CREATE TABLE ChiTietHoaDon (
    MaChiTietHoaDon INT IDENTITY(1,1) PRIMARY KEY,
    MaHoaDon INT NOT NULL,
    MaBienThe INT NOT NULL,
    SoLuong INT NOT NULL,
    DonGia DECIMAL(18, 2) NOT NULL,
    ThanhTien AS (SoLuong * DonGia),
    FOREIGN KEY (MaHoaDon) REFERENCES HoaDon(MaHoaDon) ON DELETE CASCADE,
    FOREIGN KEY (MaBienThe) REFERENCES BienTheSanPham(MaBienThe)
);
GO

-- =================================================================================
-- DỮ LIỆU MẪU TIẾNG VIỆT CHUẨN (SEED DATA)
-- =================================================================================

-- 1. Quản trị viên
INSERT INTO QuanTriVien (TenDangNhap, MatKhau, HoTen, Email, SoDienThoai, VaiTro, TrangThai)
VALUES
('admin', '123456', N'Nguyễn Quản Trị (Quản trị cấp cao)', 'admin@stridelab.vn', '0901234567', N'SuperAdmin', 1),
('nhanvien', '123456', N'Trần Nhân Viên Bán Hàng', 'sale@stridelab.vn', '0902345678', N'NhanVien', 1);

-- 2. Danh mục 2 cấp (Cấp 1: MaDanhMuc 1..4, Cấp 2: MaDanhMuc 5..14)
SET IDENTITY_INSERT DanhMuc ON;
INSERT INTO DanhMuc (MaDanhMuc, TenDanhMuc, MaDanhMucCha, MoTa) VALUES
-- Danh mục Cha (Cấp 1)
(1, N'Giày Thể Thao', NULL, N'Dòng giày hiệu năng cao cho chạy bộ, tập luyện và vận động chuyên nghiệp'),
(2, N'Giày Thời Trang & Đường Phố', NULL, N'Phong cách đường phố hiện đại, năng động cho mọi hoạt động hàng ngày'),
(3, N'Giày Bóng Đá & Bóng Rổ', NULL, N'Giày thi đấu chuyên dụng bám sân, bảo vệ cổ chân tối ưu'),
(4, N'Giày Công Sở & Lịch Lãm', NULL, N'Lịch lãm, tinh tế với chất liệu da cao cấp và phom dáng chuẩn mực'),

-- Danh mục Con (Cấp 2) thuộc Giày Thể Thao (MaDanhMucCha = 1)
(5, N'Giày Chạy Bộ Đường Dài', 1, N'Đệm êm ái, hoàn trả năng lượng vượt trội trên mọi cung đường'),
(6, N'Giày Tập Thể Hình & Phòng Tập', 1, N'Đế bằng vững chãi, hỗ trợ nâng tạ và bài tập cường độ cao'),
(7, N'Giày Quần Vợt & Cầu Lông', 1, N'Chống lật cổ chân, đế chống mài mòn chuyên dụng cho sân cứng'),

-- Danh mục Con (Cấp 2) thuộc Giày Thời Trang & Đường Phố (MaDanhMucCha = 2)
(8, N'Giày Thể Thao Cổ Thấp', 2, N'Dễ phối đồ, trẻ trung và thoáng khí suốt ngày dài'),
(9, N'Giày Thể Thao Cổ Cao', 2, N'Cá tính, ôm cổ chân đậm chất cổ điển và đường phố'),
(10, N'Giày Đế Cao Tôn Dáng', 2, N'Đế dày tôn chiều cao, xu hướng thời trang giới trẻ'),

-- Danh mục Con (Cấp 2) thuộc Giày Bóng Đá & Bóng Rổ (MaDanhMucCha = 3)
(11, N'Giày Đá Bóng Sân Cỏ Nhân Tạo', 3, N'Đinh dăm cao su bám sân cỏ nhân tạo cực tốt'),
(12, N'Giày Bóng Rổ Chuyên Nghiệp', 3, N'Đệm khí giảm chấn tối đa khi bật nhảy tiếp đất'),

-- Danh mục Con (Cấp 2) thuộc Giày Công Sở & Lịch Lãm (MaDanhMucCha = 4)
(13, N'Giày Lười Da Cao Cấp', 4, N'Tiện lợi, sang trọng không cần buộc dây'),
(14, N'Giày Tây Da Công Sở', 4, N'Biểu tượng lịch lãm cho quý ông công sở và sự kiện');
SET IDENTITY_INSERT DanhMuc OFF;

-- 3. Màu sắc
SET IDENTITY_INSERT MauSac ON;
INSERT INTO MauSac (MaMau, TenMau, MaHex) VALUES
(1, N'Đen Tuyền', '#111827'),
(2, N'Trắng Tinh Khôi', '#F8FAFC'),
(3, N'Cam Thể Thao', '#FF5722'),
(4, N'Xanh Đen Navy', '#1E3A8A'),
(5, N'Đỏ Đô', '#DC2626'),
(6, N'Xám Bạc', '#64748B'),
(7, N'Xanh Rêu', '#065F46'),
(8, N'Kem Sữa', '#E5D3B3');
SET IDENTITY_INSERT MauSac OFF;

-- 4. Kích cỡ
SET IDENTITY_INSERT KichCo ON;
INSERT INTO KichCo (MaKichCo, SoKichCo) VALUES
(1, '36'),
(2, '37'),
(3, '38'),
(4, '39'),
(5, '40'),
(6, '41'),
(7, '42'),
(8, '43'),
(9, '44');
SET IDENTITY_INSERT KichCo OFF;

-- 5. Sản phẩm (14 đôi giày thực tế đa dạng danh mục, thương hiệu, mức giá)
SET IDENTITY_INSERT SanPham ON;
INSERT INTO SanPham (MaSanPham, TenSanPham, MaDanhMuc, ThuongHieu, GiaGoc, HinhAnh, GioiTinh, NhanNoiBat, MoTa) VALUES
(1, N'Giày Chạy Bộ Nike Air Zoom Pegasus 41', 5, N'Nike', 3650000, 'https://images.unsplash.com/photo-1542291026-7eec264c27ff?auto=format&fit=crop&w=900&q=80', N'Nam', N'Moi,BanChay',
 N'Giày chạy bộ Nike Air Zoom Pegasus 41 mang lại cảm giác bật nảy đầy năng lượng cho những buổi chạy hàng ngày. Kết hợp hai túi khí Air Zoom cùng lớp đệm đàn hồi hoàn toàn mới giúp tăng 13% độ hoàn trả năng lượng so với thế hệ trước, đồng thời lớp lưới dệt kỹ thuật siêu thoáng khí ôm sát bàn chân.'),

(2, N'Giày Chạy Bộ Adidas Ultraboost Siêu Nhẹ', 5, N'Adidas', 4200000, 'https://images.unsplash.com/photo-1608231387042-66d1773070a5?auto=format&fit=crop&w=900&q=80', N'Unisex', N'BanChay,GiamGia',
 N'Trải nghiệm năng lượng bất tận với giày chạy bộ Adidas Ultraboost thế hệ mới. Công nghệ đệm hạt bọt khí nhẹ hơn 30% so với thế hệ truyền thống, kết hợp thân giày sợi dệt ôm chân tự nhiên và đế ngoài cao su bám đường vượt trội trong cả điều kiện khô và ướt.'),

(3, N'Giày Tập Thể Hình Nike Metcon 9', 6, N'Nike', 3490000, 'https://images.unsplash.com/photo-1606107557195-0e29a4b5b4aa?auto=format&fit=crop&w=900&q=80', N'Nam', N'Moi',
 N'Được thiết kế dành riêng cho các vận động viên thể hình và tập luyện chuyên nghiệp, Nike Metcon 9 sở hữu tấm đệm gót lớn hơn giúp cố định trọng tâm vững chắc khi gánh tạ và nâng tạ nặng. Phần bọc cao su mở rộng ở thân giày tăng độ bền tối đa.'),

(4, N'Giày Quần Vợt Asics Court FF 3 Novak', 7, N'Asics', 4150000, 'https://images.unsplash.com/photo-1560769629-975ec94e6a86?auto=format&fit=crop&w=900&q=80', N'Unisex', N'Moi,BanChay',
 N'Mẫu giày thi đấu đỉnh cao cho bộ môn Quần vợt và Cầu lông. Công nghệ chống xoắn cổ chân và đệm giảm chấn toàn bàn chân giúp người chơi chuyển hướng cực nhanh, tiếp đất êm ái và giữ thăng bằng tối đa trong những pha cứu bóng tốc độ cao.'),

(5, N'Giày Thể Thao Cổ Thấp Nike Air Force 1', 8, N'Nike', 2950000, 'https://images.unsplash.com/photo-1595950653106-6c9ebd614d3a?auto=format&fit=crop&w=900&q=80', N'Unisex', N'BanChay',
 N'Huyền thoại bất tử của làng giày thời trang thế giới. Nike Air Force 1 giữ trọn vẻ đẹp cổ điển từ chất liệu da thật mềm mại, đường may tỉ mỉ cùng bộ đệm khí êm ái suốt ngày dài. Phù hợp với mọi phong cách từ quần bò, quần thể thao đến trang phục đi học, đi làm.'),

(6, N'Giày Thời Trang Cổ Điển Adidas Samba OG', 8, N'Adidas', 2890000, 'https://images.unsplash.com/photo-1587563871167-1ee9c731aefb?auto=format&fit=crop&w=900&q=80', N'Unisex', N'Moi,BanChay',
 N'Ra đời từ sân cỏ trong nhà và trở thành biểu tượng thời trang toàn cầu. Adidas Samba OG sở hữu thân da mềm cao cấp kết hợp mũi giày da lộn chữ T đặc trưng và đế cao su nâu cổ điển, tạo điểm nhấn tinh tế cho mọi trang phục.'),

(7, N'Giày Cổ Cao Air Jordan 1 Phiên Bản Đặc Biệt', 9, N'Jordan', 4850000, 'https://images.unsplash.com/photo-1552346154-21d32810aba3?auto=format&fit=crop&w=900&q=80', N'Nam', N'Moi,BanChay',
 N'Đôi giày mở đầu kỷ nguyên thời trang đường phố. Air Jordan 1 Cổ Cao mang phối màu đỏ đen kinh điển, cổ giày cao ôm sát bảo vệ mắt cá chân, biểu tượng đôi cánh dập nổi sắc nét cùng chất liệu da nguyên tấm cao cấp vượt thời gian.'),

(8, N'Giày Vải Cổ Cao Converse Chuck 70 Cổ Điển', 9, N'Converse', 2100000, 'https://images.unsplash.com/photo-1607522370275-f14206abe5d3?auto=format&fit=crop&w=900&q=80', N'Unisex', N'GiamGia',
 N'Phiên bản nâng cấp toàn diện của dòng Converse cổ điển. Converse Chuck 70 sử dụng vải bố định lượng cao dày dặn, đế cao su phủ bóng màu ngà cổ điển và lót đệm êm ái gấp đôi.'),

(9, N'Giày Thời Trang Đế Cao New Balance 530', 10, N'New Balance', 2790000, 'https://images.unsplash.com/photo-1539185441755-769473a23570?auto=format&fit=crop&w=900&q=80', N'Nữ', N'BanChay,GiamGia',
 N'Sự kết hợp hoàn hảo giữa thẩm mỹ hoài cổ thập niên 2000 và công nghệ chạy bộ hiện đại. Đệm đế hấp thụ lực va chạm vượt trội, thân giày phối lưới thoáng khí với các đường nét ánh bạc cá tính.'),

(10, N'Giày Đế Dày Đường Phố Puma RS-X', 10, N'Puma', 2650000, 'https://images.unsplash.com/photo-1600185365483-26d7a4cc7519?auto=format&fit=crop&w=900&q=80', N'Unisex', N'Moi,GiamGia',
 N'Thiết kế đế dày hầm hố đầy phá cách từ dòng giày thể thao trứ danh của Puma. Phối màu đa lớp ấn tượng, phần gót đúc nổi khối và phom dáng tôn chiều cao cực kỳ bắt mắt.'),

(11, N'Giày Đá Bóng Sân Nhân Tạo Nike Mercurial 15', 11, N'Nike', 2450000, 'https://images.unsplash.com/photo-1511886929837-354d827aae26?auto=format&fit=crop&w=900&q=80', N'Nam', N'Moi,BanChay',
 N'Vũ khí tốc độ trên mặt sân cỏ nhân tạo. Nike Mercurial Vapor 15 trang bị bộ đệm khí ở gót giày cùng bề mặt da tổng hợp có vân nổi giúp kiểm soát bóng dính chân và dứt điểm uy lực.'),

(12, N'Giày Bóng Rổ Chuyên Nghiệp Nike LeBron 21', 12, N'Nike', 4600000, 'https://images.unsplash.com/photo-1579338559194-a162d19bf842?auto=format&fit=crop&w=900&q=80', N'Nam', N'Moi',
 N'Mẫu giày bóng rổ thế hệ mới với hệ thống cáp cố định 360 độ kết hợp bộ đôi túi khí đàn hồi ở mũi và gót giày, hỗ trợ những pha bứt tốc, đổi hướng đột ngột và bật nhảy ghi điểm bùng nổ.'),

(13, N'Giày Lười Da Bò Anh Quốc Clarks Cao Cấp', 13, N'Clarks', 3100000, 'https://images.unsplash.com/photo-1614252369475-531eba835eb1?auto=format&fit=crop&w=900&q=80', N'Nam', N'BanChay',
 N'Đôi giày lười da bò nguyên tấm từ thương hiệu Anh Quốc Clarks. Thiết kế xỏ chân sang trọng, lót da cừu mềm mại kết hợp hệ thống lỗ thoáng khí giúp đôi chân luôn khô thoáng suốt 10 giờ làm việc.'),

(14, N'Giày Tây Công Sở Đế Êm Cole Haan', 14, N'Cole Haan', 3950000, 'https://images.unsplash.com/photo-1549298916-b41d501d3772?auto=format&fit=crop&w=900&q=80', N'Nam', N'Moi,GiamGia',
 N'Định nghĩa lại giày Tây công sở hiện đại: Thân trên là da bò đục lỗ họa tiết thủ công lịch lãm, bên dưới là bộ đế rãnh sâu linh hoạt và nhẹ nhàng tựa giày thể thao.');
SET IDENTITY_INSERT SanPham OFF;

-- 6. Biến thể sản phẩm (Mỗi sản phẩm có nhiều màu sắc & kích cỡ, giá bán, tồn kho, mã SKU)
INSERT INTO BienTheSanPham (MaSanPham, MaMau, MaKichCo, SoLuongTon, GiaBan, MaSKU) VALUES
-- SP 1
(1, 3, 5, 15, 3390000, 'NK-PEG41-CAM-40'),
(1, 3, 6, 20, 3390000, 'NK-PEG41-CAM-41'),
(1, 3, 7, 12, 3390000, 'NK-PEG41-CAM-42'),
(1, 1, 6, 18, 3490000, 'NK-PEG41-DEN-41'),
(1, 1, 7, 10, 3490000, 'NK-PEG41-DEN-42'),
(1, 4, 8, 8,  3490000, 'NK-PEG41-XANH-43'),

-- SP 2
(2, 2, 4, 14, 3690000, 'AD-UBLP-TRG-39'),
(2, 2, 5, 22, 3690000, 'AD-UBLP-TRG-40'),
(2, 2, 6, 16, 3690000, 'AD-UBLP-TRG-41'),
(2, 1, 6, 19, 3690000, 'AD-UBLP-DEN-41'),
(2, 1, 7, 11, 3690000, 'AD-UBLP-DEN-42'),
(2, 3, 7, 5,  3590000, 'AD-UBLP-CAM-42'),

-- SP 3
(3, 1, 5, 10, 3290000, 'NK-MET9-DEN-40'),
(3, 1, 6, 15, 3290000, 'NK-MET9-DEN-41'),
(3, 1, 7, 14, 3290000, 'NK-MET9-DEN-42'),
(3, 6, 7, 9,  3290000, 'NK-MET9-XAM-42'),
(3, 3, 8, 7,  3350000, 'NK-MET9-CAM-43'),

-- SP 4
(4, 4, 5, 12, 3890000, 'AS-CFF3-XANH-40'),
(4, 4, 6, 16, 3890000, 'AS-CFF3-XANH-41'),
(4, 4, 7, 10, 3890000, 'AS-CFF3-XANH-42'),
(4, 2, 5, 8,  3890000, 'AS-CFF3-TRG-40'),
(4, 2, 6, 11, 3890000, 'AS-CFF3-TRG-41'),

-- SP 5
(5, 2, 2, 25, 2790000, 'NK-AF1-TRG-37'),
(5, 2, 3, 30, 2790000, 'NK-AF1-TRG-38'),
(5, 2, 4, 28, 2790000, 'NK-AF1-TRG-39'),
(5, 2, 5, 24, 2790000, 'NK-AF1-TRG-40'),
(5, 2, 6, 20, 2790000, 'NK-AF1-TRG-41'),
(5, 1, 6, 15, 2790000, 'NK-AF1-DEN-41'),
(5, 1, 7, 12, 2790000, 'NK-AF1-DEN-42'),

-- SP 6
(6, 2, 3, 18, 2690000, 'AD-SAMBA-TRG-38'),
(6, 2, 4, 22, 2690000, 'AD-SAMBA-TRG-39'),
(6, 2, 5, 19, 2690000, 'AD-SAMBA-TRG-40'),
(6, 1, 5, 15, 2690000, 'AD-SAMBA-DEN-40'),
(6, 1, 6, 17, 2690000, 'AD-SAMBA-DEN-41'),
(6, 7, 6, 9,  2750000, 'AD-SAMBA-REU-41'),

-- SP 7
(7, 5, 5, 8,  4590000, 'JD-AJ1H-DO-40'),
(7, 5, 6, 12, 4590000, 'JD-AJ1H-DO-41'),
(7, 5, 7, 10, 4590000, 'JD-AJ1H-DO-42'),
(7, 4, 7, 7,  4650000, 'JD-AJ1H-XANH-42'),
(7, 1, 8, 6,  4590000, 'JD-AJ1H-DEN-43'),

-- SP 8
(8, 1, 2, 20, 1790000, 'CV-C70-DEN-37'),
(8, 1, 3, 25, 1790000, 'CV-C70-DEN-38'),
(8, 1, 4, 18, 1790000, 'CV-C70-DEN-39'),
(8, 8, 4, 22, 1790000, 'CV-C70-KEM-39'),
(8, 8, 5, 16, 1790000, 'CV-C70-KEM-40'),
(8, 5, 6, 12, 1850000, 'CV-C70-DO-41'),

-- SP 9
(9, 6, 1, 15, 2390000, 'NB-530-XAM-36'),
(9, 6, 2, 24, 2390000, 'NB-530-XAM-37'),
(9, 6, 3, 20, 2390000, 'NB-530-XAM-38'),
(9, 2, 3, 18, 2390000, 'NB-530-TRG-38'),
(9, 8, 4, 14, 2450000, 'NB-530-KEM-39'),

-- SP 10
(10, 3, 4, 12, 2190000, 'PM-RSX-CAM-39'),
(10, 3, 5, 16, 2190000, 'PM-RSX-CAM-40'),
(10, 3, 6, 14, 2190000, 'PM-RSX-CAM-41'),
(10, 1, 6, 11, 2190000, 'PM-RSX-DEN-41'),
(10, 4, 7, 9,  2250000, 'PM-RSX-XANH-42'),

-- SP 11
(11, 3, 5, 18, 2250000, 'NK-MER15-CAM-40'),
(11, 3, 6, 22, 2250000, 'NK-MER15-CAM-41'),
(11, 3, 7, 15, 2250000, 'NK-MER15-CAM-42'),
(11, 4, 6, 14, 2250000, 'NK-MER15-XANH-41'),
(11, 1, 7, 10, 2250000, 'NK-MER15-DEN-42'),

-- SP 12
(12, 4, 6, 9,  4290000, 'NK-LB21-XANH-41'),
(12, 4, 7, 12, 4290000, 'NK-LB21-XANH-42'),
(12, 4, 8, 10, 4290000, 'NK-LB21-XANH-43'),
(12, 5, 8, 8,  4350000, 'NK-LB21-DO-43'),
(12, 1, 9, 6,  4290000, 'NK-LB21-DEN-44'),

-- SP 13
(13, 1, 5, 14, 2890000, 'CK-LUOI-DEN-40'),
(13, 1, 6, 18, 2890000, 'CK-LUOI-DEN-41'),
(13, 1, 7, 15, 2890000, 'CK-LUOI-DEN-42'),
(13, 4, 6, 10, 2890000, 'CK-LUOI-XANH-41'),

-- SP 14
(14, 1, 5, 11, 3450000, 'CH-TAY-DEN-40'),
(14, 1, 6, 15, 3450000, 'CH-TAY-DEN-41'),
(14, 1, 7, 13, 3450000, 'CH-TAY-DEN-42'),
(14, 8, 6, 9,  3450000, 'CH-TAY-KEM-41'),
(14, 4, 8, 7,  3490000, 'CH-TAY-XANH-43');

-- 7. Khách hàng mẫu
INSERT INTO KhachHang (HoTen, Email, MatKhau, SoDienThoai, DiaChi) VALUES
(N'Lê Minh Hoàng', 'khachhang@gmail.com', '123456', '0912345678', N'125 Nguyễn Huệ, Quận 1, TP. Hồ Chí Minh'),
(N'Trần Thị Minh Anh', 'minhanh.tran@gmail.com', '123456', '0987654321', N'48 Cầu Giấy, Quận Cầu Giấy, Hà Nội'),
(N'Phạm Quốc Bảo', 'quocbao.pham@gmail.com', '123456', '0933445566', N'88 Nguyễn Văn Linh, Quận Hải Châu, Đà Nẵng');

-- 8. Chương trình khuyến mãi
INSERT INTO KhuyenMai (MaCode, TenChuongTrinh, PhanTramGiam, NgayBatDau, NgayKetThuc, TrangThai) VALUES
('WELCOME10', N'Ưu đãi chào mừng thành viên mới - Giảm 10%', 10, '2024-01-01', '2028-12-31', 1),
('STRIDE20',  N'Siêu khuyến mãi thể thao StrideLab - Giảm 20%', 20, '2024-01-01', '2028-12-31', 1),
('SPORT15',   N'Tuần lễ giày chạy bộ & tập luyện - Giảm 15%', 15, '2024-01-01', '2028-12-31', 1),
('VIP30',     N'Tri ân khách hàng thân thiết - Giảm 30%',    30, '2024-01-01', '2028-12-31', 1);

-- 9. Hóa đơn & Chi tiết hóa đơn mẫu
INSERT INTO HoaDon (MaKhachHang, MaAdmin, MaKM, TenNguoiNhan, SDTNguoiNhan, DiaChiGiaoHang, PhuongThucThanhToan, TienGiam, TongTien, TrangThai, NgayLap)
VALUES
(1, 1, 1, N'Lê Minh Hoàng', '0912345678', N'125 Nguyễn Huệ, Quận 1, TP. Hồ Chí Minh', N'COD', 339000, 3051000, N'Hoàn thành', DATEADD(DAY, -5, GETDATE())),
(2, 1, 2, N'Trần Thị Minh Anh', '0987654321', N'48 Cầu Giấy, Quận Cầu Giấy, Hà Nội', N'Chuyển khoản', 1016000, 4064000, N'Đang giao', DATEADD(DAY, -2, GETDATE())),
(1, NULL, NULL, N'Lê Minh Hoàng', '0912345678', N'125 Nguyễn Huệ, Quận 1, TP. Hồ Chí Minh', N'COD', 0, 2690000, N'Chờ xác nhận', GETDATE());

INSERT INTO ChiTietHoaDon (MaHoaDon, MaBienThe, SoLuong, DonGia)
VALUES
(1, 2, 1, 3390000),
(2, 23, 1, 2690000),
(2, 41, 1, 2390000),
(3, 29, 1, 2690000);
GO
