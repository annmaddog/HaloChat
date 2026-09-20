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
        // SetOnInsert chỉ ghi Id/ThoiGianAn khi tạo mới; nếu bản ghi đã có (ẩn lần 2) thì không đụng tới _id bất biến.
        var capNhat = Builders<TinNhanAn>.Update
            .SetOnInsert(x => x.Id, MongoDB.Bson.ObjectId.GenerateNewId().ToString())
            .SetOnInsert(x => x.ThoiGianAn, DateTime.UtcNow);
        await _collection.UpdateOneAsync(boLoc, capNhat, new UpdateOptions { IsUpsert = true });
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
