name: 
costs: 1
body:
1
1/2 chance of doubling damage
exhausts
oder
exhausted nicht, aber höherer schaden gilt nur für den kampf

# GamblingMod - Ideas

Slay the Spire 2 card mod about odds, luck and risk/reward.
This file is the single place for ideas. Add new ideas with the templates at the bottom.

---

## 1. How to use this file

1. New idea -> copy a blueprint (section 6), fill what you know, add a row to the index (section 2).
2. Move the **Status** along: `IDEA` -> `DESIGNED` -> `CODED` -> `TESTED` -> `BALANCED`.
3. When something blocks you (unknown API, unclear rule), add it to **Open questions** (section 5).
4. After each playtest, add a line to the **Balance log** (section 7).

Naming rules (keep them, they matter for text lookup):
- Class name in PascalCase: `RepeatStrike`
- Localization key in UPPER_SNAKE_CASE, derived from the class: `REPEAT_STRIKE`
- One card per file in `src/Cards/`, relics in `src/Relics/`, powers in `src/Powers/`
- Register every new thing in `src/GamblingContent.cs`

---

## 2. Index

| # | Name | Kind | Status | Difficulty | Depends on |
|---|------|------|--------|------------|------------|
| 1 | Repeat Strike | Card | CODED | easy | - |
| 2 | Fate / Luck meter | Core system | IDEA | medium-hard | custom power API |
| 3 | Loaded Dice | Card | IDEA | medium | #2 |
| 4 | Lucky Strike | Card | IDEA | easy | #2 (optional) |
| 5 | Double or Nothing | Card | IDEA | easy | - |
| 6 | Snake Eyes | Card | IDEA | medium | #2, fail counter |
| 7 | Breeding Chance (self-copy) | Card | IDEA | hard | deck APIs |
| 8 | Blessing or Curse | Card | IDEA | medium-hard | #2, card creation API |
| 9 | Wild Buff (random target) | Card | IDEA | hard | target selection API |

Difficulty: easy = copy a card and change numbers, medium = needs one new system, hard = needs APIs we have not looked up yet.

---

## 3. Design principles

1. **The odds are the mechanic.** Every gamble card shows its chance on the card text.
2. **Luck scales chances multiplicatively, never flat.**
   `chance = base * (1 + 0.10 * Luck * weight)`, capped at 90%.
   Guaranteed (100%) effects are never touched.
3. **Per-card weight.** Mild effects weight 1.0, strong effects 0.5, chain effects 0.25.
4. **A failed roll should still do something small**, or at least cost little. No dead 1-energy cards without a plan to fix it.
5. **Balance by expected value (EV),** not by the best case.
6. **Chains need a hard cap** (default 20) so they cannot run forever.
7. **Co-op safe:** use the run's RNG (`RunState.Rng`), not a local random source, so all players roll the same.
8. **Scope:** add cards to existing character pools first. No new character until the core is fun.

### Quick math

Chain card (roll, on success hit and roll again, stop on first failure):

| chance p | expected hits p/(1-p) | P(0 hits) |
|----------|-----------------------|-----------|
| 30% | 0.43 | 70% |
| 40% | 0.67 | 60% |
| 50% | 1.00 | 50% |
| 60% | 1.50 | 40% |
| 70% | 2.33 | 30% |
| 75% | 3.00 | 25% |
| 90% | 9.00 | 10% |

Expected damage = damage_per_hit * p/(1-p). Compare against Strike: 6 damage for 1 energy.

Self-copy card, chance scales with each copy with 1/k (k = copies in deck):

| copies k | chance 1/2^(k-1) |
|----------|------------------|
| 1 | 100% |
| 2 | 50% |
| 3 | 33% |
| 4 | 25% |
| 5 | 20% |


Luck example (+10% of the base chance per stack, weight 1.0):

| base | Luck 1 | Luck 3 | Luck 5 |
|------|--------|--------|--------|
| 10% | 11% | 13% | 15% |
| 25% | 27.5% | 32.5% | 37.5% |
| 50% | 55% | 65% | 75% |

---

## 4. Ideas

### 4.1 Repeat Strike (Card) - CODED

| Field | Value |
|-------|-------|
| Class / key | `RepeatStrike` / `REPEAT_STRIKE` |
| Cost / type / rarity | 1 / Attack / Common |
| Pool / target | Ironclad / any enemy |
| Numbers | damage per hit 6 (upgrade: +3) |
| Roll | 50% per hit, each success adds a hit and rolls again, first failure stops, cap 20 (to be adressed, no hard limit planned) |
| Luck weight | ? less than 1.0?|
| Text | "Roll 50%: on success, deal {Damage} damage and roll again." |
| EV | 6 damage at 50% (same as Strike on average, very high variance) |

Implementation: roll all hits first, then one `DamageCmd.Attack(...).WithHitCount(hits)`.
0 hits = no damage.

Notes / next: replace the fixed 50% with `Fate.Chance(0.5f, 0.25f, 0.75f)` once Luck exists.

### 4.2 Luck meter (Core system) - IDEA

The one shared stat all gamble cards read.

| Field | Value |
|-------|-------|
| Form | Prototype: static counter `Luck.Meter`. Final: a real power with stacks, shown on screen. |
| Effect | Raises all listed chances (see design principle 2). Negative Luck lowers them. |
| Reset | Per combat |
| Code | `Fate.Chance(baseChance, weight, cap)` |

Open: how to create a custom power, read its stack count, and roll a float (see section 5).

### 4.3 Loaded Dice (Card) - IDEA

| Field | Value |
|-------|-------|
| Cost / type / rarity | 1 / Skill / Common |
| Effect | Gain 1 Luck this combat (upgrade: 2) |
| Notes | The setup card. Needs #2. |


### 4.4 Snake Eyes (Card) - IDEA

| Field | Value |
|-------|-------|
| Cost / type / rarity | 1 / Attack / Uncommon |
| Effect | Deal damage that scales with failed gamble rolls this combat. |
| Notes | Needs a per-combat fail counter. Pairs with bad-luck protection. |

### 4.5 Breeding Chance (self-copy card) - IDEA

| Field | Value |
|-------|-------|
| Cost / type / rarity | 1 / Skill / Rare |
| Effect | Chance of 1/k. With k=Number of Cards in the Deck.  On success, gain 5 block, deal 5 damage and add a copy of this card to the discard pile (only for this combat). |
| Notes | Needs APIs to count copies in the deck and add a card. |

### 4.6 Blessing or Curse (Card) - IDEA

| Field | Value |
|-------|-------|
| Cost / type / rarity | 1 / Skill / Uncommon |
| Effect | Default 50/50: add a Blessing (decent buff card) or a Curse (bad card) to your hand. Luck shifts the split. |
| Notes | Curse should hurt, blessing should be decent, so the odds are fair around 50%. Needs a card-creation API. |

### 4.7 Wild Buff (random target) - IDEA

| Field | Value |
|-------|-------|
| Cost / type / rarity | 1 / Skill / Uncommon |
| Effect | Apply a buff or a debuff to a random creature: you, an ally, or an enemy. |
| Notes | Needs target selection and power application APIs. Co-op: roll with the run RNG. Possibly too swingy, test last. |

###  <Name> (Card) - IDEA

| Field | Value |
|-------|-------|
| Cost / type / rarity | 1 / Attack / Rare
| Numbers | 1 damage (upgrade: ?) |
| Roll | 50% chance of doubling the damage, either forever and exhausts or only for the combat and doesnt exhaust |
---

## 5. Open questions (APIs / rules to look up)

| Question | Where to look | Status |
|----------|---------------|--------|
| How to roll a float / int with the run RNG? | Decompiled RNG class (`NextFloat`? `NextInt`?) | OPEN |
| How to create a custom power and read its stacks? | ModSmith examples, decompiled `Powers` | OPEN |
| How to count copies of a card in the deck / add a card? | Decompiled deck / `CardCmd` classes | OPEN |
| How to pick random targets (allies and enemies)? | Decompiled cards that target randomly | OPEN |
| Does the first-hit-misses design feel fair? | Playtesting | OPEN |
| Do custom cards stay in sync in co-op? | Test with a friend, same mod versions | OPEN |

---

## 6. Blueprints (copy these)

### Card blueprint

```
### X.Y <Name> (Card) - IDEA

| Field | Value |
|-------|-------|
| Class / key | `<ClassName>` / `<UPPER_SNAKE_KEY>` |
| Cost / type / rarity | <0-3> / <Attack|Skill|Power> / <Common|Uncommon|Rare> |
| Pool / target | <pool> / <Self|AnyEnemy|AllEnemies|...> |
| Numbers | <damage/block/etc> (upgrade: <change>) |
| Roll | <chance, what happens on success and on failure, cap> |
| Luck weight | <1.0 | 0.5 | 0.25 | none> |
| Text | "<card description with {Vars}>" |
| EV | <expected value vs a normal card of the same cost> |

Implementation: <which commands/APIs, what is still unknown>
Notes / next: <what to try next>
```

### Relic blueprint

```
### X.Y <Name> (Relic) - IDEA

| Field | Value |
|-------|-------|
| Class / key | `<ClassName>` / `<UPPER_SNAKE_KEY>` |
| Rarity / pool | <Common|Uncommon|Rare|Boss|Shop> / <character or shared> |
| Trigger | <start of combat, on card played, on roll, ...> |
| Effect | <what it does> |
| Luck interaction | <e.g. +1 Luck at combat start> |
| Text | "<description>" |

Implementation / notes: <...>
```

### Power / buff blueprint

```
### X.Y <Name> (Power) - IDEA

| Field | Value |
|-------|-------|
| Class / key | `<ClassName>` / `<UPPER_SNAKE_KEY>` |
| Type | <Buff|Debuff> |
| Stacks | <what a stack means, does it decay> |
| Trigger | <when it acts> |
| Text | "<description>" |

Implementation / notes: <...>
```

### Event / other blueprint

```
### X.Y <Name> (Event|Ancient|Potion) - IDEA

Setup: <where it appears>
Choices: <option 1 -> result, option 2 -> result>
Gamble twist: <how it uses odds>
Notes: <...>
```

---

## 7. Balance log

Add one line per playtest session.

| Date | Card / system | Change | Result |
|------|---------------|--------|--------|
| 2026-11-22 | Repeat Strike | first version, 50% chain, 6 dmg | not tested yet |

---

## 8. Parking lot

Raw ideas not worth a blueprint yet. One line each.

- Character-specific twist: Ironclad gambles with HP (lose HP on fail, gain on success)?
- A relic that shows your last 5 roll results.
- A card that lets you re-roll the last failed roll.
- Event: a casino room (bet gold on a coin flip chain).