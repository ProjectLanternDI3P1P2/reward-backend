# Récupérer le contenu d'un coffre (Claim chest contents)

> Documentation de la fonctionnalité : ce qui a été codé, pourquoi, et comment la
> reprendre. Tous les noms de code sont en anglais ; ce document est en français
> comme la documentation du MCD.

## User story

> As a player, I want to retrieve the contents of a chest, so I can use the
> rewards I discover during the dungeon.

**Description :** Claim the contents of a chest.

### Critères d'acceptation et leur traduction dans le code

| # | Critère | Où c'est garanti | Test qui le prouve |
|---|---|---|---|
| 1 | Coffre avec récompenses + capacité suffisante → toutes les récompenses vont dans l'inventaire | `Chest.TransferTo` | `Handle_FilledChestAndEnoughCapacity_TransfersEveryRewardToTheInventory`, `ClaimContents_WhenChestIsFilled_TransfersEveryRewardAndEmptiesTheChest` |
| 2 | Transfert terminé → le coffre passe à l'état `Empty` | `Chest.TransferTo` renseigne `reward.hero_id` ; `Chest.State` en déduit `Empty` | `TransferTo_NonStackableReward_CreatesOneItemInstancePerUnit`, test d'intégration ci-dessus |
| 3 | Coffre `Empty` + nouvelle interaction → rien n'est généré ni transféré | `ClaimChestContentsCommandHandler` retourne tout de suite si `State == Empty` | `Handle_EmptyChest_ReturnsEmptyStateWithoutTransferringAnything`, `ClaimContents_WhenChestIsAlreadyEmpty_DoesNotTransferRewardsAgain` |

## Décision principale : pas de table `chest`, pas de migration

Le coffre lui-même (son identifiant, sa position, son apparition dans le donjon)
**appartient au service Donjon**. Ce service gère **son contenu** :

1. **Génération** (autre US, déjà mergée dans `dev`) : le service Donjon appelle le
   gRPC `RewardLootService.GenerateChestLoot` avec `(dungeonRunId, chestId, floor,
   difficulty)`. `GenerateChestLootCommandHandler` tire le contenu dans les tables de
   butin et l'enregistre dans `reward` / `reward_item`, avec une ligne
   `chest_loot_generation` qui relie `(dungeon_run_id, chest_id)` à la `reward`.
2. **Récupération** (cette US) : quand le joueur ouvre le coffre, le contenu est
   transféré dans son inventaire.

**Aucune migration, aucune modification du schéma** (vérifié avec
`dotnet ef migrations has-pending-model-changes` : aucun changement).

| Concept métier | Représentation dans la base |
|---|---|
| Le coffre `(dungeonRunId, chestId)` | Une ligne `chest_loot_generation` |
| Le contenu du coffre | La `reward` liée (`reward_key = chest:{runId}:{chestId}`) et ses `reward_item` |
| État `Filled` (plein) | `reward.hero_id` = `00000000-0000-0000-0000-000000000000` (aucun destinataire) |
| État `Empty` (vide) | `reward.hero_id` = le héros qui a récupéré le contenu |
| Objet transféré | `reward_item.item_instance_id` renseigné + nouvelle ligne `item_instance` |

Pourquoi `reward.hero_id` comme marqueur ? La génération crée la récompense **sans
destinataire** (`hero_id` vide) et directement au statut `APPLIED` ; aucune colonne
« récupéré » n'existe, et le schéma ne doit pas changer. Le héros qui récupère le
contenu devient donc le destinataire de la récompense : tant qu'il est vide, le
coffre est plein. Conséquence : `reward.status = APPLIED` signifie ici « généré »,
pas « récupéré ».

## Endpoint

```http
POST /api/v1/heroes/{heroId}/runs/{dungeonRunId}/chests/{chestId}/claim
```

Pas de corps de requête. Le `dungeonRunId` est obligatoire : la génération du butin
est identifiée par le couple `(run, chest)`, pas par le seul `chestId`.

### Réponses

| Code | Quand | Corps |
|---|---|---|
| `200 OK` | Coffre vidé maintenant **ou** déjà vide | `ClaimChestContentsResult` (voir ci-dessous) |
| `404 Not Found` | Aucun butin généré pour ce `(run, chest)`, ou aucun inventaire pour ce héros | ProblemDetails |
| `409 Conflict` | Capacité d'inventaire insuffisante (`InventoryCapacityExceededException`) | ProblemDetails, ex. *"The inventory has reached its item capacity of 40."* |
| `422 Unprocessable Entity` | `heroId`, `dungeonRunId` ou `chestId` vaut `00000000-0000-0000-0000-000000000000` | ValidationProblemDetails |

Les codes d'erreur viennent du `ExceptionHandlingMiddleware` existant, qui n'a pas
été modifié.

Exemple de réponse, premier appel :

```json
{
  "chestId": "7d1f0d4c-4f8e-4b0a-9f3e-2f1b8c9a6d10",
  "state": "EMPTY",
  "items": [
    { "itemInstanceId": "…", "itemId": "…", "quantity": 1 },
    { "itemInstanceId": "…", "itemId": "…", "quantity": 3 }
  ],
  "alreadyEmpty": false
}
```

Deuxième appel sur le même coffre : `"items": []`, `"alreadyEmpty": true`.
Le deuxième appel ne renvoie **pas** d'erreur : interagir avec un coffre vide
est un cas normal du jeu (critère 3). Un client peut donc relancer la requête
sans risque, par exemple après une coupure réseau (il ne retrouve alors pas la
liste des objets, voir les limites).

## Règles métier

1. **Tout ou rien.** Si un seul objet ne rentre pas dans l'inventaire, rien n'est
   transféré et le coffre reste `Filled`. L'exception est levée avant
   `SaveChanges`, et le `CommandTransactionBehavior` existant annule toute la
   transaction.
2. **Capacité.** On réutilise `Inventory.Receive` sans le modifier : 40 emplacements
   d'objets et 20 de potions, un emplacement par `item_instance`.
3. **Objets empilables (`item.stackable = true`)** : une seule `item_instance` avec
   la quantité de la récompense (ex. 3 potions → une pile de 3, un seul emplacement).
4. **Objets non empilables** : une `item_instance` de quantité 1 **par unité**
   (ex. 2 épées → 2 lignes, 2 emplacements), comme l'ajout manuel d'objet.
5. **Pas de fusion** avec une pile déjà présente dans l'inventaire : le MCD ne fixe
   pas encore les règles de fusion des piles (même choix que `Reward.CreditItem`).
6. **Traçabilité.** Chaque `item_instance` créée reçoit
   `idempotency_key = '{reward_key}:{reward_item.id}:{index}'`. L'index unique en
   base empêche donc aussi physiquement un double transfert.
   `reward_item.item_instance_id` pointe vers la (première) instance créée.
7. **Concurrence.** Le handler prend d'abord le verrou de génération du coffre
   (`IChestLootRepository.AcquireGenerationLockAsync`, le même que celui de la
   génération), puis verrouille l'inventaire (`GetByHeroIdForUpdateAsync` existant).
   Deux clics simultanés, ou un clic pendant la génération, sont traités l'un après
   l'autre : le second trouve le coffre déjà `Empty`.
8. **Premier arrivé, premier servi.** Le premier héros qui récupère le contenu en
   devient le destinataire ; les suivants reçoivent `alreadyEmpty: true`.

## Fichiers

### Ajoutés

| Couche | Fichier | Rôle |
|---|---|---|
| Domain | `Reward.Domain/Enums/ChestState.cs` | Enum `Filled` / `Empty` |
| Domain | `Reward.Domain/Entities/Chest.cs` | Objet métier non persisté. Il enveloppe la `Reward` du coffre, calcule `State` et effectue le transfert (`TransferTo`) |
| Application | `Reward.Application/Features/ChestUseCase/ClaimChestContents/ClaimChestContentsCommand.cs` | Commande CQRS `(HeroId, DungeonRunId, ChestId)` |
| Application | `…/ClaimChestContentsCommandHandler.cs` | Orchestration : verrouiller, charger, vérifier l'état, transférer |
| Application | `…/ClaimChestContentsCommandValidator.cs` | FluentValidation : identifiants non vides |
| Application | `…/ClaimChestContentsResult.cs` | `ClaimChestContentsResult` + `ClaimedChestItem` |
| Presentation | `Reward.Presentation/Controllers/ChestController.cs` | Endpoint REST |
| Tests | `Reward.Test/Domain/ChestTests.cs` | Règles du domaine (états, empilable ou non, capacité) |
| Tests | `Reward.Test/Features/ChestUseCase/ClaimChestContentsCommandHandlerTests.cs` | Handler avec mocks Moq + validateur |
| Tests | `Reward.Test/Integration/Chests/Controllers/*` | Tests HTTP de bout en bout sur PostgreSQL réel, base dédiée `reward_test_chest` réinitialisée par Respawn |

### Existants, étendus sans rien retirer

`IRewardRepository` et `RewardRepository` (créés par la feature de génération du
butin) reçoivent **une seule méthode ajoutée** : `GetChestRewardAsync(dungeonRunId,
chestId)`. Elle charge la `reward` du coffre avec ses `reward_item`, leurs objets et
leurs catégories. Aucune méthode de la génération n'est modifiée.

### Flux d'un appel

```text
ChestController.ClaimContentsAsync
  └─ MediatR : ValidationBehavior → LoggingBehavior → CommandTransactionBehavior (BEGIN)
       └─ ClaimChestContentsCommandHandler
            1. chestLootRepository.AcquireGenerationLockAsync(runId, chestId)
            2. rewardRepository.GetChestRewardAsync(runId, chestId)           → 404 si absent
            3. new Chest(chestId, reward) ; si State == Empty → 200, alreadyEmpty = true
            4. inventoryRepository.GetByHeroIdForUpdateAsync(heroId)          → 404 si absent
            5. chest.TransferTo(inventory, clock.UtcNow)                      → 409 si capacité
            6. inventoryRepository.AddItemInstance(...) pour chaque instance
  └─ CommandTransactionBehavior : SaveChanges + COMMIT
```

## Tests

- **12 tests unitaires** (domaine + handler + validateur), sans base de données.
- **5 tests d'intégration HTTP** : chaque test remplit d'abord le coffre avec la
  **vraie génération du butin** (`GenerateChestLootCommand`), puis appelle
  l'endpoint. Cas couverts : succès, second appel sur coffre vide, capacité
  insuffisante (409 + rien modifié), coffre inconnu (404), identifiant vide (422).
- Suite complète au moment de la livraison : **125 tests, 0 échec**.

```bash
docker compose up -d postgres
dotnet test Reward.Test/Reward.Test.csproj
```

Les tests d'intégration créent leur propre base (`reward_test_chest`) et ne
touchent jamais aux données de développement.

## Tester à la main

1. Générer le butin d'un coffre avec le gRPC existant (port `8081`, exposé en local
   uniquement), en choisissant un `floor` et une `difficulty` qui ont une table de
   butin `CHEST` dans `loot_table` (le seed de développement en crée) :

```bash
grpcurl -plaintext -import-path Reward.Contracts/Protos -proto reward_chest_loot_v1.proto \
  -d '{"command_id":"<guid>","dungeon_run_id":"<runId>","chest_id":"<chestId>","floor":1,"difficulty":"NORMAL"}' \
  localhost:8081 reward.v1.RewardLootService/GenerateChestLoot
```

2. Récupérer le contenu avec un héros du seed (`SELECT hero_id FROM inventory`) :

```bash
curl -X POST http://localhost:8080/api/v1/heroes/<heroId>/runs/<runId>/chests/<chestId>/claim
```

Relancer la même commande renvoie `alreadyEmpty: true` sans créer de nouvel objet.
Pour que l'API embarque le nouveau code : `docker compose up -d --build`.

## Limites connues et pistes pour la suite

- **Propriété du coffre.** Rien ne relie une run à un héros dans ce service : tout
  héros qui connaît `dungeonRunId` et `chestId` peut récupérer le contenu, et le
  premier arrivé l'emporte. Quand l'authentification ou la propagation d'identité
  (ADR-GLOB-004) et le lien run → héros seront disponibles, il faudra ajouter ce
  contrôle dans le handler (403 ou 404).
- **État du coffre déduit de `reward.hero_id`.** C'est le seul marqueur disponible
  sans changer le schéma. Une colonne ou un statut dédié (ex. `CLAIMED`) serait plus
  explicite si une migration devient possible.
- **Nouvelle tentative après coupure réseau.** Si la réponse du premier appel est
  perdue, la relance renvoie `alreadyEmpty: true` sans la liste des objets. Le
  client doit alors relire l'inventaire du héros.
- **Objet non empilable en plusieurs unités.** `reward_item.item_instance_id` ne
  référence que la première `item_instance` créée ; les autres sont retrouvables
  par leur `idempotency_key`.
- **Inventaire de run.** Le MCD prévoit qu'un butin obtenu *pendant* une run
  alimente `run_item_state` et ne soit validé dans l'inventaire permanent qu'en cas
  de victoire. Aucune fonctionnalité de run n'est encore codée. Comme l'ajout
  d'objet existant, cette US écrit donc dans l'inventaire permanent. À revoir
  quand les sessions de run seront implémentées : seul `Chest.TransferTo` et le
  handler seraient à adapter.
- **XP.** `reward.xp_amount` n'est pas traité : l'expérience relève du service
  héros/progression.
- **gRPC.** L'action vient d'un joueur, donc elle est exposée en REST (ADR 0018).
  Aucun endpoint gRPC n'a été ajouté.
