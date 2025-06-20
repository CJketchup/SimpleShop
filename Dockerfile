# 基礎執行階段映像（使用 .NET 8.0）
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

# 建置階段映像（含 SDK）
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 複製整個專案資料夾
COPY . .

# 還原 NuGet 套件（請確認你專案名稱是 SimpleShop）
RUN dotnet restore SimpleShop/SimpleShop.csproj

# 建立並發布專案
RUN dotnet publish SimpleShop/SimpleShop.csproj -c Release -o /app/publish

# 最終映像
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# 啟動網站
ENTRYPOINT ["dotnet", "SimpleShop.dll"]
