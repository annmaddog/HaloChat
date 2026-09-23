import { useEffect, useRef, useState, type FormEvent, type ChangeEvent, type ClipboardEvent } from 'react';
import { BieuTuongGhim, BieuTuongTraLoi, BieuTuongTaiLieu, BieuTuongTai, BieuTuongBaCham, BieuTuongMatCuoi, BieuTuongKhoLuuTru, BieuTuongTimKiem } from './BieuTuong';
import { Avatar } from './Avatar';
import { DIA_CHI_GOC } from '../DichVuApi';
import type { TinNhan, LoaiCamXuc } from '../KieuDuLieu';
import './KhungTinNhan.css';

export type TinNhanHienThi = TinNhan & { dangGui?: boolean };

function dinhDangKichThuoc(bytes: number): string {
  const mb = bytes / (1024 * 1024);
  return mb >= 1 ? `${mb.toFixed(1)}MB` : `${Math.ceil(bytes / 1024)}KB`;
}

// [Bỏ trạng thái đã gửi/đã nhận/đã xem] Theo yêu cầu, không hiện trạng thái
// tin nhắn thường trực nữa — chỉ hiện giờ:phút gửi khi người dùng bấm vào
// đúng tin nhắn đó (xem state tinDangMoId bên dưới).
function dinhDangGio(thoiGianTao: string): string {
  const ngay = new Date(thoiGianTao);
  return ngay.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit', timeZone: 'Asia/Ho_Chi_Minh' });
}

// [Bộ emoji ô nhập] Danh sách phẳng, không chia danh mục/tab — theo đúng
// yêu cầu "đơn giản, 1 nhóm" đã chốt.
const DANH_SACH_EMOJI = [
  '😀', '😁', '😂', '🤣', '😊', '😍', '😘', '😗', '😉', '😜',
  '🤔', '😐', '😑', '😶', '🙄', '😏', '😥', '😮', '😯', '😪',
  '😫', '😴', '😌', '😛', '😝', '🤤', '😒', '😓', '😔', '😕',
  '🙁', '😖', '😞', '😟', '😤', '😢', '😭', '😦', '😧', '😨',
  '😩', '🤯', '😬', '😰', '😱', '😳', '😡', '😠', '🤬', '😷',
  '🥳', '🥰', '🤗', '🤩', '😇', '🤪', '😎', '🤠', '👍', '👎',
  '👏', '🙏', '❤️', '💔', '🔥', '🎉',
];

const EMOJI_CAM_XUC: Record<LoaiCamXuc, string> = {
  Thich: '👍', YeuThich: '❤️', Haha: '😂', Wow: '😮', Buon: '😢', PhanNo: '😠',
};
const THU_TU_CAM_XUC: LoaiCamXuc[] = ['Thich', 'YeuThich', 'Haha', 'Wow', 'Buon', 'PhanNo'];

function trichNoiDungTinNhan(tn: TinNhan): string {
  if (tn.daThuHoi) return 'Tin nhắn đã được thu hồi.';
  if (tn.loaiTinNhan === 'Text') return tn.noiDungTinNhan;
  if (tn.loaiTinNhan === 'Anh') return '[Ảnh]';
  return `[File] ${tn.tenFileGoc}`;
}

interface PropsKhungTinNhan {
  loaiHoiThoai: 'nguoiDung' | 'nhom';
  tenHienThi: string;
  phuDe?: string;
  danhSachTinNhan: TinNhanHienThi[];
  idHienTai: string;
  dangKetNoi: boolean;
  dangTaiLichSu: boolean;
  coTheTaiThem: boolean;
  onTaiThemLichSuCu: () => void;
  onGuiVanBan: (noiDung: string, traLoiId: string | null) => void;
  onGuiTep: (tep: File, traLoiId: string | null) => void;
  dangTaiTep: boolean;
  loi: string | null;
  onQuayLai?: () => void;
  onBamTieuDe?: () => void;
  layTenNguoiGui?: (nguoiGuiId: string) => string;
  onThuHoi: (id: string) => void;
  onGhim: (id: string) => void;
  onBoGhim: (id: string) => void;
  onAn: (id: string) => void;
  danhSachTinNhanGhim: TinNhan[];
  onMoKhoMedia: () => void;
  onTimKiem: (tuKhoa: string) => Promise<TinNhan[]>;
  onNhayToiTinNhan: (id: string) => Promise<boolean>;
  onThaCamXuc: (id: string, loaiCamXuc: LoaiCamXuc) => void;
  onBoCamXuc: (id: string) => void;
  duongDanAnh?: string | null;
}

export function KhungTinNhan({
  tenHienThi, phuDe, danhSachTinNhan, idHienTai, dangKetNoi, dangTaiLichSu,
  coTheTaiThem, onTaiThemLichSuCu, onGuiVanBan, onGuiTep, dangTaiTep, loi, onQuayLai, onBamTieuDe, layTenNguoiGui,
  onThuHoi, onGhim, onBoGhim, onAn, danhSachTinNhanGhim, onMoKhoMedia, onTimKiem, onNhayToiTinNhan,
  onThaCamXuc, onBoCamXuc, duongDanAnh,
}: PropsKhungTinNhan) {
  const inputTepRef = useRef<HTMLInputElement | null>(null);
  const cuoiDanhSachRef = useRef<HTMLDivElement | null>(null);
  const noiDungRef = useRef<HTMLInputElement | null>(null);
  const [tinDangMoId, setTinDangMoId] = useState<string | null>(null);
  const [dangTraLoiId, setDangTraLoiId] = useState<string | null>(null);
  const [menuMoChoTinNhanId, setMenuMoChoTinNhanId] = useState<string | null>(null);
  const [popupCamXucChoTinNhanId, setPopupCamXucChoTinNhanId] = useState<string | null>(null);
  const homGioHanCamXucRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const [hienBangEmoji, setHienBangEmoji] = useState(false);
  const [hienOTimKiem, setHienOTimKiem] = useState(false);
  const [tuKhoaTim, setTuKhoaTim] = useState('');
  const [ketQuaTim, setKetQuaTim] = useState<TinNhan[]>([]);
  const [loiTim, setLoiTim] = useState<string | null>(null);
  const [idCanCuonToi, setIdCanCuonToi] = useState<string | null>(null);
  const [idDangNoiBat, setIdDangNoiBat] = useState<string | null>(null);
  const thamChieuBongBongRef = useRef<Map<string, HTMLDivElement>>(new Map());
  const bomTimKiemRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    setDangTraLoiId(null);
  }, [tenHienThi]);

  useEffect(() => {
    if (!hienOTimKiem) return;
    if (bomTimKiemRef.current) clearTimeout(bomTimKiemRef.current);
    if (!tuKhoaTim.trim()) {
      setKetQuaTim([]);
      return;
    }
    bomTimKiemRef.current = setTimeout(() => {
      onTimKiem(tuKhoaTim.trim()).then(setKetQuaTim).catch(() => setKetQuaTim([]));
    }, 300);
    return () => {
      if (bomTimKiemRef.current) clearTimeout(bomTimKiemRef.current);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [tuKhoaTim, hienOTimKiem]);

  useEffect(() => {
    if (!idCanCuonToi) return;
    const phanTu = thamChieuBongBongRef.current.get(idCanCuonToi);
    if (!phanTu) return;
    phanTu.scrollIntoView?.({ block: 'center' });
    setIdDangNoiBat(idCanCuonToi);
    setIdCanCuonToi(null);
    const bom = setTimeout(() => setIdDangNoiBat(null), 2000);
    return () => clearTimeout(bom);
  }, [idCanCuonToi, danhSachTinNhan]);

  async function moKetQuaTim(tn: TinNhan) {
    const daCoSan = danhSachTinNhan.some((t) => t.id === tn.id);
    setLoiTim(null);
    if (daCoSan) {
      setHienOTimKiem(false);
      setTuKhoaTim('');
      setIdCanCuonToi(tn.id);
      return;
    }
    const timThay = await onNhayToiTinNhan(tn.id);
    if (timThay) {
      setHienOTimKiem(false);
      setTuKhoaTim('');
      setIdCanCuonToi(tn.id);
    } else {
      setLoiTim('Không tìm thấy tin nhắn này trong lịch sử.');
    }
  }

  // Đóng menu "..." khi bấm ra ngoài hoặc bấm Escape — bấm bên trong
  // `.khung-tin-nhan__icon-noi` (icon Trả lời/"..."/chính menu) không tính
  // là "ra ngoài", để không tự đóng ngay khi vừa mở hoặc khi thao tác trong menu.
  useEffect(() => {
    if (!menuMoChoTinNhanId) return;

    function xuLyBamNgoai(su: MouseEvent) {
      const dich = su.target as HTMLElement;
      if (!dich.closest('.khung-tin-nhan__icon-noi')) {
        setMenuMoChoTinNhanId(null);
      }
    }

    function xuLyPhimEscape(su: KeyboardEvent) {
      if (su.key === 'Escape') setMenuMoChoTinNhanId(null);
    }

    document.addEventListener('mousedown', xuLyBamNgoai);
    document.addEventListener('keydown', xuLyPhimEscape);
    return () => {
      document.removeEventListener('mousedown', xuLyBamNgoai);
      document.removeEventListener('keydown', xuLyPhimEscape);
    };
  }, [menuMoChoTinNhanId]);

  // Đóng bảng emoji khi bấm ra ngoài hoặc bấm Escape — cùng cơ chế với menu "...".
  useEffect(() => {
    if (!hienBangEmoji) return;

    function xuLyBamNgoai(su: MouseEvent) {
      const dich = su.target as HTMLElement;
      if (!dich.closest('.khung-tin-nhan__emoji-cum')) {
        setHienBangEmoji(false);
      }
    }

    function xuLyPhimEscape(su: KeyboardEvent) {
      if (su.key === 'Escape') setHienBangEmoji(false);
    }

    document.addEventListener('mousedown', xuLyBamNgoai);
    document.addEventListener('keydown', xuLyPhimEscape);
    return () => {
      document.removeEventListener('mousedown', xuLyBamNgoai);
      document.removeEventListener('keydown', xuLyPhimEscape);
    };
  }, [hienBangEmoji]);

  // Đóng popup 6 cảm xúc khi bấm ra ngoài hoặc bấm Escape — cùng cơ chế
  // với menu "..."/bảng emoji ở trên (spec yêu cầu "giống dropdown menu
  // đã có"); trước khi có effect này, popup chỉ đóng khi chuột rời khỏi
  // chính popup nên có thể kẹt mở nếu người dùng di chuột ra ngoài theo
  // hướng khác.
  useEffect(() => {
    if (!popupCamXucChoTinNhanId) return;

    function xuLyBamNgoai(su: MouseEvent) {
      const dich = su.target as HTMLElement;
      if (!dich.closest('.khung-tin-nhan__cam-xuc-cum')) {
        setPopupCamXucChoTinNhanId(null);
      }
    }

    function xuLyPhimEscape(su: KeyboardEvent) {
      if (su.key === 'Escape') setPopupCamXucChoTinNhanId(null);
    }

    document.addEventListener('mousedown', xuLyBamNgoai);
    document.addEventListener('keydown', xuLyPhimEscape);
    return () => {
      document.removeEventListener('mousedown', xuLyBamNgoai);
      document.removeEventListener('keydown', xuLyPhimEscape);
    };
  }, [popupCamXucChoTinNhanId]);

  // Chèn emoji vào đúng vị trí con trỏ trong ô nhập (uncontrolled input —
  // thao tác trực tiếp qua ref) rồi focus lại và đóng bảng.
  function chenEmoji(emoji: string) {
    const oNhap = noiDungRef.current;
    if (!oNhap) return;
    const batDau = oNhap.selectionStart ?? oNhap.value.length;
    const ketThuc = oNhap.selectionEnd ?? oNhap.value.length;
    oNhap.value = oNhap.value.slice(0, batDau) + emoji + oNhap.value.slice(ketThuc);
    const viTriMoi = batDau + emoji.length;
    oNhap.focus();
    oNhap.setSelectionRange(viTriMoi, viTriMoi);
    setHienBangEmoji(false);
  }

  const tinDangTraLoi = danhSachTinNhan.find((tn) => tn.id === dangTraLoiId) ?? null;

  useEffect(() => {
    cuoiDanhSachRef.current?.scrollIntoView?.({ block: 'end' });
  }, [danhSachTinNhan]);

  function xuLySubmit(su: FormEvent) {
    su.preventDefault();
    const gtHienTai = noiDungRef.current?.value.trim();
    if (!gtHienTai) return;
    onGuiVanBan(gtHienTai, dangTraLoiId);
    if (noiDungRef.current) noiDungRef.current.value = '';
    setDangTraLoiId(null);
  }

  // Không kiểm tra kích thước file ở đây — component cha (TrangChat/TrangNhom)
  // đã kiểm tra giới hạn kích thước và tự set "loi" khi vượt quá, giữ đúng 1
  // nguồn sự thật cho thông báo lỗi hiển thị. Component này chỉ chuyển tiếp
  // file đã chọn.
  function xuLyChonTep(su: ChangeEvent<HTMLInputElement>) {
    const tep = su.target.files?.[0];
    if (!tep) return;
    onGuiTep(tep, dangTraLoiId);
    setDangTraLoiId(null);
  }

  // [Ctrl+V dán ảnh] Chỉ hỗ trợ dán ẢNH từ clipboard (chụp màn hình, copy
  // ảnh...) — KHÔNG hỗ trợ dán file khác (clipboard trình duyệt hầu như
  // không mang được file thường dạng "kind: file" trừ ảnh, và người dùng
  // yêu cầu rõ chỉ cần ảnh). Dán văn bản thường vẫn hoạt động bình thường
  // (không preventDefault) vì clipboardData.items không có mục "file" ảnh.
  function xuLyDanClipboard(su: ClipboardEvent<HTMLInputElement>) {
    const cacMuc = Array.from(su.clipboardData?.items ?? []);
    const mucAnh = cacMuc.find((muc) => muc.kind === 'file' && muc.type.startsWith('image/'));
    if (!mucAnh) return;

    const anh = mucAnh.getAsFile();
    if (!anh) return;

    su.preventDefault();
    onGuiTep(anh, dangTraLoiId);
    setDangTraLoiId(null);
  }

  return (
    <main className="khung-tin-nhan">
      {loi && (
        <p className="thong-bao-loi" role="alert">
          {loi}
        </p>
      )}
      <header className="khung-tin-nhan__tieu-de">
        {onQuayLai && (
          <button className="khung-tin-nhan__nut-quay-lai" onClick={onQuayLai} aria-label="Quay lại danh sách">
            ←
          </button>
        )}
        {hienOTimKiem ? (
          <div className="khung-tin-nhan__o-tim-kiem-cum">
            <input
              autoFocus
              type="text"
              className="khung-tin-nhan__o-tim-kiem"
              placeholder="Tìm tin nhắn..."
              value={tuKhoaTim}
              onChange={(su) => setTuKhoaTim(su.target.value)}
            />
            {(ketQuaTim.length > 0 || loiTim) && (
              <div className="khung-tin-nhan__ket-qua-tim">
                {loiTim && <p className="khung-tin-nhan__loi-tim">{loiTim}</p>}
                {ketQuaTim.map((tn) => (
                  <button
                    key={tn.id}
                    data-testid={`ket-qua-tim-${tn.id}`}
                    className="khung-tin-nhan__dong-ket-qua-tim"
                    onClick={() => moKetQuaTim(tn)}
                  >
                    <span className="khung-tin-nhan__ket-qua-ten">{layTenNguoiGui ? layTenNguoiGui(tn.nguoiGuiId) : 'một người dùng'}</span>
                    <span className="khung-tin-nhan__ket-qua-noi-dung">{tn.noiDungTinNhan}</span>
                    <span className="khung-tin-nhan__ket-qua-gio">{dinhDangGio(tn.thoiGianTao)}</span>
                  </button>
                ))}
              </div>
            )}
          </div>
        ) : (
          onBamTieuDe ? (
            <button className="khung-tin-nhan__tieu-de-bam" onClick={onBamTieuDe}>
              <Avatar id={tenHienThi} ten={tenHienThi} duongDanAnh={duongDanAnh} />
              <div className="khung-tin-nhan__ten-cum">
                <span className="khung-tin-nhan__ten">{tenHienThi}</span>
                {phuDe && <span className="khung-tin-nhan__phu-de">{phuDe}</span>}
              </div>
            </button>
          ) : (
            <>
              <Avatar id={tenHienThi} ten={tenHienThi} duongDanAnh={duongDanAnh} />
              <div className="khung-tin-nhan__ten-cum">
                <span className="khung-tin-nhan__ten">{tenHienThi}</span>
                {phuDe && <span className="khung-tin-nhan__phu-de">{phuDe}</span>}
              </div>
            </>
          )
        )}
        <button
          className="khung-tin-nhan__nut-tim-kiem"
          onClick={() => setHienOTimKiem((truoc) => !truoc)}
          aria-label="Tìm tin nhắn"
        >
          <BieuTuongTimKiem />
        </button>
        <button className="khung-tin-nhan__nut-kho-media" onClick={onMoKhoMedia} aria-label="Kho lưu trữ Media & Tệp">
          <BieuTuongKhoLuuTru />
        </button>
        {!dangKetNoi && <span className="khung-tin-nhan__mat-ket-noi">Mất kết nối realtime...</span>}
      </header>

      {danhSachTinNhanGhim.length > 0 && (
        <div className="khung-tin-nhan__banner-ghim">
          {danhSachTinNhanGhim.map((tn) => (
            <div key={tn.id} className="khung-tin-nhan__dong-ghim">
              <BieuTuongGhim />
              <span className="khung-tin-nhan__dong-ghim-noi-dung">
                {layTenNguoiGui ? layTenNguoiGui(tn.nguoiGuiId) : 'một người dùng'}: {trichNoiDungTinNhan(tn)}
              </span>
              <button onClick={() => onBoGhim(tn.id)} aria-label="Bỏ ghim">×</button>
            </div>
          ))}
        </div>
      )}

      <div className="khung-tin-nhan__danh-sach-tin-nhan">
        {coTheTaiThem && (
          <button className="khung-tin-nhan__nut-tai-them" onClick={onTaiThemLichSuCu} disabled={dangTaiLichSu}>
            {dangTaiLichSu ? 'Đang tải...' : 'Tải tin nhắn cũ hơn'}
          </button>
        )}
        {danhSachTinNhan.map((tn) => {
          const laCuaMinh = tn.nguoiGuiId === idHienTai;
          return (
            <div
              key={tn.id}
              ref={(el) => {
                if (el) thamChieuBongBongRef.current.set(tn.id, el);
                else thamChieuBongBongRef.current.delete(tn.id);
              }}
              className={`khung-tin-nhan__hang${laCuaMinh ? ' khung-tin-nhan__hang--minh' : ''}${tinDangMoId === tn.id ? ' khung-tin-nhan__hang--mo' : ''}${idDangNoiBat === tn.id ? ' khung-tin-nhan__hang--noi-bat' : ''}`}
            >
              <div className="khung-tin-nhan__icon-noi">
                {!tn.daThuHoi && (
                  <div className="khung-tin-nhan__cam-xuc-cum">
                    <button
                      type="button"
                      className="khung-tin-nhan__nut-cam-xuc"
                      aria-label="Thích tin nhắn này"
                      onClick={(su) => {
                        su.stopPropagation();
                        const daCoCuaMinh = tn.danhSachCamXuc.some((cx) => cx.nguoiDungId === idHienTai);
                        if (daCoCuaMinh) onBoCamXuc(tn.id);
                        else onThaCamXuc(tn.id, 'Thich');
                      }}
                      onMouseEnter={() => {
                        if (homGioHanCamXucRef.current) clearTimeout(homGioHanCamXucRef.current);
                        homGioHanCamXucRef.current = setTimeout(() => setPopupCamXucChoTinNhanId(tn.id), 400);
                      }}
                      onMouseLeave={() => {
                        if (homGioHanCamXucRef.current) clearTimeout(homGioHanCamXucRef.current);
                      }}
                    >
                      👍
                    </button>
                    {popupCamXucChoTinNhanId === tn.id && (
                      <div
                        className="khung-tin-nhan__popup-cam-xuc"
                        onMouseLeave={() => setPopupCamXucChoTinNhanId(null)}
                      >
                        {THU_TU_CAM_XUC.map((loai) => (
                          <button
                            key={loai}
                            type="button"
                            aria-label={`Thả cảm xúc ${loai}`}
                            onClick={(su) => {
                              su.stopPropagation();
                              onThaCamXuc(tn.id, loai);
                              setPopupCamXucChoTinNhanId(null);
                            }}
                          >
                            {EMOJI_CAM_XUC[loai]}
                          </button>
                        ))}
                      </div>
                    )}
                  </div>
                )}
                <button
                  type="button"
                  className="khung-tin-nhan__nut-tra-loi"
                  onClick={(su) => { su.stopPropagation(); setDangTraLoiId(tn.id); noiDungRef.current?.focus(); }}
                  aria-label="Trả lời tin nhắn này"
                >
                  <BieuTuongTraLoi />
                </button>
                <button
                  type="button"
                  className="khung-tin-nhan__nut-them"
                  onClick={(su) => { su.stopPropagation(); setMenuMoChoTinNhanId((truoc) => (truoc === tn.id ? null : tn.id)); }}
                  aria-label="Thêm tùy chọn"
                >
                  <BieuTuongBaCham />
                </button>
                {menuMoChoTinNhanId === tn.id && (
                  <div className="khung-tin-nhan__menu" onClick={(su) => su.stopPropagation()}>
                    {!tn.daThuHoi && tn.loaiTinNhan !== 'Text' && (
                      <a href={`${DIA_CHI_GOC}${tn.duongDanFile}`} download target="_blank" rel="noreferrer" onClick={() => setMenuMoChoTinNhanId(null)}>
                        Lưu về thiết bị
                      </a>
                    )}
                    {!tn.daThuHoi && !tn.daGhim && (
                      <button onClick={() => { onGhim(tn.id); setMenuMoChoTinNhanId(null); }}>Ghim</button>
                    )}
                    {!tn.daThuHoi && tn.daGhim && (
                      <button onClick={() => { onBoGhim(tn.id); setMenuMoChoTinNhanId(null); }}>Bỏ ghim</button>
                    )}
                    {laCuaMinh && !tn.daThuHoi && (
                      <button onClick={() => { onThuHoi(tn.id); setMenuMoChoTinNhanId(null); }}>Thu hồi tin nhắn</button>
                    )}
                    <button className="khung-tin-nhan__menu-nguy-hiem" onClick={() => { onAn(tn.id); setMenuMoChoTinNhanId(null); }}>Xóa</button>
                  </div>
                )}
              </div>
              <div
                className={`khung-tin-nhan__bong${laCuaMinh ? ' khung-tin-nhan__bong--minh' : ''}`}
                onClick={() => setTinDangMoId((truoc) => (truoc === tn.id ? null : tn.id))}
                role="button"
                tabIndex={0}
              >
                {tn.traLoi && (
                  <div className="khung-tin-nhan__trich-dan">
                    <span className="khung-tin-nhan__trich-dan-ten">{tn.traLoi.tenNguoiGui}</span>
                    <span className="khung-tin-nhan__trich-dan-noi-dung">{tn.traLoi.noiDungTomTat}</span>
                  </div>
                )}
                {tn.daThuHoi ? (
                  <span className="khung-tin-nhan__da-thu-hoi">Tin nhắn đã được thu hồi.</span>
                ) : (
                  <>
                    {tn.loaiTinNhan === 'Anh' && (
                      <img className="khung-tin-nhan__anh" src={`${DIA_CHI_GOC}${tn.duongDanFile}`} alt={tn.tenFileGoc ?? 'ảnh'} />
                    )}
                    {tn.loaiTinNhan === 'File' && (
                      <div className="khung-tin-nhan__file">
                        <span className="khung-tin-nhan__file-icon"><BieuTuongTaiLieu /></span>
                        <div className="khung-tin-nhan__file-thong-tin">
                          <span className="khung-tin-nhan__file-ten">{tn.tenFileGoc}</span>
                          <span className="khung-tin-nhan__file-size">{dinhDangKichThuoc(tn.kichThuocFile ?? 0)}</span>
                        </div>
                        <a
                          className="khung-tin-nhan__file-nut-tai"
                          href={`${DIA_CHI_GOC}${tn.duongDanFile}`}
                          target="_blank"
                          rel="noreferrer"
                          onClick={(su) => su.stopPropagation()}
                          aria-label={`Tải xuống ${tn.tenFileGoc}`}
                        >
                          <BieuTuongTai />
                        </a>
                      </div>
                    )}
                    {tn.loaiTinNhan === 'Text' && tn.noiDungTinNhan}
                  </>
                )}
                {!tn.daThuHoi && tn.danhSachCamXuc.length > 0 && (
                  <span className="khung-tin-nhan__badge-cam-xuc">
                    {Array.from(new Set(tn.danhSachCamXuc.map((cx) => cx.loaiCamXuc)))
                      .slice(0, 3)
                      .map((loai) => EMOJI_CAM_XUC[loai])
                      .join('')}
                    {' '}
                    {tn.danhSachCamXuc.length}
                  </span>
                )}
                {tinDangMoId === tn.id && (
                  <span className="khung-tin-nhan__thoi-gian">{dinhDangGio(tn.thoiGianTao)}</span>
                )}
              </div>
            </div>
          );
        })}
        <div ref={cuoiDanhSachRef} />
      </div>

      {tinDangTraLoi && (
        <div className="khung-tin-nhan__dang-tra-loi">
          <div className="khung-tin-nhan__dang-tra-loi-noi-dung">
            <span className="khung-tin-nhan__dang-tra-loi-tieu-de">
              ↩ Trả lời {layTenNguoiGui ? layTenNguoiGui(tinDangTraLoi.nguoiGuiId) : 'một người dùng'}
            </span>
            <span className="khung-tin-nhan__dang-tra-loi-trich">{trichNoiDungTinNhan(tinDangTraLoi)}</span>
          </div>
          <button type="button" className="khung-tin-nhan__dang-tra-loi-huy" onClick={() => setDangTraLoiId(null)} aria-label="Hủy trả lời">×</button>
        </div>
      )}
      <form className="khung-tin-nhan__form-gui" onSubmit={xuLySubmit}>
        <button
          type="button"
          className="khung-tin-nhan__nut-ghim"
          onClick={() => inputTepRef.current?.click()}
          disabled={!dangKetNoi || dangTaiTep}
          aria-label="Đính kèm file"
        >
          <BieuTuongGhim />
        </button>
        <input
          ref={inputTepRef}
          type="file"
          className="khung-tin-nhan__input-tep"
          accept="image/jpeg,image/png,image/gif,image/webp,application/pdf,.docx,.xlsx,.zip"
          onChange={(su) => {
            xuLyChonTep(su);
            su.target.value = '';
          }}
        />
        <input ref={noiDungRef} type="text" placeholder="Nhập tin nhắn..." disabled={!dangKetNoi} onPaste={xuLyDanClipboard} />
        <div className="khung-tin-nhan__emoji-cum">
          <button
            type="button"
            className="khung-tin-nhan__nut-emoji"
            onClick={() => setHienBangEmoji((truoc) => !truoc)}
            disabled={!dangKetNoi}
            aria-label="Chọn emoji"
          >
            <BieuTuongMatCuoi />
          </button>
          {hienBangEmoji && (
            <div className="khung-tin-nhan__bang-emoji" data-testid="bang-emoji">
              {DANH_SACH_EMOJI.map((emoji) => (
                <button key={emoji} type="button" onClick={() => chenEmoji(emoji)} aria-label={`Emoji ${emoji}`}>
                  {emoji}
                </button>
              ))}
            </div>
          )}
        </div>
        <button type="submit" disabled={!dangKetNoi}>
          Gửi
        </button>
      </form>
      {dangTaiTep && <p className="khung-tin-nhan__dang-tai-tep">Đang tải file lên...</p>}
    </main>
  );
}
