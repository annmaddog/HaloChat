# HaloChat — Header nhóm bấm được + Thông tin nhóm + Quản lý nhóm (GĐ5e)

> Spec này chốt lại thiết kế đã thống nhất qua brainstorm cho phần header
> trang chat nhóm, thay thế panel "Thành viên" luôn hiện hiện tại.

## 1. Bối cảnh & mục tiêu

`TrangNhom.tsx` hiện có 1 panel "Thành viên" luôn hiển thị bên phải khung
chat (tên thành viên, nút xóa cho admin, dropdown thêm thành viên, nút Rời
nhóm/Giải tán nhóm). Header của khung chat (avatar + tên nhóm + số thành
viên, dùng chung component `KhungTinNhan`) hiện không bấm được.

Mục tiêu: bấm vào avatar/tên/số thành viên ở header mở ra 1 trang "Thông
tin nhóm" (giống Messenger/Zalo), thay thế hẳn panel "Thành viên" luôn
hiện. Các thao tác quản trị (thêm/xóa thành viên, đổi tên/ảnh) chuyển vào
1 màn hình con "Quản lý nhóm" riêng, chỉ admin vào được.

## 2. Phạm vi

**Trong phạm vi:**
- `KhungTinNhan.tsx`: thêm prop tùy chọn để header bấm được — CHỈ dùng ở
  chat nhóm (`TrangNhom.tsx`), không ảnh hưởng chat 1-1 (`TrangChat.tsx`).
- Xóa hẳn panel "Thành viên" luôn hiện hiện tại trong `TrangNhom.tsx`.
- `PanelThongTinNhom` (component mới): avatar lớn, tên nhóm, số thành
  viên, nút "Chỉnh sửa" (chỉ admin — mở `PanelQuanLyNhom`), nút "Nhắn tin"
  (đóng panel), hàng avatar 8 thành viên đầu + nút "•••" mở rộng xem hết
  (toggle tại chỗ, không cần màn hình riêng), "Quản lý nhóm" (chỉ admin —
  mở `PanelQuanLyNhom`), "Rời nhóm"/"Giải tán nhóm" (dùng lại logic đã
  có).
- `PanelQuanLyNhom` (component mới, chỉ admin vào được): form đổi tên +
  ảnh đại diện nhóm (dùng lại `TaiLenTep` + `CapNhatNhom` đã có sẵn, không
  cần backend mới), danh sách thành viên có nút xóa, dropdown thêm thành
  viên (dùng lại logic `themThanhVien`/`xoaThanhVien` đã có).
- Sửa nhanh: `.trang-nhom__nut-roi` (nút Rời nhóm/Giải tán nhóm) đang có
  nền `#fff` cứng, không đọc được ở giao diện tối — đổi sang
  `var(--mau-nen-the)`.

**Ngoài phạm vi (đã chốt khi brainstorm):**
- Không làm link tham gia nhóm.
- Không làm khu vực Ảnh/Video.
- Không thêm vai trò admin phụ — chỉ `NguoiTaoId` (đúng permission model hiện có).
- Không đổi backend cho việc đổi tên/ảnh nhóm — `CapNhatNhom` đã đủ dùng.

## 3. Backend

**Không cần thay đổi backend.** `CapNhatNhom`
(`PUT /api/nhom/{id}` → `duongDanAnhDaiDien`) và `TaiLenTep`
(`POST /api/tinnhan/upload`) đã đủ để đổi tên/ảnh nhóm; thêm/xóa thành
viên/rời nhóm đã có endpoint từ GĐ5b.

## 4. Frontend

### 4.1. `KhungTinNhan.tsx` — header bấm được (tùy chọn)

Thêm prop mới `onBamTieuDe?: () => void`. Khi có prop này, bọc avatar +
cụm tên/phụ đề trong 1 `<button>` bấm được; khi không có, giữ nguyên `<span>`
tĩnh như hiện tại (để `TrangChat.tsx` không đổi gì).

```tsx
interface PropsKhungTinNhan {
  // ... các prop hiện có giữ nguyên
  onBamTieuDe?: () => void;
}
```

Trong JSX phần `<header className="khung-tin-nhan__tieu-de">`, thay khối
avatar + cụm tên hiện tại:
```tsx
<span className="khung-tin-nhan__avatar">{tenHienThi.charAt(0).toUpperCase()}</span>
<div className="khung-tin-nhan__ten-cum">
  <span className="khung-tin-nhan__ten">{tenHienThi}</span>
  {phuDe && <span className="khung-tin-nhan__phu-de">{phuDe}</span>}
</div>
```
bằng:
```tsx
{onBamTieuDe ? (
  <button className="khung-tin-nhan__tieu-de-bam" onClick={onBamTieuDe}>
    <span className="khung-tin-nhan__avatar">{tenHienThi.charAt(0).toUpperCase()}</span>
    <div className="khung-tin-nhan__ten-cum">
      <span className="khung-tin-nhan__ten">{tenHienThi}</span>
      {phuDe && <span className="khung-tin-nhan__phu-de">{phuDe}</span>}
    </div>
  </button>
) : (
  <>
    <span className="khung-tin-nhan__avatar">{tenHienThi.charAt(0).toUpperCase()}</span>
    <div className="khung-tin-nhan__ten-cum">
      <span className="khung-tin-nhan__ten">{tenHienThi}</span>
      {phuDe && <span className="khung-tin-nhan__phu-de">{phuDe}</span>}
    </div>
  </>
)}
```
CSS mới `.khung-tin-nhan__tieu-de-bam`: reset style nút (border:none,
background:none, padding:0, cursor:pointer, display:flex, align-items:center,
gap giống layout cũ, text-align:left) để trông giống hệt bản tĩnh, chỉ khác
là bấm được.

### 4.2. `TrangNhom.tsx` — xóa panel cũ, thêm điều hướng panel mới

State mới: `panelDangMo: 'khong' | 'thong-tin' | 'quan-ly'` (thay hẳn
`<aside className="trang-nhom__thong-tin">` hiện có).

- Truyền `onBamTieuDe={() => setPanelDangMo('thong-tin')}` vào
  `<KhungTinNhan>`.
- Toàn bộ state/hàm quản trị đã có (`themThanhVien`, `xoaThanhVien`,
  `roiNhom`, `tatCaNguoiDung`, `laAdmin`) giữ nguyên, chỉ chuyển nơi hiển
  thị từ aside cũ sang 2 component panel mới.
- Render có điều kiện:
```tsx
{panelDangMo === 'thong-tin' && nhomDangChon && (
  <PanelThongTinNhom
    nhom={nhomDangChon}
    laAdmin={laAdmin}
    onDong={() => setPanelDangMo('khong')}
    onMoQuanLy={() => setPanelDangMo('quan-ly')}
    onRoiNhom={roiNhom}
  />
)}
{panelDangMo === 'quan-ly' && nhomDangChon && laAdmin && (
  <PanelQuanLyNhom
    nhom={nhomDangChon}
    tatCaNguoiDung={tatCaNguoiDung}
    onDong={() => setPanelDangMo('thong-tin')}
    onThemThanhVien={themThanhVien}
    onXoaThanhVien={xoaThanhVien}
    onCapNhatNhom={(nhomMoi) => setDanhSachNhom((truoc) => truoc.map((n) => (n.id === nhomMoi.id ? nhomMoi : n)))}
  />
)}
```
Khi đổi `nhomDangChonId` (chọn nhóm khác), reset `panelDangMo` về `'khong'`
(thêm vào effect/hàm chọn nhóm hiện có, hoặc 1 effect nhỏ theo dõi
`nhomDangChonId`).

### 4.3. `PanelThongTinNhom.tsx` (component mới)

```tsx
import { useState } from 'react';
import { Avatar } from '../ThanhPhan/Avatar';
import type { Nhom } from '../KieuDuLieu';
import './PanelThongTinNhom.css';

const SO_AVATAR_HIEN_MAC_DINH = 8;

interface PropsPanelThongTinNhom {
  nhom: Nhom;
  laAdmin: boolean;
  onDong: () => void;
  onMoQuanLy: () => void;
  onRoiNhom: () => void;
}

export function PanelThongTinNhom({ nhom, laAdmin, onDong, onMoQuanLy, onRoiNhom }: PropsPanelThongTinNhom) {
  const [hienHetThanhVien, setHienHetThanhVien] = useState(false);
  const thanhVienHien = hienHetThanhVien ? nhom.thanhVien : nhom.thanhVien.slice(0, SO_AVATAR_HIEN_MAC_DINH);
  const conAnDi = nhom.thanhVien.length - SO_AVATAR_HIEN_MAC_DINH;

  return (
    <aside className="panel-thong-tin-nhom">
      <button className="panel-thong-tin-nhom__dong" onClick={onDong} aria-label="Đóng">×</button>

      <Avatar id={nhom.id} ten={nhom.tenNhom} kichThuoc="lon" />
      <h3 className="panel-thong-tin-nhom__ten">{nhom.tenNhom}</h3>
      <p className="panel-thong-tin-nhom__so-thanh-vien">{nhom.thanhVien.length} thành viên</p>

      <div className="panel-thong-tin-nhom__hang-nut">
        {laAdmin && (
          <button className="nut-phu" onClick={onMoQuanLy}>Chỉnh sửa</button>
        )}
        <button className="nut-chinh" onClick={onDong}>Nhắn tin</button>
      </div>

      <div className="panel-thong-tin-nhom__phan">
        <h4>Thành viên</h4>
        <div className="panel-thong-tin-nhom__avatar-hang">
          {thanhVienHien.map((tv) => (
            <Avatar key={tv.id} id={tv.id} ten={tv.tenTaiKhoan} kichThuoc="nho" />
          ))}
          {!hienHetThanhVien && conAnDi > 0 && (
            <button className="panel-thong-tin-nhom__nut-them" onClick={() => setHienHetThanhVien(true)} aria-label="Xem tất cả thành viên">
              •••
            </button>
          )}
        </div>
      </div>

      {laAdmin && (
        <button className="panel-thong-tin-nhom__hang-lien-ket" onClick={onMoQuanLy}>
          Quản lý nhóm
        </button>
      )}

      <button className="trang-nhom__nut-roi" onClick={onRoiNhom}>
        {laAdmin ? 'Giải tán nhóm' : 'Rời nhóm'}
      </button>
    </aside>
  );
}
```

### 4.4. `PanelQuanLyNhom.tsx` (component mới, chỉ admin)

```tsx
import { useState } from 'react';
import { CapNhatNhom, TaiLenTep, LoiGoiApi } from '../DichVuApi';
import { useXacThuc } from '../NguCanh/NguCanhXacThuc';
import { Avatar } from '../ThanhPhan/Avatar';
import type { Nhom, NguoiDungTomTat } from '../KieuDuLieu';
import './PanelQuanLyNhom.css';

const GIOI_HAN_ANH_BYTES = 5 * 1024 * 1024;

interface PropsPanelQuanLyNhom {
  nhom: Nhom;
  tatCaNguoiDung: NguoiDungTomTat[];
  onDong: () => void;
  onThemThanhVien: (userId: string) => void;
  onXoaThanhVien: (userId: string) => void;
  onCapNhatNhom: (nhomMoi: Nhom) => void;
}

export function PanelQuanLyNhom({
  nhom, tatCaNguoiDung, onDong, onThemThanhVien, onXoaThanhVien, onCapNhatNhom,
}: PropsPanelQuanLyNhom) {
  const { token } = useXacThuc();
  const [tenNhom, setTenNhom] = useState(nhom.tenNhom);
  const [dangLuu, setDangLuu] = useState(false);
  const [dangTaiAnh, setDangTaiAnh] = useState(false);
  const [loi, setLoi] = useState<string | null>(null);

  function luuTen() {
    if (!token || !tenNhom.trim() || tenNhom === nhom.tenNhom) return;
    setDangLuu(true);
    CapNhatNhom(token, nhom.id, tenNhom.trim(), nhom.moTa, nhom.duongDanAnhDaiDien)
      .then(onCapNhatNhom)
      .catch((loiBat) => setLoi(loiBat instanceof LoiGoiApi ? loiBat.message : 'Đổi tên nhóm thất bại.'))
      .finally(() => setDangLuu(false));
  }

  function doiAnh(tep: File) {
    if (!token) return;
    if (!tep.type.startsWith('image/')) {
      setLoi('Chỉ chấp nhận file ảnh.');
      return;
    }
    if (tep.size > GIOI_HAN_ANH_BYTES) {
      setLoi(`Ảnh vượt quá giới hạn ${GIOI_HAN_ANH_BYTES / 1024 / 1024}MB.`);
      return;
    }
    setDangTaiAnh(true);
    TaiLenTep(token, tep)
      .then((daTaiLen) => CapNhatNhom(token, nhom.id, nhom.tenNhom, nhom.moTa, daTaiLen.duongDanFile))
      .then(onCapNhatNhom)
      .catch(() => setLoi('Đổi ảnh đại diện thất bại.'))
      .finally(() => setDangTaiAnh(false));
  }

  return (
    <aside className="panel-quan-ly-nhom">
      <button className="panel-quan-ly-nhom__dong" onClick={onDong} aria-label="Quay lại">←</button>
      <h3>Quản lý nhóm</h3>

      {loi && <p className="thong-bao-loi" role="alert">{loi}</p>}

      <div className="panel-quan-ly-nhom__doi-anh">
        <Avatar id={nhom.id} ten={nhom.tenNhom} kichThuoc="lon" />
        <label className="nut-phu panel-quan-ly-nhom__nut-doi-anh">
          {dangTaiAnh ? 'Đang tải...' : 'Đổi ảnh đại diện'}
          <input
            type="file"
            accept="image/jpeg,image/png,image/gif,image/webp"
            hidden
            disabled={dangTaiAnh}
            onChange={(su) => {
              const tep = su.target.files?.[0];
              if (tep) doiAnh(tep);
              su.target.value = '';
            }}
          />
        </label>
      </div>

      <label className="panel-quan-ly-nhom__nhan">
        Tên nhóm
        <div className="panel-quan-ly-nhom__hang-ten">
          <input type="text" value={tenNhom} onChange={(su) => setTenNhom(su.target.value)} disabled={dangLuu} />
          <button className="nut-chinh" onClick={luuTen} disabled={dangLuu || !tenNhom.trim() || tenNhom === nhom.tenNhom}>
            Lưu
          </button>
        </div>
      </label>

      <div className="panel-quan-ly-nhom__phan">
        <h4>Thành viên ({nhom.thanhVien.length})</h4>
        <ul className="panel-quan-ly-nhom__ds-thanh-vien">
          {nhom.thanhVien.map((tv) => (
            <li key={tv.id}>
              <Avatar id={tv.id} ten={tv.tenTaiKhoan} kichThuoc="nho" />
              <span>{tv.tenTaiKhoan}{tv.id === nhom.nguoiTaoId ? ' (Admin)' : ''}</span>
              {tv.id !== nhom.nguoiTaoId && (
                <button onClick={() => onXoaThanhVien(tv.id)} aria-label={`Xóa ${tv.tenTaiKhoan}`}>×</button>
              )}
            </li>
          ))}
        </ul>
        <select
          className="panel-quan-ly-nhom__them-thanh-vien"
          onChange={(su) => { if (su.target.value) onThemThanhVien(su.target.value); su.target.value = ''; }}
        >
          <option value="">+ Thêm thành viên...</option>
          {tatCaNguoiDung
            .filter((nd) => !nhom.thanhVien.some((tv) => tv.id === nd.id))
            .map((nd) => (
              <option key={nd.id} value={nd.id}>{nd.tenTaiKhoan}</option>
            ))}
        </select>
      </div>
    </aside>
  );
}
```

### 4.5. CSS

- `PanelThongTinNhom.css`/`PanelQuanLyNhom.css`: theo đúng phong cách
  panel hồ sơ đã có ở `TrangBanBe.css` (`.trang-ban-be__ho-so`) — cột dọc
  cố định chiều rộng ~280-320px, `background: var(--mau-nen-the)`,
  `border-left: 1px solid var(--mau-vien)`, dùng token màu hiện có (không
  hardcode `#fff`/hex — bài học từ 2 lần lỗi dark mode ở GĐ5c/GĐ5d).
- Sửa `.trang-nhom__nut-roi` trong `TrangNhom.css`: đổi `background: #fff;`
  thành `background: var(--mau-nen-the);` — cả 2 component panel mới đều
  dùng lại class này cho nút Rời/Giải tán nhóm (không cần đổi tên class,
  chỉ cần TrangNhom.css có style áp dụng đúng, vẫn hoạt động dù dùng ở
  component khác).

## 5. Kiểm thử

- `KhungTinNhan.test.tsx`: header bấm gọi `onBamTieuDe` khi có prop; không
  render nút bấm được khi không có prop (giữ nguyên hành vi cũ cho
  `TrangChat`).
- `TrangNhom.test.tsx`: bấm header mở `PanelThongTinNhom`; bấm "Chỉnh sửa"
  (admin) hoặc "Quản lý nhóm" mở `PanelQuanLyNhom`; đổi nhóm đang chọn thì
  đóng panel; không phải admin thì không thấy nút "Chỉnh sửa"/"Quản lý
  nhóm".
- Test riêng cho `PanelThongTinNhom` (nếu tách file test riêng, hoặc gộp
  vào `TrangNhom.test.tsx`): hiện đúng 8 avatar mặc định + nút "•••", bấm
  "•••" hiện hết; bấm "Rời nhóm"/"Giải tán nhóm" gọi đúng hàm.
- Test riêng cho `PanelQuanLyNhom`: đổi tên gọi `CapNhatNhom` đúng tham
  số; chọn ảnh gọi `TaiLenTep` rồi `CapNhatNhom` với `duongDanFile` trả
  về; thêm/xóa thành viên gọi đúng hàm được truyền vào qua props.
