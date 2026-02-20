# 🚀 TwitterService - Guide d'accès rapide

## ⚡ 30 secondes pour démarrer

### Étape 1 : Ajouter la clé API
```bash
# Éditer le fichier
SponsorPulse/wwwroot/appsettings.json

# Remplacer
"ApiKey": "TON_API_KEY"

# Par votre vraie clé
"ApiKey": "votre-clé-xpoz-ici"
```

### Étape 2 : Utiliser le service
```csharp
// Dans n'importe quel service/contrôleur
public class MonService(ITwitterService twitterService)
{
    public async Task<IActionResult> Analyser()
    {
        var result = await twitterService.GetTwitterMetricsAsync(
            "#MonEvenement",
            DateTime.UtcNow.AddDays(-7),
            DateTime.UtcNow
        );
        
        if (result.IsSuccess)
        {
            // result.Value contient TwitterAnalytics
            var metriques = result.Value;
        }
    }
}
```

### Étape 3 : Compiler et tester
```bash
dotnet build    # ✅ Compile sans erreurs
dotnet run      # Lance l'application

# Test API
curl http://localhost:5000/api/twitteranalytics/%23test?days=7
```

---

## 📍 Où trouver quoi

### ❓ J'ai une question sur...

#### Configuration
```
Question : Comment ajouter ma clé API Xpoz?
Réponse  : appsettings.json → XpozSettings → ApiKey
```

```
Question : Comment modifier le CPM pour les calculs AVE?
Réponse  : appsettings.json → XpozSettings → CpmEur (default: 0.005)
```

```
Question : Comment augmenter le nombre de retries?
Réponse  : appsettings.json → XpozSettings → MaxRetries (default: 3)
```

#### Utilisation
```
Question : Comment utiliser le TwitterService?
Réponse  : Injectez ITwitterService puis appelez GetTwitterMetricsAsync()
Fichier  : TwitterAnalyticsController.cs (exemples)
```

```
Question : Quels sont les 4 endpoints disponibles?
Réponse  : 
  1. GET  /api/twitteranalytics/{query}?days=7
  2. POST /api/twitteranalytics/range
  3. POST /api/twitteranalytics/batch
  4. POST /api/twitteranalytics/compare
Fichier  : TwitterAnalyticsController.cs
```

#### Implémentation
```
Question : Comment fonctionne le retry automatique?
Réponse  : Retry exponentiel (1s → 2s → 4s) sur erreurs réseau
Fichier  : TwitterInfrastructureService.cs::FetchTwitterPostsWithRetryAsync()
```

```
Question : Comment sont calculées les impressions estimées?
Réponse  : Somme des followers des auteurs uniques
Fichier  : TwitterInfrastructureService.cs::ProcessTwitterDataAsync()
```

```
Question : Comment marche l'analyse de sentiment?
Réponse  : Mots-clés français et anglais, distribution en %
Fichier  : TwitterInfrastructureService.cs::CalculateSentimentRatio()
```

#### Intégration Xpoz
```
Question : Quel endpoint Xpoz est utilisé?
Réponse  : POST /v1/twitter/search/getTwitterPostsByKeywords
Fichier  : docs/XPOZ_INTEGRATION.md
```

```
Question : Comment fonctonne l'API Xpoz?
Réponse  : Détails complets dans la documentation
Fichier  : docs/XPOZ_INTEGRATION.md
```

#### Documentation générale
```
Question : Je veux comprendre tout le service
Réponse  : Lisez la doc complète
Fichier  : docs/TWITTER_SERVICE.md
```

---

## 🎯 Fichiers essentiels

### Aller à...

| But | Fichier | Ligne |
|-----|---------|-------|
| Ajouter clé API | `appsettings.json` | ~12 |
| Utiliser service | Injecter `ITwitterService` | - |
| Voir API REST | `TwitterAnalyticsController.cs` | ~1 |
| Code implémentation | `TwitterInfrastructureService.cs` | ~1 |
| Structures de données | `TwitterAnalytics.cs` | ~1 |
| Configuration | `XpozSettings.cs` | ~1 |
| Doc complète | `docs/TWITTER_SERVICE.md` | ~1 |

---

## 💡 Cas d'usage courants

### 1️⃣ Analyser un événement Twitter
```csharp
var result = await twitterService.GetTwitterMetricsAsync(
    "#MonEvenement",
    new DateTime(2026, 2, 1),
    new DateTime(2026, 2, 28)
);

if (result.IsSuccess)
{
    var metrics = result.Value;
    Console.WriteLine($"Tweets: {metrics.TotalTweets}");
    Console.WriteLine($"Engagement: {metrics.TotalEngagement}");
    Console.WriteLine($"AVE: {metrics.AdValueEquivalent}€");
}
```

### 2️⃣ Comparer deux initiatives
```csharp
var result1 = await twitterService.GetTwitterMetricsAsync("#Marque1", ...);
var result2 = await twitterService.GetTwitterMetricsAsync("#Marque2", ...);

// Comparer metrics1.Value vs metrics2.Value
```

### 3️⃣ Analyser plusieurs hashtags en parallèle
```csharp
var hashtags = new[] { "#Event1", "#Event2", "#Event3" };
var tasks = hashtags.Select(tag => 
    twitterService.GetTwitterMetricsAsync(tag, startDate, endDate)
).ToList();

var results = await Task.WhenAll(tasks);
```

### 4️⃣ Utiliser dans un Razor Component
```csharp
@page "/twitter-analytics"
@inject ITwitterService TwitterService

<button @onclick="Analyser">Analyser</button>

@code {
    private async Task Analyser()
    {
        var result = await TwitterService.GetTwitterMetricsAsync(
            "#MonEvent",
            DateTime.UtcNow.AddDays(-7),
            DateTime.UtcNow
        );
        // Afficher result.Value
    }
}
```

---

## 🐛 Dépannage rapide

### ❌ Erreur : "Authentification échouée"
```
Cause   : Clé API Xpoz invalide
Solution: Vérifiez "ApiKey" dans appsettings.json
```

### ❌ Erreur : "Impossible de récupérer les tweets"
```
Cause   : Problème réseau ou API Xpoz indisponible
Solution: Attendez quelques secondes et réessayez
          Le service fait 3 tentatives automatiques
```

### ❌ Erreur : "La requête ne peut pas être vide"
```
Cause   : Vous avez passé une query vide
Solution: Vérifiez votre paramètre : "#UnHashtag" ou "unemot-cle"
```

### ❌ Erreur : "La date de début doit être antérieure"
```
Cause   : startDate >= endDate
Solution: Assurez-vous que startDate < endDate
```

### ❌ Aucun tweet trouvé
```
Cause   : Aucun tweet ne correspond à la requête
Possible: La requête est trop spécifique ou trop ancienne
Solution: Essayez une requête plus générale ou augmentez la plage
```

---

## 📊 Métriques retournées

```csharp
TwitterAnalytics metrics = result.Value;

// Compteurs
int tweets = metrics.TotalTweets;                    // 245
int engagement = metrics.TotalEngagement;            // 3845

// Reach et impact
long impressions = metrics.EstimatedImpressions;     // 2,500,000
decimal ave = metrics.AdValueEquivalent;             // 12500€

// Analyse
Dictionary<string, double> sentiment = 
    metrics.SentimentRatio;                          // Positif: 72.5%, Neutre: 20.3%, Négatif: 7.2%

// Viral et influence
double viralMultiplier = metrics.ViralMultiplier;    // 1.84
TopInfluencer influencer = metrics.TopInfluencer;    // Jean Dupont, 50K followers

// Top content
List<TopTweet> topTweets = metrics.TopTweets;        // Top 3 tweets
```

---

## 🔗 Endpoints API

### GET - Analyse simple
```bash
curl "http://localhost:5000/api/twitteranalytics/%23MonEvent?days=7"

# Retourne les 7 derniers jours d'un hashtag
```

### POST - Avec dates personnalisées
```bash
curl -X POST http://localhost:5000/api/twitteranalytics/range \
  -H "Content-Type: application/json" \
  -d '{
    "query": "#MonEvent",
    "startDate": "2026-02-01T00:00:00Z",
    "endDate": "2026-02-28T00:00:00Z"
  }'
```

### POST - Analyse parallèle
```bash
curl -X POST http://localhost:5000/api/twitteranalytics/batch \
  -H "Content-Type: application/json" \
  -d '{
    "queries": ["#Event1", "#Event2", "#Event3"],
    "startDate": "2026-02-01T00:00:00Z",
    "endDate": "2026-02-28T00:00:00Z"
  }'
```

### POST - Comparaison
```bash
curl -X POST http://localhost:5000/api/twitteranalytics/compare \
  -H "Content-Type: application/json" \
  -d '{
    "query1": "#Marque1",
    "query2": "#Marque2",
    "startDate": "2026-02-01T00:00:00Z",
    "endDate": "2026-02-28T00:00:00Z"
  }'
```

---

## 🎓 Concepts clés

### Retry exponentiel
Automatique avec delais : 1s → 2s → 4s (3 tentatives)

### Impression/Reach
Somme des followers des auteurs uniques. Un auteur qui tweete 5 fois ne compte qu'une fois.

### AVE (Advertising Value Equivalent)
Valeur publicitaire en euros = (Impressions × CPM) / 1000

### Sentiment
Analyse basée sur mots-clés. Catégories: Positif, Neutre, Négatif

### Viral Multiplier
Ratio retweets/tweets total. Idéal > 1.5

### Top Tweets
Les 3 tweets avec le plus d'engagement (likes + retweets + replies)

### Top Influencer
L'auteur avec le plus de followers ayant tweeté dans la période

---

## 📚 Lectures recommandées

### Pour démarrer
1. Ce fichier (vous êtes ici!)
2. `appsettings.json` - Configuration

### Pour comprendre
1. `TwitterAnalyticsController.cs` - Utilisation API
2. `TwitterAnalytics.cs` - Structures de données
3. `TwitterInfrastructureService.cs` - Implémentation

### Pour approfondir
1. `docs/TWITTER_SERVICE.md` - Doc complète
2. `docs/XPOZ_INTEGRATION.md` - Intégration Xpoz
3. `TWITTER_SERVICE_COMPLETION.md` - Rapport technique

---

## ⏱️ Temps estimés

| Tâche | Temps |
|-------|-------|
| Ajouter clé API | 1 min |
| Compiler projet | 5 min |
| Appeler premier endpoint | 2 min |
| Intégrer dans votre code | 10 min |
| Lire doc complète | 30 min |
| Comprendre toute l'implémentation | 1h |

---

## ✅ Checklist de démarrage

- [ ] Clé API Xpoz ajoutée dans appsettings.json
- [ ] Projet compilé (`dotnet build`)
- [ ] Application lancée (`dotnet run`)
- [ ] Premier endpoint testé (`curl ...`)
- [ ] Service injecté dans mon code
- [ ] Doc TWITTER_SERVICE.md lue
- [ ] Cas d'usage implémenté

---

## 🆘 Support rapide

### Besoin d'aide?

1. **Erreur de compilation?**
   → Vérifiez que tous les fichiers sont créés
   → Lancez `dotnet build` pour voir l'erreur exacte

2. **Service ne marche pas?**
   → Vérifiez la clé API Xpoz dans appsettings.json
   → Vérifiez que l'application démarre sans erreur

3. **Pas d'options ou de contexte?**
   → Les services sont auto-enregistrés via AddInfrastructure()
   → Vérifiez que Program.cs appelle cette méthode

4. **Questions techniques?**
   → Lisez docs/TWITTER_SERVICE.md (complet et détaillé)
   → Examinez le code: les méthodes sont bien commentées

---

## 🏁 Vous êtes prêt!

Maintenant vous pouvez :
- ✅ Configurer l'API Xpoz
- ✅ Utiliser le TwitterService
- ✅ Appeler les endpoints REST
- ✅ Analyser les données Twitter

Bon développement! 🚀

---

**Version** : 1.0  
**Dernière mise à jour** : 2026-02-18  
**Statut** : ✅ **READY TO GO**
