import { useState } from 'react';
import { BieuTuongKhoLuuTru, BieuTuongAnhMo, BieuTuongDong, BieuTuongTaiLieu, BieuTuongTai } from '../ThanhPhan/BieuTuong';
import { DIA_CHI_GOC } from '../DichVuApi';
import type { TinNhan } from '../KieuDuLieu';
import './PanelKhoMedia.css';

interface PropsPanelKhoMedia {
  danhSachMedia: TinNhan[];
  tenCuocTroChuyen: string;
  onDong: () => void;
}

function dinhDangKichThuoc(bytes: number): string {
  const mb = bytes / (1024 * 1024);
  return mb >= 1 ? `${mb.toFixed(1)}MB` : `${Math.ceil(bytes / 1024)}KB`;
}

export function PanelKhoMedia({ danhSachMedia, tenCuocTroChuyen, onDong }: PropsPanelKhoMedia) {
  const [tabDangChon, setTabDangChon] = useState<'anh' | 'tep'>('anh');
  const [anhDangXemToId, setAnhDangXemToId] = useState<string | null>(null);

  const danhSachAnh = danhSachMedia.filter((tn) => tn.loaiTinNhan === 'Anh');
  const danhSachTep = danhSachMedia.filter((tn) => tn.loaiTinNhan === 'File');
  const anhDangXemTo = danhSachAnh.find((tn) => tn.id === anhDangXemToId) ?? null;

  return (
    <div className="panel-kho-media-nen" onClick={onDong}>
      <div className="panel-kho-media" onClick={(su) => su.stopPropagation()}>
        <div className="panel-kho-media__dau">
          <span className="panel-kho-media__icon"><BieuTuongKhoLuuTru /></span>
          <div className="panel-kho-media__tieu-de-cum">
            <h3 className="panel-kho-media__tieu-de">Kho lưu trữ Media & Tệp</h3>
            <p className="panel-kho-media__phu-de">{tenCuocTroChuyen}</p>
          </div>
          <button className="panel-kho-media__dong" onClick={onDong} aria-label="Đóng"><BieuTuongDong /></button>
        </div>

        <div className="panel-kho-media__tab-cum">
          <button
            className={`panel-kho-media__tab${tabDangChon === 'anh' ? ' panel-kho-media__tab--chon' : ''}`}
            onClick={() => setTabDangChon('anh')}
          >
            Hình ảnh ({danhSachAnh.length})
          </button>
          <button
            className={`panel-kho-media__tab${tabDangChon === 'tep' ? ' panel-kho-media__tab--chon' : ''}`}
            onClick={() => setTabDangChon('tep')}
          >
            Tài liệu & Tệp ({danhSachTep.length})
          </button>
        </div>

        <div className="panel-kho-media__noi-dung">
          {tabDangChon === 'anh' && (
            danhSachAnh.length === 0 ? (
              <div className="panel-kho-media__trong">
                <BieuTuongAnhMo />
                <p>Chưa có hình ảnh nào được chia sẻ trong đoạn chat này</p>
              </div>
            ) : (
              <div className="panel-kho-media__luoi-anh">
                {danhSachAnh.map((tn) => (
                  <img
                    key={tn.id}
                    className="panel-kho-media__anh-nho"
                    src={`${DIA_CHI_GOC}${tn.duongDanFile}`}
                    alt={tn.tenFileGoc ?? 'ảnh'}
                    onClick={() => setAnhDangXemToId(tn.id)}
                  />
                ))}
              </div>
            )
          )}

          {tabDangChon === 'tep' && (
            danhSachTep.length === 0 ? (
              <div className="panel-kho-media__trong">
                <BieuTuongAnhMo />
                <p>Chưa có tài liệu nào được chia sẻ trong đoạn chat này</p>
              </div>
            ) : (
              <ul className="panel-kho-media__ds-tep">
                {danhSachTep.map((tn) => (
                  <li key={tn.id}>
                    <a href={`${DIA_CHI_GOC}${tn.duongDanFile}`} target="_blank" rel="noreferrer">
                      <span className="panel-kho-media__tep-icon"><BieuTuongTaiLieu /></span>
                      <div className="panel-kho-media__tep-thong-tin">
                        <span className="panel-kho-media__tep-ten">{tn.tenFileGoc}</span>
                        <span className="panel-kho-media__tep-size">{dinhDangKichThuoc(tn.kichThuocFile ?? 0)}</span>
                      </div>
                      <BieuTuongTai />
                    </a>
                  </li>
                ))}
              </ul>
            )
          )}
        </div>
      </div>

      {anhDangXemTo && (
        <div className="panel-kho-media__lightbox" onClick={() => setAnhDangXemToId(null)}>
          <img src={`${DIA_CHI_GOC}${anhDangXemTo.duongDanFile}`} alt={`Xem ảnh lớn ${anhDangXemTo.tenFileGoc}`} />
          <button className="panel-kho-media__lightbox-dong" onClick={() => setAnhDangXemToId(null)} aria-label="Đóng ảnh lớn">
            <BieuTuongDong />
          </button>
        </div>
      )}
    </div>
  );
}
