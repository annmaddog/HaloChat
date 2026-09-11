using HaloChat.Api.Repositories;
using HaloChat.Api.Tests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HaloChat.Api.Tests;

/// <summary>
/// Fixture kiểm thử tích hợp: khởi động toàn bộ pipeline HTTP thật (qua
/// WebApplicationFactory) nhưng thay INguoiDungRepository bằng bản giả lập
/// trong bộ nhớ và cấp cấu hình Jwt/MongoDb giả cho môi trường kiểm thử,
/// để dotnet test không cần kết nối Atlas thật.
///
/// Lưu ý quan trọng: các giá trị cấu hình được cấp qua BIẾN MÔI TRƯỜNG
/// (không phải ConfigureAppConfiguration) vì Program.cs đọc "Jwt:ChuoiBiMat"
/// vào một biến cục bộ (dùng cho TokenValidationParameters) TRƯỚC khi gọi
/// builder.Build(); còn hook ConfigureWebHost của WebApplicationFactory chỉ
/// được chèn vào đúng lúc Build() chạy — tức là SAU dòng đọc đó. Nếu cấp
/// cấu hình qua ConfigureAppConfiguration, khóa ký JWT (đọc sớm) và khóa
/// dùng để tạo JWT qua IOptions&lt;TuyChonJwt&gt; (đọc trễ, sau Build()) sẽ
/// lệch nhau, khiến mọi request có Authorization header đều bị 401 dù token
/// hợp lệ. Biến môi trường có mặt ngay từ dòng đầu tiên của Program.cs nên
/// tránh được lệch pha này.
/// </summary>
public class ThietLapKiemThuTichHop : WebApplicationFactory<Program>
{
    public NguoiDungGiaLap KhoGiaLap { get; } = new();

    static ThietLapKiemThuTichHop()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        // Bỏ qua khối tạo chỉ mục duy nhất lúc khởi động (Finding 2): trong
        // kiểm thử không có kết nối MongoDB thật để tạo chỉ mục.
        Environment.SetEnvironmentVariable("BoQuaKhoiTaoChiMuc", "true");
        Environment.SetEnvironmentVariable("MongoDb__ChuoiKetNoi", "mongodb://localhost:27017");
        Environment.SetEnvironmentVariable("MongoDb__TenCoSoDuLieu", "HaloChatKiemThu");
        Environment.SetEnvironmentVariable(
            "Jwt__ChuoiBiMat",
            "khoa-bi-mat-du-dai-danh-cho-kiem-thu-tich-hop-toi-thieu-32-ky-tu");
        Environment.SetEnvironmentVariable("Jwt__NguoiPhatHanh", "HaloChat");
        Environment.SetEnvironmentVariable("Jwt__DoiTuong", "HaloChatNguoiDung");
        Environment.SetEnvironmentVariable("Jwt__SoPhutHetHan", "60");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(dichVu =>
        {
            dichVu.RemoveAll<INguoiDungRepository>();
            dichVu.AddSingleton<INguoiDungRepository>(KhoGiaLap);
        });
    }
}
