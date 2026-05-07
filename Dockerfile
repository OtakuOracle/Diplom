# 1. Используем SDK 8.0 для сборки
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# 2. Копируем ВЕСЬ ваш код в контейнер сразу
# Это решит проблемы с путями между проектами TgBot и Elbrus
COPY . ./

# 3. Восстанавливаем зависимости для всего решения
RUN dotnet restore

# 4. Собираем конкретно проект бота
RUN dotnet publish TgBot/TgBot.csproj -c Release -o out


# 5. Финальный образ для запуска
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app/out .

# 6. Запуск
# Проверьте: если ваш проект называется "TgBot", то и файл будет "TgBot.dll"
# В Linux регистр букв ВАЖЕН. Если папка TgBot, то и в команде пишем TgBot
ENTRYPOINT ["dotnet", "TgBot.dll"]
