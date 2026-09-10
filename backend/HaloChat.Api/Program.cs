using HaloChat.Api.Options;
using Microsoft.Extensions.Options;
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
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(TenChinhSachCors);
app.UseAuthorization();
app.MapControllers();

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
