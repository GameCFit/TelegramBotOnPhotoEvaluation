using System;
using System.Collections.Generic;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Data.SQLite;
using System.Data;

namespace EboBotTelega
{
    public class Program
    {
        public static string token = "";

        public static int lenghtGirls = 42;

        static SQLiteConnection connection;
        static SQLiteCommand command;

        public static async Task Main(string[] args)
        {
            var bot = new TelegramBotClient(token);

            var me = await bot.GetMeAsync();

            using CancellationTokenSource cts = new();

            // StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
            ReceiverOptions receiverOptions = new()
            {
                AllowedUpdates = Array.Empty<UpdateType>() // receive all update types except ChatMember related updates
            };

            bot.StartReceiving(
                updateHandler: HandleUpdateAsync,
                pollingErrorHandler: HandlePollingErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: cts.Token
            );

            Console.WriteLine($"Start listening for @{me.Username}");
            Console.ReadLine();
            // Send cancellation request to stop bot

            cts.Cancel();
        }
        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Message is not { } message)
                return;
            // Only process text messages
            if (message.Text is not { } messageText)
                return;

            var chatId = message.Chat.Id;

            Console.WriteLine($"Text '{messageText}' id {chatId}.");

            if (message.Text == "/next")
            {
                Random random = new Random();
                int girl = random.Next(1, lenghtGirls);
                string nextUrl = "";
                int nextId = 0;

                string adress = @"users.sqlite";

                connection = new SQLiteConnection($"Data Source={adress};Version=3; FailIfMissing=False");
                connection.Open();
                var command = new SQLiteCommand(connection);
                command.CommandText = $"SELECT * FROM girls WHERE id = {girl}";
                DataTable data = new DataTable();
                SQLiteDataAdapter adapter = new SQLiteDataAdapter(command);
                adapter.Fill(data);
                foreach (DataRow row in data.Rows)
                {
                    nextUrl = row[1].ToString();
                    nextId = Convert.ToInt32(row[0].ToString());
                }
                var command1 = new SQLiteCommand(connection);
                command1.CommandText = $"UPDATE user SET selectGirl = {nextId} WHERE id = {message.Chat.Id}";
                command1.ExecuteNonQuery();

                ReplyKeyboardMarkup replyKeyboardMarkup = new(new[]
                {
                    new KeyboardButton[] { "👎", "❤️" },
                })
                {
                    ResizeKeyboard = true
                };

                Message sendMessage = await botClient.SendPhotoAsync(
                    chatId: chatId,
                    photo: InputFile.FromUri(nextUrl),
                    caption: "?",
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken,
                    replyMarkup: replyKeyboardMarkup);
            }
            else if (message.Text == "/start")
            {
                string adress = @"users.sqlite";

                connection = new SQLiteConnection($"Data Source={adress};Version=3; FailIfMissing=False");
                connection.Open();
                var command = new SQLiteCommand(connection);
                command.CommandText = $"INSERT INTO user(id, selectGirl) VALUES({message.Chat.Id}, 0)";
                command.ExecuteNonQuery();
                ReplyKeyboardMarkup replyKeyboardMarkup = new(new[]
{
                    new KeyboardButton[] { "/next", "/results" },
                })
                {
                    ResizeKeyboard = true
                };
                Message sentMessage = await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "❗️❗️<b>Все фото взяты из полностью открытых источников, ваши ответы анонимны</b>🌐.\n<i>Также сразу скажу что бот не использует конструкторы и написан на чистом языке🧑‍💻 и база данных не на сервере, так что разраба не найдёте)))🤭</i>",
                    parseMode: ParseMode.Html,
                    replyMarkup: replyKeyboardMarkup,
                    cancellationToken: cancellationToken);
            }
            else if(message.Text == "❤️" || message.Text == "👎")
            {
                string adress = @"users.sqlite";
                int likes = 0;
                string userId = "";
                int userSelect = 0;
                connection = new SQLiteConnection($"Data Source={adress};Version=3; FailIfMissing=False");
                connection.Open();
                var command2 = new SQLiteCommand(connection);
                command2.CommandText = $"SELECT * FROM user WHERE id = {message.Chat.Id}";
                DataTable data1 = new DataTable();
                SQLiteDataAdapter adapter1 = new SQLiteDataAdapter(command2);
                adapter1.Fill(data1);
                foreach (DataRow row in data1.Rows)
                {
                    userId = row[0].ToString();
                    userSelect = Convert.ToInt32(row[1].ToString());
                }
                var command1 = new SQLiteCommand(connection);
                command1.CommandText = $"SELECT * FROM girls WHERE id = {userSelect}";
                DataTable data = new DataTable();
                SQLiteDataAdapter adapter = new SQLiteDataAdapter(command1);
                adapter.Fill(data);
                foreach (DataRow row in data.Rows)
                {
                    likes = Convert.ToInt32(row[2].ToString());
                }
                likes += message.Text == "❤️" ? 1 : -1;
                var command = new SQLiteCommand(connection);
                command.CommandText = $"UPDATE girls SET likes = {likes} WHERE id = {userSelect}";
                command.ExecuteNonQuery();
                ReplyKeyboardMarkup replyKeyboardMarkup = new(new[]
                {
                    new KeyboardButton[] { "/next", "/results" },
                })
                {
                    ResizeKeyboard = true
                };
                Message sentMessage = await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "✅",
                    replyMarkup: replyKeyboardMarkup,
                    cancellationToken: cancellationToken);
            }
            else if(message.Text == "/results")
            {
                string adress = @"users.sqlite";

                string lastUserUrl = "";

                string likes = "";

                connection = new SQLiteConnection($"Data Source={adress};Version=3; FailIfMissing=False");
                connection.Open();
                var command = new SQLiteCommand(connection);
                command.CommandText = "SELECT * FROM girls ORDER BY likes DESC";
                DataTable data = new DataTable();
                SQLiteDataAdapter adapter = new SQLiteDataAdapter(command);
                adapter.Fill(data);
                foreach (DataRow row in data.Rows)
                {
                    lastUserUrl = row[1].ToString();
                    likes = row[2].ToString();
                }
                Message sendMessage = await botClient.SendPhotoAsync(
                chatId: chatId,
                photo: InputFile.FromUri(lastUserUrl),
                caption: $"👎🏿👎🏿👎🏿({likes})",
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken,
                replyMarkup: new ReplyKeyboardRemove());

                var command1 = new SQLiteCommand(connection);
                command1.CommandText = "SELECT * FROM girls ORDER BY likes DESC LIMIT 1";
                DataTable data1 = new DataTable();
                SQLiteDataAdapter adapter1 = new SQLiteDataAdapter(command1);
                adapter1.Fill(data1);
                foreach (DataRow row in data1.Rows)
                {
                    lastUserUrl = row[1].ToString();
                    likes = row[2].ToString();
                }

                ReplyKeyboardMarkup replyKeyboardMarkup = new(new[]
                {
                    new KeyboardButton[] { "/next" },
                })
                {
                    ResizeKeyboard = true
                };

                Message sendMessage1 = await botClient.SendPhotoAsync(
                chatId: chatId,
                photo: InputFile.FromUri(lastUserUrl),
                caption: $"❤️❤️❤️({likes})",
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken,
                replyMarkup: replyKeyboardMarkup);
            }
        }

        public static Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }
    }
}
