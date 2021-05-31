using System;
using System.Collections.Generic;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot;

namespace Awesome
{
    class Program
    { 
        static void Main()
        {
            try
            {
                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

                TelegramBotProcess hlp = new TelegramBotProcess(token: "1789190445:AAFRm6LzhvHXxw6YgEoAMwUX732nJOowdDo");
                hlp.GetUpdates();
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
            
        }
    } 
}