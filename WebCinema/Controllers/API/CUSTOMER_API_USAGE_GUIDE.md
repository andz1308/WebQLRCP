# 🚀 CustomerApiController v2.0 - Hướng Dẫn Sử Dụng

## ✨ Cải Tiến Tóm Tắt

| Cải Tiến | Trước | Sau |
|---------|-------|-----|
| **Reflection** | ❌ Sử dụng (nguy hiểm) | ✅ Xóa |
| **Authorization** | ❌ Không có | ✅ `[Authorize]` trên endpoints nhạy cảm |
| **N+1 Query** | ❌ Có | ✅ Tối ưu |
| **Input Validation** | ❌ Không có | ✅ Kiểm tra page, IDs, ranges |
| **Tính Toán Ghế** | ❌ Sai | ✅ Chính xác (chỉ ghế trang_thai=2) |
| **Error Handling** | ❌ HTTP 200 mọi lúc | ✅ HTTP 400/404/500 đúng |
| **Data Filtering** | ❌ Bao gồm dữ liệu xóa | ✅ Chỉ dữ liệu hoạt động |

---

## 📡 Các Endpoints

### **1️⃣ Lấy Danh Sách Phim (Phân Trang)**

```http
GET /api/customer/movies?page=1&pageSize=10
Content-Type: application/json
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Lấy danh sách phim thành công",
  "data": {
    "movies": [
      {
        "movie_id": 1,
        "title": "Avengers: Endgame",
        "description": "Marvel superhero film",
        "duration": 181,
        "release_date": "2024-01-15T00:00:00",
        "image": "avengers.jpg"
      }
    ],
    "total": 25,
    "current_page": 1,
    "total_pages": 3,
    "page_size": 10
  }
}
```

**Validation:**
- ✅ `page` phải >= 1
- ✅ `pageSize` phải trong [1, 100], default = 10
- ✅ Tự động fix giá trị không hợp lệ

---

### **2️⃣ Lấy Suất Chiếu của Phim**

```http
GET /api/customer/showtimes/5
Content-Type: application/json
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Lấy danh sách suất chiếu thành công",
  "data": [
    {
      "showtime_id": 101,
      "cinema": "CGV Hà Nội",
      "room": "Phòng 1",
      "date": "2024-01-20",
      "start_time": "14:00",
      "price": 150000,
      "total_seats": 120,
      "booked_seats": 45,
      "available_seats": 75
    }
  ]
}
```

**Error (400 Bad Request):**
```json
{
  "Message": "Movie ID không hợp lệ"
}
```

**Validation:**
- ✅ `movieId` phải > 0
- ✅ Kiểm tra phim tồn tại
- ✅ Chỉ suất chiếu từ hôm nay trở đi

---

### **3️⃣ Lấy Lịch Sử Đặt Vé (Yêu Cầu Auth)**

```http
GET /api/customer/bookings/1
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Lấy lịch sử đặt vé thành công",
  "data": [
    {
      "booking_id": 123,
      "created_at": "2024-01-15 10:30",
      "status": "Đã Thanh toán",
      "total_amount": 300000,
      "tickets_count": 2,
      "movie_title": "Avatar 3"
    }
  ]
}
```

**Error (401 Unauthorized):**
```json
{
  "Message": "Authorization has been denied for this request."
}
```

**Validation:**
- ✅ `customerId` phải > 0
- ✅ **Yêu cầu Token xác thực**
- ✅ Kiểm tra khách hàng tồn tại

---

### **4️⃣ Lấy Chi Tiết Đơn Đặt Vé (Yêu Cầu Auth)**

```http
GET /api/customer/booking/123
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Lấy chi tiết đơn đặt thành công",
  "data": {
    "booking_id": 123,
    "customer_name": "Nguyễn Văn A",
    "customer_email": "nguyena@gmail.com",
    "customer_phone": "0912345678",
    "created_at": "2024-01-15 10:30",
    "status": "Đã Thanh toán",
    "total_amount": 300000,
    "payment_method": "N/A",
    "movie": {
      "movie_id": 5,
      "title": "Avatar 3"
    },
    "showtime": {
      "showtime_id": 101,
      "cinema": "CGV Hà Nội",
      "room": "Phòng 1",
      "date": "2024-01-20",
      "time": "14:00"
    },
    "tickets": [
      {
        "ticket_id": 1001,
        "seat_number": "A1",
        "qr_code": "QRCODE-123456",
        "price": 150000,
        "status": "Chưa sử dụng"
      },
      {
        "ticket_id": 1002,
        "seat_number": "A2",
        "qr_code": "QRCODE-123457",
        "price": 150000,
        "status": "Chưa sử dụng"
      }
    ],
    "food_items": [
      {
        "food_name": "Bỏng ngô",
        "quantity": 2,
        "price": 50000
      }
    ]
  }
}
```

**Error (404 Not Found):**
```json
{
  "Message": "Not Found"
}
```

**Validation:**
- ✅ `bookingId` phải > 0
- ✅ **Yêu cầu Token xác thực**
- ✅ Kiểm tra đơn đặt tồn tại

---

### **5️⃣ Lấy Danh Sách Đồ Ăn**

```http
GET /api/customer/foods
Content-Type: application/json
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Lấy danh sách đồ ăn thành công",
  "data": [
    {
      "food_id": 1,
      "name": "Bỏng ngô",
      "price": 50000,
      "description": "Bỏng ngô vị bơ tự chế"
    },
    {
      "food_id": 2,
      "name": "Nước ngọt",
      "price": 30000,
      "description": "Nước ngọt lạnh"
    }
  ]
}
```

**Validation:**
- ✅ Chỉ trả về đồ ăn có giá > 0
- ✅ Sort theo tên

---

### **6️⃣ Lấy Danh Sách Rạp Chiếu**

```http
GET /api/customer/cinemas
Content-Type: application/json
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Lấy danh sách rạp thành công",
  "data": [
    {
      "cinema_id": 1,
      "name": "CGV Hà Nội",
      "address": "Tầng 5, Tràng Tiền Plaza, Hà Nội"
    },
    {
      "cinema_id": 2,
      "name": "CGV TP. HCM",
      "address": "Quận 1, TP. HCM"
    }
  ]
}
```

---

## 🔒 Security

### **Authorization**

Các endpoint này **yêu cầu xác thực (Token)**:
- ✅ `GET /api/customer/bookings/{customerId}`
- ✅ `GET /api/customer/booking/{bookingId}`

**Cách Gửi Token:**
```http
Authorization: Bearer <your_jwt_token_here>
```

**Ví dụ trong Postman:**
1. Chọn tab **Authorization**
2. Chọn type **Bearer Token**
3. Paste JWT token vào field **Token**

**Ví dụ trong Flutter:**
```dart
final response = await http.get(
  Uri.parse('https://yourapi.com/api/customer/bookings/1'),
  headers: {
    'Authorization': 'Bearer $jwtToken',
  },
);
```

---

## 🧪 Testing với Postman

### **Import Collection:**

```json
{
  "info": {
    "name": "Customer API v2.0",
    "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  "item": [
    {
      "name": "Get Movies",
      "request": {
        "method": "GET",
        "url": "{{base_url}}/api/customer/movies?page=1&pageSize=10"
      }
    },
    {
      "name": "Get Showtimes",
      "request": {
        "method": "GET",
        "url": "{{base_url}}/api/customer/showtimes/5"
      }
    },
    {
      "name": "Get Bookings (Auth)",
      "request": {
        "method": "GET",
        "header": [
          {
            "key": "Authorization",
            "value": "Bearer {{token}}"
          }
        ],
        "url": "{{base_url}}/api/customer/bookings/1"
      }
    },
    {
      "name": "Get Booking Detail (Auth)",
      "request": {
        "method": "GET",
        "header": [
          {
            "key": "Authorization",
            "value": "Bearer {{token}}"
          }
        ],
        "url": "{{base_url}}/api/customer/booking/123"
      }
    },
    {
      "name": "Get Foods",
      "request": {
        "method": "GET",
        "url": "{{base_url}}/api/customer/foods"
      }
    },
    {
      "name": "Get Cinemas",
      "request": {
        "method": "GET",
        "url": "{{base_url}}/api/customer/cinemas"
      }
    }
  ]
}
```

---

## 📊 Performance

### **Query Optimization**

Tất cả endpoints đã được tối ưu:

| Endpoint | Queries | Trước → Sau |
|----------|---------|----------|
| GetMovies | 1 | 1 ✅ |
| GetShowtimes | 1 | 1 ✅ |
| GetBookings | n+1 | 1 ✅ (FirstOrDefault → Select) |
| GetBookingDetail | 2 | 2 ✅ |
| GetFoods | 1 | 1 ✅ |
| GetCinemas | 1 | 1 ✅ |

---

## ❌ Error Codes

| Status | Mô Tả | Ví Dụ |
|--------|-------|-------|
| **200 OK** | Thành công | Trả về dữ liệu |
| **400 Bad Request** | Input không hợp lệ | Page < 1, movieId <= 0 |
| **401 Unauthorized** | Không có token | Không gửi Authorization header |
| **404 Not Found** | Resource không tồn tại | Booking không tồn tại |
| **500 Internal Server Error** | Lỗi server | Database error |

---

## 🔮 Tương Lai

### **Cần Thêm:**
1. ✅ Row-Level Security (chỉ user xem dữ liệu của chính họ)
2. ✅ Rate Limiting (ngăn chặn spam)
3. ✅ CORS Policy (chỉ cho phép origin cụ thể)
4. ✅ Caching (tăng tốc độ)
5. ✅ Logging (audit trail)

---

## 📞 Support

Nếu gặp lỗi, hãy:
1. Kiểm tra format request
2. Kiểm tra Authorization header
3. Kiểm tra logs server
4. Liên hệ team development

---

**Version:** 2.0  
**Last Updated:** 2024  
**Status:** ✅ Production Ready
