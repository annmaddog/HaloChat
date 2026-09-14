using HaloChat.Api.Services;

namespace HaloChat.Api.Tests.Fakes;

public class DichVuLuuTruFileGiaLap : IDichVuLuuTruFile
{
    private readonly Dictionary<string, KetQuaLayFile> _luuTru = new();

    public Task<string> LuuAsync(Stream noiDung, string tenFile, string loaiMime)
    {
        using var boDem = new MemoryStream();
        noiDung.CopyTo(boDem);
        var id = Guid.NewGuid().ToString();
        _luuTru[id] = new KetQuaLayFile(boDem.ToArray(), loaiMime, tenFile);
        return Task.FromResult(id);
    }

    public Task<KetQuaLayFile?> LayAsync(string id)
    {
        _luuTru.TryGetValue(id, out var ketQua);
        return Task.FromResult(ketQua);
    }
}
