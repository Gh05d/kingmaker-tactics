#!/usr/bin/env bash
# Lists Wrath Tactics fix commits since the fork that Kingmaker Tactics has not taken over yet.
# A fix counts as ported when a Kingmaker commit message names its hash: `git cherry-pick -x`
# writes "(cherry picked from commit <full hash>)"; a hand-adapted port names "Wrath <short hash>".
# Wrath-only fixes (no Kingmaker counterpart) go into SKIP below with a reason.
set -u
cd "$(dirname "$0")/.."

FORK_POINT=abb2cb9   # 2026-10-03, last commit shared with wrath/master
SKIP="5df1431"       # Arcane Reservoir row: Arcanist does not exist in Kingmaker

git fetch -q wrath || { echo "could not fetch remote 'wrath'" >&2; exit 2; }
messages=$(git log --format=%B "$FORK_POINT"..HEAD)

missing=0
while read -r full subject; do
    short=${full:0:7}
    case " $SKIP " in *" $short "*) continue ;; esac
    if grep -q -e "$full" -e "Wrath $short" <<<"$messages"; then continue; fi
    echo "missing: $short $subject"
    missing=$((missing + 1))
done < <(git log --format='%H %s' "$FORK_POINT"..wrath/master | grep -E '^[0-9a-f]+ fix')

[ "$missing" -eq 0 ] && echo "all Wrath fixes since $FORK_POINT are ported"
exit 0
