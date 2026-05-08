FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Копируем абсолютно всё
COPY . ./

# ЭТА СТРОКА ВАЖНА: она покажет нам структуру файлов в логах, если сборка упадет
RUN ls -R

# Мы заходим в папку проекта и запускаем сборку оттуда
WORKDIR /app/TgBot
RUN dotnet restore
RUN dotnet publish -c Release -o /out

# Образ запуска
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /out .

# Убедитесь, что ваш выходной файл называется именно TgBot.dll
# Если проект называется иначе, поменяйте имя ниже
ENTRYPOINT ["dotnet", "TgBot.dll"]
