# GĐ6a — Trả lời tin nhắn + nút tải file luôn hiện — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Thêm nút tải xuống hiện sẵn trên tin nhắn File, và thêm luồng
trả lời tin nhắn (icon nổi khi hover/bấm, khối "đang trả lời" phía trên
ô nhập, trích dẫn snapshot gắn kèm khi gửi và hiển thị trong bong bóng).

**Architecture:** Backend lưu snapshot thông tin tin gốc (không tham
chiếu sống) ngay vào tin nhắn mới khi gửi, qua 1 tham số mới
`traLoiId` xuyên suốt Hub → Service → DTO. Frontend thêm state cục bộ
trong `KhungTinNhan` cho chế độ "đang trả lời", dùng CSS `:hover` (không
JS) để hiện icon nổi trên desktop, tái dùng state click hiện có cho
mobile.

**Tech Stack:** ASP.NET Core .NET 9 (backend/HaloChat.Api) + MongoDB
Driver, xUnit; React 19 + TypeScript + Vite (frontend/), Vitest +
Testing Library.

**Spec:** `docs/superpowers/specs/2026-09-16-halochat-tra-loi-tin-nhan.md`

## Global Constraints

- Đặt tên định danh (biến, hàm, route, class) không dấu tiếng Việt.
- `TinNhanDto`/`IDichVuTinNhan.GuiTinNhanAsync` chỉ được **thêm** tham
  số vào CUỐI, không xóa/sắp xếp lại tham số hiện có.
- Trích dẫn trả lời là **snapshot tại thời điểm gửi**, không tham chiếu
  sống tới tin gốc (không join lại khi tin gốc đổi sau này).
- Bấm vào khối trích dẫn KHÔNG cuộn tới tin gốc (đã chốt — ngoài phạm vi).
- Icon "..." (menu hành động) KHÔNG render ở GĐ6a — chỉ render icon
  Trả lời. Icon "..." thuộc GĐ6b.
- Nội dung text trích dẫn rút gọn tối đa 80 ký tự + `"…"` nếu dài hơn.

---

## Task 1: Backend — `TinNhan.TimTheoIdAsync` + model `TraLoiThongTin`

**Files:**
- Create: `backend/HaloChat.Api/Models/TraLoiThongTin.cs`
- Modify: `backend/HaloChat.Api/Models/TinNhan.cs`
- Modify: `backend/HaloChat.Api/Repositories/ITinNhanRepository.cs`
- Modify: `backend/HaloChat.Api/Repositories/TinNhanRepository.cs`
- Modify: `backend/HaloChat.Api.Tests/Fakes/TinNhanGiaLap.cs`
- Test: `backend/HaloChat.Api.Tests/Repositories/TinNhanGiaLapTimTheoIdTests.cs` (thư mục `Repositories/` đã tồn tại — chứa `NguoiDungRepositoryOtpTests.cs`, namespace `HaloChat.Api.Tests.Repositories`)

**Interfaces:**
- Consumes: không có (task nền tảng).
- Produces: `ITinNhanRepository.TimTheoIdAsync(string id): Task<TinNhan?>`
  — Task 2 dùng để validate trả lời; `TraLoiThongTin` (model nhúng) —
  Task 2 dùng để lưu snapshot.

- [ ] **Step 1: Tạo model `TraLoiThongTin`**

Tạo `backend/HaloChat.Api/Models/TraLoiThongTin.cs`:

```csharp
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HaloChat.Api.Models;

public class TraLoiThongTin
{
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;
    public string TenNguoiGui { get; set; } = string.Empty;
    public string NoiDungTomTat { get; set; } = string.Empty;
    [BsonRepresentation(BsonType.String)]
    public LoaiTinNhan LoaiTinNhan { get; set; }
}
```

- [ ] **Step 2: Thêm field `TraLoi` vào `TinNhan.cs`**

Mở `backend/HaloChat.Api/Models/TinNhan.cs`, thêm vào cuối class (ngay
trước dấu `}` cuối file, sau field `ThoiGianTao`):

```csharp
    // [GĐ6a] Snapshot thông tin tin gốc tại thời điểm trả lời — KHÔNG
    // tham chiếu sống. Null nếu tin này không phải trả lời tin nào.
    public TraLoiThongTin? TraLoi { get; set; }
```

- [ ] **Step 3: Viết test cho `TimTheoIdAsync` (FAIL trước)**

Tạo `backend/HaloChat.Api.Tests/Repositories/TinNhanGiaLapTimTheoIdTests.cs`:

```csharp
using HaloChat.Api.Models;
using HaloChat.Api.Tests.Fakes;
using Xunit;

namespace HaloChat.Api.Tests.Repositories;

public class TinNhanGiaLapTimTheoIdTests
{
    [Fact]
    public async Task TimTheoIdAsync_TonTai_TraVeDungTinNhan()
    {
        var kho = new TinNhanGiaLap();
        var tinNhan = new TinNhan { NoiDungTinNhan = "Xin chào" };
        kho.DanhSach.Add(tinNhan);

        var ketQua = await kho.TimTheoIdAsync(tinNhan.Id);

        Assert.NotNull(ketQua);
        Assert.Equal("Xin chào", ketQua!.NoiDungTinNhan);
    }

    [Fact]
    public async Task TimTheoIdAsync_KhongTonTai_TraVeNull()
    {
        var kho = new TinNhanGiaLap();

        var ketQua = await kho.TimTheoIdAsync("507f1f77bcf86cd799439099");

        Assert.Null(ketQua);
    }
}
```

- [ ] **Step 4: Chạy test để thấy FAIL**

Run: `cd backend && dotnet test --filter TimTheoIdAsync`
Expected: FAIL biên dịch (`TimTheoIdAsync` chưa tồn tại trên
`TinNhanGiaLap`/`ITinNhanRepository`).

- [ ] **Step 5: Thêm `TimTheoIdAsync` vào interface + repository thật**

`backend/HaloChat.Api/Repositories/ITinNhanRepository.cs` — thêm vào
cuối interface (trước dấu `}`):

```csharp
    /// <summary>Lấy 1 tin nhắn theo id, null nếu không tồn tại. Dùng để validate trả lời (GĐ6a) và các hành động trên tin nhắn (GĐ6b).</summary>
    Task<TinNhan?> TimTheoIdAsync(string id);
```

`backend/HaloChat.Api/Repositories/TinNhanRepository.cs` — thêm method
mới cuối class (biến collection đã xác nhận tên là `_collection`,
khai báo ở constructor: `_collection = csdl.GetCollection<TinNhan>("TinNhan");`):

```csharp
    public async Task<TinNhan?> TimTheoIdAsync(string id)
    {
        return await _collection.Find(t => t.Id == id).FirstOrDefaultAsync();
    }
```

- [ ] **Step 6: Thêm `TimTheoIdAsync` vào fake `TinNhanGiaLap`**

`backend/HaloChat.Api.Tests/Fakes/TinNhanGiaLap.cs` — thêm method mới
(cuối class, trước dấu `}` cuối file):

```csharp
    public Task<TinNhan?> TimTheoIdAsync(string id)
    {
        return Task.FromResult(DanhSach.FirstOrDefault(t => t.Id == id));
    }
```

- [ ] **Step 7: Chạy test để thấy PASS**

Run: `cd backend && dotnet test --filter TimTheoIdAsync`
Expected: PASS (2/2).

- [ ] **Step 8: Build + chạy toàn bộ test backend**

Run: `cd backend && dotnet build && dotnet test`
Expected: build 0 lỗi; toàn bộ test PASS (thêm field `TraLoi` có mặc
định `null` nên không vỡ gì; thêm method vào interface không ảnh hưởng
implementer khác trừ `TinNhanGiaLap` đã cập nhật ở Step 6 — kiểm tra
không còn implementer nào khác của `ITinNhanRepository` bị thiếu bằng
cách xem lỗi build nếu có).

- [ ] **Step 9: Commit**

```bash
git add backend/HaloChat.Api/Models/TraLoiThongTin.cs backend/HaloChat.Api/Models/TinNhan.cs backend/HaloChat.Api/Repositories/ITinNhanRepository.cs backend/HaloChat.Api/Repositories/TinNhanRepository.cs backend/HaloChat.Api.Tests/Fakes/TinNhanGiaLap.cs backend/HaloChat.Api.Tests/Repositories/TinNhanGiaLapTimTheoIdTests.cs
git commit -m "feat(backend): them TinNhan.TimTheoIdAsync va model TraLoiThongTin (GD6a)"
```

---

## Task 2: Backend — validate + lưu snapshot trả lời khi gửi tin

**Files:**
- Create: `backend/HaloChat.Api/Dto/TraLoiThongTinDto.cs`
- Modify: `backend/HaloChat.Api/Dto/TinNhanDto.cs`
- Modify: `backend/HaloChat.Api/Services/IDichVuTinNhan.cs`
- Modify: `backend/HaloChat.Api/Services/DichVuTinNhan.cs`
- Modify: `backend/HaloChat.Api/Hubs/ChatHub.cs`
- Modify: `backend/HaloChat.Api.Tests/ChatHubTests.cs`
- Test: `backend/HaloChat.Api.Tests/Services/DichVuTinNhanTests.cs`

**Interfaces:**
- Consumes: `ITinNhanRepository.TimTheoIdAsync` (Task 1),
  `NguoiDung.TenHienThiThucTe()` (đã có sẵn từ GĐ5f).
- Produces: `TinNhanDto` có thêm field cuối `TraLoiThongTinDto? TraLoi`
  — Task 3 (frontend) đọc field này qua JSON (`traLoi` camelCase).
  `IDichVuTinNhan.GuiTinNhanAsync(...)` có thêm tham số cuối
  `string? traLoiId` — hub `GuiTinNhan` cũng thêm tham số cuối cùng tên.

- [ ] **Step 1: Tạo `TraLoiThongTinDto`**

Tạo `backend/HaloChat.Api/Dto/TraLoiThongTinDto.cs`:

```csharp
namespace HaloChat.Api.Dto;

public record TraLoiThongTinDto(string Id, string TenNguoiGui, string NoiDungTomTat, string LoaiTinNhan);
```

- [ ] **Step 2: Viết 4 test mới cho luồng trả lời (FAIL trước)**

Mở `backend/HaloChat.Api.Tests/Services/DichVuTinNhanTests.cs`, thêm vào
cuối class (trước dấu `}` cuối file):

```csharp
    // --- Trả lời tin nhắn (GĐ6a) ---

    [Fact]
    public async Task GuiTinNhanAsync_CoTraLoiHopLe_LuuSnapshotDungThongTin()
    {
        var (dichVu, _, khoNguoiDung, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdNguoiGui, TenTaiKhoan = "NguoiGui", ChoPhepTinNhanTuNguoiLa = true });
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        var tinA = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Tin gốc", null, null, null, null, null);

        var tinB = await dichVu.GuiTinNhanAsync(IdNguoiNhan, IdNguoiGui, null, "Text", "Tin trả lời", null, null, null, null, tinA.Id);

        Assert.NotNull(tinB.TraLoi);
        Assert.Equal(tinA.Id, tinB.TraLoi!.Id);
        Assert.Equal("NguoiGui", tinB.TraLoi.TenNguoiGui);
        Assert.Equal("Tin gốc", tinB.TraLoi.NoiDungTomTat);
        Assert.Equal("Text", tinB.TraLoi.LoaiTinNhan);
    }

    [Fact]
    public async Task GuiTinNhanAsync_TraLoiTinKhongTonTai_NemTinNhanKhongHopLe()
    {
        var (dichVu, _, khoNguoiDung, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());

        await Assert.ThrowsAsync<TinNhanKhongHopLeException>(() =>
            dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Trả lời", null, null, null, null, "507f1f77bcf86cd799439099"));
    }

    [Fact]
    public async Task GuiTinNhanAsync_TraLoiTinThuocCuocTroChuyenKhac_NemTinNhanKhongHopLe()
    {
        const string IdNguoiThuBa = "507f1f77bcf86cd799439013";
        var (dichVu, _, khoNguoiDung, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdNguoiGui, TenTaiKhoan = "NguoiGui", ChoPhepTinNhanTuNguoiLa = true });
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdNguoiThuBa, TenTaiKhoan = "NguoiThuBa", ChoPhepTinNhanTuNguoiLa = true });
        var tinGiuaGuiVaNhan = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Tin giữa A-B", null, null, null, null, null);

        await Assert.ThrowsAsync<TinNhanKhongHopLeException>(() =>
            dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiThuBa, null, "Text", "Trả lời sai cuộc trò chuyện", null, null, null, null, tinGiuaGuiVaNhan.Id));
    }

    [Fact]
    public async Task GuiTinNhanAsync_NoiDungTextDaiHon80KyTu_RutGonConDauBaChamCuoi()
    {
        var (dichVu, _, khoNguoiDung, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdNguoiGui, TenTaiKhoan = "NguoiGui", ChoPhepTinNhanTuNguoiLa = true });
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        var noiDungDai = new string('a', 100);
        var tinA = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", noiDungDai, null, null, null, null, null);

        var tinB = await dichVu.GuiTinNhanAsync(IdNguoiNhan, IdNguoiGui, null, "Text", "Trả lời", null, null, null, null, tinA.Id);

        Assert.Equal(new string('a', 80) + "…", tinB.TraLoi!.NoiDungTomTat);
    }
```

- [ ] **Step 3: Chạy test để thấy FAIL**

Run: `cd backend && dotnet test --filter DichVuTinNhanTests`
Expected: FAIL biên dịch (`GuiTinNhanAsync` chưa nhận 10 tham số,
`TinNhanDto.TraLoi` chưa tồn tại).

- [ ] **Step 4: Cập nhật `TinNhanDto`**

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
    DateTime ThoiGianTao,
    TraLoiThongTinDto? TraLoi);
```

- [ ] **Step 5: Cập nhật `IDichVuTinNhan.cs`**

Đổi dòng khai báo `GuiTinNhanAsync` thành:

```csharp
    Task<TinNhanDto> GuiTinNhanAsync(
        string nguoiGuiId, string? nguoiNhanId, string? nhomId, string loaiTinNhan, string noiDungTinNhan,
        string? duongDanFile, string? tenFileGoc, long? kichThuocFile, string? loaiFile, string? traLoiId);
```

- [ ] **Step 6: Cập nhật `DichVuTinNhan.GuiTinNhanAsync`**

Mở `backend/HaloChat.Api/Services/DichVuTinNhan.cs`. Đổi chữ ký hàm
`GuiTinNhanAsync` thành có thêm tham số cuối `string? traLoiId`. Sau
khối `if (nhomId is not null) {...} else {...}` hiện có (khối set
`tinNhan.NhomId`/`tinNhan.NguoiNhanId`/`tinNhan.DaNhan`) và TRƯỚC dòng
`await _khoTinNhan.ThemMoiAsync(tinNhan);`, chèn:

```csharp
        traLoiId = string.IsNullOrEmpty(traLoiId) ? null : traLoiId;
        if (traLoiId is not null)
        {
            if (!ObjectId.TryParse(traLoiId, out _))
            {
                throw new TinNhanKhongHopLeException("Tin nhắn được trả lời không hợp lệ.");
            }

            var tinGoc = await _khoTinNhan.TimTheoIdAsync(traLoiId)
                ?? throw new TinNhanKhongHopLeException("Tin nhắn được trả lời không tồn tại.");

            var cungHoiThoai = nhomId is not null
                ? tinGoc.NhomId == nhomId
                : tinGoc.NhomId is null
                    && new[] { tinGoc.NguoiGuiId, tinGoc.NguoiNhanId }.Contains(nguoiGuiId)
                    && new[] { tinGoc.NguoiGuiId, tinGoc.NguoiNhanId }.Contains(nguoiNhanId);
            if (!cungHoiThoai)
            {
                throw new TinNhanKhongHopLeException("Tin nhắn được trả lời không thuộc cuộc trò chuyện này.");
            }

            var nguoiGuiGoc = await _khoNguoiDung.TimTheoIdAsync(tinGoc.NguoiGuiId);
            var tenNguoiGuiGoc = nguoiGuiGoc?.TenHienThiThucTe() ?? "Người dùng đã xoá";
            var noiDungTomTat = tinGoc.LoaiTinNhan switch
            {
                LoaiTinNhan.Text => tinGoc.NoiDungTinNhan.Length > 80
                    ? tinGoc.NoiDungTinNhan[..80] + "…"
                    : tinGoc.NoiDungTinNhan,
                LoaiTinNhan.Anh => "[Ảnh]",
                _ => $"[File] {tinGoc.TenFileGoc}",
            };

            tinNhan.TraLoi = new TraLoiThongTin
            {
                Id = tinGoc.Id,
                TenNguoiGui = tenNguoiGuiGoc,
                NoiDungTomTat = noiDungTomTat,
                LoaiTinNhan = tinGoc.LoaiTinNhan,
            };
        }
```

Cập nhật `AnhXaDto` (private static method cuối file) thành:

```csharp
    private static TinNhanDto AnhXaDto(TinNhan t) => new(
        t.Id, t.NguoiGuiId, t.NguoiNhanId, t.NhomId, t.LoaiTinNhan.ToString(), t.NoiDungTinNhan,
        t.DuongDanFile, t.TenFileGoc, t.KichThuocFile, t.LoaiFile, t.DaDoc, t.DaNhan, t.ThoiGianTao,
        t.TraLoi is null ? null : new TraLoiThongTinDto(t.TraLoi.Id, t.TraLoi.TenNguoiGui, t.TraLoi.NoiDungTomTat, t.TraLoi.LoaiTinNhan.ToString()));
```

- [ ] **Step 7: Build để lộ mọi điểm gọi bị vỡ do thêm tham số**

Run: `cd backend && dotnet build`
Expected: FAIL — liệt kê lỗi CS7036 tại `ChatHub.cs` (1 chỗ) và
`DichVuTinNhanTests.cs` (18 chỗ, TẤT CẢ lời gọi `GuiTinNhanAsync(...)`
đang có sẵn TRƯỚC bước này — không tính 4 test mới ở Step 2 đã tự viết
đủ 10 tham số).

- [ ] **Step 8: Thêm `null` vào cuối MỌI lời gọi `GuiTinNhanAsync` cũ**

Trong `backend/HaloChat.Api.Tests/Services/DichVuTinNhanTests.cs`, thêm
`, null` vào cuối THAM SỐ (trước dấu đóng ngoặc `)`) của từng lời gọi
`dichVu.GuiTinNhanAsync(...)` đã tồn tại TRƯỚC Step 2 (không đụng 4 test
mới vừa viết, chúng đã có `traLoiId` đúng vị trí). Dùng lệnh build ở
Step 7 để xác nhận đã sửa đủ — KHÔNG dùng số dòng cố định vì việc thêm 4
test mới ở Step 2 dịch số dòng các test sau đó; định vị bằng nội dung
"GuiTinNhanAsync(" và biên dịch lại tới khi hết lỗi CS7036 trong file
test này.

- [ ] **Step 9: Cập nhật `ChatHub.cs`**

Mở `backend/HaloChat.Api/Hubs/ChatHub.cs`, đổi method `GuiTinNhan`
thành:

```csharp
    public async Task<TinNhanDto> GuiTinNhan(
        string? nguoiNhanId, string? nhomId, string loaiTinNhan, string noiDungTinNhan,
        string? duongDanFile, string? tenFileGoc, long? kichThuocFile, string? loaiFile, string? traLoiId)
    {
        try
        {
            var tinNhan = await _dichVuTinNhan.GuiTinNhanAsync(
                NguoiDungHienTaiId, nguoiNhanId, nhomId, loaiTinNhan, noiDungTinNhan,
                duongDanFile, tenFileGoc, kichThuocFile, loaiFile, traLoiId);

            if (nhomId is not null)
            {
                await Clients.OthersInGroup("nhom-" + nhomId).SendAsync("NhanTinNhan", tinNhan);
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
```

(Chỉ đổi chữ ký hàm và lời gọi `GuiTinNhanAsync` — phần thân `try/catch`
còn lại giữ nguyên y hệt bản gốc.)

- [ ] **Step 10: Build lại, xác nhận 0 lỗi**

Run: `cd backend && dotnet build`
Expected: 0 lỗi.

- [ ] **Step 11: Sửa 4 lời gọi `InvokeAsync` trong `ChatHubTests.cs`**

QUAN TRỌNG: `ChatHubTests.cs` gọi hub qua `HubConnection.InvokeAsync(...)`
— đây là lời gọi dynamic qua `object[]`, KHÔNG được `dotnet build` kiểm
tra kiểu tĩnh. Thêm hub tham số mới ở Step 9 sẽ khiến các lời gọi này
LỖI KHI CHẠY (SignalR báo sai số lượng tham số) chứ không lỗi khi build
— phải tự tìm và sửa thủ công, không dựa vào build để phát hiện. Mở
`backend/HaloChat.Api.Tests/ChatHubTests.cs`, thêm `, null` vào cuối
tham số của ĐÚNG 4 lời gọi `InvokeAsync<TinNhanDto>("GuiTinNhan", ...)`
(dòng 77, 95, 133, 166 tại thời điểm viết plan — định vị bằng nội dung
`"GuiTinNhan"` nếu số dòng lệch do các sửa trước đó dịch chuyển dòng).

Ví dụ dòng 76-77 hiện tại:
```csharp
        var tinNhanGui = await ketNoiA.InvokeAsync<TinNhanDto>(
            "GuiTinNhan", idNguoiB, null, "Text", "Chào bạn", null, null, null, null);
```
sửa thành:
```csharp
        var tinNhanGui = await ketNoiA.InvokeAsync<TinNhanDto>(
            "GuiTinNhan", idNguoiB, null, "Text", "Chào bạn", null, null, null, null, null);
```
Áp dụng đúng cách thêm 1 `null` cuối cùng cho cả 4 lời gọi.

- [ ] **Step 12: Chạy toàn bộ test backend**

Run: `cd backend && dotnet test`
Expected: PASS toàn bộ, bao gồm 4 test mới ở Step 2 VÀ toàn bộ
`ChatHubTests.cs` (những test này SẼ FAIL/timeout nếu Step 11 bị bỏ
sót — xác nhận kỹ log test thay vì chỉ đọc dòng tổng kết PASS/FAIL).

- [ ] **Step 13: Commit**

```bash
git add backend/HaloChat.Api/Dto/TraLoiThongTinDto.cs backend/HaloChat.Api/Dto/TinNhanDto.cs backend/HaloChat.Api/Services/IDichVuTinNhan.cs backend/HaloChat.Api/Services/DichVuTinNhan.cs backend/HaloChat.Api/Hubs/ChatHub.cs backend/HaloChat.Api.Tests/Services/DichVuTinNhanTests.cs backend/HaloChat.Api.Tests/ChatHubTests.cs
git commit -m "feat(backend): validate va luu snapshot tra loi tin nhan khi gui (GD6a)"
```

---

## Task 3: Frontend — UI trả lời + card File trong `KhungTinNhan.tsx`

**Files:**
- Modify: `frontend/src/KieuDuLieu.ts`
- Modify: `frontend/src/ThanhPhan/BieuTuong.tsx`
- Modify: `frontend/src/ThanhPhan/KhungTinNhan.tsx`
- Modify: `frontend/src/ThanhPhan/KhungTinNhan.css`
- Test: `frontend/src/ThanhPhan/KhungTinNhan.test.tsx`

**Interfaces:**
- Consumes: `TinNhan.traLoi` (JSON field từ backend, Task 2).
- Produces: `PropsKhungTinNhan.onGuiVanBan: (noiDung: string, traLoiId: string | null) => void`,
  `PropsKhungTinNhan.onGuiTep: (tep: File, traLoiId: string | null) => void`,
  `PropsKhungTinNhan.layTenNguoiGui?: (nguoiGuiId: string) => string` —
  Task 4 (TrangChat.tsx/TrangNhom.tsx) phải cập nhật theo đúng 3 chữ ký
  này.

- [ ] **Step 1: Thêm field `traLoi` vào kiểu `TinNhan`**

`frontend/src/KieuDuLieu.ts` — sửa interface `TinNhan`, thêm field cuối:

```typescript
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
  traLoi: { id: string; tenNguoiGui: string; noiDungTomTat: string; loaiTinNhan: LoaiTinNhan } | null;
}
```

- [ ] **Step 2: Build TypeScript để lộ fixture thiếu field (nếu có)**

Run: `cd frontend && npx tsc --noEmit`
Expected: có thể FAIL tại các fixture `TinNhan` literal trong test hiện
có (`TrangChat.test.tsx`, `TrangNhom.test.tsx`, `KhungTinNhan.test.tsx`)
— ghi lại danh sách lỗi để sửa ở Task 4 (KHÔNG sửa test fixture trong
Task này — Task 3 chỉ sửa component/kiểu dữ liệu, việc sửa fixture của
2 trang thuộc Task 4). Nếu `tsc` KHÔNG báo lỗi (do mock đi qua đường
untyped như đã ghi nhận ở GĐ5f), bỏ qua bước này và tiếp tục — không
phải lỗi của bạn, Task 4 vẫn sẽ rà thủ công.

- [ ] **Step 3: Thêm 3 icon mới vào `BieuTuong.tsx`**

Mở `frontend/src/ThanhPhan/BieuTuong.tsx`, thêm vào cuối file (sau icon
cuối cùng đã có):

```tsx
export function BieuTuongTraLoi() {
  return (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
      <path d="M9 17l-5-5 5-5" />
      <path d="M4 12h10a5 5 0 0 1 5 5v2" />
    </svg>
  );
}

export function BieuTuongTaiLieu() {
  return (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
      <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
      <path d="M14 2v6h6" />
    </svg>
  );
}

export function BieuTuongTai() {
  return (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
      <path d="M12 3v12" />
      <path d="M7 10l5 5 5-5" />
      <path d="M4 21h16" />
    </svg>
  );
}
```

- [ ] **Step 4: Viết test mới cho `KhungTinNhan.tsx` (FAIL trước)**

Mở `frontend/src/ThanhPhan/KhungTinNhan.test.tsx`. File đã có sẵn 2
hằng số mẫu ở đầu file: `PROPS_MAC_DINH` (props mặc định cho
`KhungTinNhan`) và `TIN_NHAN_MAU` (1 tin nhắn Text mẫu, hiện KHÔNG có
field `traLoi`). Trước tiên sửa `TIN_NHAN_MAU`, thêm field cuối:

```typescript
const TIN_NHAN_MAU = {
  id: 'm1',
  nguoiGuiId: '1',
  nguoiNhanId: null,
  nhomId: 'n1',
  loaiTinNhan: 'Text' as const,
  noiDungTinNhan: 'Chào mọi người',
  duongDanFile: null,
  tenFileGoc: null,
  kichThuocFile: null,
  loaiFile: null,
  daDoc: false,
  daNhan: false,
  thoiGianTao: '2026-01-01T10:30:00.000Z',
  traLoi: null,
};
```

(Chỉ thêm dòng `traLoi: null,` cuối — mọi field khác giữ nguyên y hệt.
Bắt buộc phải sửa NGAY BÂY GIỜ, không để dành Task 4, vì `TIN_NHAN_MAU`
được nhiều test SẴN CÓ trong chính file này dùng — một khi
`TinNhan.traLoi` trở thành field bắt buộc ở Step 1, các test đó sẽ vỡ
biên dịch nếu không sửa.)

Sau đó thêm các test mới vào cuối file, bên trong khối `describe('KhungTinNhan', () => { ... })` hiện có (thêm ngay trước dấu `});` đóng khối describe):

```typescript
it('bam icon Tra loi hien khoi dang tra loi voi ten va trich dan dung', async () => {
  const tinGoc: TinNhan = {
    id: 'm1', nguoiGuiId: 'nguoi-kia', nguoiNhanId: 'toi', nhomId: null,
    loaiTinNhan: 'Text', noiDungTinNhan: 'Xin chào bạn', duongDanFile: null, tenFileGoc: null,
    kichThuocFile: null, loaiFile: null, daDoc: true, daNhan: true,
    thoiGianTao: '2026-01-01T00:00:00Z', traLoi: null,
  };
  render(<KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[tinGoc]} idHienTai="toi" tenHienThi="Nguoi Kia" />);

  await userEvent.click(screen.getByRole('button', { name: 'Trả lời tin nhắn này' }));

  expect(screen.getByText(/↩ Trả lời/)).toBeInTheDocument();
  expect(screen.getByText('Xin chào bạn')).toBeInTheDocument();
});

it('huy tra loi an khoi preview', async () => {
  const tinGoc: TinNhan = {
    id: 'm1', nguoiGuiId: 'nguoi-kia', nguoiNhanId: 'toi', nhomId: null,
    loaiTinNhan: 'Text', noiDungTinNhan: 'Xin chào bạn', duongDanFile: null, tenFileGoc: null,
    kichThuocFile: null, loaiFile: null, daDoc: true, daNhan: true,
    thoiGianTao: '2026-01-01T00:00:00Z', traLoi: null,
  };
  render(<KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[tinGoc]} idHienTai="toi" tenHienThi="Nguoi Kia" />);
  await userEvent.click(screen.getByRole('button', { name: 'Trả lời tin nhắn này' }));

  await userEvent.click(screen.getByRole('button', { name: 'Hủy trả lời' }));

  expect(screen.queryByText(/↩ Trả lời/)).not.toBeInTheDocument();
});

it('gui tin nhan luc dang tra loi goi onGuiVanBan voi dung traLoiId', async () => {
  const tinGoc: TinNhan = {
    id: 'm1', nguoiGuiId: 'nguoi-kia', nguoiNhanId: 'toi', nhomId: null,
    loaiTinNhan: 'Text', noiDungTinNhan: 'Xin chào bạn', duongDanFile: null, tenFileGoc: null,
    kichThuocFile: null, loaiFile: null, daDoc: true, daNhan: true,
    thoiGianTao: '2026-01-01T00:00:00Z', traLoi: null,
  };
  const onGuiVanBan = vi.fn();
  render(<KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[tinGoc]} idHienTai="toi" tenHienThi="Nguoi Kia" onGuiVanBan={onGuiVanBan} />);
  await userEvent.click(screen.getByRole('button', { name: 'Trả lời tin nhắn này' }));

  await userEvent.type(screen.getByPlaceholderText('Nhập tin nhắn...'), 'Đây là câu trả lời{enter}');

  expect(onGuiVanBan).toHaveBeenCalledWith('Đây là câu trả lời', 'm1');
});

it('tin nhan co truong traLoi hien khoi trich dan trong bong bong', () => {
  const tinTraLoi: TinNhan = {
    id: 'm2', nguoiGuiId: 'toi', nguoiNhanId: 'nguoi-kia', nhomId: null,
    loaiTinNhan: 'Text', noiDungTinNhan: 'Đây là câu trả lời', duongDanFile: null, tenFileGoc: null,
    kichThuocFile: null, loaiFile: null, daDoc: false, daNhan: false,
    thoiGianTao: '2026-01-01T00:01:00Z',
    traLoi: { id: 'm1', tenNguoiGui: 'Nguoi Kia', noiDungTomTat: 'Xin chào bạn', loaiTinNhan: 'Text' },
  };
  render(<KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[tinTraLoi]} idHienTai="toi" tenHienThi="Nguoi Kia" />);

  expect(screen.getByText('Nguoi Kia')).toBeInTheDocument();
  expect(screen.getByText('Xin chào bạn')).toBeInTheDocument();
});

it('card File hien nut tron tai xuong rieng biet', () => {
  const tinFile: TinNhan = {
    id: 'm3', nguoiGuiId: 'toi', nguoiNhanId: 'nguoi-kia', nhomId: null,
    loaiTinNhan: 'File', noiDungTinNhan: '', duongDanFile: '/api/tinnhan/file/507f1f77bcf86cd799439099',
    tenFileGoc: 'bao-cao.docx', kichThuocFile: 15360, loaiFile: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
    daDoc: false, daNhan: false, thoiGianTao: '2026-01-01T00:00:00Z', traLoi: null,
  };
  render(<KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[tinFile]} idHienTai="toi" tenHienThi="Nguoi Kia" />);

  expect(screen.getByText('bao-cao.docx')).toBeInTheDocument();
  expect(screen.getByRole('link', { name: 'Tải xuống bao-cao.docx' })).toBeInTheDocument();
});
```

- [ ] **Step 5: Chạy test để thấy FAIL**

Run: `cd frontend && npm test -- --run KhungTinNhan`
Expected: FAIL (UI trả lời/card File mới chưa tồn tại).

- [ ] **Step 6: Sửa `PropsKhungTinNhan` + thêm state trả lời**

Mở `frontend/src/ThanhPhan/KhungTinNhan.tsx`. Đổi phần đầu import và
interface:

```tsx
import { useEffect, useRef, useState, type FormEvent, type ChangeEvent } from 'react';
import { BieuTuongGhim, BieuTuongTraLoi, BieuTuongTaiLieu, BieuTuongTai } from './BieuTuong';
import { DIA_CHI_GOC } from '../DichVuApi';
import type { TinNhan } from '../KieuDuLieu';
import './KhungTinNhan.css';

export type TinNhanHienThi = TinNhan & { dangGui?: boolean };

function dinhDangKichThuoc(bytes: number): string {
  const mb = bytes / (1024 * 1024);
  return mb >= 1 ? `${mb.toFixed(1)}MB` : `${Math.ceil(bytes / 1024)}KB`;
}

function dinhDangGio(thoiGianTao: string): string {
  const ngay = new Date(thoiGianTao);
  return ngay.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });
}

function trichNoiDungTinNhan(tn: TinNhan): string {
  if (tn.loaiTinNhan === 'Text') return tn.noiDungTinNhan;
  if (tn.loaiTinNhan === 'Anh') return '[Ảnh]';
  return `[File] ${tn.tenFileGoc}`;
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
  onGuiVanBan: (noiDung: string, traLoiId: string | null) => void;
  onGuiTep: (tep: File, traLoiId: string | null) => void;
  dangTaiTep: boolean;
  loi: string | null;
  onQuayLai?: () => void;
  onBamTieuDe?: () => void;
  layTenNguoiGui?: (nguoiGuiId: string) => string;
}
```

Trong thân component `KhungTinNhan`, đổi dòng destructure props và
thêm state mới:

```tsx
export function KhungTinNhan({
  tenHienThi, phuDe, danhSachTinNhan, idHienTai, dangKetNoi, dangTaiLichSu,
  coTheTaiThem, onTaiThemLichSuCu, onGuiVanBan, onGuiTep, dangTaiTep, loi, onQuayLai, onBamTieuDe, layTenNguoiGui,
}: PropsKhungTinNhan) {
  const inputTepRef = useRef<HTMLInputElement | null>(null);
  const cuoiDanhSachRef = useRef<HTMLDivElement | null>(null);
  const noiDungRef = useRef<HTMLInputElement | null>(null);
  const [tinDangMoId, setTinDangMoId] = useState<string | null>(null);
  const [dangTraLoiId, setDangTraLoiId] = useState<string | null>(null);

  const tinDangTraLoi = danhSachTinNhan.find((tn) => tn.id === dangTraLoiId) ?? null;
```

- [ ] **Step 7: Cập nhật `xuLySubmit`/`xuLyChonTep`**

```tsx
  function xuLySubmit(su: FormEvent) {
    su.preventDefault();
    const gtHienTai = noiDungRef.current?.value.trim();
    if (!gtHienTai) return;
    onGuiVanBan(gtHienTai, dangTraLoiId);
    if (noiDungRef.current) noiDungRef.current.value = '';
    setDangTraLoiId(null);
  }

  function xuLyChonTep(su: ChangeEvent<HTMLInputElement>) {
    const tep = su.target.files?.[0];
    if (!tep) return;
    onGuiTep(tep, dangTraLoiId);
    setDangTraLoiId(null);
  }
```

- [ ] **Step 8: Bọc mỗi tin nhắn trong `.khung-tin-nhan__hang` + icon nổi**

Trong khối `{danhSachTinNhan.map((tn) => { ... })}`, thay toàn bộ JSX
trả về của mỗi tin nhắn (từ `<div key={tn.id} className={...khung-tin-nhan__bong...}>`
tới hết `</div>` đóng của nó) bằng:

```tsx
        {danhSachTinNhan.map((tn) => {
          const laCuaMinh = tn.nguoiGuiId === idHienTai;
          return (
            <div key={tn.id} className={`khung-tin-nhan__hang${laCuaMinh ? ' khung-tin-nhan__hang--minh' : ''}`}>
              <div className="khung-tin-nhan__icon-noi">
                <button
                  type="button"
                  className="khung-tin-nhan__nut-tra-loi"
                  onClick={(su) => { su.stopPropagation(); setDangTraLoiId(tn.id); noiDungRef.current?.focus(); }}
                  aria-label="Trả lời tin nhắn này"
                >
                  <BieuTuongTraLoi />
                </button>
              </div>
              <div
                className={`khung-tin-nhan__bong${laCuaMinh ? ' khung-tin-nhan__bong--minh' : ''}`}
                onClick={() => setTinDangMoId((truoc) => (truoc === tn.id ? null : tn.id))}
                role="button"
                tabIndex={0}
              >
                {tn.traLoi && (
                  <div className="khung-tin-nhan__trich-dan">
                    <span className="khung-tin-nhan__trich-dan-ten">{tn.traLoi.tenNguoiGui}</span>
                    <span className="khung-tin-nhan__trich-dan-noi-dung">{tn.traLoi.noiDungTomTat}</span>
                  </div>
                )}
                {tn.loaiTinNhan === 'Anh' && (
                  <img className="khung-tin-nhan__anh" src={`${DIA_CHI_GOC}${tn.duongDanFile}`} alt={tn.tenFileGoc ?? 'ảnh'} />
                )}
                {tn.loaiTinNhan === 'File' && (
                  <div className="khung-tin-nhan__file">
                    <span className="khung-tin-nhan__file-icon"><BieuTuongTaiLieu /></span>
                    <div className="khung-tin-nhan__file-thong-tin">
                      <span className="khung-tin-nhan__file-ten">{tn.tenFileGoc}</span>
                      <span className="khung-tin-nhan__file-size">{dinhDangKichThuoc(tn.kichThuocFile ?? 0)}</span>
                    </div>
                    <a
                      className="khung-tin-nhan__file-nut-tai"
                      href={`${DIA_CHI_GOC}${tn.duongDanFile}`}
                      target="_blank"
                      rel="noreferrer"
                      onClick={(su) => su.stopPropagation()}
                      aria-label={`Tải xuống ${tn.tenFileGoc}`}
                    >
                      <BieuTuongTai />
                    </a>
                  </div>
                )}
                {tn.loaiTinNhan === 'Text' && tn.noiDungTinNhan}
                {tinDangMoId === tn.id && (
                  <span className="khung-tin-nhan__thoi-gian">{dinhDangGio(tn.thoiGianTao)}</span>
                )}
              </div>
            </div>
          );
        })}
```

- [ ] **Step 9: Thêm khối "đang trả lời" trước `<form>`**

Ngay TRƯỚC dòng `<form className="khung-tin-nhan__form-gui" onSubmit={xuLySubmit}>`,
thêm:

```tsx
      {tinDangTraLoi && (
        <div className="khung-tin-nhan__dang-tra-loi">
          <div className="khung-tin-nhan__dang-tra-loi-noi-dung">
            <span className="khung-tin-nhan__dang-tra-loi-tieu-de">
              ↩ Trả lời {layTenNguoiGui ? layTenNguoiGui(tinDangTraLoi.nguoiGuiId) : 'một người dùng'}
            </span>
            <span className="khung-tin-nhan__dang-tra-loi-trich">{trichNoiDungTinNhan(tinDangTraLoi)}</span>
          </div>
          <button type="button" className="khung-tin-nhan__dang-tra-loi-huy" onClick={() => setDangTraLoiId(null)} aria-label="Hủy trả lời">×</button>
        </div>
      )}
```

- [ ] **Step 10: Thêm CSS mới**

Mở `frontend/src/ThanhPhan/KhungTinNhan.css`, thêm vào cuối file:

```css
.khung-tin-nhan__hang {
  position: relative;
  display: flex;
}

.khung-tin-nhan__hang--minh {
  justify-content: flex-end;
}

.khung-tin-nhan__icon-noi {
  position: absolute;
  top: -14px;
  right: 8px;
  display: none;
  gap: 4px;
}

.khung-tin-nhan__hang:hover .khung-tin-nhan__icon-noi,
.khung-tin-nhan__hang--mo .khung-tin-nhan__icon-noi {
  display: flex;
}

.khung-tin-nhan__nut-tra-loi {
  width: 26px;
  height: 26px;
  border-radius: 50%;
  border: 1px solid var(--mau-vien);
  background: var(--mau-nen-the);
  color: var(--mau-chu-dam);
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  box-shadow: 0 1px 4px rgba(0, 0, 0, 0.15);
}

.khung-tin-nhan__trich-dan {
  display: flex;
  flex-direction: column;
  gap: 1px;
  padding: 4px 8px;
  border-left: 3px solid currentColor;
  background: rgba(0, 0, 0, 0.06);
  border-radius: 6px;
  font-size: 12px;
  opacity: 0.9;
}

.khung-tin-nhan__trich-dan-ten {
  font-weight: 700;
}

.khung-tin-nhan__file {
  display: flex;
  align-items: center;
  gap: 10px;
}

.khung-tin-nhan__file-icon {
  flex-shrink: 0;
  width: 32px;
  height: 32px;
  border-radius: 8px;
  background: var(--mau-chinh-dam);
  color: #fff;
  display: flex;
  align-items: center;
  justify-content: center;
}

.khung-tin-nhan__file-thong-tin {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
}

.khung-tin-nhan__file-ten {
  font-weight: 600;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.khung-tin-nhan__file-size {
  font-size: 11px;
  opacity: 0.8;
}

.khung-tin-nhan__file-nut-tai {
  flex-shrink: 0;
  width: 30px;
  height: 30px;
  border-radius: 50%;
  background: rgba(255, 255, 255, 0.25);
  color: inherit;
  display: flex;
  align-items: center;
  justify-content: center;
}

.khung-tin-nhan__dang-tra-loi {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  padding: 8px 24px;
  border-top: 1px solid var(--mau-vien);
  background: var(--mau-nen-tren);
  font-size: 13px;
}

.khung-tin-nhan__dang-tra-loi-noi-dung {
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.khung-tin-nhan__dang-tra-loi-tieu-de {
  font-weight: 700;
  color: var(--mau-chinh-dam);
}

.khung-tin-nhan__dang-tra-loi-trich {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  color: var(--mau-chu-phu);
}

.khung-tin-nhan__dang-tra-loi-huy {
  flex-shrink: 0;
  border: none;
  background: none;
  font-size: 18px;
  cursor: pointer;
  color: var(--mau-chu-phu);
}
```

Đổi rule `.khung-tin-nhan__bong` hiện có: xóa `align-self: flex-start;`
(chuyển việc căn trái/phải sang `.khung-tin-nhan__hang`/`--minh` mới) —
nếu rule cũ còn `align-self: flex-end;` trên `.khung-tin-nhan__bong--minh`,
xóa luôn dòng đó vì `justify-content: flex-end` trên `.khung-tin-nhan__hang--minh`
đã đảm nhiệm việc căn phải.

Áp class `khung-tin-nhan__hang--mo` khi `tinDangMoId === tn.id` (mobile:
bấm hiện đồng thời giờ + icon) — sửa dòng `className` của
`.khung-tin-nhan__hang` ở Step 8 thành:

```tsx
<div key={tn.id} className={`khung-tin-nhan__hang${laCuaMinh ? ' khung-tin-nhan__hang--minh' : ''}${tinDangMoId === tn.id ? ' khung-tin-nhan__hang--mo' : ''}`}>
```

- [ ] **Step 11: Chạy test để thấy PASS**

Run: `cd frontend && npm test -- --run KhungTinNhan`
Expected: PASS toàn bộ (test cũ + 5 test mới ở Step 4).

- [ ] **Step 12: `tsc --noEmit`**

Run: `cd frontend && npx tsc --noEmit`
Expected: có thể còn lỗi ở `TrangChat.tsx`/`TrangNhom.tsx` (chữ ký
`onGuiVanBan`/`onGuiTep` đổi) — đây LÀ phạm vi của Task 4, không sửa ở
đây. Xác nhận KHÔNG còn lỗi nào bên trong chính
`KhungTinNhan.tsx`/`KhungTinNhan.test.tsx`/`BieuTuong.tsx`.

- [ ] **Step 13: Commit**

```bash
git add frontend/src/KieuDuLieu.ts frontend/src/ThanhPhan/BieuTuong.tsx frontend/src/ThanhPhan/KhungTinNhan.tsx frontend/src/ThanhPhan/KhungTinNhan.css frontend/src/ThanhPhan/KhungTinNhan.test.tsx
git commit -m "feat(frontend): UI tra loi tin nhan + card File co nut tai rieng (GD6a)"
```

---

## Task 4: Frontend — nối dây `TrangChat.tsx`/`TrangNhom.tsx`

**Files:**
- Modify: `frontend/src/Trang/TrangChat.tsx`
- Modify: `frontend/src/Trang/TrangNhom.tsx`
- Test: `frontend/src/Trang/TrangChat.test.tsx`
- Test: `frontend/src/Trang/TrangNhom.test.tsx`

**Interfaces:**
- Consumes: `PropsKhungTinNhan.onGuiVanBan/onGuiTep/layTenNguoiGui` (Task 3).
- Produces: không có task nào phụ thuộc (task cuối của GĐ6a).

- [ ] **Step 1: Cập nhật `guiTinNhanVanBan`/`guiTep` trong `TrangChat.tsx`**

Đổi chữ ký + lời gọi `invoke`:

```typescript
  function guiTinNhanVanBan(noiDungGui: string, traLoiId: string | null) {
    if (!ketNoi || !nguoiDangChon) return;
    const idTam = `tam-${Date.now()}`;
    const tinNhanTam: TinNhanHienThi = {
      id: idTam, nguoiGuiId: idHienTai, nguoiNhanId: nguoiDangChon.id, nhomId: null,
      loaiTinNhan: 'Text', noiDungTinNhan: noiDungGui, duongDanFile: null, tenFileGoc: null,
      kichThuocFile: null, loaiFile: null, daDoc: false, daNhan: false,
      thoiGianTao: new Date().toISOString(), dangGui: true, traLoi: null,
    };
    setTinNhanTheoNguoiDung((truoc) => ({ ...truoc, [nguoiDangChon.id]: [...(truoc[nguoiDangChon.id] ?? []), tinNhanTam] }));

    ketNoi
      .invoke<TinNhanHienThi>('GuiTinNhan', nguoiDangChon.id, null, 'Text', noiDungGui, null, null, null, null, traLoiId)
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

  function guiTep(tep: File, traLoiId: string | null) {
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
          daTaiLen.duongDanFile, daTaiLen.tenFileGoc, daTaiLen.kichThuocFile, daTaiLen.loaiFile, traLoiId,
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
```

- [ ] **Step 2: Truyền prop `layTenNguoiGui` cho `<KhungTinNhan>` trong `TrangChat.tsx`**

Trong JSX render `<KhungTinNhan ... />`, thêm dòng:

```tsx
          layTenNguoiGui={(id) => (id === idHienTai ? 'Bạn' : (nguoiDangChon?.tenHienThi ?? 'một người dùng'))}
```

- [ ] **Step 3: Cập nhật `guiTinNhanVanBan`/`guiTep` trong `TrangNhom.tsx`**

Đổi chữ ký + lời gọi `invoke`:

```typescript
  function guiTinNhanVanBan(noiDungGui: string, traLoiId: string | null) {
    if (!ketNoi || !nhomDangChon) return;
    ketNoi
      .invoke<TinNhanHienThi>('GuiTinNhan', null, nhomDangChon.id, 'Text', noiDungGui, null, null, null, null, traLoiId)
      .then((tinNhanDaGui) => {
        setTinNhanTheoNhom((truoc) => ({ ...truoc, [nhomDangChon.id]: [...(truoc[nhomDangChon.id] ?? []), tinNhanDaGui] }));
      })
      .catch((loiBat) => setLoi(loiBat instanceof Error ? loiBat.message : 'Gửi tin nhắn thất bại.'));
  }

  function guiTep(tep: File, traLoiId: string | null) {
    if (!ketNoi || !nhomDangChon || !token) return;

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
          'GuiTinNhan', null, nhomDangChon.id, tep.type.startsWith('image/') ? 'Anh' : 'File', '',
          daTaiLen.duongDanFile, daTaiLen.tenFileGoc, daTaiLen.kichThuocFile, daTaiLen.loaiFile, traLoiId,
        ),
      )
      .then((tinNhanDaGui) => {
        setTinNhanTheoNhom((truoc) => ({ ...truoc, [nhomDangChon.id]: [...(truoc[nhomDangChon.id] ?? []), tinNhanDaGui] }));
      })
      .catch(() => setLoi('Gửi file thất bại.'))
      .finally(() => setDangTaiTep(false));
  }
```

- [ ] **Step 4: Truyền prop `layTenNguoiGui` cho `<KhungTinNhan>` trong `TrangNhom.tsx`**

```tsx
          layTenNguoiGui={(id) => (id === idHienTai ? 'Bạn' : (nhomDangChon?.thanhVien.find((tv) => tv.id === id)?.tenHienThi ?? 'một người dùng'))}
```

- [ ] **Step 5: Build TypeScript để liệt kê fixture `TinNhan` thiếu `traLoi`**

Run: `cd frontend && npx tsc --noEmit`
Expected: nếu còn lỗi thiếu field `traLoi` ở fixture trong
`TrangChat.test.tsx`/`TrangNhom.test.tsx`, liệt kê đủ vị trí. Nếu KHÔNG
báo lỗi (do mock đi qua đường untyped, đã ghi nhận là khả năng thật ở
GĐ5f), chuyển sang Step 6 và tự grep `noiDungTinNhan:` cạnh
`nguoiGuiId:`/`kichThuocFile:` để tìm thủ công mọi fixture `TinNhan`
literal trong 2 file test này.

- [ ] **Step 6: Thêm `traLoi: null` vào mọi fixture `TinNhan` đã tìm được**

Với mỗi object literal `TinNhan` tìm thấy ở Step 5 (nhận diện qua field
`thoiGianTao:` đứng cạnh `daDoc:`/`daNhan:`), thêm dòng
`traLoi: null,` ngay sau `thoiGianTao: '...',`.

- [ ] **Step 7: Chạy toàn bộ test frontend**

Run: `cd frontend && npm test -- --run`
Expected: PASS toàn bộ.

- [ ] **Step 8: `tsc --noEmit` toàn repo frontend**

Run: `cd frontend && npx tsc --noEmit`
Expected: 0 lỗi.

- [ ] **Step 9: Commit**

```bash
git add frontend/src/Trang/TrangChat.tsx frontend/src/Trang/TrangNhom.tsx frontend/src/Trang/TrangChat.test.tsx frontend/src/Trang/TrangNhom.test.tsx
git commit -m "feat(frontend): noi day traLoiId va layTenNguoiGui vao TrangChat/TrangNhom (GD6a)"
```

---

## Ghi chú cho reviewer / executor

- Task 1 độc lập, có thể review trước tiên.
- Task 2 phụ thuộc Task 1 (`TimTheoIdAsync`).
- Task 3 phụ thuộc Task 2 CHỈ về mặt kiểu dữ liệu JSON (`traLoi` field)
  — có thể code song song về mặt tĩnh, review độc lập vì dùng mock/test
  đơn vị, không cần backend chạy thật.
- Task 4 phụ thuộc Task 3 (chữ ký prop `onGuiVanBan`/`onGuiTep`/`layTenNguoiGui`).
- Không có 2 task nào cùng sửa 1 file — Task 2 chỉ backend, Task 3 chỉ
  `KhungTinNhan.*`/`BieuTuong.tsx`/`KieuDuLieu.ts`, Task 4 chỉ
  `TrangChat.*`/`TrangNhom.*`.
