#!/usr/bin/env bash
# Regenerates the content, compiles the dream headlessly with Mono's mcs, and plays it.
#   Tools/Content/sim/run.sh good|bad [-v]
# Needs python3 and mono (apt install mono-mcs). Unity is not involved: the Dream is a plain
# class, so the whole path can be played as fast as the machine allows.
set -e
HERE="$(cd "$(dirname "$0")" && pwd)"
ROOT="$(cd "$HERE/../../.." && pwd)"
S="$ROOT/Assets/Scripts"
python3 "$HERE/../gen.py" "$ROOT/Assets"
mcs -nologo -out:"$HERE/sim.exe" "$HERE/Stubs.cs" "$HERE/NoPlaceholder.cs" "$HERE/Sim.cs" \
    "$S"/WhatLiesInTheDepths/Data/{Dream,Model,OpeningContent}.cs \
    "$S"/Ursine/Runtime/Core/{Fmt,Unlocks}.cs "$S"/Ursine/Runtime/Economy/*.cs "$S"/Ursine/Runtime/Combat/Odds.cs \
    2>&1 | grep -v warning || true
mono "$HERE/sim.exe" "$@"
