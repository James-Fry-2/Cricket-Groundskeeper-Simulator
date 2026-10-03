# Phase 5 plan: first playtest

Oct 3, 2026 · @James Fry

## Context
Phase 4 made a whole season playable, with strip rotation, requests and a season review, and Gate B showed in the harness that planning ahead beats greedy play. Phase 5 puts the season in front of 5 to 8 people and asks the MVP's question: is rotating strips while balancing pressures fun? It ends at Gate C: most testers finish the season, can explain at least one verdict, and ask to play another.

Grounded in [the MVP plan](mvp-plan.md) ("Goal and pass test", "Content, saves and determinism", "Playtest telemetry", the risk table) and [progress](progress.md) (things to watch from phases 3 and 4).

Decisions made for this phase:
- **Saves:** the seed, the content version and every command with the turn it was given; loading replays them. A save made with different content is refused rather than migrated. This replaces the MVP plan's full state snapshot, which can come later if saves must survive content changes.
- **Pace:** a fast-forward that advances until something needs the player. A season is 353 turns today.
- **Returns:** at the end of a season the game names one folder holding the save, the telemetry and an optional notes file. Testers zip it and send it with the questionnaire. Nothing leaves their machine on its own.
- **Testers:** a mix of management-sim players and cricket people, so onboarding explains both the interface and the cricket.

Out of this phase: the Unity UI (phase 6), new mechanics, varied soils, renovation and multiple seasons of consequence, automatic uploads.

## Tasks
Rough hours, 40 to 55 for the build, then the playtest itself, which is mostly waiting. Each task keeps the usual rules: tests first, numbers in content, and the replay snapshot updated only for intended changes.

### 1. Saves (8 to 10 h)
- **Contents:** a save file (JSON) holds:
  - a schema version;
  - a content hash over every content file;
  - the seed and game start;
  - each command, tagged with the number of the turn it was submitted in.
- **Recording:** the Cli records accepted commands only; rejected ones change nothing, so replay doesn't need them.
- **Loading:** replay the commands turn by turn and check that the result matches. A loaded game's view and truth must equal those of the original at the same turn; this is a test over many seeds and scripted sessions.
- **Saving and resuming:**
  - autosave after every turn and command, to a per-season file in the playtest folder;
  - on launch the Cli offers to resume the last unfinished season;
  - `save` is also a command, to name a copy.
- **Refusals:** a save whose content hash or schema doesn't match is refused, with a clear message.
- **Core changes:** none to the simulation. The core gains a content hash, and either a command journal or an `IGame` wrapper that records commands.

### 2. Telemetry (5 to 7 h)
- **Format:** a JSON Lines log beside the save, one line per event. Each line carries the turn, the game time and the wall-clock time; wall clock lives in the Cli only, never in the core.
- **Events, each mapped to a row of the pass test:**
  - **Planning ahead:** each strip assignment, with how many fixtures ahead it was made.
  - **Readings drive decisions:** each reading (tool, strip, who) and each job ordered, so "a reading before a key job" and "a plan changed after a reading" can be found.
  - **Pressures force choices:** each request answered, and how.
  - **Pace:** each advance or fast-forward, with the time spent on that turn.
  - **Readable verdicts:** each time the verdict, record or review is viewed.
  - **Context:** session start and end, and the season review summary.
- **Consent:** the first launch says what's recorded and where, in plain words. No personal data is recorded.

### 3. Fast-forward (4 to 6 h)
- **Command:** `ff` advances turn by turn until something needs the player:
  - a request arrives;
  - a strip locks;
  - a build-up starts for a fixture with no strip;
  - a match day begins, or a session break comes;
  - rain is forecast at 60% or more for a strip in its build-up;
  - the season ends.
- **Stopping:** the stop rules live in the Cli, as queries on the view, so they can't depend on truth. It never skips a turn on which a notice arrives.
- **Telemetry:** each fast-forward is logged with how many turns it skipped.

### 4. Onboarding (6 to 8 h)
- **First launch:** a one-screen introduction covering:
  - the job, the ground and the season;
  - what the captain, the board and the referee want;
  - the four commands to start with.
- **First-season tips:** short hints the first few times something happens, such as the first request, the first lock, the first match day and the first verdict. Each says what it means and which command answers it. They're switched off in later seasons.
- **A quick-start guide, `docs/playtest/guide.md`, for testers.** It covers the interface for cricket people and the cricket for sim players:
  - what a strip, rolling, footholes and a referee's rating are;
  - how to read the screens.
- **Help:** examples added to the help screen where the syntax isn't obvious.

### 5. Tester builds (4 to 6 h)
- **`scripts/publish.sh`:** publishes the Cli self-contained, single-file, for macOS (Apple silicon and Intel) and Windows x64, with `content/` alongside. It stamps a version from git into the binary, the saves and the telemetry.
- **A README per platform:** how to run it, where the playtest folder is, and how to send it back.
  - **Mac:** the build is unsigned, so the README explains the first-run "Open anyway" step.
  - **Windows:** a terminal that handles Unicode box drawing.
- **Checks before sending:** each build is run on its own platform (Windows in a VM, as the MVP plan says), loading a save, fast-forwarding and reaching the review.

### 6. Playtest kit (3 to 4 h)
- **`docs/playtest/questionnaire.md`:** short, and mapped to the pass test:
  - which strip they meant for which fixture, and when they decided;
  - one verdict they can explain;
  - a request they turned down and why;
  - where it dragged;
  - would they play another season.
- **`docs/playtest/briefing.md`:** what we're testing, that looks come later, how long it takes, and what to send back.
- **A tracker** for who has which build and what came back.

### 7. Telemetry analysis (6 to 8 h)
- **`harness playtest <folder>`:** reads returned folders, replays each save to check it, and reports the pass-test rows per tester and overall:
  - assignments made two or more fixtures ahead;
  - readings taken before key jobs, and plans changed after a reading;
  - requests turned down on purpose;
  - runs of plain advances, and fast-forward use;
  - sessions to finish;
  - the review reached.
- **Gate C summary:**
  - Combines the report with the questionnaire answers: finished, explained a verdict, asked to play another.
  - Thresholds from the MVP plan: most of the testers on each.

### 8. Run the playtest (mostly waiting, 2 to 4 weeks)
- Send builds in two waves: two testers first to catch blockers, then the rest.
- Fix only blockers between the waves, and log every change.
- Collect the folders and questionnaires, run the analysis, record Gate C in `progress.md`, and agree with the user what phase 6 or a second round should change.

## Dependencies
1 → 2 (telemetry rides on the command recording) → 7. Task 3 before 2's pace events are final. Task 4 after 3, since tips mention `ff`. Task 5 after 1 to 4. Task 6 can run alongside. Task 8 needs everything.

## Risks
| Risk | Fallback |
|---|---|
| A rule change between waves breaks earlier saves | Content hash refuses them clearly; testers start a new season, and the old folder is still analysed from its log |
| Replay drifts from play (a hidden dependence on something not recorded) | The replay-equals-original test over many seeds and scripted sessions; a mismatch on load is reported, not hidden |
| Unsigned builds put testers off | README with the exact steps and a screenshot; offer a call to the first wave |
| Testers judge the text UI, not the loop | Recruit people who play management sims, and brief them that looks come later (MVP risk table) |
| The season takes too long | Fast-forward; telemetry shows where time goes; the second wave can get a shorter fixture list if needed |
| Harsh numbers from phase 4 (third use near-certain unsatisfactory, reused strips never very good) feel unfair | Watch for it in the questionnaire; numbers are in content, so tune between waves |

## Verification
- **Tasks 1 to 3:**
  - `dotnet build -warnaserror` and `dotnet test` pass;
  - a loaded save equals the original over many seeds;
  - fast-forward never skips a turn with a notice.
- **Task 5:** each build reaches the review from a fresh start and from a loaded save, on its own platform.
- **Task 8:** Gate C recorded in `progress.md` with testers (anonymised), results per pass-test row, and the decision on what comes next.
