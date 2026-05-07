using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Elbrus.Models;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System.Threading.Tasks;

namespace Elbrus;

public partial class AddEditClient : Window
{
    public Client? ExistingClient { get; set; }

    public AddEditClient()
    {
        InitializeComponent();
        this.Title = "Добавить нового клиента";
        AddOrUpdateButton.Content = "Добавить";
    }

    public AddEditClient(Client client)
    {
        InitializeComponent();
        ExistingClient = client;

        this.Title = "Редактировать клиента";
        AddOrUpdateButton.Content = "Сохранить";

        FioBox.Text = ExistingClient.FullName;
        CodeBox.Text = ExistingClient.ClientCode.ToString();
        PassportBox.Text = ExistingClient.Passport;
        if (ExistingClient.Birthday.HasValue)
        {

            BirthdayPicker.SelectedDate = new DateTime(
                ExistingClient.Birthday.Value.Year,
                ExistingClient.Birthday.Value.Month,
                ExistingClient.Birthday.Value.Day);
        }
        AddressBox.Text = ExistingClient.Address;
        EmailBox.Text = ExistingClient.Email;
        PasswordBox.Text = ExistingClient.Password;
    }

    private async void AddClient_OnClick(object? sender, RoutedEventArgs e)
    {
        using var context = new DiplomContext();

        if (string.IsNullOrWhiteSpace(FioBox.Text) ||
            string.IsNullOrWhiteSpace(CodeBox.Text) ||
            string.IsNullOrWhiteSpace(PassportBox.Text) ||
            BirthdayPicker.SelectedDate == null ||
            string.IsNullOrWhiteSpace(AddressBox.Text) ||
            string.IsNullOrWhiteSpace(EmailBox.Text) ||
            string.IsNullOrWhiteSpace(PasswordBox.Text))
        {
            await MessageBoxManager.GetMessageBoxStandard(
                "Ошибка",
                "Пожалуйста, заполните все поля!",
                ButtonEnum.Ok,
                MsBox.Avalonia.Enums.Icon.Error)
                .ShowAsync();
            return;
        }

        try
        {
            CorrectInput();

            int clientCode = Convert.ToInt32(CodeBox.Text);

            if (ExistingClient == null)
            {
                if (context.Clients.Any(c => c.ClientCode == clientCode))
                {
                    await MessageBoxManager.GetMessageBoxStandard(
                        "Ошибка",
                        "Клиент с таким кодом уже существует!",
                        ButtonEnum.Ok,
                        MsBox.Avalonia.Enums.Icon.Error)
                        .ShowAsync();
                    return;
                }

                var newClient = new Client
                {
                    FullName = FioBox.Text.Trim(),
                    ClientCode = clientCode,
                    Passport = PassportBox.Text.Trim(),
                    Birthday = DateOnly.FromDateTime(BirthdayPicker.SelectedDate.Value.DateTime),
                    Address = AddressBox.Text.Trim(),
                    Email = EmailBox.Text.Trim(),
                    Password = PasswordBox.Text,
                    RoleId = 4
                };

                context.Clients.Add(newClient);
            }
            else
            {
                var clientToUpdate = context.Clients.Find(ExistingClient.ClientId);

                if (clientToUpdate != null)
                {
                    if (clientCode != clientToUpdate.ClientCode && context.Clients.Any(c => c.ClientCode == clientCode))
                    {
                        await MessageBoxManager.GetMessageBoxStandard(
                           "Ошибка",
                           "Клиент с таким кодом уже существует!",
                           ButtonEnum.Ok,
                           MsBox.Avalonia.Enums.Icon.Error)
                           .ShowAsync();
                        return;
                    }

                    clientToUpdate.FullName = FioBox.Text.Trim();
                    clientToUpdate.ClientCode = clientCode;
                    clientToUpdate.Passport = PassportBox.Text.Trim();
                    clientToUpdate.Birthday = DateOnly.FromDateTime(BirthdayPicker.SelectedDate.Value.DateTime);
                    clientToUpdate.Address = AddressBox.Text.Trim();
                    clientToUpdate.Email = EmailBox.Text.Trim();
                    clientToUpdate.Password = PasswordBox.Text;
                }
                else
                {
                    await MessageBoxManager.GetMessageBoxStandard(
                        "Ошибка",
                        "Не удалось найти клиента для обновления.",
                        ButtonEnum.Ok,
                        MsBox.Avalonia.Enums.Icon.Error)
                        .ShowAsync();
                    return;
                }
            }

            await context.SaveChangesAsync();

            await MessageBoxManager.GetMessageBoxStandard(
                "Успех",
                ExistingClient == null ? "Клиент успешно добавлен!" : "Изменения сохранены!",
                ButtonEnum.Ok,
               MsBox.Avalonia.Enums.Icon.Success)
               .ShowAsync();

            this.Close();
        }
        catch (ArgumentException argEx)
        {
            await MessageBoxManager.GetMessageBoxStandard(
               "Ошибка ввода",
               argEx.Message,
               ButtonEnum.Ok,
              MsBox.Avalonia.Enums.Icon.Error)
              .ShowAsync();
        }
        catch (Exception ex)
        {
            await MessageBoxManager.GetMessageBoxStandard(
                "Ошибка",
                $"Произошла непредвиденная ошибка: {ex.Message}",
                ButtonEnum.Ok,
               MsBox.Avalonia.Enums.Icon.Error)
               .ShowAsync();
        }
    }

    private void CorrectInput()
    {
        if (!int.TryParse(CodeBox.Text, out _) || CodeBox.Text.Length > 3 || CodeBox.Text.Length < 1)
        {
            throw new ArgumentException("Код клиента должен быть числом от 1 до 3 цифр");
        }

        if (PassportBox.Text.Length != 10 || !PassportBox.Text.All(char.IsDigit))
        {
            throw new ArgumentException("Серия и номер паспорта должны содержать ровно 10 цифр");
        }

        string email = EmailBox.Text.Trim(); 
        var atSymbolIndex = email.IndexOf('@');
        var dotSymbolIndex = email.LastIndexOf('.'); 

        if (atSymbolIndex == -1 || dotSymbolIndex == -1 || atSymbolIndex >= dotSymbolIndex || dotSymbolIndex == atSymbolIndex + 1 || atSymbolIndex == 0 || dotSymbolIndex == email.Length - 1)
        {
            throw new ArgumentException("Email должен содержать '@' и '.' ");
        }

        if (BirthdayPicker.SelectedDate == null)
        {
            throw new ArgumentException("Пожалуйста,\nвыберите дату рождения");
        }

        if (BirthdayPicker.SelectedDate.Value.DateTime.Date > DateTime.Today)
        {
            throw new ArgumentException("Дата рождения\nне может быть в будущем");
        }


    }

    private void BackOnOrder(object? sender, RoutedEventArgs e)
    {
        this.Close();
    }
}
