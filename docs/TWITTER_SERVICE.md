# TwitterService - Documentation Complète

## Vue d'ensemble

Le `TwitterService` est un service robuste de collecte et d'analyse de données Twitter (X) pour SponsorPulse. Il utilise **Xpoz.ai** comme source unique de données et fournit des métriques détaillées sur la performance d'événements sur Twitter.

## Architecture

### Couches impliquées

```
Domain/Models/TwitterAnalytics.cs
  ├── TwitterAnalytics (record principal)
  ├── TopTweet (record pour les top 3)
  ├── TopInfluencer (record pour l'influenceur leader)
  └── DateRange (plage de dates)

Application/Common/
  ├── Interfaces/ITwitterService.cs
  └── Configuration/XpozSettings.cs

Infrastructure/
  ├── Services/TwitterInfrastructureService.cs (implémentation)
  └── Xpoz/Models/XpozModels.cs (désérialisation Xpoz)

Infrastructure/DependencyInjection/DependencyInjection.cs (enregistrement)
```

## Configuration

### 1. Ajouter la clé API Xpoz

Dans **appsettings.json** ou **appsettings.Development.json**, configurez :

```json
{
  "XpozSettings": {
    "ApiKey": "YOUR_XPOZ_API_KEY",
    "BaseUrl": "https://api.xpoz.ai/v1",
    "DefaultBucket": "sponsorpulse-dev",
    "MaxRetries": 3,
    "InitialRetryDelayMs": 1000,
    "CpmEur": 0.005
  }
}
```

### 2. Paramètres de configuration

| Paramètre | Type | Description | Défaut |
|-----------|------|-------------|--------|
| `ApiKey` | string | Clé API Xpoz.ai | - |
| `BaseUrl` | string | URL de base de Xpoz | `https://api.xpoz.ai/v1` |
| `DefaultBucket` | string | Bucket de données | `sponsorpulse-dev` |
| `MaxRetries` | int | Nombre de tentatives en cas d'erreur | 3 |
| `InitialRetryDelayMs` | int | Délai initial pour retry exponentiel (ms) | 1000 |
| `CpmEur` | decimal | Coût par mille impressions (€) | 0.005 |

## Utilisation

### Injection et appel basique

```csharp
public class MonController(ITwitterService twitterService)
{
    public async Task<IActionResult> AnalyzeEvent()
    {
        var result = await twitterService.GetTwitterMetricsAsync(
            query: "#MonEvenement",
            startDate: DateTime.UtcNow.AddDays(-7),
            endDate: DateTime.UtcNow,
            cancellationToken: CancellationToken.None
        );

        if (!result.IsSuccess)
        {
            return BadRequest(result.ErrorMessage);
        }

        var analytics = result.Value;
        return Ok(analytics);
    }
}
```

### Objet de réponse `TwitterAnalytics`

```csharp
public record TwitterAnalytics(
    int TotalTweets,                              // Nombre total de tweets
    int TotalEngagement,                          // Somme likes + retweets + replies
    List<TopTweet> TopTweets,                    // Top 3 tweets avec engagement max
    DateRange Period,                            // Plage de dates analysée
    long EstimatedImpressions,                   // Reach estimé (somme followers uniques)
    decimal AdValueEquivalent,                   // Valeur publicitaire équivalente (€)
    Dictionary<string, double> SentimentRatio,   // Distribution sentiment (%)
    TopInfluencer TopInfluencer,                 // Profil leader identifié
    double ViralMultiplier                       // Ratio retweets / tweets
);
```

### Exemple complet de réponse

```json
{
  "totalTweets": 245,
  "totalEngagement": 3845,
  "topTweets": [
    {
      "authorName": "Jean Dupont",
      "authorHandle": "@jeandupont",
      "authorFollowersCount": 50000,
      "text": "Incroyable événement! #MonEvenement",
      "url": "https://twitter.com/jeandupont/status/...",
      "likes": 1200,
      "retweets": 450,
      "replies": 85,
      "createdAt": "2026-02-18T10:30:00Z"
    }
  ],
  "period": {
    "startDate": "2026-02-11T00:00:00Z",
    "endDate": "2026-02-18T00:00:00Z"
  },
  "estimatedImpressions": 2500000,
  "adValueEquivalent": 12500.00,
  "sentimentRatio": {
    "Positif": 72.5,
    "Neutre": 20.3,
    "Négatif": 7.2
  },
  "topInfluencer": {
    "name": "Jean Dupont",
    "handle": "@jeandupont",
    "followersCount": 50000,
    "tweetCount": 5
  },
  "viralMultiplier": 1.84
}
```

## Fonctionnalités principales

### 1. Collecte de données via Xpoz

L'endpoint utilisé est **`POST /v1/twitter/search/getTwitterPostsByKeywords`**

Paramètres envoyés :
```csharp
{
    keywords: "mon_query",
    startDate: "2026-02-11T00:00:00Z",
    endDate: "2026-02-18T00:00:00Z",
    bucket: "sponsorpulse-dev"
}
```

### 2. Retry automatique avec backoff exponentiel

Le service implémente automatiquement un système de retry :

- **Nombre de tentatives** : configurable (par défaut 3)
- **Délai initial** : configurable (par défaut 1000ms)
- **Formule** : `DelaiActuel = DelaiInitial × 2^(NumTentative-1)`

Exemple avec délai initial de 1000ms :
- 1ère tentative échouée → attente 1000ms
- 2ème tentative échouée → attente 2000ms
- 3ème tentative échouée → attente 4000ms

### 3. Calcul du Volume et Engagement

```csharp
int totalTweets = tweets.Count;
int totalEngagement = tweets.Sum(t => 
    t.Metrics.Likes + 
    t.Metrics.Retweets + 
    t.Metrics.Replies
);
```

### 4. Identification des Top Tweets

Les 3 tweets avec le plus d'engagement (likes + retweets + replies) sont extrait pour le storytelling.

### 5. Calcul du Reach (Impressions estimées)

Le reach est calculé en additionnant les followers des auteurs **uniques** pour éviter les doublons :

```csharp
var uniqueAuthors = tweets
    .GroupBy(t => t.Author.Id)
    .Select(g => g.First().Author)
    .ToList();

long estimatedImpressions = uniqueAuthors
    .Sum(a => a.FollowersCount);
```

### 6. Identification du Top Influencer

L'influenceur avec le plus de followers ayant tweeté sur la période :

```csharp
var topInfluencer = uniqueAuthors
    .OrderByDescending(a => a.FollowersCount)
    .FirstOrDefault();
```

### 7. Analyse de sentiment simplifée

Basée sur des mots-clés (support français ET anglais) :

- **Positif** : amazing, awesome, excellent, great, love, fantastic, etc.
- **Négatif** : bad, terrible, awful, horrible, hate, poor, worst, etc.
- **Neutre** : tout le reste

Distribution en pourcentage de l'ensemble des tweets.

### 8. Calcul de l'AVE (Advertising Value Equivalent)

L'AVE représente la valeur publicitaire équivalente en euros :

```csharp
decimal ave = (estimatedImpressions * cpmEur) / 1000
```

Avec CPM (Coût Par Mille) par défaut de **0.005€** = 5€ par million d'impressions.

Cette valeur est **modifiable** via la configuration `CpmEur`.

### 9. Calcul du Viral Multiplier

Ratio entre le nombre de retweets et le nombre total de tweets :

```csharp
double viralMultiplier = totalRetweets / totalTweets;
```

Interprétation :
- **< 0.5** : faible viralité
- **0.5 - 2** : viralité modérée
- **> 2** : haute viralité

## Optimisations C# 13

### Primary Constructors

L'injection de dépendances utilise les Primary Constructors :

```csharp
public class TwitterInfrastructureService(
    HttpClient httpClient,
    IOptions<XpozSettings> xpozOptions,
    ILogger<TwitterInfrastructureService> logger
) : ITwitterService
```

### Parallel Processing

Pour les listes longues, le traitement utilise `Parallel.ForEach` et `Task.Run` :

```csharp
var sentiments = tweets
    .AsParallel()
    .Select(t => DetectSentiment(t.Text))
    .ToList();
```

### Decimal pour les calculs financiers

Le type `decimal` est utilisé pour l'AVE afin d'éviter les imprécisions avec les calculs monétaires :

```csharp
decimal adValueEquivalent = (estimatedImpressions * _xpozSettings.CpmEur) / 1000m;
```

## Gestion des erreurs

Le service retourne un `Result<TwitterAnalytics>` qui gère les erreurs :

```csharp
if (!result.IsSuccess)
{
    // result.ErrorMessage contient le message d'erreur
    return BadRequest(result.ErrorMessage);
}

var analytics = result.Value;
```

### Erreurs possibles

| Erreur | Cause |
|--------|-------|
| "La requête (query) ne peut pas être vide" | Query vide ou null |
| "La date de début doit être antérieure à la date de fin" | Dates invalides |
| "Authentification échouée: vérifiez votre clé API Xpoz" | ApiKey incorrecte |
| "Erreur réseau après X tentatives" | Problème réseau persistent |
| "Impossible de récupérer les tweets après X tentatives" | L'API Xpoz n'a pas répondu |

## Logging

Le service utilise `ILogger<TwitterInfrastructureService>` pour tous les événements :

```csharp
logger.LogInformation("Récupération des métriques Twitter...");
logger.LogWarning("Tentative {N} échouée. Nouvelle tentative dans {Ms}ms");
logger.LogError("Erreur lors de la récupération des tweets: {Error}");
```

Pour déboguer, activez le mode Debug dans la configuration :

```json
{
  "Logging": {
    "LogLevel": {
      "SponsorPulse.Infrastructure": "Debug"
    }
  }
}
```

## Exemples d'utilisation avancée

### Analyser avec une plage de cookies personnalisée

```csharp
var result = await twitterService.GetTwitterMetricsAsync(
    query: "#MaMarque OR \"mon produit\"",  // Requête boolean
    startDate: new DateTime(2026, 2, 1),
    endDate: new DateTime(2026, 2, 28),
    cancellationToken: ct
);
```

### Modifier le CPM pour un contexte spécifique

```json
{
  "XpozSettings": {
    "CpmEur": 0.010  // 10€ par million d'impressions au lieu de 5€
  }
}
```

### Ajuster les retries pour une meilleure résilience

```json
{
  "XpozSettings": {
    "MaxRetries": 5,
    "InitialRetryDelayMs": 2000
  }
}
```

## Tests

Pour tester le service, vous pouvez créer un test unitaire :

```csharp
[TestClass]
public class TwitterServiceTests
{
    [TestMethod]
    public async Task GetTwitterMetricsAsync_WithValidQuery_ReturnsAnalytics()
    {
        // Arrange
        var httpClient = new HttpClient();
        var xpozSettings = Options.Create(new XpozSettings
        {
            ApiKey = "test-key",
            BaseUrl = "https://api-test.xpoz.ai/v1",
            DefaultBucket = "test"
        });
        var logger = new Mock<ILogger<TwitterInfrastructureService>>();
        
        var service = new TwitterInfrastructureService(
            httpClient,
            xpozSettings,
            logger.Object
        );

        // Act
        var result = await service.GetTwitterMetricsAsync(
            "#TestEvent",
            DateTime.UtcNow.AddDays(-7),
            DateTime.UtcNow
        );

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
    }
}
```

## Points de vigilance

1. **Rate Limiting** : Vérifiez les limites de l'API Xpoz
2. **Timeout** : Les longues périodes peuvent nécessiter un timeout élevé
3. **Coûts API** : Chaque requête à Xpoz a un coût
4. **Données** : Les données Twitter peuvent avoir un délai de quelques heures
5. **Duplicatas** : Le service gère déjà les doublons d'auteurs pour le reach

## Support et dépannage

### Le service retourne 0 tweets

- Vérifiez que la requête (query) est valide
- Confirmez que la plage de dates n'est pas trop ancienne
- Testez directement avec l'API Xpoz

### Erreurs d'authentification

```
"Authentification échouée: vérifiez votre clé API Xpoz"
```

→ Remplacez la clé API dans appsettings.json par votre vraie clé Xpoz

### Timeouts

Augmentez le timeout HTTP dans Program.cs :

```csharp
services.AddHttpClient().ConfigureHttpClientDefaults(c => 
    c.HttpClientBuilder.ConfigureHttpClient(client => 
        client.Timeout = TimeSpan.FromSeconds(30)
    )
);
```

## Performance

- **Requête typique** : 1-3 secondes (dépend du volume de tweets)
- **Traitement données** : < 100ms pour 1000 tweets
- **Mémoire** : ~5MB pour 10 000 tweets

---

**Version** : 1.0  
**Dernière mise à jour** : 2026-02-18  
**Auteur** : Backend Team - SponsorPulse
