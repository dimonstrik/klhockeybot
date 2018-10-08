using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Telegram.Bot;
using KLHockeyBot.Configs;
using Telegram.Bot.Types;
using File = System.IO.File;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;

namespace KLHockeyBot
{
    public class CommandProcessor
    {
        private TelegramBotClient Bot;
        private DBCore DB;

        public CommandProcessor(TelegramBotClient bot)
        {
            Bot = bot;
            DB = new DBCore();
        }

        public async void FindCommands(string msg, Chat chatFinded, int fromId)
        {
            var commands = msg.Split(' ');

            if(commands.Length > 10)
            {
                await Bot.SendTextMessageAsync(chatFinded.Id, "Сорри, но мне лень обрабатывать столько команд.");
                return;
            }

            if(chatFinded.CommandsQueue.Count > 20)
            {
                Console.WriteLine("Too big queue. Reset it.");
                chatFinded.CommandsQueue.Clear();
                return;
            }

            foreach (var command in commands)
            {
                chatFinded.CommandsQueue.Enqueue(command);
            }

            ProcessCommands(chatFinded, fromId);            
        }

        private async void ProcessCommands(Chat chatFinded, int fromId)
        {
            var commands = chatFinded.CommandsQueue;
            var rxNums = new Regex(@"^\d+$"); // проверка на число

            while (commands.Count > 0)
            {
                var command = commands.Dequeue();
                var isLastCommand = (commands.Count == 0);                

                //set modes
                if (command == "add")
                {
                    if (!Config.BotAdmin.isAdmin(fromId))
                    {
                        await Bot.SendTextMessageAsync(chatFinded.Id, "Вам не разрешено пользоваться командой add. Запрос отменён.");
                        chatFinded.ResetMode();
                        continue;
                    }

                    chatFinded.AddMode = true;
                    if (isLastCommand)
                    {
                        await Bot.SendTextMessageAsync(chatFinded.Id, "Добавьте игрока в формате '99;Имя;Фамилия;Nickname'");
                    }
                    continue;
                }

                if (command == "remove")
                {
                    if (!Config.BotAdmin.isAdmin(fromId))
                    {
                        await Bot.SendTextMessageAsync(chatFinded.Id, "Вам не разрешено пользоваться командой remove. Запрос отменён.");
                        chatFinded.ResetMode();
                        continue;
                    }

                    chatFinded.RemoveMode = true;
                    if (isLastCommand)
                    {
                        await Bot.SendTextMessageAsync(chatFinded.Id, "Удалите игрока по 'номеру'");
                    }
                    continue;
                }

                if (command == "vote")
                {
                    chatFinded.VoteMode = true;
                    if (isLastCommand)
                    {
                        await Bot.SendTextMessageAsync(chatFinded.Id, "Задайте вопрос голосования:");
                    }
                    continue;
                }

                //check modes
                if (chatFinded.AddMode)
                {
                    AddPlayer(chatFinded, command);
                    continue;
                }

                if (chatFinded.RemoveMode)
                {
                    try
                    {
                        var number = int.Parse(command);
                        RemovePlayer(chatFinded, number);
                        continue;
                    }
                    catch (Exception ex)
                    {
                        ExceptionOnCmd(chatFinded, ex);
                        continue;
                    }
                }

                if (chatFinded.VoteMode)
                {
                    while (commands.Count != 0) command += " " + commands.Dequeue();
                    AddVoting(chatFinded, command);
                    continue;
                }

                //do command
                if (command == "init")
                {
                    try
                    {
                        DBCore.Initialization();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Unknown DBCore exception: " + e.Message + "\n" + e.InnerException);
                    }
                    continue;
                }
                if (command == "помощь")
                {
                    Help(chatFinded);
                    continue;
                }
                if (command == "новости")
                {
                    News(chatFinded);
                    continue;
                }
                if (command == "игры")
                {
                    Game(chatFinded);
                    continue;
                }
                if (command == "трени")
                {
                    Training(chatFinded);
                    continue;
                }                

                //если не в режиме, не установили режим, не выполнили команду сразу, может пользователь ввёл число для поиска игрока
                if (rxNums.IsMatch(command))
                {
                    //в случае числа показываем игрока
                    try
                    {
                        var number = int.Parse(command);
                        ShowPlayerByNubmer(chatFinded, number);
                        continue;
                    }
                    catch (Exception ex)
                    {
                        ExceptionOnCmd(chatFinded, ex);
                        continue;
                    }
                }

                if (isLastCommand)
                {
                    //в случае букв ищем по имени или фамилии 
                    try
                    {
                        ShowPlayersByNameOrSurname(chatFinded, command);
                        continue;
                    }
                    catch (Exception ex)
                    {
                        ExceptionOnCmd(chatFinded, ex);
                        continue;
                    }
                }                
            }
        }

        internal async void ContinueWaitingVoting(Chat chatFinded, int msgid, CallbackQuery e)
        {
            var voting = chatFinded.WaitingVotings.FindLast(x => x.MessageId == msgid);
            if (voting == null) return;

            var vote = new Vote(e.From.FirstName, e.From.LastName, e.Data);
            var voteDupl = voting.V.FindLast(x => x.Name == vote.Name && x.Surname == vote.Surname);
            if (voteDupl != null)
            {
                if (voteDupl.Data == vote.Data) return;

                voteDupl.Data = vote.Data;
                DB.UpdateVoteData(msgid, vote.Name, vote.Surname, vote.Data);
            }
            else
            {
                voting.V.Add(vote);
                DB.AddVote(msgid, vote.Name, vote.Surname, vote.Data);
            }

            var yes_cnt = voting.V.Count(x => x.Data == "Да");
            var detailedResult = $"\nДа – {yes_cnt}\n";
            var votes = voting.V.FindAll(x => x.Data == "Да");
            foreach (var v in votes)
            {
                detailedResult += $" {v.Name} {v.Surname}\n";
            }
            if (votes.Count == 0) detailedResult += " -\n";

            var no_cnt = voting.V.Count(x => x.Data == "Не");
            detailedResult += $"\nНе – {no_cnt}\n";
            votes = voting.V.FindAll(x => x.Data == "Не");
            foreach (var v in voting.V.FindAll(x => x.Data == "Не"))
            {
                detailedResult += $" {v.Name} {v.Surname}\n";
            }
            if (votes.Count == 0) detailedResult += " -\n";

            var cnt = yes_cnt + no_cnt;

            var btn_yes = new InlineKeyboardButton
            {
                Text = $"Да – {yes_cnt}",
                CallbackData = "Да"
            };
            var btn_no = new InlineKeyboardButton
            {
                Text = $"Не – {no_cnt}",
                CallbackData = "Не"
            };

            InlineKeyboardMarkup keyboard = new InlineKeyboardMarkup(new[] { btn_yes, btn_no });

            try
            {
                var answer = $"*{voting.Question}*\n{detailedResult}\n👥 {cnt} человек проголосовало.";
                await Bot.EditMessageTextAsync(chatFinded.Id, msgid, answer, parseMode: ParseMode.Markdown, replyMarkup: keyboard);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private async void WrongCmd(Chat chatFinded)
        {
            chatFinded.ResetMode();
            var keys = new ReplyKeyboardMarkup
            {
                Keyboard = new[] { new KeyboardButton[1] { new KeyboardButton("/помощь") } }
            };
            keys.ResizeKeyboard = true;
            keys.OneTimeKeyboard = true;
            await Bot.SendTextMessageAsync(chatFinded.Id, "Неверный запрос, воспользуйтесь /помощь", ParseMode.Default, false, false, 0, keys);
        }

        private async void ExceptionOnCmd(Chat chatFinded, Exception ex)
        {
            chatFinded.ResetMode();
            Console.WriteLine(ex.Message);
            await Bot.SendTextMessageAsync(chatFinded.Id, "Ваш запрос не удалось обработать. Запрос отменён.");
        }

        private async void News(Chat chatFinded)
        {
            var events = File.ReadAllLines(Config.DBGamesInfoPath);
            var result = "";
            foreach (var even in events)
            {
                result += even.Replace('%', '\n');
                result += "\n\n";
            }
            if (result == "")
            {
                await Bot.SendTextMessageAsync(chatFinded.Id, "Нет новостей :(");
            }
            else
            {
                await Bot.SendTextMessageAsync(chatFinded.Id, result, parseMode: ParseMode.Markdown);
            }
        }

        private async void AddPlayer(Chat chatFinded, string argv)
        {
            //argv format is number;name;surname
            chatFinded.AddMode = false;
            var playerinfo = argv.Split(';');
            if (playerinfo.Length == 4)
            {
                var player = new Player(int.Parse(playerinfo[0]), playerinfo[1].Trim(), playerinfo[2].Trim(), playerinfo[3].Trim());
                DB.AddPlayer(player);
                await Bot.SendTextMessageAsync(chatFinded.Id, $"Попробовали добавить {player.Number}.");
            }
            else
            {
                await Bot.SendTextMessageAsync(chatFinded.Id, $"Неверный формат запроса: {argv}");
            }
        }

        private async void RemovePlayer(Chat chatFinded, int number)
        {
            chatFinded.RemoveMode = false;
            DB.RemovePlayerByNumber(number);
            await Bot.SendTextMessageAsync(chatFinded.Id, $"Попробовали удалить {number}, проверим успешность поиском.");
            ShowPlayerByNubmer(chatFinded, number);
        }

        private async void AddVoting(Chat chatFinded, string command)
        {
            chatFinded.VoteMode = false;
            var btn_yes = new InlineKeyboardButton
            {
                Text = "Да",
                CallbackData = "Да"
            };
            var btn_no = new InlineKeyboardButton
            {
                Text = "Не",
                CallbackData = "Не"
            };
            var keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[2] { btn_yes, btn_no });

            var msg = await Bot.SendTextMessageAsync(chatFinded.Id, $"{command}", replyMarkup: keyboard);
            var v = new List<Vote>();
            var voting = new WaitingVoting() {MessageId = msg.MessageId, V = v, Question = command};

            DB.AddVoting(voting);
            chatFinded.WaitingVotings.Add(voting);
        }

        private async void ShowPlayerByNubmer(Chat chatFinded, int playerNumber)
        {
            if (playerNumber < 0 || playerNumber > 100)
            {
                await Bot.SendTextMessageAsync(chatFinded.Id, "Неверный формат, введите корректный номер игрока от 0 до 100.");
                return;
            }

            try
            {
                var player = DB.GetPlayerByNumber(playerNumber);
                if (player == null)
                {
                    await Bot.SendTextMessageAsync(chatFinded.Id, $"Игрок под номером {playerNumber} не найден.");
                }
                else
                {
                    var playerDescription = $"#{player.Number} {player.Name} {player.Surname}";

                    var photopath = Path.Combine(Config.DBPlayersPhotoDirPath, player.PhotoFile);

                    Console.WriteLine($"Send player:{player.Surname}");
                    if (File.Exists(photopath))
                    {
                            var photo = new Telegram.Bot.Types.InputFiles.InputOnlineFile(
                            (new StreamReader(photopath)).BaseStream,
                            player.Number + ".jpg");
                                                    
                        var button = new InlineKeyboardButton()
                        {
                                Text = "Cтатистика"
                        };
                        var keyboard = new InlineKeyboardMarkup(new[] { new[] { button } });

                        var msg = await Bot.SendPhotoAsync(chatFinded.Id, photo, playerDescription, replyMarkup: keyboard);
                        chatFinded.WaitingStatistics.Add(new WaitingStatistic() { Msg = msg, Plr = player });
                    }
                    else
                    {
                        Console.WriteLine($"Photo file {photopath} not found.");
                        await Bot.SendTextMessageAsync(chatFinded.Id, playerDescription);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                await Bot.SendTextMessageAsync(chatFinded.Id, "Ваш запрос не удалось обработать.");
            }
        }

        private async void ShowPlayersByNameOrSurname(Chat chatFinded, string nameOrSurname)
        {
            try
            {
                var players = DB.GetPlayersByNameOrSurname(nameOrSurname);
                if (players.Count == 0)
                {
                    //иначе пользователь ввёл хуйню
                    WrongCmd(chatFinded);
                    return;
                }
                else
                {
                    if(players.Count > 1)
                    {
                        await Bot.SendTextMessageAsync(chatFinded.Id, "По вашему запросу найдено несколько игроков, сейчас их покажу.");
                    }

                    foreach (var player in players)
                    {
                        var playerDescription = $"#{player.Number} {player.Name} {player.Surname}";

                        var photopath = Path.Combine(Config.DBPlayersPhotoDirPath, player.PhotoFile);

                        Console.WriteLine($"Send player:{player.Surname}");
                        if (File.Exists(photopath))
                        {
                            var photo = new Telegram.Bot.Types.InputFiles.InputOnlineFile(
                                    (new StreamReader(photopath)).BaseStream,
                                    player.Number + ".jpg");

                            var msg = await Bot.SendPhotoAsync(chatFinded.Id, photo, playerDescription);
                        }
                        else
                        {
                            Console.WriteLine($"Photo file {photopath} not found.");
                            await Bot.SendTextMessageAsync(chatFinded.Id, playerDescription);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                await Bot.SendTextMessageAsync(chatFinded.Id, "Ваш запрос не удалось обработать.");
            }
        }

        private async void Game(Chat chatFinded)
        {
            try
            {
                var games = DB.GetEventsByType("Игра");
                if (games.Count == 0)
                {
                    await Bot.SendTextMessageAsync(chatFinded.Id, "Ближайших игр не найдено.");
                    return;
                }
                else
                {
                    foreach (var game in games)
                    {
                        var msg = await Bot.SendTextMessageAsync(chatFinded.Id, $"*{game.Date} {game.Time}*\n{game.Place}\n{game.Details}\n{game.Result}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                await Bot.SendTextMessageAsync(chatFinded.Id, "Ваш запрос не удалось обработать.");
            }
        }

        private async void Training(Chat chatFinded)
        {
            try
            {
                var games = DB.GetEventsByType("Треня");
                if (games.Count == 0)
                {
                    await Bot.SendTextMessageAsync(chatFinded.Id, "Ближайших трень не найдено.");
                    return;
                }
                else
                {                   
                    foreach (var game in games)
                    {
                        var msg = await Bot.SendTextMessageAsync(chatFinded.Id, $"*{game.Date} {game.Time}*\n{game.Place}\n{game.Details}\n{game.Result}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                await Bot.SendTextMessageAsync(chatFinded.Id, "Ваш запрос не удалось обработать.");
            }
        }

        private async void Help(Chat chatFinded)
        {
            var keys = new ReplyKeyboardMarkup
            {
                    Keyboard = new KeyboardButton[2][]
                    {
                        new KeyboardButton[2]
                        {
                            new KeyboardButton() {Text = "/" + "трени"},
                            new KeyboardButton() {Text = "/" + "игры"}
                        },
                        new KeyboardButton[2]
                        {
                            new KeyboardButton() {Text = "/" + "новости"},
                            new KeyboardButton() {Text = "/" + "помощь"}
                        }
                    },
                OneTimeKeyboard = true
            };

            var help =
@"*Бот умеет*:

*Поискать* игрока 
по номеру или имени

*Показать*
/игры
/трени
/новости
/помощь

💥Удачи!💥";

            await Bot.SendTextMessageAsync(chatFinded.Id, help, ParseMode.Markdown, false, false, 0, keys);
        }

        public void TryToRestoreVotingFromDb(int messageId, Chat chat)
        {
            var voting = DB.GetVotingById(messageId);
            if (voting == null) return;

            chat.WaitingVotings.Add(voting);

            voting.V = DB.GetVotesByMessageId(messageId);
            Console.WriteLine("Voting restored from DB: " + voting.Question);
        }
    }
}
