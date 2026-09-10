using HaloChat.Api.Models;

namespace HaloChat.Api.Services;

public interface IDichVuJwt
{
    string TaoJwt(NguoiDung nguoiDung);
}
