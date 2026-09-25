# Content: the whole dream, as data

`design.py` is the design source for chapters 1–5: resources, Focus tasks, constructs,
upgrades, Realizations, Visions, the 100 veils, the 30 places on the road, units, the Journal
and both endings. `gen.py` turns it into
`Assets/Scripts/WhatLiesInTheDepths/Data/OpeningContent.cs` and the matching keys in
`Assets/Resources/WhatLiesInTheDepths/Strings.json`.

```
python3 Tools/Content/gen.py Assets
```

Edit the design, not the generated C#. Card effect lines (`fx`, a place's "When won") are
written from the effects themselves, so a card cannot claim something its numbers do not do.

## The simulated player

`sim/` plays the dream headlessly from the first journal entry to the last battle with a
greedy policy (save for the cheapest thing in reach, put attention and Oneiri on whatever
makes what it is short of, dive otherwise, fight at 70% odds), and prints when each milestone
falls. If it stops making progress for an hour of game time it prints exactly what it is
waiting on.

```
Tools/Content/sim/run.sh good     # leaves the nightmares asleep
Tools/Content/sim/run.sh bad      # wakes them
```

It needs `python3` and Mono's `mcs`. Stubs stand in for the few UnityEngine types the data
layer touches.

First-pass timings for the bot (a person will be slower):

| Milestone | Time |
|---|---|
| Chapter 1 done (level 20) | 0h54 |
| Chapter 2 done (level 40) | 2h09 |
| Chapter 3 done (level 60) | 4h10 |
| Chapter 4 done (level 80) | 8h48 |
| The bottom (level 100) | 13h40 |
| The end, nightmares woken | 13h40 |
| The end, nightmares asleep | about 16h40 |
