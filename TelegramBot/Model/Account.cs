using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace TelegramBot.Model
{
    public class Account
    {
        public Account()
        {
            Id = "";
            Preference = "";
            ChatId = "";
            WalletAdresses = new List<string>();
            Subs = new List<string>();
        }

        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("walletAdresses")]
        public List<string> WalletAdresses { get; set; }
        [JsonPropertyName("preference")]
        public string Preference { get; set; }
        [JsonPropertyName("chatId")]
        public string ChatId { get; set; }
        [JsonPropertyName("subs")]
        public List<string> Subs { get; set; }
    }
}
