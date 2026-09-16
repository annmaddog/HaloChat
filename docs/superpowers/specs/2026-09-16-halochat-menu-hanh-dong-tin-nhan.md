# HaloChat — Menu hành động tin nhắn: Lưu / Ghim / Thu hồi / Xóa (GĐ6b)

## 1. Bối cảnh & mục tiêu

Nối tiếp GĐ6a (icon "Trả lời" nổi khi hover/bấm vào tin nhắn), GĐ6b thêm
icon "..." đặt cạnh icon Trả lời, mở menu 4 hành động trên tin nhắn:
Lưu về thiết bị, Ghim/Bỏ ghim, Thu hồi (chỉ người gửi), Xóa (chỉ ẩn ở
phía người bấm).

**Phụ thuộc GĐ6a:** cần `KhungTinNhan.tsx` đã có wrapper
`.khung-tin-nhan__hang` + `.khung-tin-nhan__icon-noi` và state
`tinDangMoId`/hover-CSS từ GĐ6a. Thực thi GĐ6b SAU khi GĐ6a đã xong.

## 2. Phạm vi

**Trong phạm vi:**
- Icon "..." nổi cạnh icon Trả lời (cùng cơ chế hover/bấm của GĐ6a), mở
  dropdown menu ngay dưới.
- **Lưu về thiết bị**: chỉ hiện với tin Ảnh/File, kích hoạt tải file
  (không có backend mới — tái dùng URL file đã có).
- **Ghim/Bỏ ghim**: bất kỳ ai trong hội thoại 1-1 hoặc thành viên nhóm
  đều ghim/bỏ ghim được (đã chốt — không giới hạn quyền admin). Lưu
  ngay trên `TinNhan` (`DaGhim`, `ThoiGianGhim`), broadcast SignalR cho
  người/nhóm còn lại. Danh sách tin đã ghim hiện ở banner đầu khung chat
  (lấy qua endpoint riêng, không phụ thuộc phân trang lịch sử).
- **Thu hồi tin nhắn**: chỉ người gửi, không giới hạn thời gian (đã
  chốt). Nội dung/file bị thay hẳn bằng placeholder ở tầng server (server
  không trả nội dung gốc cho AI kể cả người gửi sau khi thu hồi),
  broadcast realtime cho phía kia.
- **Xóa (ẩn cục bộ)**: lưu bảng `{NguoiDungId, TinNhanId}` — server lọc
  tin đã ẩn ra khỏi mọi kết quả lịch sử/tin ghim trả về cho đúng người
  đó. Không broadcast.
- Endpoint `GET .../ghim` lọc luôn tin mà người gọi đã ẩn cục bộ (nhất
  quán: đã ẩn thì không hiện ở đâu nữa với người đó).

**Ngoài phạm vi:**
- Giới hạn thời gian thu hồi — không giới hạn (đã chốt).
- Quyền ghim riêng cho admin nhóm — không phân biệt quyền (đã chốt).
- Thông báo "X đã thu hồi 1 tin nhắn" dạng system message trong lịch sử
  — KHÔNG làm; tin nhắn tại chỗ chỉ đổi nội dung thành placeholder, không
  chèn thêm 1 tin nhắn hệ thống riêng.
- Sửa/chỉnh sửa nội dung tin nhắn (edit) — không nằm trong yêu cầu.
- Undo/khôi phục tin đã xóa cục bộ hoặc đã thu hồi — không có, cả 2 đều
  là hành động một chiều.

## 3. Backend

### 3.1. Model

**`Models/TinNhan.cs`** — thêm field (cuối class):

```csharp
    // [GĐ6b] Thu hồi: chỉ người gửi, không giới hạn thời gian. Khi true,
    // tầng DTO (AnhXaDto) LUÔN trả nội dung/file bằng placeholder — xem
    // §3.3, không phụ thuộc giá trị gốc còn lưu trong Mongo hay không.
    public bool DaThuHoi { get; set; } = false;

    // [GĐ6b] Ghim: ai trong hội thoại/nhóm cũng ghim/bỏ ghim được.
    public bool DaGhim { get; set; } = false;
    public DateTime? ThoiGianGhim { get; set; }
```

**`Models/TinNhanAn.cs`** (mới) — collection riêng ghi nhận tin nhắn đã
bị 1 người dùng ẩn cục bộ (không ảnh hưởng người khác):

```csharp
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HaloChat.Api.Models;

public class TinNhanAn
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonRepresentation(BsonType.ObjectId)]
    public string NguoiDungId { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.ObjectId)]
    public string TinNhanId { get; set; } = string.Empty;

    public DateTime ThoiGianAn { get; set; } = DateTime.UtcNow;
}
```

### 3.2. Exceptions — `Services/NgoaiLeTinNhan.cs` (mới)

```csharp
namespace HaloChat.Api.Services;

public class TinNhanKhongTonTaiException : Exception
{
    public TinNhanKhongTonTaiException() : base("Tin nhắn không tồn tại.")
    {
    }
}

public class KhongPhaiNguoiGuiException : Exception
{
    public KhongPhaiNguoiGuiException() : base("Bạn không phải người gửi tin nhắn này.")
    {
    }
}

public class KhongCoQuyenTrenTinNhanException : Exception
{
    public KhongCoQuyenTrenTinNhanException() : base("Bạn không có quyền thao tác trên tin nhắn này.")
    {
    }
}
```

### 3.3. Repository

**`Repositories/ITinNhanRepository.cs`** — thêm vào cuối interface:

```csharp
    /// <summary>[GĐ6b] Đánh dấu tin nhắn đã thu hồi.</summary>
    Task DanhDauThuHoiAsync(string id);

    /// <summary>[GĐ6b] Đặt/bỏ trạng thái ghim. thoiGianGhim = null khi bỏ ghim.</summary>
    Task DatGhimAsync(string id, bool daGhim, DateTime? thoiGianGhim);

    /// <summary>[GĐ6b] Toàn bộ tin đã ghim giữa 2 người dùng (2 chiều), mới ghim nhất trước.</summary>
    Task<List<TinNhan>> LayTinDaGhimTheoNguoiDungAsync(string nguoiA, string nguoiB);

    /// <summary>[GĐ6b] Toàn bộ tin đã ghim của 1 nhóm, mới ghim nhất trước.</summary>
    Task<List<TinNhan>> LayTinDaGhimTheoNhomAsync(string nhomId);
```

`TinNhanRepository.cs` implement tương ứng — `DatGhimAsync` set cả
`DaGhim`/`ThoiGianGhim`; 2 hàm `LayTinDaGhim...` filter `DaGhim == true`
và sort theo `ThoiGianGhim` giảm dần, theo đúng convention `Builders<T>.Filter`/`.Sort` đã dùng ở các method khác trong cùng file (mở file để soi
đúng cú pháp `IMongoCollection` hiện có trước khi viết).

**`Repositories/ITinNhanAnRepository.cs`** (mới) + implementation
`TinNhanAnRepository.cs` (theo đúng pattern các repo khác — constructor
nhận `IMongoDatabase`, collection tên `"TinNhanAn"`):

```csharp
namespace HaloChat.Api.Repositories;

public interface ITinNhanAnRepository
{
    Task AnAsync(string nguoiDungId, string tinNhanId);

    /// <summary>Trả về tập id trong tinNhanIds mà nguoiDungId đã ẩn.</summary>
    Task<HashSet<string>> LayDanhSachIdDaAnAsync(string nguoiDungId, IEnumerable<string> tinNhanIds);
}
```

`AnAsync` dùng `ReplaceOneAsync` với `IsUpsert = true` trên filter
`(NguoiDungId, TinNhanId)` để bấm Xóa nhiều lần không tạo bản ghi trùng
(không cần index Mongo riêng, upsert đã đủ idempotent ở tầng ứng dụng).

### 3.4. DI — `Program.cs`

Đăng ký thêm `builder.Services.AddScoped<ITinNhanAnRepository, TinNhanAnRepository>();`
cạnh các đăng ký repository khác.

### 3.5. Service — `IDichVuTinNhan.cs` / `DichVuTinNhan.cs`

Interface thêm:

```csharp
    Task<TinNhanDto> ThuHoiAsync(string idHienTai, string tinNhanId);
    Task<TinNhanDto> GhimAsync(string idHienTai, string tinNhanId);
    Task<TinNhanDto> BoGhimAsync(string idHienTai, string tinNhanId);
    Task AnAsync(string idHienTai, string tinNhanId);
    Task<List<TinNhanDto>> LayTinDaGhimTheoNguoiDungAsync(string idHienTai, string doiTacId);
    Task<List<TinNhanDto>> LayTinDaGhimTheoNhomAsync(string idHienTai, string nhomId);
```

Constructor `DichVuTinNhan` thêm tham số `ITinNhanAnRepository khoTinNhanAn`
(field `_khoTinNhanAn`).

Implementation:

```csharp
    private async Task KiemTraQuyenTrenTinNhanAsync(string idHienTai, TinNhan tinNhan)
    {
        if (tinNhan.NhomId is not null)
        {
            var nhom = await _khoNhom.TimTheoIdAsync(tinNhan.NhomId) ?? throw new NhomKhongTonTaiException();
            if (!nhom.ThanhVienIds.Contains(idHienTai))
            {
                throw new KhongPhaiThanhVienNhomException();
            }
        }
        else if (tinNhan.NguoiGuiId != idHienTai && tinNhan.NguoiNhanId != idHienTai)
        {
            throw new KhongCoQuyenTrenTinNhanException();
        }
    }

    public async Task<TinNhanDto> ThuHoiAsync(string idHienTai, string tinNhanId)
    {
        var tinNhan = await _khoTinNhan.TimTheoIdAsync(tinNhanId) ?? throw new TinNhanKhongTonTaiException();
        if (tinNhan.NguoiGuiId != idHienTai)
        {
            throw new KhongPhaiNguoiGuiException();
        }

        await _khoTinNhan.DanhDauThuHoiAsync(tinNhanId);
        tinNhan.DaThuHoi = true;
        return AnhXaDto(tinNhan);
    }

    public async Task<TinNhanDto> GhimAsync(string idHienTai, string tinNhanId)
    {
        var tinNhan = await _khoTinNhan.TimTheoIdAsync(tinNhanId) ?? throw new TinNhanKhongTonTaiException();
        await KiemTraQuyenTrenTinNhanAsync(idHienTai, tinNhan);

        var thoiGian = DateTime.UtcNow;
        await _khoTinNhan.DatGhimAsync(tinNhanId, true, thoiGian);
        tinNhan.DaGhim = true;
        tinNhan.ThoiGianGhim = thoiGian;
        return AnhXaDto(tinNhan);
    }

    public async Task<TinNhanDto> BoGhimAsync(string idHienTai, string tinNhanId)
    {
        var tinNhan = await _khoTinNhan.TimTheoIdAsync(tinNhanId) ?? throw new TinNhanKhongTonTaiException();
        await KiemTraQuyenTrenTinNhanAsync(idHienTai, tinNhan);

        await _khoTinNhan.DatGhimAsync(tinNhanId, false, null);
        tinNhan.DaGhim = false;
        tinNhan.ThoiGianGhim = null;
        return AnhXaDto(tinNhan);
    }

    public async Task AnAsync(string idHienTai, string tinNhanId)
    {
        var tinNhan = await _khoTinNhan.TimTheoIdAsync(tinNhanId) ?? throw new TinNhanKhongTonTaiException();
        await KiemTraQuyenTrenTinNhanAsync(idHienTai, tinNhan);
        await _khoTinNhanAn.AnAsync(idHienTai, tinNhanId);
    }

    public async Task<List<TinNhanDto>> LayTinDaGhimTheoNguoiDungAsync(string idHienTai, string doiTacId)
    {
        var ghim = await _khoTinNhan.LayTinDaGhimTheoNguoiDungAsync(idHienTai, doiTacId);
        var idDaAn = await _khoTinNhanAn.LayDanhSachIdDaAnAsync(idHienTai, ghim.Select(t => t.Id));
        return ghim.Where(t => !idDaAn.Contains(t.Id)).Select(AnhXaDto).ToList();
    }

    public async Task<List<TinNhanDto>> LayTinDaGhimTheoNhomAsync(string idHienTai, string nhomId)
    {
        var nhom = await _khoNhom.TimTheoIdAsync(nhomId) ?? throw new NhomKhongTonTaiException();
        if (!nhom.ThanhVienIds.Contains(idHienTai))
        {
            throw new KhongPhaiThanhVienNhomException();
        }

        var ghim = await _khoTinNhan.LayTinDaGhimTheoNhomAsync(nhomId);
        var idDaAn = await _khoTinNhanAn.LayDanhSachIdDaAnAsync(idHienTai, ghim.Select(t => t.Id));
        return ghim.Where(t => !idDaAn.Contains(t.Id)).Select(AnhXaDto).ToList();
    }
```

**`AnhXaDto`** — thêm override placeholder khi `DaThuHoi`, đặt NGAY ĐẦU
hàm (trước khi dựng `TraLoiThongTinDto` của GĐ6a — không đổi cách xử lý
`TraLoi`, chỉ đổi phần nội dung/file):

```csharp
    private static TinNhanDto AnhXaDto(TinNhan t)
    {
        var traLoi = t.TraLoi is null ? null : new TraLoiThongTinDto(t.TraLoi.Id, t.TraLoi.TenNguoiGui, t.TraLoi.NoiDungTomTat, t.TraLoi.LoaiTinNhan.ToString());

        if (t.DaThuHoi)
        {
            return new(
                t.Id, t.NguoiGuiId, t.NguoiNhanId, t.NhomId, t.LoaiTinNhan.ToString(),
                "Tin nhắn đã được thu hồi.", null, null, null, null,
                t.DaDoc, t.DaNhan, t.ThoiGianTao, traLoi, t.DaThuHoi, t.DaGhim, t.ThoiGianGhim);
        }

        return new(
            t.Id, t.NguoiGuiId, t.NguoiNhanId, t.NhomId, t.LoaiTinNhan.ToString(), t.NoiDungTinNhan,
            t.DuongDanFile, t.TenFileGoc, t.KichThuocFile, t.LoaiFile, t.DaDoc, t.DaNhan, t.ThoiGianTao,
            traLoi, t.DaThuHoi, t.DaGhim, t.ThoiGianGhim);
    }
```

`LayLichSuAsync`/`LayLichSuNhomAsync` — thêm bước lọc tin đã ẩn cục bộ
TRƯỚC khi map DTO:

```csharp
    public async Task<List<TinNhanDto>> LayLichSuAsync(string nguoiHienTaiId, string nguoiKiaId, string? truocId, int soLuong)
    {
        var lichSu = await _khoTinNhan.LayLichSuTheoNguoiDungAsync(nguoiHienTaiId, nguoiKiaId, truocId, soLuong);
        var idDaAn = await _khoTinNhanAn.LayDanhSachIdDaAnAsync(nguoiHienTaiId, lichSu.Select(t => t.Id));
        return lichSu.Where(t => !idDaAn.Contains(t.Id)).Select(AnhXaDto).ToList();
    }
```

(tương tự cho `LayLichSuNhomAsync`). Lưu ý đã biết: nếu 1 trang có tin bị
ẩn, trang trả về sẽ CÓ THỂ ít hơn `soLuong` yêu cầu — chấp nhận được, đây
là đánh đổi hợp lý, không cần bù thêm truy vấn.

### 3.6. DTO — `Dto/TinNhanDto.cs`

Thêm 3 tham số cuối (sau `TraLoi` của GĐ6a):

```csharp
public record TinNhanDto(
    string Id, string NguoiGuiId, string? NguoiNhanId, string? NhomId, string LoaiTinNhan,
    string NoiDungTinNhan, string? DuongDanFile, string? TenFileGoc, long? KichThuocFile, string? LoaiFile,
    bool DaDoc, bool DaNhan, DateTime ThoiGianTao, TraLoiThongTinDto? TraLoi,
    bool DaThuHoi, bool DaGhim, DateTime? ThoiGianGhim);
```

### 3.7. Hub — `ChatHub.cs`

Thêm 3 method + 1 helper broadcast dùng chung:

```csharp
    public async Task<TinNhanDto> ThuHoiTinNhan(string tinNhanId)
    {
        try
        {
            var tinNhan = await _dichVuTinNhan.ThuHoiAsync(NguoiDungHienTaiId, tinNhanId);
            await GuiBroadcastCapNhatAsync(tinNhan, "TinNhanDaThuHoi");
            return tinNhan;
        }
        catch (TinNhanKhongTonTaiException loi) { throw new HubException(loi.Message); }
        catch (KhongPhaiNguoiGuiException loi) { throw new HubException(loi.Message); }
    }

    public async Task<TinNhanDto> GhimTinNhan(string tinNhanId)
    {
        try
        {
            var tinNhan = await _dichVuTinNhan.GhimAsync(NguoiDungHienTaiId, tinNhanId);
            await GuiBroadcastCapNhatAsync(tinNhan, "TinNhanDaGhim");
            return tinNhan;
        }
        catch (TinNhanKhongTonTaiException loi) { throw new HubException(loi.Message); }
        catch (KhongCoQuyenTrenTinNhanException loi) { throw new HubException(loi.Message); }
        catch (NhomKhongTonTaiException loi) { throw new HubException(loi.Message); }
        catch (KhongPhaiThanhVienNhomException loi) { throw new HubException(loi.Message); }
    }

    public async Task<TinNhanDto> BoGhimTinNhan(string tinNhanId)
    {
        try
        {
            var tinNhan = await _dichVuTinNhan.BoGhimAsync(NguoiDungHienTaiId, tinNhanId);
            await GuiBroadcastCapNhatAsync(tinNhan, "TinNhanBoGhim");
            return tinNhan;
        }
        catch (TinNhanKhongTonTaiException loi) { throw new HubException(loi.Message); }
        catch (KhongCoQuyenTrenTinNhanException loi) { throw new HubException(loi.Message); }
        catch (NhomKhongTonTaiException loi) { throw new HubException(loi.Message); }
        catch (KhongPhaiThanhVienNhomException loi) { throw new HubException(loi.Message); }
    }

    private async Task GuiBroadcastCapNhatAsync(TinNhanDto tinNhan, string tenSuKien)
    {
        if (tinNhan.NhomId is not null)
        {
            await Clients.OthersInGroup("nhom-" + tinNhan.NhomId).SendAsync(tenSuKien, tinNhan);
        }
        else
        {
            var idKia = tinNhan.NguoiGuiId == NguoiDungHienTaiId ? tinNhan.NguoiNhanId : tinNhan.NguoiGuiId;
            await Clients.User(idKia!).SendAsync(tenSuKien, tinNhan);
        }
    }
```

### 3.8. Controller — `TinNhanController.cs`

```csharp
    [HttpPost("{id}/an")]
    public async Task<IActionResult> An(string id)
    {
        if (IdHienTai is null)
        {
            return Unauthorized();
        }

        try
        {
            await _dichVuTinNhan.AnAsync(IdHienTai, id);
            return Ok(new { thongBao = "Đã ẩn tin nhắn." });
        }
        catch (TinNhanKhongTonTaiException loi) { return NotFound(new { thongBao = loi.Message }); }
        catch (KhongCoQuyenTrenTinNhanException loi) { return StatusCode(403, new { thongBao = loi.Message }); }
        catch (NhomKhongTonTaiException loi) { return NotFound(new { thongBao = loi.Message }); }
        catch (KhongPhaiThanhVienNhomException loi) { return StatusCode(403, new { thongBao = loi.Message }); }
    }

    [HttpGet("nguoi-dung/{id}/ghim")]
    public async Task<IActionResult> LayTinDaGhimTheoNguoiDung(string id)
    {
        if (IdHienTai is null)
        {
            return Unauthorized();
        }

        return Ok(await _dichVuTinNhan.LayTinDaGhimTheoNguoiDungAsync(IdHienTai, id));
    }

    [HttpGet("nhom/{id}/ghim")]
    public async Task<IActionResult> LayTinDaGhimTheoNhom(string id)
    {
        if (IdHienTai is null)
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await _dichVuTinNhan.LayTinDaGhimTheoNhomAsync(IdHienTai, id));
        }
        catch (NhomKhongTonTaiException loi) { return NotFound(new { thongBao = loi.Message }); }
        catch (KhongPhaiThanhVienNhomException loi) { return StatusCode(403, new { thongBao = loi.Message }); }
    }
```

### 3.9. Test impact backend

- `dotnet build` sẽ báo lỗi ở mọi nơi dựng `TinNhanDto` positional (nếu
  còn sót đâu đó ngoài `AnhXaDto`) và mọi implementer của
  `IDichVuTinNhan`/`ITinNhanRepository` (fake test) — cập nhật fake
  trong `backend/HaloChat.Api.Tests/Fakes/` (grep `IDichVuTinNhan\|ITinNhanRepository` để tìm hết).
- Test service mới (`DichVuTinNhanTests.cs`):
  - `ThuHoiAsync_LaNguoiGui_DatDaThuHoiVaAnNoiDung`
  - `ThuHoiAsync_KhongPhaiNguoiGui_NemKhongPhaiNguoiGui`
  - `GhimAsync_ThanhVienHopLe_DatDaGhim`
  - `GhimAsync_KhongThuocHoiThoai_NemKhongCoQuyen`
  - `BoGhimAsync_DatLaiDaGhimFalse`
  - `AnAsync_SauKhiAn_KhongConXuatHienTrongLichSuNguoiDoAn`
  - `AnAsync_KhongAnhHuongLichSuCuaNguoiKhac`
  - `LayTinDaGhimTheoNguoiDungAsync_LocDungTinDaAn`
- Test hub (`ChatHubTests.cs` nếu có, hoặc tương đương) hoặc test
  controller cho endpoint `an`/`ghim` — theo pattern tích hợp đã có ở
  `TinNhanControllerTests.cs`.

## 4. Frontend

### 4.1. Kiểu dữ liệu — `KieuDuLieu.ts`

```typescript
export interface TinNhan {
  // ... field hiện có (bao gồm traLoi của GĐ6a) ...
  daThuHoi: boolean;
  daGhim: boolean;
  thoiGianGhim: string | null;
}
```

### 4.2. `DichVuApi.ts` — hàm mới

```typescript
export async function AnTinNhan(token: string, id: string): Promise<void> {
  await goiApi<{ thongBao: string }>(`/tinnhan/${id}/an`, {
    method: 'POST',
    headers: { Authorization: `Bearer ${token}` },
  });
}

export async function LayTinDaGhimTheoNguoiDung(token: string, doiTacId: string): Promise<TinNhan[]> {
  return goiApi<TinNhan[]>(`/tinnhan/nguoi-dung/${doiTacId}/ghim`, {
    headers: { Authorization: `Bearer ${token}` },
  });
}

export async function LayTinDaGhimTheoNhom(token: string, nhomId: string): Promise<TinNhan[]> {
  return goiApi<TinNhan[]>(`/tinnhan/nhom/${nhomId}/ghim`, {
    headers: { Authorization: `Bearer ${token}` },
  });
}
```

### 4.3. `KhungTinNhan.tsx`

- Thêm icon "..." vào `.khung-tin-nhan__icon-noi` (đã có từ GĐ6a), CẠNH
  icon Trả lời:
  ```tsx
  <button
    type="button"
    className="khung-tin-nhan__nut-them"
    onClick={(su) => { su.stopPropagation(); setMenuMoChoTinNhanId((truoc) => (truoc === tn.id ? null : tn.id)); }}
    aria-label="Thêm tùy chọn"
  >
    <BieuTuongBaCham />
  </button>
  ```
- State mới: `const [menuMoChoTinNhanId, setMenuMoChoTinNhanId] = useState<string | null>(null);`
- Props mới: `onThuHoi`, `onGhim`, `onBoGhim`, `onAn` (mỗi hàm nhận
  `(id: string) => void`), `danhSachTinNhanGhim: TinNhan[]`.
- Dropdown menu render khi `menuMoChoTinNhanId === tn.id`, nội dung theo
  đúng điều kiện đã chốt:
  ```tsx
  {menuMoChoTinNhanId === tn.id && (
    <div className="khung-tin-nhan__menu" onClick={(su) => su.stopPropagation()}>
      {!tn.daThuHoi && tn.loaiTinNhan !== 'Text' && (
        <a href={`${DIA_CHI_GOC}${tn.duongDanFile}`} download target="_blank" rel="noreferrer" onClick={() => setMenuMoChoTinNhanId(null)}>
          Lưu về thiết bị
        </a>
      )}
      {!tn.daThuHoi && !tn.daGhim && (
        <button onClick={() => { onGhim(tn.id); setMenuMoChoTinNhanId(null); }}>Ghim</button>
      )}
      {!tn.daThuHoi && tn.daGhim && (
        <button onClick={() => { onBoGhim(tn.id); setMenuMoChoTinNhanId(null); }}>Bỏ ghim</button>
      )}
      {laCuaMinh && !tn.daThuHoi && (
        <button onClick={() => { onThuHoi(tn.id); setMenuMoChoTinNhanId(null); }}>Thu hồi tin nhắn</button>
      )}
      <button className="khung-tin-nhan__menu-nguy-hiem" onClick={() => { onAn(tn.id); setMenuMoChoTinNhanId(null); }}>Xóa</button>
    </div>
  )}
  ```
- Bong bóng: khi `tn.daThuHoi`, hiện placeholder in nghiêng thay cho nội
  dung/file/ảnh:
  ```tsx
  {tn.daThuHoi ? (
    <span className="khung-tin-nhan__da-thu-hoi">Tin nhắn đã được thu hồi.</span>
  ) : (
    <>{/* nhánh Anh/File/Text hiện có, giữ nguyên */}</>
  )}
  ```
- Banner tin ghim: render ngay dưới `<header className="khung-tin-nhan__tieu-de">`,
  ẨN nếu `danhSachTinNhanGhim.length === 0`:
  ```tsx
  {danhSachTinNhanGhim.length > 0 && (
    <div className="khung-tin-nhan__banner-ghim">
      {danhSachTinNhanGhim.map((tn) => (
        <div key={tn.id} className="khung-tin-nhan__dong-ghim">
          <BieuTuongGhim />
          <span className="khung-tin-nhan__dong-ghim-noi-dung">
            {layTenNguoiGui ? layTenNguoiGui(tn.nguoiGuiId) : 'một người dùng'}:{' '}
            {tn.loaiTinNhan === 'Text' ? tn.noiDungTinNhan : tn.loaiTinNhan === 'Anh' ? '[Ảnh]' : `[File] ${tn.tenFileGoc}`}
          </span>
          <button onClick={() => onBoGhim(tn.id)} aria-label="Bỏ ghim">×</button>
        </div>
      ))}
    </div>
  )}
  ```
  (`layTenNguoiGui` là prop đã thêm ở GĐ6a — tái dùng, không thêm prop
  mới cho việc này.)

### 4.4. `BieuTuong.tsx` — icon mới

```tsx
export function BieuTuongBaCham() {
  return (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor" stroke="none">
      <circle cx="5" cy="12" r="2" />
      <circle cx="12" cy="12" r="2" />
      <circle cx="19" cy="12" r="2" />
    </svg>
  );
}
```

(`BieuTuongGhim` dùng trong banner đã tồn tại sẵn — dùng lại nguyên
trạng, KHÔNG tạo icon ghim mới.)

### 4.5. `KhungTinNhan.css` — quy tắc mới

```css
.khung-tin-nhan__nut-them {
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

.khung-tin-nhan__menu {
  position: absolute;
  top: 16px;
  right: 8px;
  z-index: 5;
  min-width: 160px;
  background: var(--mau-nen-the);
  border: 1px solid var(--mau-vien);
  border-radius: 10px;
  box-shadow: var(--bong-the);
  padding: 6px;
  display: flex;
  flex-direction: column;
}

.khung-tin-nhan__menu button,
.khung-tin-nhan__menu a {
  border: none;
  background: none;
  text-align: left;
  padding: 8px 10px;
  border-radius: 8px;
  font-family: inherit;
  font-size: 13px;
  color: var(--mau-chu-dam);
  cursor: pointer;
  text-decoration: none;
}

.khung-tin-nhan__menu button:hover,
.khung-tin-nhan__menu a:hover {
  background: var(--mau-nen-tren);
}

.khung-tin-nhan__menu-nguy-hiem {
  color: var(--mau-loi) !important;
}

.khung-tin-nhan__da-thu-hoi {
  font-style: italic;
  opacity: 0.75;
}

.khung-tin-nhan__banner-ghim {
  padding: 6px 24px;
  border-bottom: 1px solid var(--mau-vien);
  background: var(--mau-nen-tren);
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.khung-tin-nhan__dong-ghim {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 12px;
  color: var(--mau-chu-phu);
}

.khung-tin-nhan__dong-ghim-noi-dung {
  flex: 1;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.khung-tin-nhan__dong-ghim button {
  flex-shrink: 0;
  border: none;
  background: none;
  cursor: pointer;
  color: var(--mau-chu-phu);
}
```

### 4.6. `TrangChat.tsx` / `TrangNhom.tsx`

- State mới: `const [tinNhanGhimTheoDoiTac, setTinNhanGhimTheoDoiTac] = useState<Record<string, TinNhan[]>>({});`
  (tương tự `tinNhanTheoNguoiDung`/`tinNhanTheoNhom` đã có — key theo id
  đối tác/nhóm).
- Effect tải tin ghim khi mở 1 cuộc trò chuyện/nhóm (đặt cạnh effect tải
  lịch sử hiện có, gọi `LayTinDaGhimTheoNguoiDung`/`LayTinDaGhimTheoNhom`
  MỘT LẦN mỗi khi đổi `nguoiDangChon`/`nhomDangChonId`, không cần lazy-set
  giống `daTaiLichSuIds` — tin ghim danh sách ngắn, tải lại mỗi lần mở
  lại tab không tốn kém).
- Handler mới:
  ```typescript
  function thuHoiTinNhan(id: string) {
    if (!ketNoi) return;
    ketNoi.invoke<TinNhanHienThi>('ThuHoiTinNhan', id)
      .then((tinCapNhat) => capNhatTinNhanTrongState(tinCapNhat))
      .catch((loiBat) => setLoi(loiBat instanceof Error ? loiBat.message : 'Thu hồi tin nhắn thất bại.'));
  }

  function ghimTinNhan(id: string) {
    if (!ketNoi) return;
    ketNoi.invoke<TinNhanHienThi>('GhimTinNhan', id)
      .then((tinCapNhat) => { capNhatTinNhanTrongState(tinCapNhat); themVaoDanhSachGhim(tinCapNhat); })
      .catch((loiBat) => setLoi(loiBat instanceof Error ? loiBat.message : 'Ghim tin nhắn thất bại.'));
  }

  function boGhimTinNhan(id: string) {
    if (!ketNoi) return;
    ketNoi.invoke<TinNhanHienThi>('BoGhimTinNhan', id)
      .then((tinCapNhat) => { capNhatTinNhanTrongState(tinCapNhat); xoaKhoiDanhSachGhim(id); })
      .catch((loiBat) => setLoi(loiBat instanceof Error ? loiBat.message : 'Bỏ ghim thất bại.'));
  }

  function anTinNhanCucBo(id: string) {
    if (!token) return;
    AnTinNhan(token, id)
      .then(() => xoaKhoiDanhSachTinNhan(id))
      .catch(() => setLoi('Xóa tin nhắn thất bại.'));
  }
  ```
  (`capNhatTinNhanTrongState`, `themVaoDanhSachGhim`, `xoaKhoiDanhSachGhim`,
  `xoaKhoiDanhSachTinNhan` là 4 hàm helper nhỏ thao tác đúng state
  `tinNhanTheoNguoiDung`/`tinNhanGhimTheoDoiTac` tương ứng với
  `nguoiDangChon.id` hiện tại — với `TrangNhom.tsx` thay bằng
  `tinNhanTheoNhom`/`tinNhanGhimTheoNhom` và `nhomDangChon.id`. Viết cụ
  thể ở bước implementation, theo đúng khuôn `setTinNhanTheoNguoiDung((truoc) => ({...}))` đã dùng ở các chỗ khác trong file.)
- Đăng ký listener SignalR mới (cạnh `ketNoi.on('NhanTinNhan', ...)`):
  ```typescript
  ketNoi.on('TinNhanDaThuHoi', xuLyTinNhanCapNhat);
  ketNoi.on('TinNhanDaGhim', xuLyTinNhanGhim);
  ketNoi.on('TinNhanBoGhim', xuLyTinNhanBoGhim);
  ```
  và `off` tương ứng trong cleanup — 3 handler này cập nhật đúng state
  giống các hàm ở trên nhưng chạy khi PHÍA KIA thực hiện hành động (dữ
  liệu tới qua tham số sự kiện, không qua kết quả `invoke`).
- Truyền prop mới cho `<KhungTinNhan>`:
  ```tsx
  onThuHoi={thuHoiTinNhan}
  onGhim={ghimTinNhan}
  onBoGhim={boGhimTinNhan}
  onAn={anTinNhanCucBo}
  danhSachTinNhanGhim={nguoiDangChon ? (tinNhanGhimTheoDoiTac[nguoiDangChon.id] ?? []) : []}
  ```
  (`TrangNhom.tsx` tương tự, dùng `nhomDangChon`/`tinNhanGhimTheoNhom`.)

## 5. Kiểm thử

- Backend: các test liệt kê ở §3.9.
- Frontend `KhungTinNhan.test.tsx`:
  - `bam ... hien menu voi dung cac muc theo trang thai tin nhan`
  - `tin da thu hoi an placeholder thay vi noi dung goc`
  - `tin da thu hoi khong con muc Thu hoi/Ghim/Luu trong menu`
  - `bam Thu hoi goi onThuHoi dung id (chi hien khi la cua minh)`
  - `bam Ghim/Bo ghim goi dung ham tuong ung`
  - `bam Xoa goi onAn dung id`
  - `banner ghim hien dung danh sach tin da ghim, an khi rong`
- Frontend `TrangChat.test.tsx`/`TrangNhom.test.tsx`:
  - `thu hoi tin nhan qua SignalR invoke cap nhat dung tin trong danh sach`
  - `nhan su kien TinNhanDaThuHoi tu phia kia cap nhat placeholder realtime`
  - `an tin nhan cuc bo goi AnTinNhan va loai tin do khoi danh sach hien tai`
  - cập nhật fixture `TinNhan` thêm `daThuHoi: false, daGhim: false, thoiGianGhim: null` ở mọi nơi dựng object literal (dùng `tsc --noEmit` liệt kê vị trí).
