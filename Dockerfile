# ビルド専用イメージです。SDKでアプリケーションを発行します。
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# 先にプロジェクトファイルだけコピーすると、ソース変更時も復元結果を再利用しやすくなります。
COPY TodoApi/TodoApi.csproj TodoApi/
RUN dotnet restore TodoApi/TodoApi.csproj

COPY TodoApi/ TodoApi/
RUN dotnet publish TodoApi/TodoApi.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

# 実行時はSDKを含まない軽量なASP.NET Coreランタイムイメージを使います。
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

# ComposeのhealthcheckでHTTPレスポンスを確認するためcurlを入れます。
RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# コンテナ内のHTTP待ち受けポートです。
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "TodoApi.dll"]
