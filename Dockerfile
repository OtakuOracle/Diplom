FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /src

# 1. Копируем всё
COPY . .

# 2. Выведем список файлов в лог, чтобы мы (и вы) увидели пути
RUN ls -R

# 3. Собираем проект. 
# ВНИМАНИЕ: Если папка называется tgbot (маленькими), замените TgBot на tgbot ниже!
RUN dotnet publish "TgBot/TgBot.csproj" -c Release -o /app/out

# Образ запуска
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app/out .

# Проверьте название вашего выходного файла. 
# Если проект TgBot.csproj, то файл будет TgBot.dll
ENTRYPOINT ["dotnet", "TgBot.dll"]
