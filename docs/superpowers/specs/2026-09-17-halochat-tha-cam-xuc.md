# HaloChat — Thả cảm xúc tin nhắn (GĐ7c)

## 1. Bối cảnh & mục tiêu

Thêm icon 👍 nổi cạnh mỗi tin nhắn (cùng cụm hover với "Trả lời"/"..."
đã có ở GĐ6a/6b). Bấm giữ/hover icon hiện 1 hàng ngang 6 cảm xúc kiểu
Messenger để chọn; bấm thẳng vào icon 👍 (không qua hàng chọn) = thả
nhanh cảm xúc "Thích"; bấm lại icon khi đã thả rồi = bỏ cảm xúc. Bong
bóng tin nhắn hiện badge nhỏ tổng hợp cảm xúc + số lượng.

## 2. Phạm vi

**Trong phạm vi:**
- Đúng 6 loại cảm xúc, cố định (không thêm/bớt được):
  `Thich` 👍, `YeuThich` ❤️, `Haha` 😂, `Wow` 😮, `Buon` 😢, `PhanNo` 😠.
- Mỗi người dùng CHỈ thả được 1 cảm xúc / 1 tin nhắn tại 1 thời điểm —
  thả cảm xúc khác thì THAY THẾ cảm xúc cũ của chính người đó (không
  cộng dồn nhiều loại cho cùng 1 người).
- Ai trong hội thoại 1-1 hoặc thành viên nhóm cũng thả được — không
  giới hạn quyền, giống Ghim (GĐ6b).
- Icon 👍 nổi: THÊM vào cụm `.khung-tin-nhan__icon-noi` đã có (cạnh Trả
  lời/...), không thay thế 2 icon đó.
- Hover/bấm-giữ icon 👍 → hiện popup ngang 6 emoji cảm xúc phía trên
  icon; bấm 1 emoji để thả đúng loại đó; di chuột ra khỏi popup không
  qua icon con nào cũng tự ẩn popup (giống dropdown menu đã có).
- Bấm thẳng vào icon 👍 (không hover chờ popup, chỉ click nhanh) → nếu
  CHƯA thả cảm xúc nào thì thả "Thích" ngay; nếu ĐÃ thả rồi (bất kỳ
  loại nào) thì bỏ cảm xúc đó (toggle).
- Dưới/cạnh mỗi bong bóng có gắn ≥1 cảm xúc: hiện badge nhỏ tổng hợp,
  ví dụ "👍❤️ 3" (tối đa 3 emoji khác nhau xuất hiện trước, kèm tổng số
  lượt) — bấm vào badge KHÔNG mở danh sách chi tiết ai đã thả gì (ngoài
  phạm vi, giữ đơn giản).
- Broadcast realtime 2 chiều (giống Ghim/Thu hồi ở GĐ6b) — phía kia
  thấy badge cập nhật ngay không cần tải lại trang.
- Tin đã THU HỒI: badge cảm xúc (nếu có, gắn trước khi thu hồi) vẫn ẩn
  đi cùng lúc với nội dung — không hiện badge trên placeholder "Tin
  nhắn đã được thu hồi." (dọn sạch UI, tránh cảm xúc trỏ vào nội dung
  không còn tồn tại).

**Ngoài phạm vi:**
- Không hiện danh sách chi tiết "ai đã thả cảm xúc gì" khi bấm badge.
- Không thêm/tùy biến bộ cảm xúc (cố định đúng 6 loại).
- Không thả cảm xúc cho tin do CHÍNH MÌNH gửi bị chặn hay cho phép đặc
  biệt gì khác — xử lý y hệt tin của người khác (tự thả cảm xúc cho tin
  của mình vẫn hợp lệ, không cần chặn — giống Facebook/Messenger thật).

## 3. Backend

### 3.1. Model

**`Models/CamXucTinNhan.cs`** (mới, nhúng trong `TinNhan`, không có
document Mongo riêng):
```csharp
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HaloChat.Api.Models;

public enum LoaiCamXuc
{
    Thich,
    YeuThich,
    Haha,
    Wow,
    Buon,
    PhanNo,
}

public class CamXucTinNhan
{
    [BsonRepresentation(BsonType.ObjectId)]
    public string NguoiDungId { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.String)]
    public LoaiCamXuc LoaiCamXuc { get; set; }
}
```

`TinNhan.cs` thêm:
```csharp
    // [GĐ7c] Mỗi người dùng chỉ có tối đa 1 phần tử trong danh sách này
    // (thả cảm xúc khác = thay thế, không cộng dồn).
    public List<CamXucTinNhan> DanhSachCamXuc { get; set; } = new();
```

### 3.2. Exception

Tái dùng `KhongCoQuyenTrenTinNhanException`/`TinNhanKhongTonTaiException`/
`NhomKhongTonTaiException`/`KhongPhaiThanhVienNhomException` đã có từ
GĐ6b — không cần exception mới.

### 3.3. Repository

`ITinNhanRepository` thêm:
```csharp
/// <summary>Thả/thay thế cảm xúc của nguoiDungId trên tin nhắn id. Tự động xóa cảm xúc cũ (nếu có) của cùng người trước khi thêm cảm xúc mới.</summary>
Task ThaCamXucAsync(string id, string nguoiDungId, LoaiCamXuc loaiCamXuc);

/// <summary>Xóa cảm xúc của nguoiDungId trên tin nhắn id (nếu có).</summary>
Task BoCamXucAsync(string id, string nguoiDungId);
```
Mongo thật dùng 2 bước nguyên tử qua `Builders<TinNhan>.Update.PullFilter` (xóa phần tử cũ của đúng `nguoiDungId` nếu có) rồi `.Push` (thêm phần tử mới) — hoặc đơn giản hơn, 1 lệnh `PullFilter` + 1 lệnh `Push` chạy tuần tự trong cùng method (không cần transaction, tình huống double-write hiếm và vô hại vì kết quả cuối vẫn đúng 1 phần tử/người sau lần ghi gần nhất). Fake `TinNhanGiaLap`: thao tác trực tiếp trên `List<CamXucTinNhan>` của phần tử tìm được trong `DanhSach`.

### 3.4. Service

`IDichVuTinNhan` thêm:
```csharp
Task<TinNhanDto> ThaCamXucAsync(string idHienTai, string tinNhanId, string loaiCamXuc);
Task<TinNhanDto> BoCamXucAsync(string idHienTai, string tinNhanId);
```
Dùng lại `KiemTraQuyenTrenTinNhanAsync` đã có ở GĐ6b (Ghim dùng
chung logic quyền y hệt — bất kỳ ai trong hội thoại/thành viên nhóm).
`ThaCamXucAsync` parse `loaiCamXuc` bằng `Enum.TryParse<LoaiCamXuc>`,
ném `TinNhanKhongHopLeException` nếu chuỗi không khớp 1 trong 6 giá trị
hợp lệ.

`AnhXaDto` — thêm field `DanhSachCamXuc` vào `TinNhanDto` (map sang
`List<CamXucDto>`), và **ẩn hoàn toàn khi `DaThuHoi`** (nhánh `if
(t.DaThuHoi)` trả `DanhSachCamXuc = new()` rỗng, bất kể dữ liệu gốc còn
gì trong Mongo — đúng yêu cầu "không hiện badge trên tin đã thu hồi").

### 3.5. DTO

```csharp
public record CamXucDto(string NguoiDungId, string LoaiCamXuc);
```
`TinNhanDto` thêm tham số cuối `List<CamXucDto> DanhSachCamXuc`.

### 3.6. Hub

```csharp
public async Task<TinNhanDto> ThaCamXucTinNhan(string tinNhanId, string loaiCamXuc)
{
    try
    {
        var tinNhan = await _dichVuTinNhan.ThaCamXucAsync(NguoiDungHienTaiId, tinNhanId, loaiCamXuc);
        await GuiBroadcastCapNhatAsync(tinNhan, "TinNhanDaCamXuc");
        return tinNhan;
    }
    catch (TinNhanKhongTonTaiException loi) { throw new HubException(loi.Message); }
    catch (TinNhanKhongHopLeException loi) { throw new HubException(loi.Message); }
    catch (KhongCoQuyenTrenTinNhanException loi) { throw new HubException(loi.Message); }
    catch (NhomKhongTonTaiException loi) { throw new HubException(loi.Message); }
    catch (KhongPhaiThanhVienNhomException loi) { throw new HubException(loi.Message); }
}

public async Task<TinNhanDto> BoCamXucTinNhan(string tinNhanId)
{
    try
    {
        var tinNhan = await _dichVuTinNhan.BoCamXucAsync(NguoiDungHienTaiId, tinNhanId);
        await GuiBroadcastCapNhatAsync(tinNhan, "TinNhanDaCamXuc");
        return tinNhan;
    }
    catch (TinNhanKhongTonTaiException loi) { throw new HubException(loi.Message); }
    catch (KhongCoQuyenTrenTinNhanException loi) { throw new HubException(loi.Message); }
    catch (NhomKhongTonTaiException loi) { throw new HubException(loi.Message); }
    catch (KhongPhaiThanhVienNhomException loi) { throw new HubException(loi.Message); }
}
```
(Dùng CHUNG sự kiện broadcast `"TinNhanDaCamXuc"` cho cả thả và bỏ —
frontend chỉ cần thay `TinNhanDto` mới nhất, không cần phân biệt hành
động, đơn giản hơn 2 sự kiện riêng.)

## 4. Frontend

### 4.1. Kiểu dữ liệu + API

```typescript
export type LoaiCamXuc = 'Thich' | 'YeuThich' | 'Haha' | 'Wow' | 'Buon' | 'PhanNo';
export interface CamXuc { nguoiDungId: string; loaiCamXuc: LoaiCamXuc; }
```
`TinNhan` thêm `danhSachCamXuc: CamXuc[];`.

Bảng ánh xạ emoji hiển thị (đặt trong `KhungTinNhan.tsx`, dùng chung
cho popup chọn VÀ badge tổng hợp):
```typescript
const EMOJI_CAM_XUC: Record<LoaiCamXuc, string> = {
  Thich: '👍', YeuThich: '❤️', Haha: '😂', Wow: '😮', Buon: '😢', PhanNo: '😠',
};
```

### 4.2. `KhungTinNhan.tsx`

Props mới: `onThaCamXuc: (id: string, loaiCamXuc: LoaiCamXuc) => void`,
`onBoCamXuc: (id: string) => void`.

- Icon 👍 mới trong `.khung-tin-nhan__icon-noi`, cạnh icon Trả lời:
  `onClick` toggle nhanh (thả "Thich" nếu tin CHƯA có cảm xúc của
  `idHienTai` trong `tn.danhSachCamXuc`, gọi `onBoCamXuc` nếu ĐÃ có).
  `onMouseEnter` (desktop) mở popup 6 emoji sau 400ms hover (tránh hiện
  ngay khi lướt chuột qua); `onMouseLeave` khỏi cả icon LẪN popup mới
  đóng (dùng state `hienPopupCamXucChoTinNhanId` + kiểm tra bằng
  `relatedTarget`/`closest` giống cơ chế đóng dropdown đã có).
- Popup 6 emoji: dãy `<button>` mỗi cái gọi
  `onThaCamXuc(tn.id, loai)` rồi đóng popup.
- Badge tổng hợp: nếu `tn.danhSachCamXuc.length > 0 && !tn.daThuHoi`,
  hiện `<span className="khung-tin-nhan__badge-cam-xuc">` chứa tối đa 3
  emoji KHÁC LOẠI xuất hiện đầu tiên (Set các `loaiCamXuc` duy nhất,
  lấy 3 phần tử đầu theo thứ tự xuất hiện trong mảng) + tổng số lượt
  (`tn.danhSachCamXuc.length`), đặt ở góc dưới bong bóng (giống mockup
  Messenger — icon nổi nhỏ đè lên mép bong bóng).

### 4.3. `BieuTuong.tsx`

Không cần thêm SVG mới cho icon 👍 nổi — dùng trực tiếp ký tự emoji
"👍" (nhất quán với cách các icon khác trong dự án đôi lúc dùng emoji
trực tiếp, ví dụ 🔒/📎 trước đây) thay vì vẽ SVG riêng, vì bản thân icon
này CHÍNH LÀ 1 emoji theo đúng mockup, không cần style icon-outline như
các icon SVG khác.

### 4.4. `TrangChat.tsx` / `TrangNhom.tsx`

Thêm 2 handler `thaCamXuc(id, loai)`/`boCamXuc(id)` gọi hub
`ThaCamXucTinNhan`/`BoCamXucTinNhan`, cập nhật state qua
`capNhatTinNhanTrongState` đã có (KHÔNG cần chạm `tinNhanGhimTheoDoiTac`/
`tinNhanGhimTheoNhom` vì cảm xúc không liên quan tới ghim — nhưng NẾU
tin đó đang có mặt trong banner ghim, `capNhatTinNhanTrongState` chỉ
cập nhật state chính, banner ghim đọc từ `tinNhanGhimTheoDoiTac` riêng
— cảm xúc không hiện trong banner ghim theo thiết kế mockup nên không
cần đồng bộ thêm, giữ scope gọn). Đăng ký/hủy đăng ký listener
`TinNhanDaCamXuc` giống các listener khác — dùng lại
`capNhatTinNhanTrongState` trực tiếp làm handler (không cần hàm bọc
riêng vì không đụng state ghim).

## 5. Kiểm thử

- Backend: `ThaCamXucAsync_ChuaTung_ThemMoi`,
  `ThaCamXucAsync_DaCoCamXucKhac_ThayTheKhongCongDon`,
  `ThaCamXucAsync_LoaiCamXucKhongHopLe_NemTinNhanKhongHopLe`,
  `BoCamXucAsync_DaCo_XoaDung`,
  `BoCamXucAsync_ChuaCo_KhongLamGi`,
  `ThaCamXucAsync_KhongThuocHoiThoai_NemKhongCoQuyen`,
  `AnhXaDto_TinDaThuHoi_AnDanhSachCamXuc` (qua `LayLichSuAsync` sau khi
  vừa thả cảm xúc rồi thu hồi — assert `DanhSachCamXuc` rỗng trong
  DTO trả về dù dữ liệu gốc còn trong Mongo); test hub cho broadcast
  2 chiều tương tự `GhimTinNhan`.
- Frontend: `KhungTinNhan.test.tsx` — bấm nhanh icon 👍 thả "Thich",
  bấm lại bỏ cảm xúc, hover mở popup 6 emoji đúng thứ tự, bấm 1 emoji
  trong popup gọi đúng `onThaCamXuc` với đúng loại, badge hiện đúng
  emoji+số lượng, tin đã thu hồi không hiện badge dù `danhSachCamXuc`
  không rỗng; test `TrangChat.test.tsx`/`TrangNhom.test.tsx` cho
  handler + listener realtime.
