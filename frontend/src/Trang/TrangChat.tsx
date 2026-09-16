import { useEffect, useMemo, useRef, useState } from 'react';
import { useLocation } from 'react-router-dom';
import {
  LayDanhSachHoiThoai, LayLichSuTinNhan, TaiLenTep, LoiGoiApi, LayTrangThaiHoatDong,
  LayTinDaGhimTheoNguoiDung, AnTinNhan,
} from '../DichVuApi';
import { useXacThuc } from '../NguCanh/NguCanhXacThuc';
import { useChat } from '../NguCanh/NguCanhChat';
import { KhungTinNhan, type TinNhanHienThi } from '../ThanhPhan/KhungTinNhan';
import { Avatar } from '../ThanhPhan/Avatar';
import type { NguoiDungTomTat, HoiThoaiTomTat, TinNhan } from '../KieuDuLieu';
import './TrangChat.css';

const GIOI_HAN_ANH_BYTES = 5 * 1024 * 1024;
const GIOI_HAN_FILE_BYTES = 20 * 1024 * 1024;
// [Tải lịch sử] Lần đầu tải ít hơn (20) để mở hội thoại nhanh hơn; mỗi lần
// bấm "Tải tin nhắn cũ hơn" tải thêm nhiều hơn (30). Ẩn nút khi lần tải gần
// nhất trả về ít hơn số lượng yêu cầu — nghĩa là đã hết lịch sử, không cần
// phân biệt theo ngày lịch thật.
const SO_LUONG_LICH_SU_DAU = 20;
const SO_LUONG_LICH_SU_THEM = 30;

function idNguoiKia(tinNhan: TinNhanHienThi, idHienTai: string): string {
  return tinNhan.nguoiGuiId === idHienTai ? (tinNhan.nguoiNhanId ?? '') : tinNhan.nguoiGuiId;
}

// [Ghim] Server luôn trả danh sách tin ghim mới-ghim-trước; sau khi ghim 1
// tin mới qua realtime/tự thao tác, sắp lại đúng thứ tự đó thay vì chỉ nối
// vào cuối mảng — tránh banner ghim lệch thứ tự so với khi tải lại trang.
function sapXepGiamDanTheoThoiGianGhim(danhSach: TinNhanHienThi[]): TinNhanHienThi[] {
  return [...danhSach].sort(
    (a, b) => new Date(b.thoiGianGhim ?? 0).getTime() - new Date(a.thoiGianGhim ?? 0).getTime(),
  );
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
  const [conThemLichSu, setConThemLichSu] = useState<Record<string, boolean>>({});
  const [tinNhanGhimTheoDoiTac, setTinNhanGhimTheoDoiTac] = useState<Record<string, TinNhan[]>>({});
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
    LayLichSuTinNhan(token, nguoiDangChon.id, undefined, SO_LUONG_LICH_SU_DAU)
      .then((moiNhatTruoc) => {
        if (moiNhatTruoc.length < SO_LUONG_LICH_SU_DAU) {
          setConThemLichSu((truoc) => ({ ...truoc, [nguoiDangChon.id]: false }));
        }
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
    if (!token || !nguoiDangChon) return;
    LayTinDaGhimTheoNguoiDung(token, nguoiDangChon.id)
      .then((ghim) => setTinNhanGhimTheoDoiTac((truoc) => ({ ...truoc, [nguoiDangChon.id]: ghim })))
      .catch(() => {});
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [token, nguoiDangChon]);

  useEffect(() => {
    if (!ketNoi || !nguoiDangChon) return;
    ketNoi.invoke('DanhDauDaDoc', nguoiDangChon.id, null).catch(() => {});
  }, [ketNoi, nguoiDangChon]);

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

    function xuLyTinNhanCapNhat(tinNhan: TinNhanHienThi) {
      if (tinNhan.nhomId) return;
      const idKia = idNguoiKia(tinNhan, idHienTai);
      setTinNhanTheoNguoiDung((truoc) => ({
        ...truoc,
        [idKia]: (truoc[idKia] ?? []).map((tn) => (tn.id === tinNhan.id ? tinNhan : tn)),
      }));
      setTinNhanGhimTheoDoiTac((truoc) => ({
        ...truoc,
        [idKia]: (truoc[idKia] ?? []).map((tn) => (tn.id === tinNhan.id ? tinNhan : tn)),
      }));
    }

    function xuLyTinNhanGhim(tinNhan: TinNhanHienThi) {
      xuLyTinNhanCapNhat(tinNhan);
      if (tinNhan.nhomId) return;
      const idKia = idNguoiKia(tinNhan, idHienTai);
      setTinNhanGhimTheoDoiTac((truoc) => ({
        ...truoc,
        [idKia]: sapXepGiamDanTheoThoiGianGhim([...(truoc[idKia] ?? []).filter((tn) => tn.id !== tinNhan.id), tinNhan]),
      }));
    }

    function xuLyTinNhanBoGhim(tinNhan: TinNhanHienThi) {
      xuLyTinNhanCapNhat(tinNhan);
      if (tinNhan.nhomId) return;
      const idKia = idNguoiKia(tinNhan, idHienTai);
      setTinNhanGhimTheoDoiTac((truoc) => ({
        ...truoc,
        [idKia]: (truoc[idKia] ?? []).filter((tn) => tn.id !== tinNhan.id),
      }));
    }

    ketNoi.on('NhanTinNhan', xuLyTinNhanMoi);
    ketNoi.on('TrangThaiHoatDongThayDoi', xuLyTrangThaiThayDoi);
    ketNoi.on('TinNhanDaThuHoi', xuLyTinNhanCapNhat);
    ketNoi.on('TinNhanDaGhim', xuLyTinNhanGhim);
    ketNoi.on('TinNhanBoGhim', xuLyTinNhanBoGhim);
    return () => {
      ketNoi.off('NhanTinNhan', xuLyTinNhanMoi);
      ketNoi.off('TrangThaiHoatDongThayDoi', xuLyTrangThaiThayDoi);
      ketNoi.off('TinNhanDaThuHoi', xuLyTinNhanCapNhat);
      ketNoi.off('TinNhanDaGhim', xuLyTinNhanGhim);
      ketNoi.off('TinNhanBoGhim', xuLyTinNhanBoGhim);
    };
  }, [ketNoi, idHienTai]);

  const tinNhanDangHien = useMemo(
    () => (nguoiDangChon ? (tinNhanTheoNguoiDung[nguoiDangChon.id] ?? []) : []),
    [nguoiDangChon, tinNhanTheoNguoiDung],
  );

  function guiTinNhanVanBan(noiDungGui: string, traLoiId: string | null) {
    if (!ketNoi || !nguoiDangChon) return;
    const idTam = `tam-${Date.now()}`;
    const tinNhanTam: TinNhanHienThi = {
      id: idTam, nguoiGuiId: idHienTai, nguoiNhanId: nguoiDangChon.id, nhomId: null,
      loaiTinNhan: 'Text', noiDungTinNhan: noiDungGui, duongDanFile: null, tenFileGoc: null,
      kichThuocFile: null, loaiFile: null, daDoc: false, daNhan: false,
      thoiGianTao: new Date().toISOString(), dangGui: true, traLoi: null,
      daThuHoi: false, daGhim: false, thoiGianGhim: null,
    };
    setTinNhanTheoNguoiDung((truoc) => ({ ...truoc, [nguoiDangChon.id]: [...(truoc[nguoiDangChon.id] ?? []), tinNhanTam] }));

    ketNoi
      .invoke<TinNhanHienThi>('GuiTinNhan', nguoiDangChon.id, null, 'Text', noiDungGui, null, null, null, null, traLoiId)
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

  function guiTep(tep: File, traLoiId: string | null) {
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
          daTaiLen.duongDanFile, daTaiLen.tenFileGoc, daTaiLen.kichThuocFile, daTaiLen.loaiFile, traLoiId,
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

  function capNhatTinNhanTrongState(tinCapNhat: TinNhanHienThi) {
    const idKia = idNguoiKia(tinCapNhat, idHienTai);
    setTinNhanTheoNguoiDung((truoc) => ({
      ...truoc,
      [idKia]: (truoc[idKia] ?? []).map((tn) => (tn.id === tinCapNhat.id ? tinCapNhat : tn)),
    }));
  }

  function thuHoiTinNhan(id: string) {
    if (!ketNoi || !nguoiDangChon) return;
    ketNoi.invoke<TinNhanHienThi>('ThuHoiTinNhan', id)
      .then((tinCapNhat) => {
        capNhatTinNhanTrongState(tinCapNhat);
        setTinNhanGhimTheoDoiTac((truoc) => ({
          ...truoc,
          [nguoiDangChon.id]: (truoc[nguoiDangChon.id] ?? []).map((tn) => (tn.id === tinCapNhat.id ? tinCapNhat : tn)),
        }));
      })
      .catch((loiBat) => setLoi(loiBat instanceof Error ? loiBat.message : 'Thu hồi tin nhắn thất bại.'));
  }

  function ghimTinNhan(id: string) {
    if (!ketNoi || !nguoiDangChon) return;
    ketNoi.invoke<TinNhanHienThi>('GhimTinNhan', id)
      .then((tinCapNhat) => {
        capNhatTinNhanTrongState(tinCapNhat);
        setTinNhanGhimTheoDoiTac((truoc) => ({
          ...truoc,
          [nguoiDangChon.id]: sapXepGiamDanTheoThoiGianGhim(
            [...(truoc[nguoiDangChon.id] ?? []).filter((tn) => tn.id !== tinCapNhat.id), tinCapNhat],
          ),
        }));
      })
      .catch((loiBat) => setLoi(loiBat instanceof Error ? loiBat.message : 'Ghim tin nhắn thất bại.'));
  }

  function boGhimTinNhan(id: string) {
    if (!ketNoi || !nguoiDangChon) return;
    ketNoi.invoke<TinNhanHienThi>('BoGhimTinNhan', id)
      .then((tinCapNhat) => {
        capNhatTinNhanTrongState(tinCapNhat);
        setTinNhanGhimTheoDoiTac((truoc) => ({
          ...truoc,
          [nguoiDangChon.id]: (truoc[nguoiDangChon.id] ?? []).filter((tn) => tn.id !== id),
        }));
      })
      .catch((loiBat) => setLoi(loiBat instanceof Error ? loiBat.message : 'Bỏ ghim thất bại.'));
  }

  function anTinNhanCucBo(id: string) {
    if (!token || !nguoiDangChon) return;
    AnTinNhan(token, id)
      .then(() => {
        setTinNhanTheoNguoiDung((truoc) => ({
          ...truoc,
          [nguoiDangChon.id]: (truoc[nguoiDangChon.id] ?? []).filter((tn) => tn.id !== id),
        }));
        setTinNhanGhimTheoDoiTac((truoc) => ({
          ...truoc,
          [nguoiDangChon.id]: (truoc[nguoiDangChon.id] ?? []).filter((tn) => tn.id !== id),
        }));
      })
      .catch(() => setLoi('Xóa tin nhắn thất bại.'));
  }

  function taiThemLichSuCu() {
    if (!token || !nguoiDangChon) return;
    const cuNhat = (tinNhanTheoNguoiDung[nguoiDangChon.id] ?? [])[0];
    if (!cuNhat) return;

    setDangTaiLichSu(true);
    LayLichSuTinNhan(token, nguoiDangChon.id, cuNhat.id, SO_LUONG_LICH_SU_THEM)
      .then((cuHon) => {
        if (cuHon.length < SO_LUONG_LICH_SU_THEM) {
          setConThemLichSu((truoc) => ({ ...truoc, [nguoiDangChon.id]: false }));
        }
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
            .filter((nd) => nd.tenHienThi.toLowerCase().includes(tuKhoaTimKiem.toLowerCase()))
            .map((nd) => (
              <li key={nd.id}>
                <button
                  className={`trang-chat__muc${nguoiDangChon?.id === nd.id ? ' trang-chat__muc--dang-chon' : ''}`}
                  onClick={() => setNguoiDangChon(nd)}
                >
                  <Avatar id={nd.id} ten={nd.tenHienThi} kichThuoc="nho" />
                  <span className="trang-chat__ten">{nd.tenHienThi}</span>
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
          tenHienThi={nguoiDangChon.tenHienThi}
          phuDe={trangThaiOnline[nguoiDangChon.id] ? 'Đang hoạt động' : undefined}
          danhSachTinNhan={tinNhanDangHien}
          idHienTai={idHienTai}
          dangKetNoi={dangKetNoi}
          dangTaiLichSu={dangTaiLichSu}
          coTheTaiThem={conThemLichSu[nguoiDangChon.id] !== false}
          onTaiThemLichSuCu={taiThemLichSuCu}
          onGuiVanBan={guiTinNhanVanBan}
          onGuiTep={guiTep}
          layTenNguoiGui={(id) => (id === idHienTai ? 'Bạn' : (nguoiDangChon?.tenHienThi ?? 'một người dùng'))}
          dangTaiTep={dangTaiTep}
          loi={loi}
          onQuayLai={() => setNguoiDangChon(null)}
          onThuHoi={thuHoiTinNhan}
          onGhim={ghimTinNhan}
          onBoGhim={boGhimTinNhan}
          onAn={anTinNhanCucBo}
          danhSachTinNhanGhim={nguoiDangChon ? (tinNhanGhimTheoDoiTac[nguoiDangChon.id] ?? []) : []}
        />
      )}
    </div>
  );
}
