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

      <Avatar id={nhom.id} ten={nhom.tenNhom} kichThuoc="lon" duongDanAnh={nhom.duongDanAnhDaiDien} />
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
