import { useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  LayBanBe,
  LayLoiMoiDen,
  LayLoiMoiGui,
  LayDanhSachNguoiDung,
  LayTrangThaiHoatDong,
  GuiLoiMoiKetBan,
  ChapNhanLoiMoiKetBan,
  TuChoiLoiMoiKetBan,
  XoaBanBe,
  LoiGoiApi,
} from '../DichVuApi';
import { useXacThuc } from '../NguCanh/NguCanhXacThuc';
import type { NguoiDungTomTat, LoiMoiKetBan } from '../KieuDuLieu';
import { Avatar } from '../ThanhPhan/Avatar';
import './TrangBanBe.css';

type Tab = 'ban-be' | 'loi-moi';

export function TrangBanBe() {
  const { token } = useXacThuc();
  const navigate = useNavigate();

  const [banBe, setBanBe] = useState<NguoiDungTomTat[]>([]);
  const [loiMoiDen, setLoiMoiDen] = useState<LoiMoiKetBan[]>([]);
  const [loiMoiGui, setLoiMoiGui] = useState<LoiMoiKetBan[]>([]);
  const [tatCaNguoiDung, setTatCaNguoiDung] = useState<NguoiDungTomTat[]>([]);
  const [trangThaiOnline, setTrangThaiOnline] = useState<Record<string, boolean>>({});
  const [loi, setLoi] = useState<string | null>(null);
  const [dangTai, setDangTai] = useState(true);
  const [tuKhoaTimKiem, setTuKhoaTimKiem] = useState('');
  const [tabDangChon, setTabDangChon] = useState<Tab>('ban-be');
  const [menuMoChoId, setMenuMoChoId] = useState<string | null>(null);
  const [hoSoDangXem, setHoSoDangXem] = useState<NguoiDungTomTat | null>(null);
  const inputTimKiemRef = useRef<HTMLInputElement | null>(null);

  async function taiLaiTatCa(tokenHienTai: string) {
    const [dsBanBe, dsLoiMoiDen, dsLoiMoiGui, dsTatCa] = await Promise.all([
      LayBanBe(tokenHienTai),
      LayLoiMoiDen(tokenHienTai),
      LayLoiMoiGui(tokenHienTai),
      LayDanhSachNguoiDung(tokenHienTai),
    ]);
    setBanBe(dsBanBe);
    setLoiMoiDen(dsLoiMoiDen);
    setLoiMoiGui(dsLoiMoiGui);
    setTatCaNguoiDung(dsTatCa);
    if (dsBanBe.length > 0) {
      LayTrangThaiHoatDong(tokenHienTai, dsBanBe.map((b) => b.id))
        .then(setTrangThaiOnline)
        .catch(() => {});
    }
  }

  useEffect(() => {
    if (!token) return;
    taiLaiTatCa(token)
      .catch((loiBat) => setLoi(loiBat instanceof Error ? loiBat.message : 'Đã có lỗi xảy ra.'))
      .finally(() => setDangTai(false));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [token]);

  async function guiLoiMoi(nguoiNhanId: string) {
    if (!token) return;
    try {
      await GuiLoiMoiKetBan(token, nguoiNhanId);
      await taiLaiTatCa(token);
    } catch (loiBat) {
      setLoi(loiBat instanceof LoiGoiApi ? loiBat.message : 'Gửi lời mời thất bại.');
    }
  }

  async function chapNhan(id: string) {
    if (!token) return;
    try {
      await ChapNhanLoiMoiKetBan(token, id);
      await taiLaiTatCa(token);
    } catch (loiBat) {
      setLoi(loiBat instanceof LoiGoiApi ? loiBat.message : 'Chấp nhận lời mời thất bại.');
    }
  }

  async function tuChoi(id: string) {
    if (!token) return;
    try {
      await TuChoiLoiMoiKetBan(token, id);
      await taiLaiTatCa(token);
    } catch (loiBat) {
      setLoi(loiBat instanceof LoiGoiApi ? loiBat.message : 'Từ chối lời mời thất bại.');
    }
  }

  function xoaBan(b: NguoiDungTomTat) {
    if (!token) return;
    setMenuMoChoId(null);
    if (!window.confirm(`Xóa ${b.tenTaiKhoan} khỏi danh sách bạn bè?`)) return;
    XoaBanBe(token, b.id)
      .then(() => setBanBe((truoc) => truoc.filter((x) => x.id !== b.id)))
      .catch((loiBat) => setLoi(loiBat instanceof LoiGoiApi ? loiBat.message : 'Xóa bạn thất bại.'));
  }

  // Khong loai nguoi da gui loi moi (loiMoiGui) khoi ket qua tim kiem: ho
  // van phai hien voi nut "Da gui loi moi" (disabled) thay vi bien mat khoi
  // danh sach, xem nhanh idDaGuiLoiMoi/ketQuaTimKiem ben duoi.
  const idDaLaBanBeHoacDangCho = new Set<string>([
    ...banBe.map((b) => b.id),
    ...loiMoiDen.map((l) => l.nguoiGui.id),
  ]);

  const dangTimKiem = tuKhoaTimKiem.trim().length > 0;
  const idDaGuiLoiMoi = new Set(loiMoiGui.map((l) => l.nguoiNhan.id));
  const ketQuaTimKiem = tatCaNguoiDung
    .filter((nd) => !idDaLaBanBeHoacDangCho.has(nd.id))
    .filter((nd) => nd.tenTaiKhoan.toLowerCase().includes(tuKhoaTimKiem.trim().toLowerCase()));

  return (
    <div className="trang-ban-be-bo-cuc">
      <div className="trang-ban-be">
        <input
          ref={inputTimKiemRef}
          type="text"
          className="trang-ban-be__tim-kiem"
          placeholder="🔎 Tìm bạn bè hoặc tên tài khoản..."
          value={tuKhoaTimKiem}
          onChange={(su) => setTuKhoaTimKiem(su.target.value)}
        />

        {loi && (
          <p className="thong-bao-loi" role="alert">
            {loi}
          </p>
        )}

        {!dangTimKiem && (
          <div className="trang-ban-be__tab-cum">
            <button
              className={`trang-ban-be__tab${tabDangChon === 'ban-be' ? ' trang-ban-be__tab--chon' : ''}`}
              onClick={() => setTabDangChon('ban-be')}
            >
              Bạn bè
            </button>
            <button
              className={`trang-ban-be__tab${tabDangChon === 'loi-moi' ? ' trang-ban-be__tab--chon' : ''}`}
              onClick={() => setTabDangChon('loi-moi')}
            >
              Lời mời ({loiMoiDen.length})
            </button>
          </div>
        )}

        {dangTai && <p>Đang tải...</p>}

        {dangTimKiem && (
          <section className="trang-ban-be__phan">
            <h2>Kết quả tìm kiếm</h2>
            {ketQuaTimKiem.length === 0 && <p className="trang-ban-be__trong">Không tìm thấy người dùng nào.</p>}
            <ul className="trang-ban-be__danh-sach">
              {ketQuaTimKiem.map((nd) => (
                <li key={nd.id} className="trang-ban-be__card trang-ban-be__card--tim-kiem">
                  <Avatar id={nd.id} ten={nd.tenTaiKhoan} />
                  <div className="trang-ban-be__card-thong-tin">
                    <span className="trang-ban-be__card-ten">{nd.tenTaiKhoan}</span>
                    <span className="trang-ban-be__card-email">{nd.email}</span>
                    {!nd.choPhepTinNhanTuNguoiLa && (
                      <span className="trang-ban-be__card-khoa">🔒 Chỉ nhận tin nhắn từ bạn bè</span>
                    )}
                  </div>
                  <div className="trang-ban-be__card-hanh-dong">
                    {nd.choPhepTinNhanTuNguoiLa && (
                      <button
                        className="trang-ban-be__nut-phu"
                        onClick={() => navigate('/nguoi-dung', { state: { moNguoiDung: nd } })}
                      >
                        Nhắn tin
                      </button>
                    )}
                    {idDaGuiLoiMoi.has(nd.id) ? (
                      <button className="nut-chinh" disabled>Đã gửi lời mời</button>
                    ) : (
                      <button className="nut-chinh" onClick={() => guiLoiMoi(nd.id)}>Kết bạn</button>
                    )}
                  </div>
                </li>
              ))}
            </ul>
          </section>
        )}

        {!dangTimKiem && tabDangChon === 'ban-be' && (
          <section className="trang-ban-be__phan">
            <h2>Bạn bè của tôi ({banBe.length})</h2>
            {banBe.length === 0 ? (
              <div className="trang-ban-be__trong-toan-trang">
                <p className="trang-ban-be__trong-tieu-de">Bạn chưa có người bạn nào</p>
                <p>Hãy tìm kiếm và kết bạn với những người bạn biết.</p>
                <button className="nut-chinh" onClick={() => inputTimKiemRef.current?.focus()}>
                  Tìm bạn bè
                </button>
              </div>
            ) : (
              <ul className="trang-ban-be__danh-sach">
                {banBe.map((b) => (
                  <li key={b.id} className="trang-ban-be__card">
                    <Avatar id={b.id} ten={b.tenTaiKhoan} />
                    <div className="trang-ban-be__card-thong-tin">
                      <span className="trang-ban-be__card-ten">{b.tenTaiKhoan}</span>
                      {trangThaiOnline[b.id] && <span className="trang-ban-be__card-trang-thai">Đang hoạt động</span>}
                    </div>
                    <button
                      className="nut-chinh trang-ban-be__nut-nhan-tin"
                      onClick={() => navigate('/nguoi-dung', { state: { moNguoiDung: b } })}
                    >
                      Nhắn tin
                    </button>
                    <div className="trang-ban-be__menu-cum">
                      <button
                        className="trang-ban-be__nut-menu"
                        aria-label={`Thêm thao tác cho ${b.tenTaiKhoan}`}
                        onClick={() => setMenuMoChoId((truoc) => (truoc === b.id ? null : b.id))}
                      >
                        ⋯
                      </button>
                      {menuMoChoId === b.id && (
                        <div className="trang-ban-be__menu">
                          <button onClick={() => { setHoSoDangXem(b); setMenuMoChoId(null); }}>Xem thông tin</button>
                          <button className="trang-ban-be__menu-nguy-hiem" onClick={() => xoaBan(b)}>Xóa bạn</button>
                        </div>
                      )}
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </section>
        )}

        {!dangTimKiem && tabDangChon === 'loi-moi' && (
          <section className="trang-ban-be__phan">
            <h2>Lời mời kết bạn ({loiMoiDen.length})</h2>
            {loiMoiDen.length === 0 ? (
              <div className="trang-ban-be__trong-toan-trang">
                <p className="trang-ban-be__trong-tieu-de">Không có lời mời kết bạn</p>
                <p>Bạn chưa có lời mời kết bạn nào.</p>
              </div>
            ) : (
              <ul className="trang-ban-be__danh-sach">
                {loiMoiDen.map((l) => (
                  <li key={l.id} className="trang-ban-be__card">
                    <Avatar id={l.nguoiGui.id} ten={l.nguoiGui.tenTaiKhoan} />
                    <div className="trang-ban-be__card-thong-tin">
                      <span className="trang-ban-be__card-ten">{l.nguoiGui.tenTaiKhoan}</span>
                      <span className="trang-ban-be__card-phu">Muốn kết bạn với bạn</span>
                    </div>
                    <div className="trang-ban-be__card-hanh-dong">
                      <button className="nut-chinh" onClick={() => chapNhan(l.id)}>Chấp nhận</button>
                      <button className="trang-ban-be__nut-phu" onClick={() => tuChoi(l.id)}>Từ chối</button>
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </section>
        )}
      </div>

      {hoSoDangXem && (
        <aside className="trang-ban-be__ho-so">
          <button className="trang-ban-be__dong-ho-so" onClick={() => setHoSoDangXem(null)} aria-label="Đóng hồ sơ">×</button>
          <Avatar id={hoSoDangXem.id} ten={hoSoDangXem.tenTaiKhoan} kichThuoc="lon" />
          <h3>{hoSoDangXem.tenTaiKhoan}</h3>
          <p className="trang-ban-be__email-ho-so">{hoSoDangXem.email}</p>
          <button
            className="nut-chinh"
            onClick={() => navigate('/nguoi-dung', { state: { moNguoiDung: hoSoDangXem } })}
          >
            Nhắn tin
          </button>
        </aside>
      )}
    </div>
  );
}
