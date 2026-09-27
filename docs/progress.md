# Progress

## Current phase
Phase 2: weather, moisture and readings. Ends at Gate A: the by-the-book policy beats neglect in the harness.

## Phase 2 tasks
Detail in `docs/phase-2-plan.md`.
- [x] 1. Distributions: `NextGaussian` and `NextExponential`
- [x] 2. Weather: `climate.json`, `WeatherSystem`, observed weather in the view, truth inspector, first replay snapshot, harness trace mode
- [x] 3. Loams and moisture: `loams.json`, two-layer model in mm, watering in mm
- [ ] 4. Covers: commands, cover limit, no rain and slower drying under covers
- [ ] 5. Staff hours: `staff.json`, job costs, daily hours
- [ ] 6. Forecast: 7-day ranges, error growing with lead time
- [ ] 7. Readings: ageing, misses, staff skill, feel readings
- [ ] 8. Harness policies (neglect, random, by the book) and the Gate A check

Gate A stand-in score: a match strip lands when, on the match morning, subsurface moisture is inside a target band and surface moisture is below a ceiling (placeholders: 24 to 30%, under 22%). By the book must land at least 25 percentage points more often than neglect over 1,000 seasons. Phase 3 replaces this with the referee's pitch rating.

## Phase 1 tasks (done)
- [x] Solution skeleton: Core, Cli, Harness, Tests projects, references, one passing test, `IGame` interface
- [x] Seeded random generator with separate streams, plus replay tests
- [x] `GameTime`, calendar, hourly tick and pace rules for the next decision point
- [x] Strip state types, one command (water), a view built from readings
- [x] Bare console loop that advances time and prints the day and strip summary

## Later
- Compare the weather model with recent detailed observations for a real Midlands station (hourly or daily Met Office data, for example Birmingham or Nottingham over the last 10 to 20 years). Check monthly means and also the shape: spell lengths, hot-day and frost counts, daily range on sunny and dull days, rain intensity per hour, and how sunshine and temperature move together. Retune `climate.json` from it and record the source.

## Balance numbers to tune
All placeholders in `content/calendar.json`:
- Season dates: 1 April to 30 September
- Morning and afternoon decision hours: 07:00 and 13:00
- Off-season turn length: 7 days
- Final prep (half-day turns) before a match: 3 days
- Match-day decision hours: 08:00 (before play), 13:00 (lunch), 16:00 (tea), 18:00 (close)

In `content/ground.json`:
- Starting moisture per strip: 22 to 28% surface, 27 to 32% subsurface; all strips county loam

In `content/loams.json`:
- County loam: clay 30%, saturation 42%, field capacity 32%, air-dry 6%, drainage 0.08/h surface and 0.02/h subsurface, capillary 0.01/h, cracking 0.5
- Heavy clay loam (not used by the ground yet): clay 40%, saturation 46%, field capacity 36%, air-dry 8%, drainage 0.04 and 0.01/h, capillary 0.008/h, cracking 0.8

In `content/moisture.json`:
- Layers: surface 25 mm, subsurface 75 mm (together the top 100 mm the research talks about)
- Potential evaporation per hour: 0.004 mm per °C above zero, times 1 + 0.02 per km/h of wind, plus 0.2 mm per hour of sunshine

In `content/tasks.json` and `content/readings.json`:
- One watering is 12 mm (a deep watering; 5 mm barely reached depth)
- A moisture probe reading is 8 percentage points wide

In `content/climate.json`:
- Monthly normals for the fictional ground (Midlands-like): mean temperature, daily range, rain days, rain total, sunshine, wind, daylight
- Rain day threshold 1 mm; wet-day persistence 0.35; rain spells 2 to 8 hours
- Temperature anomaly: spread 2 °C, persistence 0.7 day to day
- Wind variability 0.35 (log scale); wet-day sunshine 40% of a dry day's
- Cloud: variability 1.2 (log-odds), persistence 0.5 day to day
- Sunshine widens the daily range by 1.2× the sunshine-fraction deviation; rain cools each rainy hour by 1.5 °C; wet days are 1.3× windier than dry days
- Sun warmth per month (°C per unit of sunshine fraction above average): −2 in midwinter up to +3 in summer

In `content/season.json` (stands in until `fixtures.json` in phase 4):
- Game start: 25 March 2027
- Match days: a four-day match 16 to 19 April, and a one-day match on 2 May

## Session log

### 2026-09-27
- Solution skeleton: `Groundsman.sln` with Core (netstandard2.1, C# 9 pinned), Cli (Spectre.Console), Harness and Tests (xUnit). `IGame` plus placeholder `GameView`, `AdvanceResult`, `IGameCommand`, and `CommandResult` with two tests.
- Seeded random generator: xoshiro256** in `Core/Randomness`, seeded via SplitMix64. `RandomStreams` holds one source per system (weather, forecast, readings, match, events); each stream's seed comes from the master seed and the stream's fixed id, so draws on one stream never shift another. State capture and restore for saves. Tests cover replay, stream independence, restore and bounds, and pin the output against an independent reference implementation.
- `GameTime` (one simulated hour) and calendar content in `content/calendar.json`, parsed in the core with Newtonsoft.Json from a string so file access stays in the front ends. Tests link `content/*.json` into their output and parse the shipped file.
- Pace rules: `PaceContext` classifies each day (off-season, in season, final prep, match day) from the calendar and a list of match dates; `PaceRules.NextDecisionPoint` never lets a weekly or daily step skip a finer day.
- Hourly tick: `TickStep` fixes the system order; `HourlyTick` runs registered `IHourlySystem`s in that order. `Game : IGame` advances hour by hour to the next decision point. `Submit` rejects everything until commands exist.
- Deferred: `Game` doesn't take a seed yet, since nothing draws random numbers. (Done later this session with readings; the fixed-seed replay test still waits for weather.)
- Match dates are passed in directly for now; fixtures will supply them later. Match-day hours are one list for every format; T20 evening starts will need per-format hours.
- Strips: `StripState` and `Square` are internal to the core, so front ends can't reach true state; the compiler enforces the truth/view split. The ground and its 12 strips come from `content/ground.json`.
- Water command: checked on submit, queued, and applied by `TasksSystem` in the Tasks step of the next hour. One watering per strip per turn.
- Readings: `TakeReading` gives a moisture probe reading straight away, since it observes rather than changes the ground. The true value sits at a random point in the range (Readings stream), so the midpoint doesn't reveal it. One reading per strip per turn. `GameView` shows the ground name and each strip's latest reading or none. `Game` now takes a seed through `GameSetup`, with content bundled in `GameContent`.
- For phase 2: readings don't age, and there are no misses, skill or feel readings yet. Until weather dries the strips, reading the same untouched strip on several days lets a player intersect the ranges and narrow in on the truth. Ranges aren't clamped, so a strip near saturation can read above 40%.
- Console loop: the Cli loads `content/*.json` from next to its build, takes `--seed N` or picks and prints one, and runs `GameLoop`. Commands: `r <strip>`, `r all`, `w <strip>`, `s`, Enter or `a` to advance, `h`, `q`. Ranges display rounded outwards so the true value stays inside. Piped stdin is read line by line and echoed, since Spectre's prompts refuse redirected input; end of input quits. Tests drive the loop through Spectre's `TestConsole`.
- `GameView` gained `Pace` and `NextMatchDay`, and `StripView` gained `WateringQueued`.
- Phase 1 complete. Try it with `dotnet run --project src/Groundsman.Cli -- --seed 7`.
- Phase 2 planned in detail in `docs/phase-2-plan.md`, using the research notes (now `docs/research.md`). Staff hours come into phase 2 because the research makes labour hours the core resource. The Gate A score now checks moisture at depth plus a dry surface, matching the research's target.
- Distributions: `RandomSource.NextGaussian` (Box-Muller, one value per call so saves only need the generator state) and `NextExponential`. Pinned against the Python reference with a tolerance for maths-library differences, plus mean and spread checks over 100,000 draws.
- Weather: `climate.json` monthly normals; `WeatherGenerator` makes a day at a time (two-state chain for wet spells, exponential wet-day totals above the 1 mm threshold in one spell, temperature anomaly carried day to day with a daily curve, log-normal wind, sunshine spread over daylight). `WeatherSystem` runs in the Weather step and generates days in date order, so looking ahead for forecasts won't change them. Over 200 simulated years, monthly rain, rain days, temperature, sunshine and wind all match the normals within tolerance.
- The view shows observed weather: rain gauge for the last 24 hours, and the last hour's temperature and wind. These are exact, since instruments aren't part of the information puzzle.
- Truth inspector: `Game.Inspect()` returns a `TruthSnapshot`, kept apart from `GameView`. The Cli's `--debug` flag shows it in a magenta column.
- First replay snapshot (`tests/Groundsman.Tests/Snapshots/replay-season-start.json`): 80 turns of scripted play on test content. After an intended rule change, rerun with `UPDATE_SNAPSHOTS=1 dotnet test` and review the diff.
- Harness: `weather --years N` (daily CSV plus monthly comparison with normals) and `trace` (truth at every decision point of an untouched season), writing to `harness-output/`. The harness links the Cli's `ContentLoader.cs`; move it to a shared project if more code ends up shared.
- Weather check, 100 years from seed 1: all months close to normal. The largest gaps are rain in July (61 vs 55 mm) and September (60 vs 55 mm), about 10% over, which is within sampling noise. Sunshine is fixed per wet or dry day within a month, with no day-to-day variation; add some if days feel samey.
- Weather revision, after play feedback that summer felt cold. Two causes: the screen showed the 06:00 temperature, near the overnight low, and hot days were too rare (about 2 days of 25 °C or more in July). Weather now follows one chain per day: wet or dry, then cloud (carried day to day), then sunshine (none while rain falls), then temperature. Sun widens the daily range and warms summer days or chills winter nights, and the warmth carries over, so heatwaves come from sunny dry spells. Wet days are windier. Every effect is centred on the monthly average, so the normals still hold.
- Result over 200 years: July about 6 days of 25 °C or more (June 2, August 4), occasional 30 °C days, hottest 35 °C; averages unchanged. Winter sunshine runs about 4% under normal because rain spells can fill a short winter day, which was accepted rather than compensated.
- The view shows yesterday's low and high from the max-min thermometer. Replay snapshot regenerated: only rain, temperature and wind changed.
- Loams and moisture: `loams.json` (per-loam saturation, field capacity, air-dry, drainage, capillary and cracking values); ground strips name their loam; `moisture.json` holds layer depths and evaporation coefficients. `MoistureModel` moves water in mm each hour: rain unless covered plus watering in, overflow soaks down then runs off, drainage above field capacity, capillary rise into a drier surface, evaporation from temperature, wind and sun slowing towards air-dry. `MoistureSystem` runs it in the Moisture step and takes queued watering as its input. The model has invariant tests (bounds, covers, drainage by clay, conservation, watering reaching depth over a day).
- Tuning from harness traces, 8 seeds, untouched strips: surface dries from wet to about 12% in 2 to 3 days of summer weather, the subsurface loses about a point a day in a dry spell, and moist at depth with a dry surface emerges on its own. Untouched strips meet the Gate A target on 23% of May to September mornings, missing about equally from dry depth (38%), wet depth (32%) and wet surface (34%), which leaves room for play to matter.
- Watering raised from 5 to 12 mm after a side-by-side run: 5 mm added about 1 point at depth and was gone in three days; 12 mm adds about 7 points at depth overnight and the surface dries back within two days, matching the research's water deeply then let it dry. Heavy rain soon after can push a watered strip near waterlogged, as the research warns.
- Game-level watering tests now compare against the same seed unwatered, because moisture moves every hour. Replay snapshot regenerated for the moisture model.
- Next: phase 2 task 4, covers.
