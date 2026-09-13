import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { LayLoiMoiDen, LayDanhSachHoiThoai } from '../DichVuApi';
import { useXacThuc } from '../NguCanh/NguCanhXacThuc';
import { useChat } from '../NguCanh/NguCanhChat';
import { BieuTuongChuong } from './BieuTuong';
import type { LoiMoiKetBan, HoiThoaiTomTat } from '../KieuDuLieu';
import './ThongBao.css';

export function ThongBao() {
  const { token } = useXacThuc();
  const { ketNoi } = useChat();
  const navigate = useNavigate();

  const [loiMoiDen, setLoiMoiDen] = useState<LoiMoiKetBan[]>([]);
  const [hoiThoaiChuaDoc, setHoiThoaiChuaDoc] = useState<HoiThoaiTomTat[]>([]);
  const [hienDropdown, setHienDropdown] = useState(false);

  function taiLai() {
    if (!token) return;
    LayLoiMoiDen(token).then(setLoiMoiDen).catch(() => {});
    LayDanhSachHoiThoai(token)
      .then((ds) => setHoiThoaiChuaDoc(ds.filter((h) => h.soTinChuaDoc > 0)))
      .catch(() => {});
  }

  useEffect(() => {
    taiLai();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [token]);

  useEffect(() => {
    if (!ketNoi) return;
    ketNoi.on('NhanLoiMoiKetBan', taiLai);
    ketNoi.on('LoiMoiKetBanDuocChapNhan', taiLai);
    ketNoi.on('NhanTinNhan', taiLai);
    return () => {
      ketNoi.off('NhanLoiMoiKetBan', taiLai);
      ketNoi.off('LoiMoiKetBanDuocChapNhan', taiLai);
      ketNoi.off('NhanTinNhan', taiLai);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [ketNoi]);

  const tongSo = loiMoiDen.length + hoiThoaiChuaDoc.length;

  return (
    <div className="thong-bao">
      <button className="thong-bao__nut" onClick={() => setHienDropdown((v) => !v)} aria-label="Thông báo">
        <BieuTuongChuong />
        {tongSo > 0 && <span className="thong-bao__cham">{tongSo}</span>}
      </button>
      {hienDropdown && (
        <div className="thong-bao__dropdown">
          {tongSo === 0 && <p className="thong-bao__trong">Không có thông báo mới.</p>}
          {loiMoiDen.map((l) => (
            <button
              key={l.id}
              className="thong-bao__muc"
              onClick={() => {
                setHienDropdown(false);
                navigate('/ban-be');
              }}
            >
              <strong>{l.nguoiGui.tenTaiKhoan}</strong> đã gửi lời mời kết bạn.
            </button>
          ))}
          {hoiThoaiChuaDoc.map((h) => (
            <button
              key={h.nguoiDung.id}
              className="thong-bao__muc"
              onClick={() => {
                setHienDropdown(false);
                navigate('/nguoi-dung', { state: { moNguoiDung: h.nguoiDung } });
              }}
            >
              <strong>{h.nguoiDung.tenTaiKhoan}</strong>: {h.tinNhanCuoi} ({h.soTinChuaDoc})
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
