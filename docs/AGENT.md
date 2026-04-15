# 📘 **agent.md — Document de référence pour IA développeuse**  
*Projet : SponsorPulse — MVP*  
*Version : 1.0*

---

# #️⃣ 1. **Description du projet**

SponsorPulse est un outil SaaS permettant aux organisateurs d’événements eSport de générer automatiquement un **rapport sponsor professionnel**.  
Le rapport inclut :

- données du stream (Twitch/YouTube)  
- analyse automatique  
- storytelling généré par IA  
- recommandations sponsor  
- PDF premium exportable  

🎯 **Objectif principal :**  
Aider les organisateurs à **fidéliser leurs sponsors** grâce à un reporting clair, visuel et narratif.

---

# #️⃣ 2. **Contexte & Enjeux**

## Contexte
Le marché eSport manque d’outils professionnels pour :

- mesurer l’impact sponsor  
- produire des rapports fiables  
- automatiser la collecte de données  
- standardiser la présentation des résultats  

Les organisateurs perdent du temps et manquent de crédibilité auprès des sponsors.

## Enjeux
- Automatiser un processus chronophage  
- Professionnaliser le reporting sponsor  
- Améliorer la fidélisation sponsor  
- Offrir un outil simple, rapide et premium  
- Créer une base solide pour une plateforme plus large (V2/V3)

---

# #️⃣ 3. **Backlog complet du MVP (avec priorisation)**

## 🟩 **P0 — Indispensable (MVP strict)**  
Ces tâches doivent être réalisées dans cet ordre.

### **1. Base du projet**
- Setup solution .NET  
- Mise en place Clean Architecture
- Création des dossiers : Domain / Application / Infrastructure / Presentation  
- Modèle Event (Domain)  
- Repositories (interfaces)  

### **2. Création d’un événement**
- Formulaire création événement  
- Validation des champs  
- Stockage DB  

### **3. Collecte données stream**
- Intégration API Twitch  
- Normalisation des données  
- Stockage dans DB  
- Saisie manuelle fallback  

### **4. Upload médias**
- Upload photos → Blob Storage  
- Stockage des URLs  

### **5. Analyse IA**
- Insights automatiques  
- Moments forts  
- Storytelling (intro + impact sponsor)  
- Recommandations  

### **6. Génération PDF**
- Template HTML  
- Génération PDF (QuestPDF ou DinkToPdf)  
- Upload PDF dans storage  
- Lien de téléchargement  

### **7. UX/UI essentielles**
- Dashboard  
- Page événement  
- Page preview rapport  
- Bouton “Analyser l’événement”  
- Bouton “Exporter PDF”  

---

# #️⃣ 4. **Stack technique**

## Stack recommandée
### **Frontend**
- Blazor WebAssembly  
- .NET 10 
- TailwindCSS

### **Backend**
- Azure Functions (isolated .NET 10)  
- Minimal API pour endpoints simples  

### **Base de données**
- SQLLite en local et Turso en production

### **Stockage**
- CloudFare R2

### **IA**
- Appels API LLM  
- Prompts structurés

### **PDF**
- QuestPDF  

---

# #️⃣ 5. **Choix architecturaux**

## Clean Architecture
Couches :

- **Domain** : modèles, règles métier  
- **Application** : use cases, services  
- **Infrastructure** : DB, storage, IA, PDF  
- **Presentation** : Blazor  

🎯 **Objectif :**  
Un code modulaire, testable, évolutif, et facile à maintenir.

---

# #️⃣ 6. **Préférences de codage (à respecter)**

L’agent IA doit respecter les règles suivantes :

### **Style**
- Code clair, lisible, maintenable
- Pas de magique string, utilise des enums si nécessaire
- Pas de magic number, utilise des constants
- Nom de méthodes parlant, par exemple eviter SetStatus(true) utilisez Activate()
- Préférer la simplicité à l’optimisation prématurée 
- Optimiser uniquement la partie IA et PDF
- Respect strict de la Clean Architecture (uniquement 1 projet, utiliser des dossier pour séparer les couches)
- Suivre l'architecture d'un monolithe modulaire

### **C#**
- Utiliser les records pour les modèles immuables  
- Utiliser les interfaces pour les services et repositories
- Utiliser des modules public pour la communication entre services.
- Pas de repositories générique. Exposer uniquement les données nécessaires 
- Utilise la HttpClientFactory pour gérer les client Http. Utiliser aussi les clients typés
- Utiliser async/await partout  
- Utiliser les fonctionalités de C#13
- Utiliser les constructeurs primaires
- Utiliser le pattern matching
- Utiliser le problem details format pour les réponses d'api
- Utiliser les early exit principles
- Retourner des arrays/list, strings vides plutôt que null
- Utiliser des pures functions quand c'est possibles
- Utiliser le GUID version 7 pour les ID
- Utiliser une classe d'extension pour ajouter les endpoints
- Pas de logique métier dans les contrôleurs/endpoints  

### **Blazor**
- Composants réutilisables  
- Pas de logique lourde dans les pages  
- Injection de dépendances propre  
- Pas de valeurs en dur. Les composants doivent gérer les paramètres vides ou null


### **PDF**
- Utiliser des templates HTML vers PDF

---

# #️⃣ 7. **Design System & UI**

l'agent doit implementer l'UI décrit par les instructions ci-dessous : 

**Instructions**

Votre tâche consiste à implémenter en suivant le cahier des charges de conception et de développement ci-dessous. Implémentez-le minutieusement, étape par étape, et utiliser les tokens de design standard inclus dans Tailwind Css au lieu de valeur en dur.

Pour les couleurs et les polices d'écritures, utiliser les valeurs défifnies présentes dans @tailwind.config.js, par exemple, 'bg-primary-500' etc. au lieu des valeurs en dur pour le noir/gris. Pour les autres couleurs, respecter les valeurs du JSON

- responsive (background occuapnt tout la largeur disponible avec le body centré pour les écrans larges )
- *Ne pasw utliser* des strings magiques, pas de valeurs en px. Remplacer par les classses Tailwind le plus possible.
- Tous les composant doivent suivre le design system

**Prompt**

Guide d'implémentation UI pour le développeur

Ce guide définit les règles visuelles et l'expérience utilisateur à reproduire pour l'interface de personnalisation des cartes de SponsorPulse.
1. Structure Spatiale et Layout

L'interface repose sur un sidebar fixe à gauche et un panneau de contenu principal flexible. Le contenu doit respirer : utilisez des espacements larges pour éviter la surcharge cognitive.

    Comportement Responsif : Sur mobile (sm), le sidebar devient un menu escamotable (burger). Sur tablette (md à lg), le layout passe d'une structure en deux colonnes (Aperçu de carte / Contrôles) à une seule colonne empilée.

2. Palette de Couleurs et Hiérarchie

L'ambiance est volontairement sombre et immersive.

    Fond : Utilisez le Ink Black comme base profonde. Appliquez un léger dégradé radial vers le centre avec le Deep Space Blue pour donner de la dimension.

    Surfaces : Les conteneurs secondaires (tabs, panels) utilisent le Charcoal Blue avec une opacité réduite pour créer un effet de verre (Glassmorphism).

    Actions : Le Lavender est réservé aux boutons d'appel à l'action principaux (Apply changes) et aux éléments actifs (sliders, icônes sélectionnées).

3. Composants Clés

    Sélecteur d'Apparence : La galerie d'images d'arrière-plan doit présenter une bordure lumineuse lorsqu'un item est sélectionné. Les options premium sont marquées par un cadenas et une icône couronne.

    Le Simulateur de Carte : C'est la pièce centrale. Elle doit refléter en temps réel les changements (Échelle de l'image, rotation, changement de police). La carte utilise un effet de flou d'arrière-plan (backdrop-filter) pour superposer proprement les informations textuelles.

    Typographie : Les polices varient entre le gras pour les titres et une police mono-espacée pour les numéros de carte afin d'assurer la lisibilité. Utilisez le gris clair pour le texte secondaire.

4. Micro-interactions

    Sliders : Les curseurs doivent afficher la valeur numérique au-dessus du "thumb" lors du déplacement.

    États de Survol : Tous les boutons interactifs doivent avoir une transition douce de 200ms, passant du transparent vers une légère opacité de Charcoal Blue.

**Specification design**
{
  "project": "SponsorPulse Dashboard",
  "theme_mode": "Dark",
  "layout": {
    "sidebar_width": "260px",
    "main_content_padding": "32px",
    "border_radius": {
      "card": "16px",
      "button": "8px",
      "input": "12px"
    }
  },
  "design_tokens": {
    "colors": {
      "primary": "#0C1821", // Ink Black
      "secondary": "#1B2A41", // Deep Space Blue
      "accent": {
        "lavender": "#CCC9DC", // Pour les actions principales
        "charcoal_blue": "#324A5F" // Pour les états de survol/cartes secondaires
      },
      "neutral": {
        "grey_text": "#94A3B8",
        "grey_muted": "#475569",
        "white": "#F8FAFC"
      }
    },
    "breakpoints": {
      "sm": "640px",
      "md": "768px",
      "lg": "1024px",
      "xl": "1280px",
      "2xl": "1536px"
    }
  },
  "components": {
    "sidebar": {
      "icons": "Outline style",
      "active_state": "Stroke-left (Green accent)",
      "navigation": ["Dashboard", "Transfers", "Your cards", "Settings"]
    },
    "tabs_navigation": {
      "style": "Ghost pills",
      "items": ["General", "Appearance", "Blocked", "Accessibility"],
      "active_bg": "rgba(30, 41, 59, 0.5)" // Charcoal blue translucide
    },
    "card_preview": {
      "aspect_ratio": "vertical",
      "features": [
        "Glassmorphism overlay",
        "Dynamic background image",
        "NFC & Chip icons",
        "Card number mask"
      ]
    },
    "controls": {
      "sliders": "Custom lavender track with value indicators",
      "font_selector": "6-grid selection with typography previews",
      "image_gallery": "Horizontal scroll with lock icons for premium features"
    }
  },
  "effects": {
    "gradients": {
      "background_mesh": "Radial-gradient(at 50% 50%, secondary 0%, primary 100%)",
      "card_accent": "linear-gradient(135deg, lavender 0%, charcoal_blue 100%)"
    },
    "shadows": "Soft outer glow on active elements using accent colors"
  }
}


---

# #️⃣ 8. **Limites de l’agent IA**

L’agent IA doit respecter les contraintes suivantes :

### ❌ Ne doit pas :
- inventer des données  
- modifier la structure du projet sans raison  
- ignorer la Clean Architecture  
- ignorer la VSA  
- générer du code non testable  
- créer des dépendances circulaires  
- utiliser des librairies non validées  
- changer la stack technique sans justification  
- créer de repositories génériques

### ✔️ Doit :
- suivre strictement le backlog MVP  
- respecter les priorités  
- produire un code propre et modulaire  
- documenter les endpoints  
- générer des prompts IA cohérents  
- produire un PDF stable et lisible  
- respecter le design system  

---

# #️⃣ 9. **Objectif final de l’agent IA**

L’agent IA doit produire un MVP fonctionnel permettant :

1. De créer un événement  
2. D’importer les données Twitch  
3. D’ajouter des photos  
4. De générer une analyse IA  
5. De générer un PDF premium  
6. De naviguer via un dashboard simple  

🎯 **Temps cible pour générer un rapport : < 10 minutes**

---
