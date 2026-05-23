using Elbrus.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using static System.Runtime.InteropServices.JavaScript.JSType;

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

        private Dictionary<long, DateOnly> _selectedDates = new();


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

        // Передаем messageId как параметр
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
                    var chatId = update.Message.Chat.Id; // chatId корректно определяется
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

                                var keyboard = new InlineKeyboardMarkup(new[] // keyboard определяется здесь
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
                    var chatId = update.CallbackQuery.Message.Chat.Id; // chatId корректно определяется
                    var messageId = update.CallbackQuery.Message.MessageId; // messageId корректно определяется
                    var data = update.CallbackQuery.Data; // data корректно определяется

                    if (data == "back_to_start")
                    {
                        var keyboard = new InlineKeyboardMarkup(new[] // keyboard определяется здесь
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

                        var keyboard = new InlineKeyboardMarkup(new[] // keyboard определяется здесь
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
                        if (!_authorizedUsers.TryGetValue(chatId, out var clientId))
                        {
                            await _botClient.SendMessage(chatId, "❗️ Сначала авторизуйтесь.");
                            return Ok();
                        }

                        var date = DateOnly.Parse(data.Replace("date_", ""));

                        // ✅ сохраняем дату
                        _selectedDates[chatId] = date;

                        // ✅ создаём заказ (без времени пока)
                        var order = new Order
                        {
                            ClientId = clientId,
                            DateCreate = date,
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
                        // ✅ проверка даты
                        if (!_selectedDates.TryGetValue(chatId, out var selectedDate))
                        {
                            await _botClient.SendMessage(chatId, "❗️ Сначала выберите дату.");
                            return Ok();
                        }

                        if (!_tempOrders.TryGetValue(chatId, out var orderId))
                        {
                            await _botClient.SendMessage(chatId, "❗️ Заказ не найден.");
                            return Ok();
                        }

                        var hour = int.Parse(data.Replace("timein_", ""));
                        var os = await _db.OrderServices.FirstOrDefaultAsync(x => x.OrderId == orderId);

                        if (os == null)
                        {
                            await _botClient.SendMessage(chatId, "❗️ Ошибка бронирования.");
                            return Ok();
                        }

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


                    // Удалена лишняя строка, где keyboard не был определен
                    // await _botClient.SendMessage(chatId, "⏳ Выберите время окончания:", replyMarkup: keyboard);

                    // return Ok(); // Эта строка была не на своем месте, удалена

                    if (data.StartsWith("timeout_"))
                    {
                        // ✅ проверка даты
                        if (!_selectedDates.TryGetValue(chatId, out var selectedDate))
                        {
                            await _botClient.SendMessage(chatId, "⚠️ Сначала выберите дату.");
                            return Ok();
                        }

                        if (!_tempOrders.TryGetValue(chatId, out var orderId))
                        {
                            await _botClient.SendMessage(chatId, "⚠️ Сессия истекла. Начните заново.");
                            return Ok();
                        }

                        var hour = int.Parse(data.Replace("timeout_", ""));
                        var os = await _db.OrderServices.FirstOrDefaultAsync(x => x.OrderId == orderId);

                        if (os == null)
                        {
                            await _botClient.SendMessage(chatId, "⚠️ Не удалось продолжить бронирование.");
                            return Ok();
                        }

                        os.TimeOut = new TimeOnly(hour, 0);

                        if (os.TimeIn.HasValue && os.TimeOut.HasValue)
                        {
                            if (os.TimeOut <= os.TimeIn)
                            {
                                await _botClient.SendMessage(chatId, "❗️ Время окончания должно быть позже времени начала");
                                return Ok();
                            }

                            os.RentTime = (int)(os.TimeOut.Value.ToTimeSpan() - os.TimeIn.Value.ToTimeSpan()).TotalHours;
                        }
                        else
                        {
                            os.RentTime = 0;
                        }

                        await _db.SaveChangesAsync();

                        // ✅ очищаем дату (всё, этап завершён)
                        _selectedDates.Remove(chatId);

                        await SendInventory(chatId);

                        return Ok();
                    }



                    if (data == "open_services")
                    {
                        var services = await _db.Services.ToListAsync();

                        var buttons = services
                            .Select(x => new[]
                            {
            InlineKeyboardButton.WithCallbackData(
                x.ServiceName,
                $"serviceinfo_{x.ServiceId}" // ✅ теперь не бронирование
            )
                            })
                            .ToList();

                        buttons.Add(new[]
                        {
        InlineKeyboardButton.WithCallbackData("⬅️ Назад", "back_to_start")
    });

                        await _botClient.EditMessageText(
                            chatId,
                            messageId,
                            "📋 Выберите услугу:",
                            replyMarkup: new InlineKeyboardMarkup(buttons)
                        );

                        return Ok();
                    }


                    if (data.StartsWith("serviceinfo_"))
                    {
                        var serviceId = int.Parse(data.Replace("serviceinfo_", ""));

                        var service = await _db.Services.FindAsync(serviceId);

                        if (service == null)
                        {
                            await _botClient.SendMessage(chatId, "❗️ Услуга не найдена.");
                            return Ok();
                        }

                        var text =
                            $"📌 {service.ServiceName}\n\n" +
                            $"💰 Цена: {service.CostPerHour} ₽/час";

                        var keyboard = new InlineKeyboardMarkup(new[]
                        {
        new[]
        {
            InlineKeyboardButton.WithCallbackData("✅ Забронировать", $"srv_{service.ServiceId}")
        },
        new[]
        {
            InlineKeyboardButton.WithCallbackData("⬅️ Назад", "open_services")
        }
    });

                        await _botClient.EditMessageText(
                            chatId,
                            messageId,
                            text,
                            replyMarkup: keyboard
                        );

                        return Ok();
                    }


                    if (data.StartsWith("srv_"))
                    {
                        if (!_tempOrders.TryGetValue(chatId, out var orderId))
                        {
                            await _botClient.SendMessage(chatId, "⚠️ Начните с выбора даты.");
                            return Ok();
                        }

                        var serviceId = int.Parse(data.Replace("srv_", ""));

                        var os = await _db.OrderServices.FirstOrDefaultAsync(x => x.OrderId == orderId);

                        if (os == null)
                        {
                            os = new OrderService
                            {
                                OrderId = orderId
                            };

                            _db.OrderServices.Add(os);
                        }

                        // ✅ сохраняем услугу
                        os.ServiceId = serviceId;

                        await _db.SaveChangesAsync();

                        await SendInventory(chatId);

                        return Ok();
                    }






                    if (data.StartsWith("inv_"))
                    {
                        if (!_authorizedUsers.TryGetValue(chatId, out var clientId))
                        {
                            await _botClient.SendMessage(chatId, "⚠️ Сначала авторизуйтесь.");
                            return Ok();
                        }

                        var inventoryId = int.Parse(data.Replace("inv_", ""));

                        var items = await _db.InventoryItems
                            .Where(x => x.InventoryId == inventoryId)
                            .ToListAsync();

                        if (!items.Any())
                        {
                            await _botClient.SendMessage(chatId, "❗️ Нет доступных размеров.");
                            return Ok();
                        }

                        var buttons = items
                            .Select(x => new[]
                            {
            InlineKeyboardButton.WithCallbackData(
                $"Размер: {x.Size}",
                $"item_{x.InventoryItemId}"
            )
                            })
                            .ToList();

                        buttons.Add(new[]
                        {
        InlineKeyboardButton.WithCallbackData("⬅️ Назад", "back_to_inventory")
    });

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
                        if (!_authorizedUsers.ContainsKey(chatId))
                        {
                            await _botClient.SendMessage(chatId, "Сначала авторизуйтесь.");
                            return Ok();
                        }

                        var itemId = int.Parse(data.Replace("item_", ""));

                        var item = await _db.InventoryItems
                            .Include(x => x.Inventory)
                            .FirstOrDefaultAsync(x => x.InventoryItemId == itemId);

                        if (item == null)
                            return Ok();

                        var keyboard = new InlineKeyboardMarkup(new[] // keyboard определяется здесь
                        {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(
                                "✅ Добавить в корзину",
                                $"rent_item_{item.InventoryItemId}"
                            )
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"inv_{item.InventoryId}")
                        }
                    });

                        await _botClient.EditMessageText(
                            chatId,
                            messageId,
                            $"📦 {item.Inventory.InventoryName}\nРазмер: {item.Size}\nЦена: {item.Inventory.RentalCostPerHour} ₽ / час",
                            replyMarkup: keyboard
                        );

                        return Ok();
                    }



                    if (data.StartsWith("rent_item_"))
                    {
                        if (!_authorizedUsers.TryGetValue(chatId, out var clientId))
                        {
                            await _botClient.SendMessage(chatId, "Сначала авторизуйтесь.");
                            return Ok();
                        }

                        if (!_tempOrders.TryGetValue(chatId, out var orderId))
                        {
                            await _botClient.SendMessage(chatId, "❗️ Сначала выберите дату и время.");
                            return Ok();
                        }

                        var itemId = int.Parse(data.Replace("rent_item_", ""));

                        var item = await _db.InventoryItems
                            .Include(x => x.Inventory)
                            .FirstOrDefaultAsync(x => x.InventoryItemId == itemId);

                        if (item == null)
                            return Ok();

                        // ✅ берём ПОСЛЕДНИЙ OrderService (а не случайный)
                        var orderService = await _db.OrderServices
                            .OrderByDescending(x => x.OrderServiceId)
                            .FirstOrDefaultAsync(x => x.OrderId == orderId);

                        // ✅ проверка, что время выбрано
                        if (orderService == null || orderService.RentTime <= 0)
                        {
                            await _botClient.SendMessage(chatId, "❗️ Сначала выберите время.");
                            return Ok();
                        }

                        // ✅ защита от дублей
                        var exists = await _db.OrderInventories.AnyAsync(x =>
                            x.OrderServiceId == orderService.OrderServiceId &&
                            x.InventoryItemId == item.InventoryItemId);

                        if (exists)
                        {
                            await _botClient.SendMessage(chatId, "⚠️ Этот предмет уже добавлен.");
                            return Ok();
                        }

                        var orderInventory = new OrderInventory
                        {
                            InventoryItemId = item.InventoryItemId,
                            OrderServiceId = orderService.OrderServiceId,
                            RentTime = orderService.RentTime
                        };

                        _db.OrderInventories.Add(orderInventory);
                        await _db.SaveChangesAsync();

                        var keyboard = new InlineKeyboardMarkup(new[]
                        {
        new[]
        {
            InlineKeyboardButton.WithCallbackData("➕ Добавить ещё", "back_to_inventory")
        },
        new[]
        {
            InlineKeyboardButton.WithCallbackData("💰 Оформить заказ", "checkout")
        }
    });

                        await _botClient.SendMessage(
                            chatId,
                            $"✅ {item.Inventory.InventoryName} добавлен в корзину.",
                            replyMarkup: keyboard
                        );

                        return Ok();
                    }



                    if (data == "checkout")
                    {
                        if (!_authorizedUsers.TryGetValue(chatId, out var clientId))
                        {
                            await _botClient.SendMessage(chatId, "❗️ Сначала авторизуйтесь.");
                            return Ok();
                        }

                        if (!_tempOrders.TryGetValue(chatId, out var orderId))
                        {
                            await _botClient.SendMessage(chatId, "❗️ Начните с выбора даты.");
                            return Ok();
                        }

                        var orderServices = await _db.OrderServices
                            .Where(x => x.OrderId == orderId)
                            .Include(x => x.Service)
                            .Include(x => x.OrderInventories)
                                .ThenInclude(oi => oi.InventoryItem)
                                    .ThenInclude(ii => ii.Inventory)
                            .ToListAsync();

                        if (!orderServices.Any())
                        {
                            await _botClient.SendMessage(chatId, "❗️ Корзина пустая.");
                            return Ok();
                        }

                        int total = 0;

                        foreach (var os in orderServices)
                        {
                            if (os.Service == null)
                            {
                                await _botClient.SendMessage(chatId, "❗️ Сначала выберите услугу.");
                                return Ok();
                            }

                            if (os.RentTime == null || os.RentTime <= 0)
                            {
                                await _botClient.SendMessage(chatId, "❗️ Некорректное время аренды.");
                                return Ok();
                            }

                            int rentTime = os.RentTime.Value;
                            int servicePrice = os.Service.CostPerHour ?? 0;

                            total += servicePrice * rentTime;

                            foreach (var inv in os.OrderInventories)
                            {
                                int invPrice = inv.InventoryItem?.Inventory?.RentalCostPerHour ?? 0;
                                total += invPrice * rentTime;
                            }
                        }

                        var order = await _db.Orders.FindAsync(orderId);

                        if (order == null)
                        {
                            await _botClient.SendMessage(chatId, "❗️ Ошибка заказа.");
                            return Ok();
                        }

                        order.TotalPrice = total;

                        await _db.SaveChangesAsync();

                        await _botClient.SendMessage(
                            chatId,
                            $"✅ Заказ оформлен!\n💰 Итог: {total} ₽"
                        );

                        _tempOrders.Remove(chatId);

                        return Ok();
                    }

                }
            }




            catch (Exception ex)
            {
                // Вам следует логировать исключение более подробно, например, с помощью _logger
                Console.WriteLine($"Error: {ex.Message}"); // Вместо Console.WriteLine лучше использовать _logger.LogError
                // _logger.LogError(ex, "An error occurred while processing the update."); // Пример логгирования
                return Ok(); // Возможно, стоит вернуть BadRequest или другой статус ошибки
            }
            return Ok(); // Этот return Ok() в конце метода можно убрать, если все обработано выше.
        }
    }
}
