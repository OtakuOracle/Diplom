# 1. Сборка
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Копируем всё (включая папку Elbrus и TgBot)
COPY . ./

# Восстанавливаем зависимости для всего решения
RUN dotnet restore

# Собираем проект бота
# ВАЖНО: Проверьте, что в папке TgBot файл называется именно TgBot.csproj
RUN dotnet publish TgBot/TgBot.csproj -c Release -o out

# 2. Запуск
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app/out .

# Имя DLL должно совпадать с названием вашего проекта
ENTRYPOINT ["dotnet", "TgBot.dll"]
