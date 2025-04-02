# Sipariş Yönetimi Servisi

Sipariş yönetimi servisi, belirli bir API'dan periyodik olarak sipariş bilgilerini çeken C# uygulamasıdır. 

## Özellikler

- Token yönetimi (otomatik yenileme, önbellekleme)
- Saatlik token istek sınırlaması (5 istek/saat)
- Her 5 dakikada bir sipariş listesi kontrolü
- Singleton tasarım desenini kullanan Token Manager

## Gereksinimler

- Visual Studio veya başka bir C# IDE

## Kurulum

1. Projeyi klonlayın:
```
git clone https://github.com/username/siparis-yonetimi.git
```

2. Visual Studio veya tercih ettiğiniz IDE'de projeyi açın.

3. `Program.cs` içindeki aşağıdaki yapılandırma bilgilerini güncelleyin:
```csharp
string tokenUrl = "https://api.example.com/token";
string clientId = "your_client_id";
string clientSecret = "your_client_secret";
string siparisApiUrl = "https://api.example.com/siparisler";
```

## Çalıştırma

Projeyi derleyin ve çalıştırın. Uygulama her 5 dakikada bir sipariş listesini kontrol edecektir.
