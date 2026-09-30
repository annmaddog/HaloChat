import { useEffect, useState } from 'react';
import { LayThongTinKyThuat, LoiGoiApi } from '../DichVuApi';
import { BieuTuongDong } from './BieuTuong';
import type { ThongTinKyThuat } from '../KieuDuLieu';
import './ModalThongTinKyThuat.css';

interface PropsModalThongTinKyThuat {
  token: string;
  tinNhanId: string;
  onDong: () => void;
}

export function ModalThongTinKyThuat({ token, tinNhanId, onDong }: PropsModalThongTinKyThuat) {
  const [thongTin, setThongTin] = useState<ThongTinKyThuat | null>(null);
  const [dangTai, setDangTai] = useState(true);
  const [loi, setLoi] = useState<string | null>(null);

  useEffect(() => {
    setDangTai(true);
    setLoi(null);
    LayThongTinKyThuat(token, tinNhanId)
      .then(setThongTin)
      .catch((loiBat) => setLoi(loiBat instanceof LoiGoiApi ? loiBat.message : 'Không tải được thông tin kỹ thuật.'))
      .finally(() => setDangTai(false));
  }, [token, tinNhanId]);

  return (
    <div className="modal-thong-tin-ky-thuat__nen" onClick={onDong}>
      <div className="modal-thong-tin-ky-thuat__hop" onClick={(su) => su.stopPropagation()}>
        <div className="modal-thong-tin-ky-thuat__dau">
          <h2>Thông tin kỹ thuật</h2>
          <button type="button" onClick={onDong} aria-label="Đóng"><BieuTuongDong /></button>
        </div>

        {dangTai && <p className="modal-thong-tin-ky-thuat__trang-thai">Đang tải...</p>}
        {loi && <p className="thong-bao-loi" role="alert">{loi}</p>}

        {thongTin && !thongTin.apDungDuoc && (
          <p className="modal-thong-tin-ky-thuat__trang-thai">Không áp dụng cho loại tin nhắn này.</p>
        )}

        {thongTin && thongTin.apDungDuoc && (
          <>
            <section className="modal-thong-tin-ky-thuat__muc">
              <div className="modal-thong-tin-ky-thuat__muc-dau">
                <span>🔒 Mã hóa</span>
                <span className={`modal-thong-tin-ky-thuat__badge ${thongTin.daMaHoa ? 'modal-thong-tin-ky-thuat__badge--xanh' : ''}`}>
                  {thongTin.daMaHoa ? 'Đã mã hóa' : 'Không mã hóa'}
                </span>
              </div>
              {thongTin.daMaHoa && (
                <dl className="modal-thong-tin-ky-thuat__ds">
                  <dt>Thuật toán</dt><dd>{thongTin.thuatToanMaHoa}</dd>
                  {thongTin.kichThuocGocByte !== null && (<><dt>Kích thước gốc</dt><dd>{thongTin.kichThuocGocByte} byte</dd></>)}
                  {thongTin.kichThuocMaHoaByte !== null && (
                    <>
                      <dt>Kích thước sau mã hóa</dt>
                      <dd>{thongTin.kichThuocMaHoaByte} byte{thongTin.tyLePhinh !== null && ` (~${thongTin.tyLePhinh.toFixed(2)} lần)`}</dd>
                    </>
                  )}
                  {thongTin.ciphertextRutGon && (<><dt>Bản mã</dt><dd className="modal-thong-tin-ky-thuat__ma">{thongTin.ciphertextRutGon}</dd></>)}
                  {thongTin.nonceRutGon && (<><dt>Nonce</dt><dd className="modal-thong-tin-ky-thuat__ma">{thongTin.nonceRutGon}</dd></>)}
                  {thongTin.authTagRutGon && (<><dt>AuthTag</dt><dd className="modal-thong-tin-ky-thuat__ma">{thongTin.authTagRutGon}</dd></>)}
                </dl>
              )}
            </section>

            {thongTin.daMaHoa && (thongTin.thoiGianMaHoaMs !== null || thongTin.thoiGianGiaiMaMs !== null) && (
              <section className="modal-thong-tin-ky-thuat__muc">
                <div className="modal-thong-tin-ky-thuat__muc-dau"><span>⏱ Thời gian (đo lại ngay lúc bấm xem)</span></div>
                <dl className="modal-thong-tin-ky-thuat__ds">
                  {thongTin.thoiGianMaHoaMs !== null && (<><dt>Mã hóa lại để đo</dt><dd>{thongTin.thoiGianMaHoaMs.toFixed(3)} ms</dd></>)}
                  {thongTin.thoiGianGiaiMaMs !== null && (<><dt>Giải mã</dt><dd>{thongTin.thoiGianGiaiMaMs.toFixed(3)} ms</dd></>)}
                </dl>
              </section>
            )}

            <section className="modal-thong-tin-ky-thuat__muc">
              <div className="modal-thong-tin-ky-thuat__muc-dau">
                <span>✒ Chữ ký số</span>
                <span className={`modal-thong-tin-ky-thuat__badge ${thongTin.daXacThucChuKy === true ? 'modal-thong-tin-ky-thuat__badge--xanh' : thongTin.daXacThucChuKy === false ? 'modal-thong-tin-ky-thuat__badge--do' : ''}`}>
                  {thongTin.daXacThucChuKy === true ? '✓ Hợp lệ' : thongTin.daXacThucChuKy === false ? 'Không hợp lệ' : 'Không có chữ ký'}
                </span>
              </div>
              {thongTin.coChuKy && (
                <dl className="modal-thong-tin-ky-thuat__ds">
                  <dt>Thuật toán</dt><dd>{thongTin.thuatToanChuKy}</dd>
                </dl>
              )}
            </section>
          </>
        )}
      </div>
    </div>
  );
}
