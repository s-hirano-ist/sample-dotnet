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

# SQLiteとData Protectionキーを書き込めるディレクトリを、実行ユーザー用に準備します。
RUN mkdir --parents /data /home/app/.aspnet/DataProtection-Keys \
    && chown --recursive $APP_UID:$APP_UID /data /home/app

# コンテナ内のHTTP待ち受けポートです。
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# root権限を持たないユーザーでアプリケーションを実行します。
USER $APP_UID

ENTRYPOINT ["dotnet", "TodoApi.dll"]
