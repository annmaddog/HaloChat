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
}
