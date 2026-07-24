# Couche Reporting — OCP_Mining_Prototype

## 1. Objectif

Cette couche fournit à Power BI des contrats SQL stables, typés et orientés analyse. Power BI doit importer uniquement les vues `dbo.vw_*` documentées ici et ne doit pas interroger directement les tables opérationnelles.

Les vues détaillées conservent les identifiants nécessaires aux relations, filtres et analyses. Les vues agrégées exposent les KPI par entité ou au niveau global sans multiplier les lignes transactionnelles.

## 2. Architecture

| Vue | Grain | Source principale | Dashboard Power BI |
|---|---|---|---|
| `dbo.vw_TripSummary` | 1 ligne par voyage | `dbo.Trip` | Trips |
| `dbo.vw_TruckPerformance` | 1 ligne par camion | `dbo.Truck` | Truck Performance |
| `dbo.vw_DriverPerformance` | 1 ligne par conducteur | `dbo.Driver` | Driver Performance |
| `dbo.vw_StopAnalysis` | 1 ligne par arrêt | `dbo.TruckStop` | Stops |
| `dbo.vw_QualityAnalysis` | 1 ligne par résultat qualité | `dbo.QualityResult` | Quality |
| `dbo.vw_ExecutiveDashboard` | 1 ligne globale | Agrégations indépendantes | Executive |

`vw_TripSummary`, `vw_StopAnalysis` et `vw_QualityAnalysis` sont des vues détaillées. `vw_TruckPerformance`, `vw_DriverPerformance` et `vw_ExecutiveDashboard` sont des vues KPI agrégées.

## 3. Dépendances

- `vw_TripSummary` dépend des tables de référence, de `Trip`, de `TruckStop` et de `QualityResult`.
- `vw_TruckPerformance` dépend de `vw_TripSummary`, de `TruckStop`, de `QualityResult` et de `Trip`.
- `vw_DriverPerformance` dépend de `vw_TripSummary`, de `TruckStop`, de `Trip` et de `QualityResult`.
- `vw_StopAnalysis` dépend uniquement des tables de base.
- `vw_QualityAnalysis` dépend uniquement des tables de base.
- `vw_ExecutiveDashboard` dépend de `vw_TripSummary`, `vw_StopAnalysis`, `vw_QualityAnalysis` et des référentiels.

Les scripts historiques de `sql/04_create_reporting_views.sql` contiennent déjà des vues nommées `vw_TruckPerformance` et `vw_DriverPerformance`. Les scripts Reporting `02` et `03` les font évoluer avec `CREATE OR ALTER VIEW`. Après installation de cette couche, ne pas réexécuter `sql/04_create_reporting_views.sql` seul, car il rétablirait les anciennes définitions de ces deux vues.

## 4. Ordre d’exécution

Exécuter les scripts dans cet ordre :

1. `Reporting/01_vw_TripSummary.sql`
2. `Reporting/02_vw_TruckPerformance.sql`
3. `Reporting/03_vw_DriverPerformance.sql`
4. `Reporting/04_vw_StopAnalysis.sql`
5. `Reporting/05_vw_QualityAnalysis.sql`
6. `Reporting/06_vw_ExecutiveDashboard.sql`
7. `Reporting/07_validate_reporting_layer.sql`

Aucun installateur fondé sur `:r` n'est fourni : son comportement dépend du mode SQLCMD de SSMS. L'ordre explicite ci-dessus est portable entre SSMS, Azure Data Studio et `sqlcmd`.

## 5. Principales règles de calcul

### Voyages

- `IsCompleted = 1` uniquement pour `Trip.Status = 'Completed'`.
- `IsIncomplete = 1` pour tout autre statut, conformément aux vues historiques du projet.
- Les durées utilisent `DATEDIFF(MINUTE, StartTime, EndTime)` et restent `NULL` sans heure de fin.
- L'utilisation de charge vaut `Tonnage / RatedPayloadTonnes × 100` avec protection par `NULLIF`.

### Camions

- Tous les camions restent présents grâce à un départ depuis `dbo.Truck`.
- Les voyages sont agrégés depuis `vw_TripSummary`.
- Les arrêts sont agrégés directement depuis `TruckStop` afin d'inclure les arrêts sans `TripID`.
- Le compteur qualité inclut toutes les unités liées; les statistiques P2O5 sont conditionnelles à `P2O5Percent`. Elles sont calculées depuis `QualityResult` relié à `Trip`, ce qui produit une moyenne pondérée par résultat et non une moyenne de moyennes par voyage.
- Aucune disponibilité mécanique ou note de performance n'est calculée.

### Conducteurs

- Tous les conducteurs restent présents grâce à un départ depuis `dbo.Driver`.
- Un arrêt n'est attribué à un conducteur que via un `TruckStop.TripID` explicite puis `Trip.DriverID`.
- Les arrêts sans voyage ne sont attribués à aucun conducteur.
- Aucun classement permanent ou score arbitraire n'est exposé.

### Arrêts

- Tous les arrêts sont conservés, liés ou non à un voyage.
- `StopReason.IsPlanned` est la seule source de `PlannedUnplannedLabel`.
- La durée est `NULL` pour un arrêt ouvert ou une période incohérente; aucune durée négative n'est exposée.
- Le contexte conducteur, poste, chargement et destination provient uniquement du voyage lié.

### Qualité

- Tous les résultats et toutes les unités restent visibles.
- `P2O5Percent` est renseigné uniquement lorsque `QualityUnit = 'P2O5Percent'`.
- Aucun seuil de conformité officiel n'est créé.
- Le point de chargement directement porté par `QualityResult` est prioritaire; celui du voyage sert de repli.
- Le dernier résultat d'un voyage est déterminé par `AnalysisDate DESC`, puis `QualityResultID DESC`.

### Executive

- Les métriques voyages, arrêts, qualité, référentiels et couverture sont agrégées dans des CTE séparées.
- La vue retourne exactement une ligne, y compris lorsque les tables transactionnelles sont vides.
- `ActiveTrucks` et `ActiveDrivers` utilisent les indicateurs physiques `IsActive`.

## 6. Types et gestion des NULL

- Identifiants : types physiques `INT` ou `BIGINT` conservés.
- Compteurs : `BIGINT`.
- Tonnes et moyennes : `DECIMAL`.
- Pourcentages : `DECIMAL(10,2)`.
- P2O5 : `DECIMAL(10,3)`.
- Indicateurs : `BIT`.
- Dates : `DATE`; timestamps : `DATETIME2`.

Les compteurs et sommes valent zéro sans observation. Les moyennes, minimums, maximums, premières/dernières dates et pourcentages impossibles restent `NULL`.

## 7. Validation

`07_validate_reporting_layer.sql` contrôle :

- l'existence des six vues;
- les comptes correspondant à chaque grain;
- l'absence de doublons d'identifiants;
- la réconciliation des voyages, tonnages, arrêts et résultats qualité;
- la conservation des arrêts et résultats qualité non liés;
- les KPI Executive;
- les plages de pourcentage, durées négatives, compteurs et indicateurs `Has*`;
- un résumé final `PASS`/`FAIL` sans arrêt au premier échec.

## 8. Installation et réexécution

Dans SSMS, ouvrir les scripts dans l'ordre indiqué et les exécuter avec un compte autorisé à créer ou modifier des vues dans `OCP_Mining_Prototype`. Tous utilisent `CREATE OR ALTER VIEW` et sont réexécutables.

Avec `sqlcmd` et un certificat local approuvé explicitement :

```powershell
sqlcmd -S localhost -E -C -b -i "Reporting\01_vw_TripSummary.sql"
sqlcmd -S localhost -E -C -b -i "Reporting\02_vw_TruckPerformance.sql"
sqlcmd -S localhost -E -C -b -i "Reporting\03_vw_DriverPerformance.sql"
sqlcmd -S localhost -E -C -b -i "Reporting\04_vw_StopAnalysis.sql"
sqlcmd -S localhost -E -C -b -i "Reporting\05_vw_QualityAnalysis.sql"
sqlcmd -S localhost -E -C -b -i "Reporting\06_vw_ExecutiveDashboard.sql"
sqlcmd -S localhost -E -C -b -i "Reporting\07_validate_reporting_layer.sql"
```

L'option `-C` fait confiance au certificat de l'instance locale. Elle doit être évaluée selon la politique de sécurité de l'environnement cible.

## 9. Connexion Power BI

1. Dans Power BI Desktop, choisir **Obtenir les données > SQL Server**.
2. Renseigner le serveur SQL et la base `OCP_Mining_Prototype`.
3. Choisir Import, sauf besoin validé de DirectQuery.
4. Sélectionner uniquement les six vues Reporting.
5. Vérifier les types détectés : dates, nombres décimaux et booléens.
6. Masquer dans le modèle les clés techniques inutiles aux visuels, sans les supprimer.
7. Configurer l'actualisation et les identifiants dans le service Power BI.

## 10. Recommandations de modèle Power BI

- Utiliser `vw_TripSummary`, `vw_StopAnalysis` et `vw_QualityAnalysis` pour les pages détaillées et les filtres.
- Utiliser les vues de performance directement pour les cartes et tableaux par camion ou conducteur.
- Utiliser `vw_ExecutiveDashboard` pour les KPI globaux et éviter de sommer à nouveau ses valeurs sur une autre dimension.
- Créer une table calendrier Power BI couvrant `TripDate`, `StopDate` et `QualityDate`.
- Ne pas relier directement entre elles les trois vues détaillées par des relations plusieurs-à-plusieurs. Préférer des dimensions conformes ou des relations à sens unique basées sur les identifiants.
- Conserver les noms lisibles pour les visuels et les identifiants pour les relations.
- Créer les mesures Power BI seulement pour les besoins interactifs (contexte de filtre, classement dynamique), pas pour recalculer les KPI SQL déjà exposés.

## 11. Index existants utiles

- `IX_Trip_TruckID_TripDate`
- `IX_Trip_DriverID_TripDate`
- `IX_Trip_TripDate`
- `IX_TruckStop_TruckID_StartTime`
- `IX_TruckStop_TripID`
- `IX_QualityResult_TripID`
- `IX_QualityResult_AnalysisDate`
- `IX_QualityResult_LoadingPointID_AnalysisDate`

Aucun index n'a été ajouté. Si les volumes augmentent, des index couvrants sur les clés de jointure avec colonnes incluses de durée ou de qualité pourront être évalués, uniquement après analyse des plans d'exécution et des statistiques d'utilisation.

## 12. Limites connues

- Les données sont synthétiques et ne représentent pas des chiffres opérationnels officiels OCP.
- Le prototype ne contient ni période planifiée camion ni heures disponibles : aucune disponibilité fiable n'est calculable.
- Il ne contient ni carburant, ni distance, ni GPS, ni coût.
- Le modèle qualité est mono-métrique dans les données actuelles (`P2O5Percent`), même si la vue détaillée accepte d'autres unités futures.
- `TargetValue` et `QualityStatus` sont des aides de prototype; ils ne constituent pas des seuils officiels.
- Les arrêts sans `TripID` n'ont pas de contexte voyage ou conducteur attribuable de façon fiable.
- La qualité sans `TripID` peut être analysée par point de chargement, mais pas par camion, conducteur, poste ou destination.
