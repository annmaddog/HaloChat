import { useEffect, useRef, useState } from 'react';
import { DoiAnhDaiDien, DoiTenHienThi, DanhDauHoanTatHoSo, TaiLenTep } from '../DichVuApi';
import { useXacThuc } from '../NguCanh/NguCanhXacThuc';
import { Avatar } from './Avatar';
import { BieuTuongMayAnh, BieuTuongDong } from './BieuTuong';
import type { HoSoCaNhan } from '../KieuDuLieu';
import './ModalHoanTatHoSo.css';

const GIOI_HAN_TEN = 50;
const GIOI_HAN_ANH_BYTES = 5 * 1024 * 1024;

interface PropsModalHoanTatHoSo {
  tenHienThiBanDau: string;
  onDong: (hoSoMoi: HoSoCaNhan | null) => void;
}

export function ModalHoanTatHoSo({ tenHienThiBanDau, onDong }: PropsModalHoanTatHoSo) {
  const { token } = useXacThuc();
  const [tepAnhDaChon, setTepAnhDaChon] = useState<File | null>(null);
  const [duongDanPreview, setDuongDanPreview] = useState<string | null>(null);
  const [tenHienThi, setTenHienThi] = useState(tenHienThiBanDau);
  const [dangLuu, setDangLuu] = useState(false);
  const [loi, setLoi] = useState<string | null>(null);
  const inputTepRef = useRef<HTMLInputElement | null>(null);

  useEffect(() => {
    return () => {
      if (duongDanPreview) URL.revokeObjectURL(duongDanPreview);
    };
  }, [duongDanPreview]);

  function chonAnh(tep: File) {
    if (!tep.type.startsWith('image/')) {
      setLoi('Chỉ chấp nhận file ảnh.');
      return;
    }
    if (tep.size > GIOI_HAN_ANH_BYTES) {
      setLoi(`Ảnh vượt quá giới hạn ${GIOI_HAN_ANH_BYTES / 1024 / 1024}MB.`);
      return;
    }
    setLoi(null);
    if (duongDanPreview) URL.revokeObjectURL(duongDanPreview);
    setTepAnhDaChon(tep);
    setDuongDanPreview(URL.createObjectURL(tep));
  }

  async function xuLyHoanTat() {
    if (!token) return;
    setLoi(null);
    setDangLuu(true);
    try {
      let hoSoMoi: HoSoCaNhan | null = null;
      if (tepAnhDaChon) {
        const daTaiLen = await TaiLenTep(token, tepAnhDaChon);
        hoSoMoi = await DoiAnhDaiDien(token, daTaiLen.duongDanFile);
      }
      if (tenHienThi.trim() && tenHienThi.trim() !== tenHienThiBanDau) {
        hoSoMoi = await DoiTenHienThi(token, tenHienThi.trim());
      }
      hoSoMoi = await DanhDauHoanTatHoSo(token);
      onDong(hoSoMoi);
    } catch {
      setLoi('Lưu hồ sơ thất bại, vui lòng thử lại.');
    } finally {
      setDangLuu(false);
    }
  }

  async function xuLyDong() {
    if (!token) {
      onDong(null);
      return;
    }
    const hoSoMoi = await DanhDauHoanTatHoSo(token).catch(() => null);
    onDong(hoSoMoi);
  }

  return (
    <div className="modal-hoan-tat-ho-so-nen">
      <div className="modal-hoan-tat-ho-so">
        <button className="modal-hoan-tat-ho-so__dong" onClick={xuLyDong} aria-label="Đóng">
          <BieuTuongDong />
        </button>
        <h3>Hoàn tất hồ sơ</h3>
        <p className="modal-hoan-tat-ho-so__phu-de">
          Hãy thiết lập hồ sơ của bạn để mọi người có thể dễ dàng nhận ra bạn trong HaloChat.
        </p>

        {loi && <p className="thong-bao-loi" role="alert">{loi}</p>}

        <div className="modal-hoan-tat-ho-so__anh-cum">
          <div className="modal-hoan-tat-ho-so__anh-vong">
            <Avatar id={tenHienThiBanDau} ten={tenHienThi || '?'} kichThuoc="lon" duongDanAnh={duongDanPreview} />
            <button
              type="button"
              className="modal-hoan-tat-ho-so__nut-camera"
              aria-label="Đổi ảnh đại diện"
              onClick={() => inputTepRef.current?.click()}
            >
              <BieuTuongMayAnh />
            </button>
          </div>
          <label className="nut-phu modal-hoan-tat-ho-so__nut-chon-anh">
            Chọn ảnh
            <input
              ref={inputTepRef}
              type="file"
              accept="image/jpeg,image/png,image/gif,image/webp"
              hidden
              onChange={(su) => {
                const tep = su.target.files?.[0];
                if (tep) chonAnh(tep);
                su.target.value = '';
              }}
            />
          </label>
        </div>

        <label className="modal-hoan-tat-ho-so__nhan">
          Tên hiển thị
          <input
            type="text"
            value={tenHienThi}
            onChange={(su) => setTenHienThi(su.target.value)}
            placeholder="Nhập tên của bạn..."
            maxLength={GIOI_HAN_TEN}
            disabled={dangLuu}
          />
          <span className="modal-hoan-tat-ho-so__dem-ky-tu">{tenHienThi.length}/{GIOI_HAN_TEN}</span>
        </label>

        <button className="nut-chinh modal-hoan-tat-ho-so__nut-hoan-tat" onClick={xuLyHoanTat} disabled={dangLuu}>
          {dangLuu ? 'Đang lưu...' : 'Hoàn tất'}
        </button>
      </div>
    </div>
  );
}
