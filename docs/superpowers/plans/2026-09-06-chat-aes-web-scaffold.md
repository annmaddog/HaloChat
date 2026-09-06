# Khung Web Chat AES Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Dựng khung web chat 1–1 chạy được đầy đủ trên .NET 8 (đăng ký/đăng nhập, danh sách người dùng, chat thời gian thực, lưu lịch sử, trạng thái online/offline), với tin nhắn đi qua một interface mã hóa placeholder đã sẵn sàng để nhóm bảo mật cắm AES thật vào ở giai đoạn 2.

**Architecture:** ASP.NET Core 8 MVC (scaffold bằng `-au Individual -uld` để có sẵn Identity + SQL Server LocalDB) + SignalR cho real-time messaging/presence + EF Core cho lưu trữ. Logic mã hóa tách thành thư viện `HaloChat.Security` riêng (dùng chung sau này với console benchmark của nhóm bảo mật). Các phần logic thuần (cipher, mapper, presence tracker) được TDD; phần hạ tầng (Identity, Hub, Controller, Views, JS) xác minh thủ công theo đúng phạm vi đã chốt trong spec.

**Tech Stack:** .NET 8, ASP.NET Core MVC, ASP.NET Core Identity, SignalR, EF Core + SQL Server (LocalDB), xUnit, EF Core InMemory provider (cho test), SignalR JS client (CDN jsDelivr).

**Spec:** `docs/superpowers/specs/2026-09-06-chat-aes-web-scaffold-design.md`

## Global Constraints

- .NET SDK: 8.0 (kiểm tra `dotnet --version` bắt đầu bằng `8.`).
- Database: SQL Server LocalDB/Express — **không** dùng SQLite hay PostgreSQL.
- Auth: chỉ dùng ASP.NET Core Identity có sẵn — không tự viết xác thực.
- Mọi mã hóa/giải mã tin nhắn **chỉ** đi qua `IMessageCipher` (namespace `HaloChat.Security`) — không cài AES thật, không thiết kế quản lý khóa trong plan này (thuộc phạm vi nhóm bảo mật, giai đoạn 2).
- Không tạo `HaloChat.Benchmark` — nằm ngoài phạm vi plan này.
- Không làm chat nhóm (>2 người), không chọn hạ tầng deploy.
- Mỗi commit kết thúc bằng dòng `Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>`.

---

## Task 1: Solution & Project Scaffold

**Files:**
- Create: `HaloChat.sln`
- Create: `src/HaloChat.Web/` (scaffold bằng `dotnet new mvc -au Individual -uld`)
- Create: `src/HaloChat.Security/` (scaffold bằng `dotnet new classlib`)
- Modify: `.gitignore` (đã có sẵn, không cần sửa trừ khi thiếu mục)

**Interfaces:**
- Consumes: không có (task đầu tiên)
- Produces: solution `HaloChat.sln` build được; `HaloChat.Web` tham chiếu `HaloChat.Security`

- [ ] **Step 1: Kiểm tra tiên quyết**

Run: `dotnet --version`
Expected: bắt đầu bằng `8.` (ví dụ `8.0.4xx`). Nếu không, cài .NET 8 SDK trước khi tiếp tục.

- [ ] **Step 2: Tạo solution**

```bash
cd /e/BMHTTT
dotnet new sln -n HaloChat
```

- [ ] **Step 3: Tạo project HaloChat.Web với Identity + LocalDB**

```bash
dotnet new mvc -au Individual -uld -o src/HaloChat.Web
```

Lệnh này tự sinh sẵn: `Areas/Identity/Data/ApplicationUser.cs`, `Data/ApplicationDbContext.cs`,
Program.cs đã cấu hình `AddDefaultIdentity` + SQL Server LocalDB, và các trang đăng
ký/đăng nhập mặc định.

- [ ] **Step 4: Tạo project HaloChat.Security**

```bash
dotnet new classlib -o src/HaloChat.Security
rm src/HaloChat.Security/Class1.cs
```

- [ ] **Step 5: Thêm 2 project vào solution, thêm project reference**

```bash
dotnet sln HaloChat.sln add src/HaloChat.Web/HaloChat.Web.csproj src/HaloChat.Security/HaloChat.Security.csproj
dotnet add src/HaloChat.Web/HaloChat.Web.csproj reference src/HaloChat.Security/HaloChat.Security.csproj
```

- [ ] **Step 6: Build thử để chắc chắn scaffold hợp lệ**

Run: `dotnet build`
Expected: `Build succeeded.` không có lỗi.

- [ ] **Step 7: Commit**

```bash
git add HaloChat.sln src/
git commit -m "$(cat <<'EOF'
Scaffold HaloChat.Web (MVC + Identity + LocalDB) và HaloChat.Security

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 2: Sửa cấu hình Identity để đăng ký/đăng nhập hoạt động ngay + migration đầu tiên

Mặc định template yêu cầu xác nhận email trước khi đăng nhập được
(`RequireConfirmedAccount = true`) — nếu không sửa, người dùng đăng ký xong sẽ
không đăng nhập được vì chưa cấu hình gửi email thật. Task này tắt yêu cầu đó
và chuyển `ApplicationUser` vào `Models/` để khớp với vị trí đã mô tả trong
spec.

**Files:**
- Modify: `src/HaloChat.Web/Program.cs`
- Move: `src/HaloChat.Web/Areas/Identity/Data/ApplicationUser.cs` → `src/HaloChat.Web/Models/ApplicationUser.cs`
- Modify: `src/HaloChat.Web/Data/ApplicationDbContext.cs` (cập nhật `using`)

**Interfaces:**
- Consumes: scaffold từ Task 1
- Produces: `HaloChat.Web.Models.ApplicationUser` (namespace mới, dùng bởi mọi task sau)

- [ ] **Step 1: Tìm file chứa `class ApplicationUser`**

Run: `grep -rln "class ApplicationUser" src/HaloChat.Web`
Expected: `src/HaloChat.Web/Areas/Identity/Data/ApplicationUser.cs`
(Nếu đường dẫn khác, dùng đường dẫn thực tế đó ở các bước dưới.)

- [ ] **Step 2: Di chuyển file và đổi namespace**

```bash
mkdir -p src/HaloChat.Web/Models
git mv src/HaloChat.Web/Areas/Identity/Data/ApplicationUser.cs src/HaloChat.Web/Models/ApplicationUser.cs
```

Sửa nội dung `src/HaloChat.Web/Models/ApplicationUser.cs`, đổi dòng
`namespace HaloChat.Web.Areas.Identity.Data;` thành:

```csharp
namespace HaloChat.Web.Models;
```

- [ ] **Step 3: Cập nhật using trong ApplicationDbContext.cs**

Trong `src/HaloChat.Web/Data/ApplicationDbContext.cs`, tìm dòng
`using HaloChat.Web.Areas.Identity.Data;` và đổi thành:

```csharp
using HaloChat.Web.Models;
```

(Nếu file dùng namespace đầy đủ trực tiếp thay vì `using`, sửa tương ứng
thành `HaloChat.Web.Models.ApplicationUser`.)

- [ ] **Step 4: Cập nhật using trong Program.cs (nếu có)**

Run: `grep -n "Areas.Identity.Data" src/HaloChat.Web/Program.cs`
Nếu có kết quả, đổi `HaloChat.Web.Areas.Identity.Data` thành `HaloChat.Web.Models`
trong dòng đó.

- [ ] **Step 5: Tắt yêu cầu xác nhận email**

Trong `src/HaloChat.Web/Program.cs`, tìm:

```csharp
options.SignIn.RequireConfirmedAccount = true
```

Đổi thành:

```csharp
options.SignIn.RequireConfirmedAccount = false
```

- [ ] **Step 6: Build lại để chắc chắn không lỗi tham chiếu**

Run: `dotnet build`
Expected: `Build succeeded.`

- [ ] **Step 7: Tạo migration đầu tiên cho schema Identity**

```bash
dotnet tool install --global dotnet-ef 2>/dev/null || true
dotnet ef migrations list -p src/HaloChat.Web -s src/HaloChat.Web
```

Nếu danh sách rỗng (chưa có migration nào), chạy:

```bash
dotnet ef migrations add InitialCreate -p src/HaloChat.Web -s src/HaloChat.Web
```

- [ ] **Step 8: Áp migration vào LocalDB**

```bash
dotnet ef database update -p src/HaloChat.Web -s src/HaloChat.Web
```

Expected: không có lỗi kết nối. Nếu báo lỗi không tìm thấy LocalDB, cài đặt
"SQL Server Express LocalDB" (đi kèm Visual Studio workload ASP.NET/web, hoặc
tải riêng từ trang Microsoft) rồi chạy lại.

- [ ] **Step 9: Xác minh thủ công đăng ký/đăng nhập**

```bash
dotnet run --project src/HaloChat.Web
```

Mở trình duyệt tới URL hiện trong console (vd `https://localhost:xxxx`),
bấm "Register", đăng ký một tài khoản test, xác nhận được chuyển vào trang
chủ ở trạng thái đã đăng nhập (không bị chặn lại vì "chưa xác nhận email").
Dừng server (Ctrl+C) sau khi xác minh xong.

- [ ] **Step 10: Commit**

```bash
git add -A
git commit -m "$(cat <<'EOF'
Cấu hình Identity đăng nhập không cần xác nhận email, chuyển ApplicationUser vào Models/

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 3: Tạo 2 test project (xUnit)

**Files:**
- Create: `tests/HaloChat.Security.Tests/`
- Create: `tests/HaloChat.Web.Tests/`

**Interfaces:**
- Consumes: `src/HaloChat.Security/HaloChat.Security.csproj`, `src/HaloChat.Web/HaloChat.Web.csproj`
- Produces: 2 test project rỗng, build và `dotnet test` chạy được (0 test)

- [ ] **Step 1: Tạo project test**

```bash
dotnet new xunit -o tests/HaloChat.Security.Tests
dotnet new xunit -o tests/HaloChat.Web.Tests
rm tests/HaloChat.Security.Tests/UnitTest1.cs
rm tests/HaloChat.Web.Tests/UnitTest1.cs
```

- [ ] **Step 2: Thêm vào solution và thêm project reference**

```bash
dotnet sln HaloChat.sln add tests/HaloChat.Security.Tests/HaloChat.Security.Tests.csproj tests/HaloChat.Web.Tests/HaloChat.Web.Tests.csproj
dotnet add tests/HaloChat.Security.Tests reference src/HaloChat.Security
dotnet add tests/HaloChat.Web.Tests reference src/HaloChat.Web
```

- [ ] **Step 3: Build + chạy test rỗng**

Run: `dotnet build && dotnet test`
Expected: build thành công, `dotnet test` báo 0 test chạy (không lỗi).

- [ ] **Step 4: Commit**

```bash
git add tests/ HaloChat.sln
git commit -m "$(cat <<'EOF'
Thêm project test xUnit cho HaloChat.Security và HaloChat.Web

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 4: IMessageCipher + EncryptedPayload + PlaintextMessageCipher (TDD)

**Files:**
- Create: `src/HaloChat.Security/EncryptedPayload.cs`
- Create: `src/HaloChat.Security/IMessageCipher.cs`
- Create: `src/HaloChat.Security/PlaintextMessageCipher.cs`
- Test: `tests/HaloChat.Security.Tests/PlaintextMessageCipherTests.cs`

**Interfaces:**
- Consumes: không có
- Produces:
  - `record EncryptedPayload(byte[] CipherText, byte[]? Iv, byte[]? Tag, string Algorithm)`
  - `interface IMessageCipher { EncryptedPayload Encrypt(string plaintext, string key); string Decrypt(EncryptedPayload payload, string key); }`
  - `class PlaintextMessageCipher : IMessageCipher`
  - Dùng bởi: Task 6 (ChatHistoryMapper), Task 8 (ChatHub), Task 9 (ChatController)

- [ ] **Step 1: Viết test thất bại**

Tạo `tests/HaloChat.Security.Tests/PlaintextMessageCipherTests.cs`:

```csharp
using HaloChat.Security;
using Xunit;

namespace HaloChat.Security.Tests;

public class PlaintextMessageCipherTests
{
    [Fact]
    public void Encrypt_SetsAlgorithmToNone()
    {
        var cipher = new PlaintextMessageCipher();

        var result = cipher.Encrypt("hello", key: "");

        Assert.Equal("none", result.Algorithm);
    }

    [Fact]
    public void Encrypt_ThenDecrypt_ReturnsOriginalPlaintext()
    {
        var cipher = new PlaintextMessageCipher();
        var original = "Xin chào, đây là tin nhắn test có dấu tiếng Việt";

        var encrypted = cipher.Encrypt(original, key: "");
        var decrypted = cipher.Decrypt(encrypted, key: "");

        Assert.Equal(original, decrypted);
    }

    [Fact]
    public void Encrypt_DoesNotSetIvOrTag()
    {
        var cipher = new PlaintextMessageCipher();

        var result = cipher.Encrypt("hello", key: "");

        Assert.Null(result.Iv);
        Assert.Null(result.Tag);
    }
}
```

- [ ] **Step 2: Chạy test, xác nhận thất bại (không build được vì thiếu type)**

Run: `dotnet test tests/HaloChat.Security.Tests`
Expected: FAIL — lỗi biên dịch "The type or namespace name 'PlaintextMessageCipher' could not be found" (hoặc tương tự).

- [ ] **Step 3: Cài đặt tối thiểu**

Tạo `src/HaloChat.Security/EncryptedPayload.cs`:

```csharp
namespace HaloChat.Security;

public record EncryptedPayload(byte[] CipherText, byte[]? Iv, byte[]? Tag, string Algorithm);
```

Tạo `src/HaloChat.Security/IMessageCipher.cs`:

```csharp
namespace HaloChat.Security;

public interface IMessageCipher
{
    EncryptedPayload Encrypt(string plaintext, string key);
    string Decrypt(EncryptedPayload payload, string key);
}
```

Tạo `src/HaloChat.Security/PlaintextMessageCipher.cs`:

```csharp
using System.Text;

namespace HaloChat.Security;

/// <summary>
/// Cài đặt tạm thời — KHÔNG mã hóa gì cả, chỉ chuyển plaintext thành UTF-8 bytes.
/// Nhóm bảo mật sẽ thay bằng AesMessageCipher ở giai đoạn 2.
/// </summary>
public class PlaintextMessageCipher : IMessageCipher
{
    public EncryptedPayload Encrypt(string plaintext, string key)
        => new(Encoding.UTF8.GetBytes(plaintext), null, null, "none");

    public string Decrypt(EncryptedPayload payload, string key)
        => Encoding.UTF8.GetString(payload.CipherText);
}
```

- [ ] **Step 4: Chạy lại test, xác nhận pass**

Run: `dotnet test tests/HaloChat.Security.Tests`
Expected: PASS — 3/3 test thành công.

- [ ] **Step 5: Commit**

```bash
git add src/HaloChat.Security tests/HaloChat.Security.Tests
git commit -m "$(cat <<'EOF'
Thêm IMessageCipher + PlaintextMessageCipher (chỗ cắm mã hóa cho giai đoạn 2)

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 5: Message entity + ApplicationDbContext + migration AddMessages

**Files:**
- Create: `src/HaloChat.Web/Models/Message.cs`
- Modify: `src/HaloChat.Web/Data/ApplicationDbContext.cs`
- Test: `tests/HaloChat.Web.Tests/MessageRepositoryTests.cs`

**Interfaces:**
- Consumes: `ApplicationDbContext` (Task 1/2)
- Produces: `HaloChat.Web.Models.Message`, `ApplicationDbContext.Messages : DbSet<Message>` — dùng bởi Task 6, 8, 9

- [ ] **Step 1: Thêm package EF Core InMemory cho test project**

```bash
dotnet add tests/HaloChat.Web.Tests package Microsoft.EntityFrameworkCore.InMemory --version 8.0.*
```

- [ ] **Step 2: Viết test thất bại**

Tạo `tests/HaloChat.Web.Tests/MessageRepositoryTests.cs`:

```csharp
using HaloChat.Web.Data;
using HaloChat.Web.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HaloChat.Web.Tests;

public class MessageRepositoryTests
{
    private static ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task SaveMessage_ThenQuery_ReturnsSameCipherTextBytes()
    {
        await using var db = CreateInMemoryContext();
        var message = new Message
        {
            SenderId = "user-a",
            ReceiverId = "user-b",
            CipherText = new byte[] { 1, 2, 3, 4 },
            Algorithm = "none",
            SentAtUtc = DateTime.UtcNow
        };

        db.Messages.Add(message);
        await db.SaveChangesAsync();

        var saved = await db.Messages.SingleAsync();
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, saved.CipherText);
        Assert.Equal("user-a", saved.SenderId);
    }

    [Fact]
    public async Task Query_ConversationBetweenTwoUsers_ExcludesOtherUsersMessages()
    {
        await using var db = CreateInMemoryContext();
        db.Messages.AddRange(
            new Message { SenderId = "a", ReceiverId = "b", CipherText = new byte[] { 1 }, Algorithm = "none", SentAtUtc = DateTime.UtcNow },
            new Message { SenderId = "b", ReceiverId = "a", CipherText = new byte[] { 2 }, Algorithm = "none", SentAtUtc = DateTime.UtcNow },
            new Message { SenderId = "a", ReceiverId = "c", CipherText = new byte[] { 3 }, Algorithm = "none", SentAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var conversation = await db.Messages
            .Where(m => (m.SenderId == "a" && m.ReceiverId == "b") || (m.SenderId == "b" && m.ReceiverId == "a"))
            .ToListAsync();

        Assert.Equal(2, conversation.Count);
    }
}
```

- [ ] **Step 3: Chạy test, xác nhận thất bại**

Run: `dotnet test tests/HaloChat.Web.Tests`
Expected: FAIL — lỗi biên dịch vì `Message` và `db.Messages` chưa tồn tại.

- [ ] **Step 4: Tạo Message entity**

Tạo `src/HaloChat.Web/Models/Message.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace HaloChat.Web.Models;

public class Message
{
    public int Id { get; set; }

    [Required]
    public string SenderId { get; set; } = default!;

    [Required]
    public string ReceiverId { get; set; } = default!;

    [Required]
    public byte[] CipherText { get; set; } = default!;

    public byte[]? Iv { get; set; }

    public byte[]? Tag { get; set; }

    [Required]
    public string Algorithm { get; set; } = "none";

    public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;
}
```

- [ ] **Step 5: Thêm DbSet vào ApplicationDbContext**

Trong `src/HaloChat.Web/Data/ApplicationDbContext.cs`, thêm
`using HaloChat.Web.Models;` ở đầu file nếu chưa có, và thêm vào trong thân
class:

```csharp
public DbSet<Message> Messages => Set<Message>();

protected override void OnModelCreating(ModelBuilder builder)
{
    base.OnModelCreating(builder);

    builder.Entity<Message>()
        .HasIndex(m => new { m.SenderId, m.ReceiverId });
}
```

(Nếu class đã có sẵn override `OnModelCreating` rỗng, chỉ cần thêm dòng
`builder.Entity<Message>()...` vào sau `base.OnModelCreating(builder);` thay
vì tạo override mới.)

- [ ] **Step 6: Chạy lại test, xác nhận pass**

Run: `dotnet test tests/HaloChat.Web.Tests`
Expected: PASS — 2/2 test thành công.

- [ ] **Step 7: Tạo và áp migration AddMessages**

```bash
dotnet ef migrations add AddMessages -p src/HaloChat.Web -s src/HaloChat.Web
dotnet ef database update -p src/HaloChat.Web -s src/HaloChat.Web
```

Expected: migration chạy không lỗi, bảng `Messages` xuất hiện trong LocalDB.

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "$(cat <<'EOF'
Thêm Message entity + migration AddMessages

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 6: ChatHistoryMapper (TDD)

**Files:**
- Create: `src/HaloChat.Web/ViewModels/MessageViewModel.cs`
- Create: `src/HaloChat.Web/Services/ChatHistoryMapper.cs`
- Test: `tests/HaloChat.Web.Tests/ChatHistoryMapperTests.cs`

**Interfaces:**
- Consumes: `HaloChat.Web.Models.Message` (Task 5), `HaloChat.Security.IMessageCipher`/`EncryptedPayload`/`PlaintextMessageCipher` (Task 4)
- Produces: `MessageViewModel { string Content; bool IsMine; DateTime SentAtUtc; }`,
  `static ChatHistoryMapper.MapToViewModels(IEnumerable<Message> messages, IMessageCipher cipher, string currentUserId) : List<MessageViewModel>` — dùng bởi Task 9 (ChatController)

- [ ] **Step 1: Viết test thất bại**

Tạo `tests/HaloChat.Web.Tests/ChatHistoryMapperTests.cs`:

```csharp
using HaloChat.Security;
using HaloChat.Web.Models;
using HaloChat.Web.Services;
using Xunit;

namespace HaloChat.Web.Tests;

public class ChatHistoryMapperTests
{
    [Fact]
    public void MapToViewModels_OrdersMessagesBySentTimeAscending()
    {
        var cipher = new PlaintextMessageCipher();
        var later = cipher.Encrypt("tin nhắn sau", key: "");
        var earlier = cipher.Encrypt("tin nhắn trước", key: "");
        var messages = new List<Message>
        {
            new() { SenderId = "a", ReceiverId = "b", CipherText = later.CipherText, Algorithm = later.Algorithm, SentAtUtc = new DateTime(2026, 1, 2) },
            new() { SenderId = "a", ReceiverId = "b", CipherText = earlier.CipherText, Algorithm = earlier.Algorithm, SentAtUtc = new DateTime(2026, 1, 1) }
        };

        var result = ChatHistoryMapper.MapToViewModels(messages, cipher, currentUserId: "a");

        Assert.Equal("tin nhắn trước", result[0].Content);
        Assert.Equal("tin nhắn sau", result[1].Content);
    }

    [Fact]
    public void MapToViewModels_SetsIsMineBasedOnSenderId()
    {
        var cipher = new PlaintextMessageCipher();
        var payload = cipher.Encrypt("hello", key: "");
        var messages = new List<Message>
        {
            new() { SenderId = "me", ReceiverId = "them", CipherText = payload.CipherText, Algorithm = payload.Algorithm, SentAtUtc = DateTime.UtcNow },
            new() { SenderId = "them", ReceiverId = "me", CipherText = payload.CipherText, Algorithm = payload.Algorithm, SentAtUtc = DateTime.UtcNow }
        };

        var result = ChatHistoryMapper.MapToViewModels(messages, cipher, currentUserId: "me");

        Assert.True(result[0].IsMine);
        Assert.False(result[1].IsMine);
    }

    [Fact]
    public void MapToViewModels_DecryptsContentUsingProvidedCipher()
    {
        var cipher = new PlaintextMessageCipher();
        var payload = cipher.Encrypt("Xin chào các bạn!", key: "");
        var messages = new List<Message>
        {
            new() { SenderId = "a", ReceiverId = "b", CipherText = payload.CipherText, Iv = payload.Iv, Tag = payload.Tag, Algorithm = payload.Algorithm, SentAtUtc = DateTime.UtcNow }
        };

        var result = ChatHistoryMapper.MapToViewModels(messages, cipher, currentUserId: "a");

        Assert.Equal("Xin chào các bạn!", result[0].Content);
    }
}
```

- [ ] **Step 2: Chạy test, xác nhận thất bại**

Run: `dotnet test tests/HaloChat.Web.Tests`
Expected: FAIL — thiếu `MessageViewModel`/`ChatHistoryMapper`.

- [ ] **Step 3: Cài đặt tối thiểu**

Tạo `src/HaloChat.Web/ViewModels/MessageViewModel.cs`:

```csharp
namespace HaloChat.Web.ViewModels;

public class MessageViewModel
{
    public string Content { get; set; } = default!;
    public bool IsMine { get; set; }
    public DateTime SentAtUtc { get; set; }
}
```

Tạo `src/HaloChat.Web/Services/ChatHistoryMapper.cs`:

```csharp
using HaloChat.Security;
using HaloChat.Web.Models;
using HaloChat.Web.ViewModels;

namespace HaloChat.Web.Services;

public static class ChatHistoryMapper
{
    public static List<MessageViewModel> MapToViewModels(
        IEnumerable<Message> messages,
        IMessageCipher cipher,
        string currentUserId)
    {
        return messages
            .OrderBy(m => m.SentAtUtc)
            .Select(m => new MessageViewModel
            {
                // key rỗng — placeholder, nhóm bảo mật sẽ thay bằng khóa thật khi cắm AES vào
                Content = cipher.Decrypt(new EncryptedPayload(m.CipherText, m.Iv, m.Tag, m.Algorithm), key: string.Empty),
                IsMine = m.SenderId == currentUserId,
                SentAtUtc = m.SentAtUtc
            })
            .ToList();
    }
}
```

- [ ] **Step 4: Chạy lại test, xác nhận pass**

Run: `dotnet test tests/HaloChat.Web.Tests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/HaloChat.Web/ViewModels src/HaloChat.Web/Services/ChatHistoryMapper.cs tests/HaloChat.Web.Tests/ChatHistoryMapperTests.cs
git commit -m "$(cat <<'EOF'
Thêm ChatHistoryMapper: map Message -> MessageViewModel kèm giải mã

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 7: IUserPresenceTracker (TDD)

**Files:**
- Create: `src/HaloChat.Web/Services/IUserPresenceTracker.cs`
- Create: `src/HaloChat.Web/Services/InMemoryUserPresenceTracker.cs`
- Test: `tests/HaloChat.Web.Tests/InMemoryUserPresenceTrackerTests.cs`

**Interfaces:**
- Consumes: không có
- Produces: `IUserPresenceTracker { bool AddConnection(string userId); bool RemoveConnection(string userId); IReadOnlyCollection<string> GetOnlineUserIds(); }`,
  `class InMemoryUserPresenceTracker : IUserPresenceTracker` — dùng bởi Task 8 (ChatHub)

- [ ] **Step 1: Viết test thất bại**

Tạo `tests/HaloChat.Web.Tests/InMemoryUserPresenceTrackerTests.cs`:

```csharp
using HaloChat.Web.Services;
using Xunit;

namespace HaloChat.Web.Tests;

public class InMemoryUserPresenceTrackerTests
{
    [Fact]
    public void AddConnection_FirstConnectionForUser_ReturnsTrue()
    {
        var tracker = new InMemoryUserPresenceTracker();

        var wentOnline = tracker.AddConnection("user-1");

        Assert.True(wentOnline);
    }

    [Fact]
    public void AddConnection_SecondConnectionForSameUser_ReturnsFalse()
    {
        var tracker = new InMemoryUserPresenceTracker();
        tracker.AddConnection("user-1");

        var wentOnline = tracker.AddConnection("user-1");

        Assert.False(wentOnline);
    }

    [Fact]
    public void RemoveConnection_LastConnectionForUser_ReturnsTrue()
    {
        var tracker = new InMemoryUserPresenceTracker();
        tracker.AddConnection("user-1");

        var wentOffline = tracker.RemoveConnection("user-1");

        Assert.True(wentOffline);
    }

    [Fact]
    public void RemoveConnection_WhenTwoTabsOpen_OnlyReturnsTrueAfterBoth()
    {
        var tracker = new InMemoryUserPresenceTracker();
        tracker.AddConnection("user-1");
        tracker.AddConnection("user-1"); // 2 tab

        var afterFirstClose = tracker.RemoveConnection("user-1");
        var afterSecondClose = tracker.RemoveConnection("user-1");

        Assert.False(afterFirstClose);
        Assert.True(afterSecondClose);
    }

    [Fact]
    public void RemoveConnection_UserNeverAdded_ReturnsFalse()
    {
        var tracker = new InMemoryUserPresenceTracker();

        var result = tracker.RemoveConnection("ghost-user");

        Assert.False(result);
    }

    [Fact]
    public void GetOnlineUserIds_ReflectsCurrentlyConnectedUsers()
    {
        var tracker = new InMemoryUserPresenceTracker();
        tracker.AddConnection("user-1");
        tracker.AddConnection("user-2");
        tracker.RemoveConnection("user-2");

        var online = tracker.GetOnlineUserIds();

        Assert.Single(online);
        Assert.Contains("user-1", online);
    }
}
```

- [ ] **Step 2: Chạy test, xác nhận thất bại**

Run: `dotnet test tests/HaloChat.Web.Tests`
Expected: FAIL — thiếu `InMemoryUserPresenceTracker`.

- [ ] **Step 3: Cài đặt tối thiểu**

Tạo `src/HaloChat.Web/Services/IUserPresenceTracker.cs`:

```csharp
namespace HaloChat.Web.Services;

public interface IUserPresenceTracker
{
    /// <returns>true nếu đây là kết nối đầu tiên của user này (user vừa online)</returns>
    bool AddConnection(string userId);

    /// <returns>true nếu đây là kết nối cuối cùng của user này (user vừa offline)</returns>
    bool RemoveConnection(string userId);

    IReadOnlyCollection<string> GetOnlineUserIds();
}
```

Tạo `src/HaloChat.Web/Services/InMemoryUserPresenceTracker.cs`:

```csharp
using System.Collections.Concurrent;

namespace HaloChat.Web.Services;

public class InMemoryUserPresenceTracker : IUserPresenceTracker
{
    private readonly ConcurrentDictionary<string, int> _connectionCounts = new();

    public bool AddConnection(string userId)
    {
        var newCount = _connectionCounts.AddOrUpdate(userId, 1, (_, count) => count + 1);
        return newCount == 1;
    }

    public bool RemoveConnection(string userId)
    {
        while (_connectionCounts.TryGetValue(userId, out var count))
        {
            var newCount = count - 1;
            if (newCount <= 0)
            {
                if (_connectionCounts.TryRemove(new KeyValuePair<string, int>(userId, count)))
                {
                    return true;
                }
            }
            else if (_connectionCounts.TryUpdate(userId, newCount, count))
            {
                return false;
            }
        }
        return false;
    }

    public IReadOnlyCollection<string> GetOnlineUserIds()
        => _connectionCounts.Keys.ToList();
}
```

- [ ] **Step 4: Chạy lại test, xác nhận pass**

Run: `dotnet test tests/HaloChat.Web.Tests`
Expected: PASS — 6/6 test thành công.

- [ ] **Step 5: Commit**

```bash
git add src/HaloChat.Web/Services/IUserPresenceTracker.cs src/HaloChat.Web/Services/InMemoryUserPresenceTracker.cs tests/HaloChat.Web.Tests/InMemoryUserPresenceTrackerTests.cs
git commit -m "$(cat <<'EOF'
Thêm IUserPresenceTracker: theo dõi trạng thái online/offline theo số connection

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 8: ChatHub (SignalR) + wiring Program.cs

**Files:**
- Create: `src/HaloChat.Web/Hubs/ChatHub.cs`
- Modify: `src/HaloChat.Web/Program.cs`

**Interfaces:**
- Consumes: `IMessageCipher` (Task 4), `ApplicationDbContext.Messages` (Task 5), `IUserPresenceTracker` (Task 7), `UserManager<ApplicationUser>` (Identity có sẵn)
- Produces: hub endpoint `/chatHub` với các method client gọi được: `SendMessage(string receiverId, string content)`, `GetOnlineUsers()`; event server đẩy xuống client: `ReceiveMessage`, `PresenceChanged`, `SendFailed` — dùng bởi Task 10 (chat.js)

- [ ] **Step 1: Tạo ChatHub**

Tạo `src/HaloChat.Web/Hubs/ChatHub.cs`:

```csharp
using HaloChat.Security;
using HaloChat.Web.Data;
using HaloChat.Web.Models;
using HaloChat.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;

namespace HaloChat.Web.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly ApplicationDbContext _db;
    private readonly IMessageCipher _cipher;
    private readonly IUserPresenceTracker _presence;
    private readonly UserManager<ApplicationUser> _userManager;

    public ChatHub(
        ApplicationDbContext db,
        IMessageCipher cipher,
        IUserPresenceTracker presence,
        UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _cipher = cipher;
        _presence = presence;
        _userManager = userManager;
    }

    private string CurrentUserId => Context.UserIdentifier
        ?? throw new InvalidOperationException("Kết nối SignalR không có UserIdentifier.");

    public override async Task OnConnectedAsync()
    {
        var becameOnline = _presence.AddConnection(CurrentUserId);
        if (becameOnline)
        {
            await Clients.All.SendAsync("PresenceChanged", CurrentUserId, true);
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var becameOffline = _presence.RemoveConnection(CurrentUserId);
        if (becameOffline)
        {
            await Clients.All.SendAsync("PresenceChanged", CurrentUserId, false);
        }
        await base.OnDisconnectedAsync(exception);
    }

    public IEnumerable<string> GetOnlineUsers() => _presence.GetOnlineUserIds();

    public async Task SendMessage(string receiverId, string content)
    {
        try
        {
            var senderId = CurrentUserId;

            if (string.IsNullOrWhiteSpace(content))
            {
                throw new InvalidOperationException("Tin nhắn không được để trống.");
            }

            if (receiverId == senderId)
            {
                throw new InvalidOperationException("Không thể tự gửi tin nhắn cho chính mình.");
            }

            var receiver = await _userManager.FindByIdAsync(receiverId);
            if (receiver is null)
            {
                throw new InvalidOperationException("Người nhận không tồn tại.");
            }

            // key rỗng — placeholder, nhóm bảo mật sẽ thay bằng khóa thật khi cắm AES vào
            var payload = _cipher.Encrypt(content, key: string.Empty);

            var message = new Message
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                CipherText = payload.CipherText,
                Iv = payload.Iv,
                Tag = payload.Tag,
                Algorithm = payload.Algorithm,
                SentAtUtc = DateTime.UtcNow
            };

            _db.Messages.Add(message);
            await _db.SaveChangesAsync();

            var outgoing = new
            {
                senderId,
                receiverId,
                content,
                sentAtUtc = message.SentAtUtc
            };

            await Clients.User(senderId).SendAsync("ReceiveMessage", outgoing);
            await Clients.User(receiverId).SendAsync("ReceiveMessage", outgoing);
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("SendFailed", ex.Message);
        }
    }
}
```

- [ ] **Step 2: Đăng ký SignalR + DI trong Program.cs**

Thêm vào đầu `src/HaloChat.Web/Program.cs` (cùng nhóm với các `using` khác):

```csharp
using HaloChat.Security;
using HaloChat.Web.Hubs;
using HaloChat.Web.Services;
```

Tìm dòng:

```csharp
builder.Services.AddControllersWithViews();
```

Thêm ngay sau dòng đó:

```csharp
builder.Services.AddSignalR();
builder.Services.AddScoped<IMessageCipher, PlaintextMessageCipher>();
builder.Services.AddSingleton<IUserPresenceTracker, InMemoryUserPresenceTracker>();
```

Tìm dòng:

```csharp
app.MapRazorPages();
```

Thêm ngay sau dòng đó:

```csharp
app.MapHub<ChatHub>("/chatHub");
```

- [ ] **Step 3: Build**

Run: `dotnet build`
Expected: `Build succeeded.`

- [ ] **Step 4: Xác minh thủ công (2 phiên đăng nhập)**

```bash
dotnet run --project src/HaloChat.Web
```

Vì chưa có UI gọi Hub (Task 9-10 chưa làm), bước này chỉ xác minh app khởi
động được và không lỗi khi có client SignalR kết nối tới `/chatHub`. Dùng
trình duyệt mở trang bất kỳ đã đăng nhập, mở DevTools Console, chạy:

```js
fetch('/chatHub/negotiate?negotiateVersion=1', { method: 'POST', credentials: 'include' })
  .then(r => r.json()).then(console.log)
```

Expected: trả về JSON có `connectionId` (không phải lỗi 401/404). Dừng
server sau khi xác minh.

- [ ] **Step 5: Commit**

```bash
git add src/HaloChat.Web/Hubs src/HaloChat.Web/Program.cs
git commit -m "$(cat <<'EOF'
Thêm ChatHub (SignalR): gửi/nhận tin nhắn realtime + presence broadcast

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 9: ChatController + Views

**Files:**
- Create: `src/HaloChat.Web/ViewModels/UserListItemViewModel.cs`
- Create: `src/HaloChat.Web/ViewModels/ConversationViewModel.cs`
- Create: `src/HaloChat.Web/Controllers/ChatController.cs`
- Create: `src/HaloChat.Web/Views/Chat/Index.cshtml`
- Create: `src/HaloChat.Web/Views/Chat/Conversation.cshtml`

**Interfaces:**
- Consumes: `ApplicationDbContext` (Task 5), `IMessageCipher` (Task 4), `ChatHistoryMapper.MapToViewModels` (Task 6), `UserManager<ApplicationUser>`
- Produces: route `GET /Chat/Index` (danh sách người dùng), `GET /Chat/Conversation/{id}` (khung chat 1–1) — dùng bởi Task 10 (chat.js đọc `data-user-id`/`data-other-user-id` từ các view này)

- [ ] **Step 1: Tạo ViewModels**

Tạo `src/HaloChat.Web/ViewModels/UserListItemViewModel.cs`:

```csharp
namespace HaloChat.Web.ViewModels;

public class UserListItemViewModel
{
    public string Id { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
}
```

Tạo `src/HaloChat.Web/ViewModels/ConversationViewModel.cs`:

```csharp
namespace HaloChat.Web.ViewModels;

public class ConversationViewModel
{
    public string OtherUserId { get; set; } = default!;
    public string OtherUserDisplayName { get; set; } = default!;
    public List<MessageViewModel> Messages { get; set; } = new();
}
```

- [ ] **Step 2: Tạo ChatController**

Tạo `src/HaloChat.Web/Controllers/ChatController.cs`:

```csharp
using HaloChat.Security;
using HaloChat.Web.Data;
using HaloChat.Web.Models;
using HaloChat.Web.Services;
using HaloChat.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HaloChat.Web.Controllers;

[Authorize]
public class ChatController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IMessageCipher _cipher;
    private readonly UserManager<ApplicationUser> _userManager;

    public ChatController(ApplicationDbContext db, IMessageCipher cipher, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _cipher = cipher;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var currentUserId = _userManager.GetUserId(User);

        var users = await _db.Users
            .Where(u => u.Id != currentUserId)
            .Select(u => new UserListItemViewModel
            {
                Id = u.Id,
                DisplayName = u.UserName ?? u.Email ?? u.Id
            })
            .ToListAsync();

        return View(users);
    }

    public async Task<IActionResult> Conversation(string id)
    {
        var currentUserId = _userManager.GetUserId(User)!;

        if (string.IsNullOrEmpty(id) || id == currentUserId)
        {
            return NotFound();
        }

        var otherUser = await _userManager.FindByIdAsync(id);
        if (otherUser is null)
        {
            return NotFound();
        }

        var messages = await _db.Messages
            .Where(m =>
                (m.SenderId == currentUserId && m.ReceiverId == id) ||
                (m.SenderId == id && m.ReceiverId == currentUserId))
            .ToListAsync();

        var viewModel = new ConversationViewModel
        {
            OtherUserId = otherUser.Id,
            OtherUserDisplayName = otherUser.UserName ?? otherUser.Email ?? otherUser.Id,
            Messages = ChatHistoryMapper.MapToViewModels(messages, _cipher, currentUserId)
        };

        return View(viewModel);
    }
}
```

- [ ] **Step 3: Tạo Views**

Tạo `src/HaloChat.Web/Views/Chat/Index.cshtml`:

```html
@model List<HaloChat.Web.ViewModels.UserListItemViewModel>
@{
    ViewData["Title"] = "Danh sách người dùng";
}

<h1>Danh sách người dùng</h1>

<ul id="user-list" class="list-group">
    @foreach (var user in Model)
    {
        <li class="list-group-item d-flex justify-content-between align-items-center" data-user-id="@user.Id">
            <a asp-action="Conversation" asp-route-id="@user.Id">@user.DisplayName</a>
            <span class="presence-dot offline" title="offline">●</span>
        </li>
    }
</ul>

@section Scripts {
    <script src="https://cdn.jsdelivr.net/npm/@@microsoft/signalr@8.0.0/dist/browser/signalr.min.js"></script>
    <script src="~/js/chat.js"></script>
}
```

Tạo `src/HaloChat.Web/Views/Chat/Conversation.cshtml`:

```html
@model HaloChat.Web.ViewModels.ConversationViewModel
@{
    ViewData["Title"] = $"Chat với {Model.OtherUserDisplayName}";
}

<h1>Chat với @Model.OtherUserDisplayName</h1>

<div id="message-list" data-other-user-id="@Model.OtherUserId">
    @foreach (var message in Model.Messages)
    {
        <div class="message @(message.IsMine ? "message-mine" : "message-theirs")">
            <span class="message-content">@message.Content</span>
            <span class="message-time">@message.SentAtUtc.ToLocalTime().ToString("HH:mm dd/MM")</span>
        </div>
    }
</div>

<div id="send-error" class="text-danger" style="display:none;"></div>

<form id="send-form">
    <input type="text" id="message-input" autocomplete="off" placeholder="Nhập tin nhắn..." />
    <button type="submit">Gửi</button>
</form>

@section Scripts {
    <script src="https://cdn.jsdelivr.net/npm/@@microsoft/signalr@8.0.0/dist/browser/signalr.min.js"></script>
    <script src="~/js/chat.js"></script>
}
```

- [ ] **Step 4: Build**

Run: `dotnet build`
Expected: `Build succeeded.`

- [ ] **Step 5: Commit**

```bash
git add src/HaloChat.Web/ViewModels src/HaloChat.Web/Controllers/ChatController.cs src/HaloChat.Web/Views/Chat
git commit -m "$(cat <<'EOF'
Thêm ChatController + views danh sách người dùng và khung chat 1-1

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 10: wwwroot/js/chat.js + CSS + điều hướng + xác minh end-to-end

**Files:**
- Create: `src/HaloChat.Web/wwwroot/js/chat.js`
- Modify: `src/HaloChat.Web/wwwroot/css/site.css`
- Modify: `src/HaloChat.Web/Views/Shared/_Layout.cshtml`
- Copy: `assets/halochat-logo.png` → `src/HaloChat.Web/wwwroot/img/halochat-logo.png`

**Interfaces:**
- Consumes: hub events/method từ Task 8 (`ReceiveMessage`, `PresenceChanged`, `SendFailed`, `SendMessage`, `GetOnlineUsers`), DOM elements từ Task 9 (`#user-list`, `#message-list[data-other-user-id]`, `#send-form`, `#message-input`, `#send-error`), file logo có sẵn tại `assets/halochat-logo.png` (repo root)
- Produces: khung web chạy được end-to-end (không có task nào phụ thuộc thêm)

- [ ] **Step 1: Tạo chat.js**

Tạo `src/HaloChat.Web/wwwroot/js/chat.js`:

```javascript
"use strict";

(function () {
    const userListEl = document.getElementById("user-list");
    const messageListEl = document.getElementById("message-list");

    if (!userListEl && !messageListEl) {
        return; // trang này không cần SignalR
    }

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/chatHub")
        .withAutomaticReconnect()
        .build();

    function setPresenceDot(userId, isOnline) {
        const dot = document.querySelector(`li[data-user-id="${userId}"] .presence-dot`);
        if (dot) {
            dot.title = isOnline ? "online" : "offline";
            dot.classList.toggle("online", isOnline);
            dot.classList.toggle("offline", !isOnline);
        }
    }

    connection.on("PresenceChanged", function (userId, isOnline) {
        setPresenceDot(userId, isOnline);
    });

    if (messageListEl) {
        const otherUserId = messageListEl.dataset.otherUserId;
        const sendForm = document.getElementById("send-form");
        const messageInput = document.getElementById("message-input");
        const sendErrorEl = document.getElementById("send-error");

        connection.on("ReceiveMessage", function (message) {
            const belongsToThisConversation =
                message.senderId === otherUserId || message.receiverId === otherUserId;
            if (!belongsToThisConversation) {
                return;
            }

            const isMine = message.receiverId === otherUserId;
            const div = document.createElement("div");
            div.className = "message " + (isMine ? "message-mine" : "message-theirs");

            const contentSpan = document.createElement("span");
            contentSpan.className = "message-content";
            contentSpan.textContent = message.content;

            const timeSpan = document.createElement("span");
            timeSpan.className = "message-time";
            timeSpan.textContent = new Date(message.sentAtUtc).toLocaleString();

            div.appendChild(contentSpan);
            div.appendChild(timeSpan);
            messageListEl.appendChild(div);
        });

        connection.on("SendFailed", function (errorMessage) {
            sendErrorEl.textContent = "Gửi thất bại: " + errorMessage;
            sendErrorEl.style.display = "block";
        });

        sendForm.addEventListener("submit", function (event) {
            event.preventDefault();
            const content = messageInput.value.trim();
            if (!content) {
                return;
            }
            sendErrorEl.style.display = "none";
            connection.invoke("SendMessage", otherUserId, content).catch(function (err) {
                sendErrorEl.textContent = "Lỗi kết nối: " + err.message;
                sendErrorEl.style.display = "block";
            });
            messageInput.value = "";
        });
    }

    connection.start()
        .then(function () {
            if (userListEl) {
                return connection.invoke("GetOnlineUsers").then(function (onlineUserIds) {
                    onlineUserIds.forEach(function (userId) {
                        setPresenceDot(userId, true);
                    });
                });
            }
        })
        .catch(function (err) {
            console.error("Không thể kết nối SignalR:", err);
        });
})();
```

- [ ] **Step 2: Thêm CSS cho presence dot và bong bóng chat**

Thêm vào cuối `src/HaloChat.Web/wwwroot/css/site.css`:

```css
.presence-dot { color: #adb5bd; font-size: 0.8rem; }
.presence-dot.online { color: #28a745; }
.presence-dot.offline { color: #adb5bd; }
.message { margin-bottom: 0.5rem; }
.message-mine { text-align: right; }
.message-theirs { text-align: left; }
.message-time { font-size: 0.75rem; color: #6c757d; margin-left: 0.5rem; }
```

- [ ] **Step 3: Thêm link điều hướng tới Chat**

Trong `src/HaloChat.Web/Views/Shared/_Layout.cshtml`, tìm đoạn chứa:

```html
<a class="nav-link text-dark" asp-area="" asp-controller="Home" asp-action="Privacy">Privacy</a>
```

Thêm ngay sau thẻ `</li>` đóng mục Privacy đó:

```html
@if (User.Identity?.IsAuthenticated == true)
{
    <li class="nav-item">
        <a class="nav-link text-dark" asp-area="" asp-controller="Chat" asp-action="Index">Chat</a>
    </li>
}
```

- [ ] **Step 4: Gắn logo HaloChat vào layout**

Copy file logo có sẵn ở gốc repo vào wwwroot:

```bash
mkdir -p src/HaloChat.Web/wwwroot/img
cp assets/halochat-logo.png src/HaloChat.Web/wwwroot/img/halochat-logo.png
```

Trong `src/HaloChat.Web/Views/Shared/_Layout.cshtml`, tìm dòng:

```html
<link rel="stylesheet" href="~/css/site.css" asp-append-version="true" />
```

Thêm ngay sau dòng đó (favicon):

```html
<link rel="icon" type="image/png" href="~/img/halochat-logo.png" />
```

Tìm thẻ liên kết có class `navbar-brand` (dạng
`<a class="navbar-brand" asp-area="" asp-controller="Home" asp-action="Index">...</a>`,
nội dung chữ bên trong có thể là tên project mặc định) và thay toàn bộ nội
dung bên trong thẻ `<a>` đó bằng:

```html
<img src="~/img/halochat-logo.png" alt="HaloChat" height="28" class="d-inline-block align-text-top" />
HaloChat
```

(Giữ nguyên các thuộc tính `class="navbar-brand" asp-area="" asp-controller="Home" asp-action="Index"` của thẻ `<a>`, chỉ đổi nội dung bên trong.)

- [ ] **Step 5: Build**

Run: `dotnet build`
Expected: `Build succeeded.`

- [ ] **Step 6: Xác minh thủ công end-to-end (2 người dùng)**

```bash
dotnet run --project src/HaloChat.Web
```

1. Mở 2 cửa sổ trình duyệt khác nhau (một cửa sổ ẩn danh) trỏ tới URL app.
2. Xác nhận logo HaloChat hiện ở góc trên bên trái (navbar brand) và ở tab
   trình duyệt (favicon).
3. Đăng ký 2 tài khoản khác nhau (User A ở cửa sổ 1, User B ở cửa sổ 2).
4. Ở mỗi cửa sổ, vào menu "Chat" — xác nhận thấy tên người dùng còn lại
   trong danh sách, chấm presence chuyển xanh (online) cho cả hai.
5. Click vào nhau để mở khung chat, gõ tin nhắn ở User A, bấm Gửi.
6. Xác nhận: tin nhắn xuất hiện ngay ở cả 2 cửa sổ mà không cần tải lại
   trang (đúng bên trái/phải theo `IsMine`).
7. Tải lại trang chat ở cả 2 cửa sổ — xác nhận lịch sử tin nhắn hiển thị lại
   đúng thứ tự.
8. Đóng một cửa sổ — xác nhận chấm presence của user đó chuyển xám ở cửa sổ
   còn lại (có thể mất vài giây do SignalR phát hiện disconnect).
9. Dừng server (Ctrl+C).

- [ ] **Step 7: Commit**

```bash
git add src/HaloChat.Web/wwwroot src/HaloChat.Web/Views/Shared/_Layout.cshtml
git commit -m "$(cat <<'EOF'
Thêm SignalR client (chat.js), CSS presence/bong bóng chat, điều hướng Chat

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 11: Cập nhật CLAUDE.md với lệnh thật + xác minh cuối cùng toàn solution

**Files:**
- Modify: `CLAUDE.md`

**Interfaces:**
- Consumes: toàn bộ solution đã hoàn thiện từ Task 1–10
- Produces: tài liệu vận hành chính xác cho phiên Claude Code sau này

- [ ] **Step 1: Chạy build + test toàn solution**

Run: `dotnet build && dotnet test`
Expected: build thành công; các test ở `HaloChat.Security.Tests` và
`HaloChat.Web.Tests` (cipher, mapper, presence tracker, message repository)
đều PASS.

- [ ] **Step 2: Cập nhật phần "Lệnh phát triển" trong CLAUDE.md**

Trong `CLAUDE.md`, tìm khối:

```
## Lệnh phát triển (áp dụng sau khi solution được scaffold theo spec)
```

Thay toàn bộ khối code phía dưới heading đó bằng:

```bash
dotnet build                                        # build toàn bộ solution
dotnet test                                         # chạy toàn bộ test (Security + Web)
dotnet run --project src/HaloChat.Web                # chạy web app (cần SQL Server LocalDB)
dotnet ef migrations add <Tên> -p src/HaloChat.Web -s src/HaloChat.Web   # thêm migration
dotnet ef database update -p src/HaloChat.Web -s src/HaloChat.Web       # áp migration vào DB
```

Đồng thời sửa heading thành:

```
## Lệnh phát triển
```

(bỏ phần "(áp dụng sau khi...)" vì solution đã tồn tại thật.)

- [ ] **Step 3: Cập nhật phần "Trạng thái hiện tại"**

Xóa đoạn "Repo này chưa có code..." ở đầu file, thay bằng:

```markdown
## Trạng thái hiện tại

Khung web (giai đoạn 1) đã hoàn thiện: đăng ký/đăng nhập, danh sách người
dùng, chat 1–1 thời gian thực, lưu lịch sử, presence online/offline, và
interface `IMessageCipher` sẵn sàng để nhóm bảo mật cắm AES thật vào (xem
`docs/superpowers/specs/2026-09-06-chat-aes-web-scaffold-design.md`).
```

- [ ] **Step 4: Commit**

```bash
git add CLAUDE.md
git commit -m "$(cat <<'EOF'
Cập nhật CLAUDE.md với lệnh dev thật và trạng thái khung web đã hoàn thiện

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```
