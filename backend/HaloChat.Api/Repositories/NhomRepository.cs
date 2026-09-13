using HaloChat.Api.Models;
using MongoDB.Driver;

namespace HaloChat.Api.Repositories;

public class NhomRepository : INhomRepository
{
    private readonly IMongoCollection<Nhom> _collection;

    public NhomRepository(IMongoDatabase csdl)
    {
        _collection = csdl.GetCollection<Nhom>("Nhom");
    }

    public Task ThemMoiAsync(Nhom nhom) => _collection.InsertOneAsync(nhom);

    public async Task<Nhom?> TimTheoIdAsync(string id) =>
        await _collection.Find(n => n.Id == id).FirstOrDefaultAsync();

    public async Task<List<Nhom>> LayTheoThanhVienAsync(string userId) =>
        await _collection.Find(n => n.ThanhVienIds.Contains(userId)).ToListAsync();

    public async Task ThemThanhVienAsync(string nhomId, string userId)
    {
        var capNhat = Builders<Nhom>.Update.AddToSet(n => n.ThanhVienIds, userId);
        await _collection.UpdateOneAsync(n => n.Id == nhomId, capNhat);
    }

    public async Task XoaThanhVienAsync(string nhomId, string userId)
    {
        var capNhat = Builders<Nhom>.Update.Pull(n => n.ThanhVienIds, userId);
        await _collection.UpdateOneAsync(n => n.Id == nhomId, capNhat);
    }

    public async Task CapNhatThongTinAsync(string nhomId, string tenNhom, string? moTa, string? duongDanAnhDaiDien)
    {
        var capNhat = Builders<Nhom>.Update
            .Set(n => n.TenNhom, tenNhom)
            .Set(n => n.MoTa, moTa)
            .Set(n => n.DuongDanAnhDaiDien, duongDanAnhDaiDien);
        await _collection.UpdateOneAsync(n => n.Id == nhomId, capNhat);
    }

    public async Task XoaNhomAsync(string nhomId) =>
        await _collection.DeleteOneAsync(n => n.Id == nhomId);
}
