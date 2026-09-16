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
      <button className="panel-quan-ly-nhom__dong" onClick={onDong} aria-label="Quay lại">← Quay lại</button>
      <h3>Quản lý nhóm</h3>

      {loi && <p className="thong-bao-loi" role="alert">{loi}</p>}

      <div className="panel-quan-ly-nhom__doi-anh">
        <Avatar id={nhom.id} ten={nhom.tenNhom} kichThuoc="lon" duongDanAnh={nhom.duongDanAnhDaiDien} />
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
              <Avatar id={tv.id} ten={tv.tenHienThi} kichThuoc="nho" duongDanAnh={tv.duongDanAnhDaiDien} />
              <span>{tv.tenHienThi}{tv.id === nhom.nguoiTaoId ? ' (Admin)' : ''}</span>
              {tv.id !== nhom.nguoiTaoId && (
                <button onClick={() => onXoaThanhVien(tv.id)} aria-label={`Xóa ${tv.tenHienThi}`}>×</button>
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
              <option key={nd.id} value={nd.id}>{nd.tenHienThi}</option>
            ))}
        </select>
      </div>
    </aside>
  );
}
