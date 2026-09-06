# Thiết kế giai đoạn 2 — Mã hóa AES thật cho HaloChat

**Ngày:** 2026-09-06
**Vai trò của phiên làm việc này:** tư vấn + viết spec/code mẫu. Nhóm bảo mật
tự tay viết lại các file trong mục "Việc nhóm bảo mật cần làm" để hiểu rõ
từng dòng, không copy-paste nguyên xi.

**Đọc trước:**
`docs/superpowers/specs/2026-09-06-chat-aes-web-scaffold-design.md` (giai
đoạn 1 — khung web, kiến trúc `IMessageCipher`).

## 1. Phạm vi

Tài liệu này thiết kế 2 phần độc lập, dùng chung `HaloChat.Security`:

1. **`AesMessageCipher`** — cài đặt thật của `IMessageCipher`, cắm vào
   `HaloChat.Web` để mã hóa tin nhắn chat thực tế.
2. **`HaloChat.Benchmark`** — console app riêng, đo thời gian mã hóa/giải mã
   và kích thước ciphertext cho AES-128/192/256 ở 4 mốc dữ liệu
   (100B/1KB/10KB/100KB), phục vụ phần đánh giá của báo cáo môn học.

Phần "khung nối dây" phía web (interface `IConversationKeyProvider`,
sửa `ChatHub`/`ChatController`/`ChatHistoryMapper` để truyền khóa thật thay
vì `string.Empty`, đăng ký DI) đã được cài đặt sẵn trong repo (commit
`de98fbc`) — nhóm bảo mật **không cần sửa 3 file đó nữa**, chỉ cần đổi 2 dòng
đăng ký DI trong `Program.cs` (mục 5) khi cắm cài đặt thật vào.

Các quyết định đã chốt (không mở lại):

| Quyết định | Lựa chọn |
|---|---|
| Chế độ AES | GCM (authenticated encryption, không cần padding) |
| Quản lý khóa | Đơn giản: sinh khóa bằng KDF từ 1 bí mật dùng chung (pepper cấu hình sẵn) + cặp UserId — không trao đổi khóa qua mạng |
| Kích thước khóa cho chat thật | Cấu hình được (`appsettings`), không hard-code 1 loại |
| Benchmark | Đo cả 3 biến thể 128/192/256, độc lập ngoài web |

## 2. Kiến trúc & luồng dữ liệu

```
ChatHub.SendMessage / ChatController.Conversation
        │
        │  key = keyProvider.GetKey(userIdA, userIdB)
        ▼
IConversationKeyProvider  (HkdfConversationKeyProvider ở giai đoạn 2)
        │  trả về 1 khóa Base64, cố định cho mỗi CẶP người dùng
        ▼
IMessageCipher.Encrypt(plaintext, key) / Decrypt(payload, key)
        │
        ▼
AesMessageCipher  →  EncryptedPayload(CipherText, Iv, Tag, Algorithm)
        │
        ▼
cột CipherText/Iv/Tag/Algorithm trong bảng Message (đã có sẵn từ giai đoạn 1)
```

Mỗi cặp người dùng có 1 khóa riêng (không phải 1 khóa chung cho toàn app) —
đây là lý do `IConversationKeyProvider` cần biết 2 UserId, khác với
`IMessageCipher` (chỉ biết nội dung + khóa, không biết ai đang chat với ai).

## 3. Sinh khóa — HKDF từ pepper + cặp UserId

Dùng **HKDF** (RFC 5869, có sẵn trong .NET: `System.Security.Cryptography.HKDF`)
thay vì PBKDF2. Lý do: PBKDF2 dùng để làm chậm brute-force cho *mật khẩu
người dùng* (entropy thấp); ở đây bí mật gốc là 1 pepper cấu hình sẵn trên
server (entropy cao, không phải mật khẩu) — HKDF là công cụ đúng mục đích để
"nở" 1 bí mật gốc thành nhiều khóa độc lập theo từng ngữ cảnh (ở đây: từng
cặp UserId, qua tham số `info`).

```csharp
// src/HaloChat.Security/AesKeyOptions.cs
namespace HaloChat.Security;

public class AesKeyOptions
{
    /// <summary>
    /// Bí mật dùng chung (pepper) — nguồn duy nhất mà mọi khóa cuộc trò chuyện
    /// được sinh ra từ đó. KHÔNG commit giá trị thật vào git.
    /// Dev: dotnet user-secrets set "Aes:Pepper" "..." --project src/HaloChat.Web
    /// Production: biến môi trường Aes__Pepper hoặc secret store của hạ tầng triển khai.
    /// </summary>
    public string Pepper { get; set; } = string.Empty;

    /// <summary>Kích thước khóa AES dùng cho chat thật: 128 / 192 / 256.</summary>
    public int KeySizeBits { get; set; } = 256;
}
```

```csharp
// src/HaloChat.Security/HkdfConversationKeyProvider.cs
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace HaloChat.Security;

public class HkdfConversationKeyProvider : IConversationKeyProvider
{
    private readonly byte[] _pepper;
    private readonly int _keySizeBytes;

    public HkdfConversationKeyProvider(IOptions<AesKeyOptions> options)
    {
        var config = options.Value;
        if (string.IsNullOrWhiteSpace(config.Pepper))
        {
            throw new InvalidOperationException(
                "Chưa cấu hình Aes:Pepper. Xem hướng dẫn dotnet user-secrets trong spec giai đoạn 2.");
        }
        if (config.KeySizeBits is not (128 or 192 or 256))
        {
            throw new InvalidOperationException("Aes:KeySizeBits phải là 128, 192 hoặc 256.");
        }

        _pepper = Encoding.UTF8.GetBytes(config.Pepper);
        _keySizeBytes = config.KeySizeBits / 8;
    }

    public string GetKey(string userIdA, string userIdB)
    {
        // Sắp thứ tự cố định để GetKey(a, b) == GetKey(b, a).
        var (first, second) = string.CompareOrdinal(userIdA, userIdB) <= 0
            ? (userIdA, userIdB)
            : (userIdB, userIdA);

        var info = Encoding.UTF8.GetBytes($"halochat-conversation:{first}:{second}");
        var keyBytes = HKDF.DeriveKey(HashAlgorithmName.SHA256, _pepper, _keySizeBytes, info: info);
        return Convert.ToBase64String(keyBytes);
    }
}
```

Khóa **không cần lưu vào DB** — được tính lại từ pepper + cặp UserId mỗi khi
cần, nên không có bài toán "lưu khóa ở đâu cho an toàn" (đổi lại: ai biết
pepper thì tính lại được mọi khóa — xem mục 8, giới hạn của mô hình).

## 4. AesMessageCipher — AES-GCM

```csharp
// src/HaloChat.Security/AesMessageCipher.cs
using System.Security.Cryptography;
using System.Text;

namespace HaloChat.Security;

public class AesMessageCipher : IMessageCipher
{
    private const int NonceSizeBytes = 12; // 96-bit — khuyến nghị chuẩn cho GCM
    private const int TagSizeBytes = 16;   // 128-bit — mức cao nhất AesGcm hỗ trợ

    public EncryptedPayload Encrypt(string plaintext, string key)
    {
        var keyBytes = Convert.FromBase64String(key);
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes); // ngẫu nhiên MỖI lần mã hóa — không được tái sử dụng
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[TagSizeBytes];

        using var aesGcm = new AesGcm(keyBytes, TagSizeBytes); // .NET 8 yêu cầu truyền tagSize tường minh
        aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        return new EncryptedPayload(ciphertext, nonce, tag, AlgorithmName(keyBytes.Length));
    }

    public string Decrypt(EncryptedPayload payload, string key)
    {
        if (payload.Iv is null || payload.Tag is null)
        {
            throw new InvalidOperationException("Thiếu IV/Tag — không thể giải mã bằng AES-GCM.");
        }

        var keyBytes = Convert.FromBase64String(key);
        var plaintextBytes = new byte[payload.CipherText.Length];

        using var aesGcm = new AesGcm(keyBytes, TagSizeBytes);
        aesGcm.Decrypt(payload.Iv, payload.CipherText, payload.Tag, plaintextBytes); // ném CryptographicException nếu tag sai (dữ liệu bị sửa hoặc sai khóa)

        return Encoding.UTF8.GetString(plaintextBytes);
    }

    private static string AlgorithmName(int keyLengthBytes) => keyLengthBytes * 8 switch
    {
        128 => "AES-128-GCM",
        192 => "AES-192-GCM",
        256 => "AES-256-GCM",
        _ => throw new ArgumentException($"Độ dài khóa AES không hợp lệ: {keyLengthBytes * 8} bit.")
    };
}
```

Không cần padding (đặc điểm của GCM — ciphertext luôn dài bằng plaintext).
`CryptographicException` khi tag sai đã được `ChatHistoryMapper` xử lý sẵn từ
giai đoạn 1 (trả về `"[không giải mã được]"` thay vì crash trang) — một tin
nhắn cũ mã hóa bằng khóa/thuật toán khác sẽ hiện placeholder đó thay vì lỗi.

## 5. Thay đổi cần làm trong `Program.cs`

Chỉ 2 dòng đăng ký DI (thay vì sửa `ChatHub`/`ChatController` — đã làm sẵn):

```csharp
// Trước (giai đoạn 1 / placeholder):
builder.Services.AddScoped<IMessageCipher, PlaintextMessageCipher>();
builder.Services.AddSingleton<IConversationKeyProvider, NullConversationKeyProvider>();

// Sau (giai đoạn 2 / AES thật):
builder.Services.Configure<AesKeyOptions>(builder.Configuration.GetSection("Aes"));
builder.Services.AddScoped<IMessageCipher, AesMessageCipher>();
builder.Services.AddSingleton<IConversationKeyProvider, HkdfConversationKeyProvider>();
```

## 6. Cấu hình pepper (`appsettings.json`)

```json
{
  "Aes": {
    "Pepper": "",
    "KeySizeBits": 256
  }
}
```

Để trống `Pepper` trong file commit vào git. Khi chạy dev, set giá trị thật
qua User Secrets (không vào git, không vào appsettings):

```bash
dotnet user-secrets init --project src/HaloChat.Web
dotnet user-secrets set "Aes:Pepper" "<chuỗi bí mật dài, ngẫu nhiên>" --project src/HaloChat.Web
```

Production: đặt biến môi trường `Aes__Pepper` (2 dấu gạch dưới — quy ước
binding cấu hình của ASP.NET Core) hoặc secret store của nơi triển khai.

## 7. `HaloChat.Benchmark` — đo AES-128/192/256

Console app riêng, **không đụng vào web app**, chỉ tham chiếu
`HaloChat.Security`.

```
src/HaloChat.Benchmark/
├─ HaloChat.Benchmark.csproj
└─ Program.cs
```

```xml
<!-- src/HaloChat.Benchmark/HaloChat.Benchmark.csproj -->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\HaloChat.Security\HaloChat.Security.csproj" />
  </ItemGroup>

</Project>
```

Thêm vào solution: `dotnet sln HaloChat.sln add src/HaloChat.Benchmark/HaloChat.Benchmark.csproj`.

```csharp
// src/HaloChat.Benchmark/Program.cs
using System.Diagnostics;
using System.Security.Cryptography;
using HaloChat.Security;

const int WarmupIterations = 20;
const int MeasuredIterations = 200;
const int NonceAndTagOverheadBytes = 12 + 16; // IV + Tag lưu riêng trong DB, không nằm trong CipherText

var messageSizes = new (string Label, int Bytes)[]
{
    ("100 B", 100),
    ("1 KB", 1024),
    ("10 KB", 10 * 1024),
    ("100 KB", 100 * 1024),
};
var keySizesBits = new[] { 128, 192, 256 };

var cipher = new AesMessageCipher();
var results = new List<BenchmarkResult>();

foreach (var keySizeBits in keySizesBits)
{
    var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(keySizeBits / 8));

    foreach (var (label, sizeBytes) in messageSizes)
    {
        var plaintext = RandomAsciiString(sizeBytes);

        for (var i = 0; i < WarmupIterations; i++) // JIT/cache warm-up — không tính vào kết quả
        {
            cipher.Decrypt(cipher.Encrypt(plaintext, key), key);
        }

        var encryptMs = new double[MeasuredIterations];
        var decryptMs = new double[MeasuredIterations];
        EncryptedPayload? lastPayload = null;
        var sw = new Stopwatch();

        for (var i = 0; i < MeasuredIterations; i++)
        {
            sw.Restart();
            lastPayload = cipher.Encrypt(plaintext, key);
            sw.Stop();
            encryptMs[i] = sw.Elapsed.TotalMilliseconds;

            sw.Restart();
            cipher.Decrypt(lastPayload, key);
            sw.Stop();
            decryptMs[i] = sw.Elapsed.TotalMilliseconds;
        }

        results.Add(new BenchmarkResult(
            keySizeBits, label,
            PlaintextBytes: sizeBytes,
            CiphertextBytes: lastPayload!.CipherText.Length,
            TotalStoredBytes: lastPayload.CipherText.Length + NonceAndTagOverheadBytes,
            AvgEncryptMs: encryptMs.Average(),
            AvgDecryptMs: decryptMs.Average()));
    }
}

PrintTable(results);
WriteCsv(results, "benchmark-results.csv");

static string RandomAsciiString(int lengthBytes)
{
    const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    var randomBytes = RandomNumberGenerator.GetBytes(lengthBytes);
    var chars = new char[lengthBytes];
    for (var i = 0; i < lengthBytes; i++)
    {
        chars[i] = alphabet[randomBytes[i] % alphabet.Length]; // mỗi ký tự ASCII = đúng 1 byte UTF-8, đảm bảo kích thước chính xác
    }
    return new string(chars);
}

static void PrintTable(List<BenchmarkResult> results)
{
    Console.WriteLine($"{"AES",-8} {"Kích thước",-10} {"Plaintext(B)",-13} {"Ciphertext(B)",-14} {"+IV/Tag(B)",-11} {"Mã hóa TB(ms)",-14} {"Giải mã TB(ms)",-14}");
    foreach (var r in results)
    {
        Console.WriteLine($"{"AES-" + r.KeySizeBits,-8} {r.MessageSizeLabel,-10} {r.PlaintextBytes,-13} {r.CiphertextBytes,-14} {r.TotalStoredBytes,-11} {r.AvgEncryptMs,-14:F4} {r.AvgDecryptMs,-14:F4}");
    }
}

static void WriteCsv(List<BenchmarkResult> results, string path)
{
    using var writer = new StreamWriter(path);
    writer.WriteLine("KeySizeBits,MessageSize,PlaintextBytes,CiphertextBytes,TotalStoredBytes,AvgEncryptMs,AvgDecryptMs");
    foreach (var r in results)
    {
        writer.WriteLine($"{r.KeySizeBits},{r.MessageSizeLabel},{r.PlaintextBytes},{r.CiphertextBytes},{r.TotalStoredBytes},{r.AvgEncryptMs:F4},{r.AvgDecryptMs:F4}");
    }
    Console.WriteLine($"\nĐã ghi kết quả chi tiết vào {Path.GetFullPath(path)}");
}

record BenchmarkResult(
    int KeySizeBits, string MessageSizeLabel,
    int PlaintextBytes, int CiphertextBytes, int TotalStoredBytes,
    double AvgEncryptMs, double AvgDecryptMs);
```

Ghi chú cho báo cáo: với GCM, `CiphertextBytes` luôn **bằng** `PlaintextBytes`
(không có padding) — điểm khác biệt so với AES-CBC. Overhead thật sự nằm ở
IV (12B) + Tag (16B) lưu riêng, cột `TotalStoredBytes` thể hiện tổng dữ liệu
thực tế cần lưu/truyền cho mỗi tin nhắn — nên đưa cả 2 cột vào bảng so sánh
trong báo cáo, không chỉ mỗi `CiphertextBytes`.

Chạy: `dotnet run --project src/HaloChat.Benchmark -c Release` (dùng cấu
hình Release để số đo thời gian phản ánh đúng hiệu năng thật, không bị nhiễu
bởi code chưa tối ưu của build Debug).

## 8. Kiểm thử

`AesMessageCipher` và `HkdfConversationKeyProvider` là logic thuần (không
phụ thuộc DB/HTTP) — viết unit test theo TDD (RED trước, code sau) như style
đã dùng cho `PlaintextMessageCipher` ở giai đoạn 1.

```csharp
// tests/HaloChat.Security.Tests/AesMessageCipherTests.cs
using System.Security.Cryptography;
using HaloChat.Security;
using Xunit;

namespace HaloChat.Security.Tests;

public class AesMessageCipherTests
{
    [Theory]
    [InlineData(128)]
    [InlineData(192)]
    [InlineData(256)]
    public void Encrypt_ThenDecrypt_ReturnsOriginalPlaintext(int keySizeBits)
    {
        var cipher = new AesMessageCipher();
        var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(keySizeBits / 8));
        const string original = "Xin chào, đây là tin nhắn test AES-GCM";

        var encrypted = cipher.Encrypt(original, key);
        var decrypted = cipher.Decrypt(encrypted, key);

        Assert.Equal(original, decrypted);
    }

    [Fact]
    public void Decrypt_WithTamperedCipherText_ThrowsCryptographicException()
    {
        var cipher = new AesMessageCipher();
        var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var encrypted = cipher.Encrypt("dữ liệu gốc", key);
        encrypted.CipherText[0] ^= 0xFF; // giả lập dữ liệu bị sửa trên đường truyền

        Assert.Throws<CryptographicException>(() => cipher.Decrypt(encrypted, key));
    }

    [Fact]
    public void Encrypt_ProducesDifferentNonceEachCall()
    {
        var cipher = new AesMessageCipher();
        var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        var first = cipher.Encrypt("cùng nội dung", key);
        var second = cipher.Encrypt("cùng nội dung", key);

        Assert.NotEqual(first.Iv, second.Iv); // nonce không được lặp lại giữa các lần mã hóa
    }
}
```

```csharp
// tests/HaloChat.Security.Tests/HkdfConversationKeyProviderTests.cs
using HaloChat.Security;
using Microsoft.Extensions.Options;
using Xunit;

namespace HaloChat.Security.Tests;

public class HkdfConversationKeyProviderTests
{
    private static IOptions<AesKeyOptions> MakeOptions(string pepper = "pepper-bi-mat-du-dai-cho-test", int keySizeBits = 256)
        => Options.Create(new AesKeyOptions { Pepper = pepper, KeySizeBits = keySizeBits });

    [Fact]
    public void GetKey_IsSymmetricRegardlessOfArgumentOrder()
    {
        var provider = new HkdfConversationKeyProvider(MakeOptions());

        Assert.Equal(provider.GetKey("user-1", "user-2"), provider.GetKey("user-2", "user-1"));
    }

    [Fact]
    public void GetKey_DifferentPairsProduceDifferentKeys()
    {
        var provider = new HkdfConversationKeyProvider(MakeOptions());

        Assert.NotEqual(provider.GetKey("user-1", "user-2"), provider.GetKey("user-1", "user-3"));
    }

    [Fact]
    public void GetKey_OutputLengthMatchesConfiguredKeySize()
    {
        var provider = new HkdfConversationKeyProvider(MakeOptions(keySizeBits: 128));

        var keyBytes = Convert.FromBase64String(provider.GetKey("a", "b"));

        Assert.Equal(16, keyBytes.Length);
    }

    [Fact]
    public void Constructor_WithEmptyPepper_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => new HkdfConversationKeyProvider(MakeOptions(pepper: "")));
    }
}
```

## 9. Giới hạn của mô hình (ghi vào báo cáo, đừng bỏ qua)

- **Đây là mô hình khóa đơn giản hóa cho đồ án**, không phải trao đổi khóa
  thực thụ (không Diffie-Hellman/PKI). Toàn bộ khóa mọi cuộc trò chuyện đều
  tính lại được từ 1 pepper duy nhất — nếu pepper lộ, mọi tin nhắn (cũ lẫn
  mới) có thể bị tính lại khóa và giải mã. Đánh đổi lấy sự đơn giản, phù hợp
  mục tiêu học thuật của môn học, không dùng nguyên trạng cho hệ thống thật.
- Nonce 96-bit sinh ngẫu nhiên cho mỗi tin nhắn — xác suất trùng nonce với
  cùng 1 khóa là không đáng kể ở quy mô tin nhắn của một đồ án; hệ thống có
  lưu lượng cực lớn nên cân nhắc nonce dựa trên counter thay vì ngẫu nhiên
  thuần túy.
- Đổi `Aes:KeySizeBits` sau khi đã có dữ liệu cũ sẽ khiến tin nhắn cũ không
  giải mã lại được (rơi vào `"[không giải mã được]"` — hành vi đã có sẵn,
  không crash, nhưng nên chốt kích thước khóa trước khi có dữ liệu thật).

## 10. Việc nhóm bảo mật cần làm (checklist)

1. Thêm `AesKeyOptions.cs` + `HkdfConversationKeyProvider.cs` vào
   `HaloChat.Security` (mục 3) — tự gõ lại theo mẫu, không copy-paste.
2. Thêm `AesMessageCipher.cs` vào `HaloChat.Security` (mục 4).
3. Viết/chạy các unit test ở mục 8, đảm bảo `dotnet test` xanh cho riêng 2
   file trên trước khi tích hợp vào web.
4. Sửa `Program.cs` — đổi 2 dòng đăng ký DI + 1 dòng `Configure` (mục 5).
5. Thêm mục `"Aes"` vào `appsettings.json` (mục 6), set pepper thật qua
   `dotnet user-secrets` khi chạy dev.
6. Chạy lại toàn bộ `dotnet test`, rồi thử chat thực tế giữa 2 tài khoản;
   xác nhận tin nhắn mới mã hóa/giải mã đúng, tin nhắn cũ (Algorithm =
   `"none"`) hiện `"[không giải mã được]"` thay vì crash trang.
7. Tạo `HaloChat.Benchmark` (mục 7), thêm vào `HaloChat.sln`, chạy đo và đưa
   bảng số liệu + nhận xét vào báo cáo môn học.
