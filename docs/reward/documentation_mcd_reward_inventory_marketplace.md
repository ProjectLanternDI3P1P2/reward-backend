# MCD — Reward, Inventory et Marketplace

> Documentation du schéma source et de ses notes. Les corrections de typographie, les cardinalités et les choix de modélisation ajoutés ci-dessous sont explicitement distingués des éléments dessinés. Les identifiants externes ne sont pas des clés étrangères locales.

## Vue d'ensemble

**Périmètre du microservice :** l’entité `HERO` appartient à un autre microservice et n’est pas stockée ici. `INVENTORY.hero_id` et `EQUIPEMENT.hero_id` conservent uniquement son identifiant externe, sous forme de colonne ordinaire **sans contrainte de clé étrangère (FK) vers une table locale `HERO`**. Les autres identifiants de joueurs externes suivent le même principe. Cette précision corrige la représentation initiale de l’image et prime sur ses liens vers `HERO`.

Le modèle couvre l'inventaire et les objets, l'équipement, la sauvegarde de l'inventaire au début d'un donjon, les récompenses, les tables de butin, la marketplace et les échanges directs entre joueurs. `ITEM` est la définition d'un type d'objet ; `ITEM_INSTANCE` représente un exemplaire effectivement détenu, susceptible d'être équipé, récompensé, vendu ou échangé. Le diagramme découpe ces fonctionnalités en sept zones numérotées « implémentation » ; ces numéros indiquent le découpage présenté sur l'image, pas des migrations SQL détaillées.

## 1. Inventaire et objets — première implémentation

| Entité | Attributs visibles sur le MCD | Fonction |
|---|---|---|
| `INVENTORY` | `id_inventory` (PK), `hero_id` (identifiant externe, sans FK), `item_capacity`, `potion_capacity`, `created_at`, `updated_at` | Inventaire du héros. |
| `ITEM` | `id_item` (PK), `id_category` (FK), `id_rarity` (correction de `FK_id-rarety` sur l’image), `name`, `description`, `level_required`, `stackable`, `created_at`, `updated_at` | Définition et caractéristiques d'un type d'objet. |
| `ITEM_INSTANCE` | `id_item_instance` (PK), `id_item` (FK), `inventory_id` (FK), `status`, `quantity`, `created_at`, `updated_at` | Pile d’objets d’un même type associée à un inventaire ; `quantity > 0`. |
| `CATEGORY` | `id_category` (PK), `label`, `created_at`, `updated_at` | Catégorie d'objet. |
| `CLASS_TAG` | `id_class_tag` (PK), `label`, `created_at`, `updated_at` | Étiquette de classe associée aux objets. |
| `MODIFIER` | `id_modifier` (PK, correction de la coquille `id_rarity` du dessin), `name`, `stat`, `value`, `type`, `created_at`, `updated_at` | Modificateur de caractéristique. Chaque modificateur possède son identifiant propre. |
| `RARITY` | `id_rarity` (PK), `label`, `color`, `rank`, `created_at`, `updated_at` | Rareté d'un objet. |

**Relations locales :** `INVENTORY` contient `ITEM_INSTANCE` ; chaque `ITEM_INSTANCE` est associé à un `ITEM` ; un `ITEM` est catégorisé par `CATEGORY`, classé par `RARITY` et peut être lié à plusieurs `CLASS_TAG` et `MODIFIER` (relations N–N dessinées pour ces deux dernières). Cardinalité retenue : un inventaire contient zéro à plusieurs instances ; chaque instance appartient **toujours à un unique inventaire permanent** (`inventory_id NOT NULL`). Une vente ou un échange conserve le propriétaire actuel jusqu’au transfert atomique. Les autres relations suivent le sens de leurs FK ; il ne s’agit pas d’une transcription littérale des chiffres parfois ambigus du dessin.

**Notes du dessin :**

- L'inventaire prévoit **20 potions et 40 emplacements pour les objets hors potions**.
- `ITEM` représente une *définition* : « Obsidian Blade » est une arme de niveau 8 donnant +12 ATK et +4 % ATK. `ITEM_INSTANCE` est l'exemplaire réellement possédé : par exemple, l'Obsidian Blade appartenant au héros 123.
- Exemples de `ITEM_INSTANCE.status` : `AVAILABLE`, `EQUIPPED`, `RESERVED`, `SOLD`, `CONSUMED`, `TRADED`.
- Exemples de catégories : `WEAPON`, `ARMOR`, `SHIELD`, `RING`, `AMULET`, `POTION`, `VIAL`.
- Exemples de tags de classe : `Physical`, `Magical`, `Nature`.
- Raretés et rangs : 1 = `Common`, 2 = `Uncommon`, 3 = `Rare`, 4 = `Epic`, 5 = `Legendary`.

## 2. Équipement — deuxième implémentation

| Entité | Attributs visibles | Fonction |
|---|---|---|
| `EQUIPMENT` | `id_equipment` (PK), `hero_id` (identifiant externe, sans FK), `id_item_instance` (FK), `id_slot` (FK), `quantity` (= 1), `created_at`, `updated_at` | Un slot d’équipement prélève une unité d’une pile ; plusieurs slots peuvent référencer la même pile si sa quantité le permet. |
| `SLOT` | `id_slot` (PK), `name`, `created_at`, `updated_at` | Emplacement d'équipement disponible. |

**Relations :** chaque ligne utilise un `SLOT` et référence une pile `ITEM_INSTANCE`. Plusieurs lignes peuvent partager un même `hero_id` **et une même instance de pile** : deux équipements identiques ne nécessitent pas deux `ITEM_INSTANCE`. Une seule ligne par `(hero_id, id_slot)` ; le nombre d’unités équipées depuis une pile ne dépasse jamais `ITEM_INSTANCE.quantity`. Aucune entité `HERO` locale. **Équiper/déséquiper est autorisé entre les combats pendant la run, interdit pendant un combat.**

**Note / exemple :**

```text
EQUIPMENT
1 | Hero 1 | ItemInstance 15 | RIGHT_HAND
2 | Hero 1 | ItemInstance 22 | BODY
3 | Hero 1 | ItemInstance 31 | JEWELRY_1
```

Emplacements illustrés : 1 = `RIGHT_HAND`, 2 = `LEFT_HAND`, 3 = `BODY`, 4 = `JEWELRY_1`, 5 = `JEWELRY_2`.

## 3. Snapshots — troisième implémentation

| Entité | Attributs visibles | Fonction |
|---|---|---|
| `INVENTORY_SNAPSHOT` | `id_inventory_snapshot` (PK), `id_inventory` (FK), `id_run` (identifiant externe, sans FK), `created_at` | État de référence de l'inventaire au début d'une run. |
| `ITEM_SNAPSHOT` | `id_item_snapshot` (PK), `id_inventory_snapshot` (FK), `id_item_instance` (FK), `created_at`, `quantity` | Objet et quantité conservés dans un snapshot. |

> Le schéma original emploie plusieurs variantes erronées de « snapshot ». Elles sont harmonisées en `INVENTORY_SNAPSHOT`, `ITEM_SNAPSHOT`, `id_inventory_snapshot` et `id_item_snapshot`.

**Relations :** l'inventaire est mémorisé sous forme de snapshots ; un snapshot sauvegarde des lignes d'objets ; chaque ligne garde la référence à un `ITEM_INSTANCE`. `id_run` lie le snapshot à la run concernée, sans entité `RUN` ni FK locale.

**Comportement décrit dans les notes :** avant le donjon, un snapshot de l'inventaire permanent est créé. Pendant la run, l'état de référence est combiné aux objets récupérés, aux potions consommées et aux modifications d'équipement. **Modèle technique retenu :** ouvrir une `RUN_INVENTORY_SESSION` par snapshot et enregistrer les piles dans `RUN_ITEM_STATE` et les slots dans `RUN_EQUIPMENT_STATE`, sans modifier l’inventaire ou l’équipement permanents avant la victoire. `RUN_ITEM_STATE.id_item_instance` est nullable pour un butin encore absent de l’inventaire permanent. Chaque action autorisée (butin, consommation, équipement/déséquipement entre combats) met à jour les états temporaires et la phase de session de manière transactionnelle ; cet état est rechargé après reconnexion ou redémarrage. Pendant un combat, aucune modification d’équipement n’est acceptée, y compris après un retry. Les ventes et échanges d’objets de l’inventaire permanent sont bloqués pendant toute la run. Si la run se termine par `VICTORY`, les changements sont validés dans l'inventaire permanent. En cas de `DEFEAT`, les changements sont abandonnés et l'inventaire revient à l'état du début de la run.

**Exemple du dessin :** le héros entre avec une épée, un bouclier et deux potions. Pendant la run, il consomme une potion et récupère une épée légendaire. En cas de défaite, il conserve l'épée, le bouclier et ses **deux potions initiales** ; l'épée légendaire disparaît. En cas de victoire, il conserve l'épée, le bouclier, **une potion** et l'épée légendaire.

## 4. Récompenses — quatrième implémentation

| Entité | Attributs visibles | Fonction |
|---|---|---|
| `REWARD` | `id_reward` (PK), `id_run` (identifiant externe, sans FK), `id_reward_source` (FK), `type`, `status`, `reward_key`, `xp_amount`, `created_at` | Récompense générée pour une run / une raison métier. |
| `REWARD_ITEM` | `id_reward_item` (PK), `id_reward` (FK), `id_item` (FK), `id_item_instance` (FK), `quantity`, `created_at` | Objet ou quantité accordée dans une récompense. |
| `REWARD_SOURCE` | `id_reward_source` (PK), `name`, `description` | Origine de la récompense. |

**Relations :** `REWARD_SOURCE` concerne des `REWARD` ; une `REWARD` contient des `REWARD_ITEM` ; les récompenses d'objets font référence à `ITEM` et peuvent générer des `ITEM_INSTANCE`.

**Notes sur l'unicité et le statut :** une récompense ne doit être attribuée **qu'une seule fois pour une même raison métier**. `reward_key` est donc une clé métier unique destinée à empêcher les attributions en double.

La note décrit un enchaînement « combat terminé → création de la récompense → création de `ITEM_INSTANCE` → ajout à `INVENTORY` ». Une erreur peut intervenir après la création de l'instance, mais avant son ajout à l'inventaire : le statut permet de savoir si l'attribution a abouti.

| Statut | Sens indiqué dans la note |
|---|---|
| `PENDING` | La récompense existe, mais n'est pas encore totalement appliquée. |
| `APPLIED` | La récompense a été correctement attribuée. |
| `FAILED` | L'attribution a échoué. |

## 5. Tables de butin — cinquième implémentation

| Entité | Attributs visibles | Fonction |
|---|---|---|
| `LOOT_TABLE` | `id_loot_table` (PK), `name`, `difficulty`, `source_type`, `created_at`, `updated_at` | Décrit une table de butin associée à une source et une difficulté. |
| `LOOT_RARITY_RULE` | `id_loot_rarity_rule` (PK), `id_loot_table` (FK), `id_rarity` (FK), `weight` | Associe à une table une rareté et son poids de tirage. |
| `LOOT_TABLE_ENTRY` | `id_loot_table_entry` (PK), `id_loot_rarity_rule` (FK), `id_item` (FK), `weight`, `min_quantity`, `max_quantity` | Objet éligible dans une règle de rareté, avec son poids et sa quantité possible. |

**Relations :** `LOOT_TABLE` possède des `LOOT_RARITY_RULE` ; chaque règle utilise une `RARITY` et contient des `LOOT_TABLE_ENTRY` ; une entrée indique quel `ITEM` peut apparaître. La note précise que **`id_loot_table` a été retiré de `LOOT_TABLE_ENTRY`** : l'entrée dépend de la règle de rareté, qui dépend elle-même de la table de butin.

**Exemples de `source_type` :** 1 = `MONSTER`, 2 = `BOSS`, 3 = `CHEST`.

**Exemples de tables :** `Standard Monster - Easy`, `Standard Monster - Normal`, `Standard Monster - Hard`, `Boss - Easy`, `Boss - Normal`, `Boss - Hard`, `Chest - Floor 1`, `Chest - Floor 2`, etc.

**Exemples de distribution présents dans le dessin :**

| Source | Distribution illustrée |
|---|---|
| Monstre standard | 60 % : aucun objet ; 35 % : Common / Uncommon ; 5 % : Rare. |
| Boss | 100 % : un objet ; parmi les objets, 75 % : Rare ; 20 % : Epic ; 5 % : Legendary. |

Pour une table `BOSS`, l'exemple donne les poids de rareté `Rare = 75`, `Epic = 20`, `Legendary = 5`. Sous la règle `Rare / 75` figurent `Ring X` et `Sword Y` ; sous `Epic / 20`, `Armor Z` et `Weapon A` ; sous `Legendary / 5`, `Blazing Aegis`. `LOOT_TABLE_ENTRY.weight` sert au tirage de l'objet à l'intérieur de la catégorie de rareté. La note n'explicite pas l'algorithme complet de tirage ni la représentation du cas « aucun objet » dans les tables.

## 6. Marketplace — sixième implémentation

| Entité | Attributs visibles | Fonction |
|---|---|---|
| `MARKETPLACE_LISTING` | `id_listing` (PK), `id_item_instance` (FK), `seller_id`, `quantity`, `price`, `status`, `created_at`, `updated_at` | Annonce de vente d'un objet concret. |
| `MARKETPLACE_RESERVATION` | `id_reservation` (PK), `id_listing` (FK), `buyer_id`, `status`, `expires_at`, `created_at`, `updated_at` | Réservation temporaire d'une annonce par un acheteur. |
| `MARKETPLACE_TRANSACTION` | `id_transaction` (PK), `id_listing` (FK), `id_reservation` (FK), `buyer_id`, `seller_id`, `amount`, `idempotency_key`, `status`, `created_at`, `updated_at` | Transaction d'achat et suivi de son état. |
| `OWNERSHIP_TRANSFER` | `id_transfer` (PK), `id_transaction` (FK), `id_item_instance` (FK), `quantity`, `from_owner_id`, `to_owner_id`, `from_inventory_id`, `to_inventory_id`, `status`, `created_at`, `completed_at` | Transfert effectif d'un objet entre propriétaires et inventaires. |

**Relations :** une `ITEM_INSTANCE` peut être listée dans des annonces ; une annonce peut posséder des réservations et être liée à des transactions ; la transaction est liée à une réservation éventuelle et peut déclencher un `OWNERSHIP_TRANSFER` ; ce transfert vise une instance d'objet.

**Notes du dessin :**

- `id_listing` identifie l'annonce ; `id_item_instance` est l'objet concret mis en vente ; `seller_id` référence le vendeur ; `price` est le prix demandé ; `status` est l'état de l'annonce.
- Les identifiants des joueurs ne sont pas gérés par Rewards : `seller_id`, `buyer_id`, `from_owner_id` et `to_owner_id` sont des **identifiants externes ordinaires, sans FK locale vers une table de joueurs**.
- Une réservation concerne `id_listing` et `buyer_id`. Ses états illustrés sont `active`, `expired`, `cancelled`, `completed` (le dessin comporte une coquille sur ce dernier).
- **Une même intention d'achat ne doit produire qu'un seul débit et un seul transfert de propriété.** La transaction comporte donc `idempotency_key`.
- Dans `OWNERSHIP_TRANSFER`, `id_transaction` désigne la transaction à l'origine du transfert ; `id_item_instance` l'objet transféré ; `from_owner_id` et `to_owner_id` les propriétaires avant et après ; `from_inventory_id` et `to_inventory_id` les inventaires d'origine et de destination ; `created_at` le début du traitement et `completed_at` son achèvement.

Le service Reward / Inventory / Marketplace conserve également les portefeuilles et les réservations de fonds. Les annonces, achats et échanges doivent être traités localement par transaction PostgreSQL avec contrôle des quantités, de la session de run et de l’idempotence.

## 7. Échanges entre joueurs — septième implémentation

| Entité | Attributs visibles | Fonction |
|---|---|---|
| `TRADE` | `id_trade` (PK), `initiator_id`, `counterparty_id`, `cash_adjustment`, `status`, `created_at`, `updated_at` | Échange proposé entre deux joueurs, avec éventuel complément monétaire. |
| `TRADE_ITEM` | `id_trade_item` (PK), `id_trade` (FK), `id_item_instance` (FK), `quantity`, `side`, `status`, `created_at`, `updated_at` | Objet concret proposé par l'un des deux participants à l'échange. |

**Relations :** `TRADE` contient une ou plusieurs lignes `TRADE_ITEM` dans le dessin ; chaque ligne référence une `ITEM_INSTANCE`, qui peut participer à des échanges.

**Notes :** `initiator_id` désigne le joueur proposant l'échange, `counterparty_id` l'autre joueur, `cash_adjustment` l'éventuel complément monétaire et `status` l'état de l'échange. Dans `TRADE_ITEM`, `side` indique le côté proposant l'objet (`Initiator` ou `counterparty`) et `status` l'état de cet objet dans l'échange. Le dessin ne définit ni la liste exhaustive de ces statuts ni le déroulement détaillé de la validation ou de l'annulation d'un échange.

## Diagramme Mermaid mis à jour

Le diagramme source se trouve dans [`mcd_reward_inventory_marketplace.mmd`](mcd_reward_inventory_marketplace.mmd). Il ne contient pas d’entité `HERO` ni de relation dessinée vers celle-ci.

## Modèle finalisé à partir des décisions métier

Les corrections orthographiques sont appliquées (`SNAPSHOT`, `id_inventory_snapshot`, `id_category`, `id_rarity`, `id_modifier`, `EQUIPMENT`, etc.). Le dessin d’origine ne contenait ni les tables de liaison, ni les tables de session, ni les tables monétaires : ces tables sont des **compléments techniques** ajoutés au MCD. `hero_id`, `id_run`, `owner_id`, `buyer_id` et `seller_id` sont des identifiants externes ordinaires, **sans FK vers des tables HERO/PLAYER/RUN locales**.

### 1. Inventaire, piles et équipement

- Une `ITEM_INSTANCE` représente une **pile homogène** d’un même `ITEM`, identifiée par son UUID, avec `quantity > 0` et un `inventory_id NOT NULL`. Plusieurs équipements identiques peuvent être équipés depuis une même pile : **une instance distincte par exemplaire n’est pas requise**. Une ligne `EQUIPMENT` correspond à une unité occupant un slot et référence la pile ; des lignes différentes peuvent donc désigner la même pile, dans la limite de la quantité possédée. `UNIQUE(hero_id, id_slot)` empêche deux unités sur le même slot. Un contrôle transactionnel doit garantir `nombre d'unités équipées + quantités réservées/engagées <= quantité de pile` selon les actions en cours.
- Une pile n’est jamais temporairement « sans inventaire » : avant le commit, le vendeur reste propriétaire ; dans **une transaction**, on soustrait la quantité vendue de sa pile puis on crée ou incrémente une pile compatible chez l’acheteur. Pour une vente portant sur toute la pile, il est également possible de changer directement `inventory_id` si cela ne brise pas les références historiques ; on privilégie ici une sortie/entrée atomique en conservant les lignes d’audit. Aucun état intermédiaire sans propriétaire n’est committé.
- `ITEM_INSTANCE.status` désigne l’état global de la pile ; l’occupation des slots relève de `EQUIPMENT`. Ne pas utiliser `EQUIPPED` comme état exclusif d’une pile partiellement équipée. Les capacités (20 potions, 40 autres emplacements dans les notes du dessin) sont vérifiées sur les quantités/slots effectivement retenus par les règles de stockage, y compris à la réception d’un achat, d’un échange ou à la victoire.
- `ITEM_CLASS_TAG(id_item, id_class_tag)` et `ITEM_MODIFIER(id_item, id_modifier)` ont des PK composites et des FK locales. `MODIFIER.id_modifier` est bien sa PK.

### 2. Run : état temporaire et reprise après redémarrage

`INVENTORY_SNAPSHOT` garde la référence initiale et `ITEM_SNAPSHOT` les piles/quantités au départ. `EQUIPMENT_SNAPSHOT` (ajout) garde les slots d’origine. `RUN_INVENTORY_SESSION` (ajout, une par snapshot) porte `status` et `phase` (`EXPLORATION` ou `COMBAT`), et `RUN_ITEM_STATE` / `RUN_EQUIPMENT_STATE` (ajouts) représentent **l’état temporaire courant**. L’identité d’un butin propre à la run est `id_run_item_state`, même si `id_item_instance` est encore NULL. Les changements de quantité et de slots sont persistés dans PostgreSQL à chaque action, dans une transaction verrouillant la session et les lignes affectées ; l’API d’action utilise une clé d’idempotence stable pour éviter de rejouer une consommation après reconnexion (table de suivi d’actions à ajouter lors de la conception des commandes). Ne pas attendre la fin de la run pour écrire ces états.

- **Entre les combats :** équiper/déséquiper et utiliser immédiatement les objets ramassés sont autorisés. **En combat :** équiper/déséquiper est interdit ; une modification de `RUN_EQUIPMENT_STATE` doit vérifier sous verrou que `phase != COMBAT`. Le changement de phase et l’action sont sérialisés pour empêcher une course entre « commencer le combat » et « équiper ». Les règles d’utilisation des potions **pendant** le combat ne sont pas fixées par cette décision ; ne pas les interdire par déduction.
- **Déconnexion/redémarrage :** recharger la session ACTIVE, ses piles et ses slots persistés ; ne pas réinitialiser avec le snapshot. Le suivi de phase doit provenir d’un état de combat fiable, et non seulement d’un drapeau contrôlable par le client.
- **Pendant une run ACTIVE :** aucune vente ni aucun échange des objets de l’inventaire permanent du héros. Les commandes marketplace/trade verrouillent et vérifient l’état de la session **dans la même transaction** que la réservation d’une pile. Une annonce déjà ouverte doit être rendue indisponible/annulée lors de l’entrée en run, ou l’entrée en run refusée tant qu’une réservation d’objet est active ; cette règle de concurrence protège l’interdiction métier.
- **Victoire :** sous verrou de session, appliquer une seule fois l’état temporaire final au stock et aux slots permanents (y compris le butin et les consommations), vérifier les capacités, puis marquer `VICTORY_COMMITTED` dans le même commit ; le retry renvoie l’issue enregistrée. **Défaite :** clore `DEFEAT_CLOSED` sans modifier l’inventaire ni l’équipement permanents ; les états temporaires peuvent ensuite être archivés/supprimés. Le snapshot permet l’audit et le contrôle, mais la restauration ne requiert pas d’annuler des écritures permanentes puisqu’aucune n’a été appliquée pendant la run.

### 3. Monnaie, ventes et échanges : écritures locales atomiques

La monnaie et les soldes appartiennent **au microservice Reward / Inventory / Marketplace**, contrairement à l’hypothèse précédente de paiement distant. Le MCD ajoute :

| Entité ajoutée | Rôle |
|---|---|
| `WALLET(id_wallet, owner_id, balance, updated_at)` | Portefeuille local par joueur (`owner_id` externe, UNIQUE), solde non négatif. |
| `WALLET_HOLD(id_wallet_hold, id_wallet, id_transaction?, id_trade?, amount, status, hold_key, created_at, updated_at)` | Réservation monétaire durable, identifiée de manière idempotente ; rattachée à un achat **ou** un échange. |
| `WALLET_LEDGER_ENTRY(id_wallet_ledger_entry, id_wallet, id_wallet_hold?, delta, operation_key, created_at)` | Écriture d’audit de crédit/débit à clé d’opération unique. |

Le **solde disponible** est `balance - somme(holds HELD)` ; un hold n’est pas un débit définitif. La création du hold sous verrou du portefeuille interdit les doubles réservations et les soldes disponibles négatifs. À la vente, valider acheteur, vendeur, quantité de la pile, absence de run active, réservations d’objets et capacité de réception ; réserver le montant et la quantité de façon durable. Tant que le transfert n’est pas réussi, **les fonds restent réservés** ; ils ne sont pas définitivement débités. Au commit final **dans une même transaction PostgreSQL**, déplacer/séparer la quantité de pile, capturer le hold, débiter le portefeuille acheteur, créditer le portefeuille vendeur, enregistrer les lignes de ledger et marquer transaction/transfert/annonce `COMPLETED`. Si une étape SQL échoue, tout le commit est annulé : la réservation initiale demeure et une reprise peut retenter. Ne jamais marquer `COMPLETED` avant les écritures effectives.

Pour `TRADE.cash_adjustment`, appliquer **la même monnaie**, le même portefeuille et un `WALLET_HOLD` attaché à `id_trade` : le participant débiteur réserve les fonds ; le transfert des objets dans les deux sens et du complément monétaire doit être atomique. Les quantités d’objets proposées par `TRADE_ITEM.quantity` peuvent provenir de piles. Une opération définitivement annulée libère les holds ; un transfert seulement en échec technique conserve ses holds et reste en attente de reprise. Pour les réservations expirables, ne pas les libérer tant qu’une opération finale est en cours : orchestrer expiration et finalisation avec verrouillage des mêmes lignes et transitions d’état explicites.

### 4. Contraintes SQL et idempotence (PostgreSQL)

Les contraintes suivantes illustrent le **minimum** à appliquer dans les migrations, en complément des FK locales, des contrôles de propriété/quantité et des verrous applicatifs :

```sql
ALTER TABLE item_instance
  ALTER COLUMN inventory_id SET NOT NULL,
  ADD CONSTRAINT ck_item_instance_quantity CHECK (quantity > 0);

ALTER TABLE equipment
  ADD CONSTRAINT uq_equipment_hero_slot UNIQUE (hero_id, id_slot),
  ADD CONSTRAINT ck_equipment_quantity_one CHECK (quantity = 1);

ALTER TABLE run_equipment_state
  ADD CONSTRAINT uq_run_equipment_slot UNIQUE (id_run_inventory_session, id_slot),
  ADD CONSTRAINT ck_run_equipment_quantity_one CHECK (quantity = 1);

ALTER TABLE inventory_snapshot
  ADD CONSTRAINT uq_inventory_snapshot_run UNIQUE (id_inventory, id_run);

ALTER TABLE run_inventory_session
  ADD CONSTRAINT uq_run_session_snapshot UNIQUE (id_inventory_snapshot);

ALTER TABLE reward
  ALTER COLUMN reward_key SET NOT NULL,
  ADD CONSTRAINT uq_reward_key UNIQUE (reward_key),
  ADD CONSTRAINT ck_reward_key_nonblank CHECK (length(btrim(reward_key)) > 0);

ALTER TABLE marketplace_transaction
  ALTER COLUMN idempotency_key SET NOT NULL,
  ADD CONSTRAINT uq_marketplace_transaction_idempotency_key UNIQUE (idempotency_key),
  ADD CONSTRAINT ck_transaction_idempotency_key_nonblank
    CHECK (length(btrim(idempotency_key)) > 0);

ALTER TABLE ownership_transfer
  ADD CONSTRAINT uq_ownership_transfer_transaction UNIQUE (id_transaction);

ALTER TABLE wallet
  ADD CONSTRAINT uq_wallet_owner UNIQUE (owner_id),
  ADD CONSTRAINT ck_wallet_balance CHECK (balance >= 0);

ALTER TABLE wallet_hold
  ADD CONSTRAINT uq_wallet_hold_key UNIQUE (hold_key),
  ADD CONSTRAINT ck_wallet_hold_amount CHECK (amount > 0),
  ADD CONSTRAINT ck_wallet_hold_single_origin
    CHECK ((id_transaction IS NOT NULL) <> (id_trade IS NOT NULL));

ALTER TABLE wallet_ledger_entry
  ADD CONSTRAINT uq_wallet_operation UNIQUE (operation_key),
  ADD CONSTRAINT ck_wallet_ledger_nonzero CHECK (delta <> 0);

CREATE UNIQUE INDEX uq_active_listing_item
  ON marketplace_listing (id_item_instance)
  WHERE status IN ('ACTIVE', 'RESERVED');

CREATE UNIQUE INDEX uq_active_reservation_listing
  ON marketplace_reservation (id_listing)
  WHERE status = 'ACTIVE';
```

Ces index sur les annonces illustrent une politique conservative **une annonce active par pile** : les ventes fractionnées s’effectuent via `quantity` et la réservation contrôlée de cette pile. Les statuts doivent correspondre exactement aux valeurs employées par l’application. Les vérifications agrégées (quantité équipée ou réservée ≤ quantité possédée, solde disponible, capacité des deux inventaires, pile non engagée dans une run) ne sont **pas** garanties par un simple `CHECK` sur une ligne : les imposer dans la couche transactionnelle, avec verrous ordonnés et tests de concurrence. Une réutilisation de `idempotency_key` avec une charge différente doit être rejetée ; un retry avec la même charge relit le résultat déjà stocké. Même règle pour les clés d’actions de run, de hold et de ledger.

**Récompenses :** `reward_key` stable pour un même événement métier ; créer `REWARD(PENDING)` puis appliquer ses lignes et marquer `APPLIED` au sein d’une transaction locale idempotente. Les récompenses obtenues **pendant une run** doivent créditer `RUN_ITEM_STATE` et non l’inventaire permanent ; seuls les résultats validés en victoire sont transférés au permanent. En cas d’échec, reprendre depuis l’état persistant sans accorder deux fois le butin.

## Points d’implémentation non déduits du dessin

Le MCD documente les choix ci-dessus mais ne fige pas le catalogue exhaustif des statuts, la tarification des piles (prix total ou unitaire), les règles de compatibilité de fusion des piles, les effets des modificateurs par exemplaire ni la politique précise d’expiration d’une réservation après une panne longue. Il faudra les détailler dans les US et les règles de validation avant de coder les handlers concernés. Aucun de ces points ne change les décisions métier confirmées dans cette mise à jour.
