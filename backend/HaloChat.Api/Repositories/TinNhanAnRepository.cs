using HaloChat.Api.Models;
using MongoDB.Driver;

namespace HaloChat.Api.Repositories;

public class TinNhanAnRepository : ITinNhanAnRepository
{
    private readonly IMongoCollection<TinNhanAn> _collection;

    public TinNhanAnRepository(IMongoDatabase csdl)
    {
        _collection = csdl.GetCollection<TinNhanAn>("TinNhanAn");
    }

    public async Task AnAsync(string nguoiDungId, string tinNhanId)
    {
        var boLoc = Builders<TinNhanAn>.Filter.And(
            Builders<TinNhanAn>.Filter.Eq(x => x.NguoiDungId, nguoiDungId),
            Builders<TinNhanAn>.Filter.Eq(x => x.TinNhanId, tinNhanId));
        var thayThe = new TinNhanAn { NguoiDungId = nguoiDungId, TinNhanId = tinNhanId };
        await _collection.ReplaceOneAsync(boLoc, thayThe, new ReplaceOptions { IsUpsert = true });
    }

    public async Task<HashSet<string>> LayDanhSachIdDaAnAsync(string nguoiDungId, IEnumerable<string> tinNhanIds)
    {
        var idQuanTam = tinNhanIds.ToList();
        if (idQuanTam.Count == 0) return new HashSet<string>();

        var boLoc = Builders<TinNhanAn>.Filter.And(
            Builders<TinNhanAn>.Filter.Eq(x => x.NguoiDungId, nguoiDungId),
            Builders<TinNhanAn>.Filter.In(x => x.TinNhanId, idQuanTam));
        var ketQua = await _collection.Find(boLoc).ToListAsync();
        return ketQua.Select(x => x.TinNhanId).ToHashSet();
    }
}
