# CvReader

Toplu yüklenen PDF CV'leri, bir iş ilanının anahtar kelimelerine anlamsal yakınlığına göre sıralar.
Metinler yerelde çalışan bir embedding modeliyle (Ollama + bge-m3) vektöre çevrilir, yakınlık
PostgreSQL üzerinde pgvector ile hesaplanır. Her kullanıcı yalnızca kendi ilanlarını ve CV'lerini görür;
CV'ler klasörlere ayrılabilir, çıkarılan metinleri görüntülenebilir ve silinebilir.

## Yapı

| Proje | İçerik |
|---|---|
| `src/CvReader.Domain` | Varlıklar; hiçbir pakete bağımlı değildir |
| `src/CvReader.Application` | İş kuralları ve dış dünyaya açılan arayüzler |
| `src/CvReader.Infrastructure` | EF Core + pgvector, PDF ayrıştırma, Ollama istemcisi, bcrypt |
| `src/CvReader.Api` | FastEndpoints uç noktaları, JWT (HttpOnly cookie), hız sınırları |
| `src/CvReader.Web` | React + Vite ön yüz; build çıktısı API'nin `wwwroot` klasörüne yazılır |
| `tests/CvReader.Tests` | Birim testleri |

## Gereksinimler

- .NET SDK 10
- Node.js 24
- Docker (PostgreSQL + pgvector için)
- [Ollama](https://ollama.com) ve `bge-m3` modeli

## Kurulum

```bash
# 1. Veritabanı (yalnızca 127.0.0.1:5433 üzerinden erişilir)
docker compose up -d

# 2. Embedding modeli
ollama pull bge-m3

# 3. Sırlar (depoya girmez)
cd src/CvReader.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5433;Database=cvreader;Username=postgres;Password=postgres"
dotnet user-secrets set "Jwt:SigningKey" "<en az 32 karakterlik rastgele bir değer>"

# 4. Şema
dotnet tool install --global dotnet-ef   # bir kez
dotnet ef database update --project ../CvReader.Infrastructure --startup-project .
```

## Çalıştırma

```bash
# API (http://localhost:5130, Swagger: /swagger — yalnızca Development ortamında)
dotnet run --project src/CvReader.Api

# Ön yüz geliştirme sunucusu (http://localhost:5173, /api isteklerini API'ye aktarır)
cd src/CvReader.Web
npm install
npm run dev
```

Tek adresten sunmak için `npm run build` çalıştırın; API çıktıyı kök adreste sunar.

## Testler

```bash
dotnet test
cd src/CvReader.Web && npm run lint && npm run build
```

## Yapılandırma

| Anahtar | Varsayılan | Açıklama |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | — | Zorunlu |
| `Jwt:SigningKey` | — | Zorunlu, en az 32 karakter |
| `Jwt:Issuer`, `Jwt:Audience` | `CvReader` | Token'da doğrulanır |
| `Jwt:ExpiryMinutes` | `60` | Oturum süresi |
| `Ollama:BaseUrl` | `http://localhost:11434` | |
| `Ollama:Model` | `bge-m3` | Vektör boyutu 1024'e sabittir; farklı boyutlu model migration gerektirir |
| `Ollama:TimeoutSeconds` | `90` | |
| `ForwardedHeaders:KnownProxies` | boş | Ters vekil arkasındaysa vekilin IP adresleri; hız sınırı gerçek istemci IP'sini bundan öğrenir |

Eksik zorunlu ayarlar uygulama açılırken hata verir.

## Sınırlar

- Yükleme: istek başına en çok 50 dosya, dosya başına 10 MB ve 50 sayfa; istek gövdesi Kestrel varsayılanıyla ~28 MB.
- Taranmış (metin içermeyen) ve şifreli PDF'ler reddedilir.
- Aynı dosya aynı kullanıcı tarafından ikinci kez yüklenemez.
