FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /src

# 1. Копируем всё содержимое репозитория
COPY . .

# 2. Умная сборка: ищем любой .csproj в папке TgBot (или tgbot) и собираем его
# Это сработает, даже если папка называется TgBot, а проект внутри иначе.
RUN dotnet publish $(find . -name "*.csproj" | grep -i "TgBot") -c Release -o /app/out

# 3. Образ для запуска
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app/out .

# 4. Запуск. 
# ВНИМАНИЕ: Если ваш основной файл называется не TgBot.dll, 
# напишите его точное название ниже вместо TgBot.dll
ENTRYPOINT ["dotnet", "TgBot.dll"]
