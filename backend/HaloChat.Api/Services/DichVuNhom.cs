using HaloChat.Api.Dto;
using HaloChat.Api.Models;
using HaloChat.Api.Repositories;
using MongoDB.Bson;

namespace HaloChat.Api.Services;

public class DichVuNhom : IDichVuNhom
{
    private readonly INhomRepository _khoNhom;
    private readonly INguoiDungRepository _khoNguoiDung;
    private readonly ITinNhanRepository _khoTinNhan;
    private readonly IDocNhomRepository _khoDocNhom;

    public DichVuNhom(
        INhomRepository khoNhom, INguoiDungRepository khoNguoiDung, ITinNhanRepository khoTinNhan,
        IDocNhomRepository khoDocNhom)
    {
        _khoNhom = khoNhom;
        _khoNguoiDung = khoNguoiDung;
        _khoTinNhan = khoTinNhan;
        _khoDocNhom = khoDocNhom;
    }

    public async Task<NhomDto> TaoNhomAsync(
        string nguoiTaoId, string tenNhom, string? moTa, string? duongDanAnhDaiDien, List<string> thanhVienIds)
    {
        var idThanhVien = new HashSet<string>(thanhVienIds) { nguoiTaoId };
        foreach (var id in idThanhVien)
        {
            if (!ObjectId.TryParse(id, out _))
            {
                throw new ThanhVienKhongTonTaiException(id);
            }

            var nguoiDung = await _khoNguoiDung.TimTheoIdAsync(id);
            if (nguoiDung is null)
            {
                throw new ThanhVienKhongTonTaiException(id);
            }

            if (id != nguoiTaoId && !nguoiDung.ChoPhepThemVaoNhom)
            {
                throw new KhongChoPhepThemVaoNhomException(nguoiDung.TenTaiKhoan);
            }
        }

        var nhom = new Nhom
        {
            TenNhom = tenNhom,
            MoTa = moTa,
            DuongDanAnhDaiDien = duongDanAnhDaiDien,
            NguoiTaoId = nguoiTaoId,
            ThanhVienIds = idThanhVien.ToList(),
        };
        await _khoNhom.ThemMoiAsync(nhom);

        return await AnhXaDtoAsync(nhom);
    }

    public async Task<List<NhomDto>> LayDanhSachAsync(string nguoiDungId)
    {
        var danhSach = await _khoNhom.LayTheoThanhVienAsync(nguoiDungId);
        var ketQua = new List<NhomDto>();
        foreach (var nhom in danhSach)
        {
            ketQua.Add(await AnhXaDtoAsync(nhom));
        }
        return ketQua;
    }

    public async Task<NhomDto> LayChiTietAsync(string nguoiDungId, string nhomId)
    {
        var nhom = await LayNhomKiemTraThanhVienAsync(nguoiDungId, nhomId);
        return await AnhXaDtoAsync(nhom);
    }

    public async Task<NhomDto> CapNhatAsync(
        string nguoiDungId, string nhomId, string tenNhom, string? moTa, string? duongDanAnhDaiDien)
    {
        var nhom = await LayNhomKiemTraQuanTriAsync(nguoiDungId, nhomId);
        await _khoNhom.CapNhatThongTinAsync(nhomId, tenNhom, moTa, duongDanAnhDaiDien);
        nhom.TenNhom = tenNhom;
        nhom.MoTa = moTa;
        nhom.DuongDanAnhDaiDien = duongDanAnhDaiDien;
        return await AnhXaDtoAsync(nhom);
    }

    /// <summary>Lấy nhóm theo id, ném lỗi nếu không tồn tại hoặc người gọi không phải thành viên.</summary>
    private async Task<Nhom> LayNhomKiemTraThanhVienAsync(string nguoiDungId, string nhomId)
    {
        var nhom = await _khoNhom.TimTheoIdAsync(nhomId) ?? throw new NhomKhongTonTaiException();
        if (!nhom.ThanhVienIds.Contains(nguoiDungId))
        {
            throw new KhongPhaiThanhVienNhomException();
        }
        return nhom;
    }

    /// <summary>Lấy nhóm theo id, ném lỗi nếu không tồn tại hoặc người gọi không phải NguoiTaoId (admin).</summary>
    private async Task<Nhom> LayNhomKiemTraQuanTriAsync(string nguoiDungId, string nhomId)
    {
        var nhom = await _khoNhom.TimTheoIdAsync(nhomId) ?? throw new NhomKhongTonTaiException();
        if (nhom.NguoiTaoId != nguoiDungId)
        {
            throw new KhongCoQuyenQuanTriNhomException();
        }
        return nhom;
    }

    private async Task<NhomDto> AnhXaDtoAsync(Nhom nhom)
    {
        var thanhVien = new List<NguoiDungTomTatDto>();
        foreach (var id in nhom.ThanhVienIds)
        {
            var nd = await _khoNguoiDung.TimTheoIdAsync(id);
            if (nd is not null)
            {
                thanhVien.Add(new NguoiDungTomTatDto(nd.Id, nd.TenTaiKhoan, nd.Email, nd.ChoPhepTinNhanTuNguoiLa, nd.TenHienThiThucTe(), nd.DuongDanAnhDaiDien));
            }
        }
        return new NhomDto(nhom.Id, nhom.TenNhom, nhom.MoTa, nhom.DuongDanAnhDaiDien, nhom.NguoiTaoId, thanhVien, nhom.ThoiGianTao);
    }

    public async Task<NhomDto> ThemThanhVienAsync(string nguoiGoiId, string nhomId, string thanhVienMoiId)
    {
        var nhom = await LayNhomKiemTraQuanTriAsync(nguoiGoiId, nhomId);

        if (!ObjectId.TryParse(thanhVienMoiId, out _))
        {
            throw new ThanhVienKhongTonTaiException(thanhVienMoiId);
        }

        var thanhVienMoi = await _khoNguoiDung.TimTheoIdAsync(thanhVienMoiId);
        if (thanhVienMoi is null)
        {
            throw new ThanhVienKhongTonTaiException(thanhVienMoiId);
        }

        if (!thanhVienMoi.ChoPhepThemVaoNhom)
        {
            throw new KhongChoPhepThemVaoNhomException(thanhVienMoi.TenTaiKhoan);
        }

        if (!nhom.ThanhVienIds.Contains(thanhVienMoiId))
        {
            await _khoNhom.ThemThanhVienAsync(nhomId, thanhVienMoiId);
            nhom.ThanhVienIds.Add(thanhVienMoiId);
        }

        return await AnhXaDtoAsync(nhom);
    }

    public async Task<NhomDto> XoaThanhVienAsync(string nguoiGoiId, string nhomId, string thanhVienId)
    {
        var nhom = await LayNhomKiemTraQuanTriAsync(nguoiGoiId, nhomId);

        if (thanhVienId == nhom.NguoiTaoId)
        {
            throw new KhongTheXoaNguoiTaoException();
        }

        await _khoNhom.XoaThanhVienAsync(nhomId, thanhVienId);
        nhom.ThanhVienIds.Remove(thanhVienId);

        return await AnhXaDtoAsync(nhom);
    }

    public async Task<KetQuaRoiNhomDto> RoiNhomAsync(string nguoiGoiId, string nhomId)
    {
        var nhom = await LayNhomKiemTraThanhVienAsync(nguoiGoiId, nhomId);

        if (nguoiGoiId == nhom.NguoiTaoId)
        {
            var thanhVienConLai = nhom.ThanhVienIds.Where(id => id != nguoiGoiId).ToList();
            await _khoTinNhan.XoaTheoNhomAsync(nhomId);
            await _khoNhom.XoaNhomAsync(nhomId);
            return new KetQuaRoiNhomDto(true, thanhVienConLai);
        }

        await _khoNhom.XoaThanhVienAsync(nhomId, nguoiGoiId);
        return new KetQuaRoiNhomDto(false, new List<string>());
    }

    public async Task<int> DemTongChuaDocAsync(string nguoiDungId)
    {
        var danhSachNhom = await _khoNhom.LayTheoThanhVienAsync(nguoiDungId);
        var tong = 0;
        foreach (var nhom in danhSachNhom)
        {
            var tinCuoiDaDoc = await _khoDocNhom.LayTinNhanCuoiDaDocAsync(nguoiDungId, nhom.Id);
            tong += await _khoTinNhan.DemTinNhanSauIdAsync(nhom.Id, tinCuoiDaDoc, nguoiDungId);
        }
        return tong;
    }
}
