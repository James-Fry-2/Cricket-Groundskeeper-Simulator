# Progress

## Current phase
Phase 2 complete: Gate A passed. Next is phase 3, matches and verdicts (plan it in detail first, as for phase 2).

## Phase 2 tasks
Detail in `docs/phase-2-plan.md`.
- [x] 1. Distributions: `NextGaussian` and `NextExponential`
- [x] 2. Weather: `climate.json`, `WeatherSystem`, observed weather in the view, truth inspector, first replay snapshot, harness trace mode
- [x] 3. Loams and moisture: `loams.json`, two-layer model in mm, watering in mm
- [x] 4. Covers: commands, cover limit, no rain and slower drying under covers
- [x] 5. Staff hours: `staff.json`, job costs, daily hours
- [x] 6. Forecast: 7-day ranges, error growing with lead time
- [x] 7. Readings: ageing, misses, staff skill, feel readings
- [x] 8. Harness policies (neglect, random, by the book) and the Gate A check

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
- Root uptake from the subsurface: 0.3 of the hour's evaporation demand, curve 3 (strong near field capacity, weak when dry)
- Potential evaporation per hour: 0.004 mm per °C above zero, times 1 + 0.02 per km/h of wind, plus 0.2 mm per hour of sunshine

In `content/covers.json`:
- 4 covers; evaporation under a cover 30% of an open strip's

In `content/staff.json`:
- You 8 h/day (reading skill 1.0), Sam Hollis deputy 8 h (0.8, tighter), Jo Pike casual 6 h (1.4, looser)
- Job hours: water 1, cover 0.25, uncover 0.25, probe reading 0.25, feel reading 0.05

In `content/forecast.json`:
- 7 days from today; ranges 1.5 error spreads either side (truth inside about 87% of the time)
- Rain error spread 1.5 mm on the day, growing 40% per day ahead; top temperature 1.0 °C, growing 30% per day

In `content/season.json` and `content/scoring.json`:
- 18 placeholder fixtures, 16 April to 25 September, four-day and one-day, on rotating strips
- Gate A stand-in: subsurface 24 to 30%, surface under 22% on the first morning

In `content/tasks.json` and `content/readings.json`:
- One watering is 12 mm (a deep watering; 5 mm barely reached depth)
- Soil core: 6 points wide, 5% miss rate at skill 1, 0.75 h
- Probe reading: 8 percentage points wide and an 8% miss rate at skill 1; a miss lands up to a quarter of the width off the truth
- Feel reading: judgement spread 2.5 points at skill 1; bands dry below 16%, damp 16 to 26%, wet 26% and up
- Ageing: ranges widen 1 point per day plus 0.5 points per mm of rain or watering since the reading

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
- Covers: `CoverStrip` and `UncoverStrip` commands, queued and applied in the Covers step before that hour's moisture. A covered strip takes no rain and evaporates at 30% of the open rate; watering still goes in, so water-then-sheet works as the research describes. The number of covers is limited; an uncover order frees its cover for another strip in the same turn. The view shows cover state, cover orders, and covers free out of owned. Cli: `c <strip>`, `u <strip>`, a Cover column and a covers-free line. The replay snapshot didn't change, confirming uncovered play is untouched.
- Staff hours: `staff.json` lists the staff (the player first) with hours per day and a reading skill, plus hours per job. Every strip command takes an optional `StaffId` and defaults to the player; a job is refused if that person hasn't the hours left, and a refused job costs nothing. Hours belong to a calendar day and refill when an advance reaches a new date, so morning and afternoon turns share them. Readings record who took them. Reading skill is stored but not applied until task 7. Cli: `r 3 sam` style names, an hours-left line, readings show who took them, and `r all` skips strips already read this turn and stops at the first refusal.
- Forecast: `Forecaster` issues one each morning (the first turn on a new date) covering today and six days on. Each day is the true weather plus an error drawn at issue from the Forecast stream, spread growing with lead; ranges are the value ± 1.5 spreads, so the truth sits inside about 87% of the time (tested over 400 seeds). Widths depend only on lead, so they can't leak the truth; rain ranges clamp at 0. Stable between turns on the same day. Looking ahead doesn't change the weather, since days are always generated in date order (tested against a standalone generator). The replay snapshot now includes the forecast, added without changing any existing line. Cli: a forecast panel of rain and high-temperature ranges.
- Chance of rain added to the forecast. A naive figure from the forecast value alone was badly calibrated (it said 70 to 80% on days that rained 28% of the time), because most days are dry. It now weighs the forecast value against the month's rain-day rate and wet-day amounts, and a test over 1,000 seeds holds each band within 8 points of what actually happens. No truth and no extra random draws.
- Readings: probe width and miss rate both scale with the taker's reading skill (tested: Sam 0.8, you 1.0, Jo 1.4 miss about 6%, 8% and 11%). A missed probe reading lands up to a quarter of its width off the truth. Feel readings (`TakeReading` with `ReadingSource.Feel`) cost 0.05 h and give a word whose band is the range, judged on truth plus noise scaled by skill, so borderline strips can get the wrong word. `KnowledgeStore` tracks water known to have reached each strip since its reading (rain while uncovered, from the gauge and cover state, plus ordered watering) and `StripView.SurfaceMoistureNow` gives the range widened for age and that water, clamped at 0. One reading of either kind per strip per turn. Cli: `f <strip> [name]`, `f all`, and the table shows the feel word or the widened range. Replay snapshot updated; only reading lines changed.
- Found and fixed while doing this: a parser edit briefly deleted `ParseSeason` and `ParseClimate`; the build caught it and they were restored from the last commit unchanged.
- Fixtures: `season.json` lists fixtures (start, days, strip); match days derive from them; the view shows the next fixture and the Cli names its strip.
- Harness: `IPolicy` plays through `IGame`, which has no `Inspect`, so policies see only what a player sees. Neglect, random, and by the book (the research loop: Sam reads the strip each morning from 10 days out, water deeply 5 days out unless it reads wet or rain is likely, again 4 days out if still dryish, a top-up 3 days out only if parched, cover in the final 3 days when the chance of rain is 40% or more, uncover otherwise). `SeasonRunner` scores each fixture's strip from the truth at 08:00 on its first day, before the policy acts. `harness gate --seasons N` runs all three in parallel with fixed results order, prints a summary and writes per-match CSV; 1,000 seasons take about 2 seconds.
- Gate A, first run over 1,000 seasons: neglect 20%, random 24%, by the book 37% on target. NOT PASSED (17-point lead, 25 needed). Not tuned to pass.
- Diagnosis from policy variants over 400 seasons (scratch code, not committed): watering only 25% (too wet below 54%), covers only about 24% (too dry below about 50%), a smarter honest policy 35%, and an oracle that sees the true moisture 59% (still too wet below 31%). Two causes: (1) the target at depth (24 to 30%) sits just under the loam's field capacity (32%), and below field capacity the model only dries the subsurface by slow capillary rise, about a point a day, so after decent rain a strip stays too wet at depth for days whatever the player does; (2) the probe reads only the surface, so honest play is guessing about the thing that's scored, which is why it tops out near 36% while the oracle reaches 59%.
- Decision (user): fix both causes rather than widen the target or lower the bar.
- Drying at depth: grass roots now draw a share of the hour's evaporation demand from the subsurface, falling away sharply as it dries (`rootUptakeShare` 0.3, `rootUptakeCurve` 3 in `moisture.json`). Chosen against a realism check from traces: a strip at field capacity dries into the 24 to 30% band in about 2.5 dry summer days; summer depth averages 22%; long droughts bottom out near 9%. A straight-line version strong enough to dry at depth over-dried the whole season.
- Soil cores: `TakeReading` with `ReadingSource.SoilCore` reads the subsurface (6 points wide, 5% misses at skill 1, 0.75 h), counted separately from surface readings, ageing the same way. Cli: `d <strip> [name]`, `d all`, Below and Cored columns. The replay snapshot was unchanged by cores (the script doesn't use them).
- By the book now cores the match strip each morning from 7 days out (Sam), probes the surface in the final 3 days, waters when the core reads under 25% and rain is unlikely (under 21% two days out), covers at 40% chance of rain when the profile is at 26% or more or in the final 3 days, and otherwise only covers a dry strip against heavy rain. Its depth thresholds sit inside the research's 25 to 30% working figure, which a groundsman would know; it doesn't read `scoring.json`.
- Gate A over 1,000 seasons: neglect 13%, random 18%, by the book 50% on target. PASSED, 37-point lead (25 needed). On seeds 5001 to 6000, never seen while choosing thresholds: neglect 13%, by the book 49%, and an oracle that sees the truth 58%, so honest play with cores gets within 9 points of perfect knowledge.
- Worth revisiting: a soil core leaves a hole in a real pitch; cores might later cost something on the match strip itself. Neglect's biggest miss is now too dry at depth (63%), since roots dry untended strips through the summer.
- Phase 2 complete.
