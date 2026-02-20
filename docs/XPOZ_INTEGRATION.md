# Guide d'intégration Xpoz.ai pour SponsorPulse

## Vue d'ensemble

Ce guide explique comment le `TwitterService` de SponsorPulse s'intègre avec **Xpoz.ai** pour collecter et analyser les données Twitter (X).

## Architecture d'intégration

```
┌─────────────────────────────────────┐
│    SponsorPulse Application         │
│  ┌───────────────────────────────┐  │
│  │   ITwitterService             │  │
│  │    (Interface)                │  │
│  └───────────────────────────────┘  │
│           ▲                          │
│           │ injecté via DI           │
│           ▼                          │
│  ┌───────────────────────────────┐  │
│  │ TwitterInfrastructureService  │  │
│  │  (Implémentation)             │  │
│  └───────────────────────────────┘  │
└──────────────┬──────────────────────┘
               │
               │ HTTP POST            
               │ x-api-key: header
               │
               ▼
┌─────────────────────────────────────┐
│        Xpoz.ai API                  │
│                                     │
│  POST /v1/twitter/search/           │
│       getTwitterPostsByKeywords     │
└─────────────────────────────────────┘
               │
               │ JSON Response
               │ (tweets array)
               │
               ▼
┌─────────────────────────────────────┐
│  Xpoz JSON Response                 │
│                                     │
│  {                                  │
│    "posts": [...],                  │
│    "totalCount": 245,               │
│    "nextCursor": "..."              │
│  }                                  │
└─────────────────────────────────────┘
```

## Endpoint Xpoz utilisé

### **getTwitterPostsByKeywords**

**Endpoint** : `POST https://api.xpoz.ai/v1/twitter/search/getTwitterPostsByKeywords`

**Headers requis** :
```http
x-api-key: YOUR_XPOZ_API_KEY
Content-Type: application/json
```

**Body Request** :
```json
{
  "keywords": "string (required) - hashtag ou requête",
  "startDate": "2026-02-11T00:00:00Z (required)",
  "endDate": "2026-02-18T00:00:00Z (required)",
  "bucket": "string (required) - container de données"
}
```

**Response Structure** :
```json
{
  "posts": [
    {
      "id": "1234567890",
      "text": "Contenu du tweet",
      "createdAt": "2026-02-18T10:30:00Z",
      "url": "https://twitter.com/...",
      "language": "fr",
      "author": {
        "id": "user123",
        "username": "@username",
        "displayName": "User Name",
        "followersCount": 50000,
        "profileImageUrl": "https://..."
      },
      "metrics": {
        "likes": 1200,
        "retweets": 450,
        "replies": 85,
        "bookmarks": 120
      }
    }
  ],
  "totalCount": 245,
  "nextCursor": "eyJwb3N0X2lkIjo..."
}
```

## Flux de requête détaillé

### 1. Validation des entrées
```csharp
// Vérification dans GetTwitterMetricsAsync
if (string.IsNullOrWhiteSpace(query)) → Erreur
if (startDate >= endDate) → Erreur
```

### 2. Construction de la requête HTTP
```csharp
var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
request.Content = JsonContent.Create(new {
    keywords = query,
    startDate = startDate.ToString("O"),  // Format ISO 8601
    endDate = endDate.ToString("O"),
    bucket = _xpozSettings.DefaultBucket
});
request.Headers.Add("x-api-key", _xpozSettings.ApiKey);
```

### 3. Envoi avec retry automatique
```
Tentative 1 → Échec (ex: timeout)
  ↓ Attendre 1000ms
Tentative 2 → Échec (ex: 503 Service Unavailable)
  ↓ Attendre 2000ms
Tentative 3 → Succès ✓
```

### 4. Désérialisation de la réponse
```csharp
var xpozResponse = JsonSerializer.Deserialize<XpozTwitterResponse>(
    content,
    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
);
```

### 5. Traitement parallèle
```csharp
// Les tweets sont traités en parallèle pour performance
tweets.AsParallel()
    .Select(t => DetectSentiment(t.Text))
    .ToList()
```

### 6. Calcul des métriques
- Sommes et moyennes calculées
- Doublons traités (deduplication des auteurs)
- Sentiment analysé
- AVE calculée

## Configuration requise

### Pour développement

**appsettings.Development.json** :
```json
{
  "XpozSettings": {
    "ApiKey": "your_dev_api_key",
    "BaseUrl": "https://api.xpoz.ai/v1",
    "DefaultBucket": "sponsorpulse-dev",
    "MaxRetries": 3,
    "InitialRetryDelayMs": 1000,
    "CpmEur": 0.005
  }
}
```

### Pour production

**appsettings.json** (sans ApiKey) ou via secrets manager :
```json
{
  "XpozSettings": {
    "ApiKey": "${XPOZ_API_KEY}",  // Depuis variables d'env
    "BaseUrl": "https://api.xpoz.ai/v1",
    "DefaultBucket": "sponsorpulse-prod",
    "MaxRetries": 5,
    "InitialRetryDelayMs": 2000,
    "CpmEur": 0.005
  }
}
```

## Gestion des erreurs Xpoz

### Staatus codes HTTP

| Code | Signification | Action |
|------|---------------|--------|
| 200 | Succès | Traiter les données |
| 400 | Bad Request | Vérifier les paramètres |
| 401 | Unauthorized | Vérifier la clé API |
| 429 | Rate Limited | Attendre + retry |
| 500 | Server Error | Retry avec backoff |
| 503 | Service Unavailable | Retry avec backoff |

### Retry Logic

```csharp
// Pour les erreurs 500-599 : automatique retry
// Pour les erreurs 400-499 : pas de retry (sauf si timeout)
// Pour HttpRequestException : retry

Délai = InitialDelay × 2^(NumTentative - 1)

Exemple avec InitialDelay = 1000ms :
Tentative 1 échouée → attendre 1s
Tentative 2 échouée → attendre 2s  
Tentative 3 échouée → attendre 4s
```

## Optimisation des requêtes

### Filtrer les résultats

```csharp
// Requête simple
var result = await twitterService.GetTwitterMetricsAsync(
    "#MonEvent",
    startDate,
    endDate
);

// Requête boolean complexe
var result = await twitterService.GetTwitterMetricsAsync(
    "#MonEvent OR #MyEvent -fake",
    startDate,
    endDate
);

// Requête avec domaine
var result = await twitterService.GetTwitterMetricsAsync(
    "from:@compte_officiel #MonEvent",
    startDate,
    endDate
);
```

### Gestion des gros volumes

Pour les requêtes susceptibles de retourner beaucoup de tweets :

```csharp
// Augmenter le timeout dans Program.cs
services.AddHttpClient().ConfigureHttpClientDefaults(c => 
    c.HttpClientBuilder.ConfigureHttpClient(client => 
        client.Timeout = TimeSpan.FromSeconds(60)  // 60 secondes
    )
);

// Réduire la charge API avec des requêtes plus spécifiques
var result = await twitterService.GetTwitterMetricsAsync(
    "#MonEvent -spam -bot",  // Exclure les spam et bots
    startDate,
    endDate
);
```

## Limitations Xpoz.ai

1. **Délai de données** : Les tweets peuvent avoir un délai de quelques heures avant d'être indexés
2. **Rate limiting** : Vérifiez vos limites d'API auprès de Xpoz
3. **Rétention** : Les anciennes données peuvent ne pas être disponibles
4. **Mots-clés** : Les requêtes doivent être valides selon la syntaxe Xpoz
5. **Volume** : Les très gros volumes peuvent être limités ou fragmentés

## Coûts API

Chaque requête à Xpoz.ai a un coût. Estimez vos besoins :

```
Cas d'usage 1 : Analyse de 1 événement par jour
= 1 requête/jour × 365 jours = 365 requêtes/an

Cas d'usage 2 : Analyse de 10 hashtags en parallèle
= 10 requêtes × 365 jours = 3650 requêtes/an

Cas d'usage 3 : Comparaison temps réel (10 requêtes/heure)
= 10 × 24 × 365 = 87 600 requêtes/an
```

Contactez Xpoz.ai pour les tarifs spécifiques.

## Debugging et Logging

### Activer le logging DEBUG

```json
{
  "Logging": {
    "LogLevel": {
      "SponsorPulse.Infrastructure.Services": "Debug"
    }
  }
}
```

### Logs générés

```
[INF] Récupération des métriques Twitter pour : #MonEvent
[WRN] Tentative 1 échouée (HTTP 503). Nouvelle tentative dans 1000ms
[INF] Analyse Twitter complétée: 245 tweets, 3845 engagements
```

### Vérifier les requêtes HTTP

Utilisez Fiddler, Charles, ou Wireshark pour inspecter :

1. **Headers** :
   - `x-api-key` correct ?
   - `Content-Type: application/json` ?

2. **Body** :
   - Dates au format ISO 8601 ?
   - Keyword non vide ?
   - Bucket valide ?

3. **Response** :
   - Status code 200 ?
   - JSON valide ?
   - Posts array peuplé ?

## Exemple complet d'intégration

### Dans Program.cs
```csharp
// Configuration automatique via DependencyInjection.AddInfrastructure()
builder.Services.AddInfrastructure(builder.Configuration);
```

### Dans un contrôleur
```csharp
[ApiController]
[Route("api/[controller]")]
public class EventAnalyticsController(
    ITwitterService twitterService
) : ControllerBase
{
    [HttpGet("{eventId}")]
    public async Task<IActionResult> GetEventMetrics(int eventId)
    {
        var evt = await _eventService.GetEventAsync(eventId);
        
        var result = await twitterService.GetTwitterMetricsAsync(
            query: evt.TwitterHashtag,
            startDate: evt.StartDate,
            endDate: evt.EndDate
        );
        
        if (!result.IsSuccess)
            return BadRequest(result.ErrorMessage);
        
        return Ok(result.Value);
    }
}
```

## Support Xpoz

- **Documentation** : https://docs.xpoz.ai
- **API Status** : https://status.xpoz.ai
- **Support Email** : support@xpoz.ai
- **Rate Limits** : Vérifiez dans votre dashboard Xpoz

## Migration future

Si vous devez changer de fournisseur de données :

1. Créez un nouvel endpoint `ITwitterDataProvider`
2. Dérivez `TwitterInfrastructureService` de cette interface
3. Créez une nouvelle implémentation `AlternativeTwitterDataProvider`
4. Changez la configuration dans `DependencyInjection.cs`

Le reste du code reste identique grâce au pattern abstrait.

---

**Dernière mise à jour** : 2026-02-18  
**Version Xpoz** : v1  
**Statut** : Production Ready ✅
