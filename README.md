# OCP Phosboucraa — Gestion de la production minière

## Contexte

Ce dépôt de travail rassemble un prototype réalisé dans le cadre d'un stage. Il couvre la gestion opérationnelle de la production minière, une couche de reporting SQL, un tableau de bord ASP.NET Core et la préparation d'un modèle Power BI. Les données documentées comme données de démonstration sont synthétiques et ne doivent pas être interprétées comme des données opérationnelles officielles d'OCP.

## Objectif du stage

L'objectif est de proposer une chaîne cohérente allant de la base SQL Server aux interfaces de gestion et de reporting :

- gérer les voyages, arrêts camion et analyses qualité ;
- appliquer des workflows et autorisations explicites ;
- exposer des vues SQL stables pour le reporting ;
- fournir des indicateurs et graphiques en lecture seule ;
- permettre une analyse Power BI filtrable par date ;
- conserver une documentation technique et fonctionnelle vérifiable.

## Périmètre fonctionnel

- voyages : consultation, création, correction et workflow de validation ;
- arrêts camion : consultation, saisie, clôture et validation ;
- qualité : consultation, saisie et correction ;
- camions et conducteurs : consultation et vues de performance factuelles ;
- référentiels compatibles : gestion administrative contrôlée ;
- tableau de bord : six KPI, cinq graphiques et filtre de période inclusif ;
- reporting : vues détaillées et agrégées destinées à l'application et à Power BI.

## Architecture applicative

L'application suit une séparation en couches :

```text
ASP.NET Core MVC / Razor
        ↓
OcpProduction.Application (contrats, DTO, critères)
        ↓
OcpProduction.Infrastructure (EF Core Database-First, services)
        ↓
SQL Server OCP_Mining_Prototype
```

Le tableau de bord suit le flux :

```text
DashboardController
  → IDashboardReadService
  → vues SQL de reporting en lecture seule
  → DashboardDto / DashboardViewModel
  → Razor et Chart.js
```

## Technologies

- .NET 10 et ASP.NET Core MVC ;
- Entity Framework Core 10, approche Database-First ;
- SQL Server ;
- Razor et Bootstrap 5 ;
- Chart.js, chargé uniquement sur la page du tableau de bord ;
- xUnit pour les tests automatisés ;
- Power BI Desktop pour le modèle analytique.

## Base de données et reporting

La base attendue est `OCP_Mining_Prototype`. Le schéma est considéré comme gelé : aucun changement ne doit être effectué par migration EF Core. Les scripts de création, de données synthétiques et de validation se trouvent dans `sql/`. Les définitions de reporting dédiées se trouvent dans `reporting/`.

Les vues détaillées utilisées pour les KPI filtrables sont notamment :

- `dbo.vw_TripSummary` ;
- `dbo.vw_StopAnalysis` ;
- `dbo.vw_QualityAnalysis`.

Les vues agrégées et de performance complètent les analyses globales, journalières, par motif, camion et conducteur.

## Modules du tableau de bord

Les cartes affichent :

- nombre de voyages ;
- tonnage des voyages terminés ;
- nombre d'arrêts ;
- durée totale des arrêts ;
- nombre d'analyses qualité ;
- voyages en attente de validation.

Les graphiques présentent :

- voyages et tonnage par jour ;
- répartition des voyages par statut ;
- durée des arrêts par motif ;
- activité des camions ;
- évolution quotidienne du P2O5.

Les requêtes du tableau de bord sont en lecture seule, utilisent les vues de reporting et appliquent le filtre de dates aux colonnes métier appropriées.

## État actuel

- architecture en couches et modèle Database-First en place ;
- modules opérationnels des Sprints 0 à 4 documentés dans le dépôt applicatif ;
- tableau de bord Sprint 5 implémenté localement et encore non validé par un commit ;
- politique dédiée `CanViewDashboard` préparée localement ;
- scripts SQL et reporting disponibles ;
- dossier Power BI présent, mais aucun fichier PBIX n'a été trouvé lors de l'audit du 24 juillet 2026 ;
- une référence d'intégration attend encore une mise à jour contrôlée après la correction autorisée de `TruckStopID 153`.

## Structure actuelle

```text
OCP Internship/
├── Application/
│   └── OcpProductionManagement_Sprint0_Starter/
│       └── OcpProductionManagement_Starter/  # dépôt Git et solution .NET
├── backups/                                  # archives historiques à revoir
├── docs/                                     # documentation fonctionnelle et technique
├── Power BI/                                 # documentation Power BI ; PBIX absent
├── reporting/                                # vues SQL et validations reporting
├── Research/                                 # documents de recherche à revoir avant publication
├── sql/                                      # création, données synthétiques et validation SQL
└── OCP_Mining_Prototype_Schema.sql            # export de schéma à conserver sous revue
```

## Prérequis locaux

- Windows avec PowerShell ;
- SDK .NET 10 ;
- SQL Server local ou accessible ;
- accès autorisé à une base `OCP_Mining_Prototype` compatible ;
- Power BI Desktop pour ouvrir ou créer le rapport.

## Configuration SQL Server

Ne placez jamais de connexion privée dans un fichier suivi par Git. Configurez-la avec ASP.NET Core User Secrets :

```powershell
dotnet user-secrets init --project "src/OcpProduction.Web"
dotnet user-secrets set "ConnectionStrings:OcpDatabase" "<connexion SQL Server locale>" --project "src/OcpProduction.Web"
```

Le fichier User Secrets est stocké par .NET hors du dépôt. Pour un poste Windows utilisant l'authentification intégrée, configurez une chaîne locale conforme à la politique de sécurité de l'environnement, sans l'écrire dans la documentation.

## Exécuter l'application

Depuis le dépôt applicatif :

```powershell
cd "Application/OcpProductionManagement_Sprint0_Starter/OcpProductionManagement_Starter"
dotnet restore
dotnet build OcpProductionManagement.sln
dotnet run --project src/OcpProduction.Web
```

Utilisez ensuite l'adresse locale affichée par ASP.NET Core.

## Exécuter les tests

```powershell
dotnet build OcpProductionManagement.sln --no-restore --configuration Debug
dotnet test OcpProductionManagement.sln --no-build --configuration Debug
```

Les tests d'intégration exigent SQL Server et la version de données attendue. Une correction de données autorisée peut nécessiter une mise à jour explicite des valeurs de référence, sans affaiblir les assertions.

## Power BI

Le rapport approuvé doit être conservé dans `Power BI/` sans fichier temporaire ni information d'identification. Ouvrez le PBIX avec Power BI Desktop, vérifiez la source SQL, puis actualisez les vues de reporting avec une authentification locale ou une source de données gérée. Consultez `Power BI/README.md`.

## Hypothèses importantes

- les données de démonstration sont synthétiques ;
- les identifiants techniques ne sont pas supposés séquentiels ;
- un KPI filtrable doit utiliser la vue détaillée et sa date métier ;
- les arrêts et analyses non liés restent inclus lorsqu'ils appartiennent au grain validé ;
- aucun seuil P2O5, objectif, score ou classement ne doit être inventé ;
- une valeur atypique n'est ni supprimée ni masquée sans correction de données autorisée et traçable.

## Rôles et permissions

Le prototype définit les rôles `AgentProduction`, `AgentQualite`, `Superviseur`, `ResponsableProduction` et `Administrateur`. Les accès sont attribués par politiques d'autorisation. Le tableau de bord utilise une politique dédiée afin de ne pas étendre automatiquement les permissions des autres pages de reporting.

Les identifiants de comptes locaux ne doivent pas être documentés dans le dépôt destiné à publication.

## Limites connues

- prototype de stage, sans preuve de préparation à une production réelle ;
- base SQL Server locale requise pour les tests d'intégration ;
- données et classifications de démonstration non officielles ;
- PBIX absent du dossier audité ;
- absence de pipeline CI/CD et de procédure de déploiement validée ;
- archives historiques et documents de recherche à examiner manuellement ;
- secrets de démonstration actuellement présents dans le dépôt applicatif et à retirer ou externaliser avant publication.

## Sécurité

- ne jamais committer de secret, certificat, sauvegarde de base ou export de données brutes ;
- conserver les connexions dans User Secrets ou un gestionnaire de secrets approuvé ;
- scanner les changements avant chaque publication ;
- désactiver toute authentification de démonstration avant un environnement réel ;
- traiter un PBIX comme potentiellement sensible lorsqu'il contient des données importées.

## Politique de sauvegarde

Les fichiers `.bak`, `.mdf` et `.ldf` doivent rester hors Git. Conservez-les dans un stockage local ou institutionnel protégé, avec contrôle d'accès et politique de rétention. Le dépôt doit privilégier les scripts reproductibles de schéma, de données synthétiques approuvées, de reporting et de validation.

Les scripts de restauration de corrections unitaires doivent être relus avant publication afin de vérifier qu'ils ne contiennent aucune donnée confidentielle.

## Contribution et workflow

1. partir d'un état Git compris et documenté ;
2. travailler par changement limité ;
3. ne jamais modifier directement les entités EF générées ;
4. ne créer aucune migration pour le schéma gelé ;
5. lancer build, tests ciblés et suite complète ;
6. exécuter `git diff --check` et relire les fichiers à publier ;
7. utiliser une branche et une revue avant fusion ;
8. ne publier qu'après résolution des éléments de sécurité et de revue manuelle.

