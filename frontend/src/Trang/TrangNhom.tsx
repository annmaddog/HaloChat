import { useEffect, useState } from 'react';
import {
  LayDanhSachNhom, TaoNhom, LayLichSuNhom, ThemThanhVien, XoaThanhVien, RoiNhom,
  LayDanhSachNguoiDung, LoiGoiApi,
} from '../DichVuApi';
import { useXacThuc } from '../NguCanh/NguCanhXacThuc';
import { useChat } from '../NguCanh/NguCanhChat';
import { KhungTinNhan, type TinNhanHienThi } from '../ThanhPhan/KhungTinNhan';
import type { Nhom, NguoiDungTomTat } from '../KieuDuLieu';
import './TrangNhom.css';

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
  const [thanhVienDuocChon, setThanhVienDuocChon] = useState<Set<string>>(new Set());
  const [daTaiLichSuIds] = useState<Set<string>>(() => new Set());

  const nhomDangChon = danhSachNhom.find((n) => n.id === nhomDangChonId) ?? null;

  useEffect(() => {
    if (!token) return;
    Promise.all([LayDanhSachNhom(token), LayDanhSachNguoiDung(token)])
      .then(([nhoms, nguoiDungs]) => {
        setDanhSachNhom(nhoms);
        setTatCaNguoiDung(nguoiDungs);
      })
      .catch((loiBat) => setLoi(loiBat instanceof Error ? loiBat.message : 'Không tải được danh sách nhóm.'))
      .finally(() => setDangTaiDanhSach(false));
  }, [token]);

  useEffect(() => {
    if (!token || !nhomDangChonId || daTaiLichSuIds.has(nhomDangChonId)) return;
    daTaiLichSuIds.add(nhomDangChonId);

    setDangTaiLichSu(true);
    LayLichSuNhom(token, nhomDangChonId)
      .then((tinNhans) => {
        const thuTu = [...tinNhans].reverse();
        setTinNhanTheoNhom((truoc) => ({ ...truoc, [nhomDangChonId]: thuTu }));
      })
      .catch(() => setLoi('Không tải được lịch sử tin nhắn nhóm.'))
      .finally(() => setDangTaiLichSu(false));
  }, [token, nhomDangChonId, daTaiLichSuIds]);

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

    ketNoi.on('NhanTinNhan', xuLyTinNhanMoi);
    ketNoi.on('DuocThemVaoNhom', xuLyDuocThem);
    ketNoi.on('BiXoaKhoiNhom', xuLyBiXoa);
    ketNoi.on('NhomDaGiaiTan', xuLyBiXoa);
    return () => {
      ketNoi.off('NhanTinNhan', xuLyTinNhanMoi);
      ketNoi.off('DuocThemVaoNhom', xuLyDuocThem);
      ketNoi.off('BiXoaKhoiNhom', xuLyBiXoa);
      ketNoi.off('NhomDaGiaiTan', xuLyBiXoa);
    };
  }, [ketNoi]);

  function taoNhomMoi() {
    if (!token || !tenNhomMoi.trim()) return;
    TaoNhom(token, tenNhomMoi.trim(), null, null, [...thanhVienDuocChon])
      .then((nhom) => {
        setDanhSachNhom((truoc) => [...truoc, nhom]);
        setHienFormTao(false);
        setTenNhomMoi('');
        setThanhVienDuocChon(new Set());
        setNhomDangChonId(nhom.id);
      })
      .catch((loiBat) => setLoi(loiBat instanceof LoiGoiApi ? loiBat.message : 'Tạo nhóm thất bại.'));
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
    setDangTaiTep(true);
    import('../DichVuApi')
      .then(({ TaiLenTep }) => TaiLenTep(token, tep))
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
    LayLichSuNhom(token, nhomDangChon.id, cuNhat.id)
      .then((cuHon) => {
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
    RoiNhom(token, nhomDangChon.id)
      .then(() => {
        setDanhSachNhom((truoc) => truoc.filter((n) => n.id !== nhomDangChon.id));
        setNhomDangChonId(null);
      })
      .catch((loiBat) => setLoi(loiBat instanceof LoiGoiApi ? loiBat.message : 'Rời nhóm thất bại.'));
  }

  const laAdmin = nhomDangChon?.nguoiTaoId === idHienTai;

  return (
    <div className="trang-nhom">
      <aside className="trang-nhom__sidebar">
        <button className="trang-nhom__nut-tao" onClick={() => setHienFormTao(true)}>
          + Tạo nhóm
        </button>
        {dangTaiDanhSach && <p>Đang tải...</p>}
        <ul className="trang-nhom__danh-sach">
          {danhSachNhom.map((n) => (
            <li key={n.id}>
              <button
                className={`trang-nhom__muc${nhomDangChonId === n.id ? ' trang-nhom__muc--dang-chon' : ''}`}
                onClick={() => setNhomDangChonId(n.id)}
              >
                <span className="trang-nhom__avatar">{n.tenNhom.charAt(0).toUpperCase()}</span>
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
            coTheTaiThem
            onTaiThemLichSuCu={taiThemLichSuCu}
            onGuiVanBan={guiTinNhanVanBan}
            onGuiTep={guiTep}
            dangTaiTep={dangTaiTep}
            loi={loi}
          />
          <aside className="trang-nhom__thong-tin">
            <h3>Thành viên</h3>
            <ul className="trang-nhom__ds-thanh-vien">
              {nhomDangChon.thanhVien.map((tv) => (
                <li key={tv.id}>
                  <span>{tv.tenTaiKhoan}{tv.id === nhomDangChon.nguoiTaoId ? ' (Admin)' : ''}</span>
                  {laAdmin && tv.id !== idHienTai && (
                    <button onClick={() => xoaThanhVien(tv.id)} aria-label={`Xóa ${tv.tenTaiKhoan}`}>×</button>
                  )}
                </li>
              ))}
            </ul>
            {laAdmin && (
              <select onChange={(su) => { if (su.target.value) themThanhVien(su.target.value); su.target.value = ''; }}>
                <option value="">+ Thêm thành viên...</option>
                {tatCaNguoiDung
                  .filter((nd) => !nhomDangChon.thanhVien.some((tv) => tv.id === nd.id))
                  .map((nd) => (
                    <option key={nd.id} value={nd.id}>{nd.tenTaiKhoan}</option>
                  ))}
              </select>
            )}
            <button className="trang-nhom__nut-roi" onClick={roiNhom}>
              {laAdmin ? 'Giải tán nhóm' : 'Rời nhóm'}
            </button>
          </aside>
        </div>
      )}

      {hienFormTao && (
        <div className="trang-nhom__modal-nen" onClick={() => setHienFormTao(false)}>
          <div className="trang-nhom__modal" onClick={(su) => su.stopPropagation()}>
            <h3>Tạo nhóm</h3>
            <input
              type="text"
              placeholder="Nhập tên nhóm..."
              value={tenNhomMoi}
              onChange={(su) => setTenNhomMoi(su.target.value)}
            />
            <p>Thêm thành viên:</p>
            <ul className="trang-nhom__chon-thanh-vien">
              {tatCaNguoiDung.map((nd) => (
                <li key={nd.id}>
                  <label>
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
                    {nd.tenTaiKhoan}
                  </label>
                </li>
              ))}
            </ul>
            <div className="trang-nhom__modal-hanh-dong">
              <button onClick={() => setHienFormTao(false)}>Hủy</button>
              <button className="nut-chinh" onClick={taoNhomMoi} disabled={!tenNhomMoi.trim()}>Tạo nhóm</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
