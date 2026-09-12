import { useEffect, useMemo, useRef, useState } from 'react';
import { LayDanhSachNguoiDung, LayLichSuTinNhan, LoiGoiApi } from '../DichVuApi';
import { useXacThuc } from '../NguCanh/NguCanhXacThuc';
import { useChat } from '../NguCanh/NguCanhChat';
import type { NguoiDungTomTat, TinNhan } from '../KieuDuLieu';
import './TrangChat.css';

function idNguoiKia(tinNhan: TinNhan, idHienTai: string): string {
  return tinNhan.nguoiGuiId === idHienTai ? (tinNhan.nguoiNhanId ?? '') : tinNhan.nguoiGuiId;
}

export function TrangChat() {
  const { token, nguoiDungHienTai, dangXuat } = useXacThuc();
  const { ketNoi, dangKetNoi } = useChat();

  const [danhSachNguoiDung, setDanhSachNguoiDung] = useState<NguoiDungTomTat[]>([]);
  const [nguoiDangChon, setNguoiDangChon] = useState<NguoiDungTomTat | null>(null);
  const [tinNhanTheoNguoiDung, setTinNhanTheoNguoiDung] = useState<Record<string, TinNhan[]>>({});
  const [dangTaiDanhSach, setDangTaiDanhSach] = useState(true);
  const [dangTaiLichSu, setDangTaiLichSu] = useState(false);
  const [noiDungDangGo, setNoiDungDangGo] = useState('');
  const [loi, setLoi] = useState<string | null>(null);
  const cuoiDanhSachRef = useRef<HTMLDivElement | null>(null);
  const idDaTaiLichSuRef = useRef<Set<string>>(new Set());

  const idHienTai = nguoiDungHienTai?.id ?? '';

  useEffect(() => {
    if (!token) return;
    LayDanhSachNguoiDung(token)
      .then(setDanhSachNguoiDung)
      .catch((loiBat) => {
        if (loiBat instanceof LoiGoiApi && loiBat.trangThai === 401) {
          dangXuat();
          return;
        }
        setLoi(loiBat instanceof Error ? loiBat.message : 'Đã có lỗi xảy ra.');
      })
      .finally(() => setDangTaiDanhSach(false));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [token]);

  useEffect(() => {
    if (!token || !nguoiDangChon) return;
    if (idDaTaiLichSuRef.current.has(nguoiDangChon.id)) return;
    idDaTaiLichSuRef.current.add(nguoiDangChon.id);

    setDangTaiLichSu(true);
    LayLichSuTinNhan(token, nguoiDangChon.id)
      .then((moiNhatTruoc) => {
        const thuTuThoiGian = [...moiNhatTruoc].reverse();
        setTinNhanTheoNguoiDung((truoc) => {
          const gop = new Map<string, TinNhan>();
          for (const tn of thuTuThoiGian) gop.set(tn.id, tn);
          for (const tn of truoc[nguoiDangChon.id] ?? []) gop.set(tn.id, tn);
          const ketQua = [...gop.values()].sort((a, b) => (a.id < b.id ? -1 : a.id > b.id ? 1 : 0));
          return { ...truoc, [nguoiDangChon.id]: ketQua };
        });
      })
      .catch((loiBat) => {
        idDaTaiLichSuRef.current.delete(nguoiDangChon.id);
        setLoi(loiBat instanceof Error ? loiBat.message : 'Không tải được lịch sử tin nhắn.');
      })
      .finally(() => setDangTaiLichSu(false));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [token, nguoiDangChon]);

  useEffect(() => {
    if (!ketNoi) return;

    function xuLyTinNhanMoi(tinNhan: TinNhan) {
      const idKia = idNguoiKia(tinNhan, idHienTai);
      setTinNhanTheoNguoiDung((truoc) => ({
        ...truoc,
        [idKia]: [...(truoc[idKia] ?? []), tinNhan],
      }));
    }

    ketNoi.on('NhanTinNhan', xuLyTinNhanMoi);
    return () => {
      ketNoi.off('NhanTinNhan', xuLyTinNhanMoi);
    };
  }, [ketNoi, idHienTai]);

  useEffect(() => {
    cuoiDanhSachRef.current?.scrollIntoView?.({ block: 'end' });
  }, [nguoiDangChon, tinNhanTheoNguoiDung]);

  const tinNhanDangHien = useMemo(
    () => (nguoiDangChon ? (tinNhanTheoNguoiDung[nguoiDangChon.id] ?? []) : []),
    [nguoiDangChon, tinNhanTheoNguoiDung],
  );

  async function guiTinNhanVanBan() {
    if (!ketNoi || !nguoiDangChon || !noiDungDangGo.trim()) return;

    const noiDungGui = noiDungDangGo.trim();
    setNoiDungDangGo('');
    try {
      const tinNhanDaGui = await ketNoi.invoke<TinNhan>(
        'GuiTinNhan',
        nguoiDangChon.id,
        'Text',
        noiDungGui,
        null,
        null,
        null,
        null,
      );
      setTinNhanTheoNguoiDung((truoc) => ({
        ...truoc,
        [nguoiDangChon.id]: [...(truoc[nguoiDangChon.id] ?? []), tinNhanDaGui],
      }));
    } catch {
      setLoi('Gửi tin nhắn thất bại. Vui lòng thử lại.');
    }
  }

  async function taiThemLichSuCu() {
    if (!token || !nguoiDangChon) return;
    const cuNhat = (tinNhanTheoNguoiDung[nguoiDangChon.id] ?? [])[0];
    if (!cuNhat) return;

    setDangTaiLichSu(true);
    try {
      const cuHon = await LayLichSuTinNhan(token, nguoiDangChon.id, cuNhat.id);
      const thuTuThoiGian = [...cuHon].reverse();
      setTinNhanTheoNguoiDung((truoc) => ({
        ...truoc,
        [nguoiDangChon.id]: [...thuTuThoiGian, ...(truoc[nguoiDangChon.id] ?? [])],
      }));
    } catch {
      setLoi('Không tải được tin nhắn cũ hơn.');
    } finally {
      setDangTaiLichSu(false);
    }
  }

  return (
    <div className="trang-chat">
      <aside className="trang-chat__sidebar">
        <div className="trang-chat__sidebar-dau">
          <h1>HaloChat</h1>
          <button className="trang-chat__nut-dang-xuat" onClick={dangXuat}>
            Đăng xuất
          </button>
        </div>
        {dangTaiDanhSach && <p>Đang tải...</p>}
        <ul className="trang-chat__danh-sach">
          {danhSachNguoiDung.map((nd) => (
            <li key={nd.id}>
              <button
                className={`trang-chat__muc${nguoiDangChon?.id === nd.id ? ' trang-chat__muc--dang-chon' : ''}`}
                onClick={() => setNguoiDangChon(nd)}
              >
                <span className="trang-chat__avatar">{nd.tenTaiKhoan.charAt(0).toUpperCase()}</span>
                <span className="trang-chat__ten">{nd.tenTaiKhoan}</span>
              </button>
            </li>
          ))}
        </ul>
      </aside>

      <main className="trang-chat__khung-chinh">
        {loi && (
          <p className="thong-bao-loi" role="alert">
            {loi}
          </p>
        )}
        {!nguoiDangChon && <p className="trang-chat__trong">Chọn một người để bắt đầu trò chuyện.</p>}
        {nguoiDangChon && (
          <>
            <header className="trang-chat__tieu-de">
              <span>{nguoiDangChon.tenTaiKhoan}</span>
              {!dangKetNoi && <span className="trang-chat__mat-ket-noi">Mất kết nối realtime...</span>}
            </header>

            <div className="trang-chat__danh-sach-tin-nhan">
              <button className="trang-chat__nut-tai-them" onClick={taiThemLichSuCu} disabled={dangTaiLichSu}>
                {dangTaiLichSu ? 'Đang tải...' : 'Tải tin nhắn cũ hơn'}
              </button>
              {tinNhanDangHien.map((tn) => (
                <div
                  key={tn.id}
                  className={`trang-chat__bong-tin-nhan${tn.nguoiGuiId === idHienTai ? ' trang-chat__bong-tin-nhan--minh' : ''}`}
                >
                  {tn.noiDungTinNhan}
                </div>
              ))}
              <div ref={cuoiDanhSachRef} />
            </div>

            <form
              className="trang-chat__form-gui"
              onSubmit={(su) => {
                su.preventDefault();
                void guiTinNhanVanBan();
              }}
            >
              <input
                type="text"
                value={noiDungDangGo}
                onChange={(su) => setNoiDungDangGo(su.target.value)}
                placeholder="Nhập tin nhắn..."
                disabled={!dangKetNoi}
              />
              <button type="submit" disabled={!dangKetNoi || !noiDungDangGo.trim()}>
                Gửi
              </button>
            </form>
          </>
        )}
      </main>
    </div>
  );
}
