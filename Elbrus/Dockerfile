# Используем официальный образ .NET SDK для сборки
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Копируем файлы проекта и восстанавливаем зависимости
# Если у вас файл проекта называется по-другому, укажите его имя вместо *.csproj
COPY *.csproj ./
RUN dotnet restore

# Копируем всё остальное и собираем проект
COPY . ./
RUN dotnet publish -c Release -o out

# Используем образ среды выполнения (runtime)
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app/out .

# Команда для запуска (УБЕДИТЕСЬ, ЧТО ТУТ ИМЯ ВАШЕГО ПРОЕКТА)
ENTRYPOINT ["dotnet", "Elbrus.dll"]
