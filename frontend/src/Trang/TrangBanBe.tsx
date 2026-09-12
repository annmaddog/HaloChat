import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  LayBanBe,
  LayLoiMoiDen,
  LayLoiMoiGui,
  LayDanhSachNguoiDung,
  GuiLoiMoiKetBan,
  ChapNhanLoiMoiKetBan,
  TuChoiLoiMoiKetBan,
  LoiGoiApi,
} from '../DichVuApi';
import { useXacThuc } from '../NguCanh/NguCanhXacThuc';
import type { NguoiDungTomTat, LoiMoiKetBan } from '../KieuDuLieu';
import './TrangBanBe.css';

export function TrangBanBe() {
  const { token } = useXacThuc();
  const navigate = useNavigate();

  const [banBe, setBanBe] = useState<NguoiDungTomTat[]>([]);
  const [loiMoiDen, setLoiMoiDen] = useState<LoiMoiKetBan[]>([]);
  const [loiMoiGui, setLoiMoiGui] = useState<LoiMoiKetBan[]>([]);
  const [tatCaNguoiDung, setTatCaNguoiDung] = useState<NguoiDungTomTat[]>([]);
  const [loi, setLoi] = useState<string | null>(null);
  const [dangTai, setDangTai] = useState(true);

  async function taiLaiTatCa(tokenHienTai: string) {
    const [dsBanBe, dsLoiMoiDen, dsLoiMoiGui, dsTatCa] = await Promise.all([
      LayBanBe(tokenHienTai),
      LayLoiMoiDen(tokenHienTai),
      LayLoiMoiGui(tokenHienTai),
      LayDanhSachNguoiDung(tokenHienTai),
    ]);
    setBanBe(dsBanBe);
    setLoiMoiDen(dsLoiMoiDen);
    setLoiMoiGui(dsLoiMoiGui);
    setTatCaNguoiDung(dsTatCa);
  }

  useEffect(() => {
    if (!token) return;
    taiLaiTatCa(token)
      .catch((loiBat) => setLoi(loiBat instanceof Error ? loiBat.message : 'Đã có lỗi xảy ra.'))
      .finally(() => setDangTai(false));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [token]);

  async function guiLoiMoi(nguoiNhanId: string) {
    if (!token) return;
    try {
      await GuiLoiMoiKetBan(token, nguoiNhanId);
      await taiLaiTatCa(token);
    } catch (loiBat) {
      setLoi(loiBat instanceof LoiGoiApi ? loiBat.message : 'Gửi lời mời thất bại.');
    }
  }

  async function chapNhan(id: string) {
    if (!token) return;
    try {
      await ChapNhanLoiMoiKetBan(token, id);
      await taiLaiTatCa(token);
    } catch (loiBat) {
      setLoi(loiBat instanceof LoiGoiApi ? loiBat.message : 'Chấp nhận lời mời thất bại.');
    }
  }

  async function tuChoi(id: string) {
    if (!token) return;
    try {
      await TuChoiLoiMoiKetBan(token, id);
      await taiLaiTatCa(token);
    } catch (loiBat) {
      setLoi(loiBat instanceof LoiGoiApi ? loiBat.message : 'Từ chối lời mời thất bại.');
    }
  }

  const idDaLaBanBeHoacDangCho = new Set<string>([
    ...banBe.map((b) => b.id),
    ...loiMoiDen.map((l) => l.nguoiGui.id),
    ...loiMoiGui.map((l) => l.nguoiNhan.id),
  ]);

  return (
    <div className="trang-ban-be">
      {loi && (
        <p className="thong-bao-loi" role="alert">
          {loi}
        </p>
      )}
      {dangTai && <p>Đang tải...</p>}

      <section className="trang-ban-be__phan">
        <h2>Lời mời kết bạn ({loiMoiDen.length})</h2>
        {loiMoiDen.length === 0 && <p className="trang-ban-be__trong">Không có lời mời nào.</p>}
        <ul className="trang-ban-be__danh-sach">
          {loiMoiDen.map((l) => (
            <li key={l.id} className="trang-ban-be__muc">
              <span>{l.nguoiGui.tenTaiKhoan}</span>
              <div className="trang-ban-be__hanh-dong">
                <button onClick={() => chapNhan(l.id)}>Chấp nhận</button>
                <button className="trang-ban-be__nut-phu" onClick={() => tuChoi(l.id)}>
                  Từ chối
                </button>
              </div>
            </li>
          ))}
        </ul>
      </section>

      <section className="trang-ban-be__phan">
        <h2>Bạn bè ({banBe.length})</h2>
        {banBe.length === 0 && <p className="trang-ban-be__trong">Chưa có bạn bè nào.</p>}
        <ul className="trang-ban-be__danh-sach">
          {banBe.map((b) => (
            <li key={b.id} className="trang-ban-be__muc">
              <span>{b.tenTaiKhoan}</span>
              <button onClick={() => navigate('/nguoi-dung', { state: { moNguoiDung: b } })}>Nhắn tin</button>
            </li>
          ))}
        </ul>
      </section>

      <section className="trang-ban-be__phan">
        <h2>Tìm người để kết bạn</h2>
        <ul className="trang-ban-be__danh-sach">
          {tatCaNguoiDung
            .filter((nd) => !idDaLaBanBeHoacDangCho.has(nd.id))
            .map((nd) => (
              <li key={nd.id} className="trang-ban-be__muc">
                <span>{nd.tenTaiKhoan}</span>
                <button onClick={() => guiLoiMoi(nd.id)}>Kết bạn</button>
              </li>
            ))}
        </ul>
      </section>
    </div>
  );
}
