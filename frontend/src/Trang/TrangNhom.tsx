import { useEffect, useState } from 'react';
import {
  LayDanhSachNhom, TaoNhom, LayLichSuNhom, ThemThanhVien, XoaThanhVien, RoiNhom,
  LayDanhSachNguoiDung, LayBanBe, LoiGoiApi, TaiLenTep,
} from '../DichVuApi';
import { useXacThuc } from '../NguCanh/NguCanhXacThuc';
import { useChat } from '../NguCanh/NguCanhChat';
import { KhungTinNhan, type TinNhanHienThi } from '../ThanhPhan/KhungTinNhan';
import { Avatar } from '../ThanhPhan/Avatar';
import { BieuTuongBanBe, BieuTuongMayAnh } from '../ThanhPhan/BieuTuong';
import { PanelThongTinNhom } from './PanelThongTinNhom';
import { PanelQuanLyNhom } from './PanelQuanLyNhom';
import type { Nhom, NguoiDungTomTat } from '../KieuDuLieu';
import './TrangNhom.css';

const GIOI_HAN_ANH_BYTES = 5 * 1024 * 1024;
const GIOI_HAN_FILE_BYTES = 20 * 1024 * 1024;
// [Tải lịch sử] Xem giải thích ở TrangChat.tsx.
const SO_LUONG_LICH_SU_DAU = 20;
const SO_LUONG_LICH_SU_THEM = 30;

export function TrangNhom() {
  const { token, nguoiDungHienTai } = useXacThuc();
  const { ketNoi, dangKetNoi } = useChat();
  const idHienTai = nguoiDungHienTai?.id ?? '';

  const [danhSachNhom, setDanhSachNhom] = useState<Nhom[]>([]);
  const [nhomDangChonId, setNhomDangChonId] = useState<string | null>(null);
  const [tinNhanTheoNhom, setTinNhanTheoNhom] = useState<Record<string, TinNhanHienThi[]>>({});
  const [dangTaiDanhSach, setDangTaiDanhSach] = useState(true);
  const [dangTaiLichSu, setDangTaiLichSu] = useState(false);
  const [loi, setLoi] = useState<string | null>(null);
  const [dangTaiTep, setDangTaiTep] = useState(false);
  const [hienFormTao, setHienFormTao] = useState(false);
  const [tenNhomMoi, setTenNhomMoi] = useState('');
  const [tatCaNguoiDung, setTatCaNguoiDung] = useState<NguoiDungTomTat[]>([]);
  const [banBe, setBanBe] = useState<NguoiDungTomTat[]>([]);
  const [thanhVienDuocChon, setThanhVienDuocChon] = useState<Set<string>>(new Set());
  const [tuKhoaTimKiemThanhVien, setTuKhoaTimKiemThanhVien] = useState('');
  const [duongDanAnhNhomMoi, setDuongDanAnhNhomMoi] = useState<string | null>(null);
  const [dangTaiAnhNhomMoi, setDangTaiAnhNhomMoi] = useState(false);
  const [daTaiLichSuIds] = useState<Set<string>>(() => new Set());
  const [tuKhoaTimKiem, setTuKhoaTimKiem] = useState('');
  const [panelDangMo, setPanelDangMo] = useState<'khong' | 'thong-tin' | 'quan-ly'>('khong');
  const [conThemLichSu, setConThemLichSu] = useState<Record<string, boolean>>({});

  const nhomDangChon = danhSachNhom.find((n) => n.id === nhomDangChonId) ?? null;

  useEffect(() => {
    if (!token) return;
    Promise.all([LayDanhSachNhom(token), LayDanhSachNguoiDung(token), LayBanBe(token)])
      .then(([nhoms, nguoiDungs, banBes]) => {
        setDanhSachNhom(nhoms);
        setTatCaNguoiDung(nguoiDungs);
        setBanBe(banBes);
      })
      .catch((loiBat) => setLoi(loiBat instanceof Error ? loiBat.message : 'Không tải được danh sách nhóm.'))
      .finally(() => setDangTaiDanhSach(false));
  }, [token]);

  useEffect(() => {
    if (!token || !nhomDangChonId || daTaiLichSuIds.has(nhomDangChonId)) return;
    daTaiLichSuIds.add(nhomDangChonId);

    setDangTaiLichSu(true);
    LayLichSuNhom(token, nhomDangChonId, undefined, SO_LUONG_LICH_SU_DAU)
      .then((tinNhans) => {
        if (tinNhans.length < SO_LUONG_LICH_SU_DAU) {
          setConThemLichSu((truoc) => ({ ...truoc, [nhomDangChonId]: false }));
        }
        const thuTu = [...tinNhans].reverse();
        setTinNhanTheoNhom((truoc) => ({ ...truoc, [nhomDangChonId]: thuTu }));
      })
      .catch(() => {
        daTaiLichSuIds.delete(nhomDangChonId);
        setLoi('Không tải được lịch sử tin nhắn nhóm.');
      })
      .finally(() => setDangTaiLichSu(false));
  }, [token, nhomDangChonId, daTaiLichSuIds]);

  useEffect(() => {
    if (!ketNoi || !nhomDangChonId) return;
    ketNoi.invoke('DanhDauDaDoc', null, nhomDangChonId).catch(() => {});
  }, [ketNoi, nhomDangChonId]);

  useEffect(() => {
    if (!ketNoi) return;

    function xuLyTinNhanMoi(tinNhan: TinNhanHienThi) {
      if (!tinNhan.nhomId) return;
      setTinNhanTheoNhom((truoc) => ({
        ...truoc,
        [tinNhan.nhomId as string]: [...(truoc[tinNhan.nhomId as string] ?? []).filter((tn) => tn.id !== tinNhan.id), tinNhan],
      }));
    }

    function xuLyDuocThem(nhom: Nhom) {
      setDanhSachNhom((truoc) => (truoc.some((n) => n.id === nhom.id) ? truoc : [...truoc, nhom]));
    }

    function xuLyBiXoa(nhomId: string) {
      setDanhSachNhom((truoc) => truoc.filter((n) => n.id !== nhomId));
      setNhomDangChonId((truoc) => (truoc === nhomId ? null : truoc));
    }

    function xuLyCapNhat(nhom: Nhom) {
      setDanhSachNhom((truoc) => truoc.map((n) => (n.id === nhom.id ? nhom : n)));
    }

    ketNoi.on('NhanTinNhan', xuLyTinNhanMoi);
    ketNoi.on('DuocThemVaoNhom', xuLyDuocThem);
    ketNoi.on('BiXoaKhoiNhom', xuLyBiXoa);
    ketNoi.on('NhomDaGiaiTan', xuLyBiXoa);
    ketNoi.on('NhomDaCapNhat', xuLyCapNhat);
    return () => {
      ketNoi.off('NhanTinNhan', xuLyTinNhanMoi);
      ketNoi.off('DuocThemVaoNhom', xuLyDuocThem);
      ketNoi.off('BiXoaKhoiNhom', xuLyBiXoa);
      ketNoi.off('NhomDaGiaiTan', xuLyBiXoa);
      ketNoi.off('NhomDaCapNhat', xuLyCapNhat);
    };
  }, [ketNoi]);

  function taoNhomMoi() {
    if (!token || !tenNhomMoi.trim()) return;
    TaoNhom(token, tenNhomMoi.trim(), null, duongDanAnhNhomMoi, [...thanhVienDuocChon])
      .then((nhom) => {
        setDanhSachNhom((truoc) => [...truoc, nhom]);
        dongModalTao();
        setNhomDangChonId(nhom.id);
      })
      .catch((loiBat) => setLoi(loiBat instanceof LoiGoiApi ? loiBat.message : 'Tạo nhóm thất bại.'));
  }

  function dongModalTao() {
    setHienFormTao(false);
    setTenNhomMoi('');
    setThanhVienDuocChon(new Set());
    setTuKhoaTimKiemThanhVien('');
    setDuongDanAnhNhomMoi(null);
  }

  function doiAnhNhomMoi(tep: File) {
    if (!token) return;
    if (!tep.type.startsWith('image/')) {
      setLoi('Chỉ chấp nhận file ảnh.');
      return;
    }
    if (tep.size > GIOI_HAN_ANH_BYTES) {
      setLoi(`Ảnh vượt quá giới hạn ${GIOI_HAN_ANH_BYTES / 1024 / 1024}MB.`);
      return;
    }
    setDangTaiAnhNhomMoi(true);
    TaiLenTep(token, tep)
      .then((daTaiLen) => setDuongDanAnhNhomMoi(daTaiLen.duongDanFile))
      .catch(() => setLoi('Tải ảnh đại diện nhóm thất bại.'))
      .finally(() => setDangTaiAnhNhomMoi(false));
  }

  function guiTinNhanVanBan(noiDungGui: string) {
    if (!ketNoi || !nhomDangChon) return;
    ketNoi
      .invoke<TinNhanHienThi>('GuiTinNhan', null, nhomDangChon.id, 'Text', noiDungGui, null, null, null, null)
      .then((tinNhanDaGui) => {
        setTinNhanTheoNhom((truoc) => ({ ...truoc, [nhomDangChon.id]: [...(truoc[nhomDangChon.id] ?? []), tinNhanDaGui] }));
      })
      .catch((loiBat) => setLoi(loiBat instanceof Error ? loiBat.message : 'Gửi tin nhắn thất bại.'));
  }

  function guiTep(tep: File) {
    if (!ketNoi || !nhomDangChon || !token) return;

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
          'GuiTinNhan', null, nhomDangChon.id, tep.type.startsWith('image/') ? 'Anh' : 'File', '',
          daTaiLen.duongDanFile, daTaiLen.tenFileGoc, daTaiLen.kichThuocFile, daTaiLen.loaiFile,
        ),
      )
      .then((tinNhanDaGui) => {
        setTinNhanTheoNhom((truoc) => ({ ...truoc, [nhomDangChon.id]: [...(truoc[nhomDangChon.id] ?? []), tinNhanDaGui] }));
      })
      .catch(() => setLoi('Gửi file thất bại.'))
      .finally(() => setDangTaiTep(false));
  }

  function taiThemLichSuCu() {
    if (!token || !nhomDangChon) return;
    const cuNhat = (tinNhanTheoNhom[nhomDangChon.id] ?? [])[0];
    if (!cuNhat) return;
    setDangTaiLichSu(true);
    LayLichSuNhom(token, nhomDangChon.id, cuNhat.id, SO_LUONG_LICH_SU_THEM)
      .then((cuHon) => {
        if (cuHon.length < SO_LUONG_LICH_SU_THEM) {
          setConThemLichSu((truoc) => ({ ...truoc, [nhomDangChon.id]: false }));
        }
        const thuTu = [...cuHon].reverse();
        setTinNhanTheoNhom((truoc) => ({ ...truoc, [nhomDangChon.id]: [...thuTu, ...(truoc[nhomDangChon.id] ?? [])] }));
      })
      .catch(() => setLoi('Không tải được tin nhắn cũ hơn.'))
      .finally(() => setDangTaiLichSu(false));
  }

  function themThanhVien(userId: string) {
    if (!token || !nhomDangChon) return;
    ThemThanhVien(token, nhomDangChon.id, userId)
      .then((nhom) => setDanhSachNhom((truoc) => truoc.map((n) => (n.id === nhom.id ? nhom : n))))
      .catch((loiBat) => setLoi(loiBat instanceof LoiGoiApi ? loiBat.message : 'Thêm thành viên thất bại.'));
  }

  function xoaThanhVien(userId: string) {
    if (!token || !nhomDangChon) return;
    XoaThanhVien(token, nhomDangChon.id, userId)
      .then((nhom) => setDanhSachNhom((truoc) => truoc.map((n) => (n.id === nhom.id ? nhom : n))))
      .catch((loiBat) => setLoi(loiBat instanceof LoiGoiApi ? loiBat.message : 'Xóa thành viên thất bại.'));
  }

  function roiNhom() {
    if (!token || !nhomDangChon) return;
    const thongDiepXacNhan = laAdmin
      ? 'Giải tán nhóm sẽ xóa nhóm vĩnh viễn cho mọi thành viên. Bạn có chắc chắn?'
      : 'Bạn có chắc muốn rời nhóm?';
    if (!window.confirm(thongDiepXacNhan)) return;
    RoiNhom(token, nhomDangChon.id)
      .then(() => {
        setDanhSachNhom((truoc) => truoc.filter((n) => n.id !== nhomDangChon.id));
        setNhomDangChonId(null);
      })
      .catch((loiBat) => setLoi(loiBat instanceof LoiGoiApi ? loiBat.message : 'Rời nhóm thất bại.'));
  }

  const laAdmin = nhomDangChon?.nguoiTaoId === idHienTai;

  return (
    <div className={`trang-nhom${nhomDangChon ? ' trang-nhom--da-chon' : ''}`}>
      <aside className="trang-nhom__sidebar">
        <button className="trang-nhom__nut-tao" onClick={() => setHienFormTao(true)}>
          + Tạo nhóm
        </button>
        <input
          type="text"
          className="trang-nhom__tim-kiem"
          placeholder="Tìm nhóm..."
          value={tuKhoaTimKiem}
          onChange={(su) => setTuKhoaTimKiem(su.target.value)}
        />
        {dangTaiDanhSach && <p>Đang tải...</p>}
        <ul className="trang-nhom__danh-sach">
          {danhSachNhom
            .filter((n) => n.tenNhom.toLowerCase().includes(tuKhoaTimKiem.toLowerCase()))
            .map((n) => (
            <li key={n.id}>
              <button
                className={`trang-nhom__muc${nhomDangChonId === n.id ? ' trang-nhom__muc--dang-chon' : ''}`}
                onClick={() => {
                  setNhomDangChonId(n.id);
                  ketNoi?.invoke('DanhDauDaDoc', null, n.id).catch(() => {});
                  setPanelDangMo('khong');
                }}
              >
                <Avatar id={n.id} ten={n.tenNhom} kichThuoc="nho" duongDanAnh={n.duongDanAnhDaiDien} />
                <div className="trang-nhom__ten-cum">
                  <span className="trang-nhom__ten">{n.tenNhom}</span>
                  <span className="trang-nhom__so-thanh-vien">{n.thanhVien.length} thành viên</span>
                </div>
              </button>
            </li>
          ))}
        </ul>
      </aside>

      {!nhomDangChon && (
        <div className="trang-nhom__trong-rong">
          <p className="trang-nhom__trong-tieu-de">Chào mừng đến HaloChat</p>
          <p>Chọn 1 nhóm hoặc tạo nhóm mới để bắt đầu.</p>
        </div>
      )}
      {nhomDangChon && (
        <div className="trang-nhom__khung-phai">
          <KhungTinNhan
            loaiHoiThoai="nhom"
            tenHienThi={nhomDangChon.tenNhom}
            phuDe={`${nhomDangChon.thanhVien.length} thành viên`}
            danhSachTinNhan={tinNhanTheoNhom[nhomDangChon.id] ?? []}
            idHienTai={idHienTai}
            dangKetNoi={dangKetNoi}
            dangTaiLichSu={dangTaiLichSu}
            coTheTaiThem={conThemLichSu[nhomDangChon.id] !== false}
            onTaiThemLichSuCu={taiThemLichSuCu}
            onGuiVanBan={guiTinNhanVanBan}
            onGuiTep={guiTep}
            dangTaiTep={dangTaiTep}
            loi={loi}
            onQuayLai={() => setNhomDangChonId(null)}
            onBamTieuDe={() => setPanelDangMo('thong-tin')}
          />
          {panelDangMo === 'thong-tin' && (
            <PanelThongTinNhom
              nhom={nhomDangChon}
              laAdmin={laAdmin}
              onDong={() => setPanelDangMo('khong')}
              onMoQuanLy={() => setPanelDangMo('quan-ly')}
              onRoiNhom={roiNhom}
            />
          )}
          {panelDangMo === 'quan-ly' && laAdmin && (
            <PanelQuanLyNhom
              nhom={nhomDangChon}
              tatCaNguoiDung={tatCaNguoiDung}
              onDong={() => setPanelDangMo('thong-tin')}
              onThemThanhVien={themThanhVien}
              onXoaThanhVien={xoaThanhVien}
              onCapNhatNhom={(nhomMoi) => setDanhSachNhom((truoc) => truoc.map((n) => (n.id === nhomMoi.id ? nhomMoi : n)))}
            />
          )}
        </div>
      )}

      {hienFormTao && (
        <div className="trang-nhom__modal-nen" onClick={dongModalTao}>
          <div className="trang-nhom__modal" onClick={(su) => su.stopPropagation()}>
            <div className="trang-nhom__modal-dau">
              <span className="trang-nhom__modal-icon"><BieuTuongBanBe /></span>
              <div className="trang-nhom__modal-tieu-de-cum">
                <h3 className="trang-nhom__modal-tieu-de">Tạo nhóm chat mới</h3>
                <p className="trang-nhom__modal-phu-de">Tạo không gian trò chuyện cùng bạn bè</p>
              </div>
              <button className="trang-nhom__modal-dong" onClick={dongModalTao} aria-label="Đóng">×</button>
            </div>
            <hr className="trang-nhom__modal-chia" />

            <div className="trang-nhom__modal-anh-ten">
              <label className="trang-nhom__modal-anh-upload">
                {dangTaiAnhNhomMoi ? (
                  '...'
                ) : duongDanAnhNhomMoi ? (
                  <Avatar id="nhom-moi" ten={tenNhomMoi || '?'} duongDanAnh={duongDanAnhNhomMoi} />
                ) : (
                  <BieuTuongMayAnh />
                )}
                <input
                  type="file"
                  accept="image/jpeg,image/png,image/gif,image/webp"
                  hidden
                  disabled={dangTaiAnhNhomMoi}
                  onChange={(su) => {
                    const tep = su.target.files?.[0];
                    if (tep) doiAnhNhomMoi(tep);
                    su.target.value = '';
                  }}
                />
              </label>
              <label className="trang-nhom__modal-nhan-ten">
                Tên nhóm chat
                <input
                  type="text"
                  placeholder="Nhập tên nhóm trò chuyện..."
                  value={tenNhomMoi}
                  onChange={(su) => setTenNhomMoi(su.target.value)}
                />
              </label>
            </div>

            <div className="trang-nhom__modal-nhan-thanh-vien">
              <span>Thêm thành viên</span>
              <span className="trang-nhom__modal-badge">{thanhVienDuocChon.size} đã chọn</span>
            </div>
            <input
              type="text"
              className="trang-nhom__modal-tim-thanh-vien"
              placeholder="Tìm bạn bè..."
              value={tuKhoaTimKiemThanhVien}
              onChange={(su) => setTuKhoaTimKiemThanhVien(su.target.value)}
            />
            <div className="trang-nhom__modal-ds-thanh-vien">
              {banBe.length === 0 && (
                <div className="trang-nhom__modal-trong">
                  <BieuTuongBanBe />
                  <p>Bạn chưa có bạn bè nào. Hãy kết bạn trước khi tạo nhóm!</p>
                </div>
              )}
              {banBe.length > 0 && (
                <ul className="trang-nhom__chon-thanh-vien">
                  {banBe
                    .filter((nd) => nd.tenHienThi.toLowerCase().includes(tuKhoaTimKiemThanhVien.trim().toLowerCase()))
                    .map((nd) => (
                      <li key={nd.id}>
                        <label className="trang-nhom__modal-hang-thanh-vien">
                          <input
                            type="checkbox"
                            checked={thanhVienDuocChon.has(nd.id)}
                            onChange={(su) => {
                              setThanhVienDuocChon((truoc) => {
                                const moi = new Set(truoc);
                                if (su.target.checked) moi.add(nd.id); else moi.delete(nd.id);
                                return moi;
                              });
                            }}
                          />
                          <Avatar id={nd.id} ten={nd.tenHienThi} kichThuoc="nho" />
                          <span>{nd.tenHienThi}</span>
                        </label>
                      </li>
                    ))}
                </ul>
              )}
            </div>

            <div className="trang-nhom__modal-hanh-dong">
              <button className="nut-phu" onClick={dongModalTao}>Hủy</button>
              <button className="nut-chinh" onClick={taoNhomMoi} disabled={!tenNhomMoi.trim()}>Tạo nhóm</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
