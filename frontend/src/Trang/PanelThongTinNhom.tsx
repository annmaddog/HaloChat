import { useState } from 'react';
import { Avatar } from '../ThanhPhan/Avatar';
import { TaiLenTep, CapNhatNhom, LoiGoiApi } from '../DichVuApi';
import { useXacThuc } from '../NguCanh/NguCanhXacThuc';
import { BieuTuongMayAnh } from '../ThanhPhan/BieuTuong';
import type { Nhom } from '../KieuDuLieu';
import './PanelThongTinNhom.css';

const GIOI_HAN_ANH_BYTES = 5 * 1024 * 1024;
const SO_AVATAR_HIEN_MAC_DINH = 8;

interface PropsPanelThongTinNhom {
  nhom: Nhom;
  laAdmin: boolean;
  onDong: () => void;
  onMoQuanLy: () => void;
  onRoiNhom: () => void;
  onCapNhatNhom: (nhomMoi: Nhom) => void;
}

export function PanelThongTinNhom({ nhom, laAdmin, onDong, onMoQuanLy, onRoiNhom, onCapNhatNhom }: PropsPanelThongTinNhom) {
  const [hienHetThanhVien, setHienHetThanhVien] = useState(false);
  const { token } = useXacThuc();
  const [dangTaiAnh, setDangTaiAnh] = useState(false);
  const [loiAnh, setLoiAnh] = useState<string | null>(null);
  const thanhVienHien = hienHetThanhVien ? nhom.thanhVien : nhom.thanhVien.slice(0, SO_AVATAR_HIEN_MAC_DINH);
  const conAnDi = nhom.thanhVien.length - SO_AVATAR_HIEN_MAC_DINH;

  function doiAnhNhom(tep: File) {
    if (!token) return;
    if (!tep.type.startsWith('image/')) {
      setLoiAnh('Chỉ chấp nhận file ảnh.');
      return;
    }
    if (tep.size > GIOI_HAN_ANH_BYTES) {
      setLoiAnh(`Ảnh vượt quá giới hạn ${GIOI_HAN_ANH_BYTES / 1024 / 1024}MB.`);
      return;
    }
    setLoiAnh(null);
    setDangTaiAnh(true);
    TaiLenTep(token, tep)
      .then((daTaiLen) => CapNhatNhom(token, nhom.id, nhom.tenNhom, nhom.moTa, daTaiLen.duongDanFile))
      .then(onCapNhatNhom)
      .catch((loiBat) => setLoiAnh(loiBat instanceof LoiGoiApi ? loiBat.message : 'Đổi ảnh đại diện thất bại.'))
      .finally(() => setDangTaiAnh(false));
  }

  return (
    <aside className="panel-thong-tin-nhom">
      <button className="panel-thong-tin-nhom__dong" onClick={onDong} aria-label="Đóng">×</button>

      <div className="panel-thong-tin-nhom__anh-cum">
        <Avatar id={nhom.id} ten={nhom.tenNhom} kichThuoc="lon" duongDanAnh={nhom.duongDanAnhDaiDien} />
        {laAdmin && (
          <label className="panel-thong-tin-nhom__nut-doi-anh" aria-label="Đổi ảnh đại diện nhóm">
            {dangTaiAnh ? '...' : <BieuTuongMayAnh />}
            <input
              type="file"
              accept="image/jpeg,image/png,image/gif,image/webp"
              hidden
              disabled={dangTaiAnh}
              onChange={(su) => {
                const tep = su.target.files?.[0];
                if (tep) doiAnhNhom(tep);
                su.target.value = '';
              }}
            />
          </label>
        )}
      </div>
      {loiAnh && <p className="thong-bao-loi" role="alert">{loiAnh}</p>}
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
            <Avatar key={tv.id} id={tv.id} ten={tv.tenHienThi} kichThuoc="nho" duongDanAnh={tv.duongDanAnhDaiDien} />
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
