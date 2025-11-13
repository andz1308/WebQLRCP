# QR Code Payment (VietQR) Implementation Guide

## ?? Tóm T?t Thay ??i

H? th?ng thanh toán ?ã ???c thay th? t? **VNPay** sang **QR Code Payment (VietQR)**:

### ? Cái Gì ?ã Thay ??i

1. **InvoiceController.cs**
   - ? Xóa/Comment: `InitiatePayment()` (VNPay)
   - ? Xóa/Comment: `PaymentCallback()` (VNPay callback)
   - ? Xóa/Comment: `PaymentIpn()` (VNPay IPN)
   - ? Thêm: `ConfirmQRPayment()` (xác nh?n thanh toán QR)
   - ? Thêm: T?o QR code URL trong `ViewInvoice()`

2. **ViewInvoice.cshtml**
   - ? Xóa: Nút "Thanh Toán VNPAY"
   - ? Thêm: Ph?n hi?n th? QR Code
   - ? Thêm: H??ng d?n quét QR
   - ? Thêm: Nút "Xác Nh?n ?ã Thanh Toán"

3. **T?o M?i**
   - ? `QRCodePaymentService.cs` - T?o mã QR VietQR

---

## ?? C?u Hình

Thêm các setting vào **Web.config**:

```xml
<appSettings>
  <!-- QR Code Payment Settings -->
  <add key="QRCode:BankId" value="970436" />          <!-- Vietcombank -->
  <add key="QRCode:AccountNo" value="1234567890" />   <!-- S? tài kho?n ngân hàng -->
  <add key="QRCode:AccountName" value="DAV CINEMA" /> <!-- Tên ch? tài kho?n -->
</appSettings>
```

### ?? Bank Codes (VietQR Standard)

| Ngân Hàng | Bank ID |
|-----------|---------|
| Vietcombank (VCB) | 970436 |
| Techcombank (TCB) | 970407 |
| VIB | 970441 |
| BIDV | 970418 |
| Agribank | 970405 |
| MB Bank | 970422 |
| ACB | 970409 |
| SacomBank | 970421 |
| TP Bank | 970423 |

---

## ?? Quy Trình Thanh Toán

### 1?? Khách Hàng Vào Trang Hóa ??n
- **URL**: `/Invoice/ViewInvoice?bookingId=123`
- Controller t?o QR code: `QRCodePaymentService.GenerateQRCodeUrl()`
- View hi?n th? QR code

### 2?? Quét Mã QR
- Khách hàng m? app ngân hàng (Mobile Banking, Momo, ZaloPay, v.v.)
- Quét mã QR hi?n th?
- App ngân hàng t? ?i?n thông tin:
  - **Tài kho?n nh?n**: 1234567890
  - **S? ti?n**: 500,000? (ví d?)
  - **N?i dung chuy?n**: DatVe123 (mã ??n hàng)

### 3?? Xác Nh?n Thanh Toán
- Khách hàng nh?n nút "**Xác Nh?n ?ã Thanh Toán**"
- G?i AJAX: `POST /Invoice/ConfirmQRPayment`
- Controller c?p nh?t `Dat_Ve.trang_thai_Dat_Ve = "?ã Thanh toán"`
- C?p nh?t tr?ng thái vé: `Ve.trang_thai_ve = "Ch?a s? d?ng"`
- Redirect sang trang thành công

---

## ?? Mã QR Format

**URL VietQR API:**
```
https://api.vietqr.io/image/{bank_id}-{account_no}-{amount}-{description}-compact.jpg
```

**Ví d?:**
```
https://api.vietqr.io/image/970436-1234567890-500000-DatVe123-compact.jpg
```

**Gi?i thích:**
- `970436` = Vietcombank Bank ID
- `1234567890` = S? tài kho?n
- `500000` = S? ti?n (VN?)
- `DatVe123` = Mô t? (Booking ID)
- `compact` = ??nh d?ng nh? g?n

---

## ?? B?o M?t

### ? Cách Xác Minh Thanh Toán (T??ng Lai)

Hi?n t?i, thanh toán ???c **xác nh?n manual** b?ng cách:
1. Khách hàng quét QR và thanh toán
2. Khách hàng nh?n nút "Xác Nh?n ?ã Thanh Toán"

### ?? Cách T??ng Lai (T? ??ng Xác Minh)

Có 2 cách tích h?p t? ??ng:

**Cách 1: Tích H?p V?i Core Banking API**
```csharp
// Connect v?i API ngân hàng ?? check giao d?ch
public bool VerifyPaymentFromBank(int bookingId, decimal amount)
{
    // Call Bank API to verify transaction
    // Return true n?u giao d?ch th?c s? chuy?n ti?n
}
```

**Cách 2: Webhook T? Ngân Hàng**
- Ngân hàng g?i POST request khi có giao d?ch
- Parse thông tin n?i dung chuy?n ?? l?y Booking ID
- T? ??ng c?p nh?t tr?ng thái

---

## ??? Tích H?p Mobile App

### Android/iOS Integration

```javascript
// App ngân hàng t? ??ng quét QR
const qrUrl = "https://api.vietqr.io/image/970436-1234567890-500000-DatVe123-compact.jpg";

// T?o QR code data
const bankTransferData = {
    bank: "VCB",
    account: "1234567890",
    amount: 500000,
    description: "DatVe123"
};

// App s? t? ??ng fill
// User ch? c?n confirm và enter PIN
```

---

## ?? C? S? D? Li?u

**B?ng: Dat_Ve**
```sql
ALTER TABLE Dat_Ve ADD COLUMN payment_method VARCHAR(50);
-- Giá tr?: 'QR_CODE', 'VNPAY' (c?), 'CASH', v.v.

ALTER TABLE Dat_Ve ADD COLUMN payment_confirmed_at DATETIME;
-- L?u th?i gian xác nh?n thanh toán
```

---

## ?? Test C?c B?

```
1. Ch?y ?ng d?ng: http://localhost:5000

2. ??t vé:
   - Ch?n phim/gh?
   - Ch?n ?? ?n
   - Vào thanh toán

3. Trang ViewInvoice s? hi?n th?:
   ? Mã QR
   ? H??ng d?n
   ? Nút "Xác Nh?n ?ã Thanh Toán"

4. Click "Xác Nh?n" ? Thanh toán thành công
   ? Tr?ng thái ??n hàng: "?ã Thanh toán"
   ? Vé: "Ch?a s? d?ng"
   ? Redirect sang PaymentSuccess page
```

---

## ?? Chi Ti?t Hàm QRCodePaymentService

### GenerateQRCodeUrl()
```csharp
// T?o URL mã QR
string qrUrl = qrService.GenerateQRCodeUrl(500000m, "DatVe123");
// Return: https://api.vietqr.io/image/970436-1234567890-500000-DatVe123-compact.jpg
```

### GenerateTransactionDescription()
```csharp
// T?o mô t? giao d?ch t? Booking ID
string desc = qrService.GenerateTransactionDescription(123);
// Return: "DatVe123"
```

### CleanDescription() (Private)
```csharp
// Làm s?ch ký t? ??c bi?t, t?i ?a 25 ký t?
// VD: "??t vé phim#123!" ? "DatVephim123"
```

---

## ?? L?u Ý Quan Tr?ng

1. **Xác Nh?n Manual**: 
   - Hi?n t?i ch? dùng xác nh?n manual
   - C?n giáo d?c khách hàng check SMS/App xác nh?n

2. **Mô T? Giao D?ch**: 
   - Ch? gi? 25 ký t? (gi?i h?n VietQR)
   - Ví d?: `DatVe123` = Booking ID 123

3. **Bank Support**: 
   - H?u h?t ngân hàng Vi?t h? tr? VietQR
   - Mobile app + Mobile Banking + e-wallet

4. **Testing**: 
   - QR code có th? quét b?ng b?t k? app nào
   - Không c?n VNPay sandbox

---

## ?? Support URLs

- **VietQR API**: https://api.vietqr.io
- **VietQR Docs**: https://vietqr.io/document
- **NganLuong**: https://nganluong.vn (alternative)

---

## ? Checklist Deployment

- [ ] C?p nh?t `Web.config` v?i Bank ID, Account
- [ ] Ki?m tra QuoteCodePaymentService.cs compile
- [ ] Test xem mã QR hi?n th? ?úng
- [ ] Test xác nh?n thanh toán
- [ ] Check log: `LoggingHelper.LogInfo()`
- [ ] Deploy lên server

---

**Ngày C?p Nh?t**: 2024
**Phiên B?n**: 1.0 (QR Code Payment - VietQR Standard)
**Status**: ? Active
