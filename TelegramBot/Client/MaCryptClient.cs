using Newtonsoft.Json;
using RestSharp;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TelegramBot.Model;

namespace TelegramBot
{
    public class MaCryptClient
    {
        private HttpClient _client;
        private static string _address;
        public MaCryptClient()
        {
            _address = "https://localhost:5001";

            _client = new HttpClient();
            _client.BaseAddress = new Uri(_address);
        }

        public async Task<Account> GetAccountInfo(string chatId)
        {
            var response = await _client.GetAsync($"/crypto/account/{chatId}");
            response.EnsureSuccessStatusCode();

            var content = response.Content.ReadAsStringAsync().Result;

            var result = JsonConvert.DeserializeObject<Account>(content);

            return result;
        }

        public async void UpdateAccount(Account account)
        {
            var newAccountSerialized = System.Text.Json.JsonSerializer.Serialize(account);
            var stringContent = new StringContent(newAccountSerialized, Encoding.UTF8, "application/json");

            _ = await _client.PutAsync("/crypto/update", stringContent);
        }

        public async void DeleteAccount(string chatId)
        {
            _ = await _client.DeleteAsync($"/crypto/delete/{chatId}");
        }

        public async void CreateAccount(Account account)
        {
            var newAccountSerialized = System.Text.Json.JsonSerializer.Serialize(account);
            var stringContent = new StringContent(newAccountSerialized, Encoding.UTF8, "application/json");

            _ = await _client.PostAsync("/crypto/create", stringContent);
        }

        public async Task<CryptoInfo> GetCryptoInfo(string symbol, string preference)
        {
            var response = await _client.GetAsync($"/Crypto/info?Symbol={symbol}&Pref={preference}");
            response.EnsureSuccessStatusCode();

            var content = response.Content.ReadAsStringAsync().Result;

            var result = JsonConvert.DeserializeObject<CryptoInfo>(content);

            return result;
        }

        public async Task<CryptoList> GetCryptoList(string order, string preference)
        {
            var response = await _client.GetAsync($"/Crypto/list?Pref={preference}&Order={order}");
            response.EnsureSuccessStatusCode();

            var content = response.Content.ReadAsStringAsync().Result;

            var result = JsonConvert.DeserializeObject<CryptoList>(content);

            return result;
        }

        public async Task<WalletInfo> GetWalletInfo(string adress)
        {
            var response = await _client.GetAsync($"/crypto/wallet?adress={adress}");
            response.EnsureSuccessStatusCode();

            var content = response.Content.ReadAsStringAsync().Result;

            var result = JsonConvert.DeserializeObject<WalletInfo>(content);

            return result;
        }
    }
}
