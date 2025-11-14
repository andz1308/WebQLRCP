# 🔧 WebAPI Configuration Guide

## 📋 File Cần Cập Nhật trong Web.config

### Thêm WebAPI routing

**File:** `WebCinema\Web.config`

Kiểm tra xem đã có dòng này chưa, nếu chưa thêm vào trong `<configuration>`:

```xml
<configuration>
  <system.web>
    <compilation debug="true" targetFramework="4.7.2" />
    <httpRuntime targetFramework="4.7.2" />
  </system.web>
  
  <system.webServer>
    <handlers>
      <remove name="ExtensionlessUrlHandler-Integrated-4.0" />
      <remove name="OPTIONSVerbHandler" />
      <remove name="TRACEVerbHandler" />
      <add name="ExtensionlessUrlHandler-Integrated-4.0" path="*." verb="*" type="System.Web.Handlers.TransferRequestHandler" preCondition="runtimeVersionv4.0,bitness32" />
    </handlers>
  </system.webServer>
</configuration>
```

---

## 🛣️ Cấu Hình Routes - App_Start\RouteConfig.cs

Thêm WebAPI routes vào file này:

```csharp
public class RouteConfig
{
    public static void RegisterRoutes(RouteCollection routes)
    {
        routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

        // WebAPI Routes
        routes.MapHttpRoute(
            name: "DefaultApi",
            routeTemplate: "api/{controller}/{action}/{id}",
            defaults: new { id = RouteParameter.Optional }
        );

        // MVC Routes
        routes.MapRoute(
            name: "Default",
            url: "{controller}/{action}/{id}",
            defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
        );
    }
}
```

---

## 📦 Cập Nhật Global.asax

**File:** `WebCinema\Global.asax.cs`

Thêm `WebApiConfig.Register()` vào phương thức `Application_Start()`:

```csharp
protected void Application_Start()
{
    AreaRegistration.RegisterAllAreas();
    GlobalConfiguration.Configure(WebApiConfig.Register);  // ← Thêm dòng này
    FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
    RouteConfig.RegisterRoutes(RouteTable.Routes);
    BundleConfig.RegisterBundles(BundleTable.Bundles);
    
    // Khởi động dịch vụ
    BookingExpirationService.StartExpirationCheck();
}
```

---

## 📄 Tạo File App_Start\WebApiConfig.cs

**File:** `WebCinema\App_Start\WebApiConfig.cs`

```csharp
using System.Web.Http;

namespace WebCinema
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // WebAPI configuration
            config.MapHttpAttributeRoutes();

            // Default route
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            // Enable CORS for mobile app
            var cors = new System.Web.Http.Cors.EnableCorsAttribute(
                origins: "*",
                headers: "*",
                methods: "*"
            );
            config.EnableCors(cors);

            // JSON format
            config.Formatters.JsonFormatter.SupportedMediaTypes.Add(
                new System.Net.Http.Headers.MediaTypeHeaderValue("text/html")
            );
        }
    }
}
```

---

## 📦 Cài đặt NuGet Packages cần thiết

Chạy lệnh này trong Package Manager Console:

```powershell
Install-Package Microsoft.AspNet.WebApi
Install-Package Microsoft.AspNet.WebApi.Core
Install-Package Microsoft.AspNet.WebApi.WebHost
Install-Package Microsoft.AspNet.Cors
```

---

## ✅ Kiểm tra API

Sau khi cấu hình, hãy test API:

### 1. Test Auth Login
```
GET http://localhost:44300/api/auth/login
Content-Type: application/json

{
  "email": "test@example.com",
  "password": "password123"
}
```

### 2. Test Get Movies
```
GET http://localhost:44300/api/movies?page=1&pageSize=10
```

### 3. Test Get Cinemas
```
GET http://localhost:44300/api/cinemas
```

---

## 🔐 CORS Configuration cho Mobile App

Nếu gặp lỗi CORS khi gọi từ Flutter, thêm vào `Web.config`:

```xml
<system.webServer>
  <httpProtocol>
    <customHeaders>
      <add name="Access-Control-Allow-Origin" value="*" />
      <add name="Access-Control-Allow-Methods" value="GET, POST, PUT, DELETE, OPTIONS" />
      <add name="Access-Control-Allow-Headers" value="Content-Type, Authorization" />
    </customHeaders>
  </httpProtocol>
</system.webServer>
```

---

## 🚀 Deploy

### Khi Deploy trên Production:

1. Thay đổi Base URL trong Flutter:
```dart
final String baseUrl = 'https://yourdomainhere.com/api';
```

2. Đảm bảo HTTPS được bật

3. Cấu hình CORS cho domain của bạn:
```csharp
var cors = new EnableCorsAttribute(
    origins: "https://yourfrontend.com",
    headers: "*",
    methods: "*"
);
```

---

## 📝 Testing với Postman

### 1. Import Collection
```json
{
  "info": {
    "name": "WebCinema API",
    "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  "item": [
    {
      "name": "Auth",
      "item": [
        {
          "name": "Login",
          "request": {
            "method": "POST",
            "url": {
              "raw": "http://localhost:44300/api/auth/login",
              "protocol": "http",
              "host": ["localhost"],
              "port": "44300",
              "path": ["api", "auth", "login"]
            },
            "body": {
              "mode": "raw",
              "raw": "{\"email\":\"test@example.com\",\"password\":\"password123\"}"
            }
          }
        }
      ]
    }
  ]
}
```

---

## ⚡ Troubleshooting

### Lỗi 404 - API không tìm thấy
- Kiểm tra RouteConfig có đúng không
- Đảm bảo controller tên kết thúc bằng "Controller" (e.g., `AuthApiController`)

### Lỗi 405 - Method Not Allowed
- Kiểm tra HTTP method (GET, POST, PUT, DELETE)
- Kiểm tra Route attribute

### Lỗi CORS
- Thêm cấu hình CORS trong WebApiConfig
- Kiểm tra Access-Control-Allow-Origin header

### Lỗi 500 - Internal Server Error
- Kiểm tra logs: `Infrastructure\LoggingHelper.cs`
- Xem chi tiết lỗi trong Visual Studio Debug

---

## ✨ Best Practices

1. **Always validate input** - Kiểm tra dữ liệu đầu vào
2. **Use HttpStatusCode** - Trả về mã HTTP phù hợp
3. **Consistent response format** - Luôn trả về cùng format
4. **Rate limiting** - Thêm giới hạn yêu cầu
5. **Authentication** - Thêm JWT token nếu cần
6. **Logging** - Log tất cả hoạt động quan trọng

---

## 🎉 Ready!

API của bạn đã sẵn sàng để phục vụ ứng dụng Flutter! 
