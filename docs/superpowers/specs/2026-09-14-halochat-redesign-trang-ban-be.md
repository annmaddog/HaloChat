# HaloChat — Redesign trang Bạn bè + Xóa bạn + Nhắn tin người lạ (GĐ5d)

> Spec này viết theo mockup chi tiết người dùng cung cấp cho trang Bạn bè,
> đồng bộ phong cách với redesign trang Nhóm/Cài đặt (GĐ5c).

## 1. Bối cảnh & mục tiêu

Trang Bạn bè hiện tại (`TrangBanBe.tsx`) là 3 khối liệt kê liên tục (lời mời
đến / bạn bè / tìm người) luôn hiện hết, không có tab, giao diện đơn sơ.
Cần làm lại theo mockup: 2 tab rõ ràng, card gọn (không nhồi quá nhiều nút),
tìm kiếm chỉ hiện kết quả khi có từ khóa, và làm rõ luồng nhắn tin với
người lạ (server đã enforce đúng từ GĐ5b-1, chỉ thiếu hiển thị đúng ở
frontend).

## 2. Phạm vi

**Trong phạm vi:**
- Viết lại `TrangBanBe.tsx`/`.css`: thanh tìm kiếm lớn, 2 tab (Bạn bè /
  Lời mời kèm số đếm), danh sách bạn bè dạng card gọn, tab Lời mời có
  accept/reject + empty state riêng, tìm kiếm chỉ hiện kết quả khi gõ,
  empty state toàn trang khi chưa có bạn nào.
- Menu "⋯" trên mỗi card bạn bè: "Xem thông tin" (mở lại khung hồ sơ đã có
  từ GĐ5c) + "Xóa bạn" (tính năng mới).
- Nút "Nhắn tin" trên card bạn bè + trên card kết quả tìm kiếm, hiển thị
  có điều kiện theo quan hệ bạn bè và cài đặt `ChoPhepTinNhanTuNguoiLa`
  của người kia (logic đã enforce ở backend từ trước — spec này chỉ thêm
  phần hiển thị đúng ở frontend).
- Backend: endpoint mới `DELETE /api/ketban/ban-be/{idBanBe}` (xóa bạn);
  mở rộng `NguoiDungTomTatDto` thêm field `ChoPhepTinNhanTuNguoiLa` để
  frontend biết hiển thị nút nào ở card tìm kiếm.

**Ngoài phạm vi:**
- Không đổi logic chặn gửi tin nhắn ở `DichVuTinNhan.GuiTinNhanAsync` —
  đã đúng, không cần sửa.
- Không thêm thông báo real-time khi bị xóa bạn (SignalR) — người bị xóa
  chỉ thấy mất khi tải lại trang.
- Không đổi trang Cài đặt (`ChoPhepTinNhanTuNguoiLa` đã có sẵn ở đó).

## 3. Backend

### 3.1. Mở rộng `NguoiDungTomTatDto`

```csharp
public record NguoiDungTomTatDto(string Id, string TenTaiKhoan, string Email, bool ChoPhepTinNhanTuNguoiLa);
```

Field này không nhạy cảm (chính là mục đích công khai của cài đặt đó), nên
dùng chung 1 DTO cho mọi nơi trả về (bạn bè, thành viên nhóm, lời mời, tìm
kiếm) thay vì tạo DTO riêng cho tìm kiếm. Cập nhật cả 4 nơi khởi tạo DTO
này: `DichVuNhom.cs` (`AnhXaDtoAsync`), `DichVuTinNhan.cs`, `DichVuNguoiDung.cs`
(`LayDanhSachNguoiDung`), `DichVuKetBan.cs` (`AnhXaDto`, `LayBanBeAsync`) —
mỗi nơi đã có sẵn object `NguoiDung` trong tay, chỉ cần đọc thêm
`.ChoPhepTinNhanTuNguoiLa`.

### 3.2. Xóa bạn — endpoint mới

```
DELETE /api/ketban/ban-be/{idBanBe}
```

- `[Authorize]` (thừa hưởng từ `[Authorize]` cấp class của `KetBanController`).
- `ILoiMoiKetBanRepository` thêm `Task XoaAsync(string nguoiA, string nguoiB)`
  — xóa (không phải cập nhật trạng thái) bản ghi `LoiMoiKetBan` có
  `TrangThai == DaChapNhan` giữa 2 người (dùng lại `BoLocCapDoi` đã có
  trong `LoiMoiKetBanRepository`), qua `_collection.DeleteOneAsync(...)`.
  Xóa hẳn (không set trạng thái mới) để `TonTaiLoiMoiDangHoatDongAsync`
  trả về `false` sau đó — cho phép gửi lại lời mời kết bạn mới nếu muốn
  kết bạn lại sau này.
- `IDichVuKetBan` thêm `Task HuyKetBanAsync(string nguoiHienTaiId, string idBanBe)`:
  kiểm tra `idBanBe` có phải id hợp lệ (`ObjectId.TryParse`) và có đang là
  bạn bè thật với `nguoiHienTaiId` không (`LaBanBeAsync`) — không phải thì
  ném `KhongPhaiBanBeException` mới (file `NgoaiLeKetBan.cs`) → `404`.
  Gọi `_khoLoiMoi.XoaAsync(nguoiHienTaiId, idBanBe)`.
- `KetBanController` thêm:
  ```csharp
  [HttpDelete("ban-be/{idBanBe}")]
  public async Task<IActionResult> XoaBanBe(string idBanBe)
  {
      if (IdHienTai is null) return Unauthorized();
      try
      {
          await _dichVu.HuyKetBanAsync(IdHienTai, idBanBe);
          return Ok(new { thongBao = "Đã xóa bạn." });
      }
      catch (KhongPhaiBanBeException loi)
      {
          return NotFound(new { thongBao = loi.Message });
      }
  }
  ```

## 4. Frontend

### 4.1. `KieuDuLieu.ts` + `DichVuApi.ts`

- `NguoiDungTomTat` thêm `choPhepTinNhanTuNguoiLa: boolean`.
- `DichVuApi.ts` thêm `XoaBanBe(token: string, idBanBe: string): Promise<KetQuaThongBao>`
  → `DELETE /ketban/ban-be/{idBanBe}`.

### 4.2. `TrangBanBe.tsx` — cấu trúc mới

State mới: `tabDangChon: 'ban-be' | 'loi-moi'`, `menuMoChoId: string | null`
(id người đang mở menu "⋯", đóng khi click ra ngoài — theo đúng pattern
đã dùng ở `ThongBao.tsx` cũ trước khi bị xóa, hoặc đơn giản hơn: toggle
theo id, đóng khi bấm lại hoặc bấm mục khác).

```
┌ Thanh tìm kiếm lớn (luôn hiện trên cùng, độc lập với tab) ┐
├ [Bạn bè]  [Lời mời (N)]  ← 2 tab                          ┤
├ (nếu có từ khóa tìm kiếm) Kết quả tìm kiếm: ...            ┤
├ (nếu KHÔNG có từ khóa) nội dung theo tab đang chọn:        │
│   tab Bạn bè: "Bạn bè của tôi (N)" + danh sách card        │
│   tab Lời mời: "Lời mời kết bạn (N)" + danh sách/empty     │
└─────────────────────────────────────────────────────────────┘
```

Khi có từ khóa tìm kiếm, ẩn nội dung tab (chỉ hiện kết quả tìm kiếm) —
đúng yêu cầu "không hiển thị khu vực trống lớn khi chưa tìm kiếm" và
tránh 2 danh sách chồng nhau.

**Card bạn bè** (tab Bạn bè):
```tsx
<div className="trang-ban-be__card">
  <Avatar id={b.id} ten={b.tenTaiKhoan} />
  <div className="trang-ban-be__card-thong-tin">
    <span className="trang-ban-be__card-ten">{b.tenTaiKhoan}</span>
    {trangThaiOnline[b.id] && <span className="trang-ban-be__card-trang-thai">Đang hoạt động</span>}
  </div>
  <button className="nut-chinh trang-ban-be__nut-nhan-tin" onClick={() => navigate('/nguoi-dung', { state: { moNguoiDung: b } })}>
    Nhắn tin
  </button>
  <div className="trang-ban-be__menu-cum">
    <button aria-label="Thêm thao tác" onClick={() => setMenuMoChoId(menuMoChoId === b.id ? null : b.id)}>⋯</button>
    {menuMoChoId === b.id && (
      <div className="trang-ban-be__menu">
        <button onClick={() => { setHoSoDangXem(b); setMenuMoChoId(null); }}>Xem thông tin</button>
        <button className="trang-ban-be__menu-nguy-hiem" onClick={() => xoaBan(b)}>Xóa bạn</button>
      </div>
    )}
  </div>
</div>
```
Trạng thái hoạt động: dùng lại `LayTrangThaiHoatDong` đã có (như
`TrangChat.tsx` đang làm) — gọi 1 lần khi tải xong danh sách bạn bè.

**Card kết quả tìm kiếm** — 3 biến thể theo đúng mockup, quyết định bằng
`nd.choPhepTinNhanTuNguoiLa` (đã có sẵn nhờ mục 3.1) và việc đã gửi lời
mời hay chưa (`idDaGuiLoiMoi` — set các id trong `loiMoiGui`):

```tsx
<div className="trang-ban-be__card trang-ban-be__card--tim-kiem">
  <Avatar id={nd.id} ten={nd.tenTaiKhoan} />
  <div className="trang-ban-be__card-thong-tin">
    <span className="trang-ban-be__card-ten">{nd.tenTaiKhoan}</span>
    <span className="trang-ban-be__card-email">{nd.email}</span>
    {!nd.choPhepTinNhanTuNguoiLa && (
      <span className="trang-ban-be__card-khoa">🔒 Chỉ nhận tin nhắn từ bạn bè</span>
    )}
  </div>
  <div className="trang-ban-be__card-hanh-dong">
    {nd.choPhepTinNhanTuNguoiLa && (
      <button className="nut-phu" onClick={() => navigate('/nguoi-dung', { state: { moNguoiDung: nd } })}>Nhắn tin</button>
    )}
    {idDaGuiLoiMoi.has(nd.id) ? (
      <button className="nut-chinh" disabled>Đã gửi lời mời</button>
    ) : (
      <button className="nut-chinh" onClick={() => guiLoiMoi(nd.id)}>Kết bạn</button>
    )}
  </div>
</div>
```
Người đã là bạn bè hoặc đang có lời mời **đến** (người khác gửi cho mình) thì
**không xuất hiện** trong kết quả tìm kiếm. Người mình đã gửi lời mời đi (lời
mời **đi**) vẫn xuất hiện, nhưng nút đổi thành "Đã gửi lời mời" (disabled) —
nếu không, tính năng hiển thị trạng thái "Đã gửi lời mời" mô tả ở phần Trạng
thái sẽ không bao giờ render được.

**Tab Lời mời:**
```tsx
{tabDangChon === 'loi-moi' && (
  <>
    <h2>Lời mời kết bạn ({loiMoiDen.length})</h2>
    {loiMoiDen.length === 0 ? (
      <div className="trang-ban-be__trong">
        <p className="trang-ban-be__trong-tieu-de">Không có lời mời kết bạn</p>
        <p>Bạn chưa có lời mời kết bạn nào.</p>
      </div>
    ) : (
      <ul className="trang-ban-be__danh-sach">
        {loiMoiDen.map((l) => (
          <li key={l.id} className="trang-ban-be__card">
            <Avatar id={l.nguoiGui.id} ten={l.nguoiGui.tenTaiKhoan} />
            <div className="trang-ban-be__card-thong-tin">
              <span className="trang-ban-be__card-ten">{l.nguoiGui.tenTaiKhoan}</span>
              <span className="trang-ban-be__card-phu">Muốn kết bạn với bạn</span>
            </div>
            <div className="trang-ban-be__card-hanh-dong">
              <button className="nut-chinh" onClick={() => chapNhan(l.id)}>Chấp nhận</button>
              <button className="nut-phu" onClick={() => tuChoi(l.id)}>Từ chối</button>
            </div>
          </li>
        ))}
      </ul>
    )}
  </>
)}
```

**Empty state toàn trang** (tab Bạn bè, chưa có bạn nào, KHÔNG đang tìm
kiếm):
```tsx
<div className="trang-ban-be__trong-toan-trang">
  <p className="trang-ban-be__trong-tieu-de">Bạn chưa có người bạn nào</p>
  <p>Hãy tìm kiếm và kết bạn với những người bạn biết.</p>
  <button className="nut-chinh" onClick={() => inputTimKiemRef.current?.focus()}>Tìm bạn bè</button>
</div>
```

**Xóa bạn:**
```tsx
function xoaBan(b: NguoiDungTomTat) {
  if (!token) return;
  if (!window.confirm(`Xóa ${b.tenTaiKhoan} khỏi danh sách bạn bè?`)) return;
  XoaBanBe(token, b.id)
    .then(() => setBanBe((truoc) => truoc.filter((x) => x.id !== b.id)))
    .catch((loiBat) => setLoi(loiBat instanceof LoiGoiApi ? loiBat.message : 'Xóa bạn thất bại.'));
}
```

Khung hồ sơ bên phải (đã có từ GĐ5c, `hoSoDangXem`) giữ nguyên, chỉ đổi
điểm gọi mở nó sang "Xem thông tin" trong menu "⋯" thay vì bấm cả dòng.

### 4.3. CSS

- Chiều rộng nội dung `max-width: 900px; margin: 0 auto;`.
- Card: `border-radius: 14px; border: 1px solid var(--mau-vien); box-shadow: 0 2px 8px -4px rgba(31,66,135,0.15);` (bóng rất nhẹ hơn hẳn `--bong-the` hiện dùng cho modal).
- Tab đang chọn: `background: var(--mau-nen-tren); color: var(--mau-chinh-dam); font-weight: 700;`.
- Tên/email dài tự động `...`: `overflow: hidden; text-overflow: ellipsis; white-space: nowrap;` trên `.trang-ban-be__card-ten`/`-email`, kèm `min-width: 0` trên container flex cha (bắt buộc để ellipsis hoạt động trong flexbox).
- Responsive `@media (max-width: 640px)`: card `.trang-ban-be__card` chuyển layout để nút "Nhắn tin" đủ lớn chạm tay (`min-height: 40px`), menu "⋯" dropdown dùng `right: 0` để không tràn ra ngoài màn hình.

## 5. Kiểm thử

- Backend: unit test `DichVuKetBanTests` cho `HuyKetBanAsync` (xóa đúng
  cặp, xóa khi không phải bạn bè → `KhongPhaiBanBeException`), integration
  test `KetBanControllerTests` cho `DELETE /api/ketban/ban-be/{id}`.
- Frontend: test cho `TrangBanBe.tsx` — hiện đúng nút theo
  `choPhepTinNhanTuNguoiLa` (3 biến thể mockup), tab chuyển đúng, tìm kiếm
  ẩn/hiện đúng nội dung, xóa bạn gọi đúng API + xác nhận, empty state hiện
  đúng khi rỗng.

## 6. Ngoài phạm vi — nhắc lại

Không đổi logic chặn gửi tin nhắn ở backend (đã đúng từ trước), không có
sự kiện real-time khi bị xóa bạn, không đổi trang Cài đặt.
