# ?? H??NG D?N KI?M TRA & G?I EMAIL HÓA ??N

## ?? C?u Hình Email (Web.config)

Email ?ã ???c c?u hình trong `Web.config`:

```xml
<add key="Email:SmtpServer" value="smtp.gmail.com" />
<add key="Email:SmtpPort" value="587" />
<add key="Email:SenderEmail" value="daianh2k00@gmail.com" />
<add key="Email:SenderPassword" value="oxpf ewhq bqyx gvwf" />
<add key="Email:EnableSSL" value="true" />
<add key="Email:InvoiceFolder" value="Content/hoadon" />
```

? ?ã c?u hình s?n Gmail SMTP

---

## ?? TEST G?I EMAIL

### 1?? Test Endpoint (Postman)

**POST**: `http://localhost:5000/api/customer/test-send-invoice/123`

(Thay `123` b?ng booking ID th?c t?)

**Headers**:
```
Authorization: Bearer {token}
Content-Type: application/json
```

**Response thành công**:
```json
{
  "success": true,
  "message": "? Email hóa ??n g?i thành công!",
  "details": {
    "recipient": "customer@email.com",
    "customer_name": "Nguy?n V?n A",
    "invoice_file": "Invoice_BK000123_20240101_123456.html",
    "booking_id": 123
  }
}
```

---

## ?? TÍNH N?NG G?I EMAIL

### Endpoint 1: G?i Hóa ??n Th? Công

**POST**: `/api/customer/send-invoice-email/{bookingId}`

- G?i hóa ??n cho khách hàng
- C?n xác th?c (token)
- Ki?m tra email khách hàng có h?p l? không

### Endpoint 2: T? ??ng G?i Khi Thanh Toán

**POST**: `/api/customer/auto-send-invoice-on-payment/{bookingId}`

- G?i t? payment gateway callback
- Không c?n xác th?c (AllowAnonymous)
- T? ??ng t?o hóa ??n & g?i email

### Endpoint 3: L?y HTML Hóa ??n

**GET**: `/api/customer/invoice-html/{bookingId}`

- L?y HTML hóa ??n ?? hi?n th? trên app
- C?n xác th?c (token)

---

## ?? FILE HÓA ??N

**???ng d?n l?u**: `Content/hoadon/`

**Format file**: `Invoice_BK{BookingId:D6}_{DateTime:yyyyMMdd_HHmmss}.html`

**Ví d?**: `Invoice_BK000123_20240101_143022.html`

---

## ?? DEBUG

Ki?m tra logs trong `Infrastructure/LoggingHelper.cs`:

```
? Email hóa ??n g?i thành công: customer@email.com - File: Invoice_BK000123_20240101_143022.html
? Hóa ??n ???c g?i qua email: Booking 123 -> customer@email.com
```

---

## ?? Các V?n ?? Th??ng G?p

### 1. Email không g?i ???c

**Ki?m tra**:
- Khách hàng có email không? (`Customer.email` không null/empty)
- File hóa ??n ???c l?u ch?a? (Ki?m tra folder `Content/hoadon`)
- C?u hình Gmail SMTP ?úng không?

**Gi?i pháp**:
- ??m b?o `daianh2k00@gmail.com` có b?t "Less Secure App Access"
- Ho?c dùng App Password t? Google Account

### 2. File hóa ??n không ???c l?u

**Ki?m tra**:
- Folder `Content/hoadon` t?n t?i không?
- Có quy?n write vào folder không?

**Gi?i pháp**:
- T?o folder: `mkdir Content/hoadon`
- Ki?m tra quy?n truy c?p

### 3. HTML hóa ??n b? tr?ng

**Ki?m tra**:
- Booking có d? li?u không?
- Vé (Ves) có ???c t?o không?

**Gi?i pháp**:
- Ki?m tra DB: `SELECT * FROM Dat_Ve WHERE Dat_Ve_id = 123`

---

## ?? LU?NG HO?T ??NG

```
1. Khách hàng thanh toán thành công
   ?
2. Payment gateway g?i callback
   ?
3. UpdateDat_Ve tr?ng thái ? "?ã Thanh toán"
   ?
4. G?i: POST /api/customer/auto-send-invoice-on-payment/123
   ?
5. Service t?o file hóa ??n HTML
   ?
6. L?u vào: Content/hoadon/Invoice_BK000123_....html
   ?
7. G?i email qua Gmail SMTP
   ?
8. Khách hàng nh?n email + file hóa ??n ?ính kèm
```

---

## ?? NEXT STEPS

1. Restart app (hot reload có th? không ??)
2. Test endpoint v?i Postman
3. Ki?m tra logs xem có error gì
4. N?u OK, tích h?p vào InvoiceController.cs payment success handler

---

## ?? EMAIL SAMPLE

**Subject**: Hóa ??n Mua Vé Xem Phim - WebCinema

**Body**: HTML v?i:
- Logo WebCinema
- Thông tin khách hàng
- Chi ti?t phim, su?t chi?u
- Danh sách vé
- T?ng ti?n
- File ?ính kèm: `Invoice_BK000123_20240101_143022.html`

---

**Created**: 2024-01-01  
**Last Updated**: 2024-01-01
