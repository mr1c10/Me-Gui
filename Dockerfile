FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copia o csproj primeiro para aproveitar o cache do Docker durante o restore
COPY ["MeGui/MeGui.csproj", "MeGui/"]
RUN dotnet restore "MeGui/MeGui.csproj"

# Copia o resto dos arquivos do projeto
COPY . .
WORKDIR "/src/MeGui"
RUN dotnet build "MeGui.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MeGui.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Adicionando as chaves diretamente conforme solicitado (Aviso: Não recomendado em produção)
ENV Telegram__BotToken="8777923499:AAGtO37kuGwA_E6kdE505E3DjrrknpP4Yig"
ENV OpenRouter__ApiKey="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"
ENV OpenRouter__Model="openai/gpt-4o-mini"
ENV OpenRouter__BaseUrl="https://openrouter.ai/api/v1"

# A porta padrão do container web
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

ENTRYPOINT ["dotnet", "MeGui.dll"]