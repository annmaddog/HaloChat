# HaloChat Backend

Backend ASP.NET Core Web API (net9.0) cho HaloChat, dùng MongoDB Atlas làm
cơ sở dữ liệu và JWT cho xác thực.

## Yêu cầu

- .NET 9 SDK

## Thiết lập cấu hình bí mật

Các giá trị bí mật (chuỗi kết nối MongoDB, khóa ký JWT) **không** được commit
vào repo. Chúng được lưu bằng `dotnet user-secrets` cho dự án
`HaloChat.Api`. Tham khảo file mẫu
`HaloChat.Api/appsettings.Development.json.example` để biết cần điền gì,
rồi thiết lập bằng hai lệnh sau (thay giá trị thật của bạn vào):

```
dotnet user-secrets set "MongoDb:ChuoiKetNoi" "<chuoi-ket-noi-mongodb-atlas-that>" --project HaloChat.Api
dotnet user-secrets set "Jwt:ChuoiBiMat" "<chuoi-bi-mat-jwt-that-toi-thieu-32-ky-tu>" --project HaloChat.Api
```

## Chạy ứng dụng

`dotnet user-secrets` chỉ được nạp khi môi trường là `Development`, nên cần
đặt biến môi trường `ASPNETCORE_ENVIRONMENT=Development` khi chạy (đây là một
vướng mắc thực tế đã gặp phải khi kiểm thử thủ công trước đây):

```
ASPNETCORE_ENVIRONMENT=Development dotnet run --project HaloChat.Api
```

Trên PowerShell:

```
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project HaloChat.Api
```

## Chạy kiểm thử

```
dotnet test HaloChat.Api.Tests
```

Bộ kiểm thử không cần kết nối MongoDB Atlas thật (kiểm thử tích hợp dùng
kho dữ liệu giả lập trong bộ nhớ thay cho MongoDB thật).

## Swagger

Khi chạy ở môi trường `Development`, giao diện Swagger UI có tại `/swagger`.
