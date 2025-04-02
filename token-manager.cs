using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SiparisYonetimi
{
    public class TokenManager
    {
        private static readonly object _lockObject = new object();
        private static TokenManager _instance;
        private readonly HttpClient _httpClient;
        private readonly string _tokenUrl;
        private readonly string _clientId;
        private readonly string _clientSecret;

        private string _accessToken;
        private string _tokenType;
        private DateTime _expiryTime;
        private int _requestCount;
        private DateTime _requestCountResetTime;
        private readonly int _maxRequestsPerHour = 5;

        private TokenManager(string tokenUrl, string clientId, string clientSecret)
        {
            _httpClient = new HttpClient();
            _tokenUrl = tokenUrl;
            _clientId = clientId;
            _clientSecret = clientSecret;
            _requestCount = 0;
            _requestCountResetTime = DateTime.UtcNow.AddHours(1);
        }

        public static TokenManager Instance(string tokenUrl, string clientId, string clientSecret)
        {
            if (_instance == null)
            {
                lock (_lockObject)
                {
                    if (_instance == null)
                    {
                        _instance = new TokenManager(tokenUrl, clientId, clientSecret);
                    }
                }
            }
            return _instance;
        }

        public async Task<string> GetTokenAsync()
        {
            lock (_lockObject)
            {
                // Saatlik istek sınırını kontrol et
                if (DateTime.UtcNow > _requestCountResetTime)
                {
                    _requestCount = 0;
                    _requestCountResetTime = DateTime.UtcNow.AddHours(1);
                }

                if (_requestCount >= _maxRequestsPerHour)
                {
                    throw new Exception("Saatlik token istek sınırı aşıldı. Lütfen daha sonra tekrar deneyin.");
                }

                if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _expiryTime.AddMinutes(-5))
                {
                    return _accessToken;
                }
            }

            lock (_lockObject)
            {
                _requestCount++;
                Console.WriteLine($"Token isteği gönderiliyor. Bu saatte yapılan istek sayısı: {_requestCount}");
            }

            try
            {
                return await RequestNewTokenAsync();
            }
            catch (Exception ex)
            {
                lock (_lockObject)
                {
                    _requestCount--;
                }
                throw new Exception($"Token alınamadı: {ex.Message}", ex);
            }
        }

        private async Task<string> RequestNewTokenAsync()
        {
            var requestContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", _clientId),
                new KeyValuePair<string, string>("client_secret", _clientSecret)
            });

            HttpResponseMessage response = await _httpClient.PostAsync(_tokenUrl, requestContent);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Token isteği başarısız: {response.StatusCode}");
            }

            string responseContent = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent);

            lock (_lockObject)
            {
                _tokenType = tokenResponse.token_type;
                _accessToken = tokenResponse.access_token;
                _expiryTime = DateTime.UtcNow.AddSeconds(tokenResponse.expires_in);

                Console.WriteLine($"Yeni token alındı. Geçerlilik süresi: {_expiryTime}");
            }

            return _accessToken;
        }

        public async Task<string> GetAuthorizationHeaderAsync()
        {
            string token = await GetTokenAsync();
            return $"{_tokenType} {token}";
        }
    }

    public class TokenResponse
    {
        public string token_type { get; set; }
        public int expires_in { get; set; }
        public string access_token { get; set; }
    }

    public class SiparisService
    {
        private readonly TokenManager _tokenManager;
        private readonly HttpClient _httpClient;
        private readonly string _siparisApiUrl;
        private readonly Timer _siparisTimer;

        public SiparisService(string tokenUrl, string clientId, string clientSecret, string siparisApiUrl)
        {
            _tokenManager = TokenManager.Instance(tokenUrl, clientId, clientSecret);
            _httpClient = new HttpClient();
            _siparisApiUrl = siparisApiUrl;
             //5dk
            _siparisTimer = new Timer(async (state) => await GetSiparisListesiAsync(), 
                null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
        }

        public async Task GetSiparisListesiAsync()
        {
            try
            {
                string authHeader = await _tokenManager.GetAuthorizationHeaderAsync();
                
                var request = new HttpRequestMessage(HttpMethod.Get, _siparisApiUrl);
                request.Headers.Authorization = AuthenticationHeaderValue.Parse(authHeader);

                HttpResponseMessage response = await _httpClient.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Sipariş listesi alındı: {DateTime.Now}");
                    Console.WriteLine(content);
                }
                else
                {
                    Console.WriteLine($"Sipariş listesi alınamadı. Hata kodu: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Sipariş listesi alınırken hata oluştu: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _siparisTimer?.Dispose();
            _httpClient?.Dispose();
        }
    }

    // Örnek Kullanım
    class Program
    {
        static async Task Main(string[] args)
        {
            string tokenUrl = "https://api.example.com/token";
            string clientId = "your_client_id";
            string clientSecret = "your_client_secret";
            string siparisApiUrl = "https://api.example.com/siparisler";

            var siparisService = new SiparisService(tokenUrl, clientId, clientSecret, siparisApiUrl);

            Console.WriteLine("Uygulama çalışıyor. Çıkmak için herhangi bir tuşa basın.");
            Console.ReadKey();
        }
    }
}
