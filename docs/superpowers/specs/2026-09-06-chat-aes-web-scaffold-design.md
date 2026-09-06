# Thiết kế: Khung web Chat AES (giai đoạn 1 — scaffold)

Ngày: 2026-09-06
Nguồn yêu cầu: `tài liệu đề tài cần làm.docx` (đề tài "Xây dựng ứng dụng chat an toàn sử dụng thuật toán mã hóa AES")

## 1. Mục tiêu & phạm vi

**Mục tiêu giai đoạn này:** dựng khung web chat 1–1 chạy được đầy đủ (đăng ký/đăng
nhập, danh sách người dùng, chat thời gian thực, lưu lịch sử, trạng thái
online/offline), với tin nhắn đi qua một lớp mã hóa **placeholder** (chưa mã
hóa thật) nhưng đã có sẵn "chỗ cắm" để nhóm bảo mật thay bằng AES thật mà
không phải sửa kiến trúc.

**Nằm trong phạm vi:**
- Web app ASP.NET Core 8 (MVC + Razor Pages cho Identity + SignalR).
- Đăng ký/đăng nhập bằng ASP.NET Core Identity.
- Danh sách người dùng + trạng thái online/offline (SignalR presence).
- Chat 1–1 thời gian thực, lưu lịch sử vào SQL Server.
- Interface `IMessageCipher` + cài đặt tạm `PlaintextMessageCipher`, tách
  riêng thành thư viện `HaloChat.Security` để dùng chung sau này.
- Schema DB đã có sẵn cột cho `CipherText`, `IV`, `Tag`, `Algorithm`.

**Ngoài phạm vi (nhóm bảo mật làm ở giai đoạn 2, không làm ở đây):**
- Cài đặt AES thật (AES-128/192/256) và chọn chế độ mã hóa (mode).
- Sinh/lưu trữ/trao đổi khóa.
- Console app `HaloChat.Benchmark` đo thời gian mã hóa/giải mã và so sánh
  kích thước plaintext/ciphertext cho 4 mốc kích thước (100B, 1KB, 10KB,
  100KB) × 3 biến thể AES — **chạy độc lập ngoài web**, không tích hợp vào
  web chat. Chỉ cần đảm bảo `HaloChat.Security` là thư viện riêng để console
  này tham chiếu được khi tới lượt làm.
- Chat nhóm (>2 người).
- Hạ tầng deploy production (tên miền riêng) — chưa chọn, tính sau.

## 2. Ngăn xếp công nghệ (đã chốt qua trao đổi)

| Thành phần | Lựa chọn | Lý do |
|---|---|---|
| Framework | .NET 8, ASP.NET Core | Yêu cầu của người dùng |
| Kiểu UI/real-time | MVC/Razor Pages + SignalR + JS | Đã chọn, quen thuộc, tách rõ backend/frontend |
| Auth | ASP.NET Core Identity | Có sẵn, chuẩn bảo mật, tiết kiệm thời gian |
| Database | SQL Server (LocalDB/Express) | Tích hợp sẵn với Identity/EF Core template |
| ORM | EF Core | Đi kèm chuẩn với Identity + SQL Server |
| Mã hóa (giai đoạn 1) | `IMessageCipher` / `PlaintextMessageCipher` | Chỗ cắm cho nhóm bảo mật, chưa mã hóa thật |
| Đo hiệu năng AES | Console app riêng (giai đoạn 2) | Không đụng vào luồng chat thật, dùng lại code AES thật |

## 3. Cấu trúc solution

```
HaloChat.sln
src/
├─ HaloChat.Web/
│  ├─ Areas/Identity/                 # Identity UI scaffolded (đăng ký/đăng nhập)
│  ├─ Controllers/
│  │  └─ ChatController.cs            # danh sách user, mở khung chat 1-1, load lịch sử
│  ├─ Hubs/
│  │  └─ ChatHub.cs                   # SignalR: gửi/nhận tin nhắn + presence
│  ├─ Models/
│  │  ├─ ApplicationUser.cs           # kế thừa IdentityUser
│  │  └─ Message.cs                   # SenderId, ReceiverId, CipherText, IV, Tag, Algorithm, SentAtUtc
│  ├─ Data/
│  │  └─ ApplicationDbContext.cs      # EF Core + Identity
│  ├─ wwwroot/js/chat.js              # SignalR client, render tin nhắn, chấm online/offline
│  └─ Views/Chat/
│     ├─ Index.cshtml                 # danh sách người dùng + trạng thái online
│     └─ Conversation.cshtml          # khung chat 1-1
└─ HaloChat.Security/                  # class library dùng chung
   ├─ IMessageCipher.cs
   ├─ EncryptedPayload.cs
   └─ PlaintextMessageCipher.cs
```

`HaloChat.Web` tham chiếu `HaloChat.Security`. Sau này `HaloChat.Benchmark`
(console, do nhóm bảo mật tạo) cũng tham chiếu `HaloChat.Security` — không
đụng vào `HaloChat.Web`.

## 4. Luồng gửi/nhận tin nhắn

1. Mở trang chat với 1 người → JS connect `/chatHub` (yêu cầu đã đăng nhập).
2. Gửi tin → JS gọi `hub.invoke("SendMessage", receiverId, content)`.
3. `ChatHub.SendMessage`:
   - Validate `receiverId` tồn tại và khác `SenderId`.
   - Gọi `IMessageCipher.Encrypt(content, key)` → nhận `EncryptedPayload`
     (giai đoạn 1: `key` là placeholder, không dùng thật).
   - Lưu `Message` vào DB qua EF Core.
   - Gửi cho người nhận qua `Clients.User(receiverId).SendAsync("ReceiveMessage", ...)`
     nếu đang online; trả về cho người gửi để hiển thị ngay (optimistic update).
4. Khi tải lại lịch sử, `ChatController` load các `Message` từ DB, gọi
   `IMessageCipher.Decrypt(...)` cho từng dòng để hiển thị nội dung.

## 5. Presence online/offline

- `ChatHub.OnConnectedAsync` / `OnDisconnectedAsync` đếm số connection theo
  UserId (`Context.UserIdentifier`, mặc định SignalR đã lấy từ claim
  `ClaimTypes.NameIdentifier` của Identity, không cần cấu hình thêm) trong
  `ConcurrentDictionary<string, int>` — một user mở nhiều tab vẫn tính 1
  online.
- Khi trạng thái đổi (0↔1 connection) → broadcast
  `Clients.All.SendAsync("PresenceChanged", userId, isOnline)`.
- `Views/Chat/Index.cshtml` hiển thị chấm xanh/xám, JS cập nhật khi nhận
  event `PresenceChanged`.

## 6. Data model

```csharp
public class Message
{
    public int Id { get; set; }
    public string SenderId { get; set; } = default!;
    public string ReceiverId { get; set; } = default!;
    public byte[] CipherText { get; set; } = default!; // giai đoạn 1: UTF8 bytes của plaintext
    public byte[]? Iv { get; set; }                     // null ở giai đoạn 1
    public byte[]? Tag { get; set; }                     // dành cho GCM, null ở giai đoạn 1
    public string Algorithm { get; set; } = "none";      // "AES-256-GCM" v.v. ở giai đoạn 2
    public DateTime SentAtUtc { get; set; }
}
```

## 7. Chỗ cắm mã hóa (`HaloChat.Security`)

```csharp
public interface IMessageCipher
{
    EncryptedPayload Encrypt(string plaintext, string key);
    string Decrypt(EncryptedPayload payload, string key);
}

public record EncryptedPayload(byte[] CipherText, byte[]? Iv, byte[]? Tag, string Algorithm);

public class PlaintextMessageCipher : IMessageCipher
{
    public EncryptedPayload Encrypt(string plaintext, string key)
        => new(Encoding.UTF8.GetBytes(plaintext), null, null, "none");

    public string Decrypt(EncryptedPayload payload, string key)
        => Encoding.UTF8.GetString(payload.CipherText);
}
```

Đăng ký DI trong `HaloChat.Web`: `services.AddScoped<IMessageCipher, PlaintextMessageCipher>();`

Giai đoạn 2, nhóm bảo mật chỉ cần:
1. Viết `AesMessageCipher : IMessageCipher` trong `HaloChat.Security` (chọn
   mode, xử lý padding/IV/tag theo biến thể AES chọn cho chat thật).
2. Thiết kế cách sinh/lưu/trao đổi `key` (không thuộc phạm vi interface này).
3. Đổi dòng đăng ký DI sang `AesMessageCipher`.
4. Không cần sửa `ChatHub`, `ChatController`, hay migration DB (đã có sẵn
   cột `Iv`/`Tag`/`Algorithm`).

Việc đo hiệu năng AES-128/192/256 (thời gian mã hóa/giải mã, so sánh kích
thước plaintext/ciphertext ở 4 mốc 100B/1KB/10KB/100KB) làm ở
`HaloChat.Benchmark` — console app riêng, gọi thẳng các class trong
`HaloChat.Security`, không tích hợp vào web.

## 8. Xử lý lỗi

- `ChatHub.SendMessage` bọc try/catch; lỗi lưu DB/mã hóa → gửi
  `Clients.Caller.SendAsync("SendFailed", errorMessage)` để JS báo "Gửi thất
  bại, thử lại".
- Identity dùng cơ chế lỗi có sẵn (ModelState/TempData) cho đăng ký/đăng
  nhập.
- Validate `ReceiverId` tồn tại & khác `SenderId` trước khi lưu.

## 9. Testing

- xUnit cho `PlaintextMessageCipher` (round-trip Encrypt→Decrypt trả lại
  đúng plaintext) — làm khung mẫu để nhóm bảo mật viết test tương tự cho
  `AesMessageCipher` sau này.
- Không viết test cho `ChatHub` ở giai đoạn này (khó unit test SignalR hub,
  để mức MVP; có thể thêm integration test bằng `WebApplicationFactory`
  sau nếu còn thời gian).

## 10. Quyết định đã chốt qua trao đổi (log)

- Mô hình UI/real-time: MVC/Razor Pages + SignalR + JS.
- Hạ tầng deploy: chưa chọn, tính sau (không ảnh hưởng khung code).
- Database: SQL Server (LocalDB/Express).
- Auth: ASP.NET Core Identity.
- Có chỗ cắm mã hóa sẵn (interface + cột DB dự phòng): có.
- Presence online/offline: có, làm từ đầu.
- Benchmark AES: console app riêng, **không tích hợp vào web**; web chat chỉ
  dùng AES để mã hóa tin nhắn thực tế (không hiển thị bảng so sánh biến thể
  trong sản phẩm).
