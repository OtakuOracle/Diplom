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

            var telegramBotToken = builder.Configuration["TelegramBot:Token"];

            if (string.IsNullOrEmpty(telegramBotToken))
            {
                throw new InvalidOperationException("Telegram bot token not configured. Please set 'TelegramBot:Token' in appsettings.json.");
            }

            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();

            builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(telegramBotToken));

            builder.Services.AddDbContext<DiplomContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddControllers(); 

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllers();

            var botClient = app.Services.GetRequiredService<ITelegramBotClient>();
            var logger = app.Services.GetRequiredService<ILogger<Program>>();

            var webhookUrl = builder.Configuration["TelegramBot:WebhookUrl"];

            if (string.IsNullOrEmpty(webhookUrl))
            {
                logger.LogWarning("TelegramBot:WebhookUrl не указан в конфигурации. Вебхук не будет установлен.");
            }
            else
            {
                try
                {
                    object value = botClient.SetWebhook(
                        url: webhookUrl,
                        allowedUpdates: Array.Empty<UpdateType>() 
                    );
                    logger.LogInformation($"Вебхук успешно установлен на: {webhookUrl}");
                }
                catch (Exception ex)
                {
                    logger.LogError($"Ошибка при установке вебхука: {ex.Message}");
                }

            }

            await app.RunAsync(); 
        }
    }
}
