import { useEffect, useMemo, useRef, useState } from 'react';
import { useLocation } from 'react-router-dom';
import { LayDanhSachHoiThoai, LayLichSuTinNhan, TaiLenTep, LoiGoiApi, LayTrangThaiHoatDong } from '../DichVuApi';
import { useXacThuc } from '../NguCanh/NguCanhXacThuc';
import { useChat } from '../NguCanh/NguCanhChat';
import { KhungTinNhan, type TinNhanHienThi } from '../ThanhPhan/KhungTinNhan';
import type { NguoiDungTomTat, HoiThoaiTomTat } from '../KieuDuLieu';
import './TrangChat.css';

const GIOI_HAN_ANH_BYTES = 5 * 1024 * 1024;
const GIOI_HAN_FILE_BYTES = 20 * 1024 * 1024;

function idNguoiKia(tinNhan: TinNhanHienThi, idHienTai: string): string {
  return tinNhan.nguoiGuiId === idHienTai ? (tinNhan.nguoiNhanId ?? '') : tinNhan.nguoiGuiId;
}

export function TrangChat() {
  const { token, nguoiDungHienTai, dangXuat } = useXacThuc();
  const { ketNoi, dangKetNoi } = useChat();
  const location = useLocation();
  const moNguoiDungTuDieuHuong = (location.state as { moNguoiDung?: NguoiDungTomTat } | null)?.moNguoiDung ?? null;

  const [danhSachHoiThoai, setDanhSachHoiThoai] = useState<HoiThoaiTomTat[]>([]);
  const [nguoiDangChon, setNguoiDangChon] = useState<NguoiDungTomTat | null>(moNguoiDungTuDieuHuong);
  const [tinNhanTheoNguoiDung, setTinNhanTheoNguoiDung] = useState<Record<string, TinNhanHienThi[]>>({});
  const [trangThaiOnline, setTrangThaiOnline] = useState<Record<string, boolean>>({});
  const [dangTaiDanhSach, setDangTaiDanhSach] = useState(true);
  const [dangTaiLichSu, setDangTaiLichSu] = useState(false);
  const [loi, setLoi] = useState<string | null>(null);
  const [dangTaiTep, setDangTaiTep] = useState(false);
  const [tuKhoaTimKiem, setTuKhoaTimKiem] = useState('');
  const idDaTaiLichSuRef = useRef<Set<string>>(new Set());

  const idHienTai = nguoiDungHienTai?.id ?? '';

  useEffect(() => {
    if (!token) return;
    LayDanhSachHoiThoai(token)
      .then((ds) => {
        setDanhSachHoiThoai(ds);
        return LayTrangThaiHoatDong(token, ds.map((h) => h.nguoiDung.id));
      })
      .then((tt) => setTrangThaiOnline(tt))
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

  const danhSachHienThi = useMemo<NguoiDungTomTat[]>(
    () => danhSachHoiThoai.map((h) => h.nguoiDung),
    [danhSachHoiThoai],
  );

  useEffect(() => {
    if (!token || !nguoiDangChon) return;
    if (idDaTaiLichSuRef.current.has(nguoiDangChon.id)) return;
    idDaTaiLichSuRef.current.add(nguoiDangChon.id);

    setDangTaiLichSu(true);
    LayLichSuTinNhan(token, nguoiDangChon.id)
      .then((moiNhatTruoc) => {
        const thuTuThoiGian = [...moiNhatTruoc].reverse();
        setTinNhanTheoNguoiDung((truoc) => {
          const gop = new Map<string, TinNhanHienThi>();
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

    function xuLyTinNhanMoi(tinNhan: TinNhanHienThi) {
      if (tinNhan.nhomId) return; // tin nhắn nhóm không thuộc trang này (Task 10 xử lý riêng)
      const idKia = idNguoiKia(tinNhan, idHienTai);
      setTinNhanTheoNguoiDung((truoc) => ({
        ...truoc,
        [idKia]: [...(truoc[idKia] ?? []).filter((tn) => tn.id !== tinNhan.id), tinNhan],
      }));
    }

    function xuLyTrangThaiThayDoi(userId: string, online: boolean) {
      setTrangThaiOnline((truoc) => ({ ...truoc, [userId]: online }));
    }

    ketNoi.on('NhanTinNhan', xuLyTinNhanMoi);
    ketNoi.on('TrangThaiHoatDongThayDoi', xuLyTrangThaiThayDoi);
    return () => {
      ketNoi.off('NhanTinNhan', xuLyTinNhanMoi);
      ketNoi.off('TrangThaiHoatDongThayDoi', xuLyTrangThaiThayDoi);
    };
  }, [ketNoi, idHienTai]);

  const tinNhanDangHien = useMemo(
    () => (nguoiDangChon ? (tinNhanTheoNguoiDung[nguoiDangChon.id] ?? []) : []),
    [nguoiDangChon, tinNhanTheoNguoiDung],
  );

  function guiTinNhanVanBan(noiDungGui: string) {
    if (!ketNoi || !nguoiDangChon) return;
    const idTam = `tam-${Date.now()}`;
    const tinNhanTam: TinNhanHienThi = {
      id: idTam, nguoiGuiId: idHienTai, nguoiNhanId: nguoiDangChon.id, nhomId: null,
      loaiTinNhan: 'Text', noiDungTinNhan: noiDungGui, duongDanFile: null, tenFileGoc: null,
      kichThuocFile: null, loaiFile: null, daDoc: false, daNhan: false,
      thoiGianTao: new Date().toISOString(), dangGui: true,
    };
    setTinNhanTheoNguoiDung((truoc) => ({ ...truoc, [nguoiDangChon.id]: [...(truoc[nguoiDangChon.id] ?? []), tinNhanTam] }));

    ketNoi
      .invoke<TinNhanHienThi>('GuiTinNhan', nguoiDangChon.id, null, 'Text', noiDungGui, null, null, null, null)
      .then((tinNhanDaGui) => {
        setTinNhanTheoNguoiDung((truoc) => ({
          ...truoc,
          [nguoiDangChon.id]: (truoc[nguoiDangChon.id] ?? []).map((tn) => (tn.id === idTam ? tinNhanDaGui : tn)),
        }));
      })
      .catch((loiBat) => {
        setTinNhanTheoNguoiDung((truoc) => ({
          ...truoc,
          [nguoiDangChon.id]: (truoc[nguoiDangChon.id] ?? []).filter((tn) => tn.id !== idTam),
        }));
        setLoi(loiBat instanceof Error ? loiBat.message : 'Gửi tin nhắn thất bại. Vui lòng thử lại.');
      });
  }

  function guiTep(tep: File) {
    if (!ketNoi || !nguoiDangChon || !token) return;

    const laAnh = tep.type.startsWith('image/');
    const gioiHan = laAnh ? GIOI_HAN_ANH_BYTES : GIOI_HAN_FILE_BYTES;
    if (tep.size > gioiHan) {
      setLoi(`File vượt quá giới hạn ${gioiHan / 1024 / 1024}MB.`);
      return;
    }

    setDangTaiTep(true);
    TaiLenTep(token, tep)
      .then((daTaiLen) =>
        ketNoi.invoke<TinNhanHienThi>(
          'GuiTinNhan', nguoiDangChon.id, null, laAnh ? 'Anh' : 'File', '',
          daTaiLen.duongDanFile, daTaiLen.tenFileGoc, daTaiLen.kichThuocFile, daTaiLen.loaiFile,
        ),
      )
      .then((tinNhanDaGui) => {
        setTinNhanTheoNguoiDung((truoc) => ({
          ...truoc,
          [nguoiDangChon.id]: [...(truoc[nguoiDangChon.id] ?? []), tinNhanDaGui],
        }));
      })
      .catch((loiBat) => {
        setLoi(loiBat instanceof Error ? loiBat.message : 'Gửi file thất bại. Vui lòng thử lại.');
      })
      .finally(() => setDangTaiTep(false));
  }

  function taiThemLichSuCu() {
    if (!token || !nguoiDangChon) return;
    const cuNhat = (tinNhanTheoNguoiDung[nguoiDangChon.id] ?? [])[0];
    if (!cuNhat) return;

    setDangTaiLichSu(true);
    LayLichSuTinNhan(token, nguoiDangChon.id, cuNhat.id)
      .then((cuHon) => {
        const thuTuThoiGian = [...cuHon].reverse();
        setTinNhanTheoNguoiDung((truoc) => ({
          ...truoc,
          [nguoiDangChon.id]: [...thuTuThoiGian, ...(truoc[nguoiDangChon.id] ?? [])],
        }));
      })
      .catch(() => setLoi('Không tải được tin nhắn cũ hơn.'))
      .finally(() => setDangTaiLichSu(false));
  }

  return (
    <div className={`trang-chat${nguoiDangChon ? ' trang-chat--da-chon' : ''}`}>
      <aside className="trang-chat__sidebar">
        <input
          type="text"
          className="trang-chat__tim-kiem"
          placeholder="Tìm cuộc trò chuyện..."
          value={tuKhoaTimKiem}
          onChange={(su) => setTuKhoaTimKiem(su.target.value)}
        />
        {dangTaiDanhSach && <p>Đang tải...</p>}
        <ul className="trang-chat__danh-sach">
          {danhSachHienThi
            .filter((nd) => nd.tenTaiKhoan.toLowerCase().includes(tuKhoaTimKiem.toLowerCase()))
            .map((nd) => (
              <li key={nd.id}>
                <button
                  className={`trang-chat__muc${nguoiDangChon?.id === nd.id ? ' trang-chat__muc--dang-chon' : ''}`}
                  onClick={() => setNguoiDangChon(nd)}
                >
                  <span className="trang-chat__avatar">{nd.tenTaiKhoan.charAt(0).toUpperCase()}</span>
                  <span className="trang-chat__ten">{nd.tenTaiKhoan}</span>
                  {trangThaiOnline[nd.id] && <span className="trang-chat__cham-online" title="Đang hoạt động" />}
                </button>
              </li>
            ))}
        </ul>
      </aside>

      {!nguoiDangChon && (
        <div className="trang-chat__trong-rong">
          <p className="trang-chat__trong-tieu-de">Chào mừng đến HaloChat</p>
          <p className="trang-chat__trong">Chọn một cuộc trò chuyện để bắt đầu nhắn tin an toàn.</p>
        </div>
      )}
      {nguoiDangChon && (
        <KhungTinNhan
          loaiHoiThoai="nguoiDung"
          tenHienThi={nguoiDangChon.tenTaiKhoan}
          phuDe={trangThaiOnline[nguoiDangChon.id] ? 'Đang hoạt động' : undefined}
          danhSachTinNhan={tinNhanDangHien}
          idHienTai={idHienTai}
          dangKetNoi={dangKetNoi}
          dangTaiLichSu={dangTaiLichSu}
          coTheTaiThem
          onTaiThemLichSuCu={taiThemLichSuCu}
          onGuiVanBan={guiTinNhanVanBan}
          onGuiTep={guiTep}
          dangTaiTep={dangTaiTep}
          loi={loi}
          onQuayLai={() => setNguoiDangChon(null)}
        />
      )}
    </div>
  );
}
