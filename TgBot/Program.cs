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

            // Порт для Amvera
            builder.WebHost.UseUrls("http://*:8080");

            var telegramBotToken = builder.Configuration["TelegramBot:Token"];
            if (string.IsNullOrEmpty(telegramBotToken))
            {
                throw new InvalidOperationException("Token not found!");
            }

            // Логирование
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();

            // Telegram bot
            builder.Services.AddSingleton<ITelegramBotClient>(
                new TelegramBotClient(telegramBotToken));

            // База данных
            builder.Services.AddDbContext<DiplomContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Контроллеры
            builder.Services.AddControllers();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseRouting();
            app.UseAuthorization();

            // Проверка работы контейнера
            app.MapGet("/", () => "✅ Бот запущен и работает на порту 8080!");

            // Контроллеры
            app.MapControllers();

            // Установка webhook
            var botClient = app.Services.GetRequiredService<ITelegramBotClient>();
            var logger = app.Services.GetRequiredService<ILogger<Program>>();
            var webhookUrl = builder.Configuration["TelegramBot:WebhookUrl"];

            if (!string.IsNullOrEmpty(webhookUrl))
            {
                try
                {
                    await botClient.SetWebhook(
                        url: webhookUrl,
                        allowedUpdates: Array.Empty<UpdateType>()
                    );

                    logger.LogInformation($"🚀 Вебхук установлен: {webhookUrl}");
                }
                catch (Exception ex)
                {
                    logger.LogError($"❌ Ошибка вебхука: {ex.Message}");
                }
            }

            await app.RunAsync();
        }
    }
}
