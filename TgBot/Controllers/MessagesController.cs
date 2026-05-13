using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Elbrus.Models;

namespace TgBot.Controllers
{
    [ApiController]
    [Route("messages")]
    public class MessagesController : ControllerBase
    {
        private readonly ITelegramBotClient _botClient;
        private readonly ILogger<MessagesController> _logger;
        private readonly DiplomContext _db;

        public MessagesController(
            ITelegramBotClient botClient,
            ILogger<MessagesController> logger,
            DiplomContext db)
        {
            _botClient = botClient;
            _logger = logger;
            _db = db;
        }


        [HttpGet]
        public IActionResult Get()
        {
            return Ok("✅ Контроллер сообщений активен и готов к работе!");
        }

        [HttpPost]
        [Consumes("application/json")]
        public async Task<IActionResult> Post([FromBody] Update? update)
        {
            _logger.LogInformation("Telegram update received");

            if (update?.Message?.Text == null)
                return Ok();

            try
            {
                var chatId = update.Message.Chat.Id;
                var text = update.Message.Text;

                _logger.LogInformation($"Message: {text}");

                if (text == "/start")
                {
                    await _botClient.SendMessage(
                        chatId,
                        "🏔 Добро пожаловать в сервис услуг и инвентаря горнолыжного курорта!\nВведите /inventory чтобы посмотреть доступный инвентарь."
                    );
                }
                else if (text == "/inventory")
                {
                    var items = await _db.Inventories.ToListAsync();

                    if (!items.Any())
                    {
                        await _botClient.SendMessage(chatId, "Инвентарь пока не добавлен.");
                    }
                    else
                    {
                        var message = "🎿 Доступный инвентарь:\n\n";

                        foreach (var item in items)
                        {
                            message +=
                                $"• {item.InventoryName}\n" +
                                $"  Модель: {item.InventoryModel}\n" +
                                $"  Размер: {item.InventorySize}\n" +
                                $"  Цена за час: {item.RentalCostPerHour} ₽\n\n";
                        }

                        await _botClient.SendMessage(chatId, message);
                    }
                }
                else
                {
                    await _botClient.SendMessage(chatId, $"Вы написали: {text}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Telegram processing error");
            }

            return Ok();
        }


    }