using HaloChat.Api.Models;
using MongoDB.Driver;

namespace HaloChat.Api.Repositories;

public class LoiMoiKetBanRepository : ILoiMoiKetBanRepository
{
    private readonly IMongoCollection<LoiMoiKetBan> _collection;

    public LoiMoiKetBanRepository(IMongoDatabase csdl)
    {
        _collection = csdl.GetCollection<LoiMoiKetBan>("LoiMoiKetBan");
    }

    private static FilterDefinition<LoiMoiKetBan> BoLocCapDoi(string nguoiA, string nguoiB) =>
        Builders<LoiMoiKetBan>.Filter.Or(
            Builders<LoiMoiKetBan>.Filter.And(
                Builders<LoiMoiKetBan>.Filter.Eq(l => l.NguoiGuiId, nguoiA),
                Builders<LoiMoiKetBan>.Filter.Eq(l => l.NguoiNhanId, nguoiB)),
            Builders<LoiMoiKetBan>.Filter.And(
                Builders<LoiMoiKetBan>.Filter.Eq(l => l.NguoiGuiId, nguoiB),
                Builders<LoiMoiKetBan>.Filter.Eq(l => l.NguoiNhanId, nguoiA)));

    public async Task<bool> TonTaiLoiMoiDangHoatDongAsync(string nguoiA, string nguoiB)
    {
        var boLoc = Builders<LoiMoiKetBan>.Filter.And(
            BoLocCapDoi(nguoiA, nguoiB),
            Builders<LoiMoiKetBan>.Filter.Ne(l => l.TrangThai, TrangThaiLoiMoiKetBan.DaTuChoi));
        return await _collection.Find(boLoc).AnyAsync();
    }

    public Task ThemMoiAsync(LoiMoiKetBan loiMoi) => _collection.InsertOneAsync(loiMoi);

    public async Task<LoiMoiKetBan?> TimTheoIdAsync(string id) =>
        await _collection.Find(l => l.Id == id).FirstOrDefaultAsync();

    public async Task CapNhatTrangThaiAsync(string id, TrangThaiLoiMoiKetBan trangThai)
    {
        var capNhat = Builders<LoiMoiKetBan>.Update.Set(l => l.TrangThai, trangThai);
        await _collection.UpdateOneAsync(l => l.Id == id, capNhat);
    }

    public async Task<List<LoiMoiKetBan>> LayBanBeAsync(string nguoiDungId)
    {
        var boLoc = Builders<LoiMoiKetBan>.Filter.And(
            Builders<LoiMoiKetBan>.Filter.Eq(l => l.TrangThai, TrangThaiLoiMoiKetBan.DaChapNhan),
            Builders<LoiMoiKetBan>.Filter.Or(
                Builders<LoiMoiKetBan>.Filter.Eq(l => l.NguoiGuiId, nguoiDungId),
                Builders<LoiMoiKetBan>.Filter.Eq(l => l.NguoiNhanId, nguoiDungId)));
        return await _collection.Find(boLoc).ToListAsync();
    }

    public async Task<List<LoiMoiKetBan>> LayLoiMoiDenAsync(string nguoiDungId)
    {
        var boLoc = Builders<LoiMoiKetBan>.Filter.And(
            Builders<LoiMoiKetBan>.Filter.Eq(l => l.NguoiNhanId, nguoiDungId),
            Builders<LoiMoiKetBan>.Filter.Eq(l => l.TrangThai, TrangThaiLoiMoiKetBan.ChoDuyet));
        return await _collection.Find(boLoc).ToListAsync();
    }

    public async Task<List<LoiMoiKetBan>> LayLoiMoiGuiAsync(string nguoiDungId)
    {
        var boLoc = Builders<LoiMoiKetBan>.Filter.And(
            Builders<LoiMoiKetBan>.Filter.Eq(l => l.NguoiGuiId, nguoiDungId),
            Builders<LoiMoiKetBan>.Filter.Eq(l => l.TrangThai, TrangThaiLoiMoiKetBan.ChoDuyet));
        return await _collection.Find(boLoc).ToListAsync();
    }

    public async Task<bool> LaBanBeAsync(string nguoiA, string nguoiB)
    {
        var boLoc = Builders<LoiMoiKetBan>.Filter.And(
            BoLocCapDoi(nguoiA, nguoiB),
            Builders<LoiMoiKetBan>.Filter.Eq(l => l.TrangThai, TrangThaiLoiMoiKetBan.DaChapNhan));
        return await _collection.Find(boLoc).AnyAsync();
    }
}
