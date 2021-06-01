using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.Model;
using static TelegramBot.Model.WalletInfo;

namespace TelegramBot
{
    class TelegramBotProcess
    {
        private string _token;
        Telegram.Bot.TelegramBotClient _client;
        MaCryptClient _maCryptClient = new MaCryptClient();
        Account account;
        Account result;
        string recentCallBack;
        string recentText;
        Timer timer;
        public TelegramBotProcess(string token)
        {
            _token = token;
        }
        internal void GetUpdates()
        {
            _client = new Telegram.Bot.TelegramBotClient(_token);
            var me = _client.GetMeAsync().Result;
            if (me != null && !string.IsNullOrEmpty(me.Username))
            {
                int offset = 0;
                while (true)
                {
                    try
                    {
                        var updates = _client.GetUpdatesAsync(offset).Result;
                        if (updates != null && updates.Count() > 0)
                        {
                            foreach (var update in updates)
                            {
                                processUpdate(update);
                                offset = update.Id + 1;
                            }
                        }
                        if (account != null)
                        {
                            if (account.Subs.Count != 0)
                            {
                                if (timer == null)
                                {
                                    int num = 0;
                                    TimerCallback tm = new TimerCallback(SubsProcess);
                                    timer = new Timer(tm, num, 0, 1800000);
                                }
                            }
                        }
                    }
                    catch (Exception ex) { Console.WriteLine(ex.Message); }
                    Thread.Sleep(1000);
                }
            }
        }
        private async void processUpdate(Telegram.Bot.Types.Update update)
        {
            switch (update.Type)
            {
                case Telegram.Bot.Types.Enums.UpdateType.Message:
                    var text = update.Message.Text;
                    result = await _maCryptClient.GetAccountInfo(Convert.ToString(update.Message.Chat.Id));
                    if (result == null)
                    {
                        account = new Account();
                        switch (text)
                        {
                            case "/start":
                                account.ChatId = Convert.ToString(update.Message.Chat.Id);
                                _ = await _client.SendTextMessageAsync(update.Message.Chat.Id, "Добро пожаловать в MaCrypt! Что же это такое?\nMaCrypt - это крипто-ассистент, который немного упростит жизнь всем любителям криптовалюты\n\nСреди функций можно найти:\n\n-Данные о вашем кошельке\n-Данные о последних транзакциях\n-Актуальный топ-криптовалюты\n-Актуальная информация про криптовалюту\n-Система подписок\n\nДля использования бота, вам необходимо создать аккаунт, выберите снизу курс, за которым вам будут показаны ценники:", replyMarkup: GetPreferenceButton());
                                _maCryptClient.CreateAccount(account);
                                break;
                            default:
                                await _client.SendTextMessageAsync(update.Message.Chat.Id, "Вам необходимо создать аккаунт! Напишите /start");
                                break;
                        }
                    }
                    else
                    {
                        account = result;
                        switch (text)
                        {
                            case "/start":
                                await _client.SendTextMessageAsync(update.Message.Chat.Id, "У вас уже есть аккаунт", replyMarkup: GetButtons());
                                break;
                            case Constants.MainMenu.BUTTON_MAINCRYPTO:
                                await _client.SendTextMessageAsync(update.Message.Chat.Id, "Что вы хотите среди криптовалюты?: ", replyMarkup: GetMainCryptoButton());
                                break;
                            case Constants.MainMenu.BUTTON_SUBS:
                                await _client.SendTextMessageAsync(update.Message.Chat.Id, "Ваши подписки: ", replyMarkup: GetSubsButton());
                                break;
                            case Constants.MainMenu.BUTTON_WALLETS:
                                await _client.SendTextMessageAsync(update.Message.Chat.Id, "Ваши кошельки: ", replyMarkup: GetWalletsButton());
                                break;
                            case Constants.MainMenu.BUTTON_SETTINGS:
                                await _client.SendTextMessageAsync(update.Message.Chat.Id, "Ваши настройки: ", replyMarkup: GetSettingsButton());
                                break;
                            default:
                                switch (recentCallBack)
                                {
                                    case "addwallet":
                                        try
                                        {
                                            account.WalletAdresses.Add(update.Message.Text);
                                            _maCryptClient.UpdateAccount(account);
                                            _ = await _client.SendTextMessageAsync(account.ChatId, "Вы успешно добавили кошелёк", replyMarkup: GetButtons());
                                        }
                                        catch
                                        {
                                            _ = await _client.SendTextMessageAsync(account.ChatId, "Введённый адрес не правильный", replyMarkup: GetButtons());
                                        }
                                        recentCallBack = "";
                                        break;
                                    case "addsub":
                                        try
                                        {
                                            _ = await _maCryptClient.GetCryptoInfo(update.Message.Text, account.Preference);
                                            account.Subs.Add(update.Message.Text);
                                            _maCryptClient.UpdateAccount(account);
                                            _ = await _client.SendTextMessageAsync(account.ChatId, "Вы успешно создали подписку", replyMarkup: GetButtons());
                                        }
                                        catch
                                        {
                                            _ = await _client.SendTextMessageAsync(account.ChatId, "Введённый символ не правильный", replyMarkup: GetButtons());
                                        }
                                        recentCallBack = "";
                                        break;
                                    case "crptinf":
                                        try
                                        {
                                            var response = await _maCryptClient.GetCryptoInfo(update.Message.Text, result.Preference);
                                            string halftext =  $"[Общая информация]\nСимвол: {response.symbol},\nПолное имя: {response.name},\nЦена: {response.price.ToString("0.00000", CultureInfo.InvariantCulture)},\nКапитализация: {response.market_cap.ToString("#,#.00", CultureInfo.InvariantCulture)},\nМинимальная цена за сутки: {response.low_24h.ToString("0.00000", CultureInfo.InvariantCulture)},\nМаксимальная цена за cутки: {response.high_24h.ToString("0.00000", CultureInfo.InvariantCulture)},\nИзменение за час: {response.delta_1h},\nИзменение за сутки: {response.delta_24h},\n\n";
                                            string intro = $"[Курс: {result.Preference}]\n\n";
                                            string exchantext = "[Цена На Популярных Обменниках]\n";
                                            foreach (Exchanx exchange in response.markets[0].exchanges) {
                                                exchantext += $"{exchange.name} - {exchange.price}\n";
                                            }
                                            string maintext = intro + halftext + exchantext;
                                            _ = await _client.SendTextMessageAsync(account.ChatId, maintext, replyMarkup: GetButtons());
                                        } 
                                        catch
                                        {
                                            try
                                            {
                                                var responsetemp  = await _maCryptClient.GetCryptoInfo("BTC", "USD");
                                                if(responsetemp != null)
                                                {
                                                    _ = await _client.SendTextMessageAsync(account.ChatId, "Вы ввели неправильный символ", replyMarkup: GetButtons());
                                                }
                                            }
                                            catch
                                            {
                                                _ = await _client.SendTextMessageAsync(account.ChatId, "Простите, в данный момент сервис Coinlib не отвечает,\nПопробуйте позже", replyMarkup: GetButtons());
                                            }
                                        }
                                        recentCallBack = "";
                                        break;
                                }
                                break;
                        }
                    }
                    break;
                case Telegram.Bot.Types.Enums.UpdateType.CallbackQuery:
                    result = await _maCryptClient.GetAccountInfo(Convert.ToString(update.CallbackQuery.From.Id));
                    if (result != null)
                    {
                        account = result;
                        switch (update.CallbackQuery.Data)
                        {
                            case "uah":
                                account.Preference = "UAH";
                                _maCryptClient.UpdateAccount(account);
                                _ = await _client.SendTextMessageAsync(account.ChatId, "Вы выбрали курс UAH", replyMarkup: GetButtons());
                                break;
                            case "usd":
                                account.Preference = "USD";
                                _maCryptClient.UpdateAccount(account);
                                _ = await _client.SendTextMessageAsync(account.ChatId, "Вы выбрали курс USD", replyMarkup: GetButtons());
                                break;
                            case "rub":
                                account.Preference = "RUB";
                                _maCryptClient.UpdateAccount(account);
                                _ = await _client.SendTextMessageAsync(account.ChatId, "Вы выбрали курс RUB", replyMarkup: GetButtons());
                                break;
                            case "eur":
                                account.Preference = "EUR";
                                _maCryptClient.UpdateAccount(account);
                                _ = await _client.SendTextMessageAsync(account.ChatId, "Вы выбрали курс EUR", replyMarkup: GetButtons());
                                break;
                            case "crptinf":
                                _ = await _client.SendTextMessageAsync(account.ChatId, "Введите символ желаемой криптовалюты:\nНапример: BTC, ETH, LINK");
                                recentCallBack = "crptinf";
                                break;
                            case "crptlst":
                                _ = await _client.SendTextMessageAsync(account.ChatId, "В каком порядке вы хотите увидеть список?", replyMarkup: GetOrderButton());
                                break;
                            case "deleteacc":
                                _ = await _client.SendTextMessageAsync(account.ChatId, "Вы точно хотите удалить аккаунт?", replyMarkup: GetConfirmationButtons());
                                break;
                            case "deleteyes":
                                _maCryptClient.DeleteAccount(account.ChatId);
                                _ = await _client.SendTextMessageAsync(account.ChatId, "Ваш аккаунт удалён\nНадеюсь, ещё увидимся");
                                break;
                            case "changepref":
                                _ = await _client.SendTextMessageAsync(account.ChatId, $"Ваш нынешний курс: {account.Preference}\nКакой курс вы хотите выбрать?", replyMarkup: GetPreferenceButton());
                                _maCryptClient.UpdateAccount(account);
                                break;
                            case "back":
                                break;
                            case "addwallet":
                                _ = await _client.SendTextMessageAsync(account.ChatId, "Введите адрес вашего Blockchain кошелька:");
                                recentCallBack = "addwallet";
                                break;
                            case "addsub":
                                _ = await _client.SendTextMessageAsync(account.ChatId, "Введите символ криптовалюты для подписки:");
                                recentCallBack = "addsub";
                                break;
                            case "wallet1":
                                var maintext1 = await WalletShowProcess(0);
                                _ = await _client.SendTextMessageAsync(account.ChatId, maintext1, replyMarkup:GetDeleteWallet1Button());
                                break;
                            case "wallet2":
                                var maintext11 = await WalletShowProcess(1);
                                _ = await _client.SendTextMessageAsync(account.ChatId, maintext11, replyMarkup: GetDeleteWallet2Button());
                                break;
                            case "wallet3":
                                var maintext12 = await WalletShowProcess(1);
                                _ = await _client.SendTextMessageAsync(account.ChatId, maintext12, replyMarkup: GetDeleteWallet3Button());
                                break;
                            case "wallet1delete":
                                WalletDeleteProcess(0);
                                break;
                            case "wallet2delete":
                                WalletDeleteProcess(1);
                                break;
                            case "wallet3delete":
                                WalletDeleteProcess(2);
                                break;
                            case "sub1":
                                var maintextsub = SubsShowProcess(0);
                                _ = await _client.SendTextMessageAsync(account.ChatId, maintextsub, replyMarkup: GetDeleteSub1Button());
                                break;
                            case "sub2":
                                var maintextsub1 = SubsShowProcess(1);
                                _ = await _client.SendTextMessageAsync(account.ChatId, maintextsub1, replyMarkup: GetDeleteSub2Button());
                                break;
                            case "sub3":
                                var maintextsub2 = SubsShowProcess(2);
                                _ = await _client.SendTextMessageAsync(account.ChatId, maintextsub2, replyMarkup: GetDeleteSub3Button());
                                break;
                            case "sub1delete":
                                SubsDeleteProcess(0);
                                break;
                            case "sub2delete":
                                SubsDeleteProcess(1);
                                break;
                            case "sub3delete":
                                SubsDeleteProcess(2);
                                break;
                            case "rankasc":
                                try
                                {
                                    var maintext = await OrderShowProcess("rank_asc");
                                    _ = await _client.SendTextMessageAsync(account.ChatId, maintext, replyMarkup: GetButtons());
                                } catch
                                {
                                    _ = await _client.SendTextMessageAsync(account.ChatId, "Простите, в данный момент сервис Coinlib не отвечает,\nПопробуйте позже", replyMarkup: GetButtons());
                                }
                                break;
                            case "rankdesc":
                                try
                                {
                                    var maintext = await OrderShowProcess("rank_desc");
                                    _ = await _client.SendTextMessageAsync(account.ChatId, maintext, replyMarkup: GetButtons());
                                }
                                catch
                                {
                                    _ = await _client.SendTextMessageAsync(account.ChatId, "Простите, в данный момент сервис Coinlib не отвечает,\nПопробуйте позже", replyMarkup: GetButtons());
                                }
                                break;
                            case "volumedesc":
                                try
                                {
                                    var maintext = await OrderShowProcess("volume_desc");
                                    _ = await _client.SendTextMessageAsync(account.ChatId, maintext, replyMarkup: GetButtons());
                                }
                                catch
                                {
                                    _ = await _client.SendTextMessageAsync(account.ChatId, "Простите, в данный момент сервис Coinlib не отвечает,\nПопробуйте позже", replyMarkup: GetButtons());
                                }
                                break;
                            case "priceasc":
                                try
                                {
                                    var maintext = await OrderShowProcess("price_asc");
                                    _ = await _client.SendTextMessageAsync(account.ChatId, maintext, replyMarkup: GetButtons());
                                }
                                catch
                                {
                                    _ = await _client.SendTextMessageAsync(account.ChatId, "Простите, в данный момент сервис Coinlib не отвечает,\nПопробуйте позже", replyMarkup: GetButtons());
                                }
                                break;
                            case "pricedesc":
                                try
                                {
                                    var maintext = await OrderShowProcess("price_desc");
                                    _ = await _client.SendTextMessageAsync(account.ChatId, maintext, replyMarkup: GetButtons());
                                }
                                catch
                                {
                                    _ = await _client.SendTextMessageAsync(account.ChatId, "Простите, в данный момент сервис Coinlib не отвечает,\nПопробуйте позже", replyMarkup: GetButtons());
                                }
                                break;
                            case "volumeasc":
                                try
                                {
                                    var maintext = await OrderShowProcess("volume_asc");
                                    _ = await _client.SendTextMessageAsync(account.ChatId, maintext, replyMarkup: GetButtons());
                                }
                                catch
                                {
                                    _ = await _client.SendTextMessageAsync(account.ChatId, "Простите, в данный момент сервис Coinlib не отвечает,\nПопробуйте позже", replyMarkup: GetButtons());
                                }
                                break;
                        }
                    }
                    break;
                default:
                    Console.WriteLine(update.Type + " Not ipmlemented!");
                    break;
            }
        }

        public async Task<string> WalletShowProcess(int id)
        {
            var response1 = await _maCryptClient.GetWalletInfo(result.WalletAdresses[id]);
            string halftext = $"[Общая информация]\nАдресс: {response1.AddressOfOwner},\nКоличество транзакций: {response1.NumOfTransactions},\nВсего получено: {response1.TotalReceived},\nВсего отправлено: {response1.TotalSent},\nНынешний баланс: {response1.CurrentBalance},\nПодробная информация: www.blockchain.com/btc/address/{response1.AddressOfOwner}\n\n";
            string txstext = "[Последние транзакции]\n\n";
            foreach (Transaction transaction in response1.Transactions)
            {
                txstext += $"Результат транзакции: {transaction.ResultOfTransaction},\nКомиссия: {transaction.Fee},\nИтоговый баланс: {transaction.BalanceAfter},\nПодробная информация: www.blockchain.com/btc/tx/{transaction.Hash} \n\n";
            }
            string maintext = halftext + txstext;
            return maintext;
        }
        public async Task<string> OrderShowProcess(string order)
        {
            var response = await _maCryptClient.GetCryptoList(order, result.Preference);
            string[] vs = new string[10];
            int i = 0;
            foreach (Coin coin in response.coins)
            {
                vs[i] = $"Символ: {coin.symbol},\nИмя крипты: {coin.name},\nРанг в топе: {coin.rank},\nЦена: {coin.price.ToString("0.00000", CultureInfo.InvariantCulture)},\nКапитализация: {coin.market_cap.ToString("#,#.00", CultureInfo.InvariantCulture)},\nОбъём за 24 часа: {coin.volume_24h.ToString("#,#.00", CultureInfo.InvariantCulture)},\nИзменение за сутки: {coin.delta_24h}\n\n";
                i++;
            }
            string maintext = $"[Курс: {result.Preference},\nПорядок: {order}]\n\n";
            foreach (string text1 in vs)
            {
                maintext += text1;
            }
            return maintext;
        }
        public string SubsShowProcess(int id)
        {
            string maintextsub = $"Вы подписаны на следующую криптовалюту: {account.Subs[id]}\n\nПодписка даст вам возможность узнать каждый час если криптовалюта изменилась больше чем на 0.6%. В будущем возможно появление введения рубежов";
            return maintextsub;
        }
        public async void WalletDeleteProcess(int id)
        {
            account.WalletAdresses.RemoveAt(id);
            _maCryptClient.UpdateAccount(account);
            _ = await _client.SendTextMessageAsync(account.ChatId, "Кошелёк успешно удалён", replyMarkup: GetButtons());
        }
        public async void SubsDeleteProcess(int id)
        {
            account.Subs.RemoveAt(id);
            _maCryptClient.UpdateAccount(account);
            _ = await _client.SendTextMessageAsync(account.ChatId, "Подписка успешно удалена", replyMarkup: GetButtons());
        }
        public async void SubsProcess(object state)
        {
            try
            {
                foreach (string s in account.Subs)
                {
                    var response = await _maCryptClient.GetCryptoInfo(s, account.Preference);
                    if (response.delta_1h >= 1.1)
                    {
                        _ = await _client.SendTextMessageAsync(account.ChatId, $"Alert! Похоже {s} за час вырос на {response.delta_1h}%");
                        Console.ReadLine();
                    }
                    else if (response.delta_1h <= -1.1)
                    {
                        _ = await _client.SendTextMessageAsync(account.ChatId, $"Alert! Похоже {s} за час упал на {response.delta_1h}%");
                        Console.ReadLine();
                    }
                    recentText = "OkCoinLib";
                }
            } catch (Exception ex)
            {
                if (recentText != "ExCoinlib")
                {
                    _ = await _client.SendTextMessageAsync(account.ChatId, "[Ошибка подписки]\nПростите, в данный момент сервис Coinlib не отвечает", replyMarkup: GetButtons());
                    recentText = "ExCoinlib";
                }
                Console.WriteLine("There is your mistake:" + ex);
            }
        }

        private IReplyMarkup GetDeleteSub1Button()
        {
            List<InlineKeyboardButton> delete = new List<InlineKeyboardButton>();
            InlineKeyboardButton deleteb = new InlineKeyboardButton();
            deleteb.Text = "Удалить подписку";
            deleteb.CallbackData = "sub1delete";
            delete.Add(deleteb);

            InlineKeyboardMarkup inline = new InlineKeyboardMarkup(delete);

            return inline;
        }
        private IReplyMarkup GetDeleteSub2Button()
        {
            List<InlineKeyboardButton> delete = new List<InlineKeyboardButton>();
            InlineKeyboardButton deleteb = new InlineKeyboardButton();
            deleteb.Text = "Удалить подписку";
            deleteb.CallbackData = "sub2delete";
            delete.Add(deleteb);

            InlineKeyboardMarkup inline = new InlineKeyboardMarkup(delete);

            return inline;
        }
        private IReplyMarkup GetDeleteSub3Button()
        {
            List<InlineKeyboardButton> delete = new List<InlineKeyboardButton>();
            InlineKeyboardButton deleteb = new InlineKeyboardButton();
            deleteb.Text = "Удалить подписку";
            deleteb.CallbackData = "sub3delete";
            delete.Add(deleteb);

            InlineKeyboardMarkup inline = new InlineKeyboardMarkup(delete);

            return inline;
        }
        private IReplyMarkup GetDeleteWallet1Button()
        {
            List<InlineKeyboardButton> delete = new List<InlineKeyboardButton>();
            InlineKeyboardButton deleteb = new InlineKeyboardButton();
            deleteb.Text = "Удалить кошелёк";
            deleteb.CallbackData = "wallet1delete";
            delete.Add(deleteb);

            InlineKeyboardMarkup inline = new InlineKeyboardMarkup(delete);

            return inline;
        }
        private IReplyMarkup GetDeleteWallet2Button()
        {
            List<InlineKeyboardButton> delete = new List<InlineKeyboardButton>();
            InlineKeyboardButton deleteb = new InlineKeyboardButton();
            deleteb.Text = "Удалить кошелёк";
            deleteb.CallbackData = "wallet2delete";
            delete.Add(deleteb);

            InlineKeyboardMarkup inline = new InlineKeyboardMarkup(delete);

            return inline;
        }
        private IReplyMarkup GetDeleteWallet3Button()
        {
            List<InlineKeyboardButton> delete = new List<InlineKeyboardButton>();
            InlineKeyboardButton deleteb = new InlineKeyboardButton();
            deleteb.Text = "Удалить кошелёк";
            deleteb.CallbackData = "wallet3delete";
            delete.Add(deleteb);

            InlineKeyboardMarkup inline = new InlineKeyboardMarkup(delete);

            return inline;
        }
        private IReplyMarkup GetOrderButton()
        {
            List<InlineKeyboardButton> rankRow = new List<InlineKeyboardButton>();
            List<InlineKeyboardButton> volumeRow = new List<InlineKeyboardButton>();
            List<InlineKeyboardButton> priceRow = new List<InlineKeyboardButton>();
            List<List<InlineKeyboardButton>> rowList = new List<List<InlineKeyboardButton>>();

            InlineKeyboardButton rankAsc = new InlineKeyboardButton();
            InlineKeyboardButton rankDesc = new InlineKeyboardButton();
            InlineKeyboardButton volumeAsc = new InlineKeyboardButton();
            InlineKeyboardButton volumeDesc = new InlineKeyboardButton();
            InlineKeyboardButton priceAsc = new InlineKeyboardButton();
            InlineKeyboardButton priceDesc = new InlineKeyboardButton();

            rankAsc.Text = "Убывание По Рангу";
            rankDesc.Text = "Возрастание По Рангу";
            volumeAsc.Text = "Убывание По Объёму";
            volumeDesc.Text = "Возрастание По Объёму";
            priceAsc.Text = "Убывание По Цене";
            priceDesc.Text = "Возрастание По Цене";

            rankAsc.CallbackData = "rankasc";
            rankDesc.CallbackData = "rankdesc";
            volumeAsc.CallbackData = "volumeasc";
            volumeDesc.CallbackData = "volumedesc";
            priceAsc.CallbackData = "priceasc";
            priceDesc.CallbackData = "pricedesc";

            rankRow.Add(rankAsc);
            rankRow.Add(rankDesc);
            volumeRow.Add(volumeAsc);
            volumeRow.Add(volumeDesc);
            priceRow.Add(priceAsc);
            priceRow.Add(priceDesc);

            rowList.Add(rankRow);
            rowList.Add(volumeRow);
            rowList.Add(priceRow);

            InlineKeyboardMarkup inline = new InlineKeyboardMarkup(rowList);

            return inline;
        }
        private IReplyMarkup GetConfirmationButtons()
        {
            List<InlineKeyboardButton> buttons = new List<InlineKeyboardButton>();
            InlineKeyboardButton yesButton = new InlineKeyboardButton();

            yesButton.Text = "Да";

            yesButton.CallbackData = "deleteyes";

            buttons.Add(yesButton);

            InlineKeyboardMarkup inline = new InlineKeyboardMarkup(buttons);

            return inline;
        }
        private IReplyMarkup GetPreferenceButton()
        {
            List<InlineKeyboardButton> preferences1 = new List<InlineKeyboardButton>();
            List<InlineKeyboardButton> preferences2 = new List<InlineKeyboardButton>();
            List<List<InlineKeyboardButton>> rowList = new List<List<InlineKeyboardButton>>();

            InlineKeyboardButton usdPref = new InlineKeyboardButton();
            InlineKeyboardButton rubPref = new InlineKeyboardButton();
            InlineKeyboardButton uahPref = new InlineKeyboardButton();
            InlineKeyboardButton eurPref = new InlineKeyboardButton();

            usdPref.Text = "USD";
            rubPref.Text = "RUB";
            uahPref.Text = "UAH";
            eurPref.Text = "EUR";

            usdPref.CallbackData = "usd";
            rubPref.CallbackData = "rub";
            uahPref.CallbackData = "uah";
            eurPref.CallbackData = "eur";

            preferences1.Add(uahPref);
            preferences1.Add(rubPref);
            preferences2.Add(usdPref);
            preferences2.Add(eurPref);

            rowList.Add(preferences2);
            rowList.Add(preferences1);


            InlineKeyboardMarkup inline = new InlineKeyboardMarkup(rowList);

            return inline;
        }
        private IReplyMarkup GetMainCryptoButton()
        {
            List<List<InlineKeyboardButton>> rowList = new List<List<InlineKeyboardButton>>();
            List<InlineKeyboardButton> button1 = new List<InlineKeyboardButton>();
            List<InlineKeyboardButton> button2 = new List<InlineKeyboardButton>();

            InlineKeyboardButton cryptoInfo = new InlineKeyboardButton();
            InlineKeyboardButton cryptoList = new InlineKeyboardButton();

            cryptoInfo.Text = "Информация Про Криптовалюту";
            cryptoList.Text = "Топ Криптовалюты";

            cryptoInfo.CallbackData = "crptinf";
            cryptoList.CallbackData = "crptlst";

            button1.Add(cryptoList);
            button2.Add(cryptoInfo);

            rowList.Add(button1);
            rowList.Add(button2);
            
            InlineKeyboardMarkup inline = new InlineKeyboardMarkup(rowList);

            return inline;
        }
        private IReplyMarkup GetSubsButton()
        {
            List<List<InlineKeyboardButton>> rowList = new List<List<InlineKeyboardButton>>();
            List<InlineKeyboardButton> sub1 = new List<InlineKeyboardButton>();
            List<InlineKeyboardButton> sub2 = new List<InlineKeyboardButton>();
            List<InlineKeyboardButton> sub3 = new List<InlineKeyboardButton>();

            if (result.Subs.Count == 0)
            {
                InlineKeyboardButton addSub = new InlineKeyboardButton();
                addSub.Text = "Создать подписку";
                addSub.CallbackData = "addsub";
                sub1.Add(addSub);
                rowList.Add(sub1);
            }
            else
            {
                InlineKeyboardButton yourSub1 = new InlineKeyboardButton();
                yourSub1.Text = account.Subs[0];
                yourSub1.CallbackData = "sub1";
                sub1.Add(yourSub1);
                rowList.Add(sub1);
                if (account.Subs.Count == 1)
                {
                    InlineKeyboardButton addSub = new InlineKeyboardButton();
                    addSub.Text = "Создать подписку";
                    addSub.CallbackData = "addsub";
                    sub2.Add(addSub);
                    rowList.Add(sub2);
                }
                else
                {
                    InlineKeyboardButton yourSub2 = new InlineKeyboardButton();
                    yourSub2.Text = account.Subs[1];
                    yourSub2.CallbackData = "sub2";
                    sub2.Add(yourSub2);
                    rowList.Add(sub2);
                    if (account.Subs.Count == 2)
                    {
                        InlineKeyboardButton addSub = new InlineKeyboardButton();
                        addSub.Text = "Создать подписку";
                        addSub.CallbackData = "addsub";
                        sub3.Add(addSub);
                        rowList.Add(sub3);
                    }
                    else
                    {
                        InlineKeyboardButton yourSub3 = new InlineKeyboardButton();
                        yourSub3.Text = account.Subs[2];
                        yourSub3.CallbackData = "sub3";
                        sub3.Add(yourSub3);
                        rowList.Add(sub3);
                    }
                }
            }
            InlineKeyboardMarkup inline = new InlineKeyboardMarkup(rowList);
            return inline;
        }
        private IReplyMarkup GetSettingsButton()
        {
            List<List<InlineKeyboardButton>> rowList = new List<List<InlineKeyboardButton>>();
            List<InlineKeyboardButton> buttons1 = new List<InlineKeyboardButton>();
            List<InlineKeyboardButton> buttons2 = new List<InlineKeyboardButton>();
            InlineKeyboardButton deleteAccount = new InlineKeyboardButton();
            InlineKeyboardButton changePref = new InlineKeyboardButton();

            deleteAccount.Text = "Удалить аккаунт";
            changePref.Text = "Изменить курс";

            deleteAccount.CallbackData = "deleteacc";
            changePref.CallbackData = "changepref";

            buttons1.Add(deleteAccount);
            buttons2.Add(changePref);

            rowList.Add(buttons1);
            rowList.Add(buttons2);

            InlineKeyboardMarkup inline = new InlineKeyboardMarkup(rowList);

            return inline;
        }
        private IReplyMarkup GetWalletsButton()
        {
            List<List<InlineKeyboardButton>> rowList = new List<List<InlineKeyboardButton>>();
            List<InlineKeyboardButton> wallet1 = new List<InlineKeyboardButton>();
            List<InlineKeyboardButton> wallet2 = new List<InlineKeyboardButton>();
            List<InlineKeyboardButton> wallet3 = new List<InlineKeyboardButton>();

            if (result.WalletAdresses.Count == 0)
            {
                InlineKeyboardButton addWallet = new InlineKeyboardButton();
                addWallet.Text = "Добавить кошелёк";
                addWallet.CallbackData = "addwallet";
                wallet1.Add(addWallet);
                rowList.Add(wallet1);
            }
            else
            {
                InlineKeyboardButton yourWallet1 = new InlineKeyboardButton();
                yourWallet1.Text = account.WalletAdresses[0];
                yourWallet1.CallbackData = "wallet1";
                wallet1.Add(yourWallet1);
                rowList.Add(wallet1);
                if (account.WalletAdresses.Count == 1)
                {
                    InlineKeyboardButton addWallet = new InlineKeyboardButton();
                    addWallet.Text = "Добавить кошелёк";
                    addWallet.CallbackData = "addwallet";
                    wallet2.Add(addWallet);
                    rowList.Add(wallet2);
                }
                else
                {
                    InlineKeyboardButton yourWallet2 = new InlineKeyboardButton();
                    yourWallet2.Text = account.WalletAdresses[1];
                    yourWallet2.CallbackData = "wallet1";
                    wallet2.Add(yourWallet2);
                    rowList.Add(wallet2);
                    if (account.WalletAdresses.Count == 2)
                    {
                        InlineKeyboardButton addWallet = new InlineKeyboardButton();
                        addWallet.Text = "Добавить кошелёк";
                        addWallet.CallbackData = "addwallet";
                        wallet3.Add(addWallet);
                        rowList.Add(wallet3);
                    }
                    else
                    {
                        InlineKeyboardButton yourWallet3 = new InlineKeyboardButton();
                        yourWallet3.Text = account.WalletAdresses[2];
                        yourWallet3.CallbackData = "wallet1";
                        wallet3.Add(yourWallet3);
                        rowList.Add(wallet3);
                    }
                }
            }
            InlineKeyboardMarkup inline = new InlineKeyboardMarkup(rowList);

            return inline;
        }
        private IReplyMarkup GetButtons()
        {
            return new ReplyKeyboardMarkup
            {
                Keyboard = new List<List<KeyboardButton>>
                {
                    new List<KeyboardButton> { new KeyboardButton { Text = Constants.MainMenu.BUTTON_MAINCRYPTO }, new KeyboardButton { Text = Constants.MainMenu.BUTTON_WALLETS } },
                    new List<KeyboardButton> { new KeyboardButton { Text = Constants.MainMenu.BUTTON_SUBS }, new KeyboardButton { Text = Constants.MainMenu.BUTTON_SETTINGS } }
                },

                ResizeKeyboard = true,

            };
        }
    }
}
