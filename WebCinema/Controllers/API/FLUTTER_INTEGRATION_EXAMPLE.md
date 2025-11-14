# 📱 Flutter App Integration Example

## 📦 pubspec.yaml

```yaml
name: cinema_app
description: Cinema booking app

publish_to: 'none'

version: 1.0.0+1

environment:
  sdk: ">=2.18.0 <3.0.0"

dependencies:
  flutter:
    sdk: flutter
  
  # HTTP
  http: ^0.13.5
  dio: ^5.0.0
  
  # State Management
  provider: ^6.0.0
  riverpod: ^2.0.0
  
  # Storage
  shared_preferences: ^2.0.15
  hive: ^2.2.3
  
  # UI
  intl: ^0.18.1
  
  dev_dependencies:
  flutter_test:
    sdk: flutter
  flutter_linter: ^2.0.1

flutter:
  uses-material-design: true
```

---

## 🏗️ Project Structure

```
lib/
├── main.dart
├── config/
│   └── api_config.dart
├── models/
│   ├── movie.dart
│   ├── cinema.dart
│   ├── booking.dart
│   ├── user.dart
│   └── response.dart
├── services/
│   ├── auth_service.dart
│   ├── movie_service.dart
│   ├── cinema_service.dart
│   ├── booking_service.dart
│   └── api_client.dart
├── screens/
│   ├── auth/
│   │   ├── login_screen.dart
│   │   └── register_screen.dart
│   ├── home/
│   │   ├── home_screen.dart
│   │   └── movie_detail_screen.dart
│   ├── booking/
│   │   ├── select_cinema_screen.dart
│   │   ├── select_showtime_screen.dart
│   │   ├── select_seats_screen.dart
│   │   ├── select_food_screen.dart
│   │   └── payment_screen.dart
│   └── profile/
│       └── profile_screen.dart
├── widgets/
│   └── common_widgets.dart
└── utils/
    ├── constants.dart
    └── extensions.dart
```

---

## 📁 lib/config/api_config.dart

```dart
class ApiConfig {
  // Development
  static const String DEV_BASE_URL = 'http://localhost:44300/api';
  
  // Production
  static const String PROD_BASE_URL = 'https://yourserver.com/api';
  
  // Current environment
  static const bool isProduction = false;
  static String get baseUrl => isProduction ? PROD_BASE_URL : DEV_BASE_URL;
  
  // Endpoints
  static const String authLogin = '/auth/login';
  static const String authRegister = '/auth/register';
  static const String authProfile = '/auth/profile';
  
  static const String movies = '/movies';
  static const String trendingMovies = '/movies/trending';
  static const String movieSearch = '/movies/search';
  
  static const String cinemas = '/cinemas';
  static const String cinemaDetail = '/cinemas';
  static const String showtimes = '/cinemas';
  static const String roomSeats = '/cinemas';
  
  static const String foods = '/foods';
  static const String popularFoods = '/foods/popular';
  
  static const String bookings = '/bookings';
  static const String createBooking = '/bookings/create';
  static const String confirmPayment = '/bookings';
}
```

---

## 📁 lib/models/response.dart

```dart
import 'package:json_annotation/json_annotation.dart';

part 'response.g.dart';

class ApiResponse<T> {
  final bool success;
  final String message;
  final T? data;
  final String? timestamp;

  ApiResponse({
    required this.success,
    required this.message,
    this.data,
    this.timestamp,
  });

  factory ApiResponse.fromJson(
    Map<String, dynamic> json,
    T Function(Object?)? fromJsonT,
  ) {
    return ApiResponse(
      success: json['success'] ?? false,
      message: json['message'] ?? '',
      data: fromJsonT != null ? fromJsonT(json['data']) : null,
      timestamp: json['timestamp'],
    );
  }
}
```

---

## 📁 lib/services/api_client.dart

```dart
import 'package:dio/dio.dart';
import '../config/api_config.dart';

class ApiClient {
  late Dio _dio;
  
  ApiClient() {
    _dio = Dio(
      BaseOptions(
        baseUrl: ApiConfig.baseUrl,
        connectTimeout: const Duration(seconds: 30),
        receiveTimeout: const Duration(seconds: 30),
        contentType: 'application/json',
        headers: {
          'Content-Type': 'application/json',
          'Accept': 'application/json',
        },
      ),
    );

    // Add interceptors
    _dio.interceptors.add(
      InterceptorsWrapper(
        onRequest: (options, handler) {
          print('📤 Request: ${options.method} ${options.path}');
          return handler.next(options);
        },
        onResponse: (response, handler) {
          print('📥 Response: ${response.statusCode}');
          return handler.next(response);
        },
        onError: (error, handler) {
          print('❌ Error: ${error.message}');
          return handler.next(error);
        },
      ),
    );
  }

  Future<T> get<T>(
    String endpoint, {
    Map<String, dynamic>? queryParameters,
  }) async {
    try {
      final response = await _dio.get(
        endpoint,
        queryParameters: queryParameters,
      );
      return response.data;
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  Future<T> post<T>(
    String endpoint, {
    Map<String, dynamic>? data,
  }) async {
    try {
      final response = await _dio.post(
        endpoint,
        data: data,
      );
      return response.data;
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  Future<T> put<T>(
    String endpoint, {
    Map<String, dynamic>? data,
  }) async {
    try {
      final response = await _dio.put(
        endpoint,
        data: data,
      );
      return response.data;
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  Exception _handleError(DioException error) {
    switch (error.type) {
      case DioExceptionType.connectionTimeout:
        return Exception('Connection timeout');
      case DioExceptionType.receiveTimeout:
        return Exception('Receive timeout');
      case DioExceptionType.badResponse:
        final statusCode = error.response?.statusCode;
        final message = error.response?.data['message'] ?? 'Unknown error';
        return Exception('Error $statusCode: $message');
      default:
        return Exception('${error.message}');
    }
  }
}
```

---

## 📁 lib/services/auth_service.dart

```dart
import 'package:shared_preferences/shared_preferences.dart';
import 'api_client.dart';
import '../config/api_config.dart';
import '../models/user.dart';

class AuthService {
  final ApiClient _apiClient;
  late SharedPreferences _prefs;
  
  int? _cachedCustomerId;
  
  AuthService(this._apiClient);
  
  Future<void> init() async {
    _prefs = await SharedPreferences.getInstance();
  }

  Future<User> login(String email, String password) async {
    try {
      final response = await _apiClient.post(
        ApiConfig.authLogin,
        data: {
          'email': email,
          'password': password,
        },
      );

      if (response['success']) {
        final userData = response['data'];
        _cachedCustomerId = userData['customer_id'];
        
        // Save to local storage
        await _prefs.setInt('customer_id', _cachedCustomerId!);
        await _prefs.setString('customer_name', userData['name']);
        await _prefs.setString('customer_email', userData['email']);

        return User.fromJson(userData);
      } else {
        throw Exception(response['message']);
      }
    } catch (e) {
      throw Exception('Login failed: $e');
    }
  }

  Future<User> register(String name, String email, String password, String phone) async {
    try {
      final response = await _apiClient.post(
        ApiConfig.authRegister,
        data: {
          'name': name,
          'email': email,
          'password': password,
          'phone': phone,
        },
      );

      if (response['success']) {
        return User(
          customerId: response['data']['customer_id'],
          name: name,
          email: email,
        );
      } else {
        throw Exception(response['message']);
      }
    } catch (e) {
      throw Exception('Registration failed: $e');
    }
  }

  Future<User?> getProfile(int customerId) async {
    try {
      final response = await _apiClient.get(
        '${ApiConfig.authProfile}/$customerId',
      );

      if (response['success']) {
        return User.fromJson(response['data']);
      }
      return null;
    } catch (e) {
      throw Exception('Get profile failed: $e');
    }
  }

  bool isLoggedIn() {
    return _prefs.getInt('customer_id') != null;
  }

  int? getCustomerId() {
    return _cachedCustomerId ?? _prefs.getInt('customer_id');
  }

  Future<void> logout() async {
    _cachedCustomerId = null;
    await _prefs.remove('customer_id');
    await _prefs.remove('customer_name');
    await _prefs.remove('customer_email');
  }
}
```

---

## 📁 lib/services/movie_service.dart

```dart
import 'api_client.dart';
import '../config/api_config.dart';
import '../models/movie.dart';

class MovieService {
  final ApiClient _apiClient;

  MovieService(this._apiClient);

  Future<List<Movie>> getMovies({int page = 1, int pageSize = 10}) async {
    try {
      final response = await _apiClient.get(
        ApiConfig.movies,
        queryParameters: {
          'page': page,
          'pageSize': pageSize,
        },
      );

      if (response['success']) {
        final movies = (response['data']['movies'] as List)
            .map((m) => Movie.fromJson(m))
            .toList();
        return movies;
      } else {
        throw Exception(response['message']);
      }
    } catch (e) {
      throw Exception('Get movies failed: $e');
    }
  }

  Future<Movie?> getMovieDetail(int movieId) async {
    try {
      final response = await _apiClient.get(
        '${ApiConfig.movies}/$movieId',
      );

      if (response['success']) {
        return Movie.fromJson(response['data']);
      }
      return null;
    } catch (e) {
      throw Exception('Get movie detail failed: $e');
    }
  }

  Future<List<Movie>> getTrendingMovies() async {
    try {
      final response = await _apiClient.get(ApiConfig.trendingMovies);

      if (response['success']) {
        final movies = (response['data'] as List)
            .map((m) => Movie.fromJson(m))
            .toList();
        return movies;
      } else {
        throw Exception(response['message']);
      }
    } catch (e) {
      throw Exception('Get trending movies failed: $e');
    }
  }

  Future<List<Movie>> searchMovies(String keyword) async {
    try {
      final response = await _apiClient.get(
        ApiConfig.movieSearch,
        queryParameters: {'keyword': keyword},
      );

      if (response['success']) {
        final movies = (response['data'] as List)
            .map((m) => Movie.fromJson(m))
            .toList();
        return movies;
      }
      return [];
    } catch (e) {
      throw Exception('Search movies failed: $e');
    }
  }
}
```

---

## 📁 lib/main.dart

```dart
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'services/api_client.dart';
import 'services/auth_service.dart';
import 'services/movie_service.dart';
import 'screens/home/home_screen.dart';
import 'screens/auth/login_screen.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();
  
  final apiClient = ApiClient();
  final authService = AuthService(apiClient);
  await authService.init();
  
  runApp(MyApp(authService: authService, apiClient: apiClient));
}

class MyApp extends StatelessWidget {
  final AuthService authService;
  final ApiClient apiClient;

  const MyApp({
    required this.authService,
    required this.apiClient,
  });

  @override
  Widget build(BuildContext context) {
    return MultiProvider(
      providers: [
        Provider.value(value: authService),
        Provider.value(value: apiClient),
        ProxyProvider<ApiClient, MovieService>(
          update: (_, apiClient, __) => MovieService(apiClient),
        ),
      ],
      child: MaterialApp(
        title: 'Cinema App',
        theme: ThemeData(
          primarySwatch: Colors.blue,
          useMaterial3: true,
        ),
        home: authService.isLoggedIn() ? const HomeScreen() : const LoginScreen(),
      ),
    );
  }
}
```

---

## ✨ Sử Dụng trong Screen

```dart
// Example: LoginScreen
class LoginScreen extends StatefulWidget {
  const LoginScreen({Key? key}) : super(key: key);

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();
  bool _isLoading = false;

  void _handleLogin(BuildContext context) async {
    setState(() => _isLoading = true);
    
    try {
      final authService = context.read<AuthService>();
      await authService.login(
        _emailController.text,
        _passwordController.text,
      );

      if (mounted) {
        Navigator.of(context).pushReplacementNamed('/home');
      }
    } catch (e) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Login failed: $e')),
      );
    } finally {
      setState(() => _isLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Login')),
      body: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          children: [
            TextField(
              controller: _emailController,
              decoration: const InputDecoration(labelText: 'Email'),
            ),
            const SizedBox(height: 16),
            TextField(
              controller: _passwordController,
              decoration: const InputDecoration(labelText: 'Password'),
              obscureText: true,
            ),
            const SizedBox(height: 32),
            ElevatedButton(
              onPressed: _isLoading ? null : () => _handleLogin(context),
              child: _isLoading
                  ? const CircularProgressIndicator()
                  : const Text('Login'),
            ),
          ],
        ),
      ),
    );
  }
}
```

---

## 🎉 Bắt đầu!

Bạn đã có tất cả những gì cần để xây dựng ứng dụng Flutter! 🚀
