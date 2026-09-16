namespace HaloChat.Api.Repositories;

public interface ITinNhanAnRepository
{
    Task AnAsync(string nguoiDungId, string tinNhanId);

    /// <summary>Trả về tập id trong tinNhanIds mà nguoiDungId đã ẩn.</summary>
    Task<HashSet<string>> LayDanhSachIdDaAnAsync(string nguoiDungId, IEnumerable<string> tinNhanIds);
}
