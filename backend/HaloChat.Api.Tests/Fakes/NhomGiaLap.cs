using HaloChat.Api.Models;
using HaloChat.Api.Repositories;

namespace HaloChat.Api.Tests.Fakes;

public class NhomGiaLap : INhomRepository
{
    public List<Nhom> DanhSach { get; } = new();

    public Task ThemMoiAsync(Nhom nhom)
    {
        DanhSach.Add(nhom);
        return Task.CompletedTask;
    }

    public Task<Nhom?> TimTheoIdAsync(string id) =>
        Task.FromResult(DanhSach.FirstOrDefault(n => n.Id == id));

    public Task<List<Nhom>> LayTheoThanhVienAsync(string userId) =>
        Task.FromResult(DanhSach.Where(n => n.ThanhVienIds.Contains(userId)).ToList());

    public Task ThemThanhVienAsync(string nhomId, string userId)
    {
        var nhom = DanhSach.FirstOrDefault(n => n.Id == nhomId);
        if (nhom is not null && !nhom.ThanhVienIds.Contains(userId))
        {
            nhom.ThanhVienIds.Add(userId);
        }
        return Task.CompletedTask;
    }

    public Task XoaThanhVienAsync(string nhomId, string userId)
    {
        DanhSach.FirstOrDefault(n => n.Id == nhomId)?.ThanhVienIds.Remove(userId);
        return Task.CompletedTask;
    }

    public Task CapNhatThongTinAsync(string nhomId, string tenNhom, string? moTa, string? duongDanAnhDaiDien)
    {
        var nhom = DanhSach.FirstOrDefault(n => n.Id == nhomId);
        if (nhom is not null)
        {
            nhom.TenNhom = tenNhom;
            nhom.MoTa = moTa;
            nhom.DuongDanAnhDaiDien = duongDanAnhDaiDien;
        }
        return Task.CompletedTask;
    }

    public Task XoaNhomAsync(string nhomId)
    {
        DanhSach.RemoveAll(n => n.Id == nhomId);
        return Task.CompletedTask;
    }
}
