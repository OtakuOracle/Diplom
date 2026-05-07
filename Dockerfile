# 1. Сборка
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Копируем сначала только файлы, которые нужны для восстановления зависимостей
# Это позволяет Aprovecha кэширование Docker, если код меняется, а зависимости нет.
# ИЗМЕНЕНИЕ: Использование *.slnx вместо *.sln
COPY *.slnx ./
# Если у вас есть .editorconfig, .gitattributes и т.п., скопируйте их тоже
# COPY .editorconfig ./

# Тут мы должны скопировать файлы проектов, чтобы dotnet restore мог их найти.
# так как Elbrus.slnx находится в корне, а проекты в папках TgBot и Elbrus
# Копируем все содержимое папок проектов
COPY TgBot/ ./TgBot/
COPY Elbrus/ ./Elbrus/

# Теперь восстанавливаем зависимости для всего решения
# Так как Elbrus.slnx находится в корне, dotnet restore найдет его автоматически
RUN dotnet restore

# Собираем проект бота TgBot
# ВАЖНО: Проверьте, что в папке TgBot файл называется именно TgBot.csproj
RUN dotnet publish TgBot/TgBot.csproj -c Release -o out

# 2. Запуск
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app/out .

# Имя DLL должно совпадать с названием вашего проекта
ENTRYPOINT ["dotnet", "TgBot.dll"]
