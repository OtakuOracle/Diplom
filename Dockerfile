# Используем SDK версии 10.0 для сборки
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-env
WORKDIR /app

# Копируем файлы
COPY . ./

# Заходим в папку с ботом
WORKDIR /app/TgBot

# Восстанавливаем зависимости и собираем проект
RUN dotnet restore
RUN dotnet publish -c Release -o /out

# Используем Runtime версии 10.0 для запуска
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build-env /out .

# Запуск (убедитесь, что файл называется TgBot.dll)
ENTRYPOINT ["dotnet", "TgBot.dll"]
