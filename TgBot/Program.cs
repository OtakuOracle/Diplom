using Microsoft.EntityFrameworkCore;
using Elbrus.Models;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace TgBot
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Было http://*:80, меняем на 8080
            builder.WebHost.UseUrls("http://*:8080");


            var telegramBotToken = builder.Configuration["TelegramBot:Token"];

            if (string.IsNullOrEmpty(telegramBotToken))
            {
                throw new InvalidOperationException("Telegram bot token not configured. Please set 'TelegramBot:Token' in appsettings.json.");
            }

            // Настройка логирования
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();

            // Регистрация клиента Telegram
            builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(telegramBotToken));

            // Настройка базы данных
            builder.Services.AddDbContext<DiplomContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Регистрация контроллеров (NewtonsoftJson нужен для работы с типами Telegram.Bot)
            builder.Services.AddControllers().AddNewtonsoftJson();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Включаем Swagger для удобства отладки (будет доступен по адресу /swagger)
            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseRouting();
            app.UseAuthorization();

            // 2. ЗАЩИТА ОТ 404: Добавляем обработку корневого адреса
            // Теперь при заходе на https://amvera-otakuoracle-run-tgbot.amvera.io/ 
            // вы увидите текст, а не ошибку 404.
            app.MapGet("/", () => "Telegram Bot API is running on port 80...");
            app.MapGet("/test", () => "Test route is working!");

            // Мапим контроллеры (ваш MessagesController)
            app.MapControllers();

            var botClient = app.Services.GetRequiredService<ITelegramBotClient>();
            var logger = app.Services.GetRequiredService<ILogger<Program>>();

            var webhookUrl = builder.Configuration["TelegramBot:WebhookUrl"];

            if (string.IsNullOrEmpty(webhookUrl))
            {
                logger.LogWarning("ВНИМАНИЕ: TelegramBot:WebhookUrl не указан в appsettings.json. Вебхук не будет установлен.");
            }
            else
            {
                try
                {
                    // Установка вебхука при старте приложения
                    await botClient.SetWebhook(
                        url: webhookUrl,
                        allowedUpdates: Array.Empty<UpdateType>()
                    );
                    logger.LogInformation($"УСПЕХ: Вебхук установлен на адрес: {webhookUrl}");
                }
                catch (Exception ex)
                {
                    logger.LogError($"ОШИБКА: Не удалось установить вебхук: {ex.Message}");
                }
            }

            // Запуск приложения
            await app.RunAsync();
        }
    }
}