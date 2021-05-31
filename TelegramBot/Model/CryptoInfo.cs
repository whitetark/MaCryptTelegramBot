using System;
using System.Collections.Generic;
using System.Text;

namespace TelegramBot.Model
{
    public class CryptoInfo
    {
        public string symbol { get; set; }
        public string name { get; set; }
        public double price { get; set; }
        public double market_cap { get; set; }
        public double low_24h { get; set; }
        public double high_24h { get; set; }
        public string delta_1h { get; set; }
        public string delta_24h { get; set; }
        public List<Market> markets { get; set; }
    }
    public class Market
    {
        public string symbol { get; set; }
        public string price { get; set; }
        public List<Exchanx> exchanges { get; set; }
    }
    public class Exchanx
    {
        public string name { get; set; }
        public string price { get; set; }
    }

}
