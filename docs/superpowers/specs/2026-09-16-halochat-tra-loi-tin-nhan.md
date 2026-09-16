# HaloChat — Trả lời tin nhắn + nút tải file luôn hiện (GĐ6a)

## 1. Bối cảnh & mục tiêu

Hiện tại tin nhắn dạng File chỉ là 1 link phẳng (📎 tên + size), không có
nút tải rõ ràng; và không có cách nào trả lời/trích dẫn 1 tin nhắn cụ
thể. Mục tiêu: thêm nút tải xuống hiện sẵn trên tin nhắn File, và thêm
luồng "trả lời tin nhắn" (bấm 1 icon nổi lên khi hover/bấm vào tin nhắn,
chuyển ô soạn tin sang chế độ trả lời, gửi kèm trích dẫn tin gốc).

Đây là bước nền cho GĐ6b (menu hành động tin nhắn) — icon "..." của GĐ6b
sẽ đặt CẠNH icon Trả lời được xây ở đây, nhưng **GĐ6a chưa render icon
"..."** (tránh 1 nút chưa có menu, chưa làm gì).

## 2. Phạm vi

**Trong phạm vi:**
- Card tin nhắn File đổi bố cục: icon tài liệu bên trái, tên+size xếp
  dọc, nút tròn tải xuống bên phải. Cả card và nút tròn đều tải file
  (endpoint đã tự set `Content-Disposition: attachment`, không cần đổi
  backend cho phần này).
- Hover (desktop, CSS `:hover`, không cần JS) hoặc bấm (mobile, dùng lại
  state `tinDangMoId` đã có) vào 1 tin nhắn hiện icon tròn nổi "Trả lời"
  ở góc bong bóng.
- Bấm icon Trả lời → ô soạn tin chuyển sang chế độ "đang trả lời": khối
  preview phía trên form nhập, ghi "↩ Trả lời {tên người gửi tin gốc}" +
  trích 1 dòng nội dung (rút gọn), có nút × hủy trả lời.
- Gửi tin (Text hoặc File/Ảnh) trong khi đang ở chế độ trả lời → tin mới
  gắn kèm `traLoiId` gửi lên hub `GuiTinNhan`.
- Backend validate: tin được trả lời phải tồn tại và thuộc ĐÚNG cuộc trò
  chuyện (cùng nhóm, hoặc cùng cặp người dùng) đang gửi tin mới.
- Backend lưu **snapshot** thông tin tin gốc (tên người gửi lúc đó, nội
  dung rút gọn, loại tin) ngay vào tin nhắn mới — không tham chiếu sống.
  Vì vậy trích dẫn không đổi kể cả nếu sau này (GĐ6b) tin gốc bị thu hồi.
- Tin nhắn đã gửi có trích dẫn hiển thị khối trích dẫn nhỏ phía trên nội
  dung trong bong bóng.

**Ngoài phạm vi:**
- Bấm vào khối trích dẫn để cuộn/nhảy tới tin gốc trong lịch sử — không
  làm (đã chốt khi brainstorm).
- Icon "..." và toàn bộ menu hành động (Lưu/Ghim/Thu hồi/Xóa) — thuộc
  GĐ6b, spec riêng.
- Nút tải xuống cho tin nhắn Ảnh (mockup chỉ yêu cầu cho File) — ảnh vẫn
  hiển thị inline như cũ, không thêm nút tải riêng ở GĐ6a.
- Giới hạn độ sâu trả lời (trả lời 1 tin đã là trả lời của tin khác) —
  không giới hạn, không xử lý đặc biệt: tin B trả lời tin A, tin C trả
  lời tin B thì C chỉ lưu snapshot của B (không đệ quy lên A).

## 3. Backend

### 3.1. Model

**`Models/TraLoiThongTin.cs`** (mới) — nested document, không có `Id`
Mongo riêng (nhúng trong `TinNhan`):

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

**`Models/TinNhan.cs`** — thêm field (cuối class, trước dấu `}`):

```csharp
    // [GĐ6a] Snapshot thông tin tin gốc tại thời điểm trả lời — KHÔNG
    // tham chiếu sống. Null nếu tin này không phải trả lời tin nào.
    public TraLoiThongTin? TraLoi { get; set; }
```

### 3.2. Repository — thêm `TimTheoIdAsync`

**`Repositories/ITinNhanRepository.cs`** — thêm vào cuối interface:

```csharp
    /// <summary>Lấy 1 tin nhắn theo id, null nếu không tồn tại. Dùng để validate trả lời (GĐ6a) và các hành động trên tin nhắn (GĐ6b).</summary>
    Task<TinNhan?> TimTheoIdAsync(string id);
```

**`Repositories/TinNhanRepository.cs`** — thêm method (cuối class):

```csharp
    public async Task<TinNhan?> TimTheoIdAsync(string id)
    {
        return await _collection.Find(t => t.Id == id).FirstOrDefaultAsync();
    }
```

(Tên collection/field theo đúng pattern hiện có trong file — mở file để
soi tên field Mongo chính xác trước khi viết, ví dụ `_collection` là tên
biến `IMongoCollection<TinNhan>` đã dùng ở các method khác trong cùng
class.)

### 3.3. DTO

**`Dto/TraLoiThongTinDto.cs`** (mới):

```csharp
namespace HaloChat.Api.Dto;

public record TraLoiThongTinDto(string Id, string TenNguoiGui, string NoiDungTomTat, string LoaiTinNhan);
```

**`Dto/TinNhanDto.cs`** — thêm tham số cuối (không xóa/sắp xếp lại tham
số cũ):

```csharp
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

### 3.4. Service — `DichVuTinNhan.cs`

`IDichVuTinNhan.GuiTinNhanAsync` thêm tham số cuối:

```csharp
    Task<TinNhanDto> GuiTinNhanAsync(
        string nguoiGuiId, string? nguoiNhanId, string? nhomId, string loaiTinNhan, string noiDungTinNhan,
        string? duongDanFile, string? tenFileGoc, long? kichThuocFile, string? loaiFile, string? traLoiId);
```

Trong `DichVuTinNhan.GuiTinNhanAsync`, sau khối xác định `nhomId`/
`nguoiNhanId` hiện có (biến `nhomId`/`nguoiNhanId` đã được chuẩn hóa ở
đầu hàm) và TRƯỚC dòng `await _khoTinNhan.ThemMoiAsync(tinNhan);`, thêm:

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

Lưu ý: đoạn trên đặt SAU khối `if (nhomId is not null) {...} else {...}`
đã có (khối đó set `tinNhan.NhomId`/`tinNhan.NguoiNhanId`), vì cần biến
`nguoiNhanId` cuối cùng đã resolve (không dùng biến gốc còn có thể null
khi validate trả lời cho tin nhóm). Đọc kỹ code hiện tại của
`GuiTinNhanAsync` trước khi chèn để đặt đúng vị trí — không chèn giữa
khối if/else.

`AnhXaDto` (private static method cuối file) — thêm tham số cuối khi
dựng `TinNhanDto`:

```csharp
    private static TinNhanDto AnhXaDto(TinNhan t) => new(
        t.Id, t.NguoiGuiId, t.NguoiNhanId, t.NhomId, t.LoaiTinNhan.ToString(), t.NoiDungTinNhan,
        t.DuongDanFile, t.TenFileGoc, t.KichThuocFile, t.LoaiFile, t.DaDoc, t.DaNhan, t.ThoiGianTao,
        t.TraLoi is null ? null : new TraLoiThongTinDto(t.TraLoi.Id, t.TraLoi.TenNguoiGui, t.TraLoi.NoiDungTomTat, t.TraLoi.LoaiTinNhan.ToString()));
```

### 3.5. Hub — `ChatHub.cs`

`GuiTinNhan` thêm tham số cuối `string? traLoiId`, truyền thẳng xuống
`_dichVuTinNhan.GuiTinNhanAsync(...)`:

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
            /* phần còn lại giữ nguyên */
```

### 3.6. Test impact backend (không placeholder — đây là checklist thực thi)

- `dotnet build` sau khi đổi `IDichVuTinNhan`/`DichVuTinNhan`/`ChatHub`
  sẽ báo lỗi CS7036 ở MỌI nơi gọi `GuiTinNhanAsync(...)` với 9 tham số
  (cả code thật lẫn test) — grep `GuiTinNhanAsync(` trong toàn bộ
  `backend/` để liệt kê đủ, thêm `null` (không trả lời) làm tham số cuối
  ở mọi lời gọi không cố ý test trả lời.
- Thêm test mới trong `DichVuTinNhanTests.cs`:
  - `GuiTinNhanAsync_CoTraLoiHopLe_LuuSnapshotDungThongTin` — gửi tin A,
    lấy `A.Id`, gửi tin B với `traLoiId = A.Id` trong CÙNG cuộc trò
    chuyện, assert `B.TraLoi` không null, `TenNguoiGui`/`NoiDungTomTat`
    khớp tin A.
  - `GuiTinNhanAsync_TraLoiTinKhongTonTai_NemTinNhanKhongHopLe` —
    `traLoiId` là 1 ObjectId hợp lệ nhưng không tồn tại trong kho giả
    lập → `TinNhanKhongHopLeException`.
  - `GuiTinNhanAsync_TraLoiTinThuocCuocTroChuyenKhac_NemTinNhanKhongHopLe`
    — gửi tin A giữa user X-Y, rồi thử gửi tin trả lời A nhưng giữa cặp
    user X-Z (khác cuộc trò chuyện) → `TinNhanKhongHopLeException`.
  - `GuiTinNhanAsync_NoiDungTextDaiHon80KyTu_RutGonConDauBaChamCuoi` —
    verify `NoiDungTomTat` bị cắt đúng 80 ký tự + `"…"`.
- Cập nhật `NguoiDungControllerTests.cs`/`TinNhanControllerTests.cs`/
  `KetBanControllerTests.cs` (bất kỳ nơi nào deserialize `TinNhanDto` và
  so sánh positional) — vì đây chỉ deserialize qua JSON (không dựng
  positional record), theo đúng pattern đã xác nhận ở GĐ5f, KHÔNG cần
  sửa test cũ nào, chỉ build là biết chắc.

## 4. Frontend

### 4.1. Kiểu dữ liệu

**`KieuDuLieu.ts`** — `TinNhan` thêm field cuối:

```typescript
export interface TinNhan {
  // ... các field hiện có giữ nguyên ...
  traLoi: { id: string; tenNguoiGui: string; noiDungTomTat: string; loaiTinNhan: LoaiTinNhan } | null;
}
```

### 4.2. `KhungTinNhan.tsx`

- Props thay đổi: `onGuiVanBan: (noiDung: string, traLoiId: string | null) => void;`
  và `onGuiTep: (tep: File, traLoiId: string | null) => void;` (thêm
  tham số cuối, không đổi cách gọi từ bên trong `KhungTinNhan` — chỉ đổi
  chữ ký để component cha nhận được `traLoiId`).
- State mới: `const [dangTraLoiId, setDangTraLoiId] = useState<string | null>(null);`
- Biến dẫn xuất: `const tinDangTraLoi = danhSachTinNhan.find((tn) => tn.id === dangTraLoiId) ?? null;`
- Mỗi tin nhắn bọc trong `<div className="khung-tin-nhan__hang">` (mới)
  chứa: bong bóng hiện tại + 1 lớp icon nổi
  `<div className="khung-tin-nhan__icon-noi">` chứa 1 nút:
  ```tsx
  <button
    type="button"
    className="khung-tin-nhan__nut-tra-loi"
    onClick={(su) => { su.stopPropagation(); setDangTraLoiId(tn.id); noiDungRef.current?.focus(); }}
    aria-label="Trả lời tin nhắn này"
  >
    <BieuTuongTraLoi />
  </button>
  ```
  (`su.stopPropagation()` để không kích hoạt toggle-giờ của bong bóng
  khi bấm đúng nút icon.)
- Trong mỗi bong bóng, NẾU `tn.traLoi` không null, hiện khối trích dẫn
  NGAY TRƯỚC nội dung tin nhắn (trước dòng `{tn.loaiTinNhan === 'Anh' && ...}`):
  ```tsx
  {tn.traLoi && (
    <div className="khung-tin-nhan__trich-dan">
      <span className="khung-tin-nhan__trich-dan-ten">{tn.traLoi.tenNguoiGui}</span>
      <span className="khung-tin-nhan__trich-dan-noi-dung">{tn.traLoi.noiDungTomTat}</span>
    </div>
  )}
  ```
- Card File đổi bố cục (thay khối `{tn.loaiTinNhan === 'File' && (...)}`
  hiện tại):
  ```tsx
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
  ```
- Trước `<form className="khung-tin-nhan__form-gui">`, NẾU
  `tinDangTraLoi` không null, hiện khối preview:
  ```tsx
  {tinDangTraLoi && (
    <div className="khung-tin-nhan__dang-tra-loi">
      <div className="khung-tin-nhan__dang-tra-loi-noi-dung">
        <span className="khung-tin-nhan__dang-tra-loi-tieu-de">
          ↩ Trả lời {tinDangTraLoi.nguoiGuiId === idHienTai ? 'chính mình' : tenHienThi}
        </span>
        <span className="khung-tin-nhan__dang-tra-loi-trich">
          {tinDangTraLoi.loaiTinNhan === 'Text'
            ? tinDangTraLoi.noiDungTinNhan
            : tinDangTraLoi.loaiTinNhan === 'Anh' ? '[Ảnh]' : `[File] ${tinDangTraLoi.tenFileGoc}`}
        </span>
      </div>
      <button type="button" className="khung-tin-nhan__dang-tra-loi-huy" onClick={() => setDangTraLoiId(null)} aria-label="Hủy trả lời">×</button>
    </div>
  )}
  ```
  Ghi chú: dùng `tenHienThi` (prop tiêu đề chung của header, xem comment
  đã có ở GĐ5f) làm tên hiển thị khi trả lời người khác — component này
  chỉ có ngữ cảnh 1 cuộc trò chuyện tại 1 thời điểm nên tên header CHÍNH
  LÀ tên người gửi gốc trong mọi trường hợp không phải chính mình (áp
  dụng cho cả 1-1 lẫn nhóm, vì trong nhóm `tenHienThi` là tên NHÓM chứ
  không phải người gửi — xem điều chỉnh bên dưới).

  **Điều chỉnh quan trọng cho nhóm:** trong hội thoại nhóm, `tenHienThi`
  là TÊN NHÓM, không phải tên người gửi gốc — vậy dòng tiêu đề trả lời
  phải dùng `tinDangTraLoi.traLoi` KHÔNG tồn tại (đó là field của tin
  ĐƯỢC trả lời TỚI, không phải chính `tinDangTraLoi`). Sửa đúng: tên
  người gửi gốc lấy trực tiếp từ chính tin đang được trả lời — cần biết
  tên hiển thị của `tinDangTraLoi.nguoiGuiId`. Vì `KhungTinNhan` không
  có sẵn danh sách người dùng để tra tên theo id, thêm prop mới:
  ```typescript
  layTenNguoiGui?: (nguoiGuiId: string) => string;
  ```
  Component cha (`TrangChat.tsx`/`TrangNhom.tsx`) truyền hàm tra tên phù
  hợp ngữ cảnh (1-1: nếu id là mình → tên mình từ `nguoiDungHienTai`,
  ngược lại → `tenHienThi` header sẵn có vì đó chính là người đang chat
  cùng; nhóm: tra trong `nhom.thanhVien` theo id). Nếu không truyền prop
  này (không bắt buộc), fallback hiển thị "một người dùng". Dòng tiêu đề
  trả lời đổi thành:
  ```tsx
  ↩ Trả lời {layTenNguoiGui ? layTenNguoiGui(tinDangTraLoi.nguoiGuiId) : 'một người dùng'}
  ```
- `xuLySubmit` gọi `onGuiVanBan(gtHienTai, dangTraLoiId)` thay vì
  `onGuiVanBan(gtHienTai)`; sau khi gọi, thêm `setDangTraLoiId(null);`.
- `xuLyChonTep` gọi `onGuiTep(tep, dangTraLoiId)` thay vì
  `onGuiTep(tep)`; sau khi gọi, thêm `setDangTraLoiId(null);`.

### 4.3. `BieuTuong.tsx` — icon mới

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

### 4.4. `KhungTinNhan.css` — quy tắc mới

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

Áp class `khung-tin-nhan__hang--minh` khi `laCuaMinh` (JSX bọc ngoài
bong bóng hiện có), và class `khung-tin-nhan__hang--mo` khi
`tinDangMoId === tn.id` (mobile: bấm hiện đồng thời giờ + icon).

### 4.5. `TrangChat.tsx` / `TrangNhom.tsx`

- `guiTinNhanVanBan(noiDungGui: string)` đổi thành
  `guiTinNhanVanBan(noiDungGui: string, traLoiId: string | null)`, thêm
  `traLoiId` làm tham số cuối khi gọi
  `ketNoi.invoke('GuiTinNhan', ..., traLoiId)`.
- `guiTep(tep: File)` đổi thành `guiTep(tep: File, traLoiId: string | null)`,
  tương tự thêm `traLoiId` vào cuối lời gọi `invoke('GuiTinNhan', ...)`.
- Truyền `onGuiVanBan={guiTinNhanVanBan}` / `onGuiTep={guiTep}` (chữ ký
  đã khớp, JSX không đổi gì thêm ở điểm gọi).
- Truyền prop mới `layTenNguoiGui` cho `<KhungTinNhan>`:
  - `TrangChat.tsx`: `layTenNguoiGui={(id) => (id === idHienTai ? (nguoiDungHienTai?.tenTaiKhoan ?? 'Bạn') : (nguoiDangChon?.tenHienThi ?? 'một người dùng'))}`
    (dùng `tenTaiKhoan` của chính mình ở đây CHỈ vì `nguoiDungHienTai`
    lấy từ JWT decode không có `tenHienThi` — xem ghi chú GĐ5f; đây là
    hạn chế đã biết trước, không phải lỗi mới. Có thể thay `'Bạn'` cho
    đơn giản và nhất quán — dùng `'Bạn'` trực tiếp, KHÔNG dùng
    `nguoiDungHienTai?.tenTaiKhoan`, để tránh phụ thuộc vào field không
    đáng tin cậy này).

    **Quyết định cuối:** `layTenNguoiGui={(id) => (id === idHienTai ? 'Bạn' : (nguoiDangChon?.tenHienThi ?? 'một người dùng'))}`
  - `TrangNhom.tsx`: `layTenNguoiGui={(id) => (id === idHienTai ? 'Bạn' : (nhomDangChon?.thanhVien.find((tv) => tv.id === id)?.tenHienThi ?? 'một người dùng'))}`

## 5. Kiểm thử

- Backend: 4 test mới liệt kê ở §3.6, cộng với việc build sạch (không
  test nào bị vỡ vì thêm tham số).
- Frontend `KhungTinNhan.test.tsx`:
  - `bam icon Tra loi hien khoi dang tra loi voi ten va trich dan dung`
  - `huy tra loi an khoi preview va khong gui kem traLoiId`
  - `gui tin nhan luc dang tra loi goi onGuiVanBan voi dung traLoiId`
  - `tin nhan co truong traLoi hien khoi trich dan trong bong bong`
  - `card File hien nut tron tai xuong rieng biet voi card`
- Frontend `TrangChat.test.tsx`/`TrangNhom.test.tsx`: cập nhật fixture
  `TinNhan` thêm field `traLoi: null` (rà mọi nơi dựng object `TinNhan`
  literal — dùng `tsc --noEmit` để tự liệt kê vị trí lỗi thiếu field,
  theo đúng kinh nghiệm GĐ5f).
