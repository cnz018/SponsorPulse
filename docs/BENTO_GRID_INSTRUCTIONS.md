# 📋 Instructions - Bento Grid Features Section

## 🎯 Objectif

Remplacer la section Features actuelle (LumexTabs) par un layout Bento Grid moderne.

---

## 📍 Emplacement

**Fichier :** `Presentation/Pages/Landing.razor`  
**Ligne de début :** ~285 (section id="features")  
**Ligne de fin :** ~400 (avant section FAQ)

---

## 🔧 Remplacement Complet

### Supprimer :
```razor
<!-- Features Section -->
<section id="features" class="relative px-4 py-32 border-t border-white/10">
    <div class="max-w-6xl mx-auto">
        <!-- Section Header -->
        <div class="text-center mb-20">
            <h2 class="text-4xl md:text-5xl font-black text-white mb-4">
                Transformez vos données eSport en <span class="text-gradient">contrats signés</span>
            </h2>
            <p class="text-[#94A3B8] max-w-2xl mx-auto">
                Une plateforme complète pour transformer vos données de streams en arguments de vente convaincants.
            </p>
        </div>

        <!-- Features Tabs -->
        <div class="w-full max-w-5xl mx-auto py-8">
            <LumexTabs Variant="@TabVariant.Underlined" Color="@ThemeColor.Info" Size="@Size.Large">
                <!-- TOUS LES TABS À SUPPRIMER -->
            </LumexTabs>
        </div>
    </div>
</section>
```

### Remplacer par :
```razor
<!-- Features Section - Bento Grid -->
<section id="features" class="relative px-4 py-20 border-t border-white/10">
    <div class="max-w-screen-xl mx-auto">
        <!-- Section Header -->
        <div class="text-center mb-16">
            <h2 class="text-3xl md:text-4xl font-black text-white mb-4">
                Une plateforme, <span class="text-gradient">quatre piliers</span>
            </h2>
            <p class="text-[#94A3B8] max-w-2xl mx-auto">
                Transformez vos données eSport en arguments de vente convaincants.
            </p>
        </div>
        
        <!-- Bento Grid -->
        <div class="bento-grid">
            <!-- Card 1: Analyse Streams (Large - 2x2) -->
            <div class="bento-card col-span-3 lg:col-span-2 row-span-2 group">
                <div class="bento-content">
                    <span class="badge">Live Data</span>
                    <h3 class="text-2xl font-bold text-white mt-4">Analyse de Stream</h3>
                    <p class="text-[#94A3B8] mt-2">
                        Extraction en temps réel des pics d'audience et de l'engagement du chat.
                    </p>
                    <ul class="feature-list mt-4">
                        <li class="feature-list-item">
                            <img src="https://api.iconify.design/ic:outline-check-circle.svg" class="check-icon" alt="" />
                            Pics d'audience en temps réel
                        </li>
                        <li class="feature-list-item">
                            <img src="https://api.iconify.design/ic:outline-check-circle.svg" class="check-icon" alt="" />
                            Engagement du chat
                        </li>
                        <li class="feature-list-item">
                            <img src="https://api.iconify.design/ic:outline-check-circle.svg" class="check-icon" alt="" />
                            Durée et fréquence des streams
                        </li>
                    </ul>
                </div>
                <div class="bento-visual">
                    <div class="visual-placeholder">
                        <img src="https://api.iconify.design/simple-icons:twitch.svg" class="visual-icon" alt="" />
                    </div>
                </div>
            </div>

            <!-- Card 2: Social Intelligence (Tall - 1x2) -->
            <div class="bento-card col-span-3 lg:col-span-1 row-span-2 group">
                <div class="bento-content">
                    <span class="badge badge-twitter">X / Twitter</span>
                    <h3 class="text-xl font-bold text-white mt-4">Social Intelligence</h3>
                    <p class="text-[#94A3B8] mt-2">
                        Mesure de portée et analyse de sentiment sur X/Twitter.
                    </p>
                    <ul class="feature-list mt-4">
                        <li class="feature-list-item">
                            <img src="https://api.iconify.design/ic:outline-check-circle.svg" class="check-icon" alt="" />
                            Analyse de hashtags
                        </li>
                        <li class="feature-list-item">
                            <img src="https://api.iconify.design/ic:outline-check-circle.svg" class="check-icon" alt="" />
                            Sentiment analysis
                        </li>
                        <li class="feature-list-item">
                            <img src="https://api.iconify.design/ic:outline-check-circle.svg" class="check-icon" alt="" />
                            Top influenceurs
                        </li>
                        <li class="feature-list-item">
                            <img src="https://api.iconify.design/ic:outline-check-circle.svg" class="check-icon" alt="" />
                            Répartition géographique
                        </li>
                    </ul>
                </div>
                <div class="bento-visual">
                    <div class="visual-placeholder">
                        <img src="https://api.iconify.design/simple-icons:x.svg" class="visual-icon" alt="" />
                    </div>
                </div>
            </div>

            <!-- Card 3: Storytelling IA (Small - 1x1) -->
            <div class="bento-card col-span-3 lg:col-span-1 group">
                <div class="bento-content">
                    <span class="badge badge-ai">IA</span>
                    <h3 class="text-xl font-bold text-white mt-4">Storytelling IA</h3>
                    <p class="text-[#94A3B8] mt-2">
                        Vos chiffres transformés en argumentaires percutants.
                    </p>
                </div>
                <div class="bento-visual">
                    <div class="visual-placeholder">
                        <img src="https://api.iconify.design/ic:outline-smart-toy.svg" class="visual-icon" alt="" />
                    </div>
                </div>
            </div>

            <!-- Card 4: Rapports PDF (Wide - 3x1) -->
            <div class="bento-card col-span-3 lg:col-span-3 group flex-row items-center justify-between">
                <div class="bento-content">
                    <span class="badge badge-pdf">Export</span>
                    <h3 class="text-xl font-bold text-white mt-4">Rapports PDF Premium</h3>
                    <p class="text-[#94A3B8] mt-2">
                        Exportez un dossier complet prêt pour vos rendez-vous sponsors.
                    </p>
                </div>
                <div class="p-4 bg-white/5 rounded-xl border border-white/10 flex items-center gap-3">
                    <img src="https://api.iconify.design/ic:outline-picture-as-pdf.svg" class="pdf-icon" alt="" />
                    <span class="text-white font-mono text-sm">RAPPORT_SPONSOR_v2.pdf</span>
                </div>
            </div>
        </div>
    </div>
</section>
```

---

## 🎨 Styles CSS à Ajouter

**Dans le `<style>` de Landing.razor** (vers la fin, avant `</style>`) :

```css
/* ── Bento Grid Styles ── */
.bento-grid {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    grid-auto-rows: minmax(180px, auto);
    gap: 1.5rem;
}

.bento-card {
    background: rgba(255, 255, 255, 0.03);
    border: 1px solid rgba(255, 255, 255, 0.08);
    border-radius: 1.5rem;
    padding: 2rem;
    position: relative;
    overflow: hidden;
    transition: all 0.3s ease;
    display: flex;
    flex-direction: column;
    justify-content: space-between;
}

.bento-card:hover {
    background: rgba(255, 255, 255, 0.05);
    border-color: rgba(255, 255, 255, 0.2);
    transform: translateY(-5px);
    box-shadow: 0 20px 40px rgba(0, 0, 0, 0.3);
}

.bento-content {
    flex: 1;
}

.bento-visual {
    margin-top: 1.5rem;
    display: flex;
    justify-content: center;
    align-items: center;
}

.visual-placeholder {
    width: 80px;
    height: 80px;
    background: rgba(204, 201, 220, 0.1);
    border-radius: 12px;
    display: flex;
    align-items: center;
    justify-content: center;
}

.visual-icon {
    width: 48px;
    height: 48px;
    filter: brightness(0) saturate(100%) invert(84%) sepia(27%) saturate(744%) hue-rotate(356deg);
}

.badge {
    background: rgba(204, 201, 220, 0.1);
    color: #CCC9DC;
    padding: 4px 12px;
    border-radius: 99px;
    font-size: 0.75rem;
    font-weight: 600;
    width: fit-content;
}

.badge-twitter {
    background: rgba(29, 155, 240, 0.15);
    color: #1D9BF0;
}

.badge-ai {
    background: rgba(34, 197, 94, 0.15);
    color: #4ADE80;
}

.badge-pdf {
    background: rgba(239, 68, 68, 0.15);
    color: #F87171;
}

.feature-list {
    list-style: none;
    padding: 0;
    margin: 0;
}

.feature-list-item {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    color: #94A3B8;
    font-size: 0.875rem;
    padding: 0.375rem 0;
}

.check-icon {
    width: 16px;
    height: 16px;
    filter: brightness(0) saturate(100%) invert(60%) sepia(35%) saturate(1200%) hue-rotate(95deg);
}

.pdf-icon {
    width: 24px;
    height: 24px;
    filter: brightness(0) saturate(100%) invert(44%) sepia(21%) saturate(1100%) hue-rotate(317deg);
}

/* Responsive */
@@media (max-width: 1024px) {
    .bento-grid {
        grid-template-columns: 1fr;
        grid-auto-rows: auto;
    }
    
    .bento-card {
        grid-column: span 1 !important;
        row-span: 1;
    }
}
```

---

## ✅ Checklist

- [ ] Supprimer section LumexTabs complète
- [ ] Ajouter nouveau code Bento Grid
- [ ] Ajouter styles CSS dans le `<style>` block
- [ ] Vérifier responsive (mobile = 1 colonne)
- [ ] Tester hover effects
- [ ] Build sans erreur

---

## 📊 Layout Final

```
Desktop (≥1024px) :
┌─────────────────────────────────────────────────────────────┐
│  Une plateforme, quatre piliers                             │
├──────────────────────┬──────────────────────────────────────┤
│  ┌────────────────┐  │  ┌──────────────┐                   │
│  │                │  │  │  Social      │                   │
│  │  Analyse       │  │  │  Intelligence│                   │
│  │  Stream        │  │  │  (X/Twitter) │                   │
│  │  (2x2)         │  │  │  (1x2)       │                   │
│  │                │  │  │              │                   │
│  └────────────────┘  │  └──────────────┘                   │
├──────────────────────┴──────────────────────────────────────┤
│  ┌────────────────┐  ┌─────────────────────────────────┐   │
│  │  Storytelling  │  │  Rapports PDF Premium           │   │
│  │  IA            │  │  (3x1 - horizontal)             │   │
│  │  (1x1)         │  │                                 │   │
│  └────────────────┘  └─────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘

Mobile (<1024px) :
┌─────────────────────────────────┐
│  Analyse Stream                 │
├─────────────────────────────────┤
│  Social Intelligence            │
├─────────────────────────────────┤
│  Storytelling IA                │
├─────────────────────────────────┤
│  Rapports PDF Premium           │
└─────────────────────────────────┘
```

---

**Note :** Cette modification est optionnelle pour le prototype fonctionnel. Le layout actuel avec Tabs fonctionne déjà.
