using HaloChat.Api.Models;
using MongoDB.Driver;

namespace HaloChat.Api.Repositories;

public class NguoiDungRepository : INguoiDungRepository
{
    private readonly IMongoCollection<NguoiDung> _collection;

    public NguoiDungRepository(IMongoDatabase csdl)
    {
        _collection = csdl.GetCollection<NguoiDung>("NguoiDung");
    }

    public async Task<bool> TonTaiDinhDanhAsync(string dinhDanh)
    {
        var boLoc = Builders<NguoiDung>.Filter.Or(
            Builders<NguoiDung>.Filter.Eq(nd => nd.TenTaiKhoan, dinhDanh),
            Builders<NguoiDung>.Filter.Eq(nd => nd.Email, dinhDanh));

        return await _collection.Find(boLoc).AnyAsync();
    }

    public async Task ThemMoiAsync(NguoiDung nguoiDung)
    {
        try
        {
            await _collection.InsertOneAsync(nguoiDung);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new TrungLapDinhDanhException();
        }
    }

    public async Task<NguoiDung?> TimTheoTenTaiKhoanHoacEmailAsync(string tenDangNhap)
    {
        var boLoc = Builders<NguoiDung>.Filter.Or(
            Builders<NguoiDung>.Filter.Eq(nd => nd.TenTaiKhoan, tenDangNhap),
            Builders<NguoiDung>.Filter.Eq(nd => nd.Email, tenDangNhap));

        return await _collection.Find(boLoc).FirstOrDefaultAsync();
    }

    public async Task<List<NguoiDung>> LayTatCaAsync()
    {
        return await _collection.Find(FilterDefinition<NguoiDung>.Empty).ToListAsync();
    }

    public async Task<NguoiDung?> TimTheoIdAsync(string id)
    {
        return await _collection.Find(nd => nd.Id == id).FirstOrDefaultAsync();
    }
}
