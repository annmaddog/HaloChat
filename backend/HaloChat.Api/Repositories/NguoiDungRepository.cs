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

    public async Task<bool> TonTaiTenTaiKhoanAsync(string tenTaiKhoan)
    {
        return await _collection.Find(nd => nd.TenTaiKhoan == tenTaiKhoan).AnyAsync();
    }

    public async Task<bool> TonTaiEmailAsync(string email)
    {
        return await _collection.Find(nd => nd.Email == email).AnyAsync();
    }

    public async Task ThemMoiAsync(NguoiDung nguoiDung)
    {
        await _collection.InsertOneAsync(nguoiDung);
    }
}
