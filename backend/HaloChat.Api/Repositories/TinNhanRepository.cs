using HaloChat.Api.Models;
using MongoDB.Driver;

namespace HaloChat.Api.Repositories;

public class TinNhanRepository : ITinNhanRepository
{
    private readonly IMongoCollection<TinNhan> _collection;

    public TinNhanRepository(IMongoDatabase csdl)
    {
        _collection = csdl.GetCollection<TinNhan>("TinNhan");
    }

    public Task ThemMoiAsync(TinNhan tinNhan) => _collection.InsertOneAsync(tinNhan);

    public async Task<List<TinNhan>> LayLichSuTheoNguoiDungAsync(string nguoiA, string nguoiB, string? truocId, int soLuong)
    {
        var boLocCapDoi = Builders<TinNhan>.Filter.Or(
            Builders<TinNhan>.Filter.And(
                Builders<TinNhan>.Filter.Eq(t => t.NguoiGuiId, nguoiA),
                Builders<TinNhan>.Filter.Eq(t => t.NguoiNhanId, nguoiB)),
            Builders<TinNhan>.Filter.And(
                Builders<TinNhan>.Filter.Eq(t => t.NguoiGuiId, nguoiB),
                Builders<TinNhan>.Filter.Eq(t => t.NguoiNhanId, nguoiA)));

        var boLoc = string.IsNullOrEmpty(truocId)
            ? boLocCapDoi
            : Builders<TinNhan>.Filter.And(boLocCapDoi, Builders<TinNhan>.Filter.Lt(t => t.Id, truocId));

        return await _collection.Find(boLoc)
            .SortByDescending(t => t.Id)
            .Limit(soLuong)
            .ToListAsync();
    }

    public async Task DanhDauDaDocAsync(string nguoiGuiId, string nguoiNhanId)
    {
        var boLoc = Builders<TinNhan>.Filter.And(
            Builders<TinNhan>.Filter.Eq(t => t.NguoiGuiId, nguoiGuiId),
            Builders<TinNhan>.Filter.Eq(t => t.NguoiNhanId, nguoiNhanId),
            Builders<TinNhan>.Filter.Eq(t => t.DaDoc, false));
        var capNhat = Builders<TinNhan>.Update.Set(t => t.DaDoc, true);

        await _collection.UpdateManyAsync(boLoc, capNhat);
    }

    public async Task<List<TinNhan>> LayTatCaLienQuanAsync(string nguoiDungId)
    {
        var boLoc = Builders<TinNhan>.Filter.And(
            Builders<TinNhan>.Filter.Eq(t => t.NhomId, null),
            Builders<TinNhan>.Filter.Or(
                Builders<TinNhan>.Filter.Eq(t => t.NguoiGuiId, nguoiDungId),
                Builders<TinNhan>.Filter.Eq(t => t.NguoiNhanId, nguoiDungId)));

        return await _collection.Find(boLoc).SortByDescending(t => t.Id).ToListAsync();
    }

    public async Task<List<TinNhan>> LayLichSuNhomAsync(string nhomId, string? truocId, int soLuong)
    {
        var boLocNhom = Builders<TinNhan>.Filter.Eq(t => t.NhomId, nhomId);
        var boLoc = string.IsNullOrEmpty(truocId)
            ? boLocNhom
            : Builders<TinNhan>.Filter.And(boLocNhom, Builders<TinNhan>.Filter.Lt(t => t.Id, truocId));

        return await _collection.Find(boLoc)
            .SortByDescending(t => t.Id)
            .Limit(soLuong)
            .ToListAsync();
    }

    public async Task<int> DemTinNhanSauIdAsync(string nhomId, string? sauId, string loaiTruNguoiGuiId)
    {
        var boLocNhom = Builders<TinNhan>.Filter.And(
            Builders<TinNhan>.Filter.Eq(t => t.NhomId, nhomId),
            Builders<TinNhan>.Filter.Ne(t => t.NguoiGuiId, loaiTruNguoiGuiId));
        var boLoc = sauId is null
            ? boLocNhom
            : Builders<TinNhan>.Filter.And(boLocNhom, Builders<TinNhan>.Filter.Gt(t => t.Id, sauId));
        return (int)await _collection.CountDocumentsAsync(boLoc);
    }

    public async Task XoaTheoNhomAsync(string nhomId)
    {
        await _collection.DeleteManyAsync(t => t.NhomId == nhomId);
    }

    public async Task<TinNhan?> TimTheoIdAsync(string id)
    {
        return await _collection.Find(t => t.Id == id).FirstOrDefaultAsync();
    }

    public async Task DanhDauThuHoiAsync(string id)
    {
        var boLoc = Builders<TinNhan>.Filter.Eq(t => t.Id, id);
        var capNhat = Builders<TinNhan>.Update.Set(t => t.DaThuHoi, true);
        await _collection.UpdateOneAsync(boLoc, capNhat);
    }

    public async Task DatGhimAsync(string id, bool daGhim, DateTime? thoiGianGhim)
    {
        var boLoc = Builders<TinNhan>.Filter.Eq(t => t.Id, id);
        var capNhat = Builders<TinNhan>.Update
            .Set(t => t.DaGhim, daGhim)
            .Set(t => t.ThoiGianGhim, thoiGianGhim);
        await _collection.UpdateOneAsync(boLoc, capNhat);
    }

    public async Task<List<TinNhan>> LayTinDaGhimTheoNguoiDungAsync(string nguoiA, string nguoiB)
    {
        var boLoc = Builders<TinNhan>.Filter.And(
            Builders<TinNhan>.Filter.Eq(t => t.DaGhim, true),
            Builders<TinNhan>.Filter.Or(
                Builders<TinNhan>.Filter.And(
                    Builders<TinNhan>.Filter.Eq(t => t.NguoiGuiId, nguoiA),
                    Builders<TinNhan>.Filter.Eq(t => t.NguoiNhanId, nguoiB)),
                Builders<TinNhan>.Filter.And(
                    Builders<TinNhan>.Filter.Eq(t => t.NguoiGuiId, nguoiB),
                    Builders<TinNhan>.Filter.Eq(t => t.NguoiNhanId, nguoiA))));
        return await _collection.Find(boLoc).SortByDescending(t => t.ThoiGianGhim).ToListAsync();
    }

    public async Task<List<TinNhan>> LayTinDaGhimTheoNhomAsync(string nhomId)
    {
        var boLoc = Builders<TinNhan>.Filter.And(
            Builders<TinNhan>.Filter.Eq(t => t.NhomId, nhomId),
            Builders<TinNhan>.Filter.Eq(t => t.DaGhim, true));
        return await _collection.Find(boLoc).SortByDescending(t => t.ThoiGianGhim).ToListAsync();
    }
}
