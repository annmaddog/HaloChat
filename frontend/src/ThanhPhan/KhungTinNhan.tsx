import { useEffect, useRef, useState, type FormEvent, type ChangeEvent, type ClipboardEvent } from 'react';
import { BieuTuongGhim, BieuTuongTraLoi, BieuTuongTaiLieu, BieuTuongTai, BieuTuongBaCham, BieuTuongMatCuoi } from './BieuTuong';
import { DIA_CHI_GOC } from '../DichVuApi';
import type { TinNhan } from '../KieuDuLieu';
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
  return ngay.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });
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
}

export function KhungTinNhan({
  tenHienThi, phuDe, danhSachTinNhan, idHienTai, dangKetNoi, dangTaiLichSu,
  coTheTaiThem, onTaiThemLichSuCu, onGuiVanBan, onGuiTep, dangTaiTep, loi, onQuayLai, onBamTieuDe, layTenNguoiGui,
  onThuHoi, onGhim, onBoGhim, onAn, danhSachTinNhanGhim,
}: PropsKhungTinNhan) {
  const inputTepRef = useRef<HTMLInputElement | null>(null);
  const cuoiDanhSachRef = useRef<HTMLDivElement | null>(null);
  const noiDungRef = useRef<HTMLInputElement | null>(null);
  const [tinDangMoId, setTinDangMoId] = useState<string | null>(null);
  const [dangTraLoiId, setDangTraLoiId] = useState<string | null>(null);
  const [menuMoChoTinNhanId, setMenuMoChoTinNhanId] = useState<string | null>(null);
  const [hienBangEmoji, setHienBangEmoji] = useState(false);

  useEffect(() => {
    setDangTraLoiId(null);
  }, [tenHienThi]);

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
        {onBamTieuDe ? (
          <button className="khung-tin-nhan__tieu-de-bam" onClick={onBamTieuDe}>
            <span className="khung-tin-nhan__avatar">{tenHienThi.charAt(0).toUpperCase()}</span>
            <div className="khung-tin-nhan__ten-cum">
              <span className="khung-tin-nhan__ten">{tenHienThi}</span>
              {phuDe && <span className="khung-tin-nhan__phu-de">{phuDe}</span>}
            </div>
          </button>
        ) : (
          <>
            <span className="khung-tin-nhan__avatar">{tenHienThi.charAt(0).toUpperCase()}</span>
            <div className="khung-tin-nhan__ten-cum">
              <span className="khung-tin-nhan__ten">{tenHienThi}</span>
              {phuDe && <span className="khung-tin-nhan__phu-de">{phuDe}</span>}
            </div>
          </>
        )}
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
            <div key={tn.id} className={`khung-tin-nhan__hang${laCuaMinh ? ' khung-tin-nhan__hang--minh' : ''}${tinDangMoId === tn.id ? ' khung-tin-nhan__hang--mo' : ''}`}>
              <div className="khung-tin-nhan__icon-noi">
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
      {/* Chỉ là dòng chữ trình bày theo mockup — mã hóa thật thuộc GĐ6, KHÔNG được gọi ở đây. */}
      <p className="khung-tin-nhan__ma-hoa">🔒 Được mã hóa bằng AES-256-GCM</p>
    </main>
  );
}
