# Deck smoke-test pack (Kingmaker)

Pack **Kingmaker Smoke** (`…00aa`) for the sub-project-1 acceptance test. Rules (top = highest priority):

1. Heal (any source) the lowest-HP ally when an ally is below 50 % HP
2. Bless on self when the Bless buff (`BlessBuff` 87b8c627…) is missing, cooldown 3 rounds
3. Inspire Courage toggle on (bards, e.g. Linzi)
4. Barbarian rage toggle on (`StandartRageActivateableAbility`, e.g. Amiri)
5. Attack nearest enemy, no cooldown

A rule a unit cannot perform (no Bless, no rage) is skipped by the validator — apply the whole
pack to every companion. Enums are numeric (`ActionType`: 0 CastSpell, 3 Toggle, 4 Attack, 5 Heal;
`TargetType`: 0 Self, 1 AllyLowestHp, 4 EnemyNearest). GUIDs are verified against
`kingmaker/il-dump/blueprints-index.tsv`.

Push with the game closed (presets/packs are read at mod start):

    tar -C docs/testing/deck-smoke -cf - Presets Packs | ssh deck-direct "tar -xf - -C '/run/media/deck/3b03f019-ee3d-473e-beb1-98236afc5254/Games/epic/PathfinderKingmaker/Mods/KingmakerTactics'"

Then apply the pack to the units in the panel (Ctrl+T). Remove the pack's rules afterwards — they fire every round.
