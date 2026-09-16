# HaloChat — Tìm tin nhắn trong cuộc trò chuyện (GĐ7b)

## 1. Bối cảnh & mục tiêu

Thêm icon kính lúp trên header `KhungTinNhan` mở ô tìm kiếm dạng pill,
tìm tin nhắn TEXT trong ĐÚNG cuộc trò chuyện đang mở (1-1 hoặc nhóm),
tìm toàn bộ lịch sử qua backend (không giới hạn tin đã tải về máy).
Bấm 1 kết quả tự động tải thêm lịch sử cũ hơn nếu cần rồi cuộn tới đúng
tin, có hiệu ứng nổi bật ngắn.

## 2. Phạm vi

**Trong phạm vi:**
- Áp dụng cho cả chat 1-1 và chat nhóm.
- Icon kính lúp trên header, cạnh icon folder (GĐ7a). Bấm mở/đóng 1
  thanh input pill "Tìm tin nhắn..." thay cho phần tên/tiêu đề (ẩn tạm
  thời khi đang tìm, hiện lại khi đóng ô tìm kiếm).
- Gõ ≥ 1 ký tự (debounce 300ms, tránh gọi API mỗi phím gõ) → gọi backend
  tìm kiếm, hiện danh sách kết quả dạng dropdown dưới ô input: mỗi dòng
  gồm tên người gửi + trích đoạn nội dung (đánh dấu từ khóa khớp nếu dễ
  làm, không bắt buộc) + giờ:phút.
- Chỉ tìm trong `NoiDungTinNhan` của tin loại Text — không tìm tên file.
- Loại trừ tin đã thu hồi/đã ẩn cục bộ (nhất quán GĐ6b).
- Bấm 1 kết quả:
  1. Nếu tin đó đã có trong `danhSachTinNhan` đang hiển thị (đã tải
     rồi) → cuộn thẳng tới, thêm class nổi bật (highlight) ~2 giây rồi
     tự tắt.
  2. Nếu CHƯA có (còn nằm sâu trong lịch sử chưa tải) → gọi lặp lại
     hàm tải-thêm-lịch-sử hiện có (`taiThemLichSuCu`/tương đương) cho
     tới khi tin đó xuất hiện trong danh sách đã tải HOẶC lịch sử đã
     tải hết (không còn tin cũ hơn) — tối đa 20 lần lặp để tránh vòng
     lặp vô hạn nếu có lỗi logic; nếu không tìm thấy sau khi hết lịch
     sử, hiện thông báo ngắn "Không tìm thấy tin nhắn này trong lịch
     sử." thay vì treo mãi.
  3. Đóng ô tìm kiếm sau khi bấm kết quả.

**Ngoài phạm vi:**
- Không tìm trong tên file Ảnh/File (đã chốt).
- Không tìm xuyên nhiều cuộc trò chuyện cùng lúc — chỉ trong cuộc đang
  mở.
- Không lưu lịch sử tìm kiếm/gợi ý tìm kiếm gần đây.
- Không hỗ trợ cú pháp tìm nâng cao (theo người gửi, theo ngày...).

## 3. Backend

### 3.1. Repository

`ITinNhanRepository` thêm:
```csharp
/// <summary>Tin Text (2 chiều) giữa 2 người dùng có NoiDungTinNhan chứa tuKhoa (không phân biệt hoa/thường), mới nhất trước.</summary>
Task<List<TinNhan>> TimKiemTheoNguoiDungAsync(string nguoiA, string nguoiB, string tuKhoa);

/// <summary>Tin Text của 1 nhóm có NoiDungTinNhan chứa tuKhoa, mới nhất trước.</summary>
Task<List<TinNhan>> TimKiemTheoNhomAsync(string nhomId, string tuKhoa);
```
Mongo thật dùng `Builders<TinNhan>.Filter.Regex(t => t.NoiDungTinNhan, new BsonRegularExpression(Regex.Escape(tuKhoa), "i"))` kết hợp filter `LoaiTinNhan == Text` và filter cặp người dùng/nhóm — `Regex.Escape` bắt buộc để tránh tuKhoa chứa ký tự regex đặc biệt gây lỗi hoặc hành vi ngoài ý muốn. Giới hạn kết quả trả về tối đa 50 tin (tránh trả về cực nhiều nếu từ khóa quá phổ biến) — `Limit(50)`. Fake `TinNhanGiaLap` dùng
`.Contains(tuKhoa, StringComparison.OrdinalIgnoreCase)` + `.Take(50)`.

### 3.2. Service

```csharp
Task<List<TinNhanDto>> TimKiemTheoNguoiDungAsync(string idHienTai, string doiTacId, string tuKhoa);
Task<List<TinNhanDto>> TimKiemTheoNhomAsync(string idHienTai, string nhomId, string tuKhoa);
```
Lọc bỏ tin đã ẩn cục bộ theo `idHienTai` (tin đã thu hồi tự động không
khớp vì nội dung gốc dùng để so khớp, nhưng NẾU đã lưu tuKhoa khớp
TRƯỚC KHI thu hồi thì message đã bị đổi field `NoiDungTinNhan`... —
KHÔNG, nhắc lại: thu hồi KHÔNG xóa `NoiDungTinNhan` trong Mongo, chỉ
`AnhXaDto` ở tầng đọc mới thay bằng placeholder — nghĩa là 1 tin đã thu
hồi VẪN CÓ THỂ khớp từ khóa tìm kiếm ở tầng repository vì nó query trực
tiếp field gốc trong Mongo. Do đó BẮT BUỘC filter thêm `!t.DaThuHoi`
ngay trong câu query Mongo (không chỉ dựa vào AnhXaDto) để tin đã thu
hồi không lọt vào kết quả tìm kiếm dưới bất kỳ hình thức nào — thêm
điều kiện này vào cả 2 hàm repository ở §3.1.
`TimKiemTheoNhomAsync` kiểm tra thành viên nhóm giống các hàm khác.
Nếu `tuKhoa` rỗng/toàn khoảng trắng sau trim, trả về danh sách rỗng
ngay (không query Mongo).

### 3.3. Controller

```
GET /api/tinnhan/nguoi-dung/{id}/tim-kiem?tuKhoa=...
GET /api/tinnhan/nhom/{id}/tim-kiem?tuKhoa=...
```
Cùng pattern auth/validate như các endpoint khác trong file.

## 4. Frontend

### 4.1. API

```typescript
export async function TimKiemTinNhanTheoNguoiDung(token: string, doiTacId: string, tuKhoa: string): Promise<TinNhan[]> {
  return goiApi<TinNhan[]>(`/tinnhan/nguoi-dung/${doiTacId}/tim-kiem?${new URLSearchParams({ tuKhoa })}`, { headers: { Authorization: `Bearer ${token}` } });
}
export async function TimKiemTinNhanTheoNhom(token: string, nhomId: string, tuKhoa: string): Promise<TinNhan[]> {
  return goiApi<TinNhan[]>(`/tinnhan/nhom/${nhomId}/tim-kiem?${new URLSearchParams({ tuKhoa })}`, { headers: { Authorization: `Bearer ${token}` } });
}
```

### 4.2. `KhungTinNhan.tsx`

- Thêm prop `onTimKiem: (tuKhoa: string) => Promise<TinNhan[]>` (component cha gọi đúng API 1-1/nhóm), `onNhayToiTinNhan: (id: string) => Promise<boolean>` (component cha lặp tải thêm lịch sử, trả `true` nếu tìm thấy/đã có sẵn, `false` nếu tải hết mà không thấy).
- State nội bộ: `hienOTimKiem: boolean`, `tuKhoaTim: string`, `ketQuaTim: TinNhan[]`, `dangTim: boolean`, `loiTim: string | null`.
- Icon kính lúp trong header bật `hienOTimKiem`; khi bật, thay cụm tên bằng `<input>` pill + debounce 300ms gọi `onTimKiem`.
- Dropdown kết quả dưới input; mỗi dòng bấm gọi `onNhayToiTinNhan(id)` rồi nếu `true`, gọi hàm cuộn nội bộ (dùng `Map<string, HTMLDivElement>` ref lưu theo `tn.id` cho từng bong bóng tin nhắn, `scrollIntoView({block:'center'})` + set state `idDangNoiBat` trong 2 giây rồi tự xóa qua `setTimeout`), rồi đóng ô tìm kiếm; nếu `false`, hiện `loiTim`.
- Mỗi bong bóng tin nhắn thêm class `khung-tin-nhan__bong--noi-bat` khi `tn.id === idDangNoiBat` (viền/nền nhấn mạnh tạm thời, CSS transition mờ dần).

### 4.3. `TrangChat.tsx` / `TrangNhom.tsx`

- `timKiemTinNhan(tuKhoa)`: gọi `TimKiemTinNhanTheoNguoiDung`/`TheoNhom` với id cuộc trò chuyện đang mở.
- `nhayToiTinNhan(id)`: nếu id đã có trong `tinNhanTheoNguoiDung[key]`/`tinNhanTheoNhom[key]` → trả `true` ngay. Nếu chưa, lặp gọi `LayLichSuTinNhan`/`LayLichSuNhom` với `truoc` = id cũ nhất hiện có (giống `taiThemLichSuCu`), gộp vào state, kiểm tra lại — lặp tối đa 20 lần hoặc tới khi trang trả về rỗng/ít hơn số lượng yêu cầu (hết lịch sử) thì dừng, trả `false`.
- Truyền `onTimKiem`/`onNhayToiTinNhan` cho `<KhungTinNhan>`.

## 5. Kiểm thử

- Backend: `TimKiemTheoNguoiDungAsync_KhopKhongPhanBietHoaThuong`,
  `TimKiemTheoNguoiDungAsync_LoaiTruTinDaThuHoi`,
  `TimKiemTheoNguoiDungAsync_LoaiTruTinDaAn`,
  `TimKiemTheoNguoiDungAsync_TuKhoaRong_TraVeRong`,
  `TimKiemTheoNhomAsync_KhongPhaiThanhVien_NemNgoaiLe`; test controller
  cho 2 endpoint.
- Frontend: `KhungTinNhan.test.tsx` thêm test cho việc gõ từ khóa gọi
  đúng debounce/API, bấm kết quả đã có sẵn trong danh sách cuộn+nổi
  bật đúng, bấm kết quả CHƯA có gọi `onNhayToiTinNhan` và xử lý đúng
  cả 2 nhánh true/false; test trong `TrangChat.test.tsx`/`TrangNhom.test.tsx`
  cho logic lặp tải lịch sử tới khi tìm thấy hoặc hết lịch sử.
