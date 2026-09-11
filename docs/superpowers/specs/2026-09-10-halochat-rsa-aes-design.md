# HaloChat — Ứng dụng chat an toàn dùng mô hình mã hóa lai RSA-AES

Ngày viết: 2026-09-10
Nguồn yêu cầu: `Xây dựng ứng dụng chat an toàn sử dụng mô hình mã hóa lai RSA.docx`
Trạng thái: đã được duyệt trong phiên brainstorming, sẵn sàng chuyển sang lập kế hoạch triển khai.

## 1. Bối cảnh

Dự án cũ trong repo (`HaloChat.Web` — Razor Pages + SQL Server + mã hóa AES đơn) đã bị xóa khỏi
thư mục làm việc và không còn khớp với tài liệu yêu cầu mới. Repo sẽ được xây lại từ đầu theo đúng
tài liệu: React + TypeScript / ASP.NET Core Web API / MongoDB / SignalR / mô hình mã hóa lai
RSA-AES (RSA-OAEP mã hóa khóa phiên AES-256, AES-256-GCM mã hóa nội dung).

Đây là dự án môn học: phần **viết mã hóa thật (AES-256-GCM, RSA-OAEP áp dụng vào luồng chat)** sẽ
do nhóm tự thực hiện ở giai đoạn sau. Vai trò của bản triển khai này là dựng toàn bộ phần còn lại
(nền tảng, xác thực, danh sách người dùng, chat realtime, gửi ảnh/file, lưu lịch sử) chạy được đầy
đủ với nội dung **plaintext**, và để lại đúng chỗ, đúng chú thích cho phần mã hóa được cắm vào sau
mà không cần đổi kiến trúc.

## 2. Kiến trúc tổng thể

```
React + TypeScript (Vite)  ──HTTP + SignalR──>  ASP.NET Core Web API  ──>  MongoDB
Login/Register/DanhSach/Chat                     Auth(JWT)/NguoiDung/TinNhan/Hub        Users/Messages
AES-256-GCM / RSA-OAEP (GĐ6, do nhóm viết)        DichVuMaHoa (stub, GĐ6)
```

Mô hình mã hóa mục tiêu (áp dụng ở GĐ6, chưa bật trong các giai đoạn đầu):

```
Người gửi: Sinh AES-256 Session Key → AES-256-GCM mã hóa tin nhắn (Ciphertext + Nonce + AuthTag)
           → RSA-OAEP mã hóa Session Key bằng Public Key người nhận → gửi Server
Server:    lưu SenderId, ReceiverId, Ciphertext, EncryptedSessionKey, Nonce, AuthTag, CreatedAt
           (không cần đọc được plaintext)
Người nhận: RSA-OAEP giải mã Session Key bằng Private Key → AES-256-GCM giải mã → tin nhắn gốc
```

## 3. Cấu trúc repo & solution

Tách 2 thư mục gốc độc lập `backend/` và `frontend/` để đóng gói/triển khai riêng từng phần cho
khách (mỗi thư mục tự chứa đủ để build/deploy mà không phụ thuộc thư mục còn lại):

```
backend/
  HaloChat.sln
  HaloChat.Api/             # ASP.NET Core Web API + SignalR Hub, JWT, MongoDB driver
  HaloChat.Security/        # Class library: DichVuMaHoa (stub AES/RSA cho GĐ6), băm mật khẩu (thật)
frontend/
  (React + TypeScript, Vite)
docs/superpowers/specs|plans/...
```

Tách `HaloChat.Security` riêng khỏi `HaloChat.Api` để: (a) dễ unit test độc lập, (b) đánh dấu rõ
ràng đây là ranh giới nơi nhóm sẽ viết mã hóa thật, không lẫn vào logic API.

### Cấu hình kết nối & bí mật (secrets)

Chuỗi kết nối MongoDB (có username/password thật) **không bao giờ được commit vào Git**:

- Local dev: lưu bằng `dotnet user-secrets` cho project `HaloChat.Api` (bí mật nằm ngoài repo,
  riêng theo từng máy) — không dùng `appsettings.json` cho giá trị thật.
- Repo chỉ commit file mẫu (`appsettings.Development.json.example` hoặc tương tự) chứa placeholder,
  kèm hướng dẫn cách tự set secret cục bộ.
- `.gitignore` (khôi phục lại, đã bị xóa khỏi working tree) bổ sung loại trừ: file cấu hình chứa
  secret thật, `bin/`, `obj/`, `node_modules/`, `uploads/` (dữ liệu người dùng tải lên — không
  commit dữ liệu thật của người dùng vào source control).
- Khi bàn giao khách/triển khai thật, secret được cấu hình qua biến môi trường của môi trường host
  (không đổi cách này).

### Thương hiệu

Tên sản phẩm chính thức: **HaloChat** (khớp với `HaloChat.Api`/`HaloChat.Security` đã dùng xuyên
suốt code). Logo: bong bóng chat gradient xanh dương kèm vòng hào quang (halo) màu vàng cam, wordmark
"HaloChat" (chữ "Halo" xanh navy đậm, chữ "Chat" xanh dương). File gốc: `assets/halochat-logo.png`
(commit cùng Task 1 của plan frontend). Bảng màu chủ đạo cho giao diện: xanh dương gradient
(`#2F7BF6` → `#1A56C4`) làm màu chính, xanh navy đậm (`#101B33`) cho chữ, nền trang dùng gradient
xanh rất nhạt. Font chữ: "Be Vietnam Pro" (hỗ trợ đầy đủ dấu tiếng Việt, thiết kế riêng cho tiếng
Việt — phù hợp sản phẩm tiếng Việt).

### Mở rộng phạm vi GĐ5 (quyết định 2026-09-11)

Ban đầu GĐ5 chỉ gồm chat 1-1 + gửi ảnh/file (xem §7 tài liệu gốc). Đã quyết định **mở rộng thêm**:

- **Hệ thống kết bạn**: gửi lời mời kết bạn, chấp nhận/từ chối, danh sách bạn bè.
- **Nhóm chat**: tạo nhóm, chat nhiều người trong 1 nhóm.
- Bảng thông báo (lời mời kết bạn mới, tin nhắn mới...) hiển thị dạng dropdown/toast, không phải
  trang riêng.

Bố cục tham chiếu (2 ảnh giao diện mẫu chia sẻ trong hội thoại, không lưu file): sidebar trái gồm
Tin nhắn/Bạn bè/Nhóm/Cài đặt, khung chat giữa, panel thông tin liên hệ bên phải, có bản mobile
riêng. Plan Frontend nền tảng (GĐ4) chỉ dùng ảnh mẫu đăng nhập/đăng ký (thẻ trắng bo tròn, tab
chuyển đổi, icon trong ô nhập) — không đụng tới kết bạn/nhóm.

Thiết kế chi tiết (mô hình dữ liệu, API, luồng SignalR, quyền hạn) cho phần mở rộng này nằm ở
**§10 Thiết kế chi tiết GĐ5** bên dưới (quyết định 2026-09-11, buổi brainstorm thứ hai).

## 4. Mô hình dữ liệu (MongoDB)

**Collection `NguoiDung`**

| Field | Kiểu | Ghi chú |
|---|---|---|
| `_id` | ObjectId | |
| `TenTaiKhoan` | string | unique index, dùng để tìm kiếm/đăng nhập |
| `Email` | string | unique index, dùng xác thực + khôi phục mật khẩu |
| `MatKhauBam` | string | SHA-256(Password + Salt) — thật, code từ đầu |
| `Salt` | string | ngẫu nhiên theo user, thật, code từ đầu |
| `KhoaCongKhai` | string | RSA Public Key (PEM/Base64) |
| `KhoaBiMat` | string | RSA Private Key — sinh cùng lúc đăng ký (xem lưu ý §9) |
| `NgayTao` | DateTime | |
| `ChoPhepTinNhanTuNguoiLa` | bool, default `false` | thêm ở GĐ5 (§10) — bật trong trang Cài đặt, cho phép người **không phải bạn bè** nhắn tin 1-1 |

Trường phục vụ quên mật khẩu (`MaOtp`, `ThoiHanOtp`, ...) được thêm khi làm module GĐ7, không
thêm trước để tránh field thừa không dùng.

**Collection `TinNhan`**

| Field | Kiểu | Ghi chú |
|---|---|---|
| `_id` | ObjectId | |
| `NguoiGuiId` | ObjectId | |
| `NguoiNhanId` | ObjectId? | tin nhắn 1-1 — null nếu là tin nhắn nhóm (xem `NhomId`) |
| `NhomId` | ObjectId? | thêm ở GĐ5 (§10) — tin nhắn nhóm; null nếu là tin nhắn 1-1. **Đúng một trong hai field `NguoiNhanId`/`NhomId` có giá trị, không bao giờ cả hai hoặc không cái nào** |
| `LoaiTinNhan` | enum: `Text` \| `Anh` \| `File` | |
| `NoiDungTinNhan` | string | plaintext ở GĐ3-5; ở GĐ6 nhóm chuyển sang lưu ciphertext |
| `CiphertextTinNhan`, `KhoaPhienDaMaHoa`, `Nonce`, `AuthTag` | string, để trống ở GĐ3-5 | dự phòng cho GĐ6, không dùng tới trước đó |
| `DuongDanFile` | string? | chỉ có khi `LoaiTinNhan != Text` |
| `TenFileGoc` | string? | tên file gốc người dùng upload |
| `KichThuocFile` | long? | bytes |
| `LoaiFile` | string? | MIME type |
| `DaDoc` | bool, default `false` | thêm ở GĐ5 (§10) — phục vụ badge "chưa đọc" trong dropdown thông báo |
| `ThoiGianTao` | DateTime | |

## 5. Trình tự triển khai theo giai đoạn

Bám sát đúng thứ tự giai đoạn trong tài liệu; mỗi giai đoạn có mốc kiểm tra rõ ràng trước khi
sang giai đoạn kế:

- **GĐ1-2 (Phân tích/Thiết kế)** — tài liệu này.
- **Scaffold** — `HaloChat.Api` chạy `dotnet run` (Swagger mở được), `halochat-web` chạy
  `npm run dev` (trang trắng load được), kết nối MongoDB thành công. Xác nhận cả hai chạy trước
  khi làm tính năng.
- **GĐ3 (Backend nền tảng)** — MongoDB models, `DangKyTaiKhoan()`, `DangNhap()` (băm mật khẩu thật,
  JWT thật), quản lý người dùng cơ bản (danh sách người dùng).
- **GĐ4 (Frontend)** — trang đăng ký/đăng nhập, danh sách người dùng, khung giao diện chat
  (React + TypeScript, Vite).
- **GĐ5a (Chat realtime lõi)** — SignalR `GuiTinNhan()`/`NhanTinNhan()`, lưu lịch sử vào
  `TinNhan` dạng **plaintext**; upload ảnh/file qua endpoint REST riêng (không qua SignalR), lưu
  đĩa + metadata Mongo (chi tiết §9, §10). **Mốc kiểm tra:** gửi thử tin nhắn + ảnh/file, mở MongoDB
  xác nhận dữ liệu được lưu thật.
- **GĐ5b (Mở rộng xã hội)** — kết bạn, nhóm chat, thông báo real-time, xây trên nền Hub của GĐ5a
  (chi tiết §10). **Mốc kiểm tra:** gửi lời mời kết bạn → chấp nhận → nhắn tin được; tạo nhóm →
  nhắn tin nhóm realtime; dropdown thông báo cập nhật không cần F5.
- **GĐ6 (Bảo mật — nhóm tự viết)** — `DichVuMaHoa.MaHoaTinNhan()/GiaiMaTinNhan()` (AES-256-GCM) và
  mã hóa khóa phiên (RSA-OAEP) hiện chỉ là stub có chú thích `[BẢO MẬT - GĐ6]`, **chưa được gọi**
  trong luồng gửi/nhận. Khi nhóm viết xong, chỉ cần nối lệnh gọi vào Hub/Controller để chuyển từ
  lưu plaintext sang lưu ciphertext — không cần đổi schema hay kiến trúc.
- **GĐ7 (Quên mật khẩu)** — module riêng, làm sau khi chat chính hoàn thiện: Email + OTP.
- **GĐ8 (Kiểm thử)** — test API bằng Swagger, test đăng ký/đăng nhập, gửi/nhận tin nhắn, ảnh/file,
  mã hóa/giải mã (sau khi GĐ6 xong), OTP, các trường hợp lỗi.
- **GĐ9 (Hoàn thiện)** — giao diện, bảo mật, tối ưu, viết báo cáo, chuẩn bị demo.

## 6. Xác thực

Đăng ký: `TenTaiKhoan` + `Email` + `Password` → sinh `Salt` ngẫu nhiên → `SHA-256(Password + Salt)`
→ lưu MongoDB. Đăng nhập: `TenTaiKhoan` hoặc `Email` + `Password` → so khớp `MatKhauBam` → phát
JWT. Cả hai đều code thật từ GĐ3 (không thuộc diện stub).

## 7. Gửi ảnh/file

- Upload qua **endpoint REST riêng** (`multipart/form-data`), không qua SignalR — giữ kênh chat
  text luôn nhẹ và tức thời bất kể ảnh/file đang được tải.
- Lưu file **trên đĩa** của `HaloChat.Api` (thư mục `uploads/`), MongoDB chỉ lưu metadata. Lý do:
  nhúng file vào document MongoDB sẽ đụng giới hạn cứng 16MB/document và làm phình database.
- Giới hạn kích thước: ảnh tối đa **5MB**, file khác tối đa **20MB** — kiểm tra ở **cả client và
  server** (server là bắt buộc, dùng Kestrel `MaxRequestBodySize` + `RequestFormLimits`, không chỉ
  dựa vào client).
- Loại file cho phép: ảnh (jpg/png/gif/webp) + tài liệu phổ biến (pdf/docx/xlsx/zip); chặn file
  thực thi (.exe/.bat/.sh/.msi...).
- Sau khi lưu xong, gửi một thông báo nhỏ (metadata) qua SignalR cho người nhận — kích thước gói
  tin tương đương một tin nhắn text, không đẩy dữ liệu nhị phân qua WebSocket.
- Làm cùng GĐ5, plaintext (chưa mã hóa nội dung file); mã hóa file (nếu nhóm muốn mở rộng) tính ở
  GĐ6 cùng đợt với mã hóa text.

## 8. Hiệu năng & khả năng chịu tải khi nhiều người gửi file cùng lúc

Ghi lại để đưa vào phần "Đánh giá mức độ an toàn và khả năng triển khai" của báo cáo:

- **Bắt buộc khi code:** upload phải **stream thẳng ra đĩa**, không đọc hết file vào bộ nhớ dạng
  byte array trước khi lưu — nếu buffer vào RAM, nhiều người upload file lớn cùng lúc sẽ làm RAM
  tăng theo số người thay vì gần như không đổi.
- Với quy mô đồ án (vài chục người dùng cùng lúc), Kestrel xử lý bất đồng bộ đủ sức chịu tải mà
  không cần thêm hạ tầng (queue/CDN/storage riêng). Kênh chat text (SignalR) không bị ảnh hưởng bởi
  upload vì đi trên kết nối/luồng xử lý riêng.
- Băng thông mạng và tốc độ đĩa của máy chạy server là giới hạn thực sự khi số người upload đồng
  thời tăng cao (vd. 100 người cùng lúc) — không phải lỗi kiến trúc, chỉ là chia sẻ tài nguyên vật
  lý, thời gian upload mỗi người sẽ dài hơn tương ứng.
- **Cải tiến tùy chọn** (không bắt buộc, có thể nêu là hướng mở rộng trong báo cáo): giới hạn số
  upload đồng thời tối đa bằng một semaphore đơn giản trong `HaloChat.Api`, để vượt ngưỡng thì
  request phải chờ tới lượt thay vì tất cả cùng chậm lại.

## 9. Chính sách stub bảo mật (GĐ6, nhóm tự viết)

Áp dụng đúng ví dụ trong tài liệu — method rỗng, có chú thích mô tả việc cần làm:

```csharp
public class DichVuMaHoa
{
    public string MaHoaTinNhan(string noiDungTinNhan)
    {
        // [BẢO MẬT - GĐ6]
        // Mã hóa nội dung bằng AES-256-GCM: sinh Nonce, trả về Ciphertext + AuthTag.
        return "";
    }

    public string GiaiMaTinNhan(string tinNhanDaMaHoa)
    {
        // [BẢO MẬT - GĐ6]
        // Giải mã nội dung bằng AES-256-GCM dùng Nonce + AuthTag đã lưu kèm.
        return "";
    }

    // [BẢO MẬT - GĐ6]
    // Mã hóa AES Session Key bằng RSA-OAEP với Public Key người nhận trước khi gửi Server.

    // [BẢO MẬT - GĐ6]
    // Giải mã AES Session Key bằng RSA Private Key của người nhận.
}
```

Các method này **không được gọi** trong luồng gửi/nhận ở GĐ3-5 (xem §5) — chat vẫn chạy đầy đủ với
`NoiDungTinNhan` plaintext cho tới khi nhóm hoàn thành GĐ6 và nối lệnh gọi vào Hub/Controller.

**Lưu ý về mô hình khóa:** tài liệu yêu cầu lưu cả `KhoaCongKhai` và `KhoaBiMat` trong document
`NguoiDung` trên MongoDB. Đây là đơn giản hóa hợp lý cho phạm vi đồ án (không phải E2EE thực thụ,
vì server có khả năng truy cập Private Key) — theo đúng thiết kế trong tài liệu, không tự ý đổi
sang mô hình lưu khóa phía client. Điểm này nên được nêu rõ như một giới hạn đã biết khi viết phần
"Đánh giá mức độ an toàn" trong báo cáo.

## 10. Thiết kế chi tiết GĐ5 (SignalR, kết bạn, nhóm chat, thông báo)

Quyết định 2026-09-11 (buổi brainstorm thứ hai). Áp dụng cho cả GĐ5a và GĐ5b (§5).

### 10.1. Ruling — nhắn tin với người lạ

Mặc định **phải là bạn bè** mới nhắn tin 1-1 được. Ngoại lệ: mỗi người dùng có 1 cờ cài đặt
`ChoPhepTinNhanTuNguoiLa` (§4, mặc định `false`) — khi bật, người khác nhắn tin 1-1 cho họ được dù
chưa kết bạn. Gửi tin nhắn cho người lạ **không** tự động tạo quan hệ bạn bè; kết bạn vẫn là hành
động riêng biệt qua `LoiMoiKetBan`.

### 10.2. Mô hình dữ liệu bổ sung

**Collection `LoiMoiKetBan`**

| Field | Kiểu | Ghi chú |
|---|---|---|
| `_id` | ObjectId | |
| `NguoiGuiId` | ObjectId | |
| `NguoiNhanId` | ObjectId | |
| `TrangThai` | enum: `ChoDuyet` \| `DaChapNhan` \| `DaTuChoi` | |
| `ThoiGianTao` | DateTime | |

Danh sách bạn bè = truy vấn `LoiMoiKetBan` với `TrangThai = DaChapNhan` liên quan tới user (không
tạo collection `BanBe` riêng — tránh 2 nguồn sự thật lệch nhau khi 1 người bị xóa/chặn sau này).
Unique index (`NguoiGuiId`, `NguoiNhanId`) theo cặp không thứ tự (kiểm tra ở tầng service khi tạo
lời mời, vì Mongo compound index không tự chuẩn hóa thứ tự cặp) để chặn gửi trùng lời mời.

**Collection `Nhom`**

| Field | Kiểu | Ghi chú |
|---|---|---|
| `_id` | ObjectId | |
| `TenNhom` | string | |
| `NguoiTaoId` | ObjectId | admin duy nhất — chỉ người này thêm/xóa thành viên, đổi tên, giải tán nhóm |
| `ThanhVienIds` | List\<ObjectId\> | nhúng thẳng trong document (quy mô đồ án nhỏ, không cần collection join riêng); luôn gồm cả `NguoiTaoId` |
| `ThoiGianTao` | DateTime | |

Thay đổi ở `NguoiDung` và `TinNhan` xem §4 (đã cập nhật: `ChoPhepTinNhanTuNguoiLa`, `NhomId`,
`DaDoc`).

### 10.3. SignalR Hub (`ChatHub`)

- **Auth qua WebSocket:** JWT truyền qua query string `?access_token=` (WebSocket không set được
  header `Authorization`) — cấu hình `JwtBearerEvents.OnMessageReceived` đọc token khi
  `path.StartsWithSegments("/hub/chat")`.
- **Định danh user cho `Clients.User(id)`:** viết `IUserIdProvider` tùy chỉnh đọc claim
  `JwtRegisteredClaimNames.Sub` — khớp đúng cách `DichVuJwt` đang phát hành token (không đổi claim
  hiện có, không dựa vào `ClaimTypes.NameIdentifier` mặc định của SignalR).
- **`OnConnectedAsync`:** load danh sách `Nhom` mà user thuộc về, `Groups.AddToGroupAsync(connectionId,
  "nhom-" + id)` cho từng nhóm — để nhận tin nhắn nhóm qua `Clients.Group(...)`.
- **Hub method (client gọi):**
  - `GuiTinNhan(nguoiNhanId?, nhomId?, loaiTinNhan, noiDung, duongDanFile?)` — kiểm tra quyền
    (bạn bè, HOẶC người nhận bật `ChoPhepTinNhanTuNguoiLa`, HOẶC là thành viên `ThanhVienIds` của
    nhóm) → lưu Mongo → gọi `Clients.User(nguoiNhanId)` hoặc `Clients.Group("nhom-"+nhomId)`
    `.NhanTinNhan(tinNhan)`. Từ chối (throw `HubException`) nếu không đủ điều kiện.
  - `DanhDauDaDoc(nguoiKiaId?, nhomId?)` — set `DaDoc = true` cho các tin nhắn liên quan tới hội
    thoại đó mà user hiện tại là người nhận.
- **Server đẩy xuống client** (gọi từ Controller qua `IHubContext<ChatHub>` khi sự kiện đến từ
  REST, không qua Hub method):
  - `NhanLoiMoiKetBan(loiMoi)` — khi có lời mời kết bạn mới.
  - `LoiMoiKetBanDuocChapNhan(nguoiDung)` — khi lời mời của mình được chấp nhận.
  - `DuocThemVaoNhom(nhom)` — khi bị/được thêm vào nhóm (đồng thời gọi
    `Groups.AddToGroupAsync` cho mọi connection hiện tại của user đó, nếu online).

### 10.4. API REST

- **Kết bạn:** `POST /api/ketban/loi-moi/{nguoiNhanId}`, `POST /api/ketban/{id}/chap-nhan`,
  `POST /api/ketban/{id}/tu-choi`, `GET /api/ketban/ban-be`, `GET /api/ketban/loi-moi-den`,
  `GET /api/ketban/loi-moi-gui`.
- **Nhóm:** `POST /api/nhom` (tên + danh sách thành viên ban đầu), `GET /api/nhom` (nhóm của
  user hiện tại), `GET /api/nhom/{id}`, `POST /api/nhom/{id}/thanh-vien` (chỉ admin),
  `DELETE /api/nhom/{id}/thanh-vien/{userId}` (chỉ admin), `POST /api/nhom/{id}/roi-nhom` (tự rời
  — admin rời thì nhóm giải tán, vì không có cơ chế chuyển quyền admin ở phạm vi đồ án này).
- **Lịch sử tin nhắn (phân trang, tải thêm khi cuộn lên):**
  `GET /api/tinnhan/nguoi-dung/{id}?truoc=&soLuong=30`,
  `GET /api/tinnhan/nhom/{id}?truoc=&soLuong=30` (`truoc` = id tin nhắn cũ nhất đã tải, để trống
  ở lần gọi đầu).
- **Upload file/ảnh:** `POST /api/tinnhan/upload` (multipart, đúng thiết kế §7 đã chốt) → trả về
  `DuongDanFile`/`TenFileGoc`/`KichThuocFile`/`LoaiFile`; client gọi `GuiTinNhan` qua Hub với các
  giá trị đó và `LoaiTinNhan = Anh | File`.
- **Cài đặt:** `PUT /api/nguoidung/cai-dat` (bật/tắt `ChoPhepTinNhanTuNguoiLa`).
- Toàn bộ endpoint trên (trừ trường hợp có ghi chú khác) yêu cầu JWT hợp lệ, theo đúng mẫu
  `[Authorize]` đã dùng ở `NguoiDungController`.

### 10.5. Frontend

- Layout mới `KhungChinh` (thay thế `TrangDanhSachNguoiDung` hiện tại làm trang chính sau đăng
  nhập): sidebar trái (Tin nhắn / Bạn bè / Nhóm / Cài đặt — đúng bố cục ảnh mẫu, §3 "Mở rộng phạm
  vi GĐ5"), khung chat giữa, panel thông tin liên hệ phải (ẩn trên mobile, có nút toggle riêng).
- `DichVuSignalR.ts` bọc thư viện `@microsoft/signalr`, kết nối 1 lần ngay sau đăng nhập trong
  `NguCanhChat.tsx` (context mới, tách khỏi `NguCanhXacThuc` để không phình trách nhiệm), tự
  reconnect khi rớt kết nối, ngắt kết nối khi đăng xuất.
- Dropdown thông báo (không phải trang riêng, theo đúng ghi chú §3 "Mở rộng phạm vi GĐ5"): gộp lời mời
  kết bạn đang chờ (`GET /api/ketban/loi-moi-den`) + hội thoại có tin nhắn `DaDoc=false`, cập nhật
  realtime qua các event Hub ở §10.3.
- Trang Cài đặt (`/cai-dat`): toggle `ChoPhepTinNhanTuNguoiLa`.

## 11. Quên mật khẩu (module riêng, làm sau)

Email → Server tạo OTP → gửi OTP qua Email → xác thực OTP → nhập mật khẩu mới → SHA-256 + Salt →
cập nhật `NguoiDung`. Không gửi mật khẩu cũ qua email. Thực hiện sau khi chat chính (GĐ3-6) hoàn
thành, theo đúng thứ tự trong tài liệu.

## 12. Kiểm thử

Swagger cho toàn bộ API. Test theo đúng danh sách tài liệu: đăng ký/đăng nhập, gửi/nhận tin nhắn,
gửi ảnh/file, kết bạn, nhóm chat, thông báo realtime, mã hóa/giải mã (sau GĐ6), OTP, các trường
hợp lỗi (sai mật khẩu, file quá khổ, loại file bị chặn, token hết hạn, gửi tin cho người lạ chưa
bật cho phép, thao tác nhóm không phải admin...).

## 13. Quy ước đặt tên

- Biến: camelCase, tiếng Việt không dấu (`tenTaiKhoan`, `email`, `matKhau`, `noiDungTinNhan`,
  `maOtp`, `khoaMaHoa`, `tinNhanDaMaHoa`, `choPhepTinNhanTuNguoiLa`, `thanhVienIds`).
- Hàm: PascalCase, tiếng Việt không dấu (`DangKyTaiKhoan()`, `DangNhap()`, `GuiTinNhan()`,
  `NhanTinNhan()`, `MaHoaTinNhan()`, `GiaiMaTinNhan()`, `KiemTraMatKhau()`, `GuiMaOtp()`,
  `DatLaiMatKhau()`, `GuiLoiMoiKetBan()`, `ChapNhanLoiMoiKetBan()`, `TuChoiLoiMoiKetBan()`,
  `TaoNhom()`, `ThemThanhVien()`, `XoaThanhVien()`, `RoiNhom()`, `DanhDauDaDoc()`).
- Class: PascalCase, tiếng Việt không dấu (`NguoiDung`, `TinNhan`, `LoiMoiKetBan`, `Nhom`,
  `PhienChat`, `DichVuMaHoa`, `DichVuNguoiDung`, `DichVuTinNhan`, `DichVuKetBan`, `DichVuNhom`,
  `ChatHub`).
- Giữ nguyên thuật ngữ kỹ thuật quen thuộc: JWT, SignalR, MongoDB, AES256, RSA, SHA256, OTP.
- Tên project/namespace (`HaloChat.Api`, `HaloChat.Security`) giữ tiếng Anh vì là tên sản phẩm,
  không thuộc phạm vi quy ước biến/hàm/class nghiệp vụ.

## 14. Ngoài phạm vi bản thiết kế này

- Cài đặt thật AES-256-GCM/RSA-OAEP vào luồng chat (GĐ6) — nhóm tự làm, bản này chỉ dựng chỗ trống
  và chú thích.
- Module quên mật khẩu (GĐ7) — thiết kế đã nêu ở §11, triển khai sau khi chat chính xong.
- Hạ tầng chịu tải nâng cao (CDN, hàng đợi, lưu trữ phân tán) — không cần cho phạm vi đồ án (§8).
- Chuyển quyền admin nhóm, chặn/report người dùng, trạng thái online/offline (presence), read-
  receipt gửi ngược cho người gửi — không nêu trong ảnh mẫu/yêu cầu, để ngoài phạm vi GĐ5b; có thể
  làm thêm ở GĐ9 nếu còn thời gian.
