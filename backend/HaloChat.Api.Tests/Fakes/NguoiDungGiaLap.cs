using HaloChat.Api.Models;
using HaloChat.Api.Repositories;

namespace HaloChat.Api.Tests.Fakes;

public class NguoiDungGiaLap : INguoiDungRepository
{
    public List<NguoiDung> DanhSach { get; } = new();

    public Task<bool> TonTaiDinhDanhAsync(string dinhDanh) =>
        Task.FromResult(DanhSach.Any(nd => nd.TenTaiKhoan == dinhDanh || nd.Email == dinhDanh));

    public Task ThemMoiAsync(NguoiDung nguoiDung)
    {
        DanhSach.Add(nguoiDung);
        return Task.CompletedTask;
    }

    public Task<NguoiDung?> TimTheoTenTaiKhoanHoacEmailAsync(string tenDangNhap)
    {
        var ketQua = DanhSach.FirstOrDefault(nd => nd.TenTaiKhoan == tenDangNhap || nd.Email == tenDangNhap);
        return Task.FromResult(ketQua);
    }

    public Task<List<NguoiDung>> LayTatCaAsync() => Task.FromResult(DanhSach.ToList());
}
