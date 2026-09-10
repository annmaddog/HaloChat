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

    public async Task<NguoiDung?> TimTheoTenTaiKhoanHoacEmailAsync(string tenDangNhap)
    {
        var boLoc = Builders<NguoiDung>.Filter.Or(
            Builders<NguoiDung>.Filter.Eq(nd => nd.TenTaiKhoan, tenDangNhap),
            Builders<NguoiDung>.Filter.Eq(nd => nd.Email, tenDangNhap));

        return await _collection.Find(boLoc).FirstOrDefaultAsync();
    }
}
