using HaloChat.Api.Models;
using MongoDB.Driver;

namespace HaloChat.Api.Repositories;

public class DocNhomRepository : IDocNhomRepository
{
    private readonly IMongoCollection<DocNhom> _collection;

    public DocNhomRepository(IMongoDatabase csdl)
    {
        _collection = csdl.GetCollection<DocNhom>("DocNhom");
    }

    public async Task DanhDauDaDocAsync(string nguoiDungId, string nhomId, string tinNhanCuoiId)
    {
        var boLoc = Builders<DocNhom>.Filter.And(
            Builders<DocNhom>.Filter.Eq(d => d.NguoiDungId, nguoiDungId),
            Builders<DocNhom>.Filter.Eq(d => d.NhomId, nhomId));
        var capNhat = Builders<DocNhom>.Update
            .Set(d => d.TinNhanCuoiDaDocId, tinNhanCuoiId)
            .Set(d => d.ThoiGianDoc, DateTime.UtcNow)
            .SetOnInsert(d => d.NguoiDungId, nguoiDungId)
            .SetOnInsert(d => d.NhomId, nhomId);
        await _collection.UpdateOneAsync(boLoc, capNhat, new UpdateOptions { IsUpsert = true });
    }

    public async Task<string?> LayTinNhanCuoiDaDocAsync(string nguoiDungId, string nhomId)
    {
        var boLoc = Builders<DocNhom>.Filter.And(
            Builders<DocNhom>.Filter.Eq(d => d.NguoiDungId, nguoiDungId),
            Builders<DocNhom>.Filter.Eq(d => d.NhomId, nhomId));
        var ketQua = await _collection.Find(boLoc).FirstOrDefaultAsync();
        return ketQua?.TinNhanCuoiDaDocId;
    }
}
