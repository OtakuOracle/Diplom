FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /src

# 1. Копируем всё содержимое
COPY . .

# 2. Пробуем восстановить зависимости напрямую для проекта бота
# Мы указываем точный путь к .csproj
RUN dotnet restore "TgBot/TgBot.csproj"

# 3. Собираем проект бота
RUN dotnet publish "TgBot/TgBot.csproj" -c Release -o /app/out

# 4. Образ запуска
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app/out .

# 5. Проверьте, что в папке TgBot после сборки файл называется именно TgBot.dll
ENTRYPOINT ["dotnet", "TgBot.dll"]
