import { useEffect, useState } from 'react';
import { LayThongTinCaNhan, CapNhatCaiDat, LoiGoiApi } from '../DichVuApi';
import { useXacThuc } from '../NguCanh/NguCanhXacThuc';
import './TrangCaiDat.css';

export function TrangCaiDat() {
  const { token } = useXacThuc();
  const [choPhep, setChoPhep] = useState(false);
  const [dangTai, setDangTai] = useState(true);
  const [dangLuu, setDangLuu] = useState(false);
  const [daLuu, setDaLuu] = useState(false);
  const [loi, setLoi] = useState<string | null>(null);

  useEffect(() => {
    if (!token) return;
    LayThongTinCaNhan(token)
      .then((hoSo) => setChoPhep(hoSo.choPhepTinNhanTuNguoiLa))
      .catch((loiBat) => setLoi(loiBat instanceof Error ? loiBat.message : 'Không tải được cài đặt.'))
      .finally(() => setDangTai(false));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [token]);

  async function xuLyDoi(gtMoi: boolean) {
    if (!token) return;
    setChoPhep(gtMoi);
    setDangLuu(true);
    setDaLuu(false);
    try {
      await CapNhatCaiDat(token, gtMoi);
      setDaLuu(true);
    } catch (loiBat) {
      setChoPhep(!gtMoi);
      setLoi(loiBat instanceof LoiGoiApi ? loiBat.message : 'Lưu cài đặt thất bại.');
    } finally {
      setDangLuu(false);
    }
  }

  return (
    <div className="trang-cai-dat">
      <h2>Cài đặt</h2>
      {loi && (
        <p className="thong-bao-loi" role="alert">
          {loi}
        </p>
      )}
      <label className="trang-cai-dat__dong">
        <input
          type="checkbox"
          checked={choPhep}
          disabled={dangTai || dangLuu}
          onChange={(su) => xuLyDoi(su.target.checked)}
        />
        <span>Cho phép người lạ (chưa kết bạn) nhắn tin cho tôi</span>
      </label>
      {daLuu && <p className="trang-cai-dat__da-luu">Đã lưu.</p>}
    </div>
  );
}
