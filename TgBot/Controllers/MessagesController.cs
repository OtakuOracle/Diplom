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
                "🏫 Выберите услугу:",
                replyMarkup: keyboard
            );
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
                        InlineKeyboardButton.WithCallbackData("🏫 Услуги","open_services")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("🎿 Инвентарь","open_inventory")
                    },
                   
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
                        InlineKeyboardButton.WithCallbackData("🏫 Услуги", "open_services")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("🎿 Инвентарь", "open_inventory")
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
                            "🏫 Выберите услугу:",
                            replyMarkup: keyboard
                        );

                        return Ok();
                    }


                    if (data.StartsWith("srv_"))
                    {
                        var serviceId = int.Parse(data.Replace("srv_", ""));
                        var service = await _db.Services.FindAsync(serviceId);

                        if (service == null)
                        {
                            await _botClient.SendMessage(chatId, "Услуга не найдена.");
                            return Ok();
                        }

                        var icon = GetIcon(service.ServiceName);

                        var message =
                            $"{icon} {service.ServiceName}\n\n" +
                            $"💰 Цена: {service.CostPerHour} ₽ / час";

                        var keyboard = new InlineKeyboardMarkup(new[]
                        {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("✅ Забронировать", $"book_srv_{serviceId}")
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("⬅️ Назад", "open_services")
                        }
                    });

                        await _botClient.EditMessageText(chatId, messageId, message, replyMarkup: keyboard);

                        return Ok();
                    }
                    if (data.StartsWith("book_srv_"))
                    {
                        var serviceId = int.Parse(data.Replace("book_srv_", ""));

                        _tempService[chatId] = serviceId;

                        var dates = Enumerable.Range(0, 7)
                            .Select(i => DateTime.Now.Date.AddDays(i))
                            .ToList();

                        var buttons = dates
                            .Select(d => InlineKeyboardButton.WithCallbackData(
                                d.ToString("dd.MM"),
                                $"date_{d:yyyy-MM-dd}"
                            ))
                            .Select(x => new[] { x })
                            .ToList();

                        buttons.Add(new[]
                        {
        InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"srv_{serviceId}")
    });

                        await _botClient.EditMessageText(
                            chatId,
                            messageId,
                            "📅 Выберите дату:",
                            replyMarkup: new InlineKeyboardMarkup(buttons)
                        );

                        return Ok();
                    }
                    if (data.StartsWith("date_"))
                    {
                        // Безопасный парсинг даты формата yyyy-MM-dd
                        var date = DateOnly.ParseExact(data.Replace("date_", ""), "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                        _tempDate[chatId] = date;

                        var times = Enumerable.Range(9, 12); // 09:00 - 20:00

                        var buttons = times
                            .Select(h => InlineKeyboardButton.WithCallbackData(
                                $"{h}:00",
                                $"timein_{h}" // Передаем час начала
                            ))
                            .Select(x => new[] { x })
                            .ToList();

                        await _botClient.EditMessageText(
                            chatId,
                            messageId,
                            "⏰ Выберите время начала:",
                            replyMarkup: new InlineKeyboardMarkup(buttons)
                        );

                        return Ok();
                    }


                    if (data.StartsWith("timein_"))
                    {
                        var hour = int.Parse(data.Replace("timein_", ""));
                        _tempTimeIn[chatId] = new TimeOnly(hour, 0);

                        var times = Enumerable.Range(hour + 1, 20 - hour);

                        var buttons = times
                            .Select(h => InlineKeyboardButton.WithCallbackData(
                                $"{h}:00",
                                $"timeout_{h}"
                            ))
                            .Select(x => new[] { x })
                            .ToList();

                        await _botClient.EditMessageText(
                            chatId,
                            messageId,
                            "⏰ Выберите время окончания:",
                            replyMarkup: new InlineKeyboardMarkup(buttons)
                        );

                        return Ok();
                    }

                    if (data.StartsWith("timeout_"))
                    {
                        if (!_authorizedUsers.ContainsKey(chatId))
                        {
                            await _botClient.SendMessage(chatId, "Сначала авторизуйтесь.");
                            return Ok();
                        }

                        var hour = int.Parse(data.Replace("timeout_", ""));
                        var timeOut = new TimeOnly(hour, 0);

                        var timeIn = _tempTimeIn[chatId];
                        var date = _tempDate[chatId];
                        var serviceId = _tempService[chatId];
                        var clientId = _authorizedUsers[chatId];

                        var rentHours = timeOut.Hour - timeIn.Hour;

                        var order = new Order
                        {
                            OrderCode = Random.Shared.Next(100, 999).ToString(),
                            ClientId = clientId,
                            DateCreate = DateOnly.FromDateTime(DateTime.Now),
                            TimeCreate = TimeOnly.FromDateTime(DateTime.Now),
                            TotalPrice = 0 // Посчитаем в самом конце в checkout_
                        };

                        _db.Orders.Add(order);
                        await _db.SaveChangesAsync();

                        var orderService = new OrderService
                        {
                            OrderId = order.OrderId,
                            ServiceId = serviceId,
                            RentTime = rentHours,
                            OrderStatusId = 1,
                            Date = date,
                            TimeIn = timeIn,
                            TimeOut = timeOut
                        };

                        _db.OrderServices.Add(orderService);
                        await _db.SaveChangesAsync();

                        // Очищаем временные данные, они нам больше не нужны
                        _tempService.Remove(chatId);
                        _tempDate.Remove(chatId);
                        _tempTimeIn.Remove(chatId);

                        // --- ИНТЕГРАЦИЯ С КОРЗИНОЙ: ВМЕСТО ПРОСТОГО СООБЩЕНИЯ ДЕЛАЕМ КНОПКИ ---

                        var keyboard = new InlineKeyboardMarkup(new[]
                        {
                     new[]
                     { 
                         // Кнопка ведет на выбор инвентаря для ЭТОЙ конкретной услуги
                         InlineKeyboardButton.WithCallbackData("🎿 Добавить инвентарь", $"add_inv_to_{orderService.OrderServiceId}")
                     },
                     new[]
                     { 
                         // Кнопка ведет на оформление заказа, если инвентарь не нужен
                         InlineKeyboardButton.WithCallbackData("✅ Оформить без инвентаря", $"checkout_{order.OrderId}")
                     }
                 });

                        // Редактируем сообщение с выбором времени, чтобы не спамить новыми сообщениями
                        await _botClient.EditMessageText(
                            chatId,
                            messageId,
                            $"Услуга выбрана!\n📅 Дата: {date:dd.MM}\n⏰ Время: {timeIn:HH:mm} - {timeOut:HH:mm} ({rentHours} ч.)",
                            replyMarkup: keyboard
                        );

                        return Ok();
                    }




                    if (data == "open_inventory")
                    {
                        var items = await _db.Inventories.ToListAsync();

                        var buttons = items
                            .Select(x => InlineKeyboardButton.WithCallbackData(
                                x.InventoryName,
                                // Передаем "inv_{id}_none", чтобы показать, что это просто просмотр без заказа
                                $"inv_{x.InventoryId}_none"
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
                            "🎿 Выберите инвентарь:",
                            replyMarkup: keyboard
                        );

                        return Ok();
                    }

                    if (data.StartsWith("inv_"))
                    {
                        // Формат: inv_{invId}_{orderServiceId} (или inv_{invId}_none)
                        var parts = data.Split('_');
                        var invId = int.Parse(parts[1]);
                        var osIdStr = parts[2]; // Это может быть ID услуги или строка "none"

                        var inventory = await _db.Inventories.FindAsync(invId);
                        if (inventory == null)
                        {
                            await _botClient.SendMessage(chatId, "Инвентарь не найден.");
                            return Ok();
                        }

                        // Собираем красивое описание инвентаря
                        var message =
                            $"🎿 **{inventory.InventoryName}**\n" +
                            $"🏷️ Модель: {inventory.InventoryModel ?? "Не указана"}\n" +
                            $"💰 Цена: {inventory.RentalCostPerHour ?? 0} ₽ / час";

                        var buttons = new List<InlineKeyboardButton[]>();

                        // Если мы зашли сюда из корзины (есть ID услуги)
                        if (osIdStr != "none")
                        {
                            buttons.Add(new[]
                            { 
            // Кнопка ведет на следующий шаг — выбор размера
            InlineKeyboardButton.WithCallbackData("📏 Выбрать размер", $"selectsize_{invId}_{osIdStr}")
        });
                            buttons.Add(new[]
                            {
            InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"add_inv_to_{osIdStr}")
        });
                        }
                        else // Если просто просматриваем из главного меню
                        {
                            buttons.Add(new[]
                            {
            InlineKeyboardButton.WithCallbackData("⬅️ Назад", "open_inventory")
        });
                        }

                        await _botClient.EditMessageText(chatId, messageId, message, replyMarkup: new InlineKeyboardMarkup(buttons));
                        return Ok();
                    }


                    if (data.StartsWith("selectsize_"))
                    {
                        // Формат: selectsize_{invId}_{orderServiceId}
                        var parts = data.Split('_');
                        var invId = int.Parse(parts[1]);
                        var osId = int.Parse(parts[2]);

                        var sizes = await _db.InventoryItems
                            .Where(x => x.InventoryId == invId && x.InventoryStatusId == 1)
                            .Select(x => x.Size).Distinct().ToListAsync();

                        if (sizes.Count == 0)
                        {
                            await _botClient.SendMessage(chatId, "К сожалению, этого инвентаря сейчас нет в наличии свободных размеров.");
                            return Ok();
                        }

                        // Генерируем кнопки размеров
                        var buttons = sizes.Select(s =>
                            InlineKeyboardButton.WithCallbackData(s, $"size_{invId}_{s}_{osId}")
                        ).Chunk(3).Select(x => x.ToArray()).ToList();

                        // Кнопка Назад вернет на экран информации об инвентаре
                        buttons.Add(new[]
                        {
                            InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"inv_{invId}_{osId}")
                        });

                        await _botClient.EditMessageText(chatId, messageId, "📐 Выберите подходящий размер:",
                            replyMarkup: new InlineKeyboardMarkup(buttons));
                        return Ok();
                    }

                    if (data.StartsWith("size_"))
                    {
                        // Формат: size_{invId}_{size}_{osId}
                        var parts = data.Split('_');
                        var invId = int.Parse(parts[1]);
                        var size = parts[2];
                        var osId = int.Parse(parts[3]);

                        // 1. НАХОДИМ УСЛУГУ, чтобы узнать её время бронирования
                        var orderService = await _db.OrderServices.FindAsync(osId);
                        if (orderService == null)
                        {
                            await _botClient.SendMessage(chatId, "Произошла ошибка: услуга не найдена.");
                            return Ok();
                        }

                        // Берем время из услуги (если там null, по умолчанию берем 1 час)
                        int serviceRentTime = orderService.RentTime ?? 1;

                        // 2. Находим свободный предмет нужного размера
                        var item = await _db.InventoryItems
                            .FirstOrDefaultAsync(x => x.InventoryId == invId && x.Size == size && x.InventoryStatusId == 1);

                        if (item != null)
                        {
                            var orderInv = new OrderInventory
                            {
                                InventoryItemId = item.InventoryItemId,
                                OrderServiceId = osId,
                                RentTime = serviceRentTime // <-- ПРИСВАИВАЕМ ВРЕМЯ ИЗ УСЛУГИ!
                            };
                            _db.OrderInventories.Add(orderInv);

                            item.InventoryStatusId = 2; // Меняем статус предмета на "Занят"
                            await _db.SaveChangesAsync();
                        }

                        // 3. Предлагаем пользователю выбор дальнейших действий
                        var keyboard = new InlineKeyboardMarkup(new[] {
        new[] { InlineKeyboardButton.WithCallbackData("➕ Еще инвентарь", $"add_inv_to_{osId}") },
        new[] { InlineKeyboardButton.WithCallbackData("🏁 Завершить", $"checkout_{orderService.OrderId}") }
    });

                        await _botClient.SendMessage(chatId, $"Добавлено! Инвентарь забронирован на {serviceRentTime} ч.", replyMarkup: keyboard);
                        return Ok();
                    }


                    if (data.StartsWith("add_inv_to_"))
                    {
                        var orderServiceId = int.Parse(data.Replace("add_inv_to_", ""));
                        var inventories = await _db.Inventories.ToListAsync();

                        var buttons = inventories.Select(i =>
                            // Передаем inv_{inventoryId}_{orderServiceId}
                            new[] { InlineKeyboardButton.WithCallbackData(i.InventoryName, $"inv_{i.InventoryId}_{orderServiceId}") }
                        ).ToList();

                        await _botClient.EditMessageText(chatId, messageId, "Выберите категорию инвентаря:",
                            replyMarkup: new InlineKeyboardMarkup(buttons));
                        return Ok();
                    }
                    if (data.StartsWith("checkout_"))
                    {
                        var orderId = int.Parse(data.Replace("checkout_", ""));
                        var order = await _db.Orders
                            .Include(o => o.OrderServices).ThenInclude(os => os.Service)
                            .Include(o => o.OrderServices).ThenInclude(os => os.OrderInventories)
                                .ThenInclude(oi => oi.InventoryItem).ThenInclude(ii => ii.Inventory)
                            .FirstOrDefaultAsync(o => o.OrderId == orderId);

                        if (order == null)
                        {
                            await _botClient.SendMessage(chatId, "Заказ не найден.");
                            return Ok();
                        }

                        int total = 0;
                        string details = "🛒 Ваш заказ:\n";

                        foreach (var os in order.OrderServices)
                        {
                            // Записываем время аренды услуги в переменную (если null, то 1 час)
                            int sRentTime = os.RentTime ?? 1;
                            int sPrice = (os.Service?.CostPerHour ?? 0) * sRentTime;
                            total += sPrice;

                            // Добавлено время аренды в скобках: (X ч.)
                            details += $"\n🔹 Услуга: {os.Service?.ServiceName} — {sPrice}₽ ({sRentTime} ч.)";

                            foreach (var oi in os.OrderInventories)
                            {
                                // Записываем время аренды инвентаря в переменную (если null, то 1 час)
                                int iRentTime = oi.RentTime ?? 1;
                                int iPrice = (oi.InventoryItem?.Inventory?.RentalCostPerHour ?? 0) * iRentTime;
                                total += iPrice;

                                // Добавлено время аренды в скобках: (X ч.)
                                details += $"\n🔸 Инвентарь: {oi.InventoryItem?.Inventory?.InventoryName} ({oi.InventoryItem?.Size}) — {iPrice}₽ ({iRentTime} ч.)";
                            }
                        }

                        order.TotalPrice = total;
                        await _db.SaveChangesAsync();

                        await _botClient.SendMessage(chatId, $"{details}\n\n💰 **Итого: {total} ₽**");
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