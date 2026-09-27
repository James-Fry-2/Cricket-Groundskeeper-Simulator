# Progress

## Current phase
Phase 1: strip model and daily loop

## Phase 1 tasks
- [x] Solution skeleton: Core, Cli, Harness, Tests projects, references, one passing test, `IGame` interface
- [x] Seeded random generator with separate streams, plus replay tests
- [x] `GameTime`, calendar, hourly tick and pace rules for the next decision point
- [x] Strip state types, one command (water), a view built from readings
- [ ] Bare console loop that advances time and prints the day and strip summary

## Balance numbers to tune
All placeholders in `content/calendar.json`:
- Season dates: 1 April to 30 September
- Morning and afternoon decision hours: 07:00 and 13:00
- Off-season turn length: 7 days
- Final prep (half-day turns) before a match: 3 days
- Match-day decision hours: 08:00 (before play), 13:00 (lunch), 16:00 (tea), 18:00 (close)

In `content/ground.json`:
- Starting moisture per strip: 22 to 28% surface, 27 to 32% subsurface
- Saturation: 40% (move to `loams.json` when loams arrive in phase 2)

In `content/tasks.json` and `content/readings.json`:
- One watering adds 4 percentage points of surface moisture
- A moisture probe reading is 8 percentage points wide

## Session log

### 2026-09-27
- Solution skeleton: `Groundsman.sln` with Core (netstandard2.1, C# 9 pinned), Cli (Spectre.Console), Harness and Tests (xUnit). `IGame` plus placeholder `GameView`, `AdvanceResult`, `IGameCommand`, and `CommandResult` with two tests.
- Seeded random generator: xoshiro256** in `Core/Randomness`, seeded via SplitMix64. `RandomStreams` holds one source per system (weather, forecast, readings, match, events); each stream's seed comes from the master seed and the stream's fixed id, so draws on one stream never shift another. State capture and restore for saves. Tests cover replay, stream independence, restore and bounds, and pin the output against an independent reference implementation.
- `GameTime` (one simulated hour) and calendar content in `content/calendar.json`, parsed in the core with Newtonsoft.Json from a string so file access stays in the front ends. Tests link `content/*.json` into their output and parse the shipped file.
- Pace rules: `PaceContext` classifies each day (off-season, in season, final prep, match day) from the calendar and a list of match dates; `PaceRules.NextDecisionPoint` never lets a weekly or daily step skip a finer day.
- Hourly tick: `TickStep` fixes the system order; `HourlyTick` runs registered `IHourlySystem`s in that order. `Game : IGame` advances hour by hour to the next decision point. `Submit` rejects everything until commands exist.
- Deferred: `Game` doesn't take a seed yet, since nothing draws random numbers. Add it with the first random system (weather) and a fixed-seed replay test then.
- Match dates are passed in directly for now; fixtures will supply them later. Match-day hours are one list for every format; T20 evening starts will need per-format hours.
- Strips: `StripState` and `Square` are internal to the core, so front ends can't reach true state; the compiler enforces the truth/view split. The ground and its 12 strips come from `content/ground.json`.
- Water command: checked on submit, queued, and applied by `TasksSystem` in the Tasks step of the next hour. One watering per strip per turn.
- Readings: `TakeReading` gives a moisture probe reading straight away, since it observes rather than changes the ground. The true value sits at a random point in the range (Readings stream), so the midpoint doesn't reveal it. One reading per strip per turn. `GameView` shows the ground name and each strip's latest reading or none. `Game` now takes a seed through `GameSetup`, with content bundled in `GameContent`.
- For phase 2: readings don't age, and there are no misses, skill or feel readings yet. Until weather dries the strips, reading the same untouched strip on several days lets a player intersect the ranges and narrow in on the truth. Ranges aren't clamped, so a strip near saturation can read above 40%.
- Next: bare console loop that advances time and prints the day and strip summary.
