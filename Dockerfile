# 1. Используем SDK 8.0 для сборки
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# 2. Копируем ВЕСЬ корень проекта (все папки и файл .slnx)
COPY . ./

# 3. Восстанавливаем зависимости для всего решения сразу
# Это важно, чтобы проекты TgBot и Elbrus "увидели" друг друга
RUN dotnet restore

# 4. Собираем именно бота. 
# Убедитесь, что внутри папки TgBot файл называется именно TgBot.csproj (с большой буквы!)
RUN dotnet publish TgBot/TgBot.csproj -c Release -o out

# 5. Образ для запуска
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app/out .

# 6. Запуск бота
# Если после сборки файл называется TgBot.dll, оставляем так.
# Если вдруг он называется по-другому, поправьте имя.
ENTRYPOINT ["dotnet", "TgBot.dll"]
