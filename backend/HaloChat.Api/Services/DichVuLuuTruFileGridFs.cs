using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace HaloChat.Api.Services;

public class DichVuLuuTruFileGridFs : IDichVuLuuTruFile
{
    private const string KhoaMetadataLoaiMime = "loaiMime";

    private readonly IGridFSBucket _bucket;

    public DichVuLuuTruFileGridFs(IGridFSBucket bucket)
    {
        _bucket = bucket;
    }

    public async Task<string> LuuAsync(Stream noiDung, string tenFile, string loaiMime)
    {
        var tuyChon = new GridFSUploadOptions
        {
            Metadata = new BsonDocument { { KhoaMetadataLoaiMime, loaiMime } },
        };
        var id = await _bucket.UploadFromStreamAsync(tenFile, noiDung, tuyChon);
        return id.ToString();
    }

    public async Task<KetQuaLayFile?> LayAsync(string id)
    {
        if (!ObjectId.TryParse(id, out var objectId))
        {
            return null;
        }

        GridFSFileInfo thongTin;
        try
        {
            var boLoc = Builders<GridFSFileInfo>.Filter.Eq(f => f.Id, objectId);
            thongTin = await (await _bucket.FindAsync(boLoc)).FirstOrDefaultAsync();
        }
        catch (GridFSFileNotFoundException)
        {
            return null;
        }

        if (thongTin is null)
        {
            return null;
        }

        var noiDung = await _bucket.DownloadAsBytesAsync(objectId);
        var loaiMime = thongTin.Metadata is not null && thongTin.Metadata.TryGetValue(KhoaMetadataLoaiMime, out var giaTri)
            ? giaTri.AsString
            : "application/octet-stream";

        return new KetQuaLayFile(noiDung, loaiMime, thongTin.Filename);
    }
}
