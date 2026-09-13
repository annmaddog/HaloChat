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

    public async Task CapNhatCaiDatAsync(string id, bool choPhepTinNhanTuNguoiLa, bool hienThiTrangThaiHoatDong)
    {
        var boLoc = Builders<NguoiDung>.Filter.Eq(nd => nd.Id, id);
        var capNhat = Builders<NguoiDung>.Update
            .Set(nd => nd.ChoPhepTinNhanTuNguoiLa, choPhepTinNhanTuNguoiLa)
            .Set(nd => nd.HienThiTrangThaiHoatDong, hienThiTrangThaiHoatDong);
        await _collection.UpdateOneAsync(boLoc, capNhat);
    }

    public async Task LuuOtpAsync(string id, string maOtpBam, DateTime hetHan, DateTime guiLucNao)
    {
        var boLoc = Builders<NguoiDung>.Filter.Eq(nd => nd.Id, id);
        var capNhat = Builders<NguoiDung>.Update
            .Set(nd => nd.MaOtpBam, maOtpBam)
            .Set(nd => nd.MaOtpHetHan, hetHan)
            .Set(nd => nd.MaOtpGuiLucNao, guiLucNao)
            .Set(nd => nd.SoLanThuSai, 0);
        await _collection.UpdateOneAsync(boLoc, capNhat);
    }

    public async Task TangSoLanThuSaiOtpAsync(string id)
    {
        var boLoc = Builders<NguoiDung>.Filter.Eq(nd => nd.Id, id);
        var capNhat = Builders<NguoiDung>.Update.Inc(nd => nd.SoLanThuSai, 1);
        await _collection.UpdateOneAsync(boLoc, capNhat);
    }

    public async Task DatLaiMatKhauAsync(string id, string matKhauBamMoi, string saltMoi)
    {
        var boLoc = Builders<NguoiDung>.Filter.Eq(nd => nd.Id, id);
        var capNhat = Builders<NguoiDung>.Update
            .Set(nd => nd.MatKhauBam, matKhauBamMoi)
            .Set(nd => nd.Salt, saltMoi)
            .Set(nd => nd.MaOtpBam, null)
            .Set(nd => nd.MaOtpHetHan, null)
            .Set(nd => nd.MaOtpGuiLucNao, null)
            .Set(nd => nd.SoLanThuSai, 0);
        await _collection.UpdateOneAsync(boLoc, capNhat);
    }
}
