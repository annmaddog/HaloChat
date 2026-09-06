# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Trạng thái hiện tại

Khung web (giai đoạn 1) đã hoàn thiện: đăng ký/đăng nhập, danh sách người
dùng, chat 1–1 thời gian thực, lưu lịch sử, presence online/offline, và
interface `IMessageCipher` sẵn sàng để nhóm bảo mật cắm AES thật vào (xem
`docs/superpowers/specs/2026-09-06-chat-aes-web-scaffold-design.md`).

## Bối cảnh dự án — đọc trước khi đụng vào phần mã hóa

Đây là đồ án môn học, chia làm **hai giai đoạn do hai nhóm phụ trách khác nhau**:

1. **Giai đoạn 1 (khung web)** — dựng chat 1–1 chạy đầy đủ (đăng ký/đăng
   nhập, danh sách người dùng, chat thời gian thực, lưu lịch sử, trạng thái
   online/offline), tin nhắn đi qua interface `IMessageCipher` với cài đặt
   tạm `PlaintextMessageCipher` (chưa mã hóa thật).
2. **Giai đoạn 2 (bảo mật, nhóm khác làm)** — cài AES thật (`AesMessageCipher`
   implement `IMessageCipher`), sinh/quản lý khóa, và một console app riêng
   `HaloChat.Benchmark` để đo thời gian mã hóa/giải mã + so sánh kích thước
   plaintext/ciphertext cho AES-128/192/256 ở 4 mốc (100B/1KB/10KB/100KB).

**Quy tắc quan trọng:** không tự ý cài AES thật hay logic quản lý khóa vào
`HaloChat.Web` — mọi thứ liên quan mã hóa chỉ đi qua `IMessageCipher` trong
`HaloChat.Security`, để nhóm bảo mật cắm vào sau mà không phải sửa web app.
Benchmark AES chạy **độc lập ngoài web**, không tích hợp vào sản phẩm chat.

Toàn bộ quyết định kiến trúc, data model, luồng dữ liệu, xử lý lỗi và kế
hoạch test nằm trong
`docs/superpowers/specs/2026-09-06-chat-aes-web-scaffold-design.md` — đọc
file đó trước khi thay đổi kiến trúc, đừng suy đoán lại từ đầu.

## Kiến trúc dự kiến (theo spec)

```
HaloChat.sln
src/
├─ HaloChat.Web/        # ASP.NET Core 8 MVC + Razor Pages (Identity) + SignalR
└─ HaloChat.Security/   # class library dùng chung: IMessageCipher, EncryptedPayload,
                        #   PlaintextMessageCipher (sau này thêm AesMessageCipher)
```

- `HaloChat.Web` tham chiếu `HaloChat.Security`.
- `HaloChat.Benchmark` (console, nhóm bảo mật tạo ở giai đoạn 2) cũng sẽ tham
  chiếu `HaloChat.Security`, tách biệt hoàn toàn với `HaloChat.Web`.
- Real-time: `Hubs/ChatHub.cs` xử lý gửi/nhận tin nhắn và presence
  online/offline; client JS ở `wwwroot/js/chat.js`.
- Auth: ASP.NET Core Identity (Areas/Identity), không tự viết xác thực riêng.
- Database: SQL Server (LocalDB/Express) qua EF Core; `Message` đã có sẵn
  cột `CipherText`/`Iv`/`Tag`/`Algorithm` để giai đoạn 2 cắm AES vào mà
  không cần migration mới.

## Lệnh phát triển

```bash
dotnet build                                        # build toàn bộ solution
dotnet test                                         # chạy toàn bộ test (Security + Web)
dotnet run --project src/HaloChat.Web                # chạy web app (cần SQL Server Express)
dotnet ef migrations add <Tên> -p src/HaloChat.Web -s src/HaloChat.Web   # thêm migration
dotnet ef database update -p src/HaloChat.Web -s src/HaloChat.Web       # áp migration vào DB
```

## Ngôn ngữ

Tài liệu đề tài, spec thiết kế và trao đổi trong dự án đều bằng tiếng Việt —
giữ nguyên tiếng Việt khi viết/cập nhật tài liệu (`docs/`, comment giải
thích nghiệp vụ); tên định danh trong code (class, method, biến) vẫn dùng
tiếng Anh theo quy ước .NET thông thường.
