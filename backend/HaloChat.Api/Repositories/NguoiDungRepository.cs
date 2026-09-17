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

        var tuyChon = new FindOptions<NguoiDung>
        {
            Collation = new Collation("en", strength: CollationStrength.Secondary),
        };
        using var cursor = await _collection.FindAsync(boLoc, tuyChon);
        return await cursor.FirstOrDefaultAsync();
    }

    public async Task<List<NguoiDung>> LayTatCaAsync()
    {
        return await _collection.Find(FilterDefinition<NguoiDung>.Empty).ToListAsync();
    }

    public async Task<NguoiDung?> TimTheoIdAsync(string id)
    {
        return await _collection.Find(nd => nd.Id == id).FirstOrDefaultAsync();
    }

    public async Task CapNhatCaiDatAsync(
        string id, bool choPhepTinNhanTuNguoiLa, bool hienThiTrangThaiHoatDong,
        bool choPhepThemVaoNhom, bool thongBaoTinNhanMoi, bool thongBaoLoiMoiKetBan, bool thongBaoNhom)
    {
        var boLoc = Builders<NguoiDung>.Filter.Eq(nd => nd.Id, id);
        var capNhat = Builders<NguoiDung>.Update
            .Set(nd => nd.ChoPhepTinNhanTuNguoiLa, choPhepTinNhanTuNguoiLa)
            .Set(nd => nd.HienThiTrangThaiHoatDong, hienThiTrangThaiHoatDong)
            .Set(nd => nd.ChoPhepThemVaoNhom, choPhepThemVaoNhom)
            .Set(nd => nd.ThongBaoTinNhanMoi, thongBaoTinNhanMoi)
            .Set(nd => nd.ThongBaoLoiMoiKetBan, thongBaoLoiMoiKetBan)
            .Set(nd => nd.ThongBaoNhom, thongBaoNhom);
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

    public async Task XoaThoiGianGuiOtpAsync(string id)
    {
        var boLoc = Builders<NguoiDung>.Filter.Eq(nd => nd.Id, id);
        var capNhat = Builders<NguoiDung>.Update.Set(nd => nd.MaOtpGuiLucNao, (DateTime?)null);
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

    public async Task CapNhatTenHienThiAsync(string id, string tenHienThi)
    {
        var boLoc = Builders<NguoiDung>.Filter.Eq(nd => nd.Id, id);
        var capNhat = Builders<NguoiDung>.Update.Set(nd => nd.TenHienThi, tenHienThi);
        await _collection.UpdateOneAsync(boLoc, capNhat);
    }

    public async Task CapNhatAnhDaiDienAsync(string id, string duongDanAnhDaiDien)
    {
        var boLoc = Builders<NguoiDung>.Filter.Eq(nd => nd.Id, id);
        var capNhat = Builders<NguoiDung>.Update.Set(nd => nd.DuongDanAnhDaiDien, duongDanAnhDaiDien);
        await _collection.UpdateOneAsync(boLoc, capNhat);
    }

    public async Task DanhDauHoanTatHoSoAsync(string id)
    {
        var boLoc = Builders<NguoiDung>.Filter.Eq(nd => nd.Id, id);
        var capNhat = Builders<NguoiDung>.Update.Set(nd => nd.DaXemHoanTatHoSo, true);
        await _collection.UpdateOneAsync(boLoc, capNhat);
    }

    public async Task CapNhatKhoaRsaAsync(string id, string khoaCongKhai, string khoaBiMat)
    {
        var boLoc = Builders<NguoiDung>.Filter.Eq(nd => nd.Id, id);
        var capNhat = Builders<NguoiDung>.Update
            .Set(nd => nd.KhoaCongKhai, khoaCongKhai)
            .Set(nd => nd.KhoaBiMat, khoaBiMat);
        await _collection.UpdateOneAsync(boLoc, capNhat);
    }
}
