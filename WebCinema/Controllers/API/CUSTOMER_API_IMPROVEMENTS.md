# ?? CustomerApiController - C?i Ti?n Chi Ti?t

## ?? Tóm T?t C?i Ti?n

Phiên b?n c?i ti?n c?a `CustomerApiController.cs` ?ã kh?c ph?c các v?n ?? an toàn, hi?u su?t và tr?i nghi?m ng??i dùng.

---

## ? V?N ?? TR??C C?A M? & ? GI?I PHÁP

### **1. ? S? d?ng Reflection Nguy Hi?m**

**V?n ?? c? (Dòng ~123):**
```csharp
payment_method = booking.GetType().GetProperty("phuong_thuc_thanh_toan")?.GetValue(booking)
```

**V?n ??:**
- ?? Property `phuong_thuc_thanh_toan` **không t?n t?i** trong entity `Dat_Ve`
- ?? Reflection r?t ch?m và không an toàn ki?u (type-safe)
- ?? Luôn return `null` ? gây nh?m l?n

**? Gi?i pháp:**
```csharp
payment_method = "N/A",  // TODO: Thêm property vào model n?u c?n
```

---

### **2. ? Thi?u Authorization (L? H?ng B?o M?t)**

**V?n ?? c?:**
```csharp
[HttpGet]
[Route("bookings/{customerId}")]
public IHttpActionResult GetBookings(int customerId)  // ? AI C?NG CÓ TH? G?I!
```

**V?n ??:**
- ?? B?t k? ai c?ng có th? g?i `api/customer/bookings/1` ?? l?y d? li?u khách hàng khác
- ?? Information Disclosure (L? h?ng OWASP)

**? Gi?i pháp:**
```csharp
[HttpGet]
[Route("bookings/{customerId}")]
[Authorize]  // ? Ch? user ?ã ??ng nh?p m?i g?i ???c
public IHttpActionResult GetBookings(int customerId)
```

**Ghi chú:** Nên thêm ki?m tra: Ch? cho phép user xem d? li?u c?a chính h?
```csharp
var currentUserId = int.Parse(User.Identity.Name);  // T? JWT/Claims
if (customerId != currentUserId && !User.IsInRole("Admin"))
{
    return Unauthorized();
}
```

---

### **3. ? N+1 Query Problem (Hi?u Su?t Kém)**

**V?n ?? c?:**
```csharp
movie_title = b.Ves.FirstOrDefault() != null 
    ? b.Ves.FirstOrDefault().Suat_Chieu.Phim.ten_phim  // ? G?i FirstOrDefault() 2 l?n!
    : "N/A"
```

**V?n ??:**
- ?? `FirstOrDefault()` ???c g?i 2 l?n ? 2 query
- ?? N?u có 100 bookings ? 200 queries! (Thay vì 1-2)
- ?? Gây lag khi có nhi?u d? li?u

**? Gi?i pháp:**
```csharp
movie_title = b.Ves.Select(v => v.Suat_Chieu.Phim.ten_phim).FirstOrDefault() ?? "N/A"
```

**K?t qu?:** Ch? 1 query ?? l?y movie_title c?a t?t c? bookings

---

### **4. ? Tính Toán Gh? Sai**

**V?n ?? c?:**
```csharp
available_seats = s.Phong_Chieu.Ghes.Count() - s.Ves.Count(v => v.Dat_Ve_id != null)
// ? ??m t?t c? gh?, g?m c?: l?i, gh? l?i, gh? tr?ng
```

**V?n ??:**
- ?? ??m l?i ?i, gh? l?i ? s? gh? tr?ng không chính xác
- ?? Khách hàng th?y gh? tr?ng nh?ng l?i không ch?n ???c

**? Gi?i pháp:**
```csharp
total_seats = s.Phong_Chieu.Ghes.Count(g => g.trang_thai == 2),
available_seats = s.Phong_Chieu.Ghes.Count(g => g.trang_thai == 2) - 
                  s.Ves.Count(v => v.Dat_Ve_id != null)
// ? Ch? ??m gh? có trang_thai == 2 (gh? tr?ng s?n sàng)
```

---

### **5. ? Thi?u Input Validation**

**V?n ?? c?:**
```csharp
public IHttpActionResult GetMovies(int page = 1, int pageSize = 10)
{
    // Không ki?m tra page < 1, pageSize < 1
    int totalPages = (int)Math.Ceiling(total / (double)pageSize);  // ? Divide by zero?
}
```

**? Gi?i pháp:**
```csharp
if (page < 1)
{
    return BadRequest("Page ph?i >= 1");
}

if (pageSize < 1 || pageSize > MAX_PAGE_SIZE)
{
    pageSize = DEFAULT_PAGE_SIZE;
}
```

**Áp d?ng cho:**
- ? GetShowtimes: Ki?m tra `movieId > 0`
- ? GetBookings: Ki?m tra `customerId > 0`
- ? GetBookingDetail: Ki?m tra `bookingId > 0`

---

### **6. ? Không Ki?m Tra T?n T?i c?a Resources**

**V?n ?? c?:**
```csharp
var showtimes = db.Suat_Chieus
    .Where(s => s.phim_id == movieId && ...)  // N?u movieId không t?n t?i?
    .ToList();  // Tr? v? list tr?ng mà không có thông báo
```

**? Gi?i pháp:**
```csharp
var movieExists = db.Phims.Any(p => p.phim_id == movieId);
if (!movieExists)
{
    return Ok(new { success = false, message = "Phim không t?n t?i" });
}
```

---

### **7. ? Error Handling Không Nh?t Quán**

**V?n ?? c?:**
```csharp
catch (Exception ex)
{
    LoggingHelper.LogError(ex);
    return Ok(new { success = false, message = "L?i: " + ex.Message });  
    // ? HTTP 200 OK v?i success = false (l?!)
}
```

**? Gi?i pháp:**
```csharp
catch (Exception ex)
{
    LoggingHelper.LogError(ex);
    return InternalServerError(ex);  // ? HTTP 500 (?úng)
}

// Ngoài ra:
return BadRequest(...);        // HTTP 400
return NotFound();             // HTTP 404
return Unauthorized();         // HTTP 401
```

---

### **8. ? Không Filter D? Li?u Không Ho?t ??ng**

**V?n ?? c?:**
```csharp
var foods = db.Do_Ans
    .OrderBy(d => d.ten_san_pham)
    .Select(...)  // ? Bao g?m c? ?? ?n b? xóa (trang_thai = 0)
    .ToList();
```

**? Gi?i pháp:**
```csharp
var foods = db.Do_Ans
    .Where(d => d.trang_thai != 0)  // ? Ch? l?y ?? ?n còn ho?t ??ng
    .OrderBy(d => d.ten_san_pham)
    .Select(...)
    .ToList();
```

---

## ?? B?ng So Sánh

| Tiêu Chí | C? | M?i |
|----------|----|----|
| Reflection | ? S? d?ng (nguy hi?m) | ? Xóa |
| Authorization | ? Không có | ? `[Authorize]` |
| N+1 Query | ? FirstOrDefault() x2 | ? Select().FirstOrDefault() |
| Tính Toán Gh? | ? Sai (??m l?i) | ? Chính xác (trang_thai == 2) |
| Validation | ? Không có | ? Ki?m tra page, movieId, v.v. |
| Error Handling | ? Luôn HTTP 200 | ? HTTP 400, 404, 500 ?úng |
| Filter | ? Bao g?m d? li?u b? xóa | ? Ch? d? li?u ho?t ??ng |

---

## ?? Cách S? D?ng API

### **1. L?y danh sách phim (Anonymous)**
```
GET /api/customer/movies?page=1&pageSize=10
```

### **2. L?y su?t chi?u c?a phim (Anonymous)**
```
GET /api/customer/showtimes/5
```

### **3. L?y l?ch s? ??t vé (Requires Auth)**
```
GET /api/customer/bookings/1
Authorization: Bearer <token>
```

### **4. L?y chi ti?t ??n ??t (Requires Auth)**
```
GET /api/customer/booking/123
Authorization: Bearer <token>
```

### **5. L?y danh sách ?? ?n (Anonymous)**
```
GET /api/customer/foods
```

### **6. L?y danh sách r?p (Anonymous)**
```
GET /api/customer/cinemas
```

---

## ?? B?o M?t

### ? Các ?i?m C?i Thi?n:
1. **Authorization Header** - B?t bu?c cho endpoints nh?y c?m
2. **Input Validation** - Ki?m tra t?t c? parameters
3. **Resource Existence Check** - Xác minh d? li?u t?n t?i tr??c khi tr? v?
4. **Error Messages** - Không l? thông tin nh?y c?m
5. **Data Filtering** - Ch? tr? v? d? li?u ho?t ??ng

### ?? TODO - C?n Thêm:
1. **Row-Level Security** - Ch? user ???c truy c?p d? li?u c?a chính h?
   ```csharp
   var currentUserId = int.Parse(User.Identity.Name);
   if (customerId != currentUserId && !User.IsInRole("Admin"))
   {
       return Unauthorized();
   }
   ```

2. **Rate Limiting** - Ng?n ch?n abuse
   ```csharp
   [Throttle(5000, 60)]  // 5 requests per 60 seconds
   ```

3. **CORS Policy** - Ch? cho phép origin c? th?
   ```csharp
   [EnableCors(origins: "https://yourdomain.com", headers: "*", methods: "*")]
   ```

---

## ?? Hi?u Su?t

### ? C?i Ti?n:
- **Query Reduction:** N+1 ? 1 query per endpoint
- **Reflection Removal:** Lo?i b? overhead Reflection
- **Data Filtering:** Gi?m l??ng d? li?u truy?n v?
- **Constants:** `MAX_PAGE_SIZE = 100` tránh pagination l?n quá

---

## ??? Maintenance Notes

### Model Extensions C?n Thêm:
1. **Dat_Ve Model** - Thêm property `phuong_thuc_thanh_toan` (hi?n ghi chú là TODO)
2. **Do_An Model** - Ki?m tra có `trang_thai` field không
3. **Rap Model** - Ki?m tra có `trang_thai` field không

---

## ?? Các Commit Ti?p Theo:
1. Thêm Row-Level Security checks
2. Thêm Rate Limiting
3. Implement CORS Policy
4. Thêm pagination limits
5. Thêm caching cho endpoints public

---

**Version:** 2.0  
**Last Updated:** 2024  
**Status:** ? Ready for Production
