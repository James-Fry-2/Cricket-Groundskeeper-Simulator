# Playtest tracker

Testers appear here by alias only (T1 to T8). Keep names and contact details out of the repo.

The briefing and questionnaire are also one shareable page, https://claude.ai/artifact/MXdnK4Js2cH2ggFZV7Ucxw (source: `kestrel-playtest.html`; keep it in step with `briefing.md` and `questionnaire.md`). Testers copy their answers or save them as a file and email them back. Share it from the page's Share menu before sending the link.

Wave 1 is two testers, to catch blockers before the rest; wave 2 is everyone else. Fix only blockers between waves, and log each fix below with the build it went into.

## Testers

| Tester | Wave | Background | Platform | Build | Sent | Folder back | Questionnaire back | Finished season | Notes |
|---|---|---|---|---|---|---|---|---|---|
| T1 | 1 | | | | | | | | |
| T2 | 1 | | | | | | | | |
| T3 | 2 | | | | | | | | |
| T4 | 2 | | | | | | | | |
| T5 | 2 | | | | | | | | |
| T6 | 2 | | | | | | | | |
| T7 | 2 | | | | | | | | |
| T8 | 2 | | | | | | | | |

**Background** is sim player, cricket person or both. **Build** is the version shown on launch, also in the zip's name.

## Builds sent

| Build | Date | Platforms | What changed |
|---|---|---|---|
| | | | |

## Blockers fixed between waves

| Found by | What happened | Fix | In build |
|---|---|---|---|
| | | | |

## How the questionnaire maps to the pass test
Kept here rather than on the questionnaire, so it doesn't steer the answers.

| Question | Pass test row or Gate C |
|---|---|
| 4 | Gate C: finished the season |
| 5 | Players plan strips ahead (two or more fixtures out) |
| 6 | Readings drive decisions |
| 7 | Pressures force real choices (turned down a request on purpose) |
| 8, 9 | Verdicts are readable; Gate C: can explain at least one verdict |
| 10 | The pace holds |
| 14 | Gate C: asks to play another |
| 1 to 3, 11 to 13, 15 | Context |

## Where returned folders go
Keep returned folders outside the repo, one folder per tester, each holding that tester's season folders:

```
~/Playtest/
  gatec.csv
  T1/season-2026-10-12-1930-18234.../
  T2/season-...
```

`gatec.csv` holds your reading of the questionnaires: did they explain a verdict (question 8), and would they play another season (question 14)?

```
tester,explained_verdict,play_again
T1,yes,yes
T2,no,maybe
```

Then run:

```
dotnet run --project src/Groundsman.Harness -- playtest ~/Playtest
```

It replays each save to check it, and reports each season, the pass-test rows and Gate C. If a tester played a build with different content, add `--content <that build's content folder>`.
