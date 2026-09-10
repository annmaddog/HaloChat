# HaloChat — Ứng dụng chat an toàn dùng mô hình mã hóa lai RSA-AES

Ngày viết: 2026-09-10
Nguồn yêu cầu: `Xây dựng ứng dụng chat an toàn sử dụng mô hình mã hóa lai RSA.docx`
Trạng thái: đã được duyệt trong phiên brainstorming, sẵn sàng chuyển sang lập kế hoạch triển khai.

## 1. Bối cảnh

Dự án cũ trong repo (`HaloChat.Web` — Razor Pages + SQL Server + mã hóa AES đơn) đã bị xóa khỏi
thư mục làm việc và không còn khớp với tài liệu yêu cầu mới. Repo sẽ được xây lại từ đầu theo đúng
tài liệu: React + TypeScript / ASP.NET Core Web API / MongoDB / SignalR / mô hình mã hóa lai
RSA-AES (RSA-OAEP mã hóa khóa phiên AES-256, AES-256-GCM mã hóa nội dung).

Đây là dự án môn học: phần **viết mã hóa thật (AES-256-GCM, RSA-OAEP áp dụng vào luồng chat)** sẽ
do nhóm tự thực hiện ở giai đoạn sau. Vai trò của bản triển khai này là dựng toàn bộ phần còn lại
(nền tảng, xác thực, danh sách người dùng, chat realtime, gửi ảnh/file, lưu lịch sử) chạy được đầy
đủ với nội dung **plaintext**, và để lại đúng chỗ, đúng chú thích cho phần mã hóa được cắm vào sau
mà không cần đổi kiến trúc.

## 2. Kiến trúc tổng thể

```
React + TypeScript (Vite)  ──HTTP + SignalR──>  ASP.NET Core Web API  ──>  MongoDB
Login/Register/DanhSach/Chat                     Auth(JWT)/NguoiDung/TinNhan/Hub        Users/Messages
AES-256-GCM / RSA-OAEP (GĐ6, do nhóm viết)        DichVuMaHoa (stub, GĐ6)
```

Mô hình mã hóa mục tiêu (áp dụng ở GĐ6, chưa bật trong các giai đoạn đầu):

```
Người gửi: Sinh AES-256 Session Key → AES-256-GCM mã hóa tin nhắn (Ciphertext + Nonce + AuthTag)
           → RSA-OAEP mã hóa Session Key bằng Public Key người nhận → gửi Server
Server:    lưu SenderId, ReceiverId, Ciphertext, EncryptedSessionKey, Nonce, AuthTag, CreatedAt
           (không cần đọc được plaintext)
Người nhận: RSA-OAEP giải mã Session Key bằng Private Key → AES-256-GCM giải mã → tin nhắn gốc
```

## 3. Cấu trúc repo & solution

```
HaloChat.sln
src/
  HaloChat.Api/            # ASP.NET Core Web API + SignalR Hub, JWT, MongoDB driver
  HaloChat.Security/       # Class library: DichVuMaHoa (stub AES/RSA cho GĐ6), băm mật khẩu (thật)
halochat-web/               # React + TypeScript (Vite) — không đưa vào .sln
docs/superpowers/specs|plans/...
```

Tách `HaloChat.Security` riêng khỏi `HaloChat.Api` để: (a) dễ unit test độc lập, (b) đánh dấu rõ
ràng đây là ranh giới nơi nhóm sẽ viết mã hóa thật, không lẫn vào logic API.

## 4. Mô hình dữ liệu (MongoDB)

**Collection `NguoiDung`**

| Field | Kiểu | Ghi chú |
|---|---|---|
| `_id` | ObjectId | |
| `TenTaiKhoan` | string | unique index, dùng để tìm kiếm/đăng nhập |
| `Email` | string | unique index, dùng xác thực + khôi phục mật khẩu |
| `MatKhauBam` | string | SHA-256(Password + Salt) — thật, code từ đầu |
| `Salt` | string | ngẫu nhiên theo user, thật, code từ đầu |
| `KhoaCongKhai` | string | RSA Public Key (PEM/Base64) |
| `KhoaBiMat` | string | RSA Private Key — sinh cùng lúc đăng ký (xem lưu ý §9) |
| `NgayTao` | DateTime | |

Trường phục vụ quên mật khẩu (`MaOtp`, `ThoiHanOtp`, ...) được thêm khi làm module GĐ7, không
thêm trước để tránh field thừa không dùng.

**Collection `TinNhan`**

| Field | Kiểu | Ghi chú |
|---|---|---|
| `_id` | ObjectId | |
| `NguoiGuiId` | ObjectId | |
| `NguoiNhanId` | ObjectId | |
| `LoaiTinNhan` | enum: `Text` \| `Anh` \| `File` | |
| `NoiDungTinNhan` | string | plaintext ở GĐ3-5; ở GĐ6 nhóm chuyển sang lưu ciphertext |
| `CiphertextTinNhan`, `KhoaPhienDaMaHoa`, `Nonce`, `AuthTag` | string, để trống ở GĐ3-5 | dự phòng cho GĐ6, không dùng tới trước đó |
| `DuongDanFile` | string? | chỉ có khi `LoaiTinNhan != Text` |
| `TenFileGoc` | string? | tên file gốc người dùng upload |
| `KichThuocFile` | long? | bytes |
| `LoaiFile` | string? | MIME type |
| `ThoiGianTao` | DateTime | |

## 5. Trình tự triển khai theo giai đoạn

Bám sát đúng thứ tự giai đoạn trong tài liệu; mỗi giai đoạn có mốc kiểm tra rõ ràng trước khi
sang giai đoạn kế:

- **GĐ1-2 (Phân tích/Thiết kế)** — tài liệu này.
- **Scaffold** — `HaloChat.Api` chạy `dotnet run` (Swagger mở được), `halochat-web` chạy
  `npm run dev` (trang trắng load được), kết nối MongoDB thành công. Xác nhận cả hai chạy trước
  khi làm tính năng.
- **GĐ3 (Backend nền tảng)** — MongoDB models, `DangKyTaiKhoan()`, `DangNhap()` (băm mật khẩu thật,
  JWT thật), quản lý người dùng cơ bản (danh sách người dùng).
- **GĐ4 (Frontend)** — trang đăng ký/đăng nhập, danh sách người dùng, khung giao diện chat
  (React + TypeScript, Vite).
- **GĐ5 (Chat realtime + ảnh/file)** — SignalR `GuiTinNhan()`/`NhanTinNhan()`, lưu lịch sử vào
  `TinNhan` dạng **plaintext**; upload ảnh/file qua endpoint REST riêng (không qua SignalR), lưu
  đĩa + metadata Mongo (chi tiết §9-10). **Mốc kiểm tra:** gửi thử tin nhắn + ảnh/file, mở MongoDB
  xác nhận dữ liệu được lưu thật.
- **GĐ6 (Bảo mật — nhóm tự viết)** — `DichVuMaHoa.MaHoaTinNhan()/GiaiMaTinNhan()` (AES-256-GCM) và
  mã hóa khóa phiên (RSA-OAEP) hiện chỉ là stub có chú thích `[BẢO MẬT - GĐ6]`, **chưa được gọi**
  trong luồng gửi/nhận. Khi nhóm viết xong, chỉ cần nối lệnh gọi vào Hub/Controller để chuyển từ
  lưu plaintext sang lưu ciphertext — không cần đổi schema hay kiến trúc.
- **GĐ7 (Quên mật khẩu)** — module riêng, làm sau khi chat chính hoàn thiện: Email + OTP.
- **GĐ8 (Kiểm thử)** — test API bằng Swagger, test đăng ký/đăng nhập, gửi/nhận tin nhắn, ảnh/file,
  mã hóa/giải mã (sau khi GĐ6 xong), OTP, các trường hợp lỗi.
- **GĐ9 (Hoàn thiện)** — giao diện, bảo mật, tối ưu, viết báo cáo, chuẩn bị demo.

## 6. Xác thực

Đăng ký: `TenTaiKhoan` + `Email` + `Password` → sinh `Salt` ngẫu nhiên → `SHA-256(Password + Salt)`
→ lưu MongoDB. Đăng nhập: `TenTaiKhoan` hoặc `Email` + `Password` → so khớp `MatKhauBam` → phát
JWT. Cả hai đều code thật từ GĐ3 (không thuộc diện stub).

## 7. Gửi ảnh/file

- Upload qua **endpoint REST riêng** (`multipart/form-data`), không qua SignalR — giữ kênh chat
  text luôn nhẹ và tức thời bất kể ảnh/file đang được tải.
- Lưu file **trên đĩa** của `HaloChat.Api` (thư mục `uploads/`), MongoDB chỉ lưu metadata. Lý do:
  nhúng file vào document MongoDB sẽ đụng giới hạn cứng 16MB/document và làm phình database.
- Giới hạn kích thước: ảnh tối đa **5MB**, file khác tối đa **20MB** — kiểm tra ở **cả client và
  server** (server là bắt buộc, dùng Kestrel `MaxRequestBodySize` + `RequestFormLimits`, không chỉ
  dựa vào client).
- Loại file cho phép: ảnh (jpg/png/gif/webp) + tài liệu phổ biến (pdf/docx/xlsx/zip); chặn file
  thực thi (.exe/.bat/.sh/.msi...).
- Sau khi lưu xong, gửi một thông báo nhỏ (metadata) qua SignalR cho người nhận — kích thước gói
  tin tương đương một tin nhắn text, không đẩy dữ liệu nhị phân qua WebSocket.
- Làm cùng GĐ5, plaintext (chưa mã hóa nội dung file); mã hóa file (nếu nhóm muốn mở rộng) tính ở
  GĐ6 cùng đợt với mã hóa text.

## 8. Hiệu năng & khả năng chịu tải khi nhiều người gửi file cùng lúc

Ghi lại để đưa vào phần "Đánh giá mức độ an toàn và khả năng triển khai" của báo cáo:

- **Bắt buộc khi code:** upload phải **stream thẳng ra đĩa**, không đọc hết file vào bộ nhớ dạng
  byte array trước khi lưu — nếu buffer vào RAM, nhiều người upload file lớn cùng lúc sẽ làm RAM
  tăng theo số người thay vì gần như không đổi.
- Với quy mô đồ án (vài chục người dùng cùng lúc), Kestrel xử lý bất đồng bộ đủ sức chịu tải mà
  không cần thêm hạ tầng (queue/CDN/storage riêng). Kênh chat text (SignalR) không bị ảnh hưởng bởi
  upload vì đi trên kết nối/luồng xử lý riêng.
- Băng thông mạng và tốc độ đĩa của máy chạy server là giới hạn thực sự khi số người upload đồng
  thời tăng cao (vd. 100 người cùng lúc) — không phải lỗi kiến trúc, chỉ là chia sẻ tài nguyên vật
  lý, thời gian upload mỗi người sẽ dài hơn tương ứng.
- **Cải tiến tùy chọn** (không bắt buộc, có thể nêu là hướng mở rộng trong báo cáo): giới hạn số
  upload đồng thời tối đa bằng một semaphore đơn giản trong `HaloChat.Api`, để vượt ngưỡng thì
  request phải chờ tới lượt thay vì tất cả cùng chậm lại.

## 9. Chính sách stub bảo mật (GĐ6, nhóm tự viết)

Áp dụng đúng ví dụ trong tài liệu — method rỗng, có chú thích mô tả việc cần làm:

```csharp
public class DichVuMaHoa
{
    public string MaHoaTinNhan(string noiDungTinNhan)
    {
        // [BẢO MẬT - GĐ6]
        // Mã hóa nội dung bằng AES-256-GCM: sinh Nonce, trả về Ciphertext + AuthTag.
        return "";
    }

    public string GiaiMaTinNhan(string tinNhanDaMaHoa)
    {
        // [BẢO MẬT - GĐ6]
        // Giải mã nội dung bằng AES-256-GCM dùng Nonce + AuthTag đã lưu kèm.
        return "";
    }

    // [BẢO MẬT - GĐ6]
    // Mã hóa AES Session Key bằng RSA-OAEP với Public Key người nhận trước khi gửi Server.

    // [BẢO MẬT - GĐ6]
    // Giải mã AES Session Key bằng RSA Private Key của người nhận.
}
```

Các method này **không được gọi** trong luồng gửi/nhận ở GĐ3-5 (xem §5) — chat vẫn chạy đầy đủ với
`NoiDungTinNhan` plaintext cho tới khi nhóm hoàn thành GĐ6 và nối lệnh gọi vào Hub/Controller.

**Lưu ý về mô hình khóa:** tài liệu yêu cầu lưu cả `KhoaCongKhai` và `KhoaBiMat` trong document
`NguoiDung` trên MongoDB. Đây là đơn giản hóa hợp lý cho phạm vi đồ án (không phải E2EE thực thụ,
vì server có khả năng truy cập Private Key) — theo đúng thiết kế trong tài liệu, không tự ý đổi
sang mô hình lưu khóa phía client. Điểm này nên được nêu rõ như một giới hạn đã biết khi viết phần
"Đánh giá mức độ an toàn" trong báo cáo.

## 10. Quên mật khẩu (module riêng, làm sau)

Email → Server tạo OTP → gửi OTP qua Email → xác thực OTP → nhập mật khẩu mới → SHA-256 + Salt →
cập nhật `NguoiDung`. Không gửi mật khẩu cũ qua email. Thực hiện sau khi chat chính (GĐ3-6) hoàn
thành, theo đúng thứ tự trong tài liệu.

## 11. Kiểm thử

Swagger cho toàn bộ API. Test theo đúng danh sách tài liệu: đăng ký/đăng nhập, gửi/nhận tin nhắn,
gửi ảnh/file, mã hóa/giải mã (sau GĐ6), OTP, các trường hợp lỗi (sai mật khẩu, file quá khổ, loại
file bị chặn, token hết hạn...).

## 12. Quy ước đặt tên

- Biến: camelCase, tiếng Việt không dấu (`tenTaiKhoan`, `email`, `matKhau`, `noiDungTinNhan`,
  `maOtp`, `khoaMaHoa`, `tinNhanDaMaHoa`).
- Hàm: PascalCase, tiếng Việt không dấu (`DangKyTaiKhoan()`, `DangNhap()`, `GuiTinNhan()`,
  `NhanTinNhan()`, `MaHoaTinNhan()`, `GiaiMaTinNhan()`, `KiemTraMatKhau()`, `GuiMaOtp()`,
  `DatLaiMatKhau()`).
- Class: PascalCase, tiếng Việt không dấu (`NguoiDung`, `TinNhan`, `PhienChat`, `DichVuMaHoa`,
  `DichVuNguoiDung`, `DichVuTinNhan`).
- Giữ nguyên thuật ngữ kỹ thuật quen thuộc: JWT, SignalR, MongoDB, AES256, RSA, SHA256, OTP.
- Tên project/namespace (`HaloChat.Api`, `HaloChat.Security`) giữ tiếng Anh vì là tên sản phẩm,
  không thuộc phạm vi quy ước biến/hàm/class nghiệp vụ.

## 13. Ngoài phạm vi bản thiết kế này

- Cài đặt thật AES-256-GCM/RSA-OAEP vào luồng chat (GĐ6) — nhóm tự làm, bản này chỉ dựng chỗ trống
  và chú thích.
- Module quên mật khẩu (GĐ7) — thiết kế đã nêu ở §10, triển khai sau khi chat chính xong.
- Hạ tầng chịu tải nâng cao (CDN, hàng đợi, lưu trữ phân tán) — không cần cho phạm vi đồ án (§8).
