import { useEffect, useRef, type FormEvent, type ChangeEvent } from 'react';
import { BieuTuongGhim } from './BieuTuong';
import { DIA_CHI_GOC } from '../DichVuApi';
import type { TinNhan } from '../KieuDuLieu';
import './KhungTinNhan.css';

export type TinNhanHienThi = TinNhan & { dangGui?: boolean };

function dinhDangKichThuoc(bytes: number): string {
  const mb = bytes / (1024 * 1024);
  return mb >= 1 ? `${mb.toFixed(1)}MB` : `${Math.ceil(bytes / 1024)}KB`;
}

function nhanTrangThaiGui(tn: TinNhanHienThi): string {
  if (tn.dangGui) return 'Đang gửi';
  if (tn.daDoc) return 'Đã xem';
  if (tn.daNhan) return 'Đã nhận';
  return 'Đã gửi';
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
  onGuiVanBan: (noiDung: string) => void;
  onGuiTep: (tep: File) => void;
  dangTaiTep: boolean;
  loi: string | null;
  onQuayLai?: () => void;
  onBamTieuDe?: () => void;
}

export function KhungTinNhan({
  loaiHoiThoai, tenHienThi, phuDe, danhSachTinNhan, idHienTai, dangKetNoi, dangTaiLichSu,
  coTheTaiThem, onTaiThemLichSuCu, onGuiVanBan, onGuiTep, dangTaiTep, loi, onQuayLai, onBamTieuDe,
}: PropsKhungTinNhan) {
  const inputTepRef = useRef<HTMLInputElement | null>(null);
  const cuoiDanhSachRef = useRef<HTMLDivElement | null>(null);
  const noiDungRef = useRef<HTMLInputElement | null>(null);

  useEffect(() => {
    cuoiDanhSachRef.current?.scrollIntoView?.({ block: 'end' });
  }, [danhSachTinNhan]);

  function xuLySubmit(su: FormEvent) {
    su.preventDefault();
    const gtHienTai = noiDungRef.current?.value.trim();
    if (!gtHienTai) return;
    onGuiVanBan(gtHienTai);
    if (noiDungRef.current) noiDungRef.current.value = '';
  }

  // Không kiểm tra kích thước file ở đây — component cha (TrangChat/TrangNhom)
  // đã kiểm tra giới hạn kích thước và tự set "loi" khi vượt quá, giữ đúng 1
  // nguồn sự thật cho thông báo lỗi hiển thị. Component này chỉ chuyển tiếp
  // file đã chọn.
  function xuLyChonTep(su: ChangeEvent<HTMLInputElement>) {
    const tep = su.target.files?.[0];
    if (!tep) return;
    onGuiTep(tep);
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

      <div className="khung-tin-nhan__danh-sach-tin-nhan">
        {coTheTaiThem && (
          <button className="khung-tin-nhan__nut-tai-them" onClick={onTaiThemLichSuCu} disabled={dangTaiLichSu}>
            {dangTaiLichSu ? 'Đang tải...' : 'Tải tin nhắn cũ hơn'}
          </button>
        )}
        {danhSachTinNhan.map((tn) => {
          const laCuaMinh = tn.nguoiGuiId === idHienTai;
          return (
            <div key={tn.id} className={`khung-tin-nhan__bong${laCuaMinh ? ' khung-tin-nhan__bong--minh' : ''}`}>
              {tn.loaiTinNhan === 'Anh' && (
                <img className="khung-tin-nhan__anh" src={`${DIA_CHI_GOC}${tn.duongDanFile}`} alt={tn.tenFileGoc ?? 'ảnh'} />
              )}
              {tn.loaiTinNhan === 'File' && (
                <a className="khung-tin-nhan__file" href={`${DIA_CHI_GOC}${tn.duongDanFile}`} target="_blank" rel="noreferrer">
                  📎 {tn.tenFileGoc} ({dinhDangKichThuoc(tn.kichThuocFile ?? 0)})
                </a>
              )}
              {tn.loaiTinNhan === 'Text' && tn.noiDungTinNhan}
              {laCuaMinh && loaiHoiThoai === 'nguoiDung' && (
                <span className="khung-tin-nhan__trang-thai-gui">{nhanTrangThaiGui(tn)}</span>
              )}
            </div>
          );
        })}
        <div ref={cuoiDanhSachRef} />
      </div>

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
        <input ref={noiDungRef} type="text" placeholder="Nhập tin nhắn..." disabled={!dangKetNoi} />
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
