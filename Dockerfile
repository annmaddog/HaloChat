# Build image cho HaloChat.Api — build context là GỐC repo (không phải backend/)
# vì solution có nhiều project tham chiếu chéo (HaloChat.Api -> HaloChat.Security).

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy trước các file .csproj/.sln để tận dụng cache layer của Docker khi restore
# (chỉ restore lại khi các file này đổi, không phải mỗi lần đổi code .cs).
COPY backend/HaloChat.sln backend/
COPY backend/HaloChat.Api/HaloChat.Api.csproj backend/HaloChat.Api/
COPY backend/HaloChat.Security/HaloChat.Security.csproj backend/HaloChat.Security/
COPY backend/HaloChat.Api.Tests/HaloChat.Api.Tests.csproj backend/HaloChat.Api.Tests/

RUN dotnet restore backend/HaloChat.Api/HaloChat.Api.csproj

COPY backend/ backend/
RUN dotnet publish backend/HaloChat.Api/HaloChat.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Render cấp cổng qua biến môi trường PORT — ứng dụng phải lắng nghe đúng cổng đó.
# Dùng "sh -c" (shell form) để $PORT được thay giá trị thật lúc container khởi động
# (không dùng được với ENTRYPOINT dạng mảng exec, vì exec form không qua shell).
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet HaloChat.Api.dll"]
