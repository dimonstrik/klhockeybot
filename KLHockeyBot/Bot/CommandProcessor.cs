using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using KLHockeyBot.Configs;
using KLHockeyBot.Data;
using KLHockeyBot.DB;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using File = System.IO.File;

namespace KLHockeyBot.Bot
{
    public class CommandProcessor
    {
        private readonly TelegramBotClient _bot;
        private DbCore _db;

        private Player _currentPlayer;
        private WaitingVoting _currentPoll;

        public CommandProcessor(TelegramBotClient bot)
        {
            _bot = bot;
            _db = new DbCore();
        }

        public async void FindCommands(string msg, Chat chatFinded, int fromId)
        {
            var commands = msg.Split(' ');

            if(commands.Length > 10)
            {
                await _bot.SendTextMessageAsync(chatFinded.Id, "Сорри, но мне лень обрабатывать столько команд.");
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

                switch (command)
                {
                    //set modes
                    case "add" when !Config.BotAdmin.IsAdmin(fromId):
                        await _bot.SendTextMessageAsync(chatFinded.Id, "Вам не разрешено пользоваться командой add. Запрос отменён.");
                        chatFinded.ResetMode();
                        continue;
                    case "add":
                        chatFinded.AddMode = true;
                        if (isLastCommand)
                        {
                            await _bot.SendTextMessageAsync(chatFinded.Id, "Добавьте игрока в формате '1;Фамилия;Имя;Отчество;01.01.1988;Вратарь;Стена;0'");
                        }
                        continue;
                    case "remove" when !Config.BotAdmin.IsAdmin(fromId):
                        await _bot.SendTextMessageAsync(chatFinded.Id, "Вам не разрешено пользоваться командой remove. Запрос отменён.");
                        chatFinded.ResetMode();
                        continue;
                    case "remove":
                        chatFinded.RemoveMode = true;
                        if (isLastCommand)
                        {
                            await _bot.SendTextMessageAsync(chatFinded.Id, "Удалите игрока по 'id'");
                        }
                        continue;
                    case "updateuserid" when !Config.BotAdmin.IsAdmin(fromId):
                        await _bot.SendTextMessageAsync(chatFinded.Id, "Вам не разрешено пользоваться командой updateuserid. Запрос отменён.");
                        chatFinded.ResetMode();
                        continue;
                    case "updateuserid":
                        chatFinded.UpdateUseridMode = true;
                        if (isLastCommand)
                        {
                            await _bot.SendTextMessageAsync(chatFinded.Id, "Задайте пару id игрока; userid telegram");
                        }
                        continue;
                    case "vote":
                        chatFinded.VoteMode = true;
                        if (isLastCommand)
                        {
                            await _bot.SendTextMessageAsync(chatFinded.Id, "Задайте вопрос голосования:");
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

                if (chatFinded.UpdateUseridMode)
                {
                    UpdatePlayerUserid(chatFinded, command);
                    continue;
                }

                if (chatFinded.VoteMode)
                {
                    while (commands.Count != 0) command += " " + commands.Dequeue();
                    AddVoting(chatFinded, command);
                    continue;
                }

                switch (command)
                {
                    //do command
                    case "secrets" when !Config.BotAdmin.IsAdmin(fromId):
                        await _bot.SendTextMessageAsync(chatFinded.Id, "Вам не разрешено пользоваться командой secrets. Запрос отменён.");
                        continue;
                    case "secrets":
                        await _bot.SendTextMessageAsync(chatFinded.Id, "/init /showuserids /showplayers /add /remove /updateuserid /vote /voteadmin");
                        continue;
                    case "admin" when !Config.BotAdmin.IsAdmin(fromId):
                        await _bot.SendTextMessageAsync(chatFinded.Id, "Вам не разрешено пользоваться командой admin. Запрос отменён.");
                        chatFinded.ResetMode();
                        continue;
                    case "admin":
                        Admin(chatFinded);
                        continue;
                    case "init":
                        try
                        {
                            _db.Disconnect();
                            DbCore.Initialization();
                            _db = new DbCore();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Unknown DBCore exception: " + e.Message + "\n" + e.InnerException);
                        }
                        continue;
                    case "showuserids":
                        ShowUserids(chatFinded);
                        continue;
                    case "admin_" + "showplayers":
                        ShowPlayers(chatFinded);
                        continue;
                    case "admin_" + "showpolls":
                        ShowPolls(chatFinded);
                        continue;
                    case "admin_" + "addvote":
                        //ContinueWaitingVoting(chatFinded, );
                        continue;
                    case "помощь":
                        Help(chatFinded);
                        continue;
                    case "новости":
                        News(chatFinded);
                        continue;
                    case "игры":
                        Game(chatFinded);
                        continue;
                    case "трени":
                        Training(chatFinded);
                        continue;
                }

                if (command.StartsWith("admin_setplr_"))
                {
                    var playerId = Convert.ToInt32(command.Split('_')[2]);
                    var player = _db.GetPlayerById(playerId);
                    if (player == null)
                    {
                        await _bot.SendTextMessageAsync(chatFinded.Id, $"Не удалось выбрать игрока с id:{playerId}.");
                        continue;
                    }

                    _currentPlayer = player;
                    await _bot.SendTextMessageAsync(chatFinded.Id, $"Выбран: {player.Name} {player.Surname}");
                    continue;
                }

                if (command.StartsWith("admin_setpoll_"))
                {
                    var pollId = Convert.ToInt32(command.Split('_')[2]);
                    var poll = _db.GetPollById(pollId);
                    if (poll == null)
                    {
                        await _bot.SendTextMessageAsync(chatFinded.Id, $"Не удалось выбрать голосование с id:{pollId}.");
                        continue;
                    }

                    _currentPoll = poll;
                    await _bot.SendTextMessageAsync(chatFinded.Id, $"Выбран: {poll.Question}");
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

                if (!isLastCommand) continue;
                
                //в случае букв ищем по имени или фамилии 
                try
                {
                    ShowPlayersByNameOrSurname(chatFinded, command);
                }
                catch (Exception ex)
                {
                    ExceptionOnCmd(chatFinded, ex);
                }
            }
        }

        private async void ShowPolls(Chat chatFinded)
        {
            try
            {
                var polls = _db.GetAllPolls();
                if (polls.Count == 0)
                {
                    await _bot.SendTextMessageAsync(chatFinded.Id, "Игроки не найдены.");
                }
                else
                {
                    var txt = "";
                    foreach (var poll in polls)
                    {
                        txt += $"/admin_setpoll_{poll.Id} *{poll.Question}* msgid:{poll.MessageId}\n";
                    }

                    txt = txt.Replace("_", @"\_");
                    await _bot.SendTextMessageAsync(chatFinded.Id, txt, ParseMode.Markdown);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                await _bot.SendTextMessageAsync(chatFinded.Id, "Ваш запрос не удалось обработать.", ParseMode.Markdown);
            }
        }

        private async void Admin(Chat chatFinded)
        {
            var keyboard = new InlineKeyboardMarkup(
                new[]
                {
                    new[]
                    {
                        new InlineKeyboardButton()
                        {
                            Text = "Set player",
                            CallbackData = "/admin_" + "showplayers"
                        },
                        new InlineKeyboardButton()
                        {
                            Text = "Set poll",
                            CallbackData = "/admin_" + "showpolls"
                        }
                    },
                    new[]
                    {
                        new InlineKeyboardButton()
                        {
                            Text = "Add Vote",
                            CallbackData = "/admin_" + "addvote"
                        },
                        new InlineKeyboardButton()
                        {
                            Text = "Delete Vote",
                            CallbackData = "/admin_" + "deletevote"
                        }
                    },
                    new[]
                    {
                        new InlineKeyboardButton()
                        {
                            Text = "Delete Poll",
                            CallbackData = "/admin_" + "deletepoll"
                        },
                        new InlineKeyboardButton()
                        {
                            Text = "Delete Player",
                            CallbackData = "/admin_" + "delteplayer"
                        }
                    },
                    new[]
                    {
                        new InlineKeyboardButton()
                        {
                            Text = "Add Player",
                            CallbackData = "/admin_" + "addplayer"
                        },
                        new InlineKeyboardButton()
                        {
                            Text = "Edit Player",
                            CallbackData = "/admin_" + "editplayer"
                        }
                    }
                });

            const string help =
                @"Установи игрока и/или опрос, затем можно командами править данные бота.";

            await _bot.SendTextMessageAsync(chatFinded.Id, help, ParseMode.Markdown, false, false, 0, keyboard);
        }


        private async void ShowPlayers(Chat chatFinded)
        {
            try
            {
                var players = _db.GetAllPlayerWitoutStatistic();
                if (players.Count == 0)
                {
                    await _bot.SendTextMessageAsync(chatFinded.Id, "Игроки не найдены.");
                }
                else
                {
                    var txt = "";
                    foreach (var player in players)
                    {
                        txt += $"/admin_setplr_{player.Id} *{player.Name} {player.Surname}* userid:{player.Userid}\n";
                    }

                    txt = txt.Replace("_", @"\_");
                    await _bot.SendTextMessageAsync(chatFinded.Id, txt, ParseMode.Markdown);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                await _bot.SendTextMessageAsync(chatFinded.Id, "Ваш запрос не удалось обработать.", ParseMode.Markdown);
            }
        }

        private async void ShowUserids(Chat chatFinded)
        {
            try
            {
                var votes = _db.GetAllUniqueVotesByUserid();
                if (votes.Count == 0)
                {
                    await _bot.SendTextMessageAsync(chatFinded.Id, "Голоса не найдены.");
                }
                else
                {
                    var txt = "";
                    foreach (var vote in votes)
                    {
                        var username = string.IsNullOrEmpty(vote.Username) ? "" : $"(@{vote.Username})";
                        txt += $"*{vote.Name} {vote.Surname}* {username} userid:{vote.Userid}\n";
                    }

                    txt = txt.Replace("_", @"\_");
                    await _bot.SendTextMessageAsync(chatFinded.Id, txt, ParseMode.Markdown);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                await _bot.SendTextMessageAsync(chatFinded.Id, "Ваш запрос не удалось обработать.", ParseMode.Markdown);
            }
        }

        private async void UpdatePlayerUserid(Chat chatFinded, string command)
        {
            //command format is id;userid
            chatFinded.UpdateUseridMode = false;
            var playerinfo = command.Split(';');
            if (playerinfo.Length == 2)
            {
                var id = int.Parse(playerinfo[0]);
                var userid = int.Parse(playerinfo[1]);
                _db.UpdatePlayerUserid(id, userid);
                var player = _db.GetPlayerById(id);
                if (player == null)
                {
                    await _bot.SendTextMessageAsync(chatFinded.Id, $"Не удалось обновить игрока с id:{id}.");
                    return;
                }
                await _bot.SendTextMessageAsync(chatFinded.Id, $"Обновили {player.Name} {player.Surname} userid:{player.Userid}.");
            }
            else
            {
                await _bot.SendTextMessageAsync(chatFinded.Id, $"Неверный формат запроса: {command}");
            }
        }

        internal async void ContinueWaitingVoting(Chat chatFinded, int msgid, CallbackQuery e, bool isShowOnly)
        {
            var voting = chatFinded.WaitingVotings.FindLast(x => x.MessageId == msgid);
            if (voting == null) return;

            if (!isShowOnly)
            {
                var user = e.From;
                var player = _db.GetPlayerByUserid(user.Id);
                var vote = new Vote(user.Id, user.Username, player == null ? user.FirstName : player.Name,
                    player == null ? user.LastName : player.Surname, e.Data);
                var voteDupl = voting.V.FindLast(x => x.Userid == vote.Userid);
                if (voteDupl != null)
                {
                    if (voteDupl.Data == vote.Data) return;

                    voteDupl.Data = vote.Data;
                    _db.UpdateVoteData(msgid, vote.Userid, vote.Data);
                }
                else
                {
                    voting.V.Add(vote);
                    _db.AddVote(msgid, vote.Userid, vote.Username, vote.Name, vote.Surname, vote.Data);
                }
            }

            var yesCnt = voting.V.Count(x => x.Data == "Да");
            var detailedResult = $"\nДа – {yesCnt}\n";
            var votes = voting.V.FindAll(x => x.Data == "Да");
            foreach (var v in votes)
            {
                var username = string.IsNullOrEmpty(v.Username) ? "" : $"(@{v.Username})";
                detailedResult += $" {v.Name} {v.Surname} {username}\n";
            }
            if (votes.Count == 0) detailedResult += " -\n";

            var noCnt = voting.V.Count(x => x.Data == "Не");
            detailedResult += $"\nНе – {noCnt}\n";
            votes = voting.V.FindAll(x => x.Data == "Не");
            foreach (var v in voting.V.FindAll(x => x.Data == "Не"))
            {
                var username = string.IsNullOrEmpty(v.Username) ? "" : $"(@{v.Username})";
                detailedResult += $" {v.Name} {v.Surname} {username}\n";
            }
            if (votes.Count == 0) detailedResult += " -\n";

            var cnt = yesCnt + noCnt;

            var btnYes = new InlineKeyboardButton
            {
                Text = $"Да – {yesCnt}",
                CallbackData = "Да"
            };
            var btnNo = new InlineKeyboardButton
            {
                Text = $"Не – {noCnt}",
                CallbackData = "Не"
            };

            var keyboard = new InlineKeyboardMarkup(new[] { btnYes, btnNo });

            try
            {
                var answer = $"*{voting.Question}*\n{detailedResult}\n👥 {cnt} человек проголосовало.";
                answer = answer.Replace("_", @"\_"); //Escaping underline in telegram api when parse_mode = Markdown
                await _bot.EditMessageTextAsync(chatFinded.Id, msgid, answer, parseMode: ParseMode.Markdown, replyMarkup: keyboard);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private async void WrongCmd(Chat chatFinded)
        {
            chatFinded.ResetMode();
            await _bot.SendTextMessageAsync(chatFinded.Id, "Неверный запрос, напишите:\n/помощь", ParseMode.Default, false, false, 0);
        }

        private async void ExceptionOnCmd(Chat chatFinded, Exception ex)
        {
            chatFinded.ResetMode();
            Console.WriteLine(ex.Message);
            await _bot.SendTextMessageAsync(chatFinded.Id, "Ваш запрос не удалось обработать. Запрос отменён.");
        }

        private async void News(Chat chatFinded)
        {
            var events = File.Exists(Config.DbGamesInfoPath) ? File.ReadAllLines(Config.DbGamesInfoPath) : new string[0];
            var result = "";
            foreach (var even in events)
            {
                result += even.Replace('%', '\n');
                result += "\n\n";
            }
            if (result == "")
            {
                await _bot.SendTextMessageAsync(chatFinded.Id, "Нет новостей :(");
            }
            else
            {
                await _bot.SendTextMessageAsync(chatFinded.Id, result, parseMode: ParseMode.Markdown);
            }
        }

        private async void AddPlayer(Chat chatFinded, string argv)
        {
            //argv format is 1;Зверев;Алексей;Александрович;23.07.1986;Вратарь;Стена;0
            chatFinded.AddMode = false;
            var playerinfo = argv.Split(';');
            if (playerinfo.Length == 8)
            {
                var player = new Player(
                    number: int.Parse(playerinfo[0]), 
                    name: playerinfo[2].Trim(), 
                    surname: playerinfo[1].Trim(), 
                    userid: int.Parse(playerinfo[7]))
                {
                    Birthday = playerinfo[4],
                    SecondName = playerinfo[3],
                    Position = playerinfo[5],
                    Status = playerinfo[6]
                };

                _db.AddPlayer(player);
                await _bot.SendTextMessageAsync(chatFinded.Id, $"Попробовали добавить /{player.Number}.");
            }
            else
            {
                await _bot.SendTextMessageAsync(chatFinded.Id, $"Неверный формат запроса: {argv}");
            }
        }

        private async void RemovePlayer(Chat chatFinded, int id)
        {
            chatFinded.RemoveMode = false;
            _db.RemovePlayerById(id);
            await _bot.SendTextMessageAsync(chatFinded.Id, $"Попробовали удалить {id}.");
        }

        private async void AddVoting(Chat chatFinded, string command)
        {
            chatFinded.VoteMode = false;
            var btnYes = new InlineKeyboardButton
            {
                Text = "Да",
                CallbackData = "Да"
            };
            var btnNo = new InlineKeyboardButton
            {
                Text = "Не",
                CallbackData = "Не"
            };
            var btnShow = new InlineKeyboardButton
            {
                Text = "Показать результаты",
                CallbackData = "Show"
            };
            var keyboard = new InlineKeyboardMarkup(new[] { new [] { btnYes, btnNo }, new[] { btnShow } } );

            var msg = await _bot.SendTextMessageAsync(chatFinded.Id, $"{command}", replyMarkup: keyboard);
            var v = new List<Vote>();
            var voting = new WaitingVoting() {MessageId = msg.MessageId, V = v, Question = command};

            _db.AddVoting(voting);
            chatFinded.WaitingVotings.Add(voting);
        }

        private async void ShowPlayerByNubmer(Chat chatFinded, int playerNumber)
        {
            if (playerNumber < 0 || playerNumber > 100)
            {
                await _bot.SendTextMessageAsync(chatFinded.Id, "Неверный формат, введите корректный номер игрока от 0 до 100.");
                return;
            }

            try
            {
                var player = _db.GetPlayerByNumber(playerNumber);
                if (player == null)
                {
                    await _bot.SendTextMessageAsync(chatFinded.Id, $"Игрок под номером {playerNumber} не найден.");
                }
                else
                {
                    var photopath = Path.Combine(Config.DbPlayersPhotoDirPath, player.PhotoFile);

                    Console.WriteLine($"Send player:{player.Surname}");
                    if (File.Exists(photopath))
                    {
                        var photo = new Telegram.Bot.Types.InputFiles.InputOnlineFile(
                                        (new StreamReader(photopath)).BaseStream,
                                        player.Number + ".jpg");
                                                    
                        await _bot.SendPhotoAsync(chatFinded.Id, photo, player.Description, parseMode: ParseMode.Markdown);
                    }
                    else
                    {
                        Console.WriteLine($"Photo file {photopath} not found.");
                        await _bot.SendTextMessageAsync(chatFinded.Id, player.Description, parseMode: ParseMode.Markdown);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                await _bot.SendTextMessageAsync(chatFinded.Id, "Ваш запрос не удалось обработать.");
            }
        }

        private async void ShowPlayersByNameOrSurname(Chat chatFinded, string nameOrSurname)
        {
            try
            {
                var players = _db.GetPlayersByNameOrSurname(nameOrSurname);
                if (players.Count == 0)
                {
                    //иначе пользователь ввёл хуйню
                    WrongCmd(chatFinded);
                }
                else
                {
                    if(players.Count > 1)
                    {
                        await _bot.SendTextMessageAsync(chatFinded.Id, "По вашему запросу найдено несколько игроков, сейчас их покажу.");
                    }

                    foreach (var player in players)
                    {
                        var photopath = Path.Combine(Config.DbPlayersPhotoDirPath, player.PhotoFile);

                        Console.WriteLine($"Send player:{player.Surname}");
                        if (File.Exists(photopath))
                        {
                            var photo = new Telegram.Bot.Types.InputFiles.InputOnlineFile(
                                    (new StreamReader(photopath)).BaseStream,
                                    player.Number + ".jpg");

                            await _bot.SendPhotoAsync(chatFinded.Id, photo, player.Description, parseMode: ParseMode.Markdown);
                        }
                        else
                        {
                            Console.WriteLine($"Photo file {photopath} not found.");
                            await _bot.SendTextMessageAsync(chatFinded.Id, player.Description, parseMode: ParseMode.Markdown);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                await _bot.SendTextMessageAsync(chatFinded.Id, "Ваш запрос не удалось обработать.");
            }
        }

        private async void Game(Chat chatFinded)
        {
            try
            {
                var games = _db.GetEventsByType("Игра");
                if (games.Count == 0)
                {
                    await _bot.SendTextMessageAsync(chatFinded.Id, "Ближайших игр не найдено.");
                }
                else
                {
                    var exist = false;
                    foreach (var game in games)
                    {
                        if (DateTime.Now >= DateTime.Parse(game.Date)) continue;

                        exist = true;
                        var txt = $"*{game.Date} {game.Time}*\n*{game.Place}*\n{game.Details}\n{game.Result}";
                        await _bot.SendTextMessageAsync(chatFinded.Id, txt, ParseMode.Markdown);
                    }
                    if (!exist)
                    {
                        await _bot.SendTextMessageAsync(chatFinded.Id, "Ближайших игр не найдено.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                await _bot.SendTextMessageAsync(chatFinded.Id, "Ваш запрос не удалось обработать.");
            }
        }

        private async void Training(Chat chatFinded)
        {
            try
            {
                var trainings = _db.GetEventsByType("Треня");
                if (trainings.Count == 0)
                {
                    await _bot.SendTextMessageAsync(chatFinded.Id, "Трени не найдены.");
                }
                else
                {                   
                    foreach (var training in trainings)
                    {
                        var txt = $"*{training.Date} {training.Time}*\n*{training.Place}*\n{training.Address}\n{training.Details}";
                        await _bot.SendTextMessageAsync(chatFinded.Id, txt, ParseMode.Markdown);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                await _bot.SendTextMessageAsync(chatFinded.Id, "Ваш запрос не удалось обработать.", ParseMode.Markdown);
            }
        }

        private async void Help(Chat chatFinded)
        {
            var keyboard = new InlineKeyboardMarkup(
                new[]
                {
                    new[]
                    {
                        new InlineKeyboardButton()
                        {
                            Text = "трени",
                            CallbackData = "/" + "трени"
                        },
                        new InlineKeyboardButton()
                        {
                            Text = "игры",
                            CallbackData = "/" + "игры"
                        }
                    },
                    new[]
                    {
                        new InlineKeyboardButton()
                        {
                            Text = "новости",
                            CallbackData = "/" + "новости"
                        },
                        new InlineKeyboardButton()
                        {
                            Text = "помощь",
                            CallbackData = "/" + "помощь"
                        }
                    }
                });

            const string help = 
@"*Попроси бота в чате*:

*Поискать* игрока по
/номеру
/имени
/фамилии

*Показать*
/игры
/трени
/новости
/помощь

💥Удачи!💥";

            await _bot.SendTextMessageAsync(chatFinded.Id, help, ParseMode.Markdown, false, false, 0, keyboard);
        }

        public void TryToRestoreVotingFromDb(int messageId, Chat chat)
        {
            var voting = _db.GetVotingById(messageId);
            if (voting == null) return;

            chat.WaitingVotings.Add(voting);

            voting.V = _db.GetVotesByMessageId(messageId);
            Console.WriteLine("Voting restored from DB: " + voting.Question);
        }
    }
}
