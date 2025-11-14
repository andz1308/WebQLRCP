# 🎬 WebCinema API - Complete Guide

## ⚠️ IMPORTANT: WebAPI Installation Required

Trước khi sử dụng, bạn **PHẢI** cài đặt ASP.NET WebAPI packages.

### 📦 Cài đặt Packages (Package Manager Console)

```powershell
Install-Package Microsoft.AspNet.WebApi
Install-Package Microsoft.AspNet.WebApi.Core
Install-Package Microsoft.AspNet.WebApi.WebHost
Install-Package Microsoft.AspNet.Cors
```

### ✅ Sau khi cài, các file API sẽ compile được!

---

## 📌 Tóm Tắt

Bạn có 4 file hướng dẫn + 1 file template Flutter:

| File | Mục Đích |
|------|---------|
| **API_DOCUMENTATION.md** | 📖 Tài liệu API chi tiết (20+ endpoints) |
| **SETUP_GUIDE.md** | 🔧 Hướng dẫn cài đặt WebAPI |
| **FLUTTER_INTEGRATION_EXAMPLE.md** | 📱 Ví dụ Flutter app (pubspec.yaml, services, screens) |
| **README.md** | 📋 File này |

---

## 🚀 5 Bước Bắt Đầu

### Bước 1: Cài WebAPI Packages ⭐ MỘT LẦN DUY NHẤT
```powershell
# Mở Package Manager Console
# Tools > NuGet Package Manager > Package Manager Console

Install-Package Microsoft.AspNet.WebApi
Install-Package Microsoft.AspNet.WebApi.Core  
Install-Package Microsoft.AspNet.WebApi.WebHost
Install-Package Microsoft.AspNet.Cors
```

### Bước 2: Đọc SETUP_GUIDE.md
- Web.config configuration
- RouteConfig setup
- Global.asax updates
- WebApiConfig.cs creation

### Bước 3: Đọc API_DOCUMENTATION.md
- Hiểu 20+ endpoints
- Request/Response format
- Postman testing

### Bước 4: Setup Flutter Project
- Dùng FLUTTER_INTEGRATION_EXAMPLE.md
- Tạo services
- Build screens

### Bước 5: Test & Deploy
- Test API bằng Postman
- Connect Flutter app
- Test full flow

---

## 📁 Tài Liệu Có Sẵn

### 1️⃣ API_DOCUMENTATION.md (Đọc sau cài packages)
```
✅ 20+ API endpoints (Authentication, Movies, Cinemas, Bookings, Foods)
✅ Dart/Flutter code samples
✅ Complete booking flow example
✅ Error handling guide
✅ Postman setup
```

### 2️⃣ SETUP_GUIDE.md (Đọc trước tiên!)
```
✅ WebAPI configuration
✅ Web.config setup
✅ RouteConfig configuration
✅ Global.asax updates
✅ WebApiConfig.cs creation
✅ CORS configuration
✅ Postman testing guide
✅ Troubleshooting tips
```

### 3️⃣ FLUTTER_INTEGRATION_EXAMPLE.md (Xây app)
```
✅ pubspec.yaml (packages)
✅ Project structure
✅ API client implementation
✅ Auth service
✅ Movie service
✅ Cinema service
✅ Booking service
✅ Complete example screens
✅ Provider setup
```

---

## 🎯 API Endpoints (Sau khi cài packages)

### Authentication
```
POST   /api/auth/login                    → Đăng nhập
POST   /api/auth/register                 → Đăng ký
GET    /api/auth/profile/{id}             → Lấy hồ sơ
PUT    /api/auth/profile/{id}             → Cập nhật hồ sơ
```

### Movies
```
GET    /api/movies                        → Danh sách phim
GET    /api/movies/{id}                   → Chi tiết phim
GET    /api/movies/trending               → Phim xu hướng
GET    /api/movies/search?keyword=...     → Tìm kiếm phim
```

### Cinemas
```
GET    /api/cinemas                       → Danh sách rạp
GET    /api/cinemas/{id}                  → Chi tiết rạp
GET    /api/cinemas/{id}/showtimes        → Suất chiếu
GET    /api/cinemas/{id}/rooms/{rid}/seats → Ghế ngồi
```

### Bookings
```
POST   /api/bookings/create               → Tạo đơn đặt
GET    /api/bookings/{customerId}         → Lịch sử đặt
GET    /api/bookings/detail/{id}          → Chi tiết đơn
POST   /api/bookings/{id}/confirm-payment → Xác nhận thanh toán
```

### Foods
```
GET    /api/foods                         → Danh sách đồ ăn
GET    /api/foods/{id}                    → Chi tiết sản phẩm
GET    /api/foods/popular                 → Đồ ăn bán chạy
GET    /api/foods/search?keyword=...      → Tìm kiếm đồ ăn
```

---

## 📖 Chi Tiết Từng File

### API_DOCUMENTATION.md
📄 **~500 dòng, bao gồm:**
- ✅ Complete API reference
- ✅ 20+ endpoint examples
- ✅ Request/response in JSON format
- ✅ Dart/Flutter code samples
- ✅ Error handling examples
- ✅ Authentication flow
- ✅ Complete booking flow
- ✅ Mobile app checklist

👉 **Đọc để**: Hiểu cách sử dụng từng API

---

### SETUP_GUIDE.md
📄 **~400 dòng, bao gồm:**
- ✅ WebAPI configuration steps
- ✅ Web.config updates
- ✅ RouteConfig setup
- ✅ Global.asax changes
- ✅ WebApiConfig.cs creation
- ✅ CORS configuration
- ✅ Postman testing guide
- ✅ Troubleshooting section
- ✅ Deployment guide

👉 **Đọc để**: Cấu hình server đúng cách

---

### FLUTTER_INTEGRATION_EXAMPLE.md
📄 **~600 dòng, bao gồm:**
- ✅ pubspec.yaml dependencies
- ✅ Project structure
- ✅ api_config.dart
- ✅ response.dart model
- ✅ api_client.dart (Dio implementation)
- ✅ auth_service.dart (Login/Register)
- ✅ movie_service.dart (Movies)
- ✅ main.dart (App setup)
- ✅ Example screens (Login, Movies, Booking)
- ✅ Provider integration

👉 **Đọc để**: Xây dựng Flutter app

---

## 💻 Quick Installation Guide

### Step 1: Add WebAPI Packages
```powershell
# Package Manager Console
PM> Install-Package Microsoft.AspNet.WebApi
PM> Install-Package Microsoft.AspNet.WebApi.Core  
PM> Install-Package Microsoft.AspNet.WebApi.WebHost
PM> Install-Package Microsoft.AspNet.Cors

# Wait for completion...
```

### Step 2: Update Web.config
```xml
<system.webServer>
  <handlers>
    <remove name="ExtensionlessUrlHandler-Integrated-4.0" />
    <add name="ExtensionlessUrlHandler-Integrated-4.0" 
         path="*." verb="*" 
         type="System.Web.Handlers.TransferRequestHandler" />
  </handlers>
</system.webServer>
```

### Step 3: Create WebApiConfig.cs
Xem SETUP_GUIDE.md mục "Tạo File App_Start\WebApiConfig.cs"

### Step 4: Update Global.asax.cs
```csharp
protected void Application_Start()
{
    AreaRegistration.RegisterAllAreas();
    GlobalConfiguration.Configure(WebApiConfig.Register); // ← ADD THIS
    FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
    RouteConfig.RegisterRoutes(RouteTable.Routes);
}
```

### Step 5: Build
```
Build > Build Solution
```

✅ **Done! API ready to use**

---

## 🔍 Verify Installation

### Test Auth API
```
GET http://localhost:44300/api/auth/login
Content-Type: application/json

{
  "email": "test@example.com",
  "password": "password123"
}
```

### Test Movies API
```
GET http://localhost:44300/api/movies?page=1&pageSize=10
```

✅ If you get JSON response, API is working!

---

## 📱 Flutter App Setup

### 1. Create Flutter Project
```bash
flutter create cinema_app
cd cinema_app
```

### 2. Add Dependencies
Copy pubspec.yaml from FLUTTER_INTEGRATION_EXAMPLE.md

### 3. Get Packages
```bash
flutter pub get
```

### 4. Copy Code
- api_config.dart → lib/config/
- api_client.dart → lib/services/
- auth_service.dart → lib/services/
- main.dart → lib/
- Create screens/ folder with example screens

### 5. Update Base URL
```dart
// lib/config/api_config.dart
static const String DEV_BASE_URL = 'http://YOUR_SERVER_IP:44300/api';
```

### 6. Run
```bash
flutter run
```

---

## 🛠️ Troubleshooting

### ❌ "System.Web.Http" not found
→ Run: `Install-Package Microsoft.AspNet.WebApi`

### ❌ "ApiController not found"
→ Run: `Install-Package Microsoft.AspNet.WebApi.Core`

### ❌ API returns 404
→ Check SETUP_GUIDE.md "Troubleshooting" section

### ❌ CORS error in Flutter
→ Add CORS config to Web.config (see SETUP_GUIDE.md)

### ❌ Flutter can't reach API
→ Update base URL to your server IP (not localhost)

---

## 📚 Next Steps After Setup

1. ✅ Cài packages
2. ✅ Đọc SETUP_GUIDE.md  
3. ✅ Cấu hình Web.config
4. ✅ Tạo WebApiConfig.cs
5. ✅ Update Global.asax
6. ✅ Test API bằng Postman
7. ✅ Đọc API_DOCUMENTATION.md
8. ✅ Tạo Flutter project
9. ✅ Copy code từ FLUTTER_INTEGRATION_EXAMPLE.md
10. ✅ Connect Flutter → API
11. ✅ Test full flow
12. ✅ Deploy!

---

## 🎯 Key Points

| Key Point | Details |
|-----------|---------|
| **Packages** | PHẢI cài WebAPI trước! |
| **Configuration** | Follow SETUP_GUIDE.md chính xác |
| **API Format** | JSON response với success flag |
| **Authentication** | Login → Get customerId → Use for APIs |
| **CORS** | Enable cho mobile app |
| **Base URL** | Dùng IP server, không localhost |
| **Testing** | Postman trước Flutter |

---

## 🎉 Bạn đã sẵn sàng!

Bây giờ bạn có:
- ✅ 3 file hướng dẫn chi tiết
- ✅ API documentation
- ✅ Flutter example code
- ✅ Setup instructions

**Let's build! 🚀**

---

**Version**: 1.0.0
**Last Updated**: 2024-01-15  
**Status**: ✅ Ready for WebAPI Setup
