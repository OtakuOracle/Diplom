# 1. Образ для сборки (SDK)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# 2. Копируем файлы проектов (.csproj)
# Это нужно для правильного восстановления зависимостей (dotnet restore)
COPY TgBot/*.csproj ./TgBot/
COPY Elbrus/*.csproj ./Elbrus/

# 3. Восстанавливаем зависимости бота
# Он автоматически подтянет зависимости из проекта Elbrus, так как мы скопировали его .csproj
RUN dotnet restore TgBot/TgBot.csproj

# 4. Копируем все исходные файлы из обеих папок
COPY . ./

# 5. Собираем проект бота
# Флаг --no-restore ускоряет сборку, так как мы уже сделали restore выше
RUN dotnet publish TgBot/TgBot.csproj -c Release -o out

# 6. Образ для запуска (Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app/out .

# 7. Запуск бота
# Убедитесь, что после сборки файл в папке out называется TgBot.dll
ENTRYPOINT ["dotnet", "TgBot.dll"]
