# ?? MOBILE APP API GUIDE - 2 ROLES (Customer & Staff)

## ?? Flutter Mobile App - ??ng Nh?p 2 Role

?ng d?ng mobile c?a b?n có th? h? tr? **2 roles** v?i **2 API khác nhau**:

### **Architecture:**

```
???????????????????????????????????????????????????????????
?              MOBILE APP (Flutter)                        ?
???????????????????????????????????????????????????????????
? CUSTOMER ROLE      ? STAFF ROLE                         ?
???????????????????????????????????????????????????????????
? - View Movies      ? - View Dashboard                   ?
? - View Showtimes   ? - Offline Booking                  ?
? - Book Tickets     ? - Verify Tickets (QR)              ?
? - View Bookings    ? - View Bookings                    ?
? - View Foods       ? - Manage Bookings                  ?
? - View Cinemas     ?                                    ?
???????????????????????????????????????????????????????????
? API: /api/customer ? API: /api/staff                    ?
? Auth: Optional     ? Auth: REQUIRED (Staff role)        ?
???????????????????????????????????????????????????????????
```

---

## ?? Authentication Flow

### **Step 1: Login with Credentials**

```dart
// User selects role: Customer or Staff
String selectedRole = "Customer"; // or "Staff"

// Call auth endpoint
POST http://yourapi.com/api/auth/login
{
  "email": "user@email.com",
  "password": "password123",
  "role": "Customer"  // Important!
}

// Response (200 OK)
{
  "success": true,
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": 1,
    "email": "user@email.com",
    "role": "Customer"  // or "Staff"
  }
}
```

### **Step 2: Store Token in Secure Storage**

```dart
// Flutter - Using flutter_secure_storage
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

const storage = FlutterSecureStorage();

// Save token
await storage.write(
  key: 'jwt_token',
  value: response['token']
);

// Save role
await storage.write(
  key: 'user_role',
  value: response['user']['role']
);
```

### **Step 3: Use Token in API Requests**

```dart
// Retrieve token from secure storage
String? token = await storage.read(key: 'jwt_token');

// Use in HTTP headers
final headers = {
  'Authorization': 'Bearer $token',
  'Content-Type': 'application/json',
};

// Example: Get Customer bookings
final response = await http.get(
  Uri.parse('http://yourapi.com/api/customer/bookings/1'),
  headers: headers,
);
```

---

## ?? CUSTOMER ROLE - API Endpoints

### **1. Get Movies List (No Auth Required)**

```dart
GET /api/customer/movies?page=1&pageSize=10

Headers:
  Content-Type: application/json

Response (200 OK):
{
  "success": true,
  "message": "L?y danh sách phim thành công",
  "data": {
    "movies": [
      {
        "movie_id": 1,
        "title": "Avatar 3",
        "description": "Sci-fi movie",
        "duration": 192,
        "release_date": "2024-12-20T00:00:00",
        "image": "avatar3.jpg"
      }
    ],
    "total": 25,
    "current_page": 1,
    "total_pages": 3,
    "page_size": 10
  }
}
```

**Flutter Example:**
```dart
Future<List<Movie>> getMovies({int page = 1}) async {
  final response = await http.get(
    Uri.parse('$apiUrl/api/customer/movies?page=$page&pageSize=10'),
  );

  if (response.statusCode == 200) {
    final data = jsonDecode(response.body);
    return List<Movie>.from(
      data['data']['movies'].map((m) => Movie.fromJson(m))
    );
  }
  throw Exception('Failed to load movies');
}
```

---

### **2. Get Showtimes (No Auth Required)**

```dart
GET /api/customer/showtimes/5

Response (200 OK):
{
  "success": true,
  "message": "L?y danh sách su?t chi?u thành công",
  "data": [
    {
      "showtime_id": 101,
      "cinema": "CGV Hà N?i",
      "room": "Phòng 1",
      "date": "2024-12-20",
      "start_time": "14:00",
      "price": 150000,
      "total_seats": 100,
      "booked_seats": 45,
      "available_seats": 55
    }
  ]
}
```

---

### **3. Get Customer Bookings (Auth Required)**

```dart
GET /api/customer/bookings/1

Headers:
  Authorization: Bearer <jwt_token>
  Content-Type: application/json

Response (200 OK):
{
  "success": true,
  "message": "L?y l?ch s? ??t vé thành công",
  "data": [
    {
      "booking_id": 123,
      "created_at": "2024-12-15 10:30",
      "status": "?ã Thanh toán",
      "total_amount": 300000,
      "tickets_count": 2,
      "movie_title": "Avatar 3"
    }
  ]
}
```

**Errors:**
```
401 Unauthorized - No token provided
403 Forbidden - Token expired or invalid
404 Not Found - Customer not found
```

---

### **4. Get Foods (No Auth Required)**

```dart
GET /api/customer/foods

Response (200 OK):
{
  "success": true,
  "message": "L?y danh sách ?? ?n thành công",
  "data": [
    {
      "food_id": 1,
      "name": "B?ng ngô",
      "price": 50000,
      "description": "Popcorn"
    }
  ]
}
```

---

## ????? STAFF ROLE - API Endpoints

### **1. Get Dashboard (Auth Required - Staff Role)**

```dart
GET /api/staff/dashboard/1

Headers:
  Authorization: Bearer <staff_jwt_token>
  Content-Type: application/json

Response (200 OK):
{
  "success": true,
  "message": "L?y th?ng kê thành công",
  "data": {
    "total_tickets": 1245,
    "total_revenue": 186750000,
    "monthly_revenue": 45200000,
    "monthly_bookings": 320,
    "tickets_verified": 980,
    "tickets_pending": 265
  }
}
```

**Error:**
```
401 Unauthorized - No Staff token
403 Forbidden - Not a Staff role
```

---

### **2. Get Showtimes for Offline Booking (Auth Required)**

```dart
GET /api/staff/showtimes?date=2024-12-20

Headers:
  Authorization: Bearer <staff_jwt_token>

Response (200 OK):
{
  "success": true,
  "data": [
    {
      "showtime_id": 101,
      "movie_title": "Avatar 3",
      "cinema": "CGV Hà N?i",
      "room": "Phòng 1",
      "date": "2024-12-20",
      "start_time": "14:00",
      "price": 150000,
      "total_seats": 100,
      "booked_seats": 45,
      "available_seats": 55
    }
  ]
}
```

---

### **3. Create Offline Booking (Auth Required)**

```dart
POST /api/staff/create-booking

Headers:
  Authorization: Bearer <staff_jwt_token>
  Content-Type: application/json

Body:
{
  "showtime_id": 101,
  "seat_ids": [1, 2, 3],
  "customer_name": "Nguy?n V?n A",
  "customer_phone": "0912345678",
  "payment_method": "cash"
}

Response (200 OK):
{
  "success": true,
  "message": "T?o ??n ??t thành công",
  "data": {
    "booking_id": 456,
    "total_amount": 450000,
    "status": "?ã Thanh toán"
  }
}

Errors (400 Bad Request):
{
  "Message": "Showtime ID không h?p l?"
}
{
  "Message": "Tên khách hàng không ???c r?ng"
}
{
  "Message": "S? ?i?n tho?i không h?p l?"  // Must be >= 10 digits
}
{
  "Message": "Ph?i ch?n ít nh?t 1 gh?"
}
{
  "Message": "Không ???c ??t quá 100 gh?"
}
```

---

### **4. Verify Ticket (QR Scan) (Auth Required)**

```dart
POST /api/staff/verify-ticket

Headers:
  Authorization: Bearer <staff_jwt_token>
  Content-Type: application/json

Body:
{
  "qr_code": "QR123456789"
}

Response (200 OK):
{
  "success": true,
  "message": "Vé h?p l?",
  "data": {
    "movie_title": "Avatar 3",
    "customer_name": "Nguy?n V?n A",
    "seat_number": "A1",
    "date": "2024-12-20",
    "time": "14:00"
  }
}

Errors:
{
  "Message": "Mã QR không ???c r?ng"  // 400
}
{
  "success": false,
  "message": "Mã QR không h?p l?"  // 200 (but success=false)
}
{
  "success": false,
  "message": "Vé này ?ã ???c s? d?ng r?i"  // 200
}
```

---

## ?? Flutter Implementation Example

### **Auth Service:**

```dart
import 'package:http/http.dart' as http;
import 'dart:convert';

class AuthService {
  static const String baseUrl = 'http://yourapi.com';
  
  Future<AuthResponse> login(
    String email,
    String password,
    String role,
  ) async {
    final response = await http.post(
      Uri.parse('$baseUrl/api/auth/login'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({
        'email': email,
        'password': password,
        'role': role,
      }),
    );

    if (response.statusCode == 200) {
      return AuthResponse.fromJson(jsonDecode(response.body));
    } else {
      throw Exception('Login failed');
    }
  }
}
```

### **Customer API Service:**

```dart
class CustomerApiService {
  static const String baseUrl = 'http://yourapi.com';
  final String? token;

  CustomerApiService(this.token);

  Future<List<Movie>> getMovies({int page = 1}) async {
    final response = await http.get(
      Uri.parse('$baseUrl/api/customer/movies?page=$page'),
    );

    if (response.statusCode == 200) {
      final data = jsonDecode(response.body);
      return (data['data']['movies'] as List)
          .map((m) => Movie.fromJson(m))
          .toList();
    } else {
      throw Exception('Failed to load movies');
    }
  }

  Future<List<Booking>> getBookings(int customerId) async {
    final response = await http.get(
      Uri.parse('$baseUrl/api/customer/bookings/$customerId'),
      headers: {'Authorization': 'Bearer $token'},
    );

    if (response.statusCode == 200) {
      final data = jsonDecode(response.body);
      return (data['data'] as List)
          .map((b) => Booking.fromJson(b))
          .toList();
    } else if (response.statusCode == 401) {
      throw UnauthorizedException('Invalid or expired token');
    } else {
      throw Exception('Failed to load bookings');
    }
  }
}
```

### **Staff API Service:**

```dart
class StaffApiService {
  static const String baseUrl = 'http://yourapi.com';
  final String token;

  StaffApiService(this.token);

  Future<Dashboard> getDashboard(int staffId) async {
    final response = await http.get(
      Uri.parse('$baseUrl/api/staff/dashboard/$staffId'),
      headers: {'Authorization': 'Bearer $token'},
    );

    if (response.statusCode == 200) {
      final data = jsonDecode(response.body);
      return Dashboard.fromJson(data['data']);
    } else if (response.statusCode == 401) {
      throw UnauthorizedException('Invalid token or not Staff');
    } else {
      throw Exception('Failed to load dashboard');
    }
  }

  Future<BookingResponse> createOfflineBooking({
    required int showtimeId,
    required List<int> seatIds,
    required String customerName,
    required String customerPhone,
    required String paymentMethod,
  }) async {
    final response = await http.post(
      Uri.parse('$baseUrl/api/staff/create-booking'),
      headers: {
        'Authorization': 'Bearer $token',
        'Content-Type': 'application/json',
      },
      body: jsonEncode({
        'showtime_id': showtimeId,
        'seat_ids': seatIds,
        'customer_name': customerName,
        'customer_phone': customerPhone,
        'payment_method': paymentMethod,
      }),
    );

    if (response.statusCode == 200) {
      return BookingResponse.fromJson(jsonDecode(response.body));
    } else if (response.statusCode == 400) {
      throw ValidationException(response.body);
    } else {
      throw Exception('Failed to create booking');
    }
  }

  Future<VerifyResponse> verifyTicket(String qrCode) async {
    final response = await http.post(
      Uri.parse('$baseUrl/api/staff/verify-ticket'),
      headers: {
        'Authorization': 'Bearer $token',
        'Content-Type': 'application/json',
      },
      body: jsonEncode({'qr_code': qrCode}),
    );

    if (response.statusCode == 200) {
      return VerifyResponse.fromJson(jsonDecode(response.body));
    } else {
      throw Exception('Failed to verify ticket');
    }
  }
}
```

---

## ?? Role-Based Navigation

```dart
// In main.dart or auth provider
String? role = await storage.read(key: 'user_role');

if (role == 'Customer') {
  // Show Customer screens:
  // - MovieListScreen
  // - ShowtimesScreen
  // - BookingScreen
  // - BookingsHistoryScreen
  Navigator.pushReplacement(
    context,
    MaterialPageRoute(builder: (_) => CustomerHomeScreen()),
  );
} else if (role == 'Staff') {
  // Show Staff screens:
  // - DashboardScreen
  // - OfflineBookingScreen
  // - TicketVerificationScreen
  Navigator.pushReplacement(
    context,
    MaterialPageRoute(builder: (_) => StaffHomeScreen()),
  );
}
```

---

## ? Testing Checklist for Mobile App

- [ ] Customer login works
- [ ] Staff login works
- [ ] Customer can view movies (no auth)
- [ ] Customer can view showtimes (no auth)
- [ ] Customer can view bookings (with auth)
- [ ] Staff can view dashboard (with auth)
- [ ] Staff can create offline booking (with auth)
- [ ] Staff can verify tickets (with auth)
- [ ] Invalid token returns 401
- [ ] Wrong role returns 403
- [ ] Invalid input returns 400
- [ ] Not found returns 404
- [ ] Token refresh working
- [ ] Logout clears token

---

## ?? Key Points

1. **2 Separate Tokens:** Customer and Staff get different JWT tokens
2. **Role-Based Access:** APIs check for specific roles
3. **All Staff Endpoints Require Auth:** No public access to staff APIs
4. **Public Customer Endpoints:** Movies, showtimes, foods, cinemas don't need auth
5. **Error Handling:** Handle 401, 403, 400, 404, 500 appropriately
6. **Secure Storage:** Store tokens in encrypted secure storage, not SharedPreferences
7. **Token Refresh:** Implement token refresh mechanism
8. **Request Signing:** Optional but recommended for extra security

---

## ?? Deployment Checklist

- [ ] Both APIs deployed to production
- [ ] HTTPS enabled
- [ ] CORS configured for mobile domain
- [ ] Rate limiting enabled
- [ ] Logging enabled
- [ ] Monitoring enabled
- [ ] Backups configured
- [ ] API documentation updated

---

**Version:** 2.0  
**Status:** ? **PRODUCTION READY**  
**Last Updated:** 2024

---

Made with ?? for Mobile Developers
