-- =====================================================
-- SCHEMA CHO HỆ THỐNG NHẬP HÀNG
-- =====================================================

-- 1. Bảng Do_An (Đã có, cần thêm cột)
ALTER TABLE Do_An
ADD so_luong INT NULL DEFAULT 0,  -- Số lượng tồn kho hiện tại
    gia_von DECIMAL(12,2) NULL;    -- Giá vốn trung bình

-- 2. Bảng Nhà Cung Cấp (Nếu chưa có)
CREATE TABLE Nha_Cung_Cap (
    nha_cung_cap_id INT IDENTITY(1,1) PRIMARY KEY,
    ten_nha_cung_cap NVARCHAR(200) NOT NULL,
    dia_chi NVARCHAR(MAX),
    so_dien_thoai NVARCHAR(20),
    email NVARCHAR(100),
    nguoi_lien_he NVARCHAR(100),
    ghi_chu NVARCHAR(MAX),
    trang_thai NVARCHAR(50) DEFAULT N'Hoạt động', -- 'Hoạt động', 'Ngừng hợp tác'
    ngay_tao DATETIME2 DEFAULT GETDATE()
);

-- 3. Bảng Phiếu Nhập Hàng (Purchase Order)
CREATE TABLE Phieu_Nhap_Hang (
    phieu_nhap_id INT IDENTITY(1,1) PRIMARY KEY,
    ma_phieu NVARCHAR(50) UNIQUE NOT NULL, -- Mã phiếu: PN-20240101-001
    nha_cung_cap_id INT FOREIGN KEY REFERENCES Nha_Cung_Cap(nha_cung_cap_id),
    nhan_vien_id INT FOREIGN KEY REFERENCES Nhan_Vien(nhanvien_id), -- Người tạo phiếu
    ngay_nhap DATETIME2 DEFAULT GETDATE(),
    ngay_lap_phieu DATETIME2 DEFAULT GETDATE(),
    tong_tien DECIMAL(18,2) NOT NULL DEFAULT 0,
    ghi_chu NVARCHAR(MAX),
    trang_thai NVARCHAR(50) DEFAULT N'Chưa duyệt', -- 'Chưa duyệt', 'Đã duyệt', 'Đã hủy'
    nguoi_duyet_id INT FOREIGN KEY REFERENCES Nhan_Vien(nhanvien_id), -- Admin duyệt
    ngay_duyet DATETIME2,
    ly_do_huy NVARCHAR(MAX)
);

-- 4. Bảng Chi Tiết Phiếu Nhập
CREATE TABLE Chi_Tiet_Phieu_Nhap (
    chi_tiet_id INT IDENTITY(1,1) PRIMARY KEY,
    phieu_nhap_id INT NOT NULL FOREIGN KEY REFERENCES Phieu_Nhap_Hang(phieu_nhap_id),
    do_an_id INT NOT NULL FOREIGN KEY REFERENCES Do_An(Do_An_id),
    so_luong INT NOT NULL CHECK(so_luong > 0),
    don_gia DECIMAL(12,2) NOT NULL CHECK(don_gia >= 0), -- Giá nhập
    thanh_tien AS (so_luong * don_gia) PERSISTED, -- Tự động tính
    ghi_chu NVARCHAR(MAX)
);

-- 5. Bảng Lịch Sử Tồn Kho (Audit Trail)
CREATE TABLE Lich_Su_Ton_Kho (
    lich_su_id INT IDENTITY(1,1) PRIMARY KEY,
    do_an_id INT NOT NULL FOREIGN KEY REFERENCES Do_An(Do_An_id),
    loai_bien_dong NVARCHAR(50) NOT NULL, -- 'Nhập', 'Xuất', 'Điều chỉnh', 'Hủy'
    so_luong_truoc INT NOT NULL,
    so_luong_bien_dong INT NOT NULL, -- +10, -5, etc
    so_luong_sau INT NOT NULL,
    phieu_nhap_id INT FOREIGN KEY REFERENCES Phieu_Nhap_Hang(phieu_nhap_id),
    dat_ve_id INT FOREIGN KEY REFERENCES Dat_Ve(Dat_Ve_id), -- Nếu xuất do bán
    nhan_vien_id INT FOREIGN KEY REFERENCES Nhan_Vien(nhanvien_id),
    ngay_bien_dong DATETIME2 DEFAULT GETDATE(),
    ghi_chu NVARCHAR(MAX)
);

-- =====================================================
-- INDEXES ĐỂ TỐI ƯU HIỆU SUẤT
-- =====================================================
CREATE INDEX IX_PhieuNhap_NgayNhap ON Phieu_Nhap_Hang(ngay_nhap);
CREATE INDEX IX_PhieuNhap_TrangThai ON Phieu_Nhap_Hang(trang_thai);
CREATE INDEX IX_ChiTiet_PhieuNhap ON Chi_Tiet_Phieu_Nhap(phieu_nhap_id);
CREATE INDEX IX_ChiTiet_DoAn ON Chi_Tiet_Phieu_Nhap(do_an_id);
CREATE INDEX IX_LichSu_DoAn ON Lich_Su_Ton_Kho(do_an_id);

-- =====================================================
-- TRIGGER: TỰ ĐỘNG CẬP NHẬT TỒN KHO KHI DUYỆT PHIẾU
-- =====================================================
GO
CREATE TRIGGER trg_UpdateStock_AfterApproval
ON Phieu_Nhap_Hang
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Chỉ chạy khi trạng thái chuyển từ 'Chưa duyệt' sang 'Đã duyệt'
    IF EXISTS (
        SELECT 1 
        FROM inserted i
        INNER JOIN deleted d ON i.phieu_nhap_id = d.phieu_nhap_id
        WHERE i.trang_thai = N'Đã duyệt' AND d.trang_thai = N'Chưa duyệt'
    )
    BEGIN
        -- Cập nhật tồn kho
        UPDATE da
        SET da.so_luong = ISNULL(da.so_luong, 0) + ct.so_luong,
            -- Cập nhật giá vốn theo phương pháp bình quân gia quyền
            da.gia_von = (
                (ISNULL(da.so_luong, 0) * ISNULL(da.gia_von, 0)) + (ct.so_luong * ct.don_gia)
            ) / (ISNULL(da.so_luong, 0) + ct.so_luong)
        FROM Do_An da
        INNER JOIN Chi_Tiet_Phieu_Nhap ct ON da.Do_An_id = ct.do_an_id
        INNER JOIN inserted i ON ct.phieu_nhap_id = i.phieu_nhap_id;
        
        -- Ghi lịch sử
        INSERT INTO Lich_Su_Ton_Kho (
            do_an_id, 
            loai_bien_dong, 
            so_luong_truoc, 
            so_luong_bien_dong, 
            so_luong_sau,
            phieu_nhap_id,
            nhan_vien_id,
            ghi_chu
        )
        SELECT 
            da.Do_An_id,
            N'Nhập',
            ISNULL(da.so_luong, 0) - ct.so_luong,
            ct.so_luong,
            ISNULL(da.so_luong, 0),
            i.phieu_nhap_id,
            i.nguoi_duyet_id,
            N'Duyệt phiếu nhập: ' + i.ma_phieu
        FROM Do_An da
        INNER JOIN Chi_Tiet_Phieu_Nhap ct ON da.Do_An_id = ct.do_an_id
        INNER JOIN inserted i ON ct.phieu_nhap_id = i.phieu_nhap_id;
    END
END;
GO

-- =====================================================
-- TRIGGER: TỰ ĐỘNG TRỪ TỒN KHO KHI BÁN HÀNG
-- =====================================================
GO
CREATE TRIGGER trg_UpdateStock_AfterSale
ON DonHang_DoAn
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Chỉ trừ kho khi đơn đã thanh toán
    IF EXISTS (
        SELECT 1 
        FROM inserted i
        INNER JOIN Dat_Ve dv ON i.Dat_Ve_id = dv.Dat_Ve_id
        WHERE dv.trang_thai_Dat_Ve = N'Đã Thanh toán'
    )
    BEGIN
        -- Trừ tồn kho
        UPDATE da
        SET da.so_luong = ISNULL(da.so_luong, 0) - i.so_luong
        FROM Do_An da
        INNER JOIN inserted i ON da.Do_An_id = i.Do_An_id
        INNER JOIN Dat_Ve dv ON i.Dat_Ve_id = dv.Dat_Ve_id
        WHERE dv.trang_thai_Dat_Ve = N'Đã Thanh toán';
        
        -- Ghi lịch sử
        INSERT INTO Lich_Su_Ton_Kho (
            do_an_id, 
            loai_bien_dong, 
            so_luong_truoc, 
            so_luong_bien_dong, 
            so_luong_sau,
            dat_ve_id,
            ghi_chu
        )
        SELECT 
            da.Do_An_id,
            N'Xuất',
            ISNULL(da.so_luong, 0) + i.so_luong,
            -i.so_luong,
            ISNULL(da.so_luong, 0),
            i.Dat_Ve_id,
            N'Bán hàng - Đơn: ' + CAST(i.Dat_Ve_id AS NVARCHAR)
        FROM Do_An da
        INNER JOIN inserted i ON da.Do_An_id = i.Do_An_id
        INNER JOIN Dat_Ve dv ON i.Dat_Ve_id = dv.Dat_Ve_id
        WHERE dv.trang_thai_Dat_Ve = N'Đã Thanh toán';
    END
END;
GO

-- =====================================================
-- DỮ LIỆU MẪU
-- =====================================================

-- Thêm nhà cung cấp mẫu
INSERT INTO Nha_Cung_Cap (ten_nha_cung_cap, dia_chi, so_dien_thoai, email, nguoi_lien_he)
VALUES 
(N'Công ty TNHH Thực phẩm Việt', N'123 Trần Hưng Đạo, Q1, HCM', '0901234567', 'sales@thucphamviet.vn', N'Nguyễn Văn A'),
(N'Công ty CP Đồ uống Miền Nam', N'456 Lê Lợi, Q3, HCM', '0907654321', 'contact@douongmiennam.com', N'Trần Thị B'),
(N'Công ty TNHH Snack Quốc tế', N'789 Nguyễn Huệ, Q1, HCM', '0912345678', 'info@snackintl.vn', N'Lê Văn C');

-- Cập nhật số lượng ban đầu cho sản phẩm có sẵn
UPDATE Do_An SET so_luong = 100 WHERE ten_san_pham LIKE N'%Bắp%';

GO
