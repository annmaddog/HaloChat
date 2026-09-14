import { useCallback, useEffect, useState } from 'react';
import { LayDanhSachHoiThoai, LayLoiMoiDen, LaySoTinNhomChuaDoc, LayThongTinCaNhan } from '../DichVuApi';
import { useXacThuc } from './NguCanhXacThuc';
import { useChat } from './NguCanhChat';

export interface SoLuongChuaDoc {
  tinNhan: number;
  banBe: number;
  nhom: number;
}

const RONG: SoLuongChuaDoc = { tinNhan: 0, banBe: 0, nhom: 0 };

export function SuDungSoLuongChuaDoc(): SoLuongChuaDoc {
  const { token } = useXacThuc();
  const { ketNoi } = useChat();
  const [soLuong, setSoLuong] = useState<SoLuongChuaDoc>(RONG);

  const taiLai = useCallback(() => {
    if (!token) {
      setSoLuong(RONG);
      return;
    }

    LayThongTinCaNhan(token)
      .then((hoSo) =>
        Promise.all([
          hoSo.thongBaoTinNhanMoi ? LayDanhSachHoiThoai(token) : Promise.resolve([]),
          hoSo.thongBaoLoiMoiKetBan ? LayLoiMoiDen(token) : Promise.resolve([]),
          hoSo.thongBaoNhom ? LaySoTinNhomChuaDoc(token) : Promise.resolve({ soTinChuaDoc: 0 }),
        ]).then(([hoiThoai, loiMoi, nhom]) => {
          setSoLuong({
            tinNhan: hoiThoai.reduce((tong, h) => tong + h.soTinChuaDoc, 0),
            banBe: loiMoi.length,
            nhom: nhom.soTinChuaDoc,
          });
        }),
      )
      .catch(() => {});
  }, [token]);

  useEffect(() => {
    taiLai();
  }, [taiLai]);

  useEffect(() => {
    if (!ketNoi) return;

    ketNoi.on('NhanTinNhan', taiLai);
    ketNoi.on('NhanLoiMoiKetBan', taiLai);
    ketNoi.on('LoiMoiKetBanDuocChapNhan', taiLai);
    ketNoi.on('NhomDaCapNhat', taiLai);
    return () => {
      ketNoi.off('NhanTinNhan', taiLai);
      ketNoi.off('NhanLoiMoiKetBan', taiLai);
      ketNoi.off('LoiMoiKetBanDuocChapNhan', taiLai);
      ketNoi.off('NhomDaCapNhat', taiLai);
    };
  }, [ketNoi, taiLai]);

  return soLuong;
}
