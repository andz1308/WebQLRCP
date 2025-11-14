# 📱 WebCinema API Documentation - Flutter/Dart Guide

## 🚀 Quick Start

### Base URL
```
http://localhost:44300/api/
```

### Response Format
Tất cả API trả về JSON format như sau:
```json
{
  "success": true,
  "message": "Success message",
  "data": { ... },
  "timestamp": "2024-01-15 14:30:45"
}
```

---

## 🔐 Authentication API

### 1. Login (Đăng nhập)
**POST** `/api/auth/login`

**Request:**
```dart
final response = await http.post(
  Uri.parse('http://localhost:44300/api/auth/login'),
  headers: {'Content-Type': 'application/json'},
  body: jsonEncode({
    'email': 'customer@example.com',
    'password': 'password123'
  }),
);

final data = jsonDecode(response.body);
if (data['success']) {
  int customerId = data['data']['customer_id'];
  String name = data['data']['name'];
}
```

**Response:**
```json
{
  "success": true,
  "message": "Đăng nhập thành công",
  "data": {
    "customer_id": 1,
    "name": "Nguyễn Văn A",
    "email": "customer@example.com",
    "phone": "0912345678",
    "created_at": "2024-01-01"
  }
}
```

---

### 2. Register (Đăng ký)
**POST** `/api/auth/register`

**Request:**
```dart
final response = await http.post(
  Uri.parse('http://localhost:44300/api/auth/register'),
  headers: {'Content-Type': 'application/json'},
  body: jsonEncode({
    'name': 'Nguyễn Văn B',
    'email': 'newuser@example.com',
    'password': 'password123',
    'phone': '0912345678'
  }),
);
```

**Response:**
```json
{
  "success": true,
  "message": "Đăng ký thành công",
  "data": {
    "customer_id": 2
  }
}
```

---

### 3. Get Profile (Lấy thông tin cá nhân)
**GET** `/api/auth/profile/{customerId}`

**Example:**
```dart
final response = await http.get(
  Uri.parse('http://localhost:44300/api/auth/profile/1'),
);
```

**Response:**
```json
{
  "success": true,
  "message": "Lấy thông tin thành công",
  "data": {
    "customer_id": 1,
    "name": "Nguyễn Văn A",
    "email": "customer@example.com",
    "phone": "0912345678"
  }
}
```

---

### 4. Update Profile (Cập nhật thông tin)
**PUT** `/api/auth/profile/{customerId}`

**Request:**
```dart
final response = await http.put(
  Uri.parse('http://localhost:44300/api/auth/profile/1'),
  headers: {'Content-Type': 'application/json'},
  body: jsonEncode({
    'name': 'Nguyễn Văn A Updated',
    'phone': '0987654321'
  }),
);
```

---

## 🎬 Movies API

### 1. Get Movies (Danh sách phim)
**GET** `/api/movies?page=1&pageSize=10&search=keyword`

**Example:**
```dart
final response = await http.get(
  Uri.parse('http://localhost:44300/api/movies?page=1&pageSize=10'),
);

final data = jsonDecode(response.body);
final movies = data['data']['movies'] as List;
```

**Response:**
```json
{
  "success": true,
  "message": "Lấy danh sách phim thành công",
  "data": {
    "movies": [
      {
        "movie_id": 1,
        "title": "The Matrix",
        "description": "...",
        "duration": 136,
        "release_date": "2024-01-15",
        "image": "https://...",
        "trailer_url": "https://..."
      }
    ],
    "total": 50,
    "current_page": 1,
    "total_pages": 5,
    "page_size": 10
  }
}
```

---

### 2. Get Movie Detail (Chi tiết phim)
**GET** `/api/movies/{movieId}`

**Example:**
```dart
final response = await http.get(
  Uri.parse('http://localhost:44300/api/movies/1'),
);
```

**Response:**
```json
{
  "success": true,
  "message": "Lấy chi tiết phim thành công",
  "data": {
    "movie_id": 1,
    "title": "The Matrix",
    "description": "...",
    "duration": 136,
    "release_date": "2024-01-15",
    "image": "https://...",
    "trailer_url": "https://...",
    "director": {
      "director_id": 1,
      "name": "Lana Wachowski"
    },
    "cast": [
      {
        "actor_id": 1,
        "name": "Keanu Reeves",
        "role": "Neo"
      }
    ],
    "producer": {
      "producer_id": 1,
      "name": "Warner Bros"
    }
  }
}
```

---

### 3. Get Trending Movies (Phim xu hướng)
**GET** `/api/movies/trending`

**Response:**
```json
{
  "success": true,
  "message": "Lấy phim xu hướng thành công",
  "data": [
    {
      "movie_id": 1,
      "title": "The Matrix",
      "image": "https://...",
      "booking_count": 150
    }
  ]
}
```

---

### 4. Search Movies (Tìm kiếm phim)
**GET** `/api/movies/search?keyword=matrix`

---

## 🏢 Cinemas API

### 1. Get Cinemas (Danh sách rạp)
**GET** `/api/cinemas`

**Response:**
```json
{
  "success": true,
  "message": "Lấy danh sách rạp thành công",
  "data": [
    {
      "cinema_id": 1,
      "name": "CGV Hà Nội",
      "address": "Tòa nhà Hà Nội Tower",
      "phone": "0243 3333 333",
      "rooms_count": 10
    }
  ]
}
```

---

### 2. Get Cinema Detail (Chi tiết rạp)
**GET** `/api/cinemas/{cinemaId}`

**Response:**
```json
{
  "success": true,
  "message": "Lấy chi tiết rạp thành công",
  "data": {
    "cinema_id": 1,
    "name": "CGV Hà Nội",
    "address": "Tòa nhà Hà Nội Tower",
    "phone": "0243 3333 333",
    "rooms": [
      {
        "room_id": 1,
        "name": "Phòng 1",
        "rows": 10,
        "columns": 16,
        "total_seats": 160
      }
    ]
  }
}
```

---

### 3. Get Showtimes (Danh sách suất chiếu)
**GET** `/api/cinemas/{cinemaId}/showtimes?movieId=1&date=2024-01-15`

**Response:**
```json
{
  "success": true,
  "message": "Lấy danh sách suất chiếu thành công",
  "data": [
    {
      "showtime_id": 1,
      "movie_id": 1,
      "movie_title": "The Matrix",
      "room_id": 1,
      "room_name": "Phòng 1",
      "date": "2024-01-15",
      "start_time": "19:00",
      "end_time": "20:50",
      "language": "Tiếng Anh - Phụ đề",
      "price": 100000,
      "available_seats": 150
    }
  ]
}
```

---

### 4. Get Room Seats (Danh sách ghế)
**GET** `/api/cinemas/{cinemaId}/rooms/{roomId}/seats?showtimeId=1`

**Response:**
```json
{
  "success": true,
  "message": "Lấy danh sách ghế thành công",
  "data": {
    "room_id": 1,
    "rows": 10,
    "columns": 16,
    "seats": [
      {
        "seat_id": 1,
        "seat_number": "A1",
        "row": "A",
        "column": 1,
        "type": "Standard",
        "status": "available",
        "price": 100000
      }
    ]
  }
}
```

---

## 🎟️ Bookings API

### 1. Create Booking (Tạo đơn đặt)
**POST** `/api/bookings/create`

**Request:**
```dart
final response = await http.post(
  Uri.parse('http://localhost:44300/api/bookings/create'),
  headers: {'Content-Type': 'application/json'},
  body: jsonEncode({
    'customer_id': 1,
    'showtime_id': 1,
    'seat_ids': [1, 2, 3],
    'food_items': [
      {
        'food_id': 1,
        'quantity': 2
      }
    ]
  }),
);
```

**Response:**
```json
{
  "success": true,
  "message": "Tạo đơn đặt thành công",
  "data": {
    "booking_id": 1,
    "total_amount": 350000,
    "status": "Chưa thanh toán"
  }
}
```

---

### 2. Get Customer Bookings (Lịch sử đặt vé)
**GET** `/api/bookings/{customerId}`

**Response:**
```json
{
  "success": true,
  "message": "Lấy lịch sử đặt vé thành công",
  "data": [
    {
      "booking_id": 1,
      "created_at": "2024-01-15 14:30:00",
      "status": "Chờ Duyệt",
      "total_amount": 350000,
      "tickets_count": 3,
      "movie_title": "The Matrix"
    }
  ]
}
```

---

### 3. Get Booking Detail (Chi tiết đơn đặt)
**GET** `/api/bookings/detail/{bookingId}`

**Response:**
```json
{
  "success": true,
  "message": "Lấy chi tiết đơn đặt thành công",
  "data": {
    "booking_id": 1,
    "customer_name": "Nguyễn Văn A",
    "customer_email": "customer@example.com",
    "customer_phone": "0912345678",
    "created_at": "2024-01-15 14:30:00",
    "status": "Chờ Duyệt",
    "total_amount": 350000,
    "movie": {
      "movie_id": 1,
      "title": "The Matrix"
    },
    "showtime": {
      "showtime_id": 1,
      "cinema": "CGV Hà Nội",
      "room": "Phòng 1",
      "date": "2024-01-15",
      "time": "19:00"
    },
    "tickets": [
      {
        "ticket_id": 1,
        "seat_number": "A1",
        "qr_code": "uuid-string-here",
        "price": 100000,
        "status": "Chưa sử dụng"
      }
    ],
    "food_items": [
      {
        "food_name": "Bỏng Ngô",
        "quantity": 2,
        "price": 50000
      }
    ]
  }
}
```

---

### 4. Confirm Payment (Xác nhận thanh toán QR)
**POST** `/api/bookings/{bookingId}/confirm-payment`

**Response:**
```json
{
  "success": true,
  "message": "Xác nhận thanh toán thành công, chờ Admin duyệt"
}
```

---

## 🍿 Foods API

### 1. Get Foods (Danh sách đồ ăn)
**GET** `/api/foods?page=1&pageSize=20`

**Response:**
```json
{
  "success": true,
  "message": "Lấy danh sách đồ ăn thành công",
  "data": {
    "foods": [
      {
        "food_id": 1,
        "name": "Bỏng Ngô",
        "description": "Bỏng ngô nóng thơm ngon",
        "price": 50000,
        "image": "https://...",
        "category": "Snack"
      }
    ],
    "total": 100,
    "current_page": 1,
    "total_pages": 5,
    "page_size": 20
  }
}
```

---

### 2. Get Food Detail (Chi tiết sản phẩm)
**GET** `/api/foods/{foodId}`

---

### 3. Get Popular Foods (Đồ ăn bán chạy)
**GET** `/api/foods/popular`

---

### 4. Search Foods (Tìm kiếm đồ ăn)
**GET** `/api/foods/search?keyword=popcorn`

---

## 📦 HTTP Headers (Luôn thêm vào request)
```dart
const headers = {
  'Content-Type': 'application/json',
  'Accept': 'application/json',
};
```

---

## ⚠️ Error Handling

```dart
try {
  final response = await http.get(Uri.parse(url));
  
  if (response.statusCode == 200) {
    final data = jsonDecode(response.body);
    if (data['success']) {
      // Xử lý dữ liệu
    } else {
      // Lỗi từ API
      print('Error: ${data['message']}');
    }
  } else {
    // Lỗi HTTP
    print('HTTP Error: ${response.statusCode}');
  }
} catch (e) {
  // Lỗi kết nối
  print('Connection Error: $e');
}
```

---

## 🔌 Dart/Flutter HTTP Package

```yaml
# pubspec.yaml
dependencies:
  http: ^0.13.5
  dio: ^5.0.0
```

---

## 📝 Example: Complete Booking Flow

```dart
import 'package:http/http.dart' as http;
import 'dart:convert';

class CinemaAPI {
  final String baseUrl = 'http://localhost:44300/api';
  
  // 1. Login
  Future<int> login(String email, String password) async {
    final response = await http.post(
      Uri.parse('$baseUrl/auth/login'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({'email': email, 'password': password}),
    );
    
    final data = jsonDecode(response.body);
    if (data['success']) {
      return data['data']['customer_id'];
    }
    throw Exception(data['message']);
  }

  // 2. Get Movies
  Future<List> getMovies() async {
    final response = await http.get(
      Uri.parse('$baseUrl/movies?page=1&pageSize=10'),
    );
    
    final data = jsonDecode(response.body);
    return data['data']['movies'];
  }

  // 3. Get Showtimes
  Future<List> getShowtimes(int cinemaId, int movieId) async {
    final response = await http.get(
      Uri.parse('$baseUrl/cinemas/$cinemaId/showtimes?movieId=$movieId'),
    );
    
    final data = jsonDecode(response.body);
    return data['data'];
  }

  // 4. Get Seats
  Future<Map> getSeats(int cinemaId, int roomId, int showtimeId) async {
    final response = await http.get(
      Uri.parse('$baseUrl/cinemas/$cinemaId/rooms/$roomId/seats?showtimeId=$showtimeId'),
    );
    
    final data = jsonDecode(response.body);
    return data['data'];
  }

  // 5. Create Booking
  Future<int> createBooking(
    int customerId,
    int showtimeId,
    List<int> seatIds,
  ) async {
    final response = await http.post(
      Uri.parse('$baseUrl/bookings/create'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({
        'customer_id': customerId,
        'showtime_id': showtimeId,
        'seat_ids': seatIds,
      }),
    );
    
    final data = jsonDecode(response.body);
    if (data['success']) {
      return data['data']['booking_id'];
    }
    throw Exception(data['message']);
  }

  // 6. Confirm Payment
  Future<void> confirmPayment(int bookingId) async {
    final response = await http.post(
      Uri.parse('$baseUrl/bookings/$bookingId/confirm-payment'),
    );
    
    final data = jsonDecode(response.body);
    if (!data['success']) {
      throw Exception(data['message']);
    }
  }
}
```

---

## 🎯 Sử Dụng Constant URLs

```dart
class ApiConstants {
  static const String BASE_URL = 'http://localhost:44300/api';
  
  // Auth endpoints
  static const String LOGIN = '$BASE_URL/auth/login';
  static const String REGISTER = '$BASE_URL/auth/register';
  static const String PROFILE = '$BASE_URL/auth/profile';
  
  // Movies endpoints
  static const String MOVIES = '$BASE_URL/movies';
  static const String TRENDING_MOVIES = '$BASE_URL/movies/trending';
  
  // Cinemas endpoints
  static const String CINEMAS = '$BASE_URL/cinemas';
  
  // Foods endpoints
  static const String FOODS = '$BASE_URL/foods';
  static const String POPULAR_FOODS = '$BASE_URL/foods/popular';
  
  // Bookings endpoints
  static const String BOOKINGS = '$BASE_URL/bookings';
}
```

---

## 🚀 Ready to use!

Bạn đã có đủ API để xây dựng ứng dụng Flutter đầy đủ. Chúc bạn code vui vẻ! 🎉
