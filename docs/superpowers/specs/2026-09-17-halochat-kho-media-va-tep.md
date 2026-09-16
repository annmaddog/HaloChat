# HaloChat — Kho Media & Tệp (GĐ7a)

## 1. Bối cảnh & mục tiêu

Thêm icon (folder) trên header `KhungTinNhan` mở modal liệt kê toàn bộ
Ảnh và Tệp đã chia sẻ trong ĐÚNG cuộc trò chuyện đang mở (1-1 hoặc
nhóm), chia 2 tab. Đây là vị trí và phạm vi HOÀN TOÀN KHÁC với mục
"Ảnh/Video" đã bị xóa khỏi trang "Thông tin nhóm" ở GĐ5e (khác trigger —
icon header khung chat chứ không phải panel thông tin nhóm — và khác
nội dung — gồm cả Tài liệu, không chỉ Ảnh) — không mâu thuẫn quyết định
cũ.

## 2. Phạm vi

**Trong phạm vi:**
- Áp dụng cho CẢ chat 1-1 lẫn chat nhóm.
- Icon folder trên header `KhungTinNhan`, cạnh cụm tên/tiêu đề.
- Modal: icon folder trong khung tròn xanh nhạt + tiêu đề "Kho lưu trữ
  Media & Tệp" + phụ đề (tên người/nhóm đang chat) + nút X đóng.
- 2 tab: "Hình ảnh (N)" / "Tài liệu & Tệp (N)" — N là số lượng thực tế.
- Rỗng: icon mờ + text "Chưa có hình ảnh nào được chia sẻ trong đoạn
  chat này" (tab Ảnh) / "Chưa có tài liệu nào được chia sẻ trong đoạn
  chat này" (tab Tệp).
- Bấm ảnh → lightbox phóng to toàn màn hình (ảnh gốc, nút đóng).
- Bấm tệp → tải xuống ngay (dùng lại URL file có sẵn, đã tự động
  `Content-Disposition: attachment`).
- Loại trừ tin đã THU HỒI hoặc đã bị NGƯỜI ĐANG XEM ẩn cục bộ — nhất
  quán tuyệt đối với quy tắc đã có ở GĐ6b.
- Tải 1 lần toàn bộ danh sách khi mở modal (không phân trang) — số
  lượng ảnh/tệp trong 1 cuộc trò chuyện thực tế không lớn tới mức cần
  phân trang riêng.

**Ngoài phạm vi:**
- Không đổi/khôi phục lại mục "Ảnh/Video" ở trang Thông tin nhóm.
- Không có chức năng xóa/tải hàng loạt từ modal.
- Không cache lại danh sách giữa các lần mở — mở lại modal là gọi lại
  API (đơn giản, dữ liệu luôn mới).

## 3. Backend

### 3.1. Repository

`ITinNhanRepository` thêm:
```csharp
/// <summary>Toàn bộ tin Anh/File (2 chiều) giữa 2 người dùng, mới nhất trước.</summary>
Task<List<TinNhan>> LayMediaTheoNguoiDungAsync(string nguoiA, string nguoiB);

/// <summary>Toàn bộ tin Anh/File của 1 nhóm, mới nhất trước.</summary>
Task<List<TinNhan>> LayMediaTheoNhomAsync(string nhomId);
```
Filter: `LoaiTinNhan == Anh || LoaiTinNhan == File`, sort theo
`ThoiGianTao` giảm dần. Implement ở `TinNhanRepository` (Mongo
`Builders<TinNhan>.Filter.In`/`.Or`) và fake `TinNhanGiaLap` (LINQ
tương đương) — theo đúng convention đã có cho các cặp
repo-thật/repo-giả trong dự án.

### 3.2. Service

`IDichVuTinNhan` thêm:
```csharp
Task<List<TinNhanDto>> LayMediaTheoNguoiDungAsync(string idHienTai, string doiTacId);
Task<List<TinNhanDto>> LayMediaTheoNhomAsync(string idHienTai, string nhomId);
```
Cả 2 lọc bỏ tin đã thu hồi (đơn giản: `.Where(t => !t.DaThuHoi)` — tin
đã thu hồi không còn nội dung/file để hiện trong kho, khác với
`AnhXaDto`'s placeholder dùng cho lịch sử chat) VÀ tin đã bị `idHienTai`
ẩn cục bộ (dùng lại `ITinNhanAnRepository.LayDanhSachIdDaAnAsync`, cùng
pattern `LayLichSuAsync`). `LayMediaTheoNhomAsync` kiểm tra thành viên
nhóm giống `LayTinDaGhimTheoNhomAsync` (ném `NhomKhongTonTaiException`/
`KhongPhaiThanhVienNhomException`).

### 3.3. Controller

Trong `TinNhanController`:
```
GET /api/tinnhan/nguoi-dung/{id}/media
GET /api/tinnhan/nhom/{id}/media
```
Cùng pattern `[Authorize]` (kế thừa), validate `ObjectId.TryParse`,
401/400/403/404 giống các endpoint `/ghim` đã có ở GĐ6b.

## 4. Frontend

### 4.1. Kiểu dữ liệu + API

`DichVuApi.ts` thêm:
```typescript
export async function LayMediaTheoNguoiDung(token: string, doiTacId: string): Promise<TinNhan[]> {
  return goiApi<TinNhan[]>(`/tinnhan/nguoi-dung/${doiTacId}/media`, { headers: { Authorization: `Bearer ${token}` } });
}
export async function LayMediaTheoNhom(token: string, nhomId: string): Promise<TinNhan[]> {
  return goiApi<TinNhan[]>(`/tinnhan/nhom/${nhomId}/media`, { headers: { Authorization: `Bearer ${token}` } });
}
```

### 4.2. Component mới `PanelKhoMedia.tsx` + `.css`

Nhận props: `danhSachMedia: TinNhan[]`, `tenCuocTroChuyen: string`,
`onDong: () => void`. Nội bộ quản lý `tabDangChon: 'anh' | 'tep'` và
`anhDangXemToId: string | null` (lightbox). Lọc `danhSachMedia` theo
`loaiTinNhan` cho từng tab. Icon folder dùng SVG mới `BieuTuongKhoLuuTru`
trong `BieuTuong.tsx`; icon rỗng dùng SVG mới `BieuTuongAnhMo`.

Lightbox: overlay full-screen `position:fixed; inset:0` khi
`anhDangXemToId` khác null, hiện `<img>` gốc + nút × đóng, bấm ra ngoài
ảnh cũng đóng.

### 4.3. `KhungTinNhan.tsx`

Thêm prop `onMoKhoMedia: () => void` (component cha quản lý việc gọi
API + hiện `PanelKhoMedia`, giữ đúng nguyên tắc `KhungTinNhan` không tự
gọi API — nhất quán với cách các panel khác được điều phối từ
`TrangChat.tsx`/`TrangNhom.tsx`). Thêm icon folder trong header, cạnh
cụm tên (không nằm trong `.khung-tin-nhan__icon-noi` — đây là nút cố
định luôn hiện, không phải icon nổi khi hover tin nhắn).

### 4.4. `TrangChat.tsx` / `TrangNhom.tsx`

State `hienKhoMedia: boolean` + `danhSachMedia: TinNhan[]`. Hàm
`moKhoMedia()` gọi `LayMediaTheoNguoiDung`/`LayMediaTheoNhom` rồi set
state hiện panel. Truyền `onMoKhoMedia={moKhoMedia}` cho
`<KhungTinNhan>`, render `<PanelKhoMedia>` khi `hienKhoMedia`.

## 5. Kiểm thử

- Backend: `LayMediaTheoNguoiDungAsync_ChiTraVeTinAnhVaFile_LoaiTruText`,
  `LayMediaTheoNguoiDungAsync_LocTinDaThuHoiVaDaAn`,
  `LayMediaTheoNhomAsync_KhongPhaiThanhVien_NemNgoaiLe`, test controller
  cho 2 endpoint (200 kèm danh sách đúng, 403/404 khi sai quyền/không
  tồn tại).
- Frontend: `PanelKhoMedia.test.tsx` (hiện đúng số lượng theo tab, đổi
  tab, trạng thái rỗng đúng text từng tab, bấm ảnh mở lightbox, bấm tệp
  gọi đúng URL tải); test trong `TrangChat.test.tsx`/`TrangNhom.test.tsx`
  cho việc bấm icon folder gọi đúng API và hiện panel.
