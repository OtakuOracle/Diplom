using Avalonia.Controls;
using Elbrus.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

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
        private static Dictionary<long, int> _activeOrderId = new(); // Хранит ID открытой корзины для каждого пользователя
        private static Dictionary<long, int> _tempInventory = new(); // Для хранения ID инвентаря при прямой аренде





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

                        int orderId;

                        // ПРОВЕРЯЕМ: Есть ли уже открытая корзина (заказ) у этого пользователя?
                        if (_activeOrderId.TryGetValue(chatId, out var existingOrderId))
                        {
                            orderId = existingOrderId; // Используем существующий заказ
                        }
                        else
                        {
                            // Если корзины нет — создаем новый заказ с рандомным кодом
                            var order = new Order
                            {
                                OrderCode = Random.Shared.Next(100, 1000).ToString(),
                                ClientId = clientId,
                                DateCreate = DateOnly.FromDateTime(DateTime.Now),
                                TimeCreate = TimeOnly.FromDateTime(DateTime.Now),
                                TotalPrice = 0
                            };

                            _db.Orders.Add(order);
                            await _db.SaveChangesAsync();

                            orderId = order.OrderId;
                            _activeOrderId[chatId] = orderId; // Запоминаем этот заказ как активную корзину
                        }

                        // Создаем запись о новой услуге и привязываем её к найденному orderId
                        var orderService = new OrderService
                        {
                            OrderId = orderId,
                            ServiceId = serviceId,
                            RentTime = rentHours,
                            OrderStatusId = 1,
                            Date = date,
                            TimeIn = timeIn,
                            TimeOut = timeOut
                        };

                        _db.OrderServices.Add(orderService);
                        await _db.SaveChangesAsync();

                        // Очищаем временные данные бронирования текущей услуги
                        _tempService.Remove(chatId);
                        _tempDate.Remove(chatId);
                        _tempTimeIn.Remove(chatId);

                        // Новые кнопки управления корзиной
                        var keyboard = new InlineKeyboardMarkup(new[]
                        {
        new[] { InlineKeyboardButton.WithCallbackData("🎿 Добавить инвентарь", $"add_inv_to_{orderService.OrderServiceId}") },
        new[] { InlineKeyboardButton.WithCallbackData("➕ Добавить еще услугу", "open_services") }, // Ведет на список услуг
        new[] { InlineKeyboardButton.WithCallbackData("🏁 Оформить весь заказ", $"checkout_{orderId}") }
    });

                        await _botClient.EditMessageText(
                            chatId,
                            messageId,
                            $"Услуга успешно добавлена в корзину!\n📅 Дата: {date:dd.MM}\n⏰ Время: {timeIn:HH:mm} - {timeOut:HH:mm} ({rentHours} ч.)\n\nЧто вы хотите сделать дальше?",
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
                        var parts = data.Split('_');
                        var invId = int.Parse(parts[1]);
                        var osIdStr = parts[2];

                        var inventory = await _db.Inventories.FindAsync(invId);
                        if (inventory == null)
                        {
                            await _botClient.SendMessage(chatId, "Инвентарь не найден.");
                            return Ok();
                        }

                        var message =
                            $"🎿 **{inventory.InventoryName}**\n" +
                            $"🏷️ Модель: {inventory.InventoryModel ?? "Не указана"}\n" +
                            $"💰 Цена: {inventory.RentalCostPerHour ?? 0} ₽ / час";

                        var buttons = new List<InlineKeyboardButton[]>();

                        if (osIdStr != "none") // Если перешли из оформленной услуги
                        {
                            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("📏 Выбрать размер", $"selectsize_{invId}_{osIdStr}") });
                            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"add_inv_to_{osIdStr}") });
                        }
                        else // ЕСЛИ ПРОСТО ИЗ МЕНЮ (Прямая аренда)
                        {
                            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("📅 Забронировать этот инвентарь", $"book_inv_{invId}") });
                            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("⬅️ Назад", "open_inventory") });
                        }

                        await _botClient.EditMessageText(chatId, messageId, message, replyMarkup: new InlineKeyboardMarkup(buttons));
                        return Ok();
                    }



                    // 1. Старт бронирования инвентаря (Выбор даты)
                    if (data.StartsWith("book_inv_"))
                    {
                        var invId = int.Parse(data.Replace("book_inv_", ""));
                        _tempInventory[chatId] = invId; // Запоминаем, что арендуем

                        var dates = Enumerable.Range(0, 7).Select(i => DateTime.Now.Date.AddDays(i)).ToList();
                        var buttons = dates.Select(d => InlineKeyboardButton.WithCallbackData(
                            d.ToString("dd.MM"),
                            $"dateinv_{d:yyyy-MM-dd}" // Префикс dateinv_
                        )).Select(x => new[] { x }).ToList();

                        buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"inv_{invId}_none") });

                        await _botClient.EditMessageText(chatId, messageId, "📅 Выберите дату начала аренды:", replyMarkup: new InlineKeyboardMarkup(buttons));
                        return Ok();
                    }

                    // 2. Выбор времени начала
                    if (data.StartsWith("dateinv_"))
                    {
                        var date = DateOnly.ParseExact(data.Replace("dateinv_", ""), "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                        _tempDate[chatId] = date;

                        var times = Enumerable.Range(9, 12);
                        var buttons = times.Select(h => InlineKeyboardButton.WithCallbackData($"{h}:00", $"timeininv_{h}")).Select(x => new[] { x }).ToList();

                        await _botClient.EditMessageText(chatId, messageId, "⏰ Выберите время начала аренды:", replyMarkup: new InlineKeyboardMarkup(buttons));
                        return Ok();
                    }

                    // 3. Выбор времени окончания
                    if (data.StartsWith("timeininv_"))
                    {
                        var hour = int.Parse(data.Replace("timeininv_", ""));
                        _tempTimeIn[chatId] = new TimeOnly(hour, 0);

                        var times = Enumerable.Range(hour + 1, 20 - hour);
                        var buttons = times.Select(h => InlineKeyboardButton.WithCallbackData($"{h}:00", $"timeoutinv_{h}")).Select(x => new[] { x }).ToList();

                        await _botClient.EditMessageText(chatId, messageId, "⏰ Выберите время окончания аренды:", replyMarkup: new InlineKeyboardMarkup(buttons));
                        return Ok();
                    }

                    // 4. Создание пустого заказа (без услуги) и переход к выбору размера
                    if (data.StartsWith("timeoutinv_"))
                    {
                        if (!_authorizedUsers.ContainsKey(chatId))
                        {
                            await _botClient.SendMessage(chatId, "Сначала авторизуйтесь.");
                            return Ok();
                        }

                        var hour = int.Parse(data.Replace("timeoutinv_", ""));
                        var timeOut = new TimeOnly(hour, 0);

                        var timeIn = _tempTimeIn[chatId];
                        var date = _tempDate[chatId];
                        var invId = _tempInventory[chatId];
                        var clientId = _authorizedUsers[chatId];
                        var rentHours = timeOut.Hour - timeIn.Hour;

                        int orderId;
                        if (_activeOrderId.TryGetValue(chatId, out var existingOrderId))
                        {
                            orderId = existingOrderId;
                        }
                        else
                        {
                            var order = new Order
                            {
                                OrderCode = Random.Shared.Next(100, 1000).ToString(),
                                ClientId = clientId,
                                DateCreate = DateOnly.FromDateTime(DateTime.Now),
                                TimeCreate = TimeOnly.FromDateTime(DateTime.Now),
                                TotalPrice = 0
                            };
                            _db.Orders.Add(order);
                            await _db.SaveChangesAsync();
                            orderId = order.OrderId;
                            _activeOrderId[chatId] = orderId;
                        }

                        // Создаем запись в OrderService, но ServiceId = null!
                        var orderService = new OrderService
                        {
                            OrderId = orderId,
                            ServiceId = null, // УСЛУГИ НЕТ, ТОЛЬКО ИНВЕНТАРЬ
                            RentTime = rentHours,
                            OrderStatusId = 1,
                            Date = date,
                            TimeIn = timeIn,
                            TimeOut = timeOut
                        };
                        _db.OrderServices.Add(orderService);
                        await _db.SaveChangesAsync();

                        // Чистим временные данные
                        _tempInventory.Remove(chatId);
                        _tempDate.Remove(chatId);
                        _tempTimeIn.Remove(chatId);

                        // Сразу перенаправляем на выбор размера для выбранного инвентаря
                        var keyboard = new InlineKeyboardMarkup(new[]
                        {
        new[] { InlineKeyboardButton.WithCallbackData("📏 Перейти к выбору размера", $"selectsize_{invId}_{orderService.OrderServiceId}") }
    });

                        await _botClient.EditMessageText(chatId, messageId, $"Время подтверждено: {rentHours} ч.\nТеперь выберите размер:", replyMarkup: keyboard);
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
                        var parts = data.Split('_');
                        var invId = int.Parse(parts[1]);
                        var size = parts[2];
                        var osId = int.Parse(parts[3]);

                        // 1. Находим запись бронирования
                        var orderService = await _db.OrderServices
                            .Include(os => os.Service)
                            .FirstOrDefaultAsync(os => os.OrderServiceId == osId);

                        if (orderService == null)
                        {
                            await _botClient.SendMessage(chatId, "Произошла ошибка: запись бронирования не найдена.");
                            return Ok();
                        }

                        int serviceRentTime = orderService.RentTime ?? 1;

                        // 2. Ищем свободный инвентарь нужного размера
                        var item = await _db.InventoryItems
                            .Include(x => x.Inventory)
                            .FirstOrDefaultAsync(x => x.InventoryId == invId && x.Size == size && x.InventoryStatusId == 1);

                        if (item == null)
                        {
                            await _botClient.SendMessage(chatId, "Извините, данный размер уже забронирован другим пользователем.");
                            return Ok();
                        }

                        // 3. Создаем запись аренды инвентаря
                        var orderInv = new OrderInventory
                        {
                            InventoryItemId = item.InventoryItemId,
                            OrderServiceId = osId,
                            RentTime = serviceRentTime // Время совпадает со временем аренды
                        };
                        _db.OrderInventories.Add(orderInv);

                        item.InventoryStatusId = 2; // Меняем статус предмета на "Занят"
                        await _db.SaveChangesAsync();

                        // 4. Считаем промежуточное ИТОГО для вывода пользователю
                        var order = await _db.Orders
                            .Include(o => o.OrderServices).ThenInclude(os => os.Service)
                            .Include(o => o.OrderServices).ThenInclude(os => os.OrderInventories)
                                .ThenInclude(oi => oi.InventoryItem).ThenInclude(ii => ii.Inventory)
                                 .FirstOrDefaultAsync(o => o.OrderId == orderService.OrderId);

                        int total = 0;
                        string details = "🛒 **Ваша корзина на данный момент:**\n";

                        foreach (var os in order.OrderServices)
                        {
                            // Выводим услугу только если она выбрана
                            if (os.Service != null)
                            {
                                int sPrice = (os.Service.CostPerHour ?? 0) * (os.RentTime ?? 1);
                                total += sPrice;
                                details += $"\n🔹 Услуга: {os.Service.ServiceName} — {sPrice}₽ ({os.RentTime} ч.)";
                            }

                            // Выводим весь инвентарь
                            foreach (var oi in os.OrderInventories)
                            {
                                int iPrice = (oi.InventoryItem?.Inventory?.RentalCostPerHour ?? 0) * (oi.RentTime ?? 1);
                                total += iPrice;
                                details += $"\n🔸 Инвентарь: {oi.InventoryItem?.Inventory?.InventoryName} ({oi.InventoryItem?.Size}) — {iPrice}₽ ({oi.RentTime} ч.)";
                            }
                        }

                        // Сохраняем промежуточную сумму в базу данных
                        order.TotalPrice = total;
                        await _db.SaveChangesAsync();

                        var keyboard = new InlineKeyboardMarkup(new[]
                        {
                            new[] { InlineKeyboardButton.WithCallbackData("➕ Добавить еще инвентарь", $"add_inv_to_{osId}") },
                            // !!! ИЗМЕНЕНО: Передаем osId в выбор услуг, чтобы наследовать время !!!
                            new[] { InlineKeyboardButton.WithCallbackData("➕ Добавить еще услугу", $"open_services_for_{osId}") },
                            new[] { InlineKeyboardButton.WithCallbackData("🏁 Завершить оформление", $"checkout_{orderService.OrderId}") }
                        });

                        // Отправляем промежуточный чек сразу после нажатия на размер
                        await _botClient.EditMessageText(
                            chatId,
                            messageId,
                            $"{details}\n\n💰 **Промежуточное итого: {total} ₽**",
                            replyMarkup: keyboard
                        );

                        return Ok();
                    }



                    if (data.StartsWith("open_services_for_"))
                    {
                        var osId = int.Parse(data.Replace("open_services_for_", ""));
                        var services = await _db.Services.ToListAsync();

                        var buttons = services
                            .Select(x => InlineKeyboardButton.WithCallbackData(
                                x.ServiceName,
                                $"srvadd_{x.ServiceId}_{osId}" // Уникальный префикс srvadd_ во избежание конфликтов
                            ))
                            .Select(x => new[] { x })
                            .ToList();

                        buttons.Add(new[]
                        {
                            InlineKeyboardButton.WithCallbackData("⬅️ Назад к корзине", $"checkout_{_activeOrderId[chatId]}")
                        });

                        await _botClient.EditMessageText(
                            chatId,
                            messageId,
                            "🎿 **Выберите услугу, которую хотите добавить на это же время:**",
                            replyMarkup: new InlineKeyboardMarkup(buttons)
                        );

                        return Ok();
                    }

                    // 2. Карточка услуги с кнопкой моментального добавления
                    if (data.StartsWith("srvadd_"))
                    {
                        // Формат: srvadd_{serviceId}_{osId}
                        var parts = data.Split('_');
                        var serviceId = int.Parse(parts[1]);
                        var osId = int.Parse(parts[2]);

                        var service = await _db.Services.FindAsync(serviceId);
                        if (service == null)
                        {
                            await _botClient.SendMessage(chatId, "Услуга не найдена.");
                            return Ok();
                        }

                        var icon = GetIcon(service.ServiceName);
                        var message =
                            $"{icon} {service.ServiceName}\n\n" +
                            $"💰 Цена: {service.CostPerHour} ₽ / час\n\n" +
                            $"⚠️ *Услуга будет забронирована на то же время, что и ваш инвентарь.*";

                        var keyboard = new InlineKeyboardMarkup(new[]
                        {
                            new[] { InlineKeyboardButton.WithCallbackData("✅ Добавить в этот заказ", $"attach_srv_{serviceId}_{osId}") },
                            new[] { InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"open_services_for_{osId}") }
                        });

                        await _botClient.EditMessageText(chatId, messageId, message, replyMarkup: keyboard);
                        return Ok();
                    }

                    // 3. Привязка услуги к времени инвентаря и вывод обновленной корзины
                    if (data.StartsWith("attach_srv_"))
                    {
                        // Формат: attach_srv_{serviceId}_{osId}
                        var parts = data.Replace("attach_srv_", "").Split('_');
                        var serviceId = int.Parse(parts[0]);
                        var osId = int.Parse(parts[1]);

                        var orderService = await _db.OrderServices.FindAsync(osId);
                        if (orderService == null)
                        {
                            await _botClient.SendMessage(chatId, "Запись времени не найдена.");
                            return Ok();
                        }

                        // Если у этой записи еще нет услуги (т.е. мы шли по пути "сначала инвентарь")
                        if (orderService.ServiceId == null)
                        {
                            orderService.ServiceId = serviceId; // Просто привязываем услугу к этому времени
                        }
                        else
                        {
                            // Если вдруг услуга уже была, создаем вторую услугу на то же время
                            var newOrderService = new OrderService
                            {
                                OrderId = orderService.OrderId,
                                ServiceId = serviceId,
                                Date = orderService.Date,
                                TimeIn = orderService.TimeIn,
                                TimeOut = orderService.TimeOut,
                                RentTime = orderService.RentTime,
                                OrderStatusId = 1
                            };
                            _db.OrderServices.Add(newOrderService);
                            osId = newOrderService.OrderServiceId; // Переключаем контекст на новую услугу
                        }

                        await _db.SaveChangesAsync();

                        // Пересчитываем корзину и выводим её пользователю
                        var order = await _db.Orders
                            .Include(o => o.OrderServices).ThenInclude(os => os.Service)
                            .Include(o => o.OrderServices).ThenInclude(os => os.OrderInventories)
                                .ThenInclude(oi => oi.InventoryItem).ThenInclude(ii => ii.Inventory)
                            .FirstOrDefaultAsync(o => o.OrderId == orderService.OrderId);

                        int total = 0;
                        string details = "🛒 **Ваша корзина обновлена:**\n";

                        foreach (var os in order.OrderServices)
                        {
                            if (os.Service != null)
                            {
                                int sPrice = (os.Service.CostPerHour ?? 0) * (os.RentTime ?? 1);
                                total += sPrice;
                                details += $"\n🔹 Услуга: {os.Service.ServiceName} — {sPrice}₽ ({os.RentTime} ч.)";
                            }

                            foreach (var oi in os.OrderInventories)
                            {
                                int iPrice = (oi.InventoryItem?.Inventory?.RentalCostPerHour ?? 0) * (oi.RentTime ?? 1);
                                total += iPrice;
                                details += $"\n🔸 Инвентарь: {oi.InventoryItem?.Inventory?.InventoryName} ({oi.InventoryItem?.Size}) — {iPrice}₽ ({oi.RentTime} ч.)";
                            }
                        }

                        order.TotalPrice = total;
                        await _db.SaveChangesAsync();

                        var keyboard = new InlineKeyboardMarkup(new[]
                        {
                            new[] { InlineKeyboardButton.WithCallbackData("➕ Добавить еще инвентарь", $"add_inv_to_{osId}") },
                            new[] { InlineKeyboardButton.WithCallbackData("➕ Добавить еще услугу", $"open_services_for_{osId}") },
                            new[] { InlineKeyboardButton.WithCallbackData("🏁 Завершить оформление", $"checkout_{order.OrderId}") }
                        });

                        await _botClient.EditMessageText(
                            chatId,
                            messageId,
                            $"{details}\n\n💰 **Промежуточное итого: {total} ₽**",
                            replyMarkup: keyboard
                        );

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
                        string details = $"🛒 **Ваш заказ №{order.OrderId}**\n";

                        foreach (var os in order.OrderServices)
                        {
                            // Выводим услугу только если она есть (ServiceId != null)
                            if (os.Service != null)
                            {
                                int sRentTime = os.RentTime ?? 1;
                                int sPrice = (os.Service.CostPerHour ?? 0) * sRentTime;
                                total += sPrice;

                                details += $"\n🔹 Услуга: {os.Service.ServiceName} — {sPrice}₽ ({sRentTime} ч.)";
                            }

                            // Выводим добавленный инвентарь
                            foreach (var oi in os.OrderInventories)
                            {
                                int iRentTime = oi.RentTime ?? 1;
                                int iPrice = (oi.InventoryItem?.Inventory?.RentalCostPerHour ?? 0) * iRentTime;
                                total += iPrice;

                                details += $"\n🔸 Инвентарь: {oi.InventoryItem?.Inventory?.InventoryName} ({oi.InventoryItem?.Size}) — {iPrice}₽ ({iRentTime} ч.)";
                            }
                        }

                        // Записываем финальную сумму и сохраняем
                        order.TotalPrice = total;
                        await _db.SaveChangesAsync();

                        await _botClient.SendMessage(chatId, $"{details}\n\n💰 **Итого к оплате: {total} ₽**");

                        // Очищаем корзину для следующего сеанса бронирования
                        _activeOrderId.Remove(chatId);

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