# HaloChat

Ứng dụng chat an toàn dùng mô hình mã hóa lai RSA-AES (đồ án). Xem thiết kế đầy đủ tại
`docs/superpowers/specs/2026-09-10-halochat-rsa-aes-design.md`.

## Chạy backend

```bash
dotnet user-secrets set "MongoDb:ChuoiKetNoi" "<chuoi-ket-noi-mongodb-that-cua-ban>" --project backend/HaloChat.Api
dotnet user-secrets set "Jwt:ChuoiBiMat" "<chuoi-ngau-nhien-toi-thieu-32-ky-tu>" --project backend/HaloChat.Api
ASPNETCORE_ENVIRONMENT=Development dotnet run --project backend/HaloChat.Api --launch-profile http
```

Xem `backend/HaloChat.Api/appsettings.Development.json.example` để biết đúng định dạng 2 giá trị trên.
API chạy tại `http://localhost:5231`, Swagger UI tại `http://localhost:5231/swagger`.

Chạy test: `dotnet test backend/HaloChat.sln`

## Chạy frontend

```bash
npm install --prefix frontend
npm run dev --prefix frontend
```

Frontend chạy tại `http://localhost:5173`, gọi thẳng vào backend ở `http://localhost:5231`.

Chạy test: `npm run test --prefix frontend`

## Thương hiệu

Tên sản phẩm: **HaloChat**. Logo tại `assets/halochat-logo.png`. Màu chủ đạo xanh dương gradient
(`#2F7BF6` → `#1A56C4`), font "Be Vietnam Pro".

## Trạng thái các giai đoạn

- GĐ3 (Backend nền tảng — đăng ký/đăng nhập/JWT/danh sách người dùng): hoàn thành.
- GĐ4 (Frontend nền tảng — trang đăng ký/đăng nhập/danh sách người dùng, giao diện HaloChat): hoàn thành.
- GĐ5a (Chat realtime lõi — SignalR ChatHub, nhắn tin 1-1, gửi ảnh/file, lịch sử phân trang):
  hoàn thành. Nhắn tin hiện mở tự do giữa mọi người dùng đã đăng nhập (chưa có kết bạn — xem spec §10.1).
- GĐ5b (Kết bạn + nhóm chat + thông báo realtime), GĐ6 (Bảo mật AES/RSA — nhóm tự viết), GĐ7
  (Quên mật khẩu): chưa bắt đầu.

### Xác minh GĐ5a thủ công

1. Chạy backend + frontend (xem hướng dẫn phía trên).
2. Mở 2 trình duyệt (hoặc 1 cửa sổ ẩn danh + 1 bình thường), đăng ký 2 tài khoản khác nhau, đăng nhập cả hai.
3. Ở tài khoản A, chọn tài khoản B trong danh sách, gửi 1 tin nhắn văn bản — xác nhận B nhận được ngay (không cần F5).
4. Gửi 1 ảnh (< 5MB) và 1 file `.pdf`/`.docx` (< 20MB) — xác nhận hiển thị ảnh/link file ở cả 2 phía.
5. Mở MongoDB Atlas, kiểm tra collection `TinNhan` có đủ các bản ghi vừa gửi (kể cả `DuongDanFile`/`TenFileGoc` cho ảnh/file).
6. Thử gửi file `.exe` — xác nhận bị từ chối với thông báo lỗi.
