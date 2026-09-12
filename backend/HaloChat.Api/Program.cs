using System.Text;
using HaloChat.Api.Options;
using HaloChat.Api.Repositories;
using HaloChat.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.Configure<TuyChonMongoDb>(builder.Configuration.GetSection(TuyChonMongoDb.TenMuc));
builder.Services.AddSingleton<IMongoDatabase>(sp =>
{
    var tuyChon = sp.GetRequiredService<IOptions<TuyChonMongoDb>>().Value;
    var client = new MongoClient(tuyChon.ChuoiKetNoi);
    return client.GetDatabase(tuyChon.TenCoSoDuLieu);
});

builder.Services.AddScoped<INguoiDungRepository, NguoiDungRepository>();
builder.Services.AddScoped<IDichVuMatKhau, DichVuMatKhau>();
builder.Services.AddScoped<IDichVuJwt, DichVuJwt>();
builder.Services.AddScoped<IDichVuNguoiDung, DichVuNguoiDung>();
builder.Services.AddScoped<ITinNhanRepository, TinNhanRepository>();
builder.Services.AddScoped<ILoiMoiKetBanRepository, LoiMoiKetBanRepository>();
builder.Services.AddScoped<IDichVuTinNhan, DichVuTinNhan>();
builder.Services.AddSignalR();
builder.Services.AddSingleton<Microsoft.AspNetCore.SignalR.IUserIdProvider, HaloChat.Api.Services.NguoiDungIdProvider>();

builder.Services.Configure<TuyChonJwt>(builder.Configuration.GetSection(TuyChonJwt.TenMuc));
var tuyChonJwt = builder.Configuration.GetSection(TuyChonJwt.TenMuc).Get<TuyChonJwt>()
    ?? throw new InvalidOperationException("Thiếu cấu hình Jwt trong appsettings/user-secrets.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = tuyChonJwt.NguoiPhatHanh,
            ValidateAudience = true,
            ValidAudience = tuyChonJwt.DoiTuong,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tuyChonJwt.ChuoiBiMat)),
        };
        // SignalR qua WebSocket/LongPolling không set được header Authorization —
        // client truyền JWT qua query string ?access_token=, chỉ chấp nhận cho
        // đúng đường dẫn Hub (spec §10.3).
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) &&
                    context.HttpContext.Request.Path.StartsWithSegments("/hub/chat"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            },
        };
    });
builder.Services.AddAuthorization();

const string TenChinhSachCors = "ChoPhepFrontend";
builder.Services.AddCors(options =>
{
    options.AddPolicy(TenChinhSachCors, policy =>
    {
        policy.WithOrigins(builder.Configuration["Cors:NguonChoPhep"] ?? "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.ParameterLocation.Header,
        Description = "Nhập: Bearer {token}",
    });
    options.AddSecurityRequirement(document => new Microsoft.OpenApi.OpenApiSecurityRequirement
    {
        [new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>(),
    });
});

var app = builder.Build();

if (!app.Configuration.GetValue<bool>("BoQuaKhoiTaoChiMuc"))
{
    using var scope = app.Services.CreateScope();
    var csdl = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
    var nguoiDungCollection = csdl.GetCollection<HaloChat.Api.Models.NguoiDung>("NguoiDung");
    var collation = new Collation("en", strength: CollationStrength.Secondary); // Không phân biệt hoa/thường
    var indexKeys1 = Builders<HaloChat.Api.Models.NguoiDung>.IndexKeys.Ascending(nd => nd.TenTaiKhoan);
    var indexKeys2 = Builders<HaloChat.Api.Models.NguoiDung>.IndexKeys.Ascending(nd => nd.Email);
    var indexOptions = new CreateIndexOptions { Unique = true, Collation = collation };
    await nguoiDungCollection.Indexes.CreateManyAsync(new[]
    {
        new CreateIndexModel<HaloChat.Api.Models.NguoiDung>(indexKeys1, indexOptions),
        new CreateIndexModel<HaloChat.Api.Models.NguoiDung>(indexKeys2, indexOptions),
    });
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(TenChinhSachCors);

// File tải lên phục vụ công khai qua đường dẫn tĩnh, không cần JWT — thẻ
// <img>/<a> không tự đính kèm được header Authorization. Tên file là GUID
// ngẫu nhiên nên chỉ ai có đúng đường link (nhận qua tin nhắn) mới xem được.
var thuMucTaiLen = Path.Combine(app.Environment.ContentRootPath, "uploads");
Directory.CreateDirectory(thuMucTaiLen);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(thuMucTaiLen),
    RequestPath = "/uploads",
});

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<HaloChat.Api.Hubs.ChatHub>("/hub/chat");

app.MapGet("/api/kiem-tra-suc-khoe", async (IMongoDatabase csdl) =>
{
    try
    {
        await csdl.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));
        return Results.Ok(new { ketNoiMongoDb = true });
    }
    catch (Exception loi)
    {
        return Results.Problem(detail: loi.Message, statusCode: 500);
    }
});

app.Run();

// Cho phép WebApplicationFactory<Program> trong dự án kiểm thử tích hợp
// truy cập lớp Program ngầm định sinh ra từ top-level statements.
public partial class Program
{
}
