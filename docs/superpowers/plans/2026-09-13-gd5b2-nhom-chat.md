# GĐ5b-2 (Nhóm chat + Thông báo realtime + Redesign) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Thêm nhóm chat (tạo/thêm/xóa thành viên/rời/giải tán), thông báo realtime (dropdown lời mời + tin chưa đọc), presence online/offline thật, trạng thái tin nhắn 4 mức cho hội thoại 1-1, và redesign giao diện responsive theo mockup người dùng cung cấp.

**Architecture:** Backend thêm collection `Nhom` + mở rộng `ChatHub`/`DichVuTinNhan` để hỗ trợ `NhomId` song song `NguoiNhanId` đã có; 1 bảng kết nối `userId -> connectionId` mới trong Hub phục vụ cả group-membership realtime lẫn presence. Frontend rút phần hiển thị tin nhắn dùng chung thành `KhungTinNhan`, thêm `TrangNhom` và dropdown thông báo, restyle CSS theo mockup có sẵn (biến màu hiện tại đã gần khớp mockup).

**Tech Stack:** ASP.NET Core 9 + SignalR + MongoDB.Driver (backend/HaloChat.Api), .NET class library (backend/HaloChat.Security), React 19 + TypeScript + Vite + react-router-dom + @microsoft/signalr (frontend/), xUnit (backend test), Vitest + Testing Library (frontend test).

**Spec:** `docs/superpowers/specs/2026-09-10-halochat-rsa-aes-design.md` §10.7, §10.8 (đọc cả 2 mục trước khi bắt đầu bất kỳ task nào).

## Global Constraints

- Tin nhắn (1-1 và nhóm) tiếp tục lưu `NoiDungTinNhan` dạng **plaintext** trên MongoDB. KHÔNG gọi `HaloChat.Security.DichVuMaHoa` ở bất kỳ đâu trong plan này — mã hóa thật thuộc GĐ6.
- KHÔNG sửa lại hành vi đã chốt ở GĐ5b-1: `LoiMoiKetBan`, `ChoPhepTinNhanTuNguoiLa`, `GET /api/tinnhan/hoi-thoai` giữ nguyên như hiện tại — nhóm KHÔNG được gộp vào danh sách hội thoại đó.
- Mọi endpoint REST mới bắt buộc `[Authorize]`, đọc id người dùng hiện tại qua `User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value` đúng mẫu `NguoiDungController`/`KetBanController`/`TinNhanController` đã có — trả `Unauthorized()` nếu null.
- Cuối MỌI task backend: `dotnet build backend/HaloChat.sln` phải 0 lỗi và `dotnet test backend/HaloChat.sln` phải xanh toàn bộ trước khi commit.
- Cuối MỌI task frontend: cả `npm run test -- --run` (vitest) VÀ `npm run build` (chạy `tsc -b` full type-check) phải xanh trước khi commit — dự án từng có bug thật lọt qua vì chỉ chạy vitest (transpile-only, không bắt lỗi kiểu) mà không chạy `tsc -b`.
- Namespace mới `HaloChat.Security` cho các file di chuyển ở Task 1 — không để sót `using HaloChat.Api.Services;` cũ nào tham chiếu `DichVuMatKhau`/`IDichVuMatKhau`.
- Toàn bộ chuỗi hiển thị cho người dùng (thông báo lỗi, nhãn UI) viết bằng tiếng Việt, đúng văn phong hiện có trong codebase (không dấu gạch dưới, câu hoàn chỉnh có dấu chấm cho thông báo lỗi).
- Giữ nguyên các biến CSS đã có trong `frontend/src/index.css` (`--mau-chinh`, `--mau-chinh-dam`, `--mau-nen-tren`, `--mau-vien`, `--ban-kinh-o`, v.v.) — KHÔNG định nghĩa bảng màu mới, bảng màu hiện tại đã khớp mockup.

---

## Task 1: Di chuyển `DichVuMatKhau` sang `HaloChat.Security`

**Files:**
- Create: `backend/HaloChat.Security/DichVuMatKhau.cs`
- Create: `backend/HaloChat.Security/IDichVuMatKhau.cs`
- Delete: `backend/HaloChat.Api/Services/DichVuMatKhau.cs`
- Delete: `backend/HaloChat.Api/Services/IDichVuMatKhau.cs`
- Modify: `backend/HaloChat.Api/Program.cs` (dòng đăng ký DI `AddScoped<IDichVuMatKhau, DichVuMatKhau>()`, thêm `using HaloChat.Security;`)
- Modify: `backend/HaloChat.Api/Services/DichVuNguoiDung.cs` (thêm `using HaloChat.Security;`)
- Modify: `backend/HaloChat.Api.Tests/Services/DichVuMatKhauTests.cs` (đổi namespace test)

**Interfaces:**
- Consumes: không có (task độc lập, chỉ di chuyển code nguyên trạng).
- Produces: `HaloChat.Security.IDichVuMatKhau` với 3 method `TaoSalt()`, `BamMatKhau(string, string)`, `KiemTraMatKhau(string, string, string)` — chữ ký giữ nguyên y hệt bản cũ, mọi task sau vẫn gọi qua DI như cũ, chỉ đổi namespace import.

- [ ] **Step 1: Tạo `IDichVuMatKhau.cs` trong `HaloChat.Security`**

```csharp
namespace HaloChat.Security;

public interface IDichVuMatKhau
{
    string TaoSalt();
    string BamMatKhau(string matKhau, string salt);
    bool KiemTraMatKhau(string matKhau, string salt, string matKhauBam);
}
```

- [ ] **Step 2: Tạo `DichVuMatKhau.cs` trong `HaloChat.Security`**

```csharp
using System.Security.Cryptography;
using System.Text;

namespace HaloChat.Security;

public class DichVuMatKhau : IDichVuMatKhau
{
    public string TaoSalt()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }

    public string BamMatKhau(string matKhau, string salt)
    {
        var duLieu = Encoding.UTF8.GetBytes(matKhau + salt);
        var bamBytes = SHA256.HashData(duLieu);
        return Convert.ToBase64String(bamBytes);
    }

    public bool KiemTraMatKhau(string matKhau, string salt, string matKhauBam)
    {
        var bamMoi = Convert.FromBase64String(BamMatKhau(matKhau, salt));
        var bamCu = Convert.FromBase64String(matKhauBam);

        if (bamMoi.Length != bamCu.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(bamMoi, bamCu);
    }
}
```

- [ ] **Step 3: Xóa 2 file cũ**

```bash
git rm backend/HaloChat.Api/Services/DichVuMatKhau.cs backend/HaloChat.Api/Services/IDichVuMatKhau.cs
```

- [ ] **Step 4: Cập nhật `using` ở `Program.cs`**

Tìm dòng `using HaloChat.Api.Options;` gần đầu file, thêm ngay dưới:

```csharp
using HaloChat.Security;
```

Dòng đăng ký DI `builder.Services.AddScoped<IDichVuMatKhau, DichVuMatKhau>();` giữ nguyên chữ — chỉ cần `using` mới ở trên là đủ để nó resolve đúng type mới (không có type `IDichVuMatKhau` nào khác trong solution nên không xung đột).

- [ ] **Step 5: Cập nhật `using` ở `DichVuNguoiDung.cs`**

Thêm `using HaloChat.Security;` vào đầu file `backend/HaloChat.Api/Services/DichVuNguoiDung.cs` (cạnh `using HaloChat.Api.Dto;`).

- [ ] **Step 6: Đổi namespace file test**

Mở `backend/HaloChat.Api.Tests/Services/DichVuMatKhauTests.cs`, đổi dòng `namespace HaloChat.Api.Tests.Services;` thành `namespace HaloChat.Security.Tests;` và thêm `using HaloChat.Security;` ở đầu file (giữ nguyên toàn bộ nội dung test bên trong, không đổi logic).

- [ ] **Step 7: Build và test toàn bộ solution**

Run: `dotnet build backend/HaloChat.sln`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

Run: `dotnet test backend/HaloChat.sln`
Expected: tất cả test pass (72 test trở lên, không có test nào bị mất do namespace đổi).

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "Chuyen DichVuMatKhau sang HaloChat.Security dung kien truc goc (spec 3)"
```

---

## Task 2: `NguoiDung.HienThiTrangThaiHoatDong` + mở rộng `PUT /api/nguoidung/cai-dat`

**Files:**
- Modify: `backend/HaloChat.Api/Models/NguoiDung.cs`
- Modify: `backend/HaloChat.Api/Dto/CapNhatCaiDatRequest.cs`
- Modify: `backend/HaloChat.Api/Dto/HoSoCaNhanDto.cs`
- Modify: `backend/HaloChat.Api/Repositories/INguoiDungRepository.cs`
- Modify: `backend/HaloChat.Api/Repositories/NguoiDungRepository.cs`
- Modify: `backend/HaloChat.Api/Services/IDichVuNguoiDung.cs`
- Modify: `backend/HaloChat.Api/Services/DichVuNguoiDung.cs`
- Modify: `backend/HaloChat.Api/Controllers/NguoiDungController.cs`
- Modify: `backend/HaloChat.Api.Tests/Fakes/NguoiDungGiaLap.cs`
- Modify: `backend/HaloChat.Api.Tests/NguoiDungControllerTests.cs` (thêm test mới)

**Interfaces:**
- Consumes: không có (độc lập với Task 1, có thể chạy song song về mặt logic nhưng plan chạy tuần tự).
- Produces: `NguoiDung.HienThiTrangThaiHoatDong: bool` (mặc định `true`) — Task 5 (ChatHub presence) đọc field này để quyết định có đẩy sự kiện `TrangThaiHoatDongThayDoi` cho user đó hay không. `HoSoCaNhanDto` thêm field `HienThiTrangThaiHoatDong` cùng thứ tự tham số ở cuối. `INguoiDungRepository.CapNhatCaiDatAsync(string id, bool choPhepTinNhanTuNguoiLa, bool hienThiTrangThaiHoatDong)` thay thế hẳn `CapNhatChoPhepTinNhanTuNguoiLaAsync` cũ (đổi tên + thêm tham số, cập nhật mọi nơi gọi).

- [ ] **Step 1: Thêm field vào model `NguoiDung`**

Trong `backend/HaloChat.Api/Models/NguoiDung.cs`, thêm ngay dưới dòng `public bool ChoPhepTinNhanTuNguoiLa { get; set; } = false;`:

```csharp
    // [GĐ5b-2] Khi false, server không đẩy sự kiện TrangThaiHoatDongThayDoi
    // cho user này tới bạn bè — vẫn cho phép ẩn trạng thái online/offline
    // theo ý muốn, đúng toggle "Hiển thị trạng thái hoạt động" ở Cài đặt.
    public bool HienThiTrangThaiHoatDong { get; set; } = true;
```

- [ ] **Step 2: Mở rộng `CapNhatCaiDatRequest` và `HoSoCaNhanDto`**

`backend/HaloChat.Api/Dto/CapNhatCaiDatRequest.cs` — nội dung mới:

```csharp
namespace HaloChat.Api.Dto;

public record CapNhatCaiDatRequest(bool ChoPhepTinNhanTuNguoiLa, bool HienThiTrangThaiHoatDong);
```

`backend/HaloChat.Api/Dto/HoSoCaNhanDto.cs` — nội dung mới:

```csharp
namespace HaloChat.Api.Dto;

public record HoSoCaNhanDto(
    string Id, string TenTaiKhoan, string Email,
    bool ChoPhepTinNhanTuNguoiLa, bool HienThiTrangThaiHoatDong);
```

- [ ] **Step 3: Đổi tên + mở rộng method repository**

Trong `backend/HaloChat.Api/Repositories/INguoiDungRepository.cs`, thay dòng
`Task CapNhatChoPhepTinNhanTuNguoiLaAsync(string id, bool choPhep);` bằng:

```csharp
    Task CapNhatCaiDatAsync(string id, bool choPhepTinNhanTuNguoiLa, bool hienThiTrangThaiHoatDong);
```

Trong `backend/HaloChat.Api/Repositories/NguoiDungRepository.cs`, thay method `CapNhatChoPhepTinNhanTuNguoiLaAsync` bằng:

```csharp
    public async Task CapNhatCaiDatAsync(string id, bool choPhepTinNhanTuNguoiLa, bool hienThiTrangThaiHoatDong)
    {
        var boLoc = Builders<NguoiDung>.Filter.Eq(nd => nd.Id, id);
        var capNhat = Builders<NguoiDung>.Update
            .Set(nd => nd.ChoPhepTinNhanTuNguoiLa, choPhepTinNhanTuNguoiLa)
            .Set(nd => nd.HienThiTrangThaiHoatDong, hienThiTrangThaiHoatDong);
        await _collection.UpdateOneAsync(boLoc, capNhat);
    }
```

- [ ] **Step 4: Cập nhật fake repository cho test**

Trong `backend/HaloChat.Api.Tests/Fakes/NguoiDungGiaLap.cs`, thay method `CapNhatChoPhepTinNhanTuNguoiLaAsync` bằng:

```csharp
    public Task CapNhatCaiDatAsync(string id, bool choPhepTinNhanTuNguoiLa, bool hienThiTrangThaiHoatDong)
    {
        var nguoiDung = DanhSach.FirstOrDefault(nd => nd.Id == id);
        if (nguoiDung is not null)
        {
            nguoiDung.ChoPhepTinNhanTuNguoiLa = choPhepTinNhanTuNguoiLa;
            nguoiDung.HienThiTrangThaiHoatDong = hienThiTrangThaiHoatDong;
        }
        return Task.CompletedTask;
    }
```

- [ ] **Step 5: Cập nhật service**

Trong `backend/HaloChat.Api/Services/IDichVuNguoiDung.cs`, thay dòng
`Task CapNhatCaiDatAsync(string idHienTai, bool choPhepTinNhanTuNguoiLa);` bằng:

```csharp
    Task CapNhatCaiDatAsync(string idHienTai, bool choPhepTinNhanTuNguoiLa, bool hienThiTrangThaiHoatDong);
```

Trong `backend/HaloChat.Api/Services/DichVuNguoiDung.cs`, thay:

```csharp
    public Task CapNhatCaiDatAsync(string idHienTai, bool choPhepTinNhanTuNguoiLa) =>
        _kho.CapNhatChoPhepTinNhanTuNguoiLaAsync(idHienTai, choPhepTinNhanTuNguoiLa);
```

bằng:

```csharp
    public Task CapNhatCaiDatAsync(string idHienTai, bool choPhepTinNhanTuNguoiLa, bool hienThiTrangThaiHoatDong) =>
        _kho.CapNhatCaiDatAsync(idHienTai, choPhepTinNhanTuNguoiLa, hienThiTrangThaiHoatDong);
```

Và trong cùng file, method `LayThongTinCaNhanAsync` — cập nhật dòng tạo `HoSoCaNhanDto`:

```csharp
    public async Task<HoSoCaNhanDto?> LayThongTinCaNhanAsync(string id)
    {
        var nguoiDung = await _kho.TimTheoIdAsync(id);
        return nguoiDung is null
            ? null
            : new HoSoCaNhanDto(
                nguoiDung.Id, nguoiDung.TenTaiKhoan, nguoiDung.Email,
                nguoiDung.ChoPhepTinNhanTuNguoiLa, nguoiDung.HienThiTrangThaiHoatDong);
    }
```

- [ ] **Step 6: Cập nhật controller**

Trong `backend/HaloChat.Api/Controllers/NguoiDungController.cs`, dòng gọi service trong `CapNhatCaiDat`:

```csharp
        await _dichVu.CapNhatCaiDatAsync(idHienTai, yeuCau.ChoPhepTinNhanTuNguoiLa, yeuCau.HienThiTrangThaiHoatDong);
```

- [ ] **Step 7: Thêm test tích hợp**

Trong `backend/HaloChat.Api.Tests/NguoiDungControllerTests.cs`, thay lời gọi
`new CapNhatCaiDatRequest(true)` ở test `CapNhatCaiDat_DaDangNhap_CapNhatThanhCong` bằng
`new CapNhatCaiDatRequest(true, false)`, và thêm assertion mới ngay dưới assertion `ChoPhepTinNhanTuNguoiLa` đã có:

```csharp
        Assert.False(nguoiDung.HienThiTrangThaiHoatDong);
```

Thêm test mới xác nhận giá trị mặc định:

```csharp
    [Fact]
    public async Task LayThongTinCaNhan_MoiDangKy_HienThiTrangThaiHoatDongMacDinhTrue()
    {
        var yeuCauDangKy = TaoYeuCauDangKyHopLe("hosonguoib");
        await _client.PostAsJsonAsync("/api/nguoidung/dang-ky", yeuCauDangKy);
        var phanHoiDangNhap = await _client.PostAsJsonAsync(
            "/api/nguoidung/dang-nhap", new DangNhapRequest(yeuCauDangKy.TenTaiKhoan, "MatKhau123"));
        var ketQuaDangNhap = await phanHoiDangNhap.Content.ReadFromJsonAsync<DangNhapResponse>();

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", ketQuaDangNhap!.Token);
        var phanHoi = await _client.GetAsync("/api/nguoidung/toi");

        var hoSo = await phanHoi.Content.ReadFromJsonAsync<HoSoCaNhanDto>();
        Assert.True(hoSo!.HienThiTrangThaiHoatDong);
    }
```

- [ ] **Step 8: Build và test**

Run: `dotnet build backend/HaloChat.sln`
Expected: 0 lỗi.

Run: `dotnet test backend/HaloChat.sln`
Expected: toàn bộ pass, bao gồm 2 test mới/sửa ở Step 7.

- [ ] **Step 9: Commit**

```bash
git add -A
git commit -m "Them NguoiDung.HienThiTrangThaiHoatDong + mo rong PUT /api/nguoidung/cai-dat"
```

---

## Task 3: Model + Repository `Nhom`

**Files:**
- Create: `backend/HaloChat.Api/Models/Nhom.cs`
- Create: `backend/HaloChat.Api/Repositories/INhomRepository.cs`
- Create: `backend/HaloChat.Api/Repositories/NhomRepository.cs`
- Create: `backend/HaloChat.Api.Tests/Fakes/NhomGiaLap.cs`
- Modify: `backend/HaloChat.Api/Program.cs` (đăng ký DI)
- Modify: `backend/HaloChat.Api.Tests/ThietLapKiemThuTichHop.cs` (đăng ký fake)

**Interfaces:**
- Consumes: không có.
- Produces: model `Nhom` (`Id`, `TenNhom`, `MoTa`, `DuongDanAnhDaiDien`, `NguoiTaoId`, `ThanhVienIds: List<string>`, `ThoiGianTao`) + `INhomRepository` với các method `ThemMoiAsync`, `TimTheoIdAsync`, `LayTheoThanhVienAsync(string userId)`, `ThemThanhVienAsync(string nhomId, string userId)`, `XoaThanhVienAsync(string nhomId, string userId)`, `CapNhatThongTinAsync(string nhomId, string tenNhom, string? moTa, string? duongDanAnhDaiDien)`, `XoaNhomAsync(string nhomId)` — Task 4 (`DichVuNhom`) gọi trực tiếp các method này.

- [ ] **Step 1: Tạo model `Nhom`**

```csharp
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HaloChat.Api.Models;

public class Nhom
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string TenNhom { get; set; } = string.Empty;

    public string? MoTa { get; set; }

    public string? DuongDanAnhDaiDien { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string NguoiTaoId { get; set; } = string.Empty;

    // Nhúng thẳng danh sách id thành viên (quy mô đồ án nhỏ, không cần
    // collection join riêng — đúng spec §10.2). Luôn chứa cả NguoiTaoId.
    [BsonRepresentation(BsonType.ObjectId)]
    public List<string> ThanhVienIds { get; set; } = new();

    public DateTime ThoiGianTao { get; set; } = DateTime.UtcNow;
}
```

- [ ] **Step 2: Tạo interface `INhomRepository`**

```csharp
using HaloChat.Api.Models;

namespace HaloChat.Api.Repositories;

public interface INhomRepository
{
    Task ThemMoiAsync(Nhom nhom);

    Task<Nhom?> TimTheoIdAsync(string id);

    /// <summary>Danh sách nhóm mà userId là thành viên (ThanhVienIds chứa id đó).</summary>
    Task<List<Nhom>> LayTheoThanhVienAsync(string userId);

    Task ThemThanhVienAsync(string nhomId, string userId);

    Task XoaThanhVienAsync(string nhomId, string userId);

    Task CapNhatThongTinAsync(string nhomId, string tenNhom, string? moTa, string? duongDanAnhDaiDien);

    Task XoaNhomAsync(string nhomId);
}
```

- [ ] **Step 3: Tạo `NhomRepository`**

```csharp
using HaloChat.Api.Models;
using MongoDB.Driver;

namespace HaloChat.Api.Repositories;

public class NhomRepository : INhomRepository
{
    private readonly IMongoCollection<Nhom> _collection;

    public NhomRepository(IMongoDatabase csdl)
    {
        _collection = csdl.GetCollection<Nhom>("Nhom");
    }

    public Task ThemMoiAsync(Nhom nhom) => _collection.InsertOneAsync(nhom);

    public async Task<Nhom?> TimTheoIdAsync(string id) =>
        await _collection.Find(n => n.Id == id).FirstOrDefaultAsync();

    public async Task<List<Nhom>> LayTheoThanhVienAsync(string userId) =>
        await _collection.Find(n => n.ThanhVienIds.Contains(userId)).ToListAsync();

    public async Task ThemThanhVienAsync(string nhomId, string userId)
    {
        var capNhat = Builders<Nhom>.Update.AddToSet(n => n.ThanhVienIds, userId);
        await _collection.UpdateOneAsync(n => n.Id == nhomId, capNhat);
    }

    public async Task XoaThanhVienAsync(string nhomId, string userId)
    {
        var capNhat = Builders<Nhom>.Update.Pull(n => n.ThanhVienIds, userId);
        await _collection.UpdateOneAsync(n => n.Id == nhomId, capNhat);
    }

    public async Task CapNhatThongTinAsync(string nhomId, string tenNhom, string? moTa, string? duongDanAnhDaiDien)
    {
        var capNhat = Builders<Nhom>.Update
            .Set(n => n.TenNhom, tenNhom)
            .Set(n => n.MoTa, moTa)
            .Set(n => n.DuongDanAnhDaiDien, duongDanAnhDaiDien);
        await _collection.UpdateOneAsync(n => n.Id == nhomId, capNhat);
    }

    public async Task XoaNhomAsync(string nhomId) =>
        await _collection.DeleteOneAsync(n => n.Id == nhomId);
}
```

- [ ] **Step 4: Đăng ký DI trong `Program.cs`**

Thêm ngay dưới dòng `builder.Services.AddScoped<ILoiMoiKetBanRepository, LoiMoiKetBanRepository>();` (hoặc dòng tương đương đăng ký repository hiện có):

```csharp
builder.Services.AddScoped<INhomRepository, NhomRepository>();
```

- [ ] **Step 5: Tạo fake repository cho test**

```csharp
using HaloChat.Api.Models;
using HaloChat.Api.Repositories;

namespace HaloChat.Api.Tests.Fakes;

public class NhomGiaLap : INhomRepository
{
    public List<Nhom> DanhSach { get; } = new();

    public Task ThemMoiAsync(Nhom nhom)
    {
        DanhSach.Add(nhom);
        return Task.CompletedTask;
    }

    public Task<Nhom?> TimTheoIdAsync(string id) =>
        Task.FromResult(DanhSach.FirstOrDefault(n => n.Id == id));

    public Task<List<Nhom>> LayTheoThanhVienAsync(string userId) =>
        Task.FromResult(DanhSach.Where(n => n.ThanhVienIds.Contains(userId)).ToList());

    public Task ThemThanhVienAsync(string nhomId, string userId)
    {
        var nhom = DanhSach.FirstOrDefault(n => n.Id == nhomId);
        if (nhom is not null && !nhom.ThanhVienIds.Contains(userId))
        {
            nhom.ThanhVienIds.Add(userId);
        }
        return Task.CompletedTask;
    }

    public Task XoaThanhVienAsync(string nhomId, string userId)
    {
        DanhSach.FirstOrDefault(n => n.Id == nhomId)?.ThanhVienIds.Remove(userId);
        return Task.CompletedTask;
    }

    public Task CapNhatThongTinAsync(string nhomId, string tenNhom, string? moTa, string? duongDanAnhDaiDien)
    {
        var nhom = DanhSach.FirstOrDefault(n => n.Id == nhomId);
        if (nhom is not null)
        {
            nhom.TenNhom = tenNhom;
            nhom.MoTa = moTa;
            nhom.DuongDanAnhDaiDien = duongDanAnhDaiDien;
        }
        return Task.CompletedTask;
    }

    public Task XoaNhomAsync(string nhomId)
    {
        DanhSach.RemoveAll(n => n.Id == nhomId);
        return Task.CompletedTask;
    }
}
```

Lưu vào `backend/HaloChat.Api.Tests/Fakes/NhomGiaLap.cs`.

- [ ] **Step 6: Đăng ký fake trong fixture kiểm thử**

Trong `backend/HaloChat.Api.Tests/ThietLapKiemThuTichHop.cs`, thêm property mới cạnh các `KhoGiaLap`/`KhoTinNhanGiaLap` đã có:

```csharp
    public NhomGiaLap KhoNhomGiaLap { get; } = new();
```

Và trong `ConfigureWebHost`, thêm cạnh các `dichVu.RemoveAll<...>()` đã có:

```csharp
            dichVu.RemoveAll<INhomRepository>();
            dichVu.AddSingleton<INhomRepository>(KhoNhomGiaLap);
```

- [ ] **Step 7: Build và test**

Run: `dotnet build backend/HaloChat.sln`
Expected: 0 lỗi (chưa có test nào dùng `Nhom` trực tiếp ở task này, chỉ xác nhận biên dịch sạch).

Run: `dotnet test backend/HaloChat.sln`
Expected: toàn bộ pass (không có test nào bị ảnh hưởng).

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "Them model va repository Nhom (chua co API/Hub)"
```

---

## Task 4: `DichVuNhom` + `NhomController` (CRUD, chưa realtime)

**Files:**
- Create: `backend/HaloChat.Api/Dto/NhomDto.cs`
- Create: `backend/HaloChat.Api/Dto/TaoNhomRequest.cs`
- Create: `backend/HaloChat.Api/Dto/CapNhatNhomRequest.cs`
- Create: `backend/HaloChat.Api/Services/NgoaiLeNhom.cs`
- Create: `backend/HaloChat.Api/Services/IDichVuNhom.cs`
- Create: `backend/HaloChat.Api/Services/DichVuNhom.cs`
- Create: `backend/HaloChat.Api/Controllers/NhomController.cs`
- Modify: `backend/HaloChat.Api/Program.cs` (đăng ký DI `IDichVuNhom`)
- Create: `backend/HaloChat.Api.Tests/NhomControllerTests.cs`

**Interfaces:**
- Consumes: `INhomRepository` (Task 3), `INguoiDungRepository.TimTheoIdAsync`/`LayTatCaAsync` (đã có).
- Produces: `IDichVuNhom` với `TaoNhomAsync`, `LayDanhSachAsync`, `LayChiTietAsync`, `CapNhatAsync` — Task 7 (thêm/xóa thành viên realtime) mở rộng interface này thêm method, không đổi 4 method ở đây. `NhomDto(string Id, string TenNhom, string? MoTa, string? DuongDanAnhDaiDien, string NguoiTaoId, List<NguoiDungTomTatDto> ThanhVien, DateTime ThoiGianTao)` — Task 9/10 (frontend) dùng đúng field names này (camelCase khi serialize JSON: `id, tenNhom, moTa, duongDanAnhDaiDien, nguoiTaoId, thanhVien, thoiGianTao`).

- [ ] **Step 1: Tạo các DTO**

`backend/HaloChat.Api/Dto/NhomDto.cs`:

```csharp
namespace HaloChat.Api.Dto;

public record NhomDto(
    string Id, string TenNhom, string? MoTa, string? DuongDanAnhDaiDien,
    string NguoiTaoId, List<NguoiDungTomTatDto> ThanhVien, DateTime ThoiGianTao);
```

`backend/HaloChat.Api/Dto/TaoNhomRequest.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace HaloChat.Api.Dto;

public record TaoNhomRequest(
    [Required(ErrorMessage = "Vui lòng nhập tên nhóm.")]
    [MinLength(2, ErrorMessage = "Tên nhóm phải có ít nhất 2 ký tự.")]
    string TenNhom,
    string? MoTa,
    string? DuongDanAnhDaiDien,
    List<string> ThanhVienIds);
```

`backend/HaloChat.Api/Dto/CapNhatNhomRequest.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace HaloChat.Api.Dto;

public record CapNhatNhomRequest(
    [Required(ErrorMessage = "Vui lòng nhập tên nhóm.")]
    [MinLength(2, ErrorMessage = "Tên nhóm phải có ít nhất 2 ký tự.")]
    string TenNhom,
    string? MoTa,
    string? DuongDanAnhDaiDien);
```

- [ ] **Step 2: Tạo các exception**

```csharp
namespace HaloChat.Api.Services;

/// <summary>Ném ra khi thao tác trên 1 nhóm không tồn tại.</summary>
public class NhomKhongTonTaiException : Exception
{
    public NhomKhongTonTaiException() : base("Nhóm không tồn tại.")
    {
    }
}

/// <summary>Ném ra khi người gọi không phải thành viên của nhóm đang thao tác.</summary>
public class KhongPhaiThanhVienNhomException : Exception
{
    public KhongPhaiThanhVienNhomException() : base("Bạn không phải thành viên của nhóm này.")
    {
    }
}

/// <summary>Ném ra khi người gọi không phải người tạo (admin) của nhóm.</summary>
public class KhongCoQuyenQuanTriNhomException : Exception
{
    public KhongCoQuyenQuanTriNhomException() : base("Chỉ người tạo nhóm mới có quyền thực hiện thao tác này.")
    {
    }
}

/// <summary>Ném ra khi id thành viên được chọn không tồn tại trong hệ thống.</summary>
public class ThanhVienKhongTonTaiException : Exception
{
    public ThanhVienKhongTonTaiException(string id) : base($"Người dùng không tồn tại: {id}.")
    {
    }
}
```

Lưu vào `backend/HaloChat.Api/Services/NgoaiLeNhom.cs`.

- [ ] **Step 3: Tạo `IDichVuNhom`**

```csharp
using HaloChat.Api.Dto;

namespace HaloChat.Api.Services;

public interface IDichVuNhom
{
    Task<NhomDto> TaoNhomAsync(string nguoiTaoId, string tenNhom, string? moTa, string? duongDanAnhDaiDien, List<string> thanhVienIds);
    Task<List<NhomDto>> LayDanhSachAsync(string nguoiDungId);
    Task<NhomDto> LayChiTietAsync(string nguoiDungId, string nhomId);
    Task<NhomDto> CapNhatAsync(string nguoiDungId, string nhomId, string tenNhom, string? moTa, string? duongDanAnhDaiDien);
}
```

- [ ] **Step 4: Tạo `DichVuNhom`**

```csharp
using HaloChat.Api.Dto;
using HaloChat.Api.Models;
using HaloChat.Api.Repositories;
using MongoDB.Bson;

namespace HaloChat.Api.Services;

public class DichVuNhom : IDichVuNhom
{
    private readonly INhomRepository _khoNhom;
    private readonly INguoiDungRepository _khoNguoiDung;

    public DichVuNhom(INhomRepository khoNhom, INguoiDungRepository khoNguoiDung)
    {
        _khoNhom = khoNhom;
        _khoNguoiDung = khoNguoiDung;
    }

    public async Task<NhomDto> TaoNhomAsync(
        string nguoiTaoId, string tenNhom, string? moTa, string? duongDanAnhDaiDien, List<string> thanhVienIds)
    {
        var idThanhVien = new HashSet<string>(thanhVienIds) { nguoiTaoId };
        foreach (var id in idThanhVien)
        {
            if (!ObjectId.TryParse(id, out _) || await _khoNguoiDung.TimTheoIdAsync(id) is null)
            {
                throw new ThanhVienKhongTonTaiException(id);
            }
        }

        var nhom = new Nhom
        {
            TenNhom = tenNhom,
            MoTa = moTa,
            DuongDanAnhDaiDien = duongDanAnhDaiDien,
            NguoiTaoId = nguoiTaoId,
            ThanhVienIds = idThanhVien.ToList(),
        };
        await _khoNhom.ThemMoiAsync(nhom);

        return await AnhXaDtoAsync(nhom);
    }

    public async Task<List<NhomDto>> LayDanhSachAsync(string nguoiDungId)
    {
        var danhSach = await _khoNhom.LayTheoThanhVienAsync(nguoiDungId);
        var ketQua = new List<NhomDto>();
        foreach (var nhom in danhSach)
        {
            ketQua.Add(await AnhXaDtoAsync(nhom));
        }
        return ketQua;
    }

    public async Task<NhomDto> LayChiTietAsync(string nguoiDungId, string nhomId)
    {
        var nhom = await LayNhomKiemTraThanhVienAsync(nguoiDungId, nhomId);
        return await AnhXaDtoAsync(nhom);
    }

    public async Task<NhomDto> CapNhatAsync(
        string nguoiDungId, string nhomId, string tenNhom, string? moTa, string? duongDanAnhDaiDien)
    {
        var nhom = await LayNhomKiemTraQuanTriAsync(nguoiDungId, nhomId);
        await _khoNhom.CapNhatThongTinAsync(nhomId, tenNhom, moTa, duongDanAnhDaiDien);
        nhom.TenNhom = tenNhom;
        nhom.MoTa = moTa;
        nhom.DuongDanAnhDaiDien = duongDanAnhDaiDien;
        return await AnhXaDtoAsync(nhom);
    }

    /// <summary>Lấy nhóm theo id, ném lỗi nếu không tồn tại hoặc người gọi không phải thành viên.</summary>
    private async Task<Nhom> LayNhomKiemTraThanhVienAsync(string nguoiDungId, string nhomId)
    {
        var nhom = await _khoNhom.TimTheoIdAsync(nhomId) ?? throw new NhomKhongTonTaiException();
        if (!nhom.ThanhVienIds.Contains(nguoiDungId))
        {
            throw new KhongPhaiThanhVienNhomException();
        }
        return nhom;
    }

    /// <summary>Lấy nhóm theo id, ném lỗi nếu không tồn tại hoặc người gọi không phải NguoiTaoId (admin).</summary>
    private async Task<Nhom> LayNhomKiemTraQuanTriAsync(string nguoiDungId, string nhomId)
    {
        var nhom = await _khoNhom.TimTheoIdAsync(nhomId) ?? throw new NhomKhongTonTaiException();
        if (nhom.NguoiTaoId != nguoiDungId)
        {
            throw new KhongCoQuyenQuanTriNhomException();
        }
        return nhom;
    }

    private async Task<NhomDto> AnhXaDtoAsync(Nhom nhom)
    {
        var thanhVien = new List<NguoiDungTomTatDto>();
        foreach (var id in nhom.ThanhVienIds)
        {
            var nd = await _khoNguoiDung.TimTheoIdAsync(id);
            if (nd is not null)
            {
                thanhVien.Add(new NguoiDungTomTatDto(nd.Id, nd.TenTaiKhoan, nd.Email));
            }
        }
        return new NhomDto(nhom.Id, nhom.TenNhom, nhom.MoTa, nhom.DuongDanAnhDaiDien, nhom.NguoiTaoId, thanhVien, nhom.ThoiGianTao);
    }
}
```

- [ ] **Step 5: Tạo `NhomController`**

```csharp
using System.IdentityModel.Tokens.Jwt;
using HaloChat.Api.Dto;
using HaloChat.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HaloChat.Api.Controllers;

[ApiController]
[Route("api/nhom")]
[Authorize]
public class NhomController : ControllerBase
{
    private readonly IDichVuNhom _dichVu;

    public NhomController(IDichVuNhom dichVu)
    {
        _dichVu = dichVu;
    }

    private string? IdHienTai => User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

    [HttpPost]
    public async Task<IActionResult> TaoNhom([FromBody] TaoNhomRequest yeuCau)
    {
        if (IdHienTai is null)
        {
            return Unauthorized();
        }

        try
        {
            var nhom = await _dichVu.TaoNhomAsync(IdHienTai, yeuCau.TenNhom, yeuCau.MoTa, yeuCau.DuongDanAnhDaiDien, yeuCau.ThanhVienIds);
            return Ok(nhom);
        }
        catch (ThanhVienKhongTonTaiException loi)
        {
            return BadRequest(new { thongBao = loi.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> LayDanhSach()
    {
        if (IdHienTai is null)
        {
            return Unauthorized();
        }

        return Ok(await _dichVu.LayDanhSachAsync(IdHienTai));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> LayChiTiet(string id)
    {
        if (IdHienTai is null)
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await _dichVu.LayChiTietAsync(IdHienTai, id));
        }
        catch (NhomKhongTonTaiException loi)
        {
            return NotFound(new { thongBao = loi.Message });
        }
        catch (KhongPhaiThanhVienNhomException loi)
        {
            return Forbid(loi.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> CapNhat(string id, [FromBody] CapNhatNhomRequest yeuCau)
    {
        if (IdHienTai is null)
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await _dichVu.CapNhatAsync(IdHienTai, id, yeuCau.TenNhom, yeuCau.MoTa, yeuCau.DuongDanAnhDaiDien));
        }
        catch (NhomKhongTonTaiException loi)
        {
            return NotFound(new { thongBao = loi.Message });
        }
        catch (KhongCoQuyenQuanTriNhomException loi)
        {
            return StatusCode(403, new { thongBao = loi.Message });
        }
    }
}
```

Ghi chú: `Forbid()` mặc định của ASP.NET Core Identity không nhận message tùy ý qua constructor kiểu đó khi không có authentication scheme cookie — dùng `StatusCode(403, new { thongBao = ... })` thống nhất cho MỌI trường hợp 403 trong file này (bao gồm sửa lại `LayChiTiet` để cùng kiểu trả về):

```csharp
        catch (KhongPhaiThanhVienNhomException loi)
        {
            return StatusCode(403, new { thongBao = loi.Message });
        }
```

- [ ] **Step 6: Đăng ký DI**

Thêm vào `Program.cs` cạnh dòng đăng ký `INhomRepository` ở Task 3:

```csharp
builder.Services.AddScoped<IDichVuNhom, DichVuNhom>();
```

- [ ] **Step 7: Viết test tích hợp**

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using HaloChat.Api.Dto;
using Xunit;

namespace HaloChat.Api.Tests;

public class NhomControllerTests : IClassFixture<ThietLapKiemThuTichHop>
{
    private readonly ThietLapKiemThuTichHop _factory;
    private readonly HttpClient _client;

    public NhomControllerTests(ThietLapKiemThuTichHop factory)
    {
        _factory = factory;
        _factory.KhoGiaLap.DanhSach.Clear();
        _factory.KhoNhomGiaLap.DanhSach.Clear();
        _client = factory.CreateClient();
    }

    private async Task<(string Token, string Id)> TaoTaiKhoanVaDangNhapAsync(string tenTaiKhoan)
    {
        await _client.PostAsJsonAsync("/api/nguoidung/dang-ky", new
        {
            TenTaiKhoan = tenTaiKhoan, Email = $"{tenTaiKhoan}@gmail.com", MatKhau = "MatKhau123",
        });
        var phanHoi = await _client.PostAsJsonAsync("/api/nguoidung/dang-nhap", new
        {
            TenDangNhap = tenTaiKhoan, MatKhau = "MatKhau123",
        });
        var ketQua = await phanHoi.Content.ReadFromJsonAsync<DangNhapResponse>();
        var id = _factory.KhoGiaLap.DanhSach.Single(nd => nd.TenTaiKhoan == tenTaiKhoan).Id;
        return (ketQua!.Token, id);
    }

    [Fact]
    public async Task TaoNhom_ThanhCong_TraVe200VaGomCaNguoiTao()
    {
        var (token, id) = await TaoTaiKhoanVaDangNhapAsync("nhomnguoia");
        var (_, idThanhVien) = await TaoTaiKhoanVaDangNhapAsync("nhomnguoib");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var phanHoi = await _client.PostAsJsonAsync("/api/nhom", new TaoNhomRequest("Nhóm CNTT", "Mô tả", null, new List<string> { idThanhVien }));

        Assert.Equal(HttpStatusCode.OK, phanHoi.StatusCode);
        var nhom = await phanHoi.Content.ReadFromJsonAsync<NhomDto>();
        Assert.Equal("Nhóm CNTT", nhom!.TenNhom);
        Assert.Equal(id, nhom.NguoiTaoId);
        Assert.Equal(2, nhom.ThanhVien.Count);
    }

    [Fact]
    public async Task TaoNhom_ThanhVienKhongTonTai_TraVe400()
    {
        var (token, _) = await TaoTaiKhoanVaDangNhapAsync("nhomnguoic");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var phanHoi = await _client.PostAsJsonAsync(
            "/api/nhom", new TaoNhomRequest("Nhóm lỗi", null, null, new List<string> { "000000000000000000000000" }));

        Assert.Equal(HttpStatusCode.BadRequest, phanHoi.StatusCode);
    }

    [Fact]
    public async Task LayChiTiet_KhongPhaiThanhVien_TraVe403()
    {
        var (tokenA, _) = await TaoTaiKhoanVaDangNhapAsync("nhomnguoid");
        var (tokenB, _) = await TaoTaiKhoanVaDangNhapAsync("nhomnguoie");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        var nhom = await (await _client.PostAsJsonAsync("/api/nhom", new TaoNhomRequest("Nhóm riêng", null, null, new List<string>())))
            .Content.ReadFromJsonAsync<NhomDto>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var phanHoi = await _client.GetAsync($"/api/nhom/{nhom!.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, phanHoi.StatusCode);
    }

    [Fact]
    public async Task CapNhat_KhongPhaiAdmin_TraVe403()
    {
        var (tokenA, idA) = await TaoTaiKhoanVaDangNhapAsync("nhomnguoif");
        var (tokenB, _) = await TaoTaiKhoanVaDangNhapAsync("nhomnguoig");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        var nhom = await (await _client.PostAsJsonAsync("/api/nhom", new TaoNhomRequest("Nhóm X", null, null, new List<string>())))
            .Content.ReadFromJsonAsync<NhomDto>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var phanHoi = await _client.PutAsJsonAsync($"/api/nhom/{nhom!.Id}", new CapNhatNhomRequest("Tên mới", null, null));

        Assert.Equal(HttpStatusCode.Forbidden, phanHoi.StatusCode);
        Assert.NotEqual(idA, "bo-qua-canh-bao-bien-khong-dung"); // giữ idA có sử dụng, tránh cảnh báo biến không dùng
    }

    [Fact]
    public async Task LayDanhSach_TraVeDungNhomDaThamGia()
    {
        var (token, _) = await TaoTaiKhoanVaDangNhapAsync("nhomnguoih");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await _client.PostAsJsonAsync("/api/nhom", new TaoNhomRequest("Nhóm Y", null, null, new List<string>()));

        var phanHoi = await _client.GetAsync("/api/nhom");
        var danhSach = await phanHoi.Content.ReadFromJsonAsync<List<NhomDto>>();

        Assert.Single(danhSach!);
    }
}
```

Ghi chú cho implementer: dòng `Assert.NotEqual(idA, "bo-qua-canh-bao-bien-khong-dung")` trong `CapNhat_KhongPhaiAdmin_TraVe403` là để tránh cảnh báo biến `idA` không dùng tới (dự án bật `TreatWarningsAsErrors` gián tiếp qua review chất lượng) — nếu build không cảnh báo gì khi bỏ dòng đó thì được phép xóa, ưu tiên code sạch hơn giữ dòng vô nghĩa; kiểm tra bằng cách build thử cả 2 cách.

- [ ] **Step 8: Build và test**

Run: `dotnet build backend/HaloChat.sln`
Expected: 0 lỗi.

Run: `dotnet test backend/HaloChat.sln`
Expected: toàn bộ pass, bao gồm 5 test mới ở `NhomControllerTests`.

- [ ] **Step 9: Commit**

```bash
git add -A
git commit -m "Them DichVuNhom + NhomController: tao/lay danh sach/chi tiet/cap nhat nhom"
```

---

## Task 5: `IQuanLyKetNoiChat` + presence (`ChatHub.OnConnectedAsync`/`OnDisconnectedAsync`) + `GET /api/nguoidung/trang-thai`

**Files:**
- Create: `backend/HaloChat.Api/Services/IQuanLyKetNoiChat.cs`
- Create: `backend/HaloChat.Api/Services/QuanLyKetNoiChat.cs`
- Modify: `backend/HaloChat.Api/Hubs/ChatHub.cs` (thêm `OnConnectedAsync`/`OnDisconnectedAsync`, join nhóm)
- Modify: `backend/HaloChat.Api/Controllers/NguoiDungController.cs` (thêm `GET /api/nguoidung/trang-thai`)
- Modify: `backend/HaloChat.Api/Program.cs` (đăng ký `IQuanLyKetNoiChat` singleton)
- Create: `backend/HaloChat.Api.Tests/Services/QuanLyKetNoiChatTests.cs`
- Modify: `backend/HaloChat.Api.Tests/ChatHubTests.cs` (thêm test presence + join nhóm khi connect)

**Interfaces:**
- Consumes: `IDichVuNhom.LayDanhSachAsync` (Task 4), `ILoiMoiKetBanRepository.LayBanBeAsync` (đã có từ GĐ5b-1), `INguoiDungRepository.TimTheoIdAsync` (đã có).
- Produces: `IQuanLyKetNoiChat` singleton với `ThemKetNoi(string userId, string connectionId) -> bool laoOnlineMoi`, `XoaKetNoi(string userId, string connectionId) -> bool vuaThanhOffline`, `DangOnline(string userId) -> bool` — Task 6 (`GuiTinNhan` mở rộng `DaNhan`) và Task 7 (thêm/xóa thành viên realtime) đều dùng `IQuanLyKetNoiChat` để lấy trạng thái/connection của 1 user, tiêm qua DI (đăng ký singleton).

- [ ] **Step 1: Tạo `IQuanLyKetNoiChat`**

```csharp
namespace HaloChat.Api.Services;

/// <summary>
/// Theo dõi các connectionId SignalR đang mở của từng user (1 user có thể
/// mở nhiều tab/thiết bị cùng lúc). Dùng cho: (a) presence online/offline
/// (user online khi có >=1 connection), (b) join/gỡ SignalR Group cho các
/// connection cụ thể khi thành viên nhóm thay đổi (Groups.AddToGroupAsync/
/// RemoveFromGroupAsync đòi connectionId, không chỉ userId).
/// </summary>
public interface IQuanLyKetNoiChat
{
    /// <summary>Ghi nhận 1 connection mới. Trả về true nếu đây là connection ĐẦU TIÊN của user (vừa online).</summary>
    bool ThemKetNoi(string userId, string connectionId);

    /// <summary>Gỡ 1 connection. Trả về true nếu đây là connection CUỐI CÙNG bị gỡ (vừa offline).</summary>
    bool XoaKetNoi(string userId, string connectionId);

    bool DangOnline(string userId);

    /// <summary>Toàn bộ connectionId đang mở của 1 user (rỗng nếu offline).</summary>
    IReadOnlyCollection<string> LayConnectionIds(string userId);
}
```

- [ ] **Step 2: Tạo `QuanLyKetNoiChat`**

```csharp
using System.Collections.Concurrent;

namespace HaloChat.Api.Services;

public class QuanLyKetNoiChat : IQuanLyKetNoiChat
{
    private readonly ConcurrentDictionary<string, HashSet<string>> _ketNoiTheoUser = new();
    private readonly object _khoa = new();

    public bool ThemKetNoi(string userId, string connectionId)
    {
        lock (_khoa)
        {
            var laOnlineMoi = !_ketNoiTheoUser.ContainsKey(userId) || _ketNoiTheoUser[userId].Count == 0;
            var tapHop = _ketNoiTheoUser.GetOrAdd(userId, _ => new HashSet<string>());
            tapHop.Add(connectionId);
            return laOnlineMoi;
        }
    }

    public bool XoaKetNoi(string userId, string connectionId)
    {
        lock (_khoa)
        {
            if (!_ketNoiTheoUser.TryGetValue(userId, out var tapHop))
            {
                return false;
            }

            tapHop.Remove(connectionId);
            if (tapHop.Count == 0)
            {
                _ketNoiTheoUser.TryRemove(userId, out _);
                return true;
            }

            return false;
        }
    }

    public bool DangOnline(string userId) =>
        _ketNoiTheoUser.TryGetValue(userId, out var tapHop) && tapHop.Count > 0;

    public IReadOnlyCollection<string> LayConnectionIds(string userId) =>
        _ketNoiTheoUser.TryGetValue(userId, out var tapHop) ? tapHop.ToList() : Array.Empty<string>();
}
```

- [ ] **Step 3: Đăng ký DI (singleton — trạng thái phải sống suốt vòng đời app, không theo từng request)**

Thêm vào `Program.cs` cạnh `builder.Services.AddSignalR();`:

```csharp
builder.Services.AddSingleton<IQuanLyKetNoiChat, QuanLyKetNoiChat>();
```

- [ ] **Step 4: Viết test đơn vị cho `QuanLyKetNoiChat`**

```csharp
using HaloChat.Api.Services;
using Xunit;

namespace HaloChat.Api.Tests.Services;

public class QuanLyKetNoiChatTests
{
    [Fact]
    public void ThemKetNoi_ConnectionDauTien_TraVeTrueVaDangOnline()
    {
        var quanLy = new QuanLyKetNoiChat();

        var laOnlineMoi = quanLy.ThemKetNoi("user1", "conn1");

        Assert.True(laOnlineMoi);
        Assert.True(quanLy.DangOnline("user1"));
    }

    [Fact]
    public void ThemKetNoi_ConnectionThuHai_TraVeFalse()
    {
        var quanLy = new QuanLyKetNoiChat();
        quanLy.ThemKetNoi("user1", "conn1");

        var laOnlineMoi = quanLy.ThemKetNoi("user1", "conn2");

        Assert.False(laOnlineMoi);
    }

    [Fact]
    public void XoaKetNoi_ConnectionCuoiCung_TraVeTrueVaHetOnline()
    {
        var quanLy = new QuanLyKetNoiChat();
        quanLy.ThemKetNoi("user1", "conn1");

        var vuaOffline = quanLy.XoaKetNoi("user1", "conn1");

        Assert.True(vuaOffline);
        Assert.False(quanLy.DangOnline("user1"));
    }

    [Fact]
    public void XoaKetNoi_ConVaiConnectionKhac_TraVeFalseVaVanOnline()
    {
        var quanLy = new QuanLyKetNoiChat();
        quanLy.ThemKetNoi("user1", "conn1");
        quanLy.ThemKetNoi("user1", "conn2");

        var vuaOffline = quanLy.XoaKetNoi("user1", "conn1");

        Assert.False(vuaOffline);
        Assert.True(quanLy.DangOnline("user1"));
    }

    [Fact]
    public void DangOnline_UserChuaTungKetNoi_TraVeFalse()
    {
        var quanLy = new QuanLyKetNoiChat();

        Assert.False(quanLy.DangOnline("user-la"));
    }
}
```

- [ ] **Step 5: Mở rộng `ChatHub` — join nhóm + presence khi connect/disconnect**

Trong `backend/HaloChat.Api/Hubs/ChatHub.cs`, thêm dependency và 2 override. File đầy đủ sau khi sửa:

```csharp
using System.IdentityModel.Tokens.Jwt;
using HaloChat.Api.Dto;
using HaloChat.Api.Repositories;
using HaloChat.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace HaloChat.Api.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IDichVuTinNhan _dichVuTinNhan;
    private readonly IDichVuNhom _dichVuNhom;
    private readonly IQuanLyKetNoiChat _quanLyKetNoi;
    private readonly INguoiDungRepository _khoNguoiDung;
    private readonly ILoiMoiKetBanRepository _khoLoiMoiKetBan;

    public ChatHub(
        IDichVuTinNhan dichVuTinNhan, IDichVuNhom dichVuNhom, IQuanLyKetNoiChat quanLyKetNoi,
        INguoiDungRepository khoNguoiDung, ILoiMoiKetBanRepository khoLoiMoiKetBan)
    {
        _dichVuTinNhan = dichVuTinNhan;
        _dichVuNhom = dichVuNhom;
        _quanLyKetNoi = quanLyKetNoi;
        _khoNguoiDung = khoNguoiDung;
        _khoLoiMoiKetBan = khoLoiMoiKetBan;
    }

    private string NguoiDungHienTaiId =>
        Context.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
        ?? throw new HubException("Không xác định được người dùng hiện tại.");

    public override async Task OnConnectedAsync()
    {
        var userId = NguoiDungHienTaiId;

        var cacNhom = await _dichVuNhom.LayDanhSachAsync(userId);
        foreach (var nhom in cacNhom)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "nhom-" + nhom.Id);
        }

        var laOnlineMoi = _quanLyKetNoi.ThemKetNoi(userId, Context.ConnectionId);
        if (laOnlineMoi)
        {
            await BaoTrangThaiHoatDongThayDoiAsync(userId, true);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = NguoiDungHienTaiId;
        var vuaOffline = _quanLyKetNoi.XoaKetNoi(userId, Context.ConnectionId);
        if (vuaOffline)
        {
            await BaoTrangThaiHoatDongThayDoiAsync(userId, false);
        }

        await base.OnDisconnectedAsync(exception);
    }

    private async Task BaoTrangThaiHoatDongThayDoiAsync(string userId, bool online)
    {
        var nguoiDung = await _khoNguoiDung.TimTheoIdAsync(userId);
        if (nguoiDung is null || !nguoiDung.HienThiTrangThaiHoatDong)
        {
            return;
        }

        var banBe = await _khoLoiMoiKetBan.LayBanBeAsync(userId);
        foreach (var loiMoi in banBe)
        {
            var idBan = loiMoi.NguoiGuiId == userId ? loiMoi.NguoiNhanId : loiMoi.NguoiGuiId;
            await Clients.User(idBan).SendAsync("TrangThaiHoatDongThayDoi", userId, online);
        }
    }

    public async Task<TinNhanDto> GuiTinNhan(
        string nguoiNhanId, string loaiTinNhan, string noiDungTinNhan,
        string? duongDanFile, string? tenFileGoc, long? kichThuocFile, string? loaiFile)
    {
        try
        {
            var tinNhan = await _dichVuTinNhan.GuiTinNhanAsync(
                NguoiDungHienTaiId, nguoiNhanId, loaiTinNhan, noiDungTinNhan,
                duongDanFile, tenFileGoc, kichThuocFile, loaiFile);

            await Clients.User(nguoiNhanId).SendAsync("NhanTinNhan", tinNhan);
            return tinNhan;
        }
        catch (NguoiNhanKhongTonTaiException loi)
        {
            throw new HubException(loi.Message);
        }
        catch (TinNhanKhongHopLeException loi)
        {
            throw new HubException(loi.Message);
        }
    }

    public Task DanhDauDaDoc(string nguoiGuiId) =>
        _dichVuTinNhan.DanhDauDaDocAsync(NguoiDungHienTaiId, nguoiGuiId);
}
```

Lưu ý: `GuiTinNhan`/`DanhDauDaDoc` ở bước này CHƯA đổi chữ ký (chưa nhận `nhomId`/tính `DaNhan`) — việc đó thuộc Task 6, để tách rõ 2 mối quan tâm (presence/join-nhóm ở đây, nội dung gửi tin ở Task 6) và giữ mỗi task review được độc lập.

- [ ] **Step 6: Thêm `GET /api/nguoidung/trang-thai` vào `NguoiDungController`**

Tiêm thêm `IQuanLyKetNoiChat` vào constructor:

```csharp
    private readonly IDichVuNguoiDung _dichVu;
    private readonly IQuanLyKetNoiChat _quanLyKetNoi;

    public NguoiDungController(IDichVuNguoiDung dichVu, IQuanLyKetNoiChat quanLyKetNoi)
    {
        _dichVu = dichVu;
        _quanLyKetNoi = quanLyKetNoi;
    }
```

Thêm action mới (dùng `_dichVu` để lấy `HienThiTrangThaiHoatDong` của TỪNG id qua `LayThongTinCaNhanAsync` đã có sẵn — tái dùng, không cần method mới ở service/repository):

```csharp
    [HttpGet("trang-thai")]
    public async Task<IActionResult> LayTrangThaiHoatDong([FromQuery] string ids)
    {
        if (IdHienTai is null)
        {
            return Unauthorized();
        }

        var ketQua = new Dictionary<string, bool>();
        foreach (var id in ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var hoSo = await _dichVu.LayThongTinCaNhanAsync(id);
            ketQua[id] = hoSo is not null && hoSo.HienThiTrangThaiHoatDong && _quanLyKetNoi.DangOnline(id);
        }

        return Ok(ketQua);
    }
```

Thêm `using HaloChat.Api.Services;` nếu file chưa có (đã có sẵn vì controller vốn dùng `IDichVuNguoiDung`).

- [ ] **Step 7: Cập nhật test `ChatHub` cho join-nhóm + presence**

Thêm 2 test mới vào `backend/HaloChat.Api.Tests/ChatHubTests.cs` (cần thêm `using HaloChat.Api.Dto;` nếu chưa có — đã có sẵn):

```csharp
    [Fact]
    public async Task OnConnected_LaThanhVienNhom_NhanDuocTinNhanNhomGuiTrongLucDangKetNoi()
    {
        var tokenChu = await TaoTaiKhoanVaDangNhapAsync("hubnhomchu");
        var idChu = _factory.KhoGiaLap.DanhSach.Single(nd => nd.TenTaiKhoan == "hubnhomchu").Id;
        var tokenThanhVien = await TaoTaiKhoanVaDangNhapAsync("hubnhomtv");
        var idThanhVien = _factory.KhoGiaLap.DanhSach.Single(nd => nd.TenTaiKhoan == "hubnhomtv").Id;

        using var clientTao = _factory.CreateClient();
        clientTao.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenChu);
        var nhom = await (await clientTao.PostAsJsonAsync(
            "/api/nhom", new { TenNhom = "Nhóm Hub", MoTa = (string?)null, DuongDanAnhDaiDien = (string?)null, ThanhVienIds = new[] { idThanhVien } }))
            .Content.ReadFromJsonAsync<HaloChat.Api.Dto.NhomDto>();

        await using var ketNoiThanhVien = TaoKetNoiHub(tokenThanhVien);
        var daNhan = new TaskCompletionSource();
        ketNoiThanhVien.On<object>("NhanTinNhan", _ => daNhan.SetResult());
        await ketNoiThanhVien.StartAsync();

        // Thành viên đã join group "nhom-{id}" ngay lúc OnConnectedAsync (trước khi
        // có tin nhắn nào) — xác nhận gián tiếp bằng cách người tạo gửi tin nhắn
        // (qua REST giả lập, ở đây dùng chính Hub) và thành viên nhận được realtime.
        // Task 6 mới thêm tham số nhomId vào GuiTinNhan — ở Task 5 này ta chỉ xác
        // nhận việc join group không lỗi, không gọi GuiTinNhan(nhomId) được vì
        // chưa tồn tại tham số đó. Test đầy đủ hành vi gửi tin nhóm chuyển sang
        // Task 6 (ChatHubTests bổ sung thêm ở đó); test này chỉ khẳng định
        // OnConnectedAsync không ném lỗi và kết nối thành công cho 1 user có nhóm.
        Assert.True(ketNoiThanhVien.State == Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Connected);
        Assert.NotNull(nhom);
    }

    [Fact]
    public async Task OnConnected_LaBanBe_NhanDuocSuKienTrangThaiHoatDongThayDoi()
    {
        var tokenA = await TaoTaiKhoanVaDangNhapAsync("hubpresencea");
        var idA = _factory.KhoGiaLap.DanhSach.Single(nd => nd.TenTaiKhoan == "hubpresencea").Id;
        var tokenB = await TaoTaiKhoanVaDangNhapAsync("hubpresenceb");
        var idB = _factory.KhoGiaLap.DanhSach.Single(nd => nd.TenTaiKhoan == "hubpresenceb").Id;
        _factory.KhoLoiMoiKetBanGiaLap.DanhSach.Add(new HaloChat.Api.Models.LoiMoiKetBan
        {
            NguoiGuiId = idA, NguoiNhanId = idB, TrangThai = HaloChat.Api.Models.TrangThaiLoiMoiKetBan.DaChapNhan,
        });

        await using var ketNoiA = TaoKetNoiHub(tokenA);
        await ketNoiA.StartAsync();

        var daNhanThayDoi = new TaskCompletionSource();
        (string UserId, bool Online)? suKienNhanDuoc = null;
        await using var ketNoiB = TaoKetNoiHub(tokenB);
        ketNoiB.On<string, bool>("TrangThaiHoatDongThayDoi", (userId, online) =>
        {
            suKienNhanDuoc = (userId, online);
            daNhanThayDoi.SetResult();
        });
        await ketNoiB.StartAsync();

        // B online sau A và là bạn của A => A phải nhận sự kiện A thấy B online.
        await daNhanThayDoi.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.NotNull(suKienNhanDuoc);
        Assert.Equal(idB, suKienNhanDuoc!.Value.UserId);
        Assert.True(suKienNhanDuoc.Value.Online);
    }
```

Ghi chú implementer: test thứ 2 lắng nghe trên `ketNoiB` (không phải `ketNoiA`) vì sự kiện được đẩy TỚI bạn bè của user VỪA online — khi B connect sau, server đẩy `TrangThaiHoatDongThayDoi(idB, true)` tới A; nhưng vì thứ tự IHubContext/`Clients.User` gửi bất đồng bộ, cách chắc chắn nhất để test là kiểm tra đúng nội dung sự kiện nhận được ở phía nhận (A) — nếu implementer thấy assertion đặt nhầm phía (nghe trên B thay vì A) khi viết thật, hãy sửa `ketNoiB.On(...)` thành `ketNoiA.On(...)` và giữ nguyên logic still-connect B sau — đây là điểm cần tự kiểm chứng bằng cách chạy test thật, không suy đoán suông.

- [ ] **Step 8: Build và test**

Run: `dotnet build backend/HaloChat.sln`
Expected: 0 lỗi.

Run: `dotnet test backend/HaloChat.sln`
Expected: toàn bộ pass. Nếu 2 test Hub mới ở Step 7 flaky do timing, tăng `TimeSpan.FromSeconds(5)` lên `10` và thử lại — không giảm assertion để né lỗi.

- [ ] **Step 9: Commit**

```bash
git add -A
git commit -m "Them IQuanLyKetNoiChat + presence online/offline that qua ChatHub"
```

---

## Task 6: Mở rộng `GuiTinNhan`/`DanhDauDaDoc` cho tin nhắn nhóm + `DaNhan` (trạng thái "đã nhận")

**Files:**
- Modify: `backend/HaloChat.Api/Models/TinNhan.cs` (thêm `DaNhan`)
- Modify: `backend/HaloChat.Api/Dto/TinNhanDto.cs` (thêm `DaNhan`)
- Modify: `backend/HaloChat.Api/Repositories/ITinNhanRepository.cs` + `TinNhanRepository.cs` (thêm 2 method nhóm)
- Modify: `backend/HaloChat.Api.Tests/Fakes/TinNhanGiaLap.cs`
- Modify: `backend/HaloChat.Api/Services/IDichVuTinNhan.cs` + `DichVuTinNhan.cs`
- Modify: `backend/HaloChat.Api/Hubs/ChatHub.cs` (đổi chữ ký `GuiTinNhan`/`DanhDauDaDoc`)
- Modify: `backend/HaloChat.Api/Controllers/TinNhanController.cs` (thêm `GET /api/tinnhan/nhom/{id}`)
- Modify: `backend/HaloChat.Api.Tests/ChatHubTests.cs` (sửa lời gọi `GuiTinNhan` theo chữ ký mới + test mới cho nhóm/`DaNhan`)
- Create: `backend/HaloChat.Api.Tests/TinNhanControllerTests.cs` nếu chưa có test cho `GET /api/tinnhan/nhom/{id}` — kiểm tra file đã tồn tại trước, nếu có thì thêm test vào cuối class hiện có thay vì tạo file mới trùng tên.

**Interfaces:**
- Consumes: `INhomRepository.TimTheoIdAsync` (Task 3), `IQuanLyKetNoiChat.DangOnline` (Task 5).
- Produces: `IDichVuTinNhan.GuiTinNhanAsync(string nguoiGuiId, string? nguoiNhanId, string? nhomId, string loaiTinNhan, string noiDungTinNhan, string? duongDanFile, string? tenFileGoc, long? kichThuocFile, string? loaiFile)` (đổi chữ ký — `nguoiNhanId` giờ `string?`, thêm `nhomId`), `LayLichSuNhomAsync(string nguoiHienTaiId, string nhomId, string? truocId, int soLuong)`, `DanhDauDaDocNhomAsync(string nguoiHienTaiId, string nhomId)`. `TinNhanDto` thêm field `DaNhan: bool` ở cuối. Hub method `GuiTinNhan(string? nguoiNhanId, string? nhomId, string loaiTinNhan, string noiDungTinNhan, string? duongDanFile, string? tenFileGoc, long? kichThuocFile, string? loaiFile)` — Task 9 (frontend) gọi `ketNoi.invoke('GuiTinNhan', nguoiNhanId, nhomId, loaiTinNhan, ...)` đúng thứ tự này (1 trong 2 tham số đầu luôn `null`).

- [ ] **Step 1: Thêm `DaNhan` vào model và DTO**

Trong `backend/HaloChat.Api/Models/TinNhan.cs`, thêm ngay dưới `public bool DaDoc { get; set; } = false;`:

```csharp
    // [GĐ5b-2] "Đã nhận" — true nếu người nhận đang online (>=1 kết nối
    // SignalR mở) tại thời điểm gửi. Chỉ có ý nghĩa với tin nhắn 1-1
    // (NguoiNhanId khác null); tin nhắn nhóm luôn để false, không track.
    public bool DaNhan { get; set; } = false;
```

`backend/HaloChat.Api/Dto/TinNhanDto.cs` — nội dung mới:

```csharp
namespace HaloChat.Api.Dto;

public record TinNhanDto(
    string Id,
    string NguoiGuiId,
    string? NguoiNhanId,
    string? NhomId,
    string LoaiTinNhan,
    string NoiDungTinNhan,
    string? DuongDanFile,
    string? TenFileGoc,
    long? KichThuocFile,
    string? LoaiFile,
    bool DaDoc,
    bool DaNhan,
    DateTime ThoiGianTao);
```

(Thêm `NhomId` vào DTO — trước đây chỉ model có field này, DTO chưa expose; frontend Task 9 cần biết tin nhắn thuộc nhóm nào khi nhận qua event `NhanTinNhan` dùng chung cho cả 2 loại hội thoại.)

- [ ] **Step 2: Thêm 2 method vào `ITinNhanRepository`/`TinNhanRepository`**

Thêm vào interface:

```csharp
    /// <summary>Lịch sử tin nhắn của 1 nhóm, mới nhất trước, phân trang lùi giống LayLichSuTheoNguoiDungAsync.</summary>
    Task<List<TinNhan>> LayLichSuNhomAsync(string nhomId, string? truocId, int soLuong);

    /// <summary>Đánh dấu đã đọc mọi tin nhắn của 1 nhóm (đơn giản hóa: không phân biệt theo từng thành viên).</summary>
    Task DanhDauDaDocNhomAsync(string nhomId);
```

Thêm vào `TinNhanRepository`:

```csharp
    public async Task<List<TinNhan>> LayLichSuNhomAsync(string nhomId, string? truocId, int soLuong)
    {
        var boLocNhom = Builders<TinNhan>.Filter.Eq(t => t.NhomId, nhomId);
        var boLoc = string.IsNullOrEmpty(truocId)
            ? boLocNhom
            : Builders<TinNhan>.Filter.And(boLocNhom, Builders<TinNhan>.Filter.Lt(t => t.Id, truocId));

        return await _collection.Find(boLoc)
            .SortByDescending(t => t.Id)
            .Limit(soLuong)
            .ToListAsync();
    }

    public async Task DanhDauDaDocNhomAsync(string nhomId)
    {
        var boLoc = Builders<TinNhan>.Filter.And(
            Builders<TinNhan>.Filter.Eq(t => t.NhomId, nhomId),
            Builders<TinNhan>.Filter.Eq(t => t.DaDoc, false));
        var capNhat = Builders<TinNhan>.Update.Set(t => t.DaDoc, true);
        await _collection.UpdateManyAsync(boLoc, capNhat);
    }
```

- [ ] **Step 3: Cập nhật fake `TinNhanGiaLap`**

Thêm vào `backend/HaloChat.Api.Tests/Fakes/TinNhanGiaLap.cs`:

```csharp
    public Task<List<TinNhan>> LayLichSuNhomAsync(string nhomId, string? truocId, int soLuong)
    {
        var ketQua = DanhSach
            .Where(t => t.NhomId == nhomId)
            .Where(t => truocId is null || string.CompareOrdinal(t.Id, truocId) < 0)
            .OrderByDescending(t => t.Id)
            .Take(soLuong)
            .ToList();
        return Task.FromResult(ketQua);
    }

    public Task DanhDauDaDocNhomAsync(string nhomId)
    {
        foreach (var t in DanhSach.Where(t => t.NhomId == nhomId))
        {
            t.DaDoc = true;
        }
        return Task.CompletedTask;
    }
```

- [ ] **Step 4: Mở rộng `IDichVuTinNhan`/`DichVuTinNhan`**

`IDichVuTinNhan.cs` — nội dung mới:

```csharp
using HaloChat.Api.Dto;

namespace HaloChat.Api.Services;

public interface IDichVuTinNhan
{
    Task<TinNhanDto> GuiTinNhanAsync(
        string nguoiGuiId, string? nguoiNhanId, string? nhomId, string loaiTinNhan, string noiDungTinNhan,
        string? duongDanFile, string? tenFileGoc, long? kichThuocFile, string? loaiFile);

    Task<List<TinNhanDto>> LayLichSuAsync(string nguoiHienTaiId, string nguoiKiaId, string? truocId, int soLuong);

    Task<List<TinNhanDto>> LayLichSuNhomAsync(string nguoiHienTaiId, string nhomId, string? truocId, int soLuong);

    Task DanhDauDaDocAsync(string nguoiHienTaiId, string nguoiGuiId);

    Task DanhDauDaDocNhomAsync(string nguoiHienTaiId, string nhomId);

    Task<List<HoiThoaiTomTatDto>> LayDanhSachHoiThoaiAsync(string nguoiDungId);
}
```

`DichVuTinNhan.cs` — thêm 2 dependency mới vào constructor và cập nhật `GuiTinNhanAsync`:

```csharp
using HaloChat.Api.Dto;
using HaloChat.Api.Models;
using HaloChat.Api.Repositories;
using MongoDB.Bson;

namespace HaloChat.Api.Services;

public class DichVuTinNhan : IDichVuTinNhan
{
    private static readonly System.Text.RegularExpressions.Regex MauDuongDanFileHopLe = new(
        @"^/uploads/[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\.(jpg|jpeg|png|gif|webp|pdf|docx|xlsx|zip)$",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    private readonly ITinNhanRepository _khoTinNhan;
    private readonly INguoiDungRepository _khoNguoiDung;
    private readonly ILoiMoiKetBanRepository _khoLoiMoiKetBan;
    private readonly INhomRepository _khoNhom;
    private readonly IQuanLyKetNoiChat _quanLyKetNoi;

    public DichVuTinNhan(
        ITinNhanRepository khoTinNhan, INguoiDungRepository khoNguoiDung, ILoiMoiKetBanRepository khoLoiMoiKetBan,
        INhomRepository khoNhom, IQuanLyKetNoiChat quanLyKetNoi)
    {
        _khoTinNhan = khoTinNhan;
        _khoNguoiDung = khoNguoiDung;
        _khoLoiMoiKetBan = khoLoiMoiKetBan;
        _khoNhom = khoNhom;
        _quanLyKetNoi = quanLyKetNoi;
    }

    public async Task<TinNhanDto> GuiTinNhanAsync(
        string nguoiGuiId, string? nguoiNhanId, string? nhomId, string loaiTinNhan, string noiDungTinNhan,
        string? duongDanFile, string? tenFileGoc, long? kichThuocFile, string? loaiFile)
    {
        if (string.IsNullOrEmpty(nguoiNhanId) == string.IsNullOrEmpty(nhomId))
        {
            throw new TinNhanKhongHopLeException("Phải chỉ định đúng 1 trong 2: người nhận hoặc nhóm.");
        }

        if (!Enum.TryParse<LoaiTinNhan>(loaiTinNhan, ignoreCase: true, out var loai))
        {
            throw new TinNhanKhongHopLeException($"Loại tin nhắn không hợp lệ: {loaiTinNhan}.");
        }

        if (loai == LoaiTinNhan.Text && string.IsNullOrWhiteSpace(noiDungTinNhan))
        {
            throw new TinNhanKhongHopLeException("Nội dung tin nhắn không được để trống.");
        }

        if (loai != LoaiTinNhan.Text)
        {
            if (string.IsNullOrWhiteSpace(duongDanFile) || !MauDuongDanFileHopLe.IsMatch(duongDanFile))
            {
                throw new TinNhanKhongHopLeException("Đường dẫn file không hợp lệ.");
            }

            if (kichThuocFile is < 0)
            {
                throw new TinNhanKhongHopLeException("Kích thước file không hợp lệ.");
            }
        }

        var tinNhan = new TinNhan
        {
            NguoiGuiId = nguoiGuiId,
            LoaiTinNhan = loai,
            NoiDungTinNhan = noiDungTinNhan ?? string.Empty,
            DuongDanFile = duongDanFile,
            TenFileGoc = tenFileGoc,
            KichThuocFile = kichThuocFile,
            LoaiFile = loaiFile,
        };

        if (nhomId is not null)
        {
            if (!ObjectId.TryParse(nhomId, out _))
            {
                throw new NhomKhongTonTaiException();
            }

            var nhom = await _khoNhom.TimTheoIdAsync(nhomId) ?? throw new NhomKhongTonTaiException();
            if (!nhom.ThanhVienIds.Contains(nguoiGuiId))
            {
                throw new KhongPhaiThanhVienNhomException();
            }

            tinNhan.NhomId = nhomId;
        }
        else
        {
            if (!ObjectId.TryParse(nguoiNhanId, out _))
            {
                throw new NguoiNhanKhongTonTaiException(nguoiNhanId!);
            }

            var nguoiNhan = await _khoNguoiDung.TimTheoIdAsync(nguoiNhanId!);
            if (nguoiNhan is null)
            {
                throw new NguoiNhanKhongTonTaiException(nguoiNhanId!);
            }

            if (nguoiGuiId != nguoiNhanId)
            {
                var laBanBe = await _khoLoiMoiKetBan.LaBanBeAsync(nguoiGuiId, nguoiNhanId!);
                if (!laBanBe && !nguoiNhan.ChoPhepTinNhanTuNguoiLa)
                {
                    throw new TinNhanKhongHopLeException("Người này chỉ nhận tin nhắn từ bạn bè. Hãy gửi lời mời kết bạn trước.");
                }
            }

            tinNhan.NguoiNhanId = nguoiNhanId;
            tinNhan.DaNhan = _quanLyKetNoi.DangOnline(nguoiNhanId!);
        }

        await _khoTinNhan.ThemMoiAsync(tinNhan);
        return AnhXaDto(tinNhan);
    }

    public async Task<List<TinNhanDto>> LayLichSuAsync(string nguoiHienTaiId, string nguoiKiaId, string? truocId, int soLuong)
    {
        var lichSu = await _khoTinNhan.LayLichSuTheoNguoiDungAsync(nguoiHienTaiId, nguoiKiaId, truocId, soLuong);
        return lichSu.Select(AnhXaDto).ToList();
    }

    public async Task<List<TinNhanDto>> LayLichSuNhomAsync(string nguoiHienTaiId, string nhomId, string? truocId, int soLuong)
    {
        var nhom = await _khoNhom.TimTheoIdAsync(nhomId) ?? throw new NhomKhongTonTaiException();
        if (!nhom.ThanhVienIds.Contains(nguoiHienTaiId))
        {
            throw new KhongPhaiThanhVienNhomException();
        }

        var lichSu = await _khoTinNhan.LayLichSuNhomAsync(nhomId, truocId, soLuong);
        return lichSu.Select(AnhXaDto).ToList();
    }

    public Task DanhDauDaDocAsync(string nguoiHienTaiId, string nguoiGuiId) =>
        _khoTinNhan.DanhDauDaDocAsync(nguoiGuiId, nguoiHienTaiId);

    public async Task DanhDauDaDocNhomAsync(string nguoiHienTaiId, string nhomId)
    {
        var nhom = await _khoNhom.TimTheoIdAsync(nhomId) ?? throw new NhomKhongTonTaiException();
        if (!nhom.ThanhVienIds.Contains(nguoiHienTaiId))
        {
            throw new KhongPhaiThanhVienNhomException();
        }

        await _khoTinNhan.DanhDauDaDocNhomAsync(nhomId);
    }

    public async Task<List<HoiThoaiTomTatDto>> LayDanhSachHoiThoaiAsync(string nguoiDungId)
    {
        var tatCaTinNhan = await _khoTinNhan.LayTatCaLienQuanAsync(nguoiDungId);
        var tatCaNguoiDung = await _khoNguoiDung.LayTatCaAsync();
        var mapNguoiDung = tatCaNguoiDung.ToDictionary(nd => nd.Id);

        var ketQua = new List<HoiThoaiTomTatDto>();
        var daXuLy = new HashSet<string>();

        foreach (var tn in tatCaTinNhan)
        {
            var idKia = tn.NguoiGuiId == nguoiDungId ? tn.NguoiNhanId : tn.NguoiGuiId;
            if (idKia is null || !daXuLy.Add(idKia))
            {
                continue;
            }

            if (!mapNguoiDung.TryGetValue(idKia, out var nguoiKia))
            {
                continue;
            }

            var soChuaDoc = tatCaTinNhan.Count(t => t.NguoiGuiId == idKia && t.NguoiNhanId == nguoiDungId && !t.DaDoc);
            var xemTruoc = tn.LoaiTinNhan == LoaiTinNhan.Text
                ? tn.NoiDungTinNhan
                : tn.LoaiTinNhan == LoaiTinNhan.Anh ? "[Ảnh]" : "[File]";

            ketQua.Add(new HoiThoaiTomTatDto(
                new NguoiDungTomTatDto(nguoiKia.Id, nguoiKia.TenTaiKhoan, nguoiKia.Email),
                xemTruoc,
                tn.ThoiGianTao,
                soChuaDoc));
        }

        return ketQua;
    }

    private static TinNhanDto AnhXaDto(TinNhan t) => new(
        t.Id, t.NguoiGuiId, t.NguoiNhanId, t.NhomId, t.LoaiTinNhan.ToString(), t.NoiDungTinNhan,
        t.DuongDanFile, t.TenFileGoc, t.KichThuocFile, t.LoaiFile, t.DaDoc, t.DaNhan, t.ThoiGianTao);
}
```

(`LayDanhSachHoiThoaiAsync` giữ nguyên y hệt — không đổi, đúng Global Constraints.)

- [ ] **Step 5: Đổi chữ ký Hub method trong `ChatHub.cs`**

Thay 2 method cuối file (giữ nguyên phần `OnConnectedAsync`/`OnDisconnectedAsync` từ Task 5):

```csharp
    public async Task<TinNhanDto> GuiTinNhan(
        string? nguoiNhanId, string? nhomId, string loaiTinNhan, string noiDungTinNhan,
        string? duongDanFile, string? tenFileGoc, long? kichThuocFile, string? loaiFile)
    {
        try
        {
            var tinNhan = await _dichVuTinNhan.GuiTinNhanAsync(
                NguoiDungHienTaiId, nguoiNhanId, nhomId, loaiTinNhan, noiDungTinNhan,
                duongDanFile, tenFileGoc, kichThuocFile, loaiFile);

            if (nhomId is not null)
            {
                await Clients.Group("nhom-" + nhomId).SendAsync("NhanTinNhan", tinNhan);
            }
            else
            {
                await Clients.User(nguoiNhanId!).SendAsync("NhanTinNhan", tinNhan);
            }

            return tinNhan;
        }
        catch (NguoiNhanKhongTonTaiException loi)
        {
            throw new HubException(loi.Message);
        }
        catch (TinNhanKhongHopLeException loi)
        {
            throw new HubException(loi.Message);
        }
        catch (NhomKhongTonTaiException loi)
        {
            throw new HubException(loi.Message);
        }
        catch (KhongPhaiThanhVienNhomException loi)
        {
            throw new HubException(loi.Message);
        }
    }

    public Task DanhDauDaDoc(string? nguoiGuiId, string? nhomId) =>
        nhomId is not null
            ? _dichVuTinNhan.DanhDauDaDocNhomAsync(NguoiDungHienTaiId, nhomId)
            : _dichVuTinNhan.DanhDauDaDocAsync(NguoiDungHienTaiId, nguoiGuiId!);
```

- [ ] **Step 6: Thêm `GET /api/tinnhan/nhom/{id}` vào `TinNhanController`**

```csharp
    [HttpGet("nhom/{id}")]
    public async Task<IActionResult> LayLichSuNhom(string id, [FromQuery] string? truoc, [FromQuery] int soLuong = 30)
    {
        if (IdHienTai is null)
        {
            return Unauthorized();
        }

        if (!ObjectId.TryParse(id, out _))
        {
            return BadRequest(new { thongBao = "Id nhóm không hợp lệ." });
        }

        try
        {
            var soLuongThucTe = Math.Clamp(soLuong, 1, 100);
            var lichSu = await _dichVuTinNhan.LayLichSuNhomAsync(IdHienTai, id, truoc, soLuongThucTe);
            return Ok(lichSu);
        }
        catch (HaloChat.Api.Services.NhomKhongTonTaiException loi)
        {
            return NotFound(new { thongBao = loi.Message });
        }
        catch (HaloChat.Api.Services.KhongPhaiThanhVienNhomException loi)
        {
            return StatusCode(403, new { thongBao = loi.Message });
        }
    }
```

- [ ] **Step 7: Sửa 2 lời gọi `GuiTinNhan` đã có trong `ChatHubTests.cs`**

Đổi:

```csharp
        var tinNhanGui = await ketNoiA.InvokeAsync<TinNhanDto>(
            "GuiTinNhan", idNguoiB, "Text", "Chào bạn", null, null, null, null);
```

thành:

```csharp
        var tinNhanGui = await ketNoiA.InvokeAsync<TinNhanDto>(
            "GuiTinNhan", idNguoiB, null, "Text", "Chào bạn", null, null, null, null);
```

Và đổi:

```csharp
        await Assert.ThrowsAsync<HubException>(() =>
            ketNoi.InvokeAsync<TinNhanDto>(
                "GuiTinNhan", "000000000000000000000000", "Text", "Xin chào", null, null, null, null));
```

thành:

```csharp
        await Assert.ThrowsAsync<HubException>(() =>
            ketNoi.InvokeAsync<TinNhanDto>(
                "GuiTinNhan", "000000000000000000000000", null, "Text", "Xin chào", null, null, null, null));
```

Đồng thời hoàn thiện test `OnConnected_LaThanhVienNhom_...` đã tạo dở ở Task 5 (Step 7 của Task 5) — sau khi có chữ ký `nhomId` thật, thay đoạn comment "chưa gọi được GuiTinNhan(nhomId)" bằng lời gọi thật:

```csharp
    [Fact]
    public async Task GuiTinNhan_ChoNhom_ThanhVienKhacNhanDuocRealtime()
    {
        var tokenChu = await TaoTaiKhoanVaDangNhapAsync("hubnhomchu2");
        var tokenThanhVien = await TaoTaiKhoanVaDangNhapAsync("hubnhomtv2");
        var idThanhVien = _factory.KhoGiaLap.DanhSach.Single(nd => nd.TenTaiKhoan == "hubnhomtv2").Id;

        using var clientTao = _factory.CreateClient();
        clientTao.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenChu);
        var nhom = await (await clientTao.PostAsJsonAsync(
            "/api/nhom", new { TenNhom = "Nhóm Hub 2", MoTa = (string?)null, DuongDanAnhDaiDien = (string?)null, ThanhVienIds = new[] { idThanhVien } }))
            .Content.ReadFromJsonAsync<HaloChat.Api.Dto.NhomDto>();

        await using var ketNoiThanhVien = TaoKetNoiHub(tokenThanhVien);
        TinNhanDto? nhanDuoc = null;
        var daNhan = new TaskCompletionSource();
        ketNoiThanhVien.On<TinNhanDto>("NhanTinNhan", tn => { nhanDuoc = tn; daNhan.SetResult(); });
        await ketNoiThanhVien.StartAsync();

        await using var ketNoiChu = TaoKetNoiHub(tokenChu);
        await ketNoiChu.StartAsync();
        await ketNoiChu.InvokeAsync<TinNhanDto>("GuiTinNhan", null, nhom!.Id, "Text", "Chào nhóm", null, null, null, null);

        await daNhan.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.NotNull(nhanDuoc);
        Assert.Equal("Chào nhóm", nhanDuoc!.NoiDungTinNhan);
        Assert.Equal(nhom.Id, nhanDuoc.NhomId);
    }
```

Xóa hẳn nội dung tạm/comment-only của `OnConnected_LaThanhVienNhom_NhanDuocTinNhanNhomGuiTrongLucDangKetNoi` viết ở Task 5 và thay bằng test `GuiTinNhan_ChoNhom_ThanhVienKhacNhanDuocRealtime` ở trên (cùng mục đích, giờ verify được đầy đủ vì `nhomId` đã tồn tại thật) — không giữ cả 2 để tránh trùng lặp.

- [ ] **Step 8: Build và test**

Run: `dotnet build backend/HaloChat.sln`
Expected: 0 lỗi. Nếu còn lỗi biên dịch do nơi khác gọi `GuiTinNhanAsync`/`TinNhanDto` theo chữ ký cũ (vd chỗ nào đó trong test khác), sửa theo đúng chữ ký mới ở Step 4.

Run: `dotnet test backend/HaloChat.sln`
Expected: toàn bộ pass.

- [ ] **Step 9: Commit**

```bash
git add -A
git commit -m "Mo rong GuiTinNhan/DanhDauDaDoc cho tin nhan nhom + them DaNhan (da nhan)"
```

---

## Task 7: Thêm/xóa thành viên nhóm + rời nhóm (realtime qua Hub)

**Files:**
- Modify: `backend/HaloChat.Api/Services/NgoaiLeNhom.cs` (thêm exception)
- Create: `backend/HaloChat.Api/Dto/ThemThanhVienRequest.cs`
- Create: `backend/HaloChat.Api/Dto/KetQuaRoiNhomDto.cs`
- Modify: `backend/HaloChat.Api/Services/IDichVuNhom.cs` + `DichVuNhom.cs`
- Modify: `backend/HaloChat.Api/Controllers/NhomController.cs`
- Modify: `backend/HaloChat.Api.Tests/NhomControllerTests.cs`

**Interfaces:**
- Consumes: `IQuanLyKetNoiChat.LayConnectionIds` (Task 5), `IHubContext<ChatHub>` (đã dùng ở `KetBanController` từ GĐ5b-1, cùng mẫu).
- Produces: `IDichVuNhom.ThemThanhVienAsync`, `XoaThanhVienAsync`, `RoiNhomAsync` — không có task nào sau dùng lại các method này (đây là task cuối cùng của backend logic nhóm).

- [ ] **Step 1: Thêm exception mới**

Thêm vào cuối `backend/HaloChat.Api/Services/NgoaiLeNhom.cs`:

```csharp

/// <summary>Ném ra khi cố xóa người tạo (admin) khỏi nhóm bằng thao tác xóa thành viên thường (phải dùng rời nhóm).</summary>
public class KhongTheXoaNguoiTaoException : Exception
{
    public KhongTheXoaNguoiTaoException() : base("Không thể xóa người tạo nhóm — người tạo phải tự rời nhóm.")
    {
    }
}
```

- [ ] **Step 2: Thêm DTO**

`backend/HaloChat.Api/Dto/ThemThanhVienRequest.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace HaloChat.Api.Dto;

public record ThemThanhVienRequest([Required(ErrorMessage = "Vui lòng chọn người để thêm.")] string ThanhVienId);
```

`backend/HaloChat.Api/Dto/KetQuaRoiNhomDto.cs`:

```csharp
namespace HaloChat.Api.Dto;

public record KetQuaRoiNhomDto(bool DaGiaiTan, List<string> ThanhVienConLai);
```

- [ ] **Step 3: Mở rộng `IDichVuNhom`**

Thêm vào interface:

```csharp
    Task<NhomDto> ThemThanhVienAsync(string nguoiGoiId, string nhomId, string thanhVienMoiId);
    Task<NhomDto> XoaThanhVienAsync(string nguoiGoiId, string nhomId, string thanhVienId);
    Task<KetQuaRoiNhomDto> RoiNhomAsync(string nguoiGoiId, string nhomId);
```

- [ ] **Step 4: Cài đặt trong `DichVuNhom`**

Thêm vào cuối class (trước dấu `}` cuối, sau method `AnhXaDtoAsync` hiện có):

```csharp
    public async Task<NhomDto> ThemThanhVienAsync(string nguoiGoiId, string nhomId, string thanhVienMoiId)
    {
        var nhom = await LayNhomKiemTraQuanTriAsync(nguoiGoiId, nhomId);

        if (!ObjectId.TryParse(thanhVienMoiId, out _) || await _khoNguoiDung.TimTheoIdAsync(thanhVienMoiId) is null)
        {
            throw new ThanhVienKhongTonTaiException(thanhVienMoiId);
        }

        if (!nhom.ThanhVienIds.Contains(thanhVienMoiId))
        {
            await _khoNhom.ThemThanhVienAsync(nhomId, thanhVienMoiId);
            nhom.ThanhVienIds.Add(thanhVienMoiId);
        }

        return await AnhXaDtoAsync(nhom);
    }

    public async Task<NhomDto> XoaThanhVienAsync(string nguoiGoiId, string nhomId, string thanhVienId)
    {
        var nhom = await LayNhomKiemTraQuanTriAsync(nguoiGoiId, nhomId);

        if (thanhVienId == nhom.NguoiTaoId)
        {
            throw new KhongTheXoaNguoiTaoException();
        }

        await _khoNhom.XoaThanhVienAsync(nhomId, thanhVienId);
        nhom.ThanhVienIds.Remove(thanhVienId);

        return await AnhXaDtoAsync(nhom);
    }

    public async Task<KetQuaRoiNhomDto> RoiNhomAsync(string nguoiGoiId, string nhomId)
    {
        var nhom = await LayNhomKiemTraThanhVienAsync(nguoiGoiId, nhomId);

        if (nguoiGoiId == nhom.NguoiTaoId)
        {
            var thanhVienConLai = nhom.ThanhVienIds.Where(id => id != nguoiGoiId).ToList();
            await _khoNhom.XoaNhomAsync(nhomId);
            return new KetQuaRoiNhomDto(true, thanhVienConLai);
        }

        await _khoNhom.XoaThanhVienAsync(nhomId, nguoiGoiId);
        return new KetQuaRoiNhomDto(false, new List<string>());
    }
```

- [ ] **Step 5: Mở rộng `NhomController`**

Đổi constructor để tiêm thêm `IHubContext<ChatHub>` và `IQuanLyKetNoiChat`:

```csharp
using System.IdentityModel.Tokens.Jwt;
using HaloChat.Api.Dto;
using HaloChat.Api.Hubs;
using HaloChat.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace HaloChat.Api.Controllers;

[ApiController]
[Route("api/nhom")]
[Authorize]
public class NhomController : ControllerBase
{
    private readonly IDichVuNhom _dichVu;
    private readonly IHubContext<ChatHub> _hub;
    private readonly IQuanLyKetNoiChat _quanLyKetNoi;

    public NhomController(IDichVuNhom dichVu, IHubContext<ChatHub> hub, IQuanLyKetNoiChat quanLyKetNoi)
    {
        _dichVu = dichVu;
        _hub = hub;
        _quanLyKetNoi = quanLyKetNoi;
    }

    private string? IdHienTai => User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
```

(Các action `TaoNhom`/`LayDanhSach`/`LayChiTiet`/`CapNhat` từ Task 4 giữ nguyên bên dưới, không đổi.) Thêm 3 action mới vào cuối class:

```csharp
    [HttpPost("{id}/thanh-vien")]
    public async Task<IActionResult> ThemThanhVien(string id, [FromBody] ThemThanhVienRequest yeuCau)
    {
        if (IdHienTai is null)
        {
            return Unauthorized();
        }

        try
        {
            var nhom = await _dichVu.ThemThanhVienAsync(IdHienTai, id, yeuCau.ThanhVienId);

            foreach (var connId in _quanLyKetNoi.LayConnectionIds(yeuCau.ThanhVienId))
            {
                await _hub.Groups.AddToGroupAsync(connId, "nhom-" + id);
            }
            await _hub.Clients.User(yeuCau.ThanhVienId).SendAsync("DuocThemVaoNhom", nhom);

            return Ok(nhom);
        }
        catch (NhomKhongTonTaiException loi)
        {
            return NotFound(new { thongBao = loi.Message });
        }
        catch (KhongCoQuyenQuanTriNhomException loi)
        {
            return StatusCode(403, new { thongBao = loi.Message });
        }
        catch (ThanhVienKhongTonTaiException loi)
        {
            return BadRequest(new { thongBao = loi.Message });
        }
    }

    [HttpDelete("{id}/thanh-vien/{userId}")]
    public async Task<IActionResult> XoaThanhVien(string id, string userId)
    {
        if (IdHienTai is null)
        {
            return Unauthorized();
        }

        try
        {
            var nhom = await _dichVu.XoaThanhVienAsync(IdHienTai, id, userId);

            foreach (var connId in _quanLyKetNoi.LayConnectionIds(userId))
            {
                await _hub.Groups.RemoveFromGroupAsync(connId, "nhom-" + id);
            }
            await _hub.Clients.User(userId).SendAsync("BiXoaKhoiNhom", id);

            return Ok(nhom);
        }
        catch (NhomKhongTonTaiException loi)
        {
            return NotFound(new { thongBao = loi.Message });
        }
        catch (KhongCoQuyenQuanTriNhomException loi)
        {
            return StatusCode(403, new { thongBao = loi.Message });
        }
        catch (KhongTheXoaNguoiTaoException loi)
        {
            return BadRequest(new { thongBao = loi.Message });
        }
    }

    [HttpPost("{id}/roi-nhom")]
    public async Task<IActionResult> RoiNhom(string id)
    {
        if (IdHienTai is null)
        {
            return Unauthorized();
        }

        try
        {
            var ketQua = await _dichVu.RoiNhomAsync(IdHienTai, id);

            if (ketQua.DaGiaiTan)
            {
                foreach (var thanhVienId in ketQua.ThanhVienConLai)
                {
                    await _hub.Clients.User(thanhVienId).SendAsync("NhomDaGiaiTan", id);
                }
            }

            return Ok(new { thongBao = ketQua.DaGiaiTan ? "Nhóm đã được giải tán." : "Đã rời nhóm." });
        }
        catch (NhomKhongTonTaiException loi)
        {
            return NotFound(new { thongBao = loi.Message });
        }
        catch (KhongPhaiThanhVienNhomException loi)
        {
            return StatusCode(403, new { thongBao = loi.Message });
        }
    }
```

- [ ] **Step 6: Thêm test tích hợp vào `NhomControllerTests.cs`**

```csharp
    [Fact]
    public async Task ThemThanhVien_LaAdmin_ThanhCong()
    {
        var (tokenAdmin, _) = await TaoTaiKhoanVaDangNhapAsync("nhomthem1");
        var (_, idMoi) = await TaoTaiKhoanVaDangNhapAsync("nhomthem2");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenAdmin);
        var nhom = await (await _client.PostAsJsonAsync("/api/nhom", new TaoNhomRequest("Nhóm thêm", null, null, new List<string>())))
            .Content.ReadFromJsonAsync<NhomDto>();

        var phanHoi = await _client.PostAsJsonAsync($"/api/nhom/{nhom!.Id}/thanh-vien", new ThemThanhVienRequest(idMoi));

        Assert.Equal(HttpStatusCode.OK, phanHoi.StatusCode);
        var nhomSau = await phanHoi.Content.ReadFromJsonAsync<NhomDto>();
        Assert.Contains(nhomSau!.ThanhVien, tv => tv.Id == idMoi);
    }

    [Fact]
    public async Task ThemThanhVien_KhongPhaiAdmin_TraVe403()
    {
        var (tokenAdmin, _) = await TaoTaiKhoanVaDangNhapAsync("nhomthem3");
        var (tokenKhac, idKhac) = await TaoTaiKhoanVaDangNhapAsync("nhomthem4");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenAdmin);
        var nhom = await (await _client.PostAsJsonAsync("/api/nhom", new TaoNhomRequest("Nhóm X2", null, null, new List<string>())))
            .Content.ReadFromJsonAsync<NhomDto>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenKhac);
        var phanHoi = await _client.PostAsJsonAsync($"/api/nhom/{nhom!.Id}/thanh-vien", new ThemThanhVienRequest(idKhac));

        Assert.Equal(HttpStatusCode.Forbidden, phanHoi.StatusCode);
    }

    [Fact]
    public async Task XoaThanhVien_XoaNguoiTao_TraVe400()
    {
        var (tokenAdmin, idAdmin) = await TaoTaiKhoanVaDangNhapAsync("nhomxoa1");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenAdmin);
        var nhom = await (await _client.PostAsJsonAsync("/api/nhom", new TaoNhomRequest("Nhóm xóa", null, null, new List<string>())))
            .Content.ReadFromJsonAsync<NhomDto>();

        var phanHoi = await _client.DeleteAsync($"/api/nhom/{nhom!.Id}/thanh-vien/{idAdmin}");

        Assert.Equal(HttpStatusCode.BadRequest, phanHoi.StatusCode);
    }

    [Fact]
    public async Task RoiNhom_ThanhVienThuong_KhongGiaiTanNhom()
    {
        var (tokenAdmin, _) = await TaoTaiKhoanVaDangNhapAsync("nhomroi1");
        var (tokenTv, idTv) = await TaoTaiKhoanVaDangNhapAsync("nhomroi2");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenAdmin);
        var nhom = await (await _client.PostAsJsonAsync("/api/nhom", new TaoNhomRequest("Nhóm rời", null, null, new List<string> { idTv })))
            .Content.ReadFromJsonAsync<NhomDto>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenTv);
        var phanHoi = await _client.PostAsync($"/api/nhom/{nhom!.Id}/roi-nhom", null);

        Assert.Equal(HttpStatusCode.OK, phanHoi.StatusCode);
        var nhomConLai = _factory.KhoNhomGiaLap.DanhSach.Single(n => n.Id == nhom.Id);
        Assert.DoesNotContain(idTv, nhomConLai.ThanhVienIds);
    }

    [Fact]
    public async Task RoiNhom_LaNguoiTao_GiaiTanNhom()
    {
        var (tokenAdmin, _) = await TaoTaiKhoanVaDangNhapAsync("nhomroi3");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenAdmin);
        var nhom = await (await _client.PostAsJsonAsync("/api/nhom", new TaoNhomRequest("Nhóm giải tán", null, null, new List<string>())))
            .Content.ReadFromJsonAsync<NhomDto>();

        var phanHoi = await _client.PostAsync($"/api/nhom/{nhom!.Id}/roi-nhom", null);

        Assert.Equal(HttpStatusCode.OK, phanHoi.StatusCode);
        Assert.DoesNotContain(_factory.KhoNhomGiaLap.DanhSach, n => n.Id == nhom.Id);
    }
```

Thêm `using System.Net.Http.Headers;` ở đầu file nếu chưa có (đã có sẵn từ Step 7 Task 4).

- [ ] **Step 7: Build và test**

Run: `dotnet build backend/HaloChat.sln`
Expected: 0 lỗi.

Run: `dotnet test backend/HaloChat.sln`
Expected: toàn bộ pass, bao gồm 5 test mới ở Step 6. Đây là task backend cuối cùng của plan — sau bước này toàn bộ API/Hub cho nhóm + presence + trạng thái tin nhắn đã hoàn chỉnh.

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "Them/xoa thanh vien nhom + roi nhom, day su kien realtime qua Hub"
```

---

## Task 8: Frontend — mở rộng `KieuDuLieu.ts` + `DichVuApi.ts`

**Files:**
- Modify: `frontend/src/KieuDuLieu.ts`
- Modify: `frontend/src/DichVuApi.ts`
- Modify: `frontend/src/DichVuApi.test.ts`

**Interfaces:**
- Consumes: các endpoint REST đã hoàn thiện ở Task 2-7 (`/api/nhom*`, `/api/nguoidung/trang-thai`, `/api/tinnhan/nhom/{id}`, `PUT /api/nguoidung/cai-dat` 2 field).
- Produces: `Nhom`, `KetQuaRoiNhom` (kiểu TS), và các hàm `TaoNhom`, `LayDanhSachNhom`, `LayChiTietNhom`, `CapNhatNhom`, `ThemThanhVien`, `XoaThanhVien`, `RoiNhom`, `LayLichSuNhom`, `LayTrangThaiHoatDong` — Task 9/10/11 gọi đúng các hàm này.

- [ ] **Step 1: Mở rộng `KieuDuLieu.ts`**

Sửa `TinNhan` (thêm `nhomId`, `daNhan`):

```ts
export interface TinNhan {
  id: string;
  nguoiGuiId: string;
  nguoiNhanId: string | null;
  nhomId: string | null;
  loaiTinNhan: LoaiTinNhan;
  noiDungTinNhan: string;
  duongDanFile: string | null;
  tenFileGoc: string | null;
  kichThuocFile: number | null;
  loaiFile: string | null;
  daDoc: boolean;
  daNhan: boolean;
  thoiGianTao: string;
}
```

Sửa `HoSoCaNhan` (thêm `hienThiTrangThaiHoatDong`):

```ts
export interface HoSoCaNhan {
  id: string;
  tenTaiKhoan: string;
  email: string;
  choPhepTinNhanTuNguoiLa: boolean;
  hienThiTrangThaiHoatDong: boolean;
}
```

Thêm mới ở cuối file:

```ts
export interface Nhom {
  id: string;
  tenNhom: string;
  moTa: string | null;
  duongDanAnhDaiDien: string | null;
  nguoiTaoId: string;
  thanhVien: NguoiDungTomTat[];
  thoiGianTao: string;
}

export interface KetQuaRoiNhom {
  daGiaiTan: boolean;
  thanhVienConLai: string[];
}
```

- [ ] **Step 2: Mở rộng `DichVuApi.ts`**

Sửa `CapNhatCaiDat` (thêm tham số thứ 2):

```ts
export async function CapNhatCaiDat(
  token: string, choPhepTinNhanTuNguoiLa: boolean, hienThiTrangThaiHoatDong: boolean,
): Promise<void> {
  await goiApi('/nguoidung/cai-dat', {
    method: 'PUT',
    headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' },
    body: JSON.stringify({ choPhepTinNhanTuNguoiLa, hienThiTrangThaiHoatDong }),
  });
}
```

Thêm import kiểu `Nhom`, `KetQuaRoiNhom` vào khối `import type` ở đầu file (dòng 1-4 hiện có), rồi thêm các hàm mới vào cuối file:

```ts
export async function LayTrangThaiHoatDong(token: string, ids: string[]): Promise<Record<string, boolean>> {
  if (ids.length === 0) return {};
  return goiApi<Record<string, boolean>>(`/nguoidung/trang-thai?ids=${ids.join(',')}`, {
    headers: { Authorization: `Bearer ${token}` },
  });
}

export async function TaoNhom(
  token: string, tenNhom: string, moTa: string | null, duongDanAnhDaiDien: string | null, thanhVienIds: string[],
): Promise<Nhom> {
  return goiApi<Nhom>('/nhom', {
    method: 'POST',
    headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' },
    body: JSON.stringify({ tenNhom, moTa, duongDanAnhDaiDien, thanhVienIds }),
  });
}

export async function LayDanhSachNhom(token: string): Promise<Nhom[]> {
  return goiApi<Nhom[]>('/nhom', { headers: { Authorization: `Bearer ${token}` } });
}

export async function LayChiTietNhom(token: string, id: string): Promise<Nhom> {
  return goiApi<Nhom>(`/nhom/${id}`, { headers: { Authorization: `Bearer ${token}` } });
}

export async function CapNhatNhom(
  token: string, id: string, tenNhom: string, moTa: string | null, duongDanAnhDaiDien: string | null,
): Promise<Nhom> {
  return goiApi<Nhom>(`/nhom/${id}`, {
    method: 'PUT',
    headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' },
    body: JSON.stringify({ tenNhom, moTa, duongDanAnhDaiDien }),
  });
}

export async function ThemThanhVien(token: string, nhomId: string, thanhVienId: string): Promise<Nhom> {
  return goiApi<Nhom>(`/nhom/${nhomId}/thanh-vien`, {
    method: 'POST',
    headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' },
    body: JSON.stringify({ thanhVienId }),
  });
}

export async function XoaThanhVien(token: string, nhomId: string, userId: string): Promise<Nhom> {
  return goiApi<Nhom>(`/nhom/${nhomId}/thanh-vien/${userId}`, {
    method: 'DELETE',
    headers: { Authorization: `Bearer ${token}` },
  });
}

export async function RoiNhom(token: string, nhomId: string): Promise<KetQuaRoiNhom> {
  return goiApi<KetQuaRoiNhom>(`/nhom/${nhomId}/roi-nhom`, {
    method: 'POST',
    headers: { Authorization: `Bearer ${token}` },
  });
}

export async function LayLichSuNhom(
  token: string, nhomId: string, truoc?: string, soLuong = 30,
): Promise<TinNhan[]> {
  const thamSo = new URLSearchParams({ soLuong: String(soLuong) });
  if (truoc) thamSo.set('truoc', truoc);
  return goiApi<TinNhan[]>(`/tinnhan/nhom/${nhomId}?${thamSo.toString()}`, {
    headers: { Authorization: `Bearer ${token}` },
  });
}
```

Ghi chú: `KetQuaRoiNhomDto` backend trả `daGiaiTan`/`thanhVienConLai` (camelCase qua JSON serializer mặc định của ASP.NET Core) — khớp đúng interface `KetQuaRoiNhom` ở Step 1, không cần ánh xạ thủ công.

- [ ] **Step 3: Cập nhật test hiện có bị ảnh hưởng bởi đổi chữ ký `CapNhatCaiDat`**

Trong `frontend/src/DichVuApi.test.ts`, sửa test `CapNhatCaiDat gửi đúng PUT với body choPhepTinNhanTuNguoiLa`:

```ts
  it('CapNhatCaiDat gửi đúng PUT với body choPhepTinNhanTuNguoiLa và hienThiTrangThaiHoatDong', async () => {
    const fetchGiaLap = vi.fn().mockResolvedValue(new Response(JSON.stringify({ thongBao: 'OK' }), { status: 200 }));
    vi.stubGlobal('fetch', fetchGiaLap);

    await CapNhatCaiDat('token-gia-lap', true, false);

    expect(fetchGiaLap).toHaveBeenCalledWith(
      expect.stringContaining('/nguoidung/cai-dat'),
      expect.objectContaining({
        method: 'PUT',
        body: JSON.stringify({ choPhepTinNhanTuNguoiLa: true, hienThiTrangThaiHoatDong: false }),
      }),
    );
  });
```

Thêm test mới cho 1 hàm nhóm tiêu biểu (không cần test hết 8 hàm — pattern giống nhau, review sẽ chấp nhận 1-2 test đại diện đủ để chứng minh hàm hoạt động đúng, các hàm còn lại được test gián tiếp qua test component ở Task 10):

```ts
  it('TaoNhom gửi đúng POST và trả về nhóm vừa tạo', async () => {
    const fetchGiaLap = vi.fn().mockResolvedValue(
      new Response(
        JSON.stringify({
          id: 'n1', tenNhom: 'Nhóm CNTT', moTa: null, duongDanAnhDaiDien: null,
          nguoiTaoId: 'a', thanhVien: [], thoiGianTao: '2026-01-01T00:00:00Z',
        }),
        { status: 200 },
      ),
    );
    vi.stubGlobal('fetch', fetchGiaLap);

    const nhom = await TaoNhom('token-gia-lap', 'Nhóm CNTT', null, null, ['b']);

    expect(nhom.tenNhom).toBe('Nhóm CNTT');
    expect(fetchGiaLap).toHaveBeenCalledWith(
      expect.stringContaining('/nhom'),
      expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({ tenNhom: 'Nhóm CNTT', moTa: null, duongDanAnhDaiDien: null, thanhVienIds: ['b'] }),
      }),
    );
  });
```

Nhớ thêm `TaoNhom` vào dòng `import { ... } from './DichVuApi';` ở đầu file test.

- [ ] **Step 4: Build và test**

Run: `cd frontend && npm run test -- --run`
Expected: toàn bộ pass.

Run: `cd frontend && npm run build`
Expected: build thành công, không lỗi `tsc -b`.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "Frontend: mo rong KieuDuLieu/DichVuApi cho nhom, presence, DaNhan"
```

---

## Task 9: Component dùng chung `KhungTinNhan` + refactor `TrangChat` (đổi Hub signature, presence, trạng thái tin nhắn 4 mức)

**Files:**
- Create: `frontend/src/ThanhPhan/KhungTinNhan.tsx`
- Create: `frontend/src/ThanhPhan/KhungTinNhan.css`
- Modify: `frontend/src/Trang/TrangChat.tsx` (refactor lớn)
- Modify: `frontend/src/Trang/TrangChat.css` (giữ lại phần sidebar, xóa phần đã chuyển sang `KhungTinNhan.css`)
- Modify: `frontend/src/Trang/TrangChat.test.tsx`

**Interfaces:**
- Consumes: `LayLichSuTinNhan`, `LayTrangThaiHoatDong`, `TaiLenTep` (Task 8), Hub method `GuiTinNhan(nguoiNhanId, nhomId, ...)` (Task 6).
- Produces: `KhungTinNhan` (component) + kiểu `TinNhanHienThi = TinNhan & { dangGui?: boolean }` xuất từ `KhungTinNhan.tsx` — Task 10 (`TrangNhom`) import và dùng lại y hệt component này với `loaiHoiThoai="nhom"`.

- [ ] **Step 1: Tạo `KhungTinNhan.tsx`**

```tsx
import { useEffect, useRef, type FormEvent, type ChangeEvent } from 'react';
import { BieuTuongGhim } from './BieuTuong';
import { DIA_CHI_GOC } from '../DichVuApi';
import type { TinNhan } from '../KieuDuLieu';
import './KhungTinNhan.css';

export type TinNhanHienThi = TinNhan & { dangGui?: boolean };

function dinhDangKichThuoc(bytes: number): string {
  const mb = bytes / (1024 * 1024);
  return mb >= 1 ? `${mb.toFixed(1)}MB` : `${Math.ceil(bytes / 1024)}KB`;
}

function nhanTrangThaiGui(tn: TinNhanHienThi): string {
  if (tn.dangGui) return 'Đang gửi';
  if (tn.daDoc) return 'Đã xem';
  if (tn.daNhan) return 'Đã nhận';
  return 'Đã gửi';
}

interface PropsKhungTinNhan {
  loaiHoiThoai: 'nguoiDung' | 'nhom';
  tenHienThi: string;
  phuDe?: string;
  danhSachTinNhan: TinNhanHienThi[];
  idHienTai: string;
  dangKetNoi: boolean;
  dangTaiLichSu: boolean;
  coTheTaiThem: boolean;
  onTaiThemLichSuCu: () => void;
  onGuiVanBan: (noiDung: string) => void;
  onGuiTep: (tep: File) => void;
  dangTaiTep: boolean;
  loi: string | null;
}

export function KhungTinNhan({
  loaiHoiThoai, tenHienThi, phuDe, danhSachTinNhan, idHienTai, dangKetNoi, dangTaiLichSu,
  coTheTaiThem, onTaiThemLichSuCu, onGuiVanBan, onGuiTep, dangTaiTep, loi,
}: PropsKhungTinNhan) {
  const inputTepRef = useRef<HTMLInputElement | null>(null);
  const cuoiDanhSachRef = useRef<HTMLDivElement | null>(null);
  const noiDungRef = useRef<HTMLInputElement | null>(null);

  useEffect(() => {
    cuoiDanhSachRef.current?.scrollIntoView?.({ block: 'end' });
  }, [danhSachTinNhan]);

  function xuLySubmit(su: FormEvent) {
    su.preventDefault();
    const gtHienTai = noiDungRef.current?.value.trim();
    if (!gtHienTai) return;
    onGuiVanBan(gtHienTai);
    if (noiDungRef.current) noiDungRef.current.value = '';
  }

  // Không kiểm tra kích thước file ở đây — component cha (TrangChat/TrangNhom)
  // đã kiểm tra giới hạn kích thước và tự set "loi" khi vượt quá, giữ đúng 1
  // nguồn sự thật cho thông báo lỗi hiển thị. Component này chỉ chuyển tiếp
  // file đã chọn.
  function xuLyChonTep(su: ChangeEvent<HTMLInputElement>) {
    const tep = su.target.files?.[0];
    if (!tep) return;
    onGuiTep(tep);
  }

  return (
    <main className="khung-tin-nhan">
      {loi && (
        <p className="thong-bao-loi" role="alert">
          {loi}
        </p>
      )}
      <header className="khung-tin-nhan__tieu-de">
        <span className="khung-tin-nhan__avatar">{tenHienThi.charAt(0).toUpperCase()}</span>
        <div className="khung-tin-nhan__ten-cum">
          <span className="khung-tin-nhan__ten">{tenHienThi}</span>
          {phuDe && <span className="khung-tin-nhan__phu-de">{phuDe}</span>}
        </div>
        {!dangKetNoi && <span className="khung-tin-nhan__mat-ket-noi">Mất kết nối realtime...</span>}
      </header>

      <div className="khung-tin-nhan__danh-sach-tin-nhan">
        {coTheTaiThem && (
          <button className="khung-tin-nhan__nut-tai-them" onClick={onTaiThemLichSuCu} disabled={dangTaiLichSu}>
            {dangTaiLichSu ? 'Đang tải...' : 'Tải tin nhắn cũ hơn'}
          </button>
        )}
        {danhSachTinNhan.map((tn) => {
          const laCuaMinh = tn.nguoiGuiId === idHienTai;
          return (
            <div key={tn.id} className={`khung-tin-nhan__bong${laCuaMinh ? ' khung-tin-nhan__bong--minh' : ''}`}>
              {tn.loaiTinNhan === 'Anh' && (
                <img className="khung-tin-nhan__anh" src={`${DIA_CHI_GOC}${tn.duongDanFile}`} alt={tn.tenFileGoc ?? 'ảnh'} />
              )}
              {tn.loaiTinNhan === 'File' && (
                <a className="khung-tin-nhan__file" href={`${DIA_CHI_GOC}${tn.duongDanFile}`} target="_blank" rel="noreferrer">
                  📎 {tn.tenFileGoc} ({dinhDangKichThuoc(tn.kichThuocFile ?? 0)})
                </a>
              )}
              {tn.loaiTinNhan === 'Text' && tn.noiDungTinNhan}
              {laCuaMinh && loaiHoiThoai === 'nguoiDung' && (
                <span className="khung-tin-nhan__trang-thai-gui">{nhanTrangThaiGui(tn)}</span>
              )}
            </div>
          );
        })}
        <div ref={cuoiDanhSachRef} />
      </div>

      <form className="khung-tin-nhan__form-gui" onSubmit={xuLySubmit}>
        <button
          type="button"
          className="khung-tin-nhan__nut-ghim"
          onClick={() => inputTepRef.current?.click()}
          disabled={!dangKetNoi || dangTaiTep}
          aria-label="Đính kèm file"
        >
          <BieuTuongGhim />
        </button>
        <input
          ref={inputTepRef}
          type="file"
          className="khung-tin-nhan__input-tep"
          accept="image/jpeg,image/png,image/gif,image/webp,application/pdf,.docx,.xlsx,.zip"
          onChange={(su) => {
            xuLyChonTep(su);
            su.target.value = '';
          }}
        />
        <input ref={noiDungRef} type="text" placeholder="Nhập tin nhắn..." disabled={!dangKetNoi} />
        <button type="submit" disabled={!dangKetNoi}>
          Gửi
        </button>
      </form>
      {dangTaiTep && <p className="khung-tin-nhan__dang-tai-tep">Đang tải file lên...</p>}
      <p className="khung-tin-nhan__ma-hoa">🔒 Được mã hóa bằng AES-256-GCM</p>
    </main>
  );
}
```

Ghi chú quan trọng: dòng `<p className="khung-tin-nhan__ma-hoa">🔒 Được mã hóa bằng AES-256-GCM</p>` **chỉ là văn bản trình bày** theo đúng mockup — KHÔNG có mã hóa thật đứng sau (xem Global Constraints + spec §10.8). Không xóa dòng ghi chú tiếng Việt phía trên nó khi implement thật, để người review sau không tưởng nhầm là bug.

Ghi chú kỹ thuật: input tin nhắn dùng `ref` không kiểm soát (uncontrolled) thay vì `useState` như bản `TrangChat` cũ — vì component này giờ generic cho nhiều loại hội thoại, tránh phải đồng bộ state input qua props khi chuyển hội thoại (mỗi lần đổi `key` ở component cha, React tự mount lại input rỗng).

- [ ] **Step 2: Tạo `KhungTinNhan.css`**

```css
.khung-tin-nhan {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.khung-tin-nhan__tieu-de {
  padding: 16px 24px;
  border-bottom: 1px solid var(--mau-vien);
  display: flex;
  align-items: center;
  gap: 12px;
}

.khung-tin-nhan__avatar {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  background: linear-gradient(135deg, var(--mau-chinh-nhat), var(--mau-chinh-dam));
  color: #fff;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: 700;
  flex-shrink: 0;
}

.khung-tin-nhan__ten-cum {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.khung-tin-nhan__ten {
  font-weight: 700;
}

.khung-tin-nhan__phu-de {
  font-size: 12px;
  color: var(--mau-chu-phu);
}

.khung-tin-nhan__mat-ket-noi {
  margin-left: auto;
  font-size: 12px;
  font-weight: 500;
  color: var(--mau-loi);
}

.khung-tin-nhan__danh-sach-tin-nhan {
  flex: 1;
  overflow-y: auto;
  padding: 16px 24px;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.khung-tin-nhan__nut-tai-them {
  align-self: center;
  border: 1px solid var(--mau-vien);
  background: #fff;
  border-radius: 999px;
  padding: 6px 16px;
  font-size: 12px;
  font-family: inherit;
  cursor: pointer;
  margin-bottom: 8px;
}

.khung-tin-nhan__bong {
  max-width: 60%;
  padding: 10px 14px;
  border-radius: var(--ban-kinh-o);
  background: var(--mau-nen-tren);
  align-self: flex-start;
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.khung-tin-nhan__bong--minh {
  align-self: flex-end;
  background: linear-gradient(135deg, var(--mau-chinh-nhat), var(--mau-chinh-dam));
  color: #fff;
}

.khung-tin-nhan__trang-thai-gui {
  font-size: 11px;
  opacity: 0.85;
  align-self: flex-end;
}

.khung-tin-nhan__form-gui {
  display: flex;
  gap: 12px;
  padding: 16px 24px;
  border-top: 1px solid var(--mau-vien);
}

.khung-tin-nhan__form-gui input[type='text'] {
  flex: 1;
  padding: 12px 16px;
  border: 1px solid var(--mau-vien);
  border-radius: var(--ban-kinh-o);
  font-family: inherit;
  font-size: 14px;
}

.khung-tin-nhan__form-gui button[type='submit'] {
  border: none;
  border-radius: var(--ban-kinh-o);
  padding: 0 24px;
  background: linear-gradient(135deg, var(--mau-chinh-nhat), var(--mau-chinh-dam));
  color: #fff;
  font-weight: 700;
  cursor: pointer;
}

.khung-tin-nhan__form-gui button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.khung-tin-nhan__input-tep {
  display: none;
}

.khung-tin-nhan__nut-ghim {
  border: 1px solid var(--mau-vien);
  background: #fff;
  border-radius: 50%;
  width: 40px;
  height: 40px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  color: var(--mau-chu-dam);
  flex-shrink: 0;
}

.khung-tin-nhan__anh {
  max-width: 220px;
  border-radius: 12px;
  display: block;
}

.khung-tin-nhan__file {
  color: inherit;
  text-decoration: underline;
}

.khung-tin-nhan__dang-tai-tep,
.khung-tin-nhan__ma-hoa {
  margin: 0;
  padding: 0 24px 12px;
  font-size: 12px;
  color: var(--mau-chu-phu);
}
```

- [ ] **Step 3: Refactor `TrangChat.tsx`**

```tsx
import { useEffect, useMemo, useRef, useState } from 'react';
import { useLocation } from 'react-router-dom';
import { LayDanhSachHoiThoai, LayLichSuTinNhan, TaiLenTep, LoiGoiApi, LayTrangThaiHoatDong } from '../DichVuApi';
import { useXacThuc } from '../NguCanh/NguCanhXacThuc';
import { useChat } from '../NguCanh/NguCanhChat';
import { KhungTinNhan, type TinNhanHienThi } from '../ThanhPhan/KhungTinNhan';
import type { NguoiDungTomTat, HoiThoaiTomTat } from '../KieuDuLieu';
import './TrangChat.css';

const GIOI_HAN_ANH_BYTES = 5 * 1024 * 1024;
const GIOI_HAN_FILE_BYTES = 20 * 1024 * 1024;

function idNguoiKia(tinNhan: TinNhanHienThi, idHienTai: string): string {
  return tinNhan.nguoiGuiId === idHienTai ? (tinNhan.nguoiNhanId ?? '') : tinNhan.nguoiGuiId;
}

export function TrangChat() {
  const { token, nguoiDungHienTai, dangXuat } = useXacThuc();
  const { ketNoi, dangKetNoi } = useChat();
  const location = useLocation();
  const moNguoiDungTuDieuHuong = (location.state as { moNguoiDung?: NguoiDungTomTat } | null)?.moNguoiDung ?? null;

  const [danhSachHoiThoai, setDanhSachHoiThoai] = useState<HoiThoaiTomTat[]>([]);
  const [nguoiDangChon, setNguoiDangChon] = useState<NguoiDungTomTat | null>(moNguoiDungTuDieuHuong);
  const [tinNhanTheoNguoiDung, setTinNhanTheoNguoiDung] = useState<Record<string, TinNhanHienThi[]>>({});
  const [trangThaiOnline, setTrangThaiOnline] = useState<Record<string, boolean>>({});
  const [dangTaiDanhSach, setDangTaiDanhSach] = useState(true);
  const [dangTaiLichSu, setDangTaiLichSu] = useState(false);
  const [loi, setLoi] = useState<string | null>(null);
  const [dangTaiTep, setDangTaiTep] = useState(false);
  const idDaTaiLichSuRef = useRef<Set<string>>(new Set());

  const idHienTai = nguoiDungHienTai?.id ?? '';

  useEffect(() => {
    if (!token) return;
    LayDanhSachHoiThoai(token)
      .then((ds) => {
        setDanhSachHoiThoai(ds);
        return LayTrangThaiHoatDong(token, ds.map((h) => h.nguoiDung.id));
      })
      .then((tt) => setTrangThaiOnline(tt))
      .catch((loiBat) => {
        if (loiBat instanceof LoiGoiApi && loiBat.trangThai === 401) {
          dangXuat();
          return;
        }
        setLoi(loiBat instanceof Error ? loiBat.message : 'Đã có lỗi xảy ra.');
      })
      .finally(() => setDangTaiDanhSach(false));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [token]);

  const danhSachHienThi = useMemo<NguoiDungTomTat[]>(
    () => danhSachHoiThoai.map((h) => h.nguoiDung),
    [danhSachHoiThoai],
  );

  useEffect(() => {
    if (!token || !nguoiDangChon) return;
    if (idDaTaiLichSuRef.current.has(nguoiDangChon.id)) return;
    idDaTaiLichSuRef.current.add(nguoiDangChon.id);

    setDangTaiLichSu(true);
    LayLichSuTinNhan(token, nguoiDangChon.id)
      .then((moiNhatTruoc) => {
        const thuTuThoiGian = [...moiNhatTruoc].reverse();
        setTinNhanTheoNguoiDung((truoc) => {
          const gop = new Map<string, TinNhanHienThi>();
          for (const tn of thuTuThoiGian) gop.set(tn.id, tn);
          for (const tn of truoc[nguoiDangChon.id] ?? []) gop.set(tn.id, tn);
          const ketQua = [...gop.values()].sort((a, b) => (a.id < b.id ? -1 : a.id > b.id ? 1 : 0));
          return { ...truoc, [nguoiDangChon.id]: ketQua };
        });
      })
      .catch((loiBat) => {
        idDaTaiLichSuRef.current.delete(nguoiDangChon.id);
        setLoi(loiBat instanceof Error ? loiBat.message : 'Không tải được lịch sử tin nhắn.');
      })
      .finally(() => setDangTaiLichSu(false));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [token, nguoiDangChon]);

  useEffect(() => {
    if (!ketNoi) return;

    function xuLyTinNhanMoi(tinNhan: TinNhanHienThi) {
      if (tinNhan.nhomId) return; // tin nhắn nhóm không thuộc trang này (Task 10 xử lý riêng)
      const idKia = idNguoiKia(tinNhan, idHienTai);
      setTinNhanTheoNguoiDung((truoc) => ({
        ...truoc,
        [idKia]: [...(truoc[idKia] ?? []).filter((tn) => tn.id !== tinNhan.id), tinNhan],
      }));
    }

    function xuLyTrangThaiThayDoi(userId: string, online: boolean) {
      setTrangThaiOnline((truoc) => ({ ...truoc, [userId]: online }));
    }

    ketNoi.on('NhanTinNhan', xuLyTinNhanMoi);
    ketNoi.on('TrangThaiHoatDongThayDoi', xuLyTrangThaiThayDoi);
    return () => {
      ketNoi.off('NhanTinNhan', xuLyTinNhanMoi);
      ketNoi.off('TrangThaiHoatDongThayDoi', xuLyTrangThaiThayDoi);
    };
  }, [ketNoi, idHienTai]);

  const tinNhanDangHien = useMemo(
    () => (nguoiDangChon ? (tinNhanTheoNguoiDung[nguoiDangChon.id] ?? []) : []),
    [nguoiDangChon, tinNhanTheoNguoiDung],
  );

  function guiTinNhanVanBan(noiDungGui: string) {
    if (!ketNoi || !nguoiDangChon) return;
    const idTam = `tam-${Date.now()}`;
    const tinNhanTam: TinNhanHienThi = {
      id: idTam, nguoiGuiId: idHienTai, nguoiNhanId: nguoiDangChon.id, nhomId: null,
      loaiTinNhan: 'Text', noiDungTinNhan: noiDungGui, duongDanFile: null, tenFileGoc: null,
      kichThuocFile: null, loaiFile: null, daDoc: false, daNhan: false,
      thoiGianTao: new Date().toISOString(), dangGui: true,
    };
    setTinNhanTheoNguoiDung((truoc) => ({ ...truoc, [nguoiDangChon.id]: [...(truoc[nguoiDangChon.id] ?? []), tinNhanTam] }));

    ketNoi
      .invoke<TinNhanHienThi>('GuiTinNhan', nguoiDangChon.id, null, 'Text', noiDungGui, null, null, null, null)
      .then((tinNhanDaGui) => {
        setTinNhanTheoNguoiDung((truoc) => ({
          ...truoc,
          [nguoiDangChon.id]: (truoc[nguoiDangChon.id] ?? []).map((tn) => (tn.id === idTam ? tinNhanDaGui : tn)),
        }));
      })
      .catch((loiBat) => {
        setTinNhanTheoNguoiDung((truoc) => ({
          ...truoc,
          [nguoiDangChon.id]: (truoc[nguoiDangChon.id] ?? []).filter((tn) => tn.id !== idTam),
        }));
        setLoi(loiBat instanceof Error ? loiBat.message : 'Gửi tin nhắn thất bại. Vui lòng thử lại.');
      });
  }

  function guiTep(tep: File) {
    if (!ketNoi || !nguoiDangChon || !token) return;

    const laAnh = tep.type.startsWith('image/');
    const gioiHan = laAnh ? GIOI_HAN_ANH_BYTES : GIOI_HAN_FILE_BYTES;
    if (tep.size > gioiHan) {
      setLoi(`File vượt quá giới hạn ${gioiHan / 1024 / 1024}MB.`);
      return;
    }

    setDangTaiTep(true);
    TaiLenTep(token, tep)
      .then((daTaiLen) =>
        ketNoi.invoke<TinNhanHienThi>(
          'GuiTinNhan', nguoiDangChon.id, null, laAnh ? 'Anh' : 'File', '',
          daTaiLen.duongDanFile, daTaiLen.tenFileGoc, daTaiLen.kichThuocFile, daTaiLen.loaiFile,
        ),
      )
      .then((tinNhanDaGui) => {
        setTinNhanTheoNguoiDung((truoc) => ({
          ...truoc,
          [nguoiDangChon.id]: [...(truoc[nguoiDangChon.id] ?? []), tinNhanDaGui],
        }));
      })
      .catch((loiBat) => {
        setLoi(loiBat instanceof Error ? loiBat.message : 'Gửi file thất bại. Vui lòng thử lại.');
      })
      .finally(() => setDangTaiTep(false));
  }

  function taiThemLichSuCu() {
    if (!token || !nguoiDangChon) return;
    const cuNhat = (tinNhanTheoNguoiDung[nguoiDangChon.id] ?? [])[0];
    if (!cuNhat) return;

    setDangTaiLichSu(true);
    LayLichSuTinNhan(token, nguoiDangChon.id, cuNhat.id)
      .then((cuHon) => {
        const thuTuThoiGian = [...cuHon].reverse();
        setTinNhanTheoNguoiDung((truoc) => ({
          ...truoc,
          [nguoiDangChon.id]: [...thuTuThoiGian, ...(truoc[nguoiDangChon.id] ?? [])],
        }));
      })
      .catch(() => setLoi('Không tải được tin nhắn cũ hơn.'))
      .finally(() => setDangTaiLichSu(false));
  }

  return (
    <div className="trang-chat">
      <aside className="trang-chat__sidebar">
        {dangTaiDanhSach && <p>Đang tải...</p>}
        <ul className="trang-chat__danh-sach">
          {danhSachHienThi.map((nd) => (
            <li key={nd.id}>
              <button
                className={`trang-chat__muc${nguoiDangChon?.id === nd.id ? ' trang-chat__muc--dang-chon' : ''}`}
                onClick={() => setNguoiDangChon(nd)}
              >
                <span className="trang-chat__avatar">{nd.tenTaiKhoan.charAt(0).toUpperCase()}</span>
                <span className="trang-chat__ten">{nd.tenTaiKhoan}</span>
                {trangThaiOnline[nd.id] && <span className="trang-chat__cham-online" title="Đang hoạt động" />}
              </button>
            </li>
          ))}
        </ul>
      </aside>

      {!nguoiDangChon && (
        <div className="trang-chat__trong-rong">
          <p className="trang-chat__trong-tieu-de">Chào mừng đến HaloChat</p>
          <p className="trang-chat__trong">Chọn một cuộc trò chuyện để bắt đầu nhắn tin an toàn.</p>
        </div>
      )}
      {nguoiDangChon && (
        <KhungTinNhan
          loaiHoiThoai="nguoiDung"
          tenHienThi={nguoiDangChon.tenTaiKhoan}
          phuDe={trangThaiOnline[nguoiDangChon.id] ? 'Đang hoạt động' : undefined}
          danhSachTinNhan={tinNhanDangHien}
          idHienTai={idHienTai}
          dangKetNoi={dangKetNoi}
          dangTaiLichSu={dangTaiLichSu}
          coTheTaiThem
          onTaiThemLichSuCu={taiThemLichSuCu}
          onGuiVanBan={guiTinNhanVanBan}
          onGuiTep={guiTep}
          dangTaiTep={dangTaiTep}
          loi={loi}
        />
      )}
    </div>
  );
}
```

Ghi chú implementer quan trọng: hàm `guiTinNhanVanBan` giờ thêm tin nhắn **optimistic** (`dangGui: true`) NGAY khi bấm gửi (khớp mockup "Đang gửi"), rồi thay thế bằng tin nhắn thật khi `invoke` resolve, hoặc gỡ bỏ nếu lỗi — đây là thay đổi hành vi có chủ ý so với bản cũ (bản cũ chỉ thêm sau khi resolve). Viết test mới ở Step 4 để phủ đúng hành vi này, KHÔNG giữ nguyên assertion cũ giả định "chỉ có 1 lần thêm tin nhắn".

- [ ] **Step 4: Sửa `TrangChat.css`** — xóa các class đã chuyển sang `KhungTinNhan.css` (`.trang-chat__khung-chinh` trở xuống đến hết file, TRỪ phần `@media (max-width: 640px)` xử lý ở Task 12), giữ lại từ đầu file đến hết `.trang-chat__avatar`. Thêm class mới cho chấm online:

```css
.trang-chat__cham-online {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: #2ecc71;
  margin-left: auto;
  flex-shrink: 0;
}

.trang-chat__muc {
  /* dòng display: flex; align-items: center; gap: 12px; đã có sẵn — không đổi,
     chỉ đảm bảo .trang-chat__cham-online nằm cùng hàng flex này */
}

.trang-chat__trong-rong {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 8px;
  color: var(--mau-chu-phu);
}

.trang-chat__trong-tieu-de {
  font-size: 18px;
  font-weight: 700;
  color: var(--mau-chu-dam);
  margin: 0;
}
```

(Giữ nguyên `.trang-chat__trong` đã có — dùng lại làm dòng phụ trong empty state mới, không xóa.)

- [ ] **Step 5: Cập nhật `TrangChat.test.tsx`**

Đổi toàn bộ các assertion `ketNoiGiaLap.invoke` từ 8 tham số cũ sang chèn thêm `null` (nhomId) làm tham số thứ 2. Ví dụ đổi:

```ts
    await waitFor(() =>
      expect(ketNoiGiaLap.invoke).toHaveBeenCalledWith('GuiTinNhan', '2', 'Text', 'Xin chào', null, null, null, null),
    );
```

thành:

```ts
    await waitFor(() =>
      expect(ketNoiGiaLap.invoke).toHaveBeenCalledWith('GuiTinNhan', '2', null, 'Text', 'Xin chào', null, null, null, null),
    );
```

Áp dụng tương tự cho lời gọi `'Anh'` ở test tải file, và test "Hub từ chối gửi tin". Cập nhật `taoTinNhanGiaLap` (thêm field `nhomId: null, daNhan: false` vào object mặc định):

```ts
function taoTinNhanGiaLap(gan: Partial<Awaited<ReturnType<typeof DichVuApi.LayLichSuTinNhan>>[number]>) {
  return {
    id: 'm1',
    nguoiGuiId: '2',
    nguoiNhanId: '1',
    nhomId: null,
    loaiTinNhan: 'Text' as const,
    noiDungTinNhan: 'Chào bạn',
    duongDanFile: null,
    tenFileGoc: null,
    kichThuocFile: null,
    loaiFile: null,
    daDoc: false,
    daNhan: false,
    thoiGianTao: new Date().toISOString(),
    ...gan,
  };
}
```

Thêm `vi.spyOn(DichVuApi, 'LayTrangThaiHoatDong').mockResolvedValue({});` vào khối `beforeEach` (ngay sau dòng `vi.spyOn(DichVuApi, 'LayDanhSachHoiThoai')...`) — nếu không mock, test sẽ gọi `fetch` thật và fail vì không có network trong môi trường test.

Sửa test `'gửi tin nhắn văn bản gọi ketNoi.invoke và hiển thị tin nhắn vừa gửi'` để phản ánh optimistic UI mới — thay thân test bằng:

```ts
  it('gửi tin nhắn văn bản hiện ngay "Đang gửi" rồi cập nhật khi Hub xác nhận', async () => {
    vi.spyOn(DichVuApi, 'LayLichSuTinNhan').mockResolvedValue([]);
    let phanGiai: (tn: unknown) => void = () => {};
    ketNoiGiaLap.invoke.mockReturnValue(new Promise((resolve) => { phanGiai = resolve; }));

    renderTrangChat();
    await userEvent.click(await screen.findByText('TranBinh'));
    await waitFor(() => expect(ketNoiGiaLap.start).toHaveBeenCalled());

    await userEvent.type(screen.getByPlaceholderText('Nhập tin nhắn...'), 'Xin chào');
    await userEvent.click(screen.getByRole('button', { name: 'Gửi' }));

    expect(await screen.findByText('Đang gửi')).toBeInTheDocument();

    phanGiai(taoTinNhanGiaLap({ id: 'm2', nguoiGuiId: '1', nguoiNhanId: '2', noiDungTinNhan: 'Xin chào', daNhan: true }));

    await waitFor(() => expect(screen.getByText('Đã nhận')).toBeInTheDocument());
    expect(screen.getByText('Xin chào')).toBeInTheDocument();
  });
```

Test `'nhận tin nhắn realtime qua sự kiện NhanTinNhan...'` và các test còn lại giữ nguyên ý nghĩa, chỉ cần đảm bảo mock `taoTinNhanGiaLap` mới (có `nhomId`/`daNhan`) không phá vỡ chúng — không cần sửa thêm gì khác trong các test đó.

- [ ] **Step 6: Build và test**

Run: `cd frontend && npm run test -- --run`
Expected: toàn bộ pass.

Run: `cd frontend && npm run build`
Expected: build thành công.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "Frontend: tach KhungTinNhan dung chung, TrangChat dung Hub signature moi + presence + trang thai gui"
```

---

## Task 10: `TrangNhom.tsx` + route `/nhom` + `KhungChinh` nav thật

**Files:**
- Create: `frontend/src/Trang/TrangNhom.tsx`
- Create: `frontend/src/Trang/TrangNhom.css`
- Create: `frontend/src/Trang/TrangNhom.test.tsx`
- Modify: `frontend/src/DinhTuyen.tsx` (thêm route `/nhom`)
- Modify: `frontend/src/DinhTuyen.test.tsx` (nếu có test liệt kê route — kiểm tra trước khi sửa)
- Modify: `frontend/src/ThanhPhan/KhungChinh.tsx` (đổi mục "Nhóm" thành `NavLink` thật)

**Interfaces:**
- Consumes: `KhungTinNhan` (Task 9), `LayDanhSachNhom`/`TaoNhom`/`LayLichSuNhom`/`ThemThanhVien`/`XoaThanhVien`/`RoiNhom` (Task 8), `LayDanhSachNguoiDung` (đã có từ GĐ4).
- Produces: route `/nhom` khả dụng, không có task nào sau phụ thuộc trực tiếp vào nội bộ `TrangNhom`.

- [ ] **Step 1: Tạo `TrangNhom.tsx`**

```tsx
import { useEffect, useState } from 'react';
import {
  LayDanhSachNhom, TaoNhom, LayLichSuNhom, ThemThanhVien, XoaThanhVien, RoiNhom,
  LayDanhSachNguoiDung, LoiGoiApi,
} from '../DichVuApi';
import { useXacThuc } from '../NguCanh/NguCanhXacThuc';
import { useChat } from '../NguCanh/NguCanhChat';
import { KhungTinNhan, type TinNhanHienThi } from '../ThanhPhan/KhungTinNhan';
import type { Nhom, NguoiDungTomTat } from '../KieuDuLieu';
import './TrangNhom.css';

export function TrangNhom() {
  const { token, nguoiDungHienTai } = useXacThuc();
  const { ketNoi, dangKetNoi } = useChat();
  const idHienTai = nguoiDungHienTai?.id ?? '';

  const [danhSachNhom, setDanhSachNhom] = useState<Nhom[]>([]);
  const [nhomDangChonId, setNhomDangChonId] = useState<string | null>(null);
  const [tinNhanTheoNhom, setTinNhanTheoNhom] = useState<Record<string, TinNhanHienThi[]>>({});
  const [dangTaiDanhSach, setDangTaiDanhSach] = useState(true);
  const [dangTaiLichSu, setDangTaiLichSu] = useState(false);
  const [loi, setLoi] = useState<string | null>(null);
  const [dangTaiTep, setDangTaiTep] = useState(false);
  const [hienFormTao, setHienFormTao] = useState(false);
  const [tenNhomMoi, setTenNhomMoi] = useState('');
  const [tatCaNguoiDung, setTatCaNguoiDung] = useState<NguoiDungTomTat[]>([]);
  const [thanhVienDuocChon, setThanhVienDuocChon] = useState<Set<string>>(new Set());
  const [daTaiLichSuIds] = useState<Set<string>>(() => new Set());

  const nhomDangChon = danhSachNhom.find((n) => n.id === nhomDangChonId) ?? null;

  useEffect(() => {
    if (!token) return;
    Promise.all([LayDanhSachNhom(token), LayDanhSachNguoiDung(token)])
      .then(([nhoms, nguoiDungs]) => {
        setDanhSachNhom(nhoms);
        setTatCaNguoiDung(nguoiDungs);
      })
      .catch((loiBat) => setLoi(loiBat instanceof Error ? loiBat.message : 'Không tải được danh sách nhóm.'))
      .finally(() => setDangTaiDanhSach(false));
  }, [token]);

  useEffect(() => {
    if (!token || !nhomDangChonId || daTaiLichSuIds.has(nhomDangChonId)) return;
    daTaiLichSuIds.add(nhomDangChonId);

    setDangTaiLichSu(true);
    LayLichSuNhom(token, nhomDangChonId)
      .then((tinNhans) => {
        const thuTu = [...tinNhans].reverse();
        setTinNhanTheoNhom((truoc) => ({ ...truoc, [nhomDangChonId]: thuTu }));
      })
      .catch(() => setLoi('Không tải được lịch sử tin nhắn nhóm.'))
      .finally(() => setDangTaiLichSu(false));
  }, [token, nhomDangChonId, daTaiLichSuIds]);

  useEffect(() => {
    if (!ketNoi) return;

    function xuLyTinNhanMoi(tinNhan: TinNhanHienThi) {
      if (!tinNhan.nhomId) return;
      setTinNhanTheoNhom((truoc) => ({
        ...truoc,
        [tinNhan.nhomId as string]: [...(truoc[tinNhan.nhomId as string] ?? []).filter((tn) => tn.id !== tinNhan.id), tinNhan],
      }));
    }

    function xuLyDuocThem(nhom: Nhom) {
      setDanhSachNhom((truoc) => (truoc.some((n) => n.id === nhom.id) ? truoc : [...truoc, nhom]));
    }

    function xuLyBiXoa(nhomId: string) {
      setDanhSachNhom((truoc) => truoc.filter((n) => n.id !== nhomId));
      setNhomDangChonId((truoc) => (truoc === nhomId ? null : truoc));
    }

    ketNoi.on('NhanTinNhan', xuLyTinNhanMoi);
    ketNoi.on('DuocThemVaoNhom', xuLyDuocThem);
    ketNoi.on('BiXoaKhoiNhom', xuLyBiXoa);
    ketNoi.on('NhomDaGiaiTan', xuLyBiXoa);
    return () => {
      ketNoi.off('NhanTinNhan', xuLyTinNhanMoi);
      ketNoi.off('DuocThemVaoNhom', xuLyDuocThem);
      ketNoi.off('BiXoaKhoiNhom', xuLyBiXoa);
      ketNoi.off('NhomDaGiaiTan', xuLyBiXoa);
    };
  }, [ketNoi]);

  function taoNhomMoi() {
    if (!token || !tenNhomMoi.trim()) return;
    TaoNhom(token, tenNhomMoi.trim(), null, null, [...thanhVienDuocChon])
      .then((nhom) => {
        setDanhSachNhom((truoc) => [...truoc, nhom]);
        setHienFormTao(false);
        setTenNhomMoi('');
        setThanhVienDuocChon(new Set());
        setNhomDangChonId(nhom.id);
      })
      .catch((loiBat) => setLoi(loiBat instanceof LoiGoiApi ? loiBat.message : 'Tạo nhóm thất bại.'));
  }

  function guiTinNhanVanBan(noiDungGui: string) {
    if (!ketNoi || !nhomDangChon) return;
    ketNoi
      .invoke<TinNhanHienThi>('GuiTinNhan', null, nhomDangChon.id, 'Text', noiDungGui, null, null, null, null)
      .then((tinNhanDaGui) => {
        setTinNhanTheoNhom((truoc) => ({ ...truoc, [nhomDangChon.id]: [...(truoc[nhomDangChon.id] ?? []), tinNhanDaGui] }));
      })
      .catch((loiBat) => setLoi(loiBat instanceof Error ? loiBat.message : 'Gửi tin nhắn thất bại.'));
  }

  function guiTep(tep: File) {
    if (!ketNoi || !nhomDangChon || !token) return;
    setDangTaiTep(true);
    import('../DichVuApi')
      .then(({ TaiLenTep }) => TaiLenTep(token, tep))
      .then((daTaiLen) =>
        ketNoi.invoke<TinNhanHienThi>(
          'GuiTinNhan', null, nhomDangChon.id, tep.type.startsWith('image/') ? 'Anh' : 'File', '',
          daTaiLen.duongDanFile, daTaiLen.tenFileGoc, daTaiLen.kichThuocFile, daTaiLen.loaiFile,
        ),
      )
      .then((tinNhanDaGui) => {
        setTinNhanTheoNhom((truoc) => ({ ...truoc, [nhomDangChon.id]: [...(truoc[nhomDangChon.id] ?? []), tinNhanDaGui] }));
      })
      .catch(() => setLoi('Gửi file thất bại.'))
      .finally(() => setDangTaiTep(false));
  }

  function taiThemLichSuCu() {
    if (!token || !nhomDangChon) return;
    const cuNhat = (tinNhanTheoNhom[nhomDangChon.id] ?? [])[0];
    if (!cuNhat) return;
    setDangTaiLichSu(true);
    LayLichSuNhom(token, nhomDangChon.id, cuNhat.id)
      .then((cuHon) => {
        const thuTu = [...cuHon].reverse();
        setTinNhanTheoNhom((truoc) => ({ ...truoc, [nhomDangChon.id]: [...thuTu, ...(truoc[nhomDangChon.id] ?? [])] }));
      })
      .catch(() => setLoi('Không tải được tin nhắn cũ hơn.'))
      .finally(() => setDangTaiLichSu(false));
  }

  function themThanhVien(userId: string) {
    if (!token || !nhomDangChon) return;
    ThemThanhVien(token, nhomDangChon.id, userId)
      .then((nhom) => setDanhSachNhom((truoc) => truoc.map((n) => (n.id === nhom.id ? nhom : n))))
      .catch((loiBat) => setLoi(loiBat instanceof LoiGoiApi ? loiBat.message : 'Thêm thành viên thất bại.'));
  }

  function xoaThanhVien(userId: string) {
    if (!token || !nhomDangChon) return;
    XoaThanhVien(token, nhomDangChon.id, userId)
      .then((nhom) => setDanhSachNhom((truoc) => truoc.map((n) => (n.id === nhom.id ? nhom : n))))
      .catch((loiBat) => setLoi(loiBat instanceof LoiGoiApi ? loiBat.message : 'Xóa thành viên thất bại.'));
  }

  function roiNhom() {
    if (!token || !nhomDangChon) return;
    RoiNhom(token, nhomDangChon.id)
      .then(() => {
        setDanhSachNhom((truoc) => truoc.filter((n) => n.id !== nhomDangChon.id));
        setNhomDangChonId(null);
      })
      .catch((loiBat) => setLoi(loiBat instanceof LoiGoiApi ? loiBat.message : 'Rời nhóm thất bại.'));
  }

  const laAdmin = nhomDangChon?.nguoiTaoId === idHienTai;

  return (
    <div className="trang-nhom">
      <aside className="trang-nhom__sidebar">
        <button className="trang-nhom__nut-tao" onClick={() => setHienFormTao(true)}>
          + Tạo nhóm
        </button>
        {dangTaiDanhSach && <p>Đang tải...</p>}
        <ul className="trang-nhom__danh-sach">
          {danhSachNhom.map((n) => (
            <li key={n.id}>
              <button
                className={`trang-nhom__muc${nhomDangChonId === n.id ? ' trang-nhom__muc--dang-chon' : ''}`}
                onClick={() => setNhomDangChonId(n.id)}
              >
                <span className="trang-nhom__avatar">{n.tenNhom.charAt(0).toUpperCase()}</span>
                <div className="trang-nhom__ten-cum">
                  <span className="trang-nhom__ten">{n.tenNhom}</span>
                  <span className="trang-nhom__so-thanh-vien">{n.thanhVien.length} thành viên</span>
                </div>
              </button>
            </li>
          ))}
        </ul>
      </aside>

      {!nhomDangChon && (
        <div className="trang-nhom__trong-rong">
          <p className="trang-nhom__trong-tieu-de">Chào mừng đến HaloChat</p>
          <p>Chọn 1 nhóm hoặc tạo nhóm mới để bắt đầu.</p>
        </div>
      )}
      {nhomDangChon && (
        <div className="trang-nhom__khung-phai">
          <KhungTinNhan
            loaiHoiThoai="nhom"
            tenHienThi={nhomDangChon.tenNhom}
            phuDe={`${nhomDangChon.thanhVien.length} thành viên`}
            danhSachTinNhan={tinNhanTheoNhom[nhomDangChon.id] ?? []}
            idHienTai={idHienTai}
            dangKetNoi={dangKetNoi}
            dangTaiLichSu={dangTaiLichSu}
            coTheTaiThem
            onTaiThemLichSuCu={taiThemLichSuCu}
            onGuiVanBan={guiTinNhanVanBan}
            onGuiTep={guiTep}
            dangTaiTep={dangTaiTep}
            loi={loi}
          />
          <aside className="trang-nhom__thong-tin">
            <h3>Thành viên</h3>
            <ul className="trang-nhom__ds-thanh-vien">
              {nhomDangChon.thanhVien.map((tv) => (
                <li key={tv.id}>
                  <span>{tv.tenTaiKhoan}{tv.id === nhomDangChon.nguoiTaoId ? ' (Admin)' : ''}</span>
                  {laAdmin && tv.id !== idHienTai && (
                    <button onClick={() => xoaThanhVien(tv.id)} aria-label={`Xóa ${tv.tenTaiKhoan}`}>×</button>
                  )}
                </li>
              ))}
            </ul>
            {laAdmin && (
              <select onChange={(su) => { if (su.target.value) themThanhVien(su.target.value); su.target.value = ''; }}>
                <option value="">+ Thêm thành viên...</option>
                {tatCaNguoiDung
                  .filter((nd) => !nhomDangChon.thanhVien.some((tv) => tv.id === nd.id))
                  .map((nd) => (
                    <option key={nd.id} value={nd.id}>{nd.tenTaiKhoan}</option>
                  ))}
              </select>
            )}
            <button className="trang-nhom__nut-roi" onClick={roiNhom}>
              {laAdmin ? 'Giải tán nhóm' : 'Rời nhóm'}
            </button>
          </aside>
        </div>
      )}

      {hienFormTao && (
        <div className="trang-nhom__modal-nen" onClick={() => setHienFormTao(false)}>
          <div className="trang-nhom__modal" onClick={(su) => su.stopPropagation()}>
            <h3>Tạo nhóm</h3>
            <input
              type="text"
              placeholder="Nhập tên nhóm..."
              value={tenNhomMoi}
              onChange={(su) => setTenNhomMoi(su.target.value)}
            />
            <p>Thêm thành viên:</p>
            <ul className="trang-nhom__chon-thanh-vien">
              {tatCaNguoiDung.map((nd) => (
                <li key={nd.id}>
                  <label>
                    <input
                      type="checkbox"
                      checked={thanhVienDuocChon.has(nd.id)}
                      onChange={(su) => {
                        setThanhVienDuocChon((truoc) => {
                          const moi = new Set(truoc);
                          if (su.target.checked) moi.add(nd.id); else moi.delete(nd.id);
                          return moi;
                        });
                      }}
                    />
                    {nd.tenTaiKhoan}
                  </label>
                </li>
              ))}
            </ul>
            <div className="trang-nhom__modal-hanh-dong">
              <button onClick={() => setHienFormTao(false)}>Hủy</button>
              <button className="nut-chinh" onClick={taoNhomMoi} disabled={!tenNhomMoi.trim()}>Tạo nhóm</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
```

- [ ] **Step 2: Tạo `TrangNhom.css`**

```css
.trang-nhom {
  height: 100vh;
  display: flex;
}

.trang-nhom__sidebar {
  width: 280px;
  flex-shrink: 0;
  background: var(--mau-nen-the);
  border-right: 1px solid var(--mau-vien);
  display: flex;
  flex-direction: column;
  padding: 16px;
  gap: 12px;
  overflow-y: auto;
}

.trang-nhom__nut-tao {
  border: none;
  border-radius: var(--ban-kinh-o);
  padding: 10px;
  background: linear-gradient(135deg, var(--mau-chinh-nhat), var(--mau-chinh-dam));
  color: #fff;
  font-weight: 700;
  cursor: pointer;
}

.trang-nhom__danh-sach {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.trang-nhom__muc {
  width: 100%;
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 10px 12px;
  border: none;
  background: none;
  border-radius: var(--ban-kinh-o);
  cursor: pointer;
  text-align: left;
  font-family: inherit;
}

.trang-nhom__muc:hover,
.trang-nhom__muc--dang-chon {
  background: var(--mau-nen-tren);
}

.trang-nhom__avatar {
  width: 36px;
  height: 36px;
  border-radius: 12px;
  background: linear-gradient(135deg, var(--mau-chinh-nhat), var(--mau-chinh-dam));
  color: #fff;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: 700;
  flex-shrink: 0;
}

.trang-nhom__ten-cum {
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.trang-nhom__ten {
  font-weight: 600;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.trang-nhom__so-thanh-vien {
  font-size: 12px;
  color: var(--mau-chu-phu);
}

.trang-nhom__trong-rong {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 8px;
  color: var(--mau-chu-phu);
}

.trang-nhom__trong-tieu-de {
  font-size: 18px;
  font-weight: 700;
  color: var(--mau-chu-dam);
  margin: 0;
}

.trang-nhom__khung-phai {
  flex: 1;
  display: flex;
  min-width: 0;
}

.trang-nhom__thong-tin {
  width: 240px;
  flex-shrink: 0;
  border-left: 1px solid var(--mau-vien);
  padding: 16px;
  overflow-y: auto;
}

.trang-nhom__ds-thanh-vien {
  list-style: none;
  margin: 0 0 12px;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.trang-nhom__ds-thanh-vien li {
  display: flex;
  align-items: center;
  justify-content: space-between;
  font-size: 13px;
}

.trang-nhom__nut-roi {
  margin-top: 16px;
  width: 100%;
  border: 1px solid var(--mau-loi);
  background: #fff;
  color: var(--mau-loi);
  border-radius: var(--ban-kinh-o);
  padding: 8px;
  cursor: pointer;
  font-family: inherit;
  font-weight: 600;
}

.trang-nhom__modal-nen {
  position: fixed;
  inset: 0;
  background: rgba(16, 27, 51, 0.4);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 10;
}

.trang-nhom__modal {
  background: #fff;
  border-radius: var(--ban-kinh-the);
  padding: 24px;
  width: 360px;
  max-width: calc(100vw - 32px);
  max-height: 80vh;
  overflow-y: auto;
}

.trang-nhom__modal input[type='text'] {
  width: 100%;
  padding: 10px 12px;
  border: 1px solid var(--mau-vien);
  border-radius: var(--ban-kinh-o);
  font-family: inherit;
  margin-bottom: 12px;
}

.trang-nhom__chon-thanh-vien {
  list-style: none;
  margin: 0 0 16px;
  padding: 0;
  max-height: 200px;
  overflow-y: auto;
}

.trang-nhom__modal-hanh-dong {
  display: flex;
  gap: 8px;
  justify-content: flex-end;
}

@media (max-width: 640px) {
  .trang-nhom {
    flex-direction: column;
    height: auto;
    min-height: 100vh;
  }

  .trang-nhom__sidebar {
    width: 100%;
  }

  .trang-nhom__khung-phai {
    flex-direction: column;
  }

  .trang-nhom__thong-tin {
    width: 100%;
    border-left: none;
    border-top: 1px solid var(--mau-vien);
  }
}
```

- [ ] **Step 3: Thêm route `/nhom` vào `DinhTuyen.tsx`**

Thêm import `TrangNhom` và route mới (cùng khuôn `/ban-be`):

```tsx
import { TrangNhom } from './Trang/TrangNhom';
```

```tsx
      <Route
        path="/nhom"
        element={
          <TuyenDuongRieng>
            <KhungChinh>
              <TrangNhom />
            </KhungChinh>
          </TuyenDuongRieng>
        }
      />
```

Kiểm tra `frontend/src/DinhTuyen.test.tsx` — nếu file này liệt kê cứng số lượng route hoặc test riêng path `/nhom` trả 404, cập nhật cho khớp; nếu chỉ test các path đã có từ trước, không cần sửa.

- [ ] **Step 4: Đổi mục "Nhóm" trong `KhungChinh.tsx` thành link thật**

Thay:

```tsx
        <span className="khung-chinh__muc khung-chinh__muc--sap-ra-mat" title="Sắp ra mắt">
          <BieuTuongNhom />
          <span>Nhóm</span>
        </span>
```

thành:

```tsx
        <NavLink to="/nhom" className={lopMuc}>
          <BieuTuongNhom />
          <span>Nhóm</span>
        </NavLink>
```

Xóa class CSS `.khung-chinh__muc--sap-ra-mat` khỏi `KhungChinh.css` nếu không còn nơi nào khác dùng (kiểm tra bằng `grep -rn "sap-ra-mat" frontend/src` trước khi xóa).

- [ ] **Step 5: Viết test cho `TrangNhom`**

```tsx
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { TrangNhom } from './TrangNhom';
import { NhaCungCapXacThuc } from '../NguCanh/NguCanhXacThuc';
import { NhaCungCapChat } from '../NguCanh/NguCanhChat';
import * as DichVuApi from '../DichVuApi';

const ketNoiGiaLap = {
  start: vi.fn().mockResolvedValue(undefined),
  stop: vi.fn().mockResolvedValue(undefined),
  on: vi.fn(),
  off: vi.fn(),
  invoke: vi.fn(),
  onreconnected: vi.fn(),
  onreconnecting: vi.fn(),
  onclose: vi.fn(),
};

vi.mock('@microsoft/signalr', () => ({
  HubConnectionBuilder: vi.fn().mockImplementation(function () {
    return {
      withUrl: vi.fn().mockReturnThis(),
      withAutomaticReconnect: vi.fn().mockReturnThis(),
      configureLogging: vi.fn().mockReturnThis(),
      build: vi.fn().mockReturnValue(ketNoiGiaLap),
    };
  }),
  LogLevel: { Warning: 2 },
}));

function renderTrangNhom() {
  return render(
    <MemoryRouter initialEntries={['/nhom']}>
      <NhaCungCapXacThuc>
        <NhaCungCapChat>
          <TrangNhom />
        </NhaCungCapChat>
      </NhaCungCapXacThuc>
    </MemoryRouter>,
  );
}

describe('TrangNhom', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    vi.clearAllMocks();
    localStorage.setItem('haloChatToken', 'token-gia-lap');
    vi.spyOn(DichVuApi, 'LayDanhSachNguoiDung').mockResolvedValue([{ id: '2', tenTaiKhoan: 'TranBinh', email: 'b@gmail.com' }]);
  });

  it('hiển thị danh sách nhóm đã tham gia', async () => {
    vi.spyOn(DichVuApi, 'LayDanhSachNhom').mockResolvedValue([
      { id: 'n1', tenNhom: 'Nhóm CNTT', moTa: null, duongDanAnhDaiDien: null, nguoiTaoId: '1', thanhVien: [], thoiGianTao: '2026-01-01T00:00:00Z' },
    ]);

    renderTrangNhom();

    expect(await screen.findByText('Nhóm CNTT')).toBeInTheDocument();
  });

  it('chọn 1 nhóm tải lịch sử và hiển thị tin nhắn', async () => {
    vi.spyOn(DichVuApi, 'LayDanhSachNhom').mockResolvedValue([
      { id: 'n1', tenNhom: 'Nhóm CNTT', moTa: null, duongDanAnhDaiDien: null, nguoiTaoId: '1', thanhVien: [], thoiGianTao: '2026-01-01T00:00:00Z' },
    ]);
    vi.spyOn(DichVuApi, 'LayLichSuNhom').mockResolvedValue([{
      id: 'm1', nguoiGuiId: '2', nguoiNhanId: null, nhomId: 'n1', loaiTinNhan: 'Text',
      noiDungTinNhan: 'Chào nhóm', duongDanFile: null, tenFileGoc: null, kichThuocFile: null,
      loaiFile: null, daDoc: false, daNhan: false, thoiGianTao: '2026-01-01T00:00:00Z',
    }]);

    renderTrangNhom();
    await userEvent.click(await screen.findByText('Nhóm CNTT'));

    expect(await screen.findByText('Chào nhóm')).toBeInTheDocument();
  });

  it('tạo nhóm mới gọi TaoNhom với đúng tên và thành viên đã chọn', async () => {
    vi.spyOn(DichVuApi, 'LayDanhSachNhom').mockResolvedValue([]);
    vi.spyOn(DichVuApi, 'TaoNhom').mockResolvedValue({
      id: 'n2', tenNhom: 'Nhóm mới', moTa: null, duongDanAnhDaiDien: null, nguoiTaoId: '1', thanhVien: [], thoiGianTao: '2026-01-01T00:00:00Z',
    });

    renderTrangNhom();
    await userEvent.click(await screen.findByText('+ Tạo nhóm'));
    await userEvent.type(screen.getByPlaceholderText('Nhập tên nhóm...'), 'Nhóm mới');
    await userEvent.click(screen.getByText('TranBinh'));
    await userEvent.click(screen.getByRole('button', { name: 'Tạo nhóm' }));

    await waitFor(() => expect(DichVuApi.TaoNhom).toHaveBeenCalledWith('token-gia-lap', 'Nhóm mới', null, null, ['2']));
  });
});
```

- [ ] **Step 6: Build và test**

Run: `cd frontend && npm run test -- --run`
Expected: toàn bộ pass, bao gồm 3 test mới ở `TrangNhom.test.tsx`.

Run: `cd frontend && npm run build`
Expected: build thành công.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "Frontend: them TrangNhom (danh sach/tao/chat nhom/quan ly thanh vien) + route /nhom"
```

---

## Task 11: Dropdown thông báo (`ThongBao.tsx`) trong `KhungChinh`

**Files:**
- Create: `frontend/src/ThanhPhan/ThongBao.tsx`
- Create: `frontend/src/ThanhPhan/ThongBao.css`
- Create: `frontend/src/ThanhPhan/ThongBao.test.tsx`
- Modify: `frontend/src/ThanhPhan/BieuTuong.tsx` (thêm `BieuTuongChuong`)
- Modify: `frontend/src/ThanhPhan/KhungChinh.tsx` (chèn `<ThongBao />`)

**Interfaces:**
- Consumes: `LayLoiMoiDen`, `LayDanhSachHoiThoai` (đã có), sự kiện Hub `NhanLoiMoiKetBan`/`LoiMoiKetBanDuocChapNhan` (đã đẩy thật từ GĐ5b-1, xem spec §10.7) và `NhanTinNhan` (đã có).
- Produces: không có (task cuối trước redesign, component độc lập).

- [ ] **Step 1: Thêm icon chuông**

Thêm vào cuối `frontend/src/ThanhPhan/BieuTuong.tsx`:

```tsx
export function BieuTuongChuong() {
  return (
    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
      <path d="M18 8a6 6 0 0 0-12 0c0 7-3 9-3 9h18s-3-2-3-9" />
      <path d="M13.73 21a2 2 0 0 1-3.46 0" />
    </svg>
  );
}
```

- [ ] **Step 2: Tạo `ThongBao.tsx`**

```tsx
import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { LayLoiMoiDen, LayDanhSachHoiThoai } from '../DichVuApi';
import { useXacThuc } from '../NguCanh/NguCanhXacThuc';
import { useChat } from '../NguCanh/NguCanhChat';
import { BieuTuongChuong } from './BieuTuong';
import type { LoiMoiKetBan, HoiThoaiTomTat } from '../KieuDuLieu';
import './ThongBao.css';

export function ThongBao() {
  const { token } = useXacThuc();
  const { ketNoi } = useChat();
  const navigate = useNavigate();

  const [loiMoiDen, setLoiMoiDen] = useState<LoiMoiKetBan[]>([]);
  const [hoiThoaiChuaDoc, setHoiThoaiChuaDoc] = useState<HoiThoaiTomTat[]>([]);
  const [hienDropdown, setHienDropdown] = useState(false);

  function taiLai() {
    if (!token) return;
    LayLoiMoiDen(token).then(setLoiMoiDen).catch(() => {});
    LayDanhSachHoiThoai(token)
      .then((ds) => setHoiThoaiChuaDoc(ds.filter((h) => h.soTinChuaDoc > 0)))
      .catch(() => {});
  }

  useEffect(() => {
    taiLai();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [token]);

  useEffect(() => {
    if (!ketNoi) return;
    ketNoi.on('NhanLoiMoiKetBan', taiLai);
    ketNoi.on('LoiMoiKetBanDuocChapNhan', taiLai);
    ketNoi.on('NhanTinNhan', taiLai);
    return () => {
      ketNoi.off('NhanLoiMoiKetBan', taiLai);
      ketNoi.off('LoiMoiKetBanDuocChapNhan', taiLai);
      ketNoi.off('NhanTinNhan', taiLai);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [ketNoi]);

  const tongSo = loiMoiDen.length + hoiThoaiChuaDoc.length;

  return (
    <div className="thong-bao">
      <button className="thong-bao__nut" onClick={() => setHienDropdown((v) => !v)} aria-label="Thông báo">
        <BieuTuongChuong />
        {tongSo > 0 && <span className="thong-bao__cham">{tongSo}</span>}
      </button>
      {hienDropdown && (
        <div className="thong-bao__dropdown">
          {tongSo === 0 && <p className="thong-bao__trong">Không có thông báo mới.</p>}
          {loiMoiDen.map((l) => (
            <button
              key={l.id}
              className="thong-bao__muc"
              onClick={() => {
                setHienDropdown(false);
                navigate('/ban-be');
              }}
            >
              <strong>{l.nguoiGui.tenTaiKhoan}</strong> đã gửi lời mời kết bạn.
            </button>
          ))}
          {hoiThoaiChuaDoc.map((h) => (
            <button
              key={h.nguoiDung.id}
              className="thong-bao__muc"
              onClick={() => {
                setHienDropdown(false);
                navigate('/nguoi-dung', { state: { moNguoiDung: h.nguoiDung } });
              }}
            >
              <strong>{h.nguoiDung.tenTaiKhoan}</strong>: {h.tinNhanCuoi} ({h.soTinChuaDoc})
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
```

- [ ] **Step 3: Tạo `ThongBao.css`**

```css
.thong-bao {
  position: relative;
}

.thong-bao__nut {
  position: relative;
  border: none;
  background: none;
  color: var(--mau-chu-phu);
  cursor: pointer;
  padding: 8px;
  display: flex;
}

.thong-bao__cham {
  position: absolute;
  top: 2px;
  right: 2px;
  background: var(--mau-loi);
  color: #fff;
  font-size: 10px;
  font-weight: 700;
  min-width: 16px;
  height: 16px;
  border-radius: 999px;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 0 3px;
}

.thong-bao__dropdown {
  position: absolute;
  bottom: 100%;
  left: 0;
  width: 280px;
  max-height: 320px;
  overflow-y: auto;
  background: #fff;
  border: 1px solid var(--mau-vien);
  border-radius: var(--ban-kinh-o);
  box-shadow: var(--bong-the);
  padding: 8px;
  z-index: 20;
}

.thong-bao__trong {
  color: var(--mau-chu-phu);
  font-size: 13px;
  text-align: center;
  padding: 12px;
  margin: 0;
}

.thong-bao__muc {
  display: block;
  width: 100%;
  text-align: left;
  border: none;
  background: none;
  padding: 10px;
  border-radius: 10px;
  font-size: 13px;
  font-family: inherit;
  cursor: pointer;
  color: var(--mau-chu-dam);
}

.thong-bao__muc:hover {
  background: var(--mau-nen-tren);
}

@media (max-width: 640px) {
  .thong-bao__dropdown {
    bottom: 100%;
    left: auto;
    right: 0;
  }
}
```

- [ ] **Step 4: Chèn vào `KhungChinh.tsx`**

Thêm import và đặt `<ThongBao />` ngay trước nút đăng xuất:

```tsx
import { ThongBao } from './ThongBao';
```

```tsx
        <ThongBao />
        <button className="khung-chinh__dang-xuat" onClick={dangXuat}>
          Đăng xuất
        </button>
```

- [ ] **Step 5: Viết test cho `ThongBao`**

```tsx
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { ThongBao } from './ThongBao';
import { NhaCungCapXacThuc } from '../NguCanh/NguCanhXacThuc';
import { NhaCungCapChat } from '../NguCanh/NguCanhChat';
import * as DichVuApi from '../DichVuApi';

vi.mock('@microsoft/signalr', () => ({
  HubConnectionBuilder: vi.fn().mockImplementation(function () {
    return {
      withUrl: vi.fn().mockReturnThis(),
      withAutomaticReconnect: vi.fn().mockReturnThis(),
      configureLogging: vi.fn().mockReturnThis(),
      build: vi.fn().mockReturnValue({
        start: vi.fn().mockResolvedValue(undefined), stop: vi.fn().mockResolvedValue(undefined),
        on: vi.fn(), off: vi.fn(), invoke: vi.fn(),
        onreconnected: vi.fn(), onreconnecting: vi.fn(), onclose: vi.fn(),
      }),
    };
  }),
  LogLevel: { Warning: 2 },
}));

function renderThongBao() {
  return render(
    <MemoryRouter>
      <NhaCungCapXacThuc>
        <NhaCungCapChat>
          <ThongBao />
        </NhaCungCapChat>
      </NhaCungCapXacThuc>
    </MemoryRouter>,
  );
}

describe('ThongBao', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    localStorage.setItem('haloChatToken', 'token-gia-lap');
  });

  it('hiển thị số lượng thông báo gộp từ lời mời kết bạn và hội thoại chưa đọc', async () => {
    vi.spyOn(DichVuApi, 'LayLoiMoiDen').mockResolvedValue([
      { id: 'l1', nguoiGui: { id: '2', tenTaiKhoan: 'TranBinh', email: 'b@gmail.com' }, nguoiNhan: { id: '1', tenTaiKhoan: 'A', email: 'a@gmail.com' }, trangThai: 'ChoDuyet', thoiGianTao: '2026-01-01T00:00:00Z' },
    ]);
    vi.spyOn(DichVuApi, 'LayDanhSachHoiThoai').mockResolvedValue([
      { nguoiDung: { id: '3', tenTaiKhoan: 'LeC', email: 'c@gmail.com' }, tinNhanCuoi: 'Chào', thoiGianTinNhanCuoi: '2026-01-01T00:00:00Z', soTinChuaDoc: 2 },
    ]);

    renderThongBao();

    expect(await screen.findByText('2')).toBeInTheDocument();
  });

  it('bấm vào 1 lời mời kết bạn mở dropdown và hiện đúng nội dung', async () => {
    vi.spyOn(DichVuApi, 'LayLoiMoiDen').mockResolvedValue([
      { id: 'l1', nguoiGui: { id: '2', tenTaiKhoan: 'TranBinh', email: 'b@gmail.com' }, nguoiNhan: { id: '1', tenTaiKhoan: 'A', email: 'a@gmail.com' }, trangThai: 'ChoDuyet', thoiGianTao: '2026-01-01T00:00:00Z' },
    ]);
    vi.spyOn(DichVuApi, 'LayDanhSachHoiThoai').mockResolvedValue([]);

    renderThongBao();
    await userEvent.click(await screen.findByLabelText('Thông báo'));

    expect(await screen.findByText(/đã gửi lời mời kết bạn/)).toBeInTheDocument();
  });

  it('không có thông báo hiện đúng dòng trống', async () => {
    vi.spyOn(DichVuApi, 'LayLoiMoiDen').mockResolvedValue([]);
    vi.spyOn(DichVuApi, 'LayDanhSachHoiThoai').mockResolvedValue([]);

    renderThongBao();
    await userEvent.click(await screen.findByLabelText('Thông báo'));

    await waitFor(() => expect(screen.getByText('Không có thông báo mới.')).toBeInTheDocument());
  });
});
```

- [ ] **Step 6: Build và test**

Run: `cd frontend && npm run test -- --run`
Expected: toàn bộ pass.

Run: `cd frontend && npm run build`
Expected: build thành công.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "Frontend: them dropdown ThongBao (loi moi ket ban + hoi thoai chua doc)"
```

---

## Task 12: Responsive mobile (1 cột + nút quay lại) + ô tìm kiếm + `TrangCaiDat` 4 mục con

**Files:**
- Modify: `frontend/src/Trang/TrangChat.tsx` + `TrangChat.css`
- Modify: `frontend/src/Trang/TrangNhom.tsx` + `TrangNhom.css`
- Modify: `frontend/src/Trang/TrangBanBe.tsx` + `TrangBanBe.css`
- Modify: `frontend/src/Trang/TrangCaiDat.tsx` + `TrangCaiDat.css`
- Modify: `frontend/src/Trang/TrangChat.test.tsx`, `TrangNhom.test.tsx`, `TrangBanBe.test.tsx` (nếu tồn tại — kiểm tra trước), `TrangCaiDat.test.tsx` (nếu tồn tại)

**Interfaces:**
- Consumes: state/props đã có ở Task 9/10 (`nguoiDangChon`, `nhomDangChonId`).
- Produces: không có (task cuối của plan).

Ghi chú áp dụng chung: KHÔNG thêm ô tìm kiếm vào `KhungChinh.tsx` (khác với hướng đã ghi sơ bộ ở spec §10.8) — `KhungChinh` chỉ là khung bọc không giữ state danh sách của từng trang con, nên ô tìm kiếm đặt NGAY TRONG sidebar của từng trang (`TrangChat`, `TrangNhom`, `TrangBanBe`) lọc đúng danh sách của trang đó — đúng vị trí trực quan như mockup, chỉ khác chỗ đặt code cho hợp kiến trúc component hiện có.

- [ ] **Step 1: Mobile 1-cột + nút quay lại cho `TrangChat`**

Trong `TrangChat.tsx`, sửa phần render cuối: bọc `<aside>` và phần khung chat vào 1 `<div>` cha có class điều kiện theo việc đã chọn hội thoại hay chưa, và thêm nút quay lại truyền vào `KhungTinNhan` qua prop mới `onQuayLai`.

Sửa `PropsKhungTinNhan` trong `KhungTinNhan.tsx` (thêm 1 field tùy chọn — sửa file này lại, đã tạo ở Task 9):

```tsx
interface PropsKhungTinNhan {
  // ...các field cũ giữ nguyên...
  onQuayLai?: () => void;
}
```

Và trong JSX của `<header className="khung-tin-nhan__tieu-de">`, thêm ngay đầu (trước `<span className="khung-tin-nhan__avatar">`):

```tsx
        {onQuayLai && (
          <button className="khung-tin-nhan__nut-quay-lai" onClick={onQuayLai} aria-label="Quay lại danh sách">
            ←
          </button>
        )}
```

(Nhớ thêm `onQuayLai` vào phần destructure tham số hàm `KhungTinNhan`.)

Trong `TrangChat.tsx`, đổi phần return thành:

```tsx
  return (
    <div className={`trang-chat${nguoiDangChon ? ' trang-chat--da-chon' : ''}`}>
      <aside className="trang-chat__sidebar">
        <input
          type="text"
          className="trang-chat__tim-kiem"
          placeholder="Tìm cuộc trò chuyện..."
          value={tuKhoaTimKiem}
          onChange={(su) => setTuKhoaTimKiem(su.target.value)}
        />
        {dangTaiDanhSach && <p>Đang tải...</p>}
        <ul className="trang-chat__danh-sach">
          {danhSachHienThi
            .filter((nd) => nd.tenTaiKhoan.toLowerCase().includes(tuKhoaTimKiem.toLowerCase()))
            .map((nd) => (
              <li key={nd.id}>
                <button
                  className={`trang-chat__muc${nguoiDangChon?.id === nd.id ? ' trang-chat__muc--dang-chon' : ''}`}
                  onClick={() => setNguoiDangChon(nd)}
                >
                  <span className="trang-chat__avatar">{nd.tenTaiKhoan.charAt(0).toUpperCase()}</span>
                  <span className="trang-chat__ten">{nd.tenTaiKhoan}</span>
                  {trangThaiOnline[nd.id] && <span className="trang-chat__cham-online" title="Đang hoạt động" />}
                </button>
              </li>
            ))}
        </ul>
      </aside>

      {!nguoiDangChon && (
        <div className="trang-chat__trong-rong">
          <p className="trang-chat__trong-tieu-de">Chào mừng đến HaloChat</p>
          <p className="trang-chat__trong">Chọn một cuộc trò chuyện để bắt đầu nhắn tin an toàn.</p>
        </div>
      )}
      {nguoiDangChon && (
        <KhungTinNhan
          loaiHoiThoai="nguoiDung"
          tenHienThi={nguoiDangChon.tenTaiKhoan}
          phuDe={trangThaiOnline[nguoiDangChon.id] ? 'Đang hoạt động' : undefined}
          danhSachTinNhan={tinNhanDangHien}
          idHienTai={idHienTai}
          dangKetNoi={dangKetNoi}
          dangTaiLichSu={dangTaiLichSu}
          coTheTaiThem
          onTaiThemLichSuCu={taiThemLichSuCu}
          onGuiVanBan={guiTinNhanVanBan}
          onGuiTep={guiTep}
          dangTaiTep={dangTaiTep}
          loi={loi}
          onQuayLai={() => setNguoiDangChon(null)}
        />
      )}
    </div>
  );
```

Thêm state mới ngay cạnh các `useState` khác trong `TrangChat`:

```tsx
  const [tuKhoaTimKiem, setTuKhoaTimKiem] = useState('');
```

- [ ] **Step 2: CSS mobile 1-cột cho `TrangChat.css`**

Thêm vào cuối file (thay thế hẳn khối `@media (max-width: 640px)` cũ nếu còn sót từ trước khi tách `KhungTinNhan` ở Task 9):

```css
.trang-chat__tim-kiem {
  margin: 12px 12px 4px;
  padding: 8px 12px;
  border: 1px solid var(--mau-vien);
  border-radius: var(--ban-kinh-o);
  font-family: inherit;
  font-size: 13px;
}

@media (max-width: 640px) {
  .trang-chat {
    height: 100vh;
  }

  .trang-chat__sidebar,
  .trang-chat__trong-rong {
    display: none;
  }

  .trang-chat:not(.trang-chat--da-chon) .trang-chat__sidebar {
    display: flex;
    width: 100%;
  }

  .trang-chat:not(.trang-chat--da-chon) .trang-chat__trong-rong {
    display: flex;
    width: 100%;
  }
}
```

Ghi chú implementer: quy tắc `.trang-chat__sidebar, .trang-chat__trong-rong { display: none; }` rồi bật lại bằng `:not(.trang-chat--da-chon)` là để mặc định (đã chọn hội thoại) ẩn sidebar/empty-state trên mobile, chỉ hiện `KhungTinNhan` — đây chính là cơ chế "chỉ hiện 1 cột tại 1 thời điểm" theo spec §10.8, thực hiện thuần bằng CSS + 1 class động (`trang-chat--da-chon`) mà test có thể assert qua `className`, không cần đọc kích thước màn hình trong JS.

- [ ] **Step 3: Thêm `.khung-tin-nhan__nut-quay-lai` vào `KhungTinNhan.css`**

```css
.khung-tin-nhan__nut-quay-lai {
  display: none;
  border: none;
  background: none;
  font-size: 20px;
  cursor: pointer;
  color: var(--mau-chu-dam);
  padding: 4px 8px 4px 0;
}

@media (max-width: 640px) {
  .khung-tin-nhan__nut-quay-lai {
    display: block;
  }
}
```

- [ ] **Step 4: Áp dụng tương tự cho `TrangNhom`**

Lặp lại chính xác pattern ở Step 1-2 cho `TrangNhom.tsx`/`TrangNhom.css`: thêm state `tuKhoaTimKiem`, ô tìm kiếm lọc `danhSachNhom` theo `tenNhom`, class gốc đổi thành `` `trang-nhom${nhomDangChon ? ' trang-nhom--da-chon' : ''}` ``, truyền `onQuayLai={() => setNhomDangChonId(null)}` vào `<KhungTinNhan>`, và thêm CSS mobile tương ứng:

```css
@media (max-width: 640px) {
  .trang-nhom__sidebar,
  .trang-nhom__trong-rong {
    display: none;
  }

  .trang-nhom:not(.trang-nhom--da-chon) .trang-nhom__sidebar {
    display: flex;
    width: 100%;
  }

  .trang-nhom:not(.trang-nhom--da-chon) .trang-nhom__trong-rong {
    display: flex;
    width: 100%;
  }

  .trang-nhom__khung-phai {
    width: 100%;
  }
}
```

(Thêm vào TRONG khối `@media (max-width: 640px)` đã có sẵn ở `TrangNhom.css` từ Task 10, không tạo khối `@media` thứ hai trùng lặp.)

- [ ] **Step 5: Ô tìm kiếm cho `TrangBanBe`**

Thêm state `tuKhoaTimKiem` vào `TrangBanBe.tsx`, ô input ngay dưới thẻ mở `<div className="trang-ban-be">`:

```tsx
      <input
        type="text"
        className="trang-ban-be__tim-kiem"
        placeholder="Tìm bạn bè..."
        value={tuKhoaTimKiem}
        onChange={(su) => setTuKhoaTimKiem(su.target.value)}
      />
```

Áp dụng lọc vào DUY NHẤT danh sách "Tìm người để kết bạn" (phần còn lại — lời mời đến/danh sách bạn bè — giữ nguyên không lọc, vì đó không phải là danh sách để "tìm kiếm" mà là danh sách cố định cần xử lý):

```tsx
          {tatCaNguoiDung
            .filter((nd) => !idDaLaBanBeHoacDangCho.has(nd.id))
            .filter((nd) => nd.tenTaiKhoan.toLowerCase().includes(tuKhoaTimKiem.toLowerCase()))
            .map((nd) => (
```

Thêm CSS vào `TrangBanBe.css`:

```css
.trang-ban-be__tim-kiem {
  width: 100%;
  padding: 10px 14px;
  border: 1px solid var(--mau-vien);
  border-radius: var(--ban-kinh-o);
  font-family: inherit;
  font-size: 14px;
  margin-bottom: 24px;
}
```

- [ ] **Step 6: `TrangCaiDat` — 4 mục con**

Viết lại toàn bộ `TrangCaiDat.tsx`:

```tsx
import { useEffect, useState } from 'react';
import { LayThongTinCaNhan, CapNhatCaiDat, LoiGoiApi } from '../DichVuApi';
import { useXacThuc } from '../NguCanh/NguCanhXacThuc';
import './TrangCaiDat.css';

type MucCaiDat = 'quyen-rieng-tu' | 'tai-khoan' | 'bao-mat' | 'thong-bao';

export function TrangCaiDat() {
  const { token, nguoiDungHienTai, dangXuat } = useXacThuc();
  const [mucDangChon, setMucDangChon] = useState<MucCaiDat>('quyen-rieng-tu');
  const [choPhepTinNhanTuNguoiLa, setChoPhepTinNhanTuNguoiLa] = useState(false);
  const [hienThiTrangThaiHoatDong, setHienThiTrangThaiHoatDong] = useState(true);
  const [dangTai, setDangTai] = useState(true);
  const [dangLuu, setDangLuu] = useState(false);
  const [daLuu, setDaLuu] = useState(false);
  const [loi, setLoi] = useState<string | null>(null);

  useEffect(() => {
    if (!token) return;
    LayThongTinCaNhan(token)
      .then((hoSo) => {
        setChoPhepTinNhanTuNguoiLa(hoSo.choPhepTinNhanTuNguoiLa);
        setHienThiTrangThaiHoatDong(hoSo.hienThiTrangThaiHoatDong);
      })
      .catch((loiBat) => setLoi(loiBat instanceof Error ? loiBat.message : 'Không tải được cài đặt.'))
      .finally(() => setDangTai(false));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [token]);

  async function luu(choPhepMoi: boolean, hienThiMoi: boolean) {
    if (!token) return;
    setChoPhepTinNhanTuNguoiLa(choPhepMoi);
    setHienThiTrangThaiHoatDong(hienThiMoi);
    setDangLuu(true);
    setDaLuu(false);
    try {
      await CapNhatCaiDat(token, choPhepMoi, hienThiMoi);
      setDaLuu(true);
    } catch (loiBat) {
      setLoi(loiBat instanceof LoiGoiApi ? loiBat.message : 'Lưu cài đặt thất bại.');
    } finally {
      setDangLuu(false);
    }
  }

  return (
    <div className="trang-cai-dat">
      <nav className="trang-cai-dat__menu">
        <button className={mucDangChon === 'quyen-rieng-tu' ? 'trang-cai-dat__muc-menu--chon' : 'trang-cai-dat__muc-menu'} onClick={() => setMucDangChon('quyen-rieng-tu')}>
          Quyền riêng tư
        </button>
        <button className={mucDangChon === 'tai-khoan' ? 'trang-cai-dat__muc-menu--chon' : 'trang-cai-dat__muc-menu'} onClick={() => setMucDangChon('tai-khoan')}>
          Tài khoản
        </button>
        <button className={mucDangChon === 'bao-mat' ? 'trang-cai-dat__muc-menu--chon' : 'trang-cai-dat__muc-menu'} onClick={() => setMucDangChon('bao-mat')}>
          Bảo mật
        </button>
        <button className={mucDangChon === 'thong-bao' ? 'trang-cai-dat__muc-menu--chon' : 'trang-cai-dat__muc-menu'} onClick={() => setMucDangChon('thong-bao')}>
          Thông báo
        </button>
      </nav>

      <div className="trang-cai-dat__noi-dung">
        {loi && (
          <p className="thong-bao-loi" role="alert">
            {loi}
          </p>
        )}

        {mucDangChon === 'quyen-rieng-tu' && (
          <>
            <h2>Quyền riêng tư</h2>
            <label className="trang-cai-dat__dong">
              <input
                type="checkbox"
                checked={choPhepTinNhanTuNguoiLa}
                disabled={dangTai || dangLuu}
                onChange={(su) => luu(su.target.checked, hienThiTrangThaiHoatDong)}
              />
              <span>Cho phép người lạ (chưa kết bạn) nhắn tin cho tôi</span>
            </label>
            <label className="trang-cai-dat__dong">
              <input
                type="checkbox"
                checked={hienThiTrangThaiHoatDong}
                disabled={dangTai || dangLuu}
                onChange={(su) => luu(choPhepTinNhanTuNguoiLa, su.target.checked)}
              />
              <span>Hiển thị trạng thái hoạt động (online/offline) cho bạn bè</span>
            </label>
            {daLuu && <p className="trang-cai-dat__da-luu">Đã lưu.</p>}
          </>
        )}

        {mucDangChon === 'tai-khoan' && (
          <>
            <h2>Tài khoản</h2>
            <p><strong>Tên tài khoản:</strong> {nguoiDungHienTai?.tenTaiKhoan}</p>
            <p><strong>Email:</strong> {nguoiDungHienTai?.email}</p>
            <button className="trang-cai-dat__nut-sap-ra-mat" disabled title="Sắp ra mắt">Đổi mật khẩu</button>
            <button className="trang-cai-dat__nut-dang-xuat" onClick={dangXuat}>Đăng xuất</button>
          </>
        )}

        {mucDangChon === 'bao-mat' && (
          <>
            <h2>Bảo mật</h2>
            <p className="trang-cai-dat__sap-ra-mat">🔒 Mã hóa tin nhắn AES-256-GCM + quản lý khóa RSA — sắp ra mắt (GĐ6).</p>
          </>
        )}

        {mucDangChon === 'thong-bao' && (
          <>
            <h2>Thông báo</h2>
            <p className="trang-cai-dat__sap-ra-mat">🔔 Tùy chỉnh loại thông báo — sắp ra mắt.</p>
          </>
        )}
      </div>
    </div>
  );
}
```

Cập nhật `TrangCaiDat.css` — thêm layout 2 cột (menu trái + nội dung phải, đúng ảnh mockup 6), giữ nguyên `.trang-cai-dat__dong`/`.trang-cai-dat__da-luu` đã có:

```css
.trang-cai-dat {
  max-width: 900px;
  margin: 0 auto;
  padding: 32px 24px;
  display: flex;
  gap: 32px;
}

.trang-cai-dat__menu {
  width: 200px;
  flex-shrink: 0;
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.trang-cai-dat__muc-menu,
.trang-cai-dat__muc-menu--chon {
  text-align: left;
  border: none;
  background: none;
  padding: 10px 14px;
  border-radius: var(--ban-kinh-o);
  font-family: inherit;
  font-size: 14px;
  cursor: pointer;
  color: var(--mau-chu-dam);
}

.trang-cai-dat__muc-menu--chon {
  background: var(--mau-nen-tren);
  color: var(--mau-chinh-dam);
  font-weight: 700;
}

.trang-cai-dat__noi-dung {
  flex: 1;
  min-width: 0;
}

.trang-cai-dat__noi-dung h2 {
  font-size: 16px;
  margin-bottom: 20px;
}

.trang-cai-dat__nut-sap-ra-mat {
  margin: 12px 0;
  border: 1px solid var(--mau-vien);
  background: #fff;
  color: var(--mau-chu-phu);
  border-radius: var(--ban-kinh-o);
  padding: 8px 16px;
  font-family: inherit;
  cursor: not-allowed;
}

.trang-cai-dat__nut-dang-xuat {
  display: block;
  border: 1px solid var(--mau-loi);
  background: #fff;
  color: var(--mau-loi);
  border-radius: var(--ban-kinh-o);
  padding: 8px 16px;
  font-family: inherit;
  cursor: pointer;
  font-weight: 600;
}

.trang-cai-dat__sap-ra-mat {
  color: var(--mau-chu-phu);
  font-size: 13px;
}

@media (max-width: 640px) {
  .trang-cai-dat {
    flex-direction: column;
  }

  .trang-cai-dat__menu {
    width: 100%;
    flex-direction: row;
    overflow-x: auto;
  }
}
```

- [ ] **Step 7: Cập nhật test hiện có bị ảnh hưởng**

Mở `frontend/src/Trang/TrangCaiDat.test.tsx` (nếu tồn tại — kiểm tra bằng cách đọc file trước khi sửa) và cập nhật mock `LayThongTinCaNhan` để trả về đủ field `hienThiTrangThaiHoatDong`, cập nhật assertion `CapNhatCaiDat` được gọi với 3 tham số thay vì 2. Áp dụng đúng pattern đã dùng ở Task 8 Step 3 cho file `DichVuApi.test.ts` (thêm tham số thứ 3, giữ nguyên cấu trúc test).

Mở `frontend/src/Trang/TrangChat.test.tsx` — xác nhận lại các test đã sửa ở Task 9 vẫn phản ánh đúng cấu trúc JSX mới (class `trang-chat--da-chon` không ảnh hưởng tới các query bằng text/role hiện có, không cần sửa thêm trừ khi 1 test cụ thể query trực tiếp bằng class CSS).

- [ ] **Step 8: Build và test toàn bộ (backend + frontend) trước khi kết thúc plan**

Run: `cd backend && dotnet build HaloChat.sln && dotnet test HaloChat.sln`
Expected: 0 lỗi, toàn bộ test pass.

Run: `cd frontend && npm run test -- --run && npm run build`
Expected: toàn bộ test pass, build thành công.

- [ ] **Step 9: Commit**

```bash
git add -A
git commit -m "Frontend: responsive 1-cot mobile + tim kiem sidebar + TrangCaiDat 4 muc con"
```

---

## Sau khi hoàn thành tất cả 12 task

Dùng **superpowers:finishing-a-development-branch** để quyết định merge/push/giữ nguyên — dự án làm việc trực tiếp trên `main` (không dùng worktree/branch riêng, theo thói quen đã thiết lập từ GĐ3), nên bước "review toàn bộ nhánh" ở cuối `subagent-driven-development` vẫn chạy trên diff từ commit đầu Task 1 tới commit cuối Task 12 trước khi quyết định push lên `origin/main`.

