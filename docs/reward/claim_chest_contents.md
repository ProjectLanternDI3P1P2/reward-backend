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
| 2 | Transfert terminé → le coffre passe à l'état `Empty` | `Chest.TransferTo` passe la `reward` à `COMPLETED` ; `Chest.State` en déduit `Empty` | `TransferTo_NonStackableReward_CreatesOneItemInstancePerUnit`, test d'intégration ci-dessus |
| 3 | Coffre `Empty` + nouvelle interaction → rien n'est généré ni transféré | `ClaimChestContentsCommandHandler` retourne tout de suite si `State == Empty` | `Handle_EmptyChest_ReturnsEmptyStateWithoutTransferringAnything`, `ClaimContents_WhenChestIsAlreadyEmpty_DoesNotTransferRewardsAgain` |

## Décision principale : pas de table `chest`

Le coffre (sa position, son ouverture dans le donjon, etc.) **appartient à un autre
microservice**. Ce service ne stocke que **son contenu**, avec les tables de
récompenses qui existent déjà. **Aucune migration, aucune modification du schéma**
(vérifié avec `dotnet ef migrations has-pending-model-changes` : aucun changement).

| Concept métier | Représentation dans la base existante |
|---|---|
| Le coffre `chestId` | Une ligne `reward` dont `reward_key = 'CHEST:{chestId}'` |
| Les récompenses du coffre | Les lignes `reward_item` rattachées à cette `reward` |
| État `Filled` (plein) | `reward.status` différent de `COMPLETED` (ex. `PENDING`, `FAILED`) |
| État `Empty` (vide) | `reward.status = 'COMPLETED'` |
| Récompense transférée | `reward_item.item_instance_id` renseigné + nouvelle ligne `item_instance` |

Pourquoi `reward_key` ? La colonne est déjà **unique** (`uq_reward_key`) et le MCD
la décrit comme la clé métier qui empêche d'attribuer deux fois une même
récompense. Un coffre = une raison métier = une clé.

Pourquoi `COMPLETED` et pas `APPLIED` (cité dans le MCD) ? Le code possède déjà
l'enum `RewardStatus { Pending, Active, Completed, Failed }`. On la réutilise
sans la modifier, `Completed.ToCode()` donne `"COMPLETED"`.

### Contrat avec le service qui génère les coffres

Pour qu'un coffre soit récupérable ici, le service propriétaire (ou la future
génération de butin) doit avoir inséré :

- une `reward` avec `reward_key = 'CHEST:' + chestId` (GUID au format standard
  `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`, minuscules), `status = 'PENDING'` ;
- une `reward_item` par objet contenu (`item_id`, `quantity > 0`,
  `item_instance_id = NULL`).

La génération des récompenses (tables de butin) **n'est pas dans le périmètre** de
cette US : l'US part d'un coffre qui « contient des récompenses générées ».

## Endpoint

```http
POST /api/v1/heroes/{heroId}/chests/{chestId}/claim
```

Pas de corps de requête. Même style que les routes d'inventaire
(`/api/v1/heroes/{heroId}/inventory/...`).

### Réponses

| Code | Quand | Corps |
|---|---|---|
| `200 OK` | Coffre vidé maintenant **ou** déjà vide | `ClaimChestContentsResult` (voir ci-dessous) |
| `404 Not Found` | Aucune `reward` pour ce coffre, ou aucun inventaire pour ce héros | ProblemDetails |
| `409 Conflict` | Capacité d'inventaire insuffisante (`InventoryCapacityExceededException`) | ProblemDetails, ex. *"The inventory has reached its item capacity of 40."* |
| `422 Unprocessable Entity` | `heroId` ou `chestId` vaut `00000000-0000-0000-0000-000000000000` | ValidationProblemDetails |

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
sans risque, par exemple après une coupure réseau.

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
   pas encore les règles de fusion des piles.
6. **Traçabilité.** Chaque `item_instance` créée reçoit
   `idempotency_key = '{reward_key}:{reward_item.id}:{index}'`. L'index unique en
   base empêche donc aussi physiquement un double transfert.
   `reward_item.item_instance_id` pointe vers la (première) instance créée.
7. **Concurrence.** Le handler verrouille la ligne `reward` (`SELECT … FOR UPDATE`)
   puis l'inventaire (`GetByHeroIdForUpdateAsync` existant). Deux clics
   simultanés sont traités l'un après l'autre : le second trouve le coffre déjà
   `Empty`.

## Fichiers ajoutés

Aucun fichier existant n'a été modifié.

| Couche | Fichier | Rôle |
|---|---|---|
| Domain | `Reward.Domain/Enums/ChestState.cs` | Enum `Filled` / `Empty` |
| Domain | `Reward.Domain/Entities/Chest.cs` | Objet métier non persisté. Il enveloppe la `Reward` du coffre, calcule `State`, construit la clé (`CreateRewardKey`) et effectue le transfert (`TransferTo`) |
| Domain | `Reward.Domain/Repositories/IRewardRepository.cs` | Accès aux récompenses : lecture verrouillée par clé + lignes `reward_item` |
| Application | `Reward.Application/Features/ChestUseCase/ClaimChestContents/ClaimChestContentsCommand.cs` | Commande CQRS `(HeroId, ChestId)` |
| Application | `…/ClaimChestContentsCommandHandler.cs` | Orchestration : charger, vérifier l'état, transférer |
| Application | `…/ClaimChestContentsCommandValidator.cs` | FluentValidation : identifiants non vides |
| Application | `…/ClaimChestContentsResult.cs` | `ClaimChestContentsResult` + `ClaimedChestItem` |
| Infrastructure | `Reward.Infrastructure/Persistence/Repositories/RewardRepository.cs` | Implémentation EF Core. Enregistrée automatiquement par Scrutor (suffixe `Repository`) |
| Presentation | `Reward.Presentation/Controllers/ChestController.cs` | Endpoint REST |
| Tests | `Reward.Test/Domain/ChestTests.cs` | Règles du domaine (états, empilable ou non, capacité) |
| Tests | `Reward.Test/Features/ChestUseCase/ClaimChestContentsCommandHandlerTests.cs` | Handler avec mocks Moq + validateur |
| Tests | `Reward.Test/Integration/Chests/Controllers/*` | Tests HTTP de bout en bout sur PostgreSQL réel, base dédiée `reward_test_chest` réinitialisée par Respawn |

### Flux d'un appel

```text
ChestController.ClaimContentsAsync
  └─ MediatR : ValidationBehavior → LoggingBehavior → CommandTransactionBehavior (BEGIN)
       └─ ClaimChestContentsCommandHandler
            1. rewardRepository.GetByRewardKeyForUpdateAsync("CHEST:{chestId}")  → 404 si absent
            2. rewardRepository.GetItemsByRewardIdAsync(reward.Id)
            3. new Chest(...) ; si State == Empty → 200, alreadyEmpty = true
            4. inventoryRepository.GetByHeroIdForUpdateAsync(heroId)             → 404 si absent
            5. chest.TransferTo(inventory, clock.UtcNow)                        → 409 si capacité
            6. inventoryRepository.AddItemInstance(...) pour chaque instance
  └─ CommandTransactionBehavior : SaveChanges + COMMIT
```

## Tests

- **14 tests unitaires** (domaine + handler + validateur), sans base de données.
- **5 tests d'intégration HTTP** : chaque test insère d'abord dans sa base jetable
  la `reward` que l'autre service aurait créée, puis appelle l'endpoint. Cas couverts : succès, second appel sur coffre vide,
  capacité insuffisante (409 + rien modifié), coffre inconnu (404), identifiant
  vide (422).
- Suite complète au moment de la livraison : **79 tests, 0 échec**.

```bash
docker compose up -d postgres
dotnet test Reward.Test/Reward.Test.csproj
```

Les tests d'intégration créent leur propre base (`reward_test_chest`) et ne
touchent jamais aux données de développement.

## Tester à la main

Ce service **ne crée jamais de coffre ni ses récompenses** : c'est le rôle du
service propriétaire du coffre. En local, ce service n'est pas là et le seed de
développement ne crée pas de récompense. Pour tester, on **simule donc à la main**
ce qu'il aurait enregistré (par exemple depuis DBeaver sur `localhost:5433`, base
`reward`). Ce script sert uniquement aux tests manuels et ne fait pas partie de la
fonctionnalité. On réutilise un héros et un objet du seed :

```sql
-- 1. Choisir un héros et un objet existants
SELECT hero_id FROM inventory LIMIT 1;
SELECT id, name, stackable FROM item LIMIT 5;

-- 2. Simuler le contenu d'un coffre fourni par l'autre service (remplacer les <...>)
INSERT INTO reward_source (id, name, description)
VALUES (gen_random_uuid(), 'CHEST', 'Dungeon chest')
ON CONFLICT (name) DO NOTHING;

INSERT INTO reward (id, run_id, reward_source_id, type, status, reward_key, xp_amount, created_at)
SELECT gen_random_uuid(), gen_random_uuid(), id, 'ITEM', 'PENDING',
       'CHEST:11111111-1111-1111-1111-111111111111', 0, now()
FROM reward_source WHERE name = 'CHEST';

INSERT INTO reward_item (id, reward_id, item_id, item_instance_id, quantity, created_at)
SELECT gen_random_uuid(), r.id, '<item_id>', NULL, 1, now()
FROM reward r WHERE r.reward_key = 'CHEST:11111111-1111-1111-1111-111111111111';
```

Puis, après `docker compose up -d --build` pour embarquer le nouveau code :

```bash
curl -X POST http://localhost:8080/api/v1/heroes/<hero_id>/chests/11111111-1111-1111-1111-111111111111/claim
```

Relancer la même commande renvoie `alreadyEmpty: true` sans créer de nouvel objet.

## Limites connues et pistes pour la suite

- **Propriété du coffre.** `reward.run_id` n'est pas relié localement à un héros.
  Le service ne peut donc pas vérifier que `heroId` est bien celui qui a trouvé le
  coffre. Quand l'authentification ou la propagation d'identité (ADR-GLOB-004) et
  le lien run → héros seront disponibles, il faudra ajouter ce contrôle dans le
  handler (403 ou 404).
- **Inventaire de run.** Le MCD prévoit qu'un butin obtenu *pendant* une run
  alimente `run_item_state` et ne soit validé dans l'inventaire permanent qu'en cas
  de victoire. Aucune fonctionnalité de run n'est encore codée. Comme l'ajout
  d'objet existant, cette US écrit donc dans l'inventaire permanent. À revoir
  quand les sessions de run seront implémentées : seul `Chest.TransferTo` et le
  handler seraient à adapter.
- **XP.** `reward.xp_amount` n'est pas traité : l'expérience relève du service
  héros/progression. Une publication d'événement (`RewardGranted`, voir
  `IMessagePublisher`) pourra être ajoutée si ce service en a besoin.
- **gRPC.** L'action vient d'un joueur, donc elle est exposée en REST (ADR 0018).
  Aucun endpoint gRPC n'a été ajouté.
