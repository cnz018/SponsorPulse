# Backlog — SponsorPulse

Date: 2026-04-06

Résumé: backlog priorisé pour mise en production. Sections principales: sécurité, CI/CD, DB, tests, observabilité, front/perf, et workflow utilisateur.

## Critical Security
- Supprimer secrets committés (voir `SponsorPulse/appsettings.json`) et rotate credentials.
- Retirer les `publish/` et `bin/` contenant des `appsettings.json` publiés.
- Utiliser un secret manager (GitHub Secrets / Azure Key Vault).

## CI / Build / Deploy
- Ajouter `dotnet test` dans le workflow CI (.github/workflows/deploy.yml).
- Vérifier que les secrets CI (FTP_*) sont dans GitHub Secrets et restreindre accès.
- Remplacer FTP par déploiement plus sûr (SFTP avec clé, ou pipeline déploy vers hosting provider).

## Database & Data
- Documenter la stratégie de persistance (SQLite → Postgres/SQL Azure en prod).
- Implémenter migrations EF Core et runbooks de migration et backup.

## Tests & Qualité
- Ajouter suites unitaires et tests d'intégration (API/DB).
- Activer scan dépendances (Dependabot) et corriger vulnérabilités.

## Observabilité & Ops
- Centraliser logs (ex: Azure Monitor / Seq) et ajouter health/readiness endpoints.
- Ajouter alerting basique (erreurs critiques, downtime, queue backlog).

## Frontend & Performance
- Vérifier build Tailwind et minification des assets (CDN, cache-control).
- Réduire `wwwroot/lib` si possible; audit bundles bootstrap/tailwind.

## Workflow Utilisateur (ajout demandé)
- Spécifier le flux utilisateur complet:
  - Création de compte (UI + API) — formulaire, validation, stockage.
  - Vérification email (envoi lien / token, confirmation).
  - Login (authentification + sessions/tokens).
  - Récupération mot de passe (reset flow + sécurité).
  - Lier le `Dashboard` aux utilisateurs (afficher données propres au user).
  - Assigner / lier `Events` aux utilisateurs (ownership, permissioning).
  - Migrations du modèle utilisateur (nouvelle table/propriétés).
  - Tests du flux auth utilisateur (unitaires + e2e).
  - Sécuriser auth et sessions (refresh tokens, anti-CSRF, durée session).

---

## Notes / actions rapides
- Pour créer la page Notion, utilisez le script `scripts/create_notion_page.js` (nécessite `NOTION_TOKEN` et idéalement `NOTION_PARENT_PAGE_ID`).
- Si vous voulez que je publie directement la page, fournissez `NOTION_PARENT_PAGE_ID` (ID de la page parent). Sans parent, le script tentera la création dans le workspace, mais Notion API peut demander un `page_id`.

