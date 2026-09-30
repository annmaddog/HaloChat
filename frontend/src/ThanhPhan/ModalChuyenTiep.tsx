import { useEffect, useState } from 'react';
import { LayBanBe, LayDanhSachNhom, LoiGoiApi } from '../DichVuApi';
import { Avatar } from './Avatar';
import { BieuTuongDong } from './BieuTuong';
import type { NguoiDungTomTat, Nhom } from '../KieuDuLieu';
import './ModalChuyenTiep.css';

export interface MucTieuChuyenTiep {
  loai: 'nguoiDung' | 'nhom';
  id: string;
  ten: string;
}

interface PropsModalChuyenTiep {
  token: string;
  dangGui: boolean;
  loi: string | null;
  onDong: () => void;
  onXacNhan: (danhSach: MucTieuChuyenTiep[]) => void;
}

export function ModalChuyenTiep({ token, dangGui, loi, onDong, onXacNhan }: PropsModalChuyenTiep) {
  const [banBe, setBanBe] = useState<NguoiDungTomTat[]>([]);
  const [nhom, setNhom] = useState<Nhom[]>([]);
  const [dangTai, setDangTai] = useState(true);
  const [loiTai, setLoiTai] = useState<string | null>(null);
  const [tuKhoa, setTuKhoa] = useState('');
  const [daChon, setDaChon] = useState<Map<string, MucTieuChuyenTiep>>(new Map());

  useEffect(() => {
    Promise.all([LayBanBe(token), LayDanhSachNhom(token)])
      .then(([dsBanBe, dsNhom]) => {
        setBanBe(dsBanBe);
        setNhom(dsNhom);
      })
      .catch((loiBat) => setLoiTai(loiBat instanceof LoiGoiApi ? loiBat.message : 'Không tải được danh sách.'))
      .finally(() => setDangTai(false));
  }, [token]);

  function boChonHoacChon(khoa: string, muc: MucTieuChuyenTiep) {
    setDaChon((truoc) => {
      const moi = new Map(truoc);
      if (moi.has(khoa)) moi.delete(khoa);
      else moi.set(khoa, muc);
      return moi;
    });
  }

  const banBeLoc = banBe.filter((nd) => nd.tenHienThi.toLowerCase().includes(tuKhoa.toLowerCase()));
  const nhomLoc = nhom.filter((n) => n.tenNhom.toLowerCase().includes(tuKhoa.toLowerCase()));

  return (
    <div className="modal-chuyen-tiep__nen" onClick={onDong}>
      <div className="modal-chuyen-tiep__hop" onClick={(su) => su.stopPropagation()}>
        <div className="modal-chuyen-tiep__dau">
          <h2>Chuyển tiếp tin nhắn</h2>
          <button type="button" onClick={onDong} aria-label="Đóng"><BieuTuongDong /></button>
        </div>

        <input
          type="text"
          className="modal-chuyen-tiep__tim-kiem"
          placeholder="Tìm bạn bè, nhóm..."
          value={tuKhoa}
          onChange={(su) => setTuKhoa(su.target.value)}
        />

        {(loi || loiTai) && <p className="thong-bao-loi" role="alert">{loi ?? loiTai}</p>}

        {dangTai ? (
          <p className="modal-chuyen-tiep__trang-thai">Đang tải...</p>
        ) : (
          <ul className="modal-chuyen-tiep__danh-sach">
            {nhomLoc.map((n) => {
              const khoa = `nhom:${n.id}`;
              return (
                <li key={khoa}>
                  <label className="modal-chuyen-tiep__muc">
                    <input
                      type="checkbox"
                      checked={daChon.has(khoa)}
                      onChange={() => boChonHoacChon(khoa, { loai: 'nhom', id: n.id, ten: n.tenNhom })}
                    />
                    <Avatar id={n.id} ten={n.tenNhom} kichThuoc="nho" duongDanAnh={n.duongDanAnhDaiDien} />
                    <span>{n.tenNhom}</span>
                  </label>
                </li>
              );
            })}
            {banBeLoc.map((nd) => {
              const khoa = `nguoiDung:${nd.id}`;
              return (
                <li key={khoa}>
                  <label className="modal-chuyen-tiep__muc">
                    <input
                      type="checkbox"
                      checked={daChon.has(khoa)}
                      onChange={() => boChonHoacChon(khoa, { loai: 'nguoiDung', id: nd.id, ten: nd.tenHienThi })}
                    />
                    <Avatar id={nd.id} ten={nd.tenHienThi} kichThuoc="nho" duongDanAnh={nd.duongDanAnhDaiDien} />
                    <span>{nd.tenHienThi}</span>
                  </label>
                </li>
              );
            })}
            {nhomLoc.length === 0 && banBeLoc.length === 0 && (
              <p className="modal-chuyen-tiep__trang-thai">Không tìm thấy kết quả.</p>
            )}
          </ul>
        )}

        <button
          type="button"
          className="nut-chinh"
          disabled={daChon.size === 0 || dangGui}
          onClick={() => onXacNhan(Array.from(daChon.values()))}
        >
          {dangGui ? 'Đang gửi...' : `Gửi${daChon.size > 0 ? ` (${daChon.size})` : ''}`}
        </button>
      </div>
    </div>
  );
}
