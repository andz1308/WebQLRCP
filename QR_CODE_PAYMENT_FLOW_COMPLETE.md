# QR Code Payment Flow Implementation Guide

## ? Tóm T?t Thay ??i

H? th?ng QR Code Payment (VietQR) ?ã ???c tích h?p hoàn ch?nh vào flow ??t vé. Khi khách hàng ??t vé, s? tr?c ti?p hi?n th? mã QR trên trang thanh toán (B??c 3).

---

## ?? Quy Trình ??t Vé M?i (4 B??c)

### **B??c 1: Ch?n Gh?**
```
SelectSeats ? Hi?n th? s? ?? gh?
```

### **B??c 2: Ch?n ?? ?n**
```
SelectFood ? Hi?n th? menu ?? ?n & th?c u?ng
```

### **B??c 3: Thanh Toán QR Code** ?
```
Checkout ? Hi?n th?:
  - Thông tin ??n hàng
  - QR Code thanh toán
  - H??ng d?n quét
  - Nút "Xác Nh?n ?ã Thanh Toán"
```

### **B??c 4: Hóa ??n & Vé**
```
PaymentSuccess ? Hi?n th?:
  - Hóa ??n chi ti?t
  - QR Code (?? in)
  - Nút "In Hóa ??n & QR Code"
  - Nút "Quay V? Trang Ch?"
```

---

## ?? File ?ã Thay ??i

| File | Thay ??i |
|------|----------|
| `Checkout.cshtml` | ? Thay th? VNPay ? QR Code Payment |
| `PaymentSuccess.cshtml` | ? Thêm QR Code + In hóa ??n |
| `BookingController.cs` | ? Thêm QR code generation ? Checkout |
| `InvoiceController.cs` | ? Thêm QR code + food items ? PaymentSuccess |
| `QRCodePaymentService.cs` | ? Service t?o mã QR VietQR |
| `Web.config` | ? Config ngân hàng (BankId, AccountNo) |

---

## ?? C?u Hình Web.config

**Hi?n t?i Web.config ?ã có:**

```xml
<appSettings>
    <!-- QR Code Payment Settings -->
    <add key="QRCode:BankId" value="970436" />          <!-- Vietcombank -->
    <add key="QRCode:AccountNo" value="1031419192" />   <!-- S? tài kho?n -->
    <add key="QRCode:AccountName" value="NGUYEN DUY AN" /> <!-- Tên tài kho?n -->
</appSettings>
```

**C?n thay ??i n?u dùng ngân hàng khác:**

| Ngân Hàng | Bank ID |
|-----------|---------|
| Vietcombank (VCB) | 970436 |
| Techcombank (TCB) | 970407 |
| VIB | 970441 |
| BIDV | 970418 |
| Agribank | 970405 |
| MB Bank | 970422 |
| ACB | 970409 |
| Sacombank | 970421 |
| TP Bank | 970423 |

**Cách c?p nh?t:**
```xml
<!-- Ví d?: Thay ??i sang Techcombank -->
<add key="QRCode:BankId" value="970407" />
<add key="QRCode:AccountNo" value="0123456789" /> <!-- S? tài kho?n c?a b?n -->
<add key="QRCode:AccountName" value="TEN CONG TY" /> <!-- Tên tài kho?n -->
```

---

## ?? Quy Trình Thanh Toán Chi Ti?t

### **1?? B??c 1-2: SelectSeats & SelectFood**
```
User ch?n gh? + ?? ?n
?
POST /Booking/Payment
?
T?o Dat_Ve (tr?ng thái: "Ch?a thanh toán")
L?u Ves + DonHang_DoAns
Tính t?ng ti?n
?
Redirect ? Checkout
```

### **2?? B??c 3: Checkout - Hi?n Th? QR**
```
GET /Booking/Checkout
?
Load Dat_Ve t? DB
L?y Tickets + Food Orders
QRCodePaymentService.GenerateQRCodeUrl():
  - Bank ID: 970436 (t? config)
  - Account: 1031419192 (t? config)
  - Amount: T?ng ti?n
  - Description: DatVe{BookingId}
  ?
  URL: https://api.vietqr.io/image/970436-1031419192-500000-DatVe123-compact.jpg
?
ViewBag.QRCodeUrl = qrUrl
?
Render Checkout.cshtml (hi?n th? QR)
```

### **3?? B??c 3.5: Khách Hàng Quét & Thanh Toán**
```
Khách hàng:
  1. M? app ngân hàng
  2. Quét mã QR
  3. Chuy?n ti?n
  4. Nh?n "Xác Nh?n ?ã Thanh Toán"
```

### **4?? B??c 3.6: Xác Nh?n Thanh Toán (AJAX)**
```
confirmQRPayment(bookingId)
?
POST /Invoice/ConfirmQRPayment
?
C?p nh?t Dat_Ve:
  - trang_thai_Dat_Ve = "?ã Thanh toán"
  - Ves.trang_thai_ve = "Ch?a s? d?ng"
?
Response: { success: true, redirectUrl: "/Invoice/PaymentSuccess?bookingId=123" }
?
Redirect ? PaymentSuccess
```

### **5?? B??c 4: PaymentSuccess - Hóa ??n & QR**
```
GET /Invoice/PaymentSuccess?bookingId=123
?
Load Dat_Ve, Tickets, Food Orders, Showtime
Tính l?i totals
QRCodePaymentService.GenerateQRCodeUrl()
  (T?o QR code l?i ?? hi?n th? trên hóa ??n in)
?
ViewBag:
  - QRCodeUrl (?? in)
  - Booking
  - Tickets
  - FoodItems
  - TicketTotal, FoodTotal, GrandTotal
?
Render PaymentSuccess.cshtml
?
User có th?:
  - Xem hóa ??n chi ti?t
  - In hóa ??n (bao g?m c? QR code)
  - Quay v? trang ch?
```

---

## ?? Database State Changes

### **Booking Record Timeline**

| Th?i ?i?m | trang_thai_Dat_Ve | Ve.trang_thai | Thao Tác |
|-----------|-------------------|---------------|---------|
| SelectFood POST | "Ch?a thanh toán" | "Ch?a s? d?ng" | T?o Dat_Ve |
| Checkout GET | "Ch?a thanh toán" | "Ch?a s? d?ng" | Hi?n th? QR |
| Confirm POST | "?ã Thanh toán" | "Ch?a s? d?ng" | C?p nh?t tr?ng thái |
| PaymentSuccess GET | "?ã Thanh toán" | "Ch?a s? d?ng" | Hi?n th? hóa ??n |

### **Gh? (Ghe) Lock Timeline**

| Tr?ng Thái Dat_Ve | Gh? Có B? Lock? | Ghi Chú |
|------------------|-----------------|---------|
| "Ch?a thanh toán" | ? Không | Gh? v?n available cho user khác |
| "?ã Thanh toán" | ? Có | Gh? b? lock cho user khác |
| "?ã H?y" | ? Không | Gh? ???c gi?i phóng |

---

## ?? B?o M?t & Xác Minh

### **Hi?n T?i (Manual Confirmation)**
```
? User xác nh?n th? công qua nút button
? Không c?n ki?m tra ngân hàng
? ??n gi?n, nhanh chóng
? Có th? user ?n nh?ng không chuy?n ti?n
```

### **T??ng Lai (Auto Verification)**
Có th? tích h?p:
1. **Core Banking API** - query xem có giao d?ch không
2. **Webhook t? Ngân Hàng** - callback khi có chuy?n ti?n
3. **QR Dynamic Payment** - real-time verification

---

## ?? Test Locally

### **B??c 1: C?u Hình Web.config**
```xml
<!-- Dùng tài kho?n test c?a b?n -->
<add key="QRCode:BankId" value="970436" />
<add key="QRCode:AccountNo" value="1031419192" />
<add key="QRCode:AccountName" value="NGUYEN DUY AN" />
```

### **B??c 2: Ch?y App**
```
http://localhost:5000
```

### **B??c 3: ??t Vé**
1. ??ng nh?p
2. Ch?n phim ? Ch?n gh?
3. Ch?n ?? ?n
4. Nh?n "Thanh Toán"
5. **Trang Checkout s? hi?n th? QR Code** ?

### **B??c 4: Test QR Code**
- M? app ngân hàng trên ?i?n tho?i
- Quét mã QR
- App s? t? ?i?n:
  - Tài kho?n: 1031419192
  - S? ti?n: VN? (t? order)
  - N?i dung: DatVe123 (Booking ID)

### **B??c 5: Xác Nh?n**
- Nh?n "Xác Nh?n ?ã Thanh Toán"
- Redirect ? PaymentSuccess
- Hi?n th? hóa ??n + QR Code
- Có th? in

---

## ?? Database Queries (Ki?m Tra)

### **Check Booking Status**
```sql
SELECT Dat_Ve_id, trang_thai_Dat_Ve, tong_tien, ngay_tao 
FROM Dat_Ve 
WHERE Dat_Ve_id = 123
```

### **Check Booked Seats**
```sql
SELECT v.ve_id, v.ghe_id, g.so_ghe, v.Dat_Ve_id, dv.trang_thai_Dat_Ve
FROM Ve v
JOIN Ghe g ON v.ghe_id = g.ghe_id
LEFT JOIN Dat_Ve dv ON v.Dat_Ve_id = dv.Dat_Ve_id
WHERE v.Suat_Chieu_id = 5
```

### **Check Food Orders**
```sql
SELECT f.*, da.ten_san_pham, da.gia
FROM DonHang_DoAn f
JOIN Do_An da ON f.Do_An_id = da.Do_An_id
WHERE f.Dat_Ve_id = 123
```

---

## ?? Troubleshooting

### ? QR Code không hi?n th?
**Nguyên nhân:** Config Web.config sai ho?c m?ng không k?t n?i VietQR API
**Gi?i pháp:**
```
1. Check Web.config BankId, AccountNo, AccountName
2. Test URL tr?c ti?p: https://api.vietqr.io/image/970436-1031419192-500000-DatVe123-compact.jpg
3. N?u URL không t?i, VietQR API down
```

### ? Gh? v?n hi?n th? available sau khi thanh toán
**Nguyên nhân:** Dat_Ve không update sang "?ã Thanh toán"
**Gi?i pháp:**
```sql
-- Ki?m tra manual
SELECT * FROM Dat_Ve WHERE Dat_Ve_id = 123

-- N?u v?n "Ch?a thanh toán", update th? công
UPDATE Dat_Ve SET trang_thai_Dat_Ve = '?ã Thanh toán' WHERE Dat_Ve_id = 123
```

### ? Redirect không ho?t ??ng
**Nguyên nhân:** Session ["BookingId"] b? m?t
**Gi?i pháp:**
```
1. Check browser console (F12) ? Network tab
2. Xem có error nào không
3. Check server logs (LoggingHelper)
```

---

## ?? Ghi Chú Quan Tr?ng

1. **QR Code t?nh**: Mã QR ???c t?o tr??c khi user quét, không thay ??i
2. **Timeout**: N?u user không xác nh?n trong vòng ~30 phút, booking h?t h?n (có th? thêm tính n?ng)
3. **L?n quét th? 2**: N?u user quét l?i mã QR sau 1 gi?, s? ti?n v?n nh? c? (QR t?nh)
4. **In hóa ??n**: QR code c?ng ???c in, user có th? dùng ?? tham kh?o

---

## ? Checklist Deployment

- [x] Web.config c?u hình BankId, AccountNo, AccountName
- [x] QRCodePaymentService.cs compiled
- [x] Checkout.cshtml hi?n th? QR
- [x] PaymentSuccess.cshtml có QR + in ???c
- [x] BookingController Checkout() t?o QR
- [x] InvoiceController PaymentSuccess() t?o QR + food items
- [x] Build successful
- [ ] Test ??t vé end-to-end
- [ ] Test quét QR
- [ ] Test in hóa ??n
- [ ] Deploy lên production

---

**Phiên B?n**: 1.0 - QR Code Payment (VietQR) Complete Flow
**Ngày**: 2024
**Status**: ? Ready to Test
