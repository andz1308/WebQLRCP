# S?a H? Th?ng H?y Vé - T? ??ng + Email Thông Báo

## ?? T?ng Quát Thay ??i

### 1. **H?y Vé T? ??ng (Không C?n Duy?t)**
- ? Khi khách hàng xác nh?n h?y vé, vé s? **h?y li?n ngay l?p t?c**
- ? Không còn c?n admin duy?t
- ? Tr?ng thái yêu c?u h?y ? **"?ã H?y"** (thay vì "Ch? duy?t")
- ? T?t c? vé trong ??n ? **"?ã H?y"**
- ? ??n ??t ? **"?ã H?y"**

### 2. **G?i Email Thông Báo**
- ? **T? ??ng g?i email** cho khách hàng khi h?y vé thành công
- ? Email ch?a thông tin chi ti?t hoàn ti?n:
  - Mã ??n
  - S? vé
  - T?ng ti?n g?c
  - S? ti?n hoàn l?i (70%)
  - Phí h?y (30%)
  - Thông tin ngân hàng
  - Th?i gian d? ki?n nh?n ti?n (1-3 ngày)

### 3. **Thêm Ch?n Ngân Hàng + STK**
- ? Form h?y vé bây gi? có:
  - **Dropdown ch?n Ngân hàng** (15+ ngân hàng ph? bi?n Vi?t Nam)
  - **Input nh?p S? tài kho?n** (10-20 ch? s?, b?t bu?c)
  - Validation client-side tr??c khi g?i

### 4. **L?u Tr? Thông Tin**
- ? D? li?u l?u vào database d??i d?ng: `"BankCode:AccountNumber"`
  - VD: `"VCB:1234567890"` (Vietcombank, STK 1234567890)
  - VD: `"BIDV:0987654321"` (BIDV, STK 0987654321)

### 5. **Hi?n Th? Thông Tin**
- ? Trang chi ti?t vé hi?n th? ngân hàng và STK che d?u
  - VD: `"Ngân hàng: VCB"` ? `"?? Vietcombank (VCB)"`
  - VD: `"STK: ****7890"` (ch? hi?n th? 4 ch? s? cu?i)

---

## ?? Files ?ã Thay ??i

### 1. **WebCinema/Controllers/TicketRefundController.cs**
**Thay ??i chính:**
- Ph??ng th?c `RequestCancel` (POST) ???c s?a toàn b?:
  - ? Thêm tham s?: `bankCode` và `bankAccount` (thay vì ch? `bankAccount`)
  - ? Validate ngân hàng và STK
  - ? T?o yêu c?u h?y v?i status **"?ã H?y"** (h?y li?n)
  - ? **C?p nh?t t?t c? vé ? "?ã H?y"**
  - ? **C?p nh?t ??n ? "?ã H?y"**
  - ? **G?i email thông báo** cho khách hàng
  - ? L?u thông tin d??i d?ng `"BankCode:AccountNumber"`

- Thêm helper method: `GetBankName()`
  - Chuy?n ??i mã ngân hàng thành tên ??y ??

### 2. **WebCinema/Views/TicketRefund/Details.cshtml**

**Ph?n style (CSS):**
- Thêm style cho `.form-select` (dropdown ngân hàng)

**Ph?n form h?y vé:**
- ? **Thay th?**: Input STK ? **Dropdown ch?n Ngân hàng** + **Input STK**
- ? Dropdown ch?a 15+ ngân hàng ph? bi?n (VCB, BIDV, TCB, MB, ACB, SHB, v.v.)
- ? C? hai tr??ng là **b?t bu?c** (required)
- ? Thêm validation: STK ph?i 10-20 ch? s?

**Ph?n hi?n th? tr?ng thái h?y:**
- ? Parse thông tin ngân hàng t? format `"BankCode:AccountNumber"`
- ? Hi?n th? tên ngân hàng và STK che d?u

**JavaScript (submitCancelRequest):**
- ? Validate `bankCode` không tr?ng
- ? Validate `bankAccount` không tr?ng
- ? Validate `bankAccount` là 10-20 ch? s?
- ? Thêm confirmation dialog hi?n th? ngân hàng
- ? G?i c? 4 tham s?: `bookingId`, `bankCode`, `bankAccount`, `reason`

---

## ?? Email Template

Email thông báo h?y vé ch?a:

```html
H?y Vé Thành Công!

Xin chào [Tên Khách Hàng],

Vé c?a b?n ?ã ???c h?y thành công.

Chi ti?t hoàn ti?n:
- Mã ??n: #[BookingID]
- S? vé: [SoVe]
- T?ng ti?n g?c: [TongTien] ?
- S? ti?n hoàn l?i (70%): [TienHoan] ?
- Phí h?y (30%): [PhiHuy] ?
- Ngân hàng: [TenNganHang]
- S? tài kho?n: ****[4SoCuoi]

? B?n s? nh?n ti?n hoàn l?i trong 1-3 ngày làm vi?c.

C?m ?n b?n ?ã s? d?ng d?ch v? c?a chúng tôi!
```

---

## ?? Danh Sách Ngân Hàng

| Mã | Tên Ngân Hàng |
|----|---------------|
| VCB | Vietcombank |
| BIDV | BIDV |
| TCB | Techcombank |
| MB | MB Bank |
| ACB | ACB |
| SHB | SHB |
| STB | Sacombank |
| VIB | VIB |
| OCB | OCB |
| ABBANK | ABBank |
| HDB | HDBank |
| MSB | MSB |
| NAB | Nam A Bank |
| EIB | Eximbank |
| Agribank | Agribank |
| DongABank | Dong A Bank |

---

## ?? Thay ??i Database

### C? s? d? li?u: `Yeu_Cau_Huy_Ve`

**C?t `so_tai_khoan_atm`:**
- **C?**: Ch? ch?a s? tài kho?n (VD: `1234567890`)
- **M?i**: Ch?a c? ngân hàng và STK (VD: `VCB:1234567890`)

**D? li?u c? không c?n migrate** vì:
- Các yêu c?u h?y c? không quan tr?ng (ch? dùng cho l?ch s?)
- Các yêu c?u h?y m?i s? t? ??ng l?u ?úng format

---

## ?? Cách Ki?m Tra

### 1. **H?y Vé**
1. ??ng nh?p khách hàng
2. Vào **"Vé c?a tôi"** ? Ch?n vé c?n h?y
3. Scroll xu?ng form **"H?y Vé"**
4. Ch?n **Ngân hàng** t? dropdown
5. Nh?p **S? tài kho?n** (10-20 ch? s?)
6. (Tùy ch?n) Nh?p **Lý do h?y**
7. Click **"Xác Nh?n H?y Vé"**

### 2. **Ki?m Tra K?t Qu?**
- ? Vé hi?n th? status **"?ã H?y"**
- ? ??n ??t có status **"?ã H?y"**
- ? **Email g?i t?i email khách hàng**
- ? Thông tin hi?n th?: `"Ngân hàng: VCB"`, `"STK: ****7890"`

### 3. **Ki?m Tra Database**
```sql
SELECT * FROM Yeu_Cau_Huy_Ve 
WHERE dat_ve_id = [BookingID]

-- K?t qu? m?u:
-- so_tai_khoan_atm: "VCB:1234567890"
-- trang_thai: "?ã H?y"
-- ngay_duyet: [Th?i gian hi?n t?i]
```

---

## ? Danh Sách Ki?m Tra

- [x] ? H?y vé t? ??ng (không c?n duy?t)
- [x] ? G?i email thông báo cho khách hàng
- [x] ? Thêm dropdown ch?n ngân hàng (15+ ngân hàng)
- [x] ? Thêm input nh?p STK (10-20 ch? s?)
- [x] ? L?u d? li?u d??i d?ng `"BankCode:AccountNumber"`
- [x] ? Validation client-side
- [x] ? Validation server-side
- [x] ? Hi?n th? thông tin ngân hàng/STK che d?u
- [x] ? Compile thành công (No errors)

---

## ?? Ghi Chú

- **Không t?o file h??ng d?n** (theo yêu c?u)
- Email s? d?ng service `EmailService` hi?n có
- Thông tin ngân hàng l?u tr? an toàn (che d?u STK khi hi?n th?)
- T?t c? thay ??i backward-compatible v?i d? li?u c?

---

**Hoàn thành: Ticket Refund Auto-Cancel Implementation**
