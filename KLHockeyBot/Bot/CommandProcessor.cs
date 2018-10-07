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
        private readonly Randomiser Gen;
        private DBCore DB;

        public CommandProcessor(TelegramBotClient bot)
        {
            Bot = bot;
            Gen = new Randomiser();
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
                        await Bot.SendTextMessageAsync(chatFinded.Id, "Добавьте игрока в формате '99;Имя;Фамилия'");
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
                if (command == "кричалки")
                {
                    Slogans(chatFinded);
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

            var detailedResult = "";
            if (e.Data == "Подробнее")
            {
                detailedResult += "\n*Да*:\n";
                var votes = voting.V.FindAll(x => x.Data == "Да");
                foreach (var vote in votes)
                {
                    detailedResult += $" {vote.Name} {vote.Surname}\n";
                }
                if (votes.Count == 0) detailedResult += " -\n";

                detailedResult += "*Нет*:\n";
                votes = voting.V.FindAll(x => x.Data == "Нет");
                foreach (var vote in voting.V.FindAll(x => x.Data == "Нет"))
                {
                    detailedResult += $" {vote.Name} {vote.Surname}\n";
                }
                if (votes.Count == 0) detailedResult += " -\n";

                detailedResult += "*Хз*:\n";
                votes = voting.V.FindAll(x => x.Data == ":(");
                foreach (var vote in voting.V.FindAll(x => x.Data == ":("))
                {
                    detailedResult += $" {vote.Name} {vote.Surname}\n";
                }
                if (votes.Count == 0) detailedResult += " -\n";
            }
            else
            {
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
            }

            var short_result = $"Да:{voting.V.Count(x => x.Data == "Да")};Нет:{voting.V.Count(x => x.Data == "Нет")};Хз:{voting.V.Count(x => x.Data == ":(")}";

            var btn_yes = new InlineKeyboardButton
            {
                Text = "Да"
            };
            var btn_no = new InlineKeyboardButton
            {
                Text = "Нет"
            };
            var btn_unk = new InlineKeyboardButton
            { 
                Text = ":(" 
            };
            var btn_res = new InlineKeyboardButton
            {
                Text = "Подробнее"
            };
            InlineKeyboardMarkup keyboard;
            if (e.Data == "Подробнее")
            {
                keyboard = new InlineKeyboardMarkup(new[] { new[] { btn_yes, btn_no, btn_unk } });
            }
            else
            {
                keyboard = new InlineKeyboardMarkup(new[] { new[] { btn_yes, btn_no, btn_unk }, new[] { btn_res } });
            }

            try
            {
                var answer = $"*{voting.Question}*\n{short_result}\n{detailedResult}";
                await Bot.EditMessageTextAsync(chatFinded.Id, msgid, answer, parseMode: ParseMode.Markdown, replyMarkup: keyboard);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public async void ContinueWaitingPlayerStatistic(Chat chatFinded, int msgid)
        {
            var stat = chatFinded.WaitingStatistics.FindLast(x => x.Msg.MessageId == msgid);
            if (stat == null) return;

            var statistic = $"*Статистика по #{stat.Plr.Number}:*\n\nПривет! Я - статистика 💥";

            var button = new InlineKeyboardButton
            {
                Text = "Donate for it!",
                CallbackData = "Soon"
            };
            var keyboard = new InlineKeyboardMarkup(new[] { new[] { button } });
            await Bot.EditMessageCaptionAsync(chatFinded.Id, msgid, stat.Msg.Caption);
            await Bot.EditMessageReplyMarkupAsync(chatFinded.Id, msgid, replyMarkup: keyboard);
            //await Bot.SendTextMessageAsync(chatFinded.Id, statistic, parseMode: ParseMode.Markdown);
            chatFinded.WaitingStatistics.Remove(stat);
        }

        public async void ContinueWaitingEvent(Chat chatFinded, int msgid)
        {
            var even = chatFinded.WaitingEvents.FindLast(x => x.Msg.MessageId == msgid);
            if (even == null) return;

            var more = $"\n\n{even.Even.Address}\n\n{even.Even.Details}";
            var who = $"{even.Even.Members}";

            await Bot.EditMessageTextAsync(chatFinded.Id, msgid, $"*{even.Msg.Text}*{more}", parseMode: ParseMode.Markdown);
            if (who != "")
            {
                await Bot.SendTextMessageAsync(chatFinded.Id, who, parseMode: ParseMode.Markdown);
            }
            chatFinded.WaitingEvents.Remove(even);
        }

        private async void WrongCmd(Chat chatFinded)
        {
            chatFinded.ResetMode();
            var keys = new ReplyKeyboardMarkup
            {
                Keyboard = new KeyboardButton[1][]
            };
            keys.Keyboard = (System.Collections.Generic.IEnumerable<System.Collections.Generic.IEnumerable<Telegram.Bot.Types.ReplyMarkups.KeyboardButton>>)(new KeyboardButton[1] { new KeyboardButton("/помощь") });
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
                result = "*Прошедшие игры:*\n\n" + result;
                await Bot.SendTextMessageAsync(chatFinded.Id, result, parseMode: ParseMode.Markdown);
            }
        }

        private async void AddPlayer(Chat chatFinded, string argv)
        {
            //argv format is number;name;surname
            chatFinded.AddMode = false;
            var playerinfo = argv.Split(';');
            if (playerinfo.Length == 3)
            {
                var player = new Player(int.Parse(playerinfo[0]), playerinfo[1].Trim(), playerinfo[2].Trim());
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
                Text = "Да"
            };
            var btn_no = new InlineKeyboardButton
            {
                Text = "Нет"
            };
            var btn_unk = new InlineKeyboardButton
            {
                Text = ":("
            };
            var keyboard = new InlineKeyboardMarkup(new[] { new[] { btn_yes, btn_no, btn_unk } });

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
                    var playerDescription = Gen.GetPlayerDescr();
                    playerDescription += $"#{player.Number} {player.Name} {player.Surname}";

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
                        var playerDescription = Gen.GetPlayerDescr();
                        playerDescription += $"#{player.Number} {player.Name} {player.Surname}";

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
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                await Bot.SendTextMessageAsync(chatFinded.Id, "Ваш запрос не удалось обработать.");
            }
        }

        private async void Slogans(Chat chatFinded)
        {
            await Bot.SendTextMessageAsync(chatFinded.Id, Gen.GetSlogan());
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
                    if (games.Count > 1)
                    {
                        await Bot.SendTextMessageAsync(chatFinded.Id, "Ура! По вашему запросу найдено несколько игр, сейчас их все покажу.");
                    }

                    foreach (var game in games)
                    {
                        var button = new InlineKeyboardButton()
                        {
                                Text = "Подробнее"
                        };
                        var keyboard = new InlineKeyboardMarkup(new[] { new[] { button } });
                                                
                        var msg = await Bot.SendTextMessageAsync(chatFinded.Id, $"*{game.Date} {game.Time}*\n{game.Place}", replyMarkup: keyboard);
                        chatFinded.WaitingEvents.Add(new WaitingEvent() { Msg = msg, Even = game });
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
                        var button = new InlineKeyboardButton()
                        {
                            Text = "Подробнее"
                        };
                        var keyboard = new InlineKeyboardMarkup(new[] { new[] { button } });

                        var msg = await Bot.SendTextMessageAsync(chatFinded.Id, $"*{game.Date} {game.Time}*\n{game.Place}", parseMode: ParseMode.Markdown, replyMarkup: keyboard);
                        chatFinded.WaitingEvents.Add(new WaitingEvent() { Msg = msg, Even = game });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                await Bot.SendTextMessageAsync(chatFinded.Id, "Ваш запрос не удалось обработать.");
            }
        }

        private async void StatisticByNumber(Chat chatFinded, int number)
        {
            chatFinded.PersonalStatMode = false;
            var result = "Игрок не найден";
            var player = DB.GetPlayerStatisticByNumber(number);
            if (player != null)
            {
                result = "Пока не закодили :(";
            }

            await Bot.SendTextMessageAsync(chatFinded.Id, result);
        }

        private async void Help(Chat chatFinded)
        {
            var p = DB.GetAllPlayerWitoutStatistic();
            var num = p[(new Random()).Next(p.Count - 1)].Number;
            var name = p[(new Random()).Next(p.Count - 1)].Name;
            var surname = p[(new Random()).Next(p.Count - 1)].Surname;

            var keys = new ReplyKeyboardMarkup
            {
                    Keyboard = new KeyboardButton[3][]
                    {
                        new KeyboardButton[2]
                        {
                            new KeyboardButton() {Text = "/" + surname},
                            new KeyboardButton() {Text = "/" + "новости"}
                        },
                        new KeyboardButton[2]
                        {
                            new KeyboardButton() {Text = "/" + "трени"},
                            new KeyboardButton() {Text = "/" + "игры"}
                        },
                        new KeyboardButton[2]
                        {
                            new KeyboardButton() {Text = "/" + "кричалки"},
                            new KeyboardButton() {Text = "/" + "помощь"}
                        }
                    },
                OneTimeKeyboard = true
            };

            var help =
@"*Бот умеет*:

*Поискать* игрока по
'%номер%'
'%имя%'
'%фамилия%'

*Показать*
/игры
/трени
/кричалки
/новости
/помощь

💥Удачи!💥";

            help = help.Replace("'%номер%'", $"{num}");
            help = help.Replace("'%имя%'", $"{name}");
            help = help.Replace("'%фамилия%'", $"{surname}");

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
