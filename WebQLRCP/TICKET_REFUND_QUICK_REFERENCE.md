# ??? H??NG D?N NHANH - H? Th?ng H?y Vé T? ??ng

## ? Các Tính N?ng M?i

### 1?? H?y Vé Li?n (Không C?n Duy?t)
- Khách hàng h?y vé ? **H?y ngay l?p t?c**
- Không còn tình tr?ng "Ch? duy?t"
- Tr?ng thái: "Ch? duy?t" ? **"?ã H?y"**

### 2?? Email Thông Báo
- **T? ??ng g?i email** cho khách hàng
- Ch?a chi ti?t hoàn ti?n & STK
- Format: `VCB:1234567890` ? Hi?n th?: `"VCB"` + `"****7890"`

### 3?? Ch?n Ngân Hàng
- Dropdown v?i 15+ ngân hàng Vi?t Nam
- B?t bu?c ch?n tr??c khi h?y

### 4?? Nh?p STK
- Input: 10-20 ch? s?
- B?t bu?c ?i?n
- T? ??ng validate

---

## ?? Quy Trình H?y Vé (M?i)

```
Khách hàng click "H?y Vé"
          ?
[Form] Ch?n Ngân hàng + Nh?p STK
          ?
Xác nh?n ? Ki?m tra ?i?u ki?n
          ?
? H?y li?n ? C?p nh?t DB ? G?i Email
          ?
Hi?n th?: "H?y thành công!"
```

---

## ?? Ki?m Tra Nhanh

### ? H?y Vé Thành Công
```
? Vé status ? "?ã H?y"
? ??n status ? "?ã H?y"
? Email g?i t?i khách hàng
? DB: so_tai_khoan_atm = "VCB:1234567890"
```

### ? H?y Vé Không ???c
```
? Vé ?ã qua h?n
? Vé ?ã s? d?ng
? Vé ?ã h?y
? Không ch?n ngân hàng
? STK không h?p l?
```

---

## ?? Database

**C?t: `Yeu_Cau_Huy_Ve.so_tai_khoan_atm`**
- Format m?i: `"NganhangCode:SoTaiKhoan"`
- VD: `"VCB:1234567890"`

---

## ?? Ngân Hàng H? Tr?

| Code | Tên | Code | Tên |
|------|-----|------|-----|
| VCB | Vietcombank | HDB | HDBank |
| BIDV | BIDV | MSB | MSB |
| TCB | Techcombank | NAB | Nam A Bank |
| MB | MB Bank | EIB | Eximbank |
| ACB | ACB | Agribank | Agribank |
| SHB | SHB | DongABank | Dong A Bank |
| STB | Sacombank |
| VIB | VIB |
| OCB | OCB |
| ABBANK | ABBank |

---

## ?? Email G?i Cho Khách Hàng

**Ch? ??:** H?y Vé Thành Công

**N?i dung:**
```
H?y Vé Thành Công!
- Mã ??n: #123
- Ti?n g?c: 500.000 ?
- Hoàn l?i: 350.000 ? (70%)
- Phí: 150.000 ? (30%)
- Ngân hàng: Vietcombank (VCB)
- STK: ****7890
- Th?i gian: 1-3 ngày
```

---

## ?? Các File Thay ??i

```
?? WebCinema/
  ??? Controllers/
  ?   ??? ? TicketRefundController.cs (S?a RequestCancel)
  ??? Views/
      ??? TicketRefund/
          ??? ? Details.cshtml (Thêm form Ngân hàng + STK)
```

---

## ? Tính N?ng N?i B?t

? **T? ??ng h?y** - Không c?n admin duy?t  
? **Email thông báo** - Khách hàng bi?t ngay  
? **Ch?n ngân hàng** - D? s? d?ng, 15+ ngân hàng  
? **Validation ch?t** - ??m b?o d? li?u ?úng  
? **B?o m?t STK** - Che d?u khi hi?n th?  
? **Compile thành công** - Không l?i  

---

**Status: ? HOÀN THÀNH**
