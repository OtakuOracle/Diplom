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
        private static Dictionary<long, string> _userStates = new();
        private static Dictionary<long, int> _authorizedUsers = new();
        private static Dictionary<long, string> _tempEmail = new();
        private static Dictionary<long, int> _tempService = new();
        private static Dictionary<long, DateOnly> _tempDate = new();
        private static Dictionary<long, TimeOnly> _tempTimeIn = new();

        private Dictionary<long, int> _tempOrders = new();




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

        private string GetIcon(string name)
        {
            name = name.ToLower();

            if (name.Contains("лыж")) return "🎿";
            if (name.Contains("сноуборд")) return "🏂";
            if (name.Contains("перчат")) return "🧤";
            if (name.Contains("очк")) return "🥽";
            if (name.Contains("коньк")) return "⛸";
            if (name.Contains("шлем")) return "🪖";
            if (name.Contains("снегоход") || name.Contains("снегокат")) return "🛷";

            if (name.Contains("урок") || name.Contains("обучен") || name.Contains("инструктор"))
                return "🧑🏫";

            if (name.Contains("экскурс"))
                return "🏔";

            if (name.Contains("гид"))
                return "🧭";

            if (name.Contains("сервис") || name.Contains("ремонт"))
                return "🛠";

            if (name.Contains("аренда"))
                return "📦";

            return "🎒";
        }


        private async Task SendInventory(long chatId)
        {
            var items = await _db.Inventories.ToListAsync();

            var buttons = items
                .Select(x => InlineKeyboardButton.WithCallbackData(
                    $"{GetIcon(x.InventoryName)} {x.InventoryName}",
                    $"inv_{x.InventoryId}"
                ))
                .Select(x => new[] { x })
                .ToList();

            var keyboard = new InlineKeyboardMarkup(buttons);

            await _botClient.SendMessage(
                chatId,
                "🎿 Выберите инвентарь:",
                replyMarkup: keyboard
            );
        }

        private async Task SendServices(long chatId, int messageId)
        {
            var services = await _db.Services.ToListAsync();

            var buttons = services
                .Select(x => InlineKeyboardButton.WithCallbackData(
                    $"{GetIcon(x.ServiceName)} {x.ServiceName}",
                    $"srv_{x.ServiceId}"
                ))
                .Select(x => new[] { x })
                .ToArray();

            var keyboard = new InlineKeyboardMarkup(buttons);

            await _botClient.EditMessageText(
                chatId,
                messageId,
                "🧑🏫 Выберите услугу:",
                replyMarkup: keyboard
            );
        }

        [HttpPost]
        [Consumes("application/json")]
        public async Task<IActionResult> Post([FromBody] Update update)
        {
            try
            {

                if (update.Message != null && update.Message.Text != null)
                {
                    var chatId = update.Message.Chat.Id;
                    var text = update.Message.Text;

                    if (text == "/start")
                    {
                        _userStates[chatId] = "wait_email";

                        await _botClient.SendMessage(chatId,
                            "🏔 Добро пожаловать!\n\nВведите email:");

                        return Ok();
                    }

                    if (_userStates.ContainsKey(chatId))
                    {
                        var state = _userStates[chatId];

                        if (state == "wait_email")
                        {
                            _tempEmail[chatId] = text;
                            _userStates[chatId] = "wait_password";

                            await _botClient.SendMessage(chatId, "Введите пароль:");
                            return Ok();
                        }

                        if (state == "wait_password")
                        {
                            var email = _tempEmail[chatId];

                            var client = await _db.Clients
                                .FirstOrDefaultAsync(x => x.Email == email && x.Password == text);

                            if (client != null)
                            {
                                _authorizedUsers[chatId] = client.ClientId;
                                _userStates.Remove(chatId);

                                var keyboard = new InlineKeyboardMarkup(new[]
                                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("🎿 Инвентарь","open_inventory")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("🧑🏫 Услуги","open_services")
                    }
                });

                                await _botClient.SendMessage(
                                    chatId,
                                    $"✅ Вы вошли как {client.FullName}",
                                    replyMarkup: keyboard
                                );
                            }
                            else
                            {
                                await _botClient.SendMessage(chatId,
                                    "❌ Неверный email или пароль");
                            }

                            return Ok();
                        }
                    }
                }


                if (update.CallbackQuery != null)
                {
                    var chatId = update.CallbackQuery.Message.Chat.Id;
                    var messageId = update.CallbackQuery.Message.MessageId;
                    var data = update.CallbackQuery.Data;

                    if (data == "back_to_start")
                    {
                        var keyboard = new InlineKeyboardMarkup(new[]
                        {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("🎿 Инвентарь", "open_inventory")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("🧑🏫 Услуги", "open_services")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("🔑 Войти", "login")
                    }
                });

                        await _botClient.EditMessageText(
                            chatId,
                            messageId,
                            "🏔 Добро пожаловать в сервис бронирования!",
                            replyMarkup: keyboard
                        );

                        return Ok();
                    }

                    if (data == "login")
                    {
                        _userStates[chatId] = "wait_email";

                        await _botClient.SendMessage(chatId, "Введите email:");
                        return Ok();
                    }

                    if (data == "open_inventory")
                    {
                        if (!_authorizedUsers.ContainsKey(chatId))
                        {
                            await _botClient.SendMessage(chatId, "Сначала авторизуйтесь.");
                            return Ok();
                        }

                        var keyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Сегодня", $"date_{DateTime.Now:yyyy-MM-dd}")
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Завтра", $"date_{DateTime.Now.AddDays(1):yyyy-MM-dd}")
                        }
                    });

                        await _botClient.SendMessage(chatId, "📅 Выберите дату:", replyMarkup: keyboard);
                        return Ok();
                    }


                    if (data.StartsWith("date_"))
                    {
                        var date = DateOnly.Parse(data.Replace("date_", ""));

                        var clientId = _authorizedUsers[chatId];

                        var order = new Order
                        {
                            ClientId = clientId,
                            DateCreate = DateOnly.FromDateTime(DateTime.Now),
                            TimeCreate = TimeOnly.FromDateTime(DateTime.Now),
                            TotalPrice = 0
                        };

                        _db.Orders.Add(order);
                        await _db.SaveChangesAsync();

                        _tempOrders[chatId] = order.OrderId;

                        var orderService = new OrderService
                        {
                            OrderId = order.OrderId,
                            Date = date,
                            OrderStatusId = 1
                        };

                        _db.OrderServices.Add(orderService);
                        await _db.SaveChangesAsync();

                        var keyboard = new InlineKeyboardMarkup(new[]
                        {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("10:00", "timein_10"),
                            InlineKeyboardButton.WithCallbackData("12:00", "timein_12")
                        }
                    });

                        await _botClient.SendMessage(chatId, "⏰ Выберите время начала:", replyMarkup: keyboard);

                        return Ok();
                    }


                    if (data.StartsWith("timein_"))
                    {
                        var hour = int.Parse(data.Replace("timein_", ""));
                        var orderId = _tempOrders[chatId];

                        var os = await _db.OrderServices.FirstOrDefaultAsync(x => x.OrderId == orderId);

                        os.TimeIn = new TimeOnly(hour, 0);

                        await _db.SaveChangesAsync();

                        var keyboard = new InlineKeyboardMarkup(new[]
                        {
        new[]
        {
            InlineKeyboardButton.WithCallbackData("14:00", "timeout_14"),
            InlineKeyboardButton.WithCallbackData("16:00", "timeout_16")
        }
    });

                        await _botClient.SendMessage(chatId, "⏳ Выберите время окончания:", replyMarkup: keyboard);

                        return Ok();
                    }
                    if (data.StartsWith("timeout_"))
                    {
                        var hour = int.Parse(data.Replace("timeout_", ""));
                        var orderId = _tempOrders[chatId];

                        var os = await _db.OrderServices.FirstOrDefaultAsync(x => x.OrderId == orderId);

                        os.TimeOut = new TimeOnly(hour, 0);

                        int rentTime = os.TimeOut.Value.Hour - os.TimeIn.Value.Hour;

                        if (rentTime <= 0)
                        {
                            await _botClient.SendMessage(chatId, "❗️ Время окончания должно быть позже начала.");
                            return Ok();
                        }

                        os.RentTime = rentTime;

                        await _db.SaveChangesAsync();

                        // ✅ теперь показываем инвентарь
                        var items = await _db.Inventories.ToListAsync();

                        var buttons = items
                            .Select(i => new[]
                            {
            InlineKeyboardButton.WithCallbackData(
                $"{i.InventoryName}",
                $"rent_item_{i.InventoryId}"
            )
                            })
                            .ToList();

                        await _botClient.SendMessage(
                            chatId,
                            "✅ Время выбрано!\nТеперь выберите инвентарь:",
                            replyMarkup: new InlineKeyboardMarkup(buttons)
                        );

                        return Ok();
                    }






                    if (data == "open_services")
                    {
                        var services = await _db.Services.ToListAsync();

                        var buttons = services
                            .Select(x => InlineKeyboardButton.WithCallbackData(
                                x.ServiceName,
                                $"srv_{x.ServiceId}"
                            ))
                            .Select(x => new[] { x })
                            .ToList();

                        buttons.Add(new[]
                        {
                    InlineKeyboardButton.WithCallbackData("⬅️ Назад", "back_to_start")
                });

                        var keyboard = new InlineKeyboardMarkup(buttons);

                        await _botClient.EditMessageText(
                            chatId,
                            messageId,
                            "🧑🏫 Выберите услугу:",
                            replyMarkup: keyboard
                        );

                        return Ok();
                    }
                    if (data.StartsWith("inv_"))
                    {
                        if (!_authorizedUsers.ContainsKey(chatId))
                        {
                            await _botClient.SendMessage(chatId, "Сначала авторизуйтесь.");
                            return Ok();
                        }

                        var inventoryId = int.Parse(data.Replace("inv_", ""));

                        var items = await _db.InventoryItems
                            .Where(x => x.InventoryId == inventoryId)
                            .ToListAsync();

                        var buttons = items
                            .Select(x => InlineKeyboardButton.WithCallbackData(
                                $"Размер: {x.Size}",
                                $"item_{x.InventoryItemId}"
                            ))
                            .Select(x => new[] { x })
                            .ToList();

                        await _botClient.EditMessageText(
                            chatId,
                            messageId,
                            "Выберите размер:",
                            replyMarkup: new InlineKeyboardMarkup(buttons)
                        );

                        return Ok();
                    }

                    if (data.StartsWith("item_"))
                    {
                        var itemId = int.Parse(data.Replace("item_", ""));

                        var item = await _db.InventoryItems
                            .Include(x => x.Inventory)
                            .FirstOrDefaultAsync(x => x.InventoryItemId == itemId);

                        if (item == null)
                            return Ok();

                        var keyboard = new InlineKeyboardMarkup(new[]
                        {
        new[]
        {
            InlineKeyboardButton.WithCallbackData(
                "✅ Добавить в корзину",
                $"rent_item_{item.InventoryItemId}"
            )
        }
    });

                        await _botClient.EditMessageText(
                            chatId,
                            messageId,
                            $"Размер: {item.Size}\nЦена: {item.Inventory.RentalCostPerHour} ₽ / час",
                            replyMarkup: keyboard
                        );

                        return Ok();
                    }


                    if (data.StartsWith("rent_item_"))
                    {
                        if (!_authorizedUsers.ContainsKey(chatId))
                        {
                            await _botClient.SendMessage(chatId, "Сначала авторизуйтесь.");
                            return Ok();
                        }

                        var itemId = int.Parse(data.Replace("rent_item_", ""));
                        var clientId = _authorizedUsers[chatId];

                        var item = await _db.InventoryItems.FindAsync(itemId);
                        if (item == null) return Ok();

                        // ✅ создаём заказ если нет
                        if (!_tempOrders.ContainsKey(chatId))
                        {
                            var order = new Order
                            {
                                ClientId = clientId,
                                DateCreate = DateOnly.FromDateTime(DateTime.Now),
                                TimeCreate = TimeOnly.FromDateTime(DateTime.Now),
                                TotalPrice = 0
                            };

                            _db.Orders.Add(order);
                            await _db.SaveChangesAsync();

                            _tempOrders[chatId] = order.OrderId;
                        }

                        var orderId = _tempOrders[chatId];

                        // ✅ создаём OrderService
                        var orderService = new OrderService
                        {
                            OrderId = orderId,
                            ServiceId = null,
                            RentTime = 1,
                            OrderStatusId = 1
                        };

                        _db.OrderServices.Add(orderService);
                        await _db.SaveChangesAsync();

                        // ✅ добавляем инвентарь
                        var orderInventory = new OrderInventory
                        {
                            InventoryItemId = item.InventoryItemId,
                            OrderServiceId = orderService.OrderServiceId,
                            RentTime = 1
                        };

                        _db.OrderInventories.Add(orderInventory);
                        await _db.SaveChangesAsync();

                        // ✅ ПОКАЗЫВАЕМ ТОЛЬКО УСЛУГИ (без оформления)
                        var services = await _db.Services.ToListAsync();

                        var buttons = services
                            .Select(s => new[]
                            {
            InlineKeyboardButton.WithCallbackData(
                $"{s.ServiceName} ({s.CostPerHour} ₽)",
                $"service_{s.ServiceId}"
            )
                            })
                            .ToList();

                        await _botClient.SendMessage(
                            chatId,
                            "✅ Инвентарь добавлен.\nТеперь выберите услугу:",
                            replyMarkup: new InlineKeyboardMarkup(buttons)
                        );

                        return Ok();
                    }

                    if (data.StartsWith("service_"))
                    {
                        var serviceId = int.Parse(data.Replace("service_", ""));
                        var orderId = _tempOrders[chatId];

                        var orderService = await _db.OrderServices
                            .FirstOrDefaultAsync(x => x.OrderId == orderId && x.ServiceId == null);

                        if (orderService != null)
                        {
                            orderService.ServiceId = serviceId;
                            await _db.SaveChangesAsync();
                        }

                        // ✅ теперь можно оформлять
                        var keyboard = new InlineKeyboardMarkup(new[]
                        {
        new[]
        {
            InlineKeyboardButton.WithCallbackData("💰 Оформить заказ", "checkout")
        }
    });

                        await _botClient.SendMessage(
                            chatId,
                            "✅ Услуга добавлена. Теперь оформите заказ:",
                            replyMarkup: keyboard
                        );

                        return Ok();
                    }

                    if (data == "checkout")
                    {
                        var orderId = _tempOrders[chatId];

                        var orderServices = await _db.OrderServices
                            .Include(x => x.OrderInventories)
                                .ThenInclude(oi => oi.InventoryItem)
                                    .ThenInclude(ii => ii.Inventory)
                            .ToListAsync();

                        int total = 0;

                        foreach (var os in orderServices.Where(x => x.OrderId == orderId))
                        {
                            if (os.ServiceId == null)
                            {
                                await _botClient.SendMessage(chatId, "❗️ Сначала выберите услугу.");
                                return Ok();
                            }

                            var service = await _db.Services.FindAsync(os.ServiceId);

                            // защита от null (int?)
                            int rentTime = os.RentTime ?? 0;
                            int servicePrice = service.CostPerHour ?? 0;

                            // ✅ услуга
                            total += servicePrice * rentTime;

                            // ✅ инвентарь
                            foreach (var inv in os.OrderInventories)
                            {
                                var item = inv.InventoryItem;

                                int invPrice = item.Inventory.RentalCostPerHour ?? 0;

                                total += invPrice * rentTime;
                            }
                        }

                        var order = await _db.Orders.FindAsync(orderId);

                        order.TotalPrice = total;

                        await _db.SaveChangesAsync();

                        await _botClient.SendMessage(chatId, $"✅ Заказ оформлен!\n💰 Итог: {total} ₽");

                        _tempOrders.Remove(chatId);

                        return Ok();
                    }
                }
            }



            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Ok();
            }
            return Ok();
        }
    }
}