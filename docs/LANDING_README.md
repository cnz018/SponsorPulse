# Landing Page - SponsorPulse

## 📋 Vue d'ensemble

La landing page de SponsorPulse est une page d'accueil moderne et responsive conçue pour présenter la plateforme aux utilisateurs potentiels. Elle suit le design system défini dans `AGENT.md` avec une palette de couleurs sombre et des effets glassmorphism.

## 🎨 Architecture & Fichiers

### Fichiers principaux

- [Landing.razor](Pages/Landing.razor) - Page d'accueil principale (`@page "/"`)
- [EmptyLayout.razor](Layout/EmptyLayout.razor) - Layout vide sans navigation par défaut
- [landing.css](../wwwroot/css/landing.css) - Styles CSS personnalisés
- [tailwind-overrides.css](../wwwroot/css/tailwind-overrides.css) - Classes Tailwind CSS personnalisées

### Configuration Tailwind

- Tailwind CSS est intégré via CDN dans [App.razor](App.razor)
- Configuration: `tailwind.config.js` (racine du projet)
- Configuration PostCSS: `postcss.config.js`
- Fichier d'entrée CSS: `tailwind.input.css`

## 🎯 Sections de la Landing Page

### 1. Header / Navigation
- Logo SponsorPulse
- Menu de navigation central (Produit, Solutions, FAQ, Support)
- Boutons CTA (Connexion, Inscription)
- Responsive: Menu caché sur mobile

### 2. Hero Section
- Titre massif et accrocheur
- Sous-titre expliquant la proposition de valeur
- Deux boutons CTA avec styles distincts
- Indicateur de scroll animé

### 3. Visual Asset Section
- Dégradé radial custom (Deep Space Blue → Lavender)
- Cartes flottantes avec métriques:
  - **Twitter Reach**: 245K impressions
  - **Sentiment**: 94% positif
  - **Ad Value Equivalent**: $18.5K USD
- Animation float continue sur les cartes
- Stats résumé (100% IA, <10min, 5+ sources)

## 🎨 Design System

### Couleurs
```css
--color-ink-black: #0C1821;           /* Fond principal */
--color-deep-space-blue: #1B2A41;     /* Gradients */
--color-charcoal-blue: #324A5F;       /* Cartes secondaires */
--color-lavender: #CCC9DC;            /* Accents */
--color-grey-text: #94A3B8;           /* Texte secondaire */
--color-grey-muted: #475569;          /* Texte mutré */
--color-white: #F8FAFC;               /* Texte principal */
```

### Effets & Styles
- **Glassmorphism**: Blend de transparence, backdrop-blur et borders blanches
- **Gradients**: Radial et linéaire pour les backgrounds
- **Animations**: Float 6s sur les cartes flottantes
- **Border Radius**: 16px (cards), 8px (boutons), 12px (inputs)

## 📱 Responsive Design

### Breakpoints Tailwind
- **SM** (640px): Menu caché, layout empilé
- **MD** (768px): Ajustement des espacements
- **LG** (1024px): Layout complet visible

### Comportement Mobile
- Header avec logo compact
- Boutons CTA empilés verticalement
- Cartes flottantes adaptées à l'écran
- Padding et marges optimisées

## 🚀 Utilisation & Développement

### Ajouter une nouvelle section

```razor
<section class="relative px-4 py-32">
    <div class="max-w-6xl mx-auto">
        <!-- Votre contenu ici -->
    </div>
</section>
```

### Ajouter un composant réutilisable

1. Créer un fichier `.razor` dans `Components/`
2. Utiliser les classes Tailwind et les variables CSS du design system
3. Importer dans Landing.razor avec `@using SponsorPulse.Presentation.Components`

### Exemples de classes personnalisées

- `.btn-cta-primary` - Bouton noir solide
- `.btn-cta-secondary` - Bouton outline blanc
- `.metric-card` - Carte avec glassmorphism
- `.hero-title` - Titre hero responsive
- `.glass-card` - Carte générique avec verre

## 🔧 Scripts NPM

```bash
# Regarder les changements Tailwind
npm run tailwind:watch

# Compiler Tailwind CSS
npm run tailwind:build
```

## ✨ Points clés à conserver

1. ✅ Utilisez le design system (couleurs, espacement, border-radius)
2. ✅ Préférez les classes Tailwind aux styles inline
3. ✅ Assurez la responsivité sur all breakpoints
4. ✅ Maintenez l'esthétique minimaliste et haut de gamme
5. ✅ Testez les animations et transitions sur navigateurs

## 📚 Ressources

- [Tailwind CSS Documentation](https://tailwindcss.com)
- [Design System (AGENT.md)](../../docs/AGENT.md)
- [Référence Twinkle.ai](https://twinkle.ai) - Inspiration visuelle

---

**Dernière mise à jour**: 2026-02-20
