# 1. Сборка
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Копируем сначала только файлы, которые нужны для восстановления зависимостей
# Это позволяет Aprovecha кэширование Docker, если код меняется, а зависимости нет.
COPY *.sln ./
# Если у вас есть .editorconfig, .gitattributes и т.п., скопируйте их тоже
# COPY .editorconfig ./

# Копируем файлы проектов, чтобы dotnet restore мог их найти
COPY TgBot/TgBot.csproj ./TgBot/
# Скопируйте другие файлы проектов, если они есть, например:
# COPY Shared/Shared.csproj ./Shared/

# Теперь восстанавливаем зависимости для всего решения
# Если .sln находится в корне, dotnet restore найдет его автоматически
RUN dotnet restore

# Копируем остальной код приложения
COPY . .

# Собираем проект бота
RUN dotnet publish TgBot/TgBot.csproj -c Release -o out

# 2. Запуск
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app/out .

ENTRYPOINT ["dotnet", "TgBot.dll"]
