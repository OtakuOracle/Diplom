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

            // Настройка порта для Amvera
            builder.WebHost.UseUrls("http://*:8080");

            var telegramBotToken = builder.Configuration["TelegramBot:Token"];
            if (string.IsNullOrEmpty(telegramBotToken))
            {
                throw new InvalidOperationException("Token not found!");
            }

            // Сервисы
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(telegramBotToken));
            builder.Services.AddDbContext<DiplomContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddControllers().AddNewtonsoftJson();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseRouting();
            app.UseAuthorization();

            // 1. Главная страница (для проверки, что контейнер жив)
            app.MapGet("/", () => "✅ Бот запущен и работает на порту 8080!");

            // 2. Подключаем контроллеры (MessagesController сам заберет адрес /messages)
            app.MapControllers();

            // Установка вебхука
            var botClient = app.Services.GetRequiredService<ITelegramBotClient>();
            var logger = app.Services.GetRequiredService<ILogger<Program>>();
            var webhookUrl = builder.Configuration["TelegramBot:WebhookUrl"];

            if (!string.IsNullOrEmpty(webhookUrl))
            {
                try
                {
                    await botClient.SetWebhook(url: webhookUrl, allowedUpdates: Array.Empty<UpdateType>());
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
