# Plan d'Implémentation - Simulation de Fetching et Fallback Manuel (Révisé)

## 📋 Vue d'ensemble

Ajouter la simulation de fetching de données avec modal de chargement et fallback manuel pour les données de stream (Twitch) et réseaux sociaux (Twitter) dans la page EventDetails.razor.

**Contraintes :**
- ❌ Pas de persistance en base de données (mémoire uniquement)
- ❌ Boutons sans texte (icônes uniquement)
- ✅ Utiliser `StaticDemoRepository` pour données démo réalistes
- ✅ Mode démo uniquement

---

## 🎯 Objectifs

1. **Simulation de fetching** : Modal de chargement avec étapes animées
2. **Fallback manuel** : Formulaires de saisie (mémoire uniquement)
3. **Icones** : Icônes dans les titres + boutons d'action (icônes seules)
4. **Données réalistes** : Réutiliser la logique de `StaticDemoRepository`

---

## 📝 Tâches Détaillées

### 1. Ajout des icônes dans les titres de section

**Fichier** : `EventDetails.razor`

**Modifications** :
- Ajouter une icône `ic:pencil.svg` à côté de `<h3 class="section-title">Données Stream</h3>`
- Ajouter une icône `ic:pencil.svg` à côté de `<h3 class="section-title">Données Twitter</h3>`
- Style : `<img src="https://api.iconify.design/ic:pencil.svg" class="section-action-icon" alt="" />`

---

### 2. Boutons d'action (icônes uniquement)

**Pour chaque section (Twitch et Twitter)** :

```razor
@if (DemoDataService.IsInDemoMode)
{
    <div class="demo-action-buttons">
        <button class="btn-icon-action" @onclick="SimulateTwitchFetch" title="Simuler">
            <img src="https://api.iconify.design/mdi:sync.svg" alt="" />
        </button>
        <button class="btn-icon-action" @onclick="ToggleTwitchManualForm" title="Éditer">
            <img src="https://api.iconify.design/ic:pencil.svg" alt="" />
        </button>
    </div>
}
```

**Position** : Juste après le `<h3>` de chaque section

---

### 3. Modal de chargement (commune Twitch/Twitter)

**État** :
```csharp
private bool _isSimulating = false;
private string _simulationType = ""; // "Twitch" ou "Twitter"
```

**UI** :
```razor
@if (_isSimulating)
{
    <div class="loading-modal" @onclick="() => _isSimulating = false">
        <div class="loading-content" @onclick:stopPropagation>
            <div class="spinner-large"></div>
            <p class="loading-text">
                @(_simulationType == "Twitch" 
                    ? "Récupération des données Twitch..." 
                    : "Analyse des tweets en cours...")
            </p>
            <div class="loading-steps">
                @if (_simulationType == "Twitch")
                {
                    <span class="step completed">✓ Connexion</span>
                    <span class="step active">⟳ Données stream</span>
                    <span class="step">⋯ Métriques</span>
                }
                else
                {
                    <span class="step completed">✓ Recherche</span>
                    <span class="step active">⟳ Analyse</span>
                    <span class="step">⋯ Calculs</span>
                }
            </div>
        </div>
    </div>
}
```

---

### 4. Simulation Twitch - Méthode

```csharp
private async Task SimulateTwitchFetch()
{
    _isSimulating = true;
    _simulationType = "Twitch";
    StateHasChanged();
    
    await Task.Delay(2000);
    
    if (_event != null)
    {
        // Utiliser StaticDemoRepository pour données réalistes
        var metrics = StaticDemoRepository.GetTwitchMetrics(_event.Id);
        
        _event.ViewerCount = metrics.ViewerCount;
        _event.PeakViewers = metrics.PeakViewers;
        _event.StreamDuration = metrics.StreamDuration;
        _event.GameName = metrics.GameName;
        
        StateHasChanged();
    }
    
    _isSimulating = false;
}
```

---

### 5. Formulaire manuel Twitch

**État** :
```csharp
private bool _showTwitchManualForm = false;
private TwitchManualData _manualTwitchData = new();

private class TwitchManualData
{
    public int ViewerCount { get; set; }
    public int PeakViewers { get; set; }
    public double DurationHours { get; set; }
    public string GameName { get; set; } = "";
}
```

**UI** :
```razor
@if (_showTwitchManualForm)
{
    <div class="manual-form-panel">
        <div class="form-grid">
            <div class="form-group">
                <label>Viewers</label>
                <input type="number" class="sp-input" @bind="_manualTwitchData.ViewerCount" />
            </div>
            <div class="form-group">
                <label>Pic Viewers</label>
                <input type="number" class="sp-input" @bind="_manualTwitchData.PeakViewers" />
            </div>
            <div class="form-group">
                <label>Durée (h)</label>
                <input type="number" class="sp-input" @bind="_manualTwitchData.DurationHours" step="0.5" />
            </div>
            <div class="form-group">
                <label>Jeu</label>
                <input type="text" class="sp-input" @bind="_manualTwitchData.GameName" />
            </div>
        </div>
        <button class="btn-save" @onclick="SaveManualTwitchData">
            Appliquer
        </button>
    </div>
}
```

**Méthode** :
```csharp
private void SaveManualTwitchData()
{
    if (_event != null)
    {
        _event.ViewerCount = _manualTwitchData.ViewerCount;
        _event.PeakViewers = _manualTwitchData.PeakViewers;
        _event.StreamDuration = TimeSpan.FromHours(_manualTwitchData.DurationHours);
        _event.GameName = _manualTwitchData.GameName;
        
        _showTwitchManualForm = false;
        StateHasChanged();
    }
}
```

---

### 6. Simulation Twitter - Méthode

```csharp
private async Task SimulateTwitterFetch()
{
    _isSimulating = true;
    _simulationType = "Twitter";
    StateHasChanged();
    
    await Task.Delay(2500);
    
    if (_event != null)
    {
        var analytics = StaticDemoRepository.GetTwitterAnalytics(_event.Id);
        _event.TwitterAnalytics = analytics;
        
        StateHasChanged();
    }
    
    _isSimulating = false;
}
```

---

### 7. Formulaire manuel Twitter

**État** :
```csharp
private bool _showTwitterManualForm = false;
private TwitterManualData _manualTwitterData = new();

private class TwitterManualData
{
    public int TotalTweets { get; set; }
    public int TotalEngagement { get; set; }
    public long EstimatedImpressions { get; set; }
    public decimal AdValueEquivalent { get; set; }
    public double SentimentPositive { get; set; }
    public double SentimentNeutral { get; set; }
}
```

**UI** :
```razor
@if (_showTwitterManualForm)
{
    <div class="manual-form-panel">
        <div class="form-grid">
            <div class="form-group">
                <label>Tweets</label>
                <input type="number" class="sp-input" @bind="_manualTwitterData.TotalTweets" />
            </div>
            <div class="form-group">
                <label>Engagement</label>
                <input type="number" class="sp-input" @bind="_manualTwitterData.TotalEngagement" />
            </div>
            <div class="form-group">
                <label>Impressions</label>
                <input type="number" class="sp-input" @bind="_manualTwitterData.EstimatedImpressions" />
            </div>
            <div class="form-group">
                <label>AVE (€)</label>
                <input type="number" class="sp-input" @bind="_manualTwitterData.AdValueEquivalent" />
            </div>
            <div class="form-group">
                <label>Positif (%)</label>
                <input type="number" class="sp-input" @bind="_manualTwitterData.SentimentPositive" min="0" max="100" />
            </div>
            <div class="form-group">
                <label>Neutre (%)</label>
                <input type="number" class="sp-input" @bind="_manualTwitterData.SentimentNeutral" min="0" max="100" />
            </div>
        </div>
        <button class="btn-save" @onclick="SaveManualTwitterData">
            Appliquer
        </button>
    </div>
}
```

**Méthode** :
```csharp
private void SaveManualTwitterData()
{
    if (_event != null)
    {
        var negativeRatio = Math.Max(0, 100 - _manualTwitterData.SentimentPositive - _manualTwitterData.SentimentNeutral);
        
        _event.TwitterAnalytics = new TwitterAnalytics
        {
            TotalTweets = _manualTwitterData.TotalTweets,
            TotalEngagement = _manualTwitterData.TotalEngagement,
            EstimatedImpressions = _manualTwitterData.EstimatedImpressions,
            AdValueEquivalent = _manualTwitterData.AdValueEquivalent,
            ViralMultiplier = 1.5m,
            SentimentRatio = new Dictionary<string, double>
            {
                { "Positif", _manualTwitterData.SentimentPositive },
                { "Neutre", _manualTwitterData.SentimentNeutral },
                { "Négatif", negativeRatio }
            }
        };
        
        _showTwitterManualForm = false;
        StateHasChanged();
    }
}
```

---

### 8. Styles CSS

```css
/* Section action icons (next to h3) */
.section-title {
    display: inline-flex;
    align-items: center;
    gap: 0.5rem;
}

.section-action-icon {
    width: 16px;
    height: 16px;
    opacity: 0.6;
    filter: brightness(0) saturate(100%) invert(85%) sepia(6%) saturate(19%) hue-rotate(194deg) brightness(94%) contrast(93%);
}

/* Icon-only action buttons */
.demo-action-buttons {
    display: inline-flex;
    gap: 0.5rem;
    margin-left: 1rem;
    vertical-align: middle;
}

.btn-icon-action {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    width: 32px;
    height: 32px;
    background: rgba(255, 255, 255, 0.05);
    border: 1px solid rgba(255, 255, 255, 0.1);
    border-radius: 6px;
    cursor: pointer;
    transition: all 0.2s;
    padding: 0;
}

.btn-icon-action:hover {
    background: rgba(255, 255, 255, 0.1);
    border-color: rgba(255, 255, 255, 0.25);
}

.btn-icon-action img {
    width: 18px;
    height: 18px;
    filter: brightness(0) saturate(100%) invert(85%) sepia(6%) saturate(19%) hue-rotate(194deg) brightness(94%) contrast(93%);
}

/* Loading Modal */
.loading-modal {
    position: fixed;
    top: 0;
    left: 0;
    width: 100vw;
    height: 100vh;
    background: rgba(5, 10, 20, 0.9);
    backdrop-filter: blur(12px);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 2000;
}

.loading-content {
    background: linear-gradient(145deg, rgba(30, 41, 59, 0.95), rgba(15, 23, 42, 0.98));
    border: 1px solid rgba(204, 201, 220, 0.2);
    border-radius: 16px;
    padding: 2rem 2.5rem;
    text-align: center;
    min-width: 320px;
}

.spinner-large {
    width: 48px;
    height: 48px;
    border: 3px solid rgba(204, 201, 220, 0.2);
    border-top-color: var(--lavender);
    border-radius: 50%;
    animation: spin 1s linear infinite;
    margin: 0 auto 1.25rem;
}

.loading-text {
    color: var(--white);
    font-size: 1rem;
    font-weight: 600;
    margin-bottom: 1.25rem;
}

.loading-steps {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    text-align: left;
    padding: 0 1rem;
}

.step {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    color: var(--grey-text);
    font-size: 0.85rem;
    opacity: 0.5;
}

.step.completed {
    color: #4ADE80;
    opacity: 1;
}

.step.active {
    color: var(--lavender);
    opacity: 1;
}

/* Manual Form Panel */
.manual-form-panel {
    background: linear-gradient(145deg, rgba(30, 58, 138, 0.15), rgba(15, 30, 90, 0.1));
    border: 1px solid rgba(59, 130, 246, 0.2);
    border-radius: 12px;
    padding: 1.25rem;
    margin-top: 1rem;
    margin-bottom: 1.5rem;
}

.form-grid {
    display: grid;
    grid-template-columns: repeat(2, 1fr);
    gap: 0.875rem;
    margin-bottom: 1rem;
}

.form-group {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
}

.form-group label {
    color: var(--grey-text);
    font-size: 0.7rem;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    font-weight: 600;
}

.sp-input {
    background: rgba(255, 255, 255, 0.05);
    border: 1px solid rgba(255, 255, 255, 0.1);
    color: white;
    padding: 0.5rem 0.75rem;
    border-radius: 6px;
    font-size: 0.9rem;
}

.btn-save {
    width: 100%;
    background: linear-gradient(135deg, var(--accent-blue) 0%, #2563EB 100%);
    color: white;
    border: none;
    padding: 0.625rem 1.25rem;
    border-radius: 8px;
    font-weight: 600;
    font-size: 0.9rem;
    cursor: pointer;
    transition: transform 0.2s, box-shadow 0.2s;
}

.btn-save:hover {
    transform: translateY(-1px);
    box-shadow: 0 8px 12px -3px rgba(37, 99, 235, 0.3);
}

@keyframes spin {
    to { transform: rotate(360deg); }
}
```

---

## 🎨 Notes de Design

- Utiliser les variables CSS existantes (`--lavender`, `--accent-blue`, `--grey-text`)
- Conserver le style "glassmorphism" de l'application
- Animations fluides avec transitions CSS
- Responsive design (grilles adaptatives)

---

**Prêt pour validation !** 🚀
