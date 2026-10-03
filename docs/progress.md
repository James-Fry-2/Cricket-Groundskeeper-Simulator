# Progress

## Current phase
Phase 5: first playtest (5 to 8 testers). Plan in `docs/phase-5-plan.md`. Ends at Gate C: most testers finish the season, can explain at least one verdict, and ask to play another.

## Phase 5 tasks
- [x] 1. Saves (seed plus command log, autosave, resume)
- [x] 2. Telemetry (JSON Lines, mapped to the pass test)
- [x] 3. Fast-forward to the next event
- [x] 4. Onboarding (intro, first-season tips, quick-start guide)
- [ ] 5. Tester builds (macOS and Windows, self-contained)
- [ ] 6. Playtest kit (briefing, questionnaire, tracker)
- [ ] 7. Telemetry analysis and the Gate C summary
- [ ] 8. Run the playtest in two waves

## Phase 4 tasks (done)
- [x] 1. Fixture ids and strip assignment
- [x] 2. Square positions and neighbour wear
- [x] 3. Lasting wear and establishment
- [x] 4. Stakeholders and requests
- [x] 5. Season review
- [x] 6. Cli: fixtures, assignment, requests, review
- [x] 7. Harness policies (greedy, planned) and Gate B

## Phase 3 tasks (done)
- [x] 1. Formats, teams and fixture details
- [x] 2. Grass: growth and mowing
- [x] 3. Compaction and rolling
- [x] 4. Pitch characteristics
- [x] 5. Wear and recovery
- [x] 6. Match engine
- [x] 7. Commentary
- [x] 8. Rating and demerits
- [x] 9. Cli: rolling, mowing, repairs, match and verdict screens
- [x] 10. Harness and check 3

Decided for phase 3: the ICC's current rating scale (very good, satisfactory, unsatisfactory, unfit; demerits 1 and 3 on a five-year window); rolling and mowing in scope; matches played session by session with Law 9 interval jobs.

## Phase 2 tasks (done)
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
- Varied soils per strip (after the MVP): different loams and starting condition across the square, so strips have their own character (pacy, slow, crumbly) as well as position and wear. Decided with the user during phase 4 planning to keep the MVP to position plus lasting wear.
- Saves (versioned JSON with the random state) and playtest telemetry (JSON Lines): moved from phase 4 to phase 5, playtest preparation.
- Compare the weather model with recent detailed observations for a real Midlands station (hourly or daily Met Office data, for example Birmingham or Nottingham over the last 10 to 20 years). Check monthly means and also the shape: spell lengths, hot-day and frost counts, daily range on sunny and dull days, rain intensity per hour, and how sunshine and temperature move together. Retune `climate.json` from it and record the source.


## Balance numbers to tune
All placeholders in `content/calendar.json`:
- Season dates: 1 April to 30 September
- Morning and afternoon decision hours: 07:00 and 13:00
- Off-season turn length: 7 days
- Final prep (half-day turns) before a match: 3 days
- Strip assignment locks 10 days before a fixture, when its build-up starts

In `content/ground.json`:
- Starting moisture per strip: 22 to 28% surface, 27 to 32% subsurface; all strips county loam
- Centre strips: 5 to 8 of 12

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

In `content/rollers.json` and `content/compaction.json`:
- Rollers (share of the remaining room to full compaction closed per hour in the window): light 0.04, medium 0.08, heavy 0.12
- Wet-rolling damage per hour: light 0.01, medium 0.03, heavy 0.08, plus 10% more per point of surface moisture over the window
- Heavy roller overuse: damage 0.05 per hour, scaled by how far compaction is above 0.8
- Starting compaction 0.62 (was 0.55 until phase 4 task 7), no structure damage; rolling 5 to 120 minutes
- Hardness = compaction × (0.3 + 0.7 × surface dryness) × (clay / 30)^0.5, capped at 1
- Rolling windows (surface moisture): county loam 18–26%, heavy clay 20–29%

In `content/pitch.json` (all characteristics 0 to 10):
- Consistency lost to earlier use: 3.0 × lasting wear, 0.15 × (1 − ends establishment)
- Lasting wear deadens: pace and bounce × (1 − 6 × lasting wear)
- Pace = hardness × grass cushion; grass cushions up to 40% of pace and bounce from 10 mm (none) to 20 mm or more (full)
- Bounce = cushion × compaction × (clay / 30)^0.5 × (0.6 + 0.4 × moisture at depth)
- Consistency = 1 − 0.6 × structure damage − 1.0 × looseness below 0.65 compaction − 0.4 × cracks − 0.4 × footholes − 0.2 × surface wear
- Carry = √(pace × bounce); seam = grass × (0.3 + 0.7 × surface wetness), grass = cover × height / 10 mm (capped)
- Spin = 0.6 × surface dryness + 0.3 × cracks + 0.5 × rough + 0.4 × surface wear − 0.4 × grass; cracking = cracks

In `content/match.json` and the formats:
- Base rates per over on a true, neutral pitch between even sides: four-day 3.2 runs and 0.07 wickets; one-day 5.2 and 0.14; T20 8.0 and 0.3
- Strength: rates shift by e^(0.8 × gap / 50); wickets +1.2 × seam share × seam/10, +1.2 × spin share × spin/10, +2.0 × unevenness, −0.4 × deadness; runs +0.4 × (carry/10 − 0.5), −0.3 × unevenness, −0.3 × deadness; deadness = 2 × max(0, 0.5 − carry/10); runs spread 0.35
- Declaration from day 3 at a lead of 250; half the next hour lost after rain; the toss winner bowls first when seam is over 6
- Law 9 jobs: cleaning takes 10% off footholes (0.25 h), filling at close of play 40% (1 h)

In `content/commentary.json`:
- Event thresholds: seam 6, dead (carry) 3, uneven (consistency) 6, keeping low (bounce) 3.5, turn 5, dust (surface wear) 0.4, cracks 0.3, footholes 0.3, referee inspects (consistency) 4, collapse 4 wickets in an hour, true pitch (consistency) 8.5 with carry 6 and little seam or turn
- All wording is placeholder text to rewrite for tone

In `content/rating.json`:
- Unfit: consistency under 3 at any hour of play (3 demerits); the referee abandons play under 2.5
- Unsatisfactory (1 demerit): average consistency under 6.5; average carry under 3.5; a multi-day result inside half the scheduled overs at under 20 runs a wicket; limited-overs innings both under half of par; a multi-day draw at over 50 runs a wicket with seam and turn never reaching 3
- Very good: average consistency 8.5 or more, carry 5.5 or more, and in multi-day cricket seam of 4 on day one or turn of 4 later
- Demerits count for 5 years; 5 in the window is a ban

In `content/wear.json`:
- Resistance = 0.5 × compaction + 0.3 × clay (full at 30%) + 0.2 × roots (share of 100 mm), halved when the surface is wetter than the rolling window
- Per over, before resistance: footholes 0.004 × seam share × (1 + heavy-footedness); rough 0.003 × (1 + 0.5 × left-arm share); surface wear 0.002 (1.5× when drier than the window); grass cover 0.05 points
- Play in the wet: all wear × 2
- Cracks open once surface dryness passes 0.6, up to 0.01 an hour × loam tendency, ×(1 + 2 × structure damage); close 0.02 an hour at or above field capacity
- Recovery: 0.02 a day at full grass growth, 0.08 once the ends are repaired; a repair fills 60% of footholes and rough; 2 h in `staff.json`
- Run-ups: a match's neighbours take 0.25 of the footholes dug on the match strip (× 2 when the neighbour is wet)
- Lasting wear: 0.06 of the wear a match digs; each unit takes half off resistance
- Ends: footholes dug × 4 off establishment; regrow over 18 growing days repaired, 45 unrepaired; look seeded below 0.3; bare ends take 0.4 off resistance

In `content/grass.json`:
- Start (late March): cover 85%, height 15 mm, roots 60 mm
- Growth by temperature: none below 5 °C, best at 18 °C, none above 30 °C; scaled by water at depth
- At best: 1.2 mm height, 1.0 point cover and 1.0 mm roots a day; cover up to 95%, roots up to 100 mm
- Drought (water at depth under 0.15 of the way from air-dry to field capacity): no growth, cover thins 0.5 points a day
- Mowing: 3 to 50 mm; taking more than 34% of the height at once scalps, costing 2 points of cover per mm over; 0.5 h a strip

In `content/formats.json`:
- Four-day: 2 innings each, 96 overs a day, play 11–13, 14–16, 16–18; turns at 08:00, 13:00, 16:00, 18:00
- One-day: 50 overs an innings, play 11–14 and 15–18; turns at 08:00, 14:00, 18:00
- T20: 20 overs an innings, play 18–20 and 20–22; turns at 08:00, 18:00, 20:00, 22:00

In `content/teams.json`:
- Home: Kestrelshire; opponents Harrowmere, Saltmarsh, Oakhollow, Brackenridge, Fenwick, each with batting and bowling strength (50–72) and an attack profile (seam 0.5–0.85, left-arm 0–0.35, heavy-footed 0.2–0.8)

In `content/season.json` and `content/scoring.json`:
- 18 placeholder fixtures, 16 April to 25 September: four-day, one-day and T20 against rotating opponents, on rotating strips
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

In `content/season.json`:
- Game start: 25 March 2027
- 18 fixtures, 16 April to 22 September: eight four-day, five one-day, five T20
- Televised: 8 of the 18, spread through the season (13 May, 6 and 13 June, 4 July, 2 and 14 August, 1 and 14 September)

In `content/stakeholders.json`:
- Satisfaction starts at 50 of 100; requests arrive 5 days before the lock
- Captain asks on 60% of fixtures: four-day green 2 / turning 1 / pace 1, one-day pace 1 / flat 1, T20 flat 2 / pace 1; delivered +12, not delivered −15, declined −4, ignored −6; home win +3, loss −3
- Board asks 30% of four-day matches to last; delivered +8, not delivered −12, declined −3, ignored −5; day four +4, over in three days −6, no result −4, televised centre +3, televised outer −6, −10 a demerit
- Referee: very good +6, satisfactory +2, unsatisfactory −8, unfit −20
- Delivery: green day-one seam 5, turning last-day spin 4, pace carry 5.5, true consistency 8, flat movement under 3
- Review moods: delighted from 75, content from 55, uneasy from 40; square wear: light from 0.01, worn 0.03, heavy 0.06 lasting wear

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
- Phase 3 planned in `docs/phase-3-plan.md`: rating scale, rolling and mowing, and session-by-session matches decided with the user.
- Formats, teams and fixtures: `formats.json` (days, innings per side, overs per innings or per day, session play hours, match-day turns) and `teams.json` (home side and opponents, strengths, attack profile). Fixtures name a format and opponent; their length comes from the format. `PaceContext` takes fixtures and gives each match day its format's turns, so T20 days turn in the evening; match-day hours left `calendar.json`. Test formats keep the old turn times, so the replay snapshot didn't change, and Gate A is unchanged (neglect 13%, by the book 50%). Sessions, innings and overs are parsed and checked now and used by the match engine in task 6.
- Grass: strips carry cover, height and root depth. `GrassModel` grows them each hour in the Grass step by a temperature curve and water at depth, and thins cover in drought. `MowStrip` is queued and applied in the next hour's Grass step (like watering in the Moisture step); scalping costs cover; cutting above the current height does nothing but still costs the time. The view shows the last cut you ordered (`MowRecord`), not the true height, keeping to the readings-only rule; a way to judge height directly could come later as a reading. `Inspect()`, the season trace and the replay snapshot now include grass (the snapshot script mows too; only lines were added). Unmown growth over a traced season: about 0.2 mm a day in April, 0.7 to 0.8 in May, June and August, 0.3 in a dry July with cover thinning, so holding 6 to 8 mm takes 2 to 3 cuts a week as the research says. Cli: `m <strip> <mm> [name]`, `m all <mm>`, a Cut column; the table was compacted to wrap less at 80 columns.
- Compaction and rolling: strips carry compaction and hidden structure damage. `RollStrip` (roller id, minutes, optional staff) is queued and applied in the Tasks step, after that hour's moisture, so the window is judged on the strip as it stands; rolling costs its minutes. Inside the loam's window it compacts with diminishing returns; drier does nothing; wetter still compacts but damages structure, heavier rollers more; the heavy roller on a tight strip also damages. `RollingModel.Hardness` combines compaction, surface dryness and clay. The view shows your rolling record and lists the rollers, never compaction. `Inspect()`, trace and snapshot include compaction, damage and hardness; the snapshot script now rolls, including heavy rolling of a wet strip, which duly builds damage. An untouched strip's hardness ranges from 0.17 when wet to about 0.5 when dry at starting compaction. Cli: `l <strip> <roller> <min> [name]`, `l all`, a Rolled column.
- Pitch characteristics: `PitchModel.Characterise` gives pace, bounce, consistency, carry, seam, spin and cracking from true state. It's internal and reaches outside code only through `Inspect()`; the player will learn pitch character from commentary and verdicts. Direction tests per driver, plus a 5,000-state fuzz keeping every value in 0–10. Cracking is from loam tendency and surface dryness for now; task 5 replaces it with accumulated cracks. Harness `pitch --seasons N` averages an untouched strip each morning by month.
- Tuning: first run gave an untouched strip near-perfect consistency (9.5), because 0.5 looseness sat below the 0.55 starting compaction and long grass had no effect. Raising looseness to 0.65 and adding grass cushioning now gives an untouched strip pace 2.3, bounce 2.8, consistency 8.4, seam 6, spin about 1: slow, low, uneven and green, the research's failure state for poor preparation. A clay-reference literal in the bounce formula was moved into content.
- Wear and recovery: strips carry footholes, rough, surface wear and cracks (0 to 1) and whether the ends are repaired. `WearModel.ApplyOvers` (called by the match engine in task 6) adds wear from an attack profile, resisted by compaction, clay, roots and moisture, and scuffs grass. The new Wear system runs each hour: cracks open as the surface dries past a threshold (faster on clay and with structure damage) and close when it's wetted to field capacity; wear heals at the grass growth rate, faster once repaired. `RepairEnds` is queued and applied in the Wear step. Pitch consistency and spin now use footholes, rough, surface wear and real cracks; the cracking stand-in and its content went. Every tick step now has a real system, so the game's test hook runs extra systems as observers after each hour.
- Scale check: a prepared strip gets footholes of about 0.07 after one day of a four-day match and 0.25 by the end; an unprepared one about 0.46. Untouched strips crack through dry summers (visible on half of July and August mornings, severe on a fifth), the research's cracking failure mode; a strip prepared to under 22% surface moisture stays below the cracking threshold until it dries further during a match.
- Law 9 limits on repairs during a match come with the match engine in task 6; for now repairs are allowed any time. Cli: `e <strip> [name]` and an Ends column.
- Tick order: agreed with the user, `CLAUDE.md` now fixes weather, covers, moisture, grass, tasks, match, wear and recovery.
- Match engine: `MatchModel` resolves an hour of overs from pitch characteristics, attack make-up and strengths (Poisson wickets, normal runs, Match stream). `MatchSystem` plays each fixture in its session hours: toss, innings, all out, overs limits, four-day declarations, chases stopping at the target, results by runs, wickets or an innings, ties, draws and no results. Rain stops play and costs half the next hour; a limited-overs first innings rained under half its overs is abandoned, otherwise the chase gets the overs faced. Through a fixture the match strip is covered by the playing conditions except for dry play (a covers override hook). Each hour of play wears the strip and is logged with its pitch for the referee. The view and Cli show the latest scoreboard.
- Law 9: `CleanFootholes` at any break and `FillFootholes` at close of play in multi-day matches, on the match strip only; full repairs and cover orders on the match strip are refused until the match is over.
- Found and fixed while building: the four-day base wicket rate of 0.03 was wrong by about three times (innings of 800 to 1,280, every match drawn); a rained-out first innings of 0 let the chase win by scoring 1. Tuned to 0.07 over 200 seeds: prepared pitch 28% draws, first innings about 320; untouched pitch 50% draws and lower scoring, the lifeless draw. One-day match tests now prepare the strip, since a neglected pitch scoring less is intended.
- Gate A still passes on the stand-in score (neglect 13%, by the book 50%); 1,000 seasons now take about 8 seconds with matches playing.
- Commentary: `Commentator` raises each of eleven events once a match when its characteristic crosses a content threshold, naming the biggest cause from `PitchModel.Explain` (a breakdown of each driver's share). Collapse causes weigh seam and spin by the bowling attack, after a first version blamed a dry surface for a collapse to seamers on a green day-one strip. Rain gets one line per spell; each break gets a score line (last two innings) at the hour the break starts. Ground ends are in `ground.json`, break names in `formats.json`. No random draws in commentary. The Cli prints new lines after each advance; the snapshot records commentary. Sample from a real run: "Kestrelshire lose 4 wickets in an hour. There's 21 mm of grass left on." on an unmown April strip; "The ball is keeping low. The strip wasn't rolled tight enough." late on day 2.
- Rating: `Referee` rates each finished match from its hourly pitch log (dry hours only), scores and result on the ICC's scale, with reasons naming causes from the commentary or the worst hour's drivers (the hour log now keeps drivers and grass height). The referee abandons play when consistency falls under 2.5, rated unfit. `DemeritLedger` keeps a five-year window and a ban threshold; the view shows active demerits and whether the ground is banned; phase 4 adds consequences. The Cli shows the rating, demerits and reasons under the scoreboard. The stand-in score remains only in the harness.
- First season-long comparison (100 seasons, 18 fixtures each, scratch code): neglect 100% unsatisfactory, nearly all for a slow low pitch, 17.7 demerits a season, banned every season; by the book as it stands (moisture only) the same; by the book plus a crude daily roll and a mow every three days 3% very good, 92% satisfactory, 5% unsatisfactory, 0.9 demerits a season, never banned. Rolling and mowing are what the referee rewards; moisture alone isn't enough. Very good is rare with that crude routine; check whether it's reachable once by the book has the research's schedule (task 10) before touching its thresholds.
- Cli screens: the view now keeps every match played (`GameView.Matches`, oldest first) and an `IntervalView` for each match-day turn until the match ends: "Before play" or the format's break name, with the Law 9 jobs allowed (cleaning at any match-day turn, filling at close of play with more cricket to come; the fill rule is shared with the command check). A match-day advance shows the scoreboard, then that turn's commentary, then the ground and an interval prompt naming the commands ("Stumps. On strip 6 you can clean the footholes (clean 6) or fill them for the night (fill 6)."). The referee's verdict appears once, in a panel with the grade, reasons, scorecard, result and demerits. `v` shows the season record (date, match, strip, result, rating with demerits). The scoreboard leaves off an innings with no overs faced. The strip table already had grass (last cut) and last rolled from task 2 and 3.
- Found while playing it: every all-out innings ended on an hour boundary, often exactly at close of play (four innings of 96 overs in one match), because a block with exactly the wickets in hand counted the whole block. The last of n wickets now falls k / (n + 1) of the way through, the average for evenly spread falls. Replay snapshot regenerated; only the match strip's wear, grass, pitch and the scores changed. Gate A unchanged (neglect 13%, by the book 50%).
- Harness and check 3 (in progress): ratings carry their reason ids; the view carries the season's fixture list (fixtures are now sorted by date in `GameSetup`); the season runner plays on to the end of the last fixture and records each match's result, grade, demerits, reasons and its true pitch averaged over the match's turns (for tuning only). `harness check` reports per policy: grades, satisfactory or better, matches with demerits, demerits per season, seasons reaching the ban threshold, draws, no results, the stand-in score, mean carry and consistency, and faults by reason. `gate` and `check` take `--seed` for the first seed.
- By the book now prepares every fixture in the next ten days, not just the next one (fixtures here are 7 to 13 days apart, so the next build-up starts while a match is on). Added from the research: mow every other day stepping down to 7 mm the day before (0.4 mm a day higher per day out, never more than 30% off at once, judging height from its own cuts plus 0.8 mm a day of growth); roll daily when a feel reading says damp (or a probe reads 18 to 25%), light 45 min from 10 days out, medium 45 min from 7, heavy 40 min from 4, light 20 min for the last two days, never on a watering day; water a surface that feels dry from 5 days out so it can be rolled next day; repair the ends the day after each match. Jobs go to whoever has hours. Random play now also mows, rolls and repairs at random.
- What limited very good: first version of the routine rolled on only about 3 of 10 build-up days (the surface felt wet or dry as often as damp), taking compaction from 0.55 to 0.62, so carry averaged 4.8 against the 5.5 very good needs (very good 3%). Watering a dry surface to roll it and the research's longer mid-build-up rolls took median match-morning compaction to 0.65 and carry to 5.2. No content changed.
- Check 3, 1,000 seasons, seeds 1 to 1000 (seeds 5001 to 6000 in brackets, never used while tuning):
  - Neglect: 0% satisfactory or better (0%), demerits on 97% of matches (97%), 17.7 a season, banned every season. Faults: dead 97%, uneven 22%.
  - Random: 12% (11%), demerits on 86%, 15.5 a season.
  - By the book: very good 19% (19%), satisfactory 75% (75%), unsatisfactory 6%, unfit 0%; 1.1 demerits a season (1.0), never banned. Remaining fault: both sides well under par in one-day and T20 matches, 5%.
  - Placeholder thresholds (by the book 70% satisfactory or better, neglect demerits on 30%): PASSED on both seed sets. Thresholds still to agree with the user.
  - Gate A with the new routine: neglect 13%, random 15%, by the book 56% (43-point lead); the stand-in improved because rolling and mowing don't fight the moisture plan.
- Check 3 thresholds agreed with the user: by the book at least 85% satisfactory or better and at least 10% very good (so very good stays reachable), neglect earning demerits on at least 80% of matches. `harness check` uses them by default. Over 1,000 seasons: by the book 94% and 19%, neglect 97%, on seeds 1 to 1000 and 5001 to 6000. Check 3 PASSED.
- Phase 3 complete.
- Phase 4 planned in `docs/phase-4-plan.md`: position plus lasting wear make strips scarce, assignment is open until prep starts 10 days out, stakes are a season review only, saves and telemetry move to phase 5.
- Strip assignment: fixtures are known by their start date (unique, since fixtures never overlap) and carry a televised flag; `season.json` no longer names strips. `AssignStrip` puts a fixture on a strip at no cost in hours until its build-up starts (`assignLockDaysOut`, 10, in `calendar.json`), when the strip locks. A strip is booked from the start of a build-up to the match's last day, so two fixtures whose build-ups overlap can't share it. At the lock an unassigned fixture gets the strip whose last match ended longest ago (unused first, then lowest number), and the view carries a notice either way. A fixture already inside its lock when the game starts locks at once. A strip in the season file becomes the starting assignment; a clash between two is a content error (the test helper now spreads its match days over strips 1, 2, 3 and so on). Matches, the interval view, Law 9 checks and covers all read the assigned strip.
- Cli: `x` lists the fixtures (strip, televised, lock date), `p <#> <strip>` assigns, notices print after each advance. Harness: by the book follows a strip plan, `SeasonPlans.Original` (the strips the season file used to name), so its results are unchanged (check 3: 94% satisfactory or better, 19% very good; Gate A 56%); neglect takes the defaults; random also assigns at random.
- Televised fixtures (placeholder, 8 of 18): 13 May, 6 Jun, 13 Jun, 4 Jul, 2 Aug, 14 Aug, 1 Sep, 14 Sep. Used from task 2.
- Replay snapshot regenerated: its second match moved from strip 1 to strip 2 (the two build-ups overlap); only strips 1 and 2 and the match lines changed from 17 April.
- Square positions: `ground.json` lists `centreStrips` (5 to 8); `GroundSettings` answers whether a strip is a centre strip and how far it is from the centre. The view marks centre strips and the Cli table shows them as `5c` with a key. The board uses them from task 4.
- Neighbour wear: each hour of play, run-ups wear the ends of the strips either side of the match strip by a share (0.25) of the footholes dug on it, doubled (the wet-play factor) if the neighbour is wet. Strips two away are untouched. `ApplyOvers` now returns the footholes it dug. Added content tests for `wear.json`, which had none. Replay snapshot regenerated: only the neighbour strip's wear and consistency changed.
- Check 3 unchanged (by the book 94% satisfactory or better, 19% very good): neighbour wear of 0.02 to 0.05 heals within days at today's recovery rate, so it only starts to bite once task 3 slows recovery and adds lasting wear.
- Lasting wear and the ends: strips carry lasting wear (0 to 1, a share of the footholes, rough and surface wear each match digs, never healing in season) and ends establishment (0 bare to 1 established). Footholes dug wear the ends back; the ends regrow with grass growth over 18 growing days once repaired, 45 if left. Lasting wear and bare ends both lower bounce consistency and make a strip wear faster; run-ups do the same to the neighbours. A match's own lasting wear and ends loss are held back until it's over and then settled, so they count against the next use rather than doubling up on the footholes in this one (the first version applied them hour by hour and dropped even a fresh strip's match consistency from 8.3 to 6.0). Commentary and the referee can name two new causes, "lastingWear" and "thinEnds".
- The player sees: a feel reading also looks at the ends (bare, seeded, thin, established), when each strip was last played, and when its ends were last repaired. The strip table was compacted to fit 80 columns: ages as "3d", cover folded into a Status column, one space of padding.
- Harness `reuse`: a four-day match from 1 June, then the same strip reused after a gap against a fresh strip, by the book, 200 seasons (true consistency, carry, satisfactory or better, very good; fresh in brackets):
  - after 11 days: 6.8 (8.3), carry 5.3 (4.9), 91% (99%), very good 0% (0%)
  - after 21 days: 7.2 (8.3), 94% (99%), 1% (0%)
  - after 28 days: 7.5 (8.4), 94% (100%), 7% (1%)
  - after 42 days: 8.1 (8.3), 99% (99%), 14% (1%)
  - third use, 21 days apart: 6.7 (8.4), 86% (98%)
- Finding to watch for Gate B: a strip reused after 4 to 6 weeks keeps its compaction and carries better than a fresh one (a ten-day build-up can't lift a fresh strip from 0.55 enough for very good's 5.5 carry), so a well-spaced second use beats a fresh strip, while quick reuse and third uses cost consistency.
- Check 3 still passes: by the book 91% satisfactory or better (was 94%), 12% very good (was 19%), banned in 4% of seasons, since its fixed plan puts matches back to back on neighbouring strips. Neglect 97% with demerits.
- Stakeholders: `stakeholders.json` sets satisfaction (0 to 100, everyone starts at 50), when requests come, how pitches are judged to deliver them, and how much each outcome moves the captain, the board and the referee. Requests for a fixture arrive 5 days before its strip locks (so 15 days before the match), drawn from the Events stream in a fixed order: the captain asks for a pitch character on 60% of fixtures (four-day: green 2, turning 1, pace 1; one-day: pace or flat; T20: flat 2, pace 1), the board asks 30% of four-day matches to last into day four. `AnswerRequest` accepts or declines at no cost in hours until the lock; unanswered at the lock means ignored.
- Judged once a match is over and its wear settled: green on day-one seam (5), turning on the last day of play's spin (4), pace on carry (5.5) with consistency (8), flat on movement under 3 with consistency 8, lasting on any cricket scheduled on day four (rain counts). No dry play spoils a pitch request (no change). Captain: delivered +12, not delivered −15, declined −4, ignored −6, home win +3, loss −3. Board: delivered +8, not delivered −12, declined −3, ignored −5; four-day into day four +4, over inside three days −6; limited-overs no result −4; televised from a centre strip +3, from an outer one −6; −10 a demerit. Referee: very good +6, satisfactory +2, unsatisfactory −8, unfit −20. Every change is logged with its reason, fixture and date for the review.
- The view carries the requests, each stakeholder's satisfaction and changes, and a notice for each request and change. Cli: requests and changes print as messages, `yes <#>` and `no <#>` answer, the status shows satisfaction and requests awaiting an answer. The replay snapshot now records satisfaction and requests (lines added only). Gate A and check 3 are unaffected: requests only use the Events stream.
- Season review: once every fixture has been played and settled with the stakeholders, the view carries a `SeasonReview`: each stakeholder's satisfaction, mood (delighted from 75, content from 55, uneasy from 40, otherwise unhappy) and three biggest reasons summed over the season; pitch ratings by grade; demerits and the ban; the county's results; and every strip's matches and wear from an end-of-season walk (fresh, lightly worn from 0.01 lasting wear, worn from 0.03, heavily worn from 0.06).
- Decided while building: the walk reveals lasting wear, which is truth, so it comes only in the review, after the last decision it could inform. Refusing to advance past the review lives in the Cli rather than the core, since tests and the harness advance beyond the last match; the core keeps ticking harmlessly.
- Cli: the review shows once in a panel; after it, Enter is refused with a prompt, `s`, `v` and `x` still work, and `new` starts another season with a new seed (`GameLoop` takes a next-season factory; Program supplies one).
- A neglected season (seed 3) ends with all three at 0: unanswered requests, 17 demerits, 17 unsatisfactory pitches, banned; satisfaction clamps at 0, so later changes log smaller than their raw size.
- Cli: most of task 6 landed with tasks 1 to 5 (assignment, lock and request messages, `yes`/`no`, played and ends in the strip table, the review). Added now: the fixtures screen shows each fixture's rest (days since its strip's previous match, played or planned), its requests in brief (green ✓, 4 days ?, flat ignored) and its rating once played; assigning a strip says how long it will have rested and after which match; the help is grouped into readings, strip work, match days, planning and game. Checked at 80 columns.
- Harness policies: by the book gained square upkeep (every strip not in a build-up or a match mown weekly towards 15 mm, its height estimate capped at 40 mm), after planned play's second uses ran into 50 mm of unmown grass that the mower couldn't take (its limit is 50 mm, the policy's estimate was 87). Its strip choice and request answers became hooks. Greedy: the day before each lock, feels every strip and takes the best-looking now (established ends, then centre, then longest rested). Planned: a whole-season rotation on the first turn, by cost: televised off a centre strip 100, untelevised on one 30, 10 a previous use, 1 a day of rest short of 35, 15 a neighbour match within 14 days. Both answer requests alike (yes to flat and lasting four days, no to green, turning and pace) so Gate B compares rotation alone. The season runner records each fixture's satisfaction points across all three stakeholders.
- First Gate B runs showed strips getting better with every use (very good 2% on a first use, 31% second, 53% third, 58% fourth): each build-up's compaction carried over and lasting wear's consistency cost was too small to matter, so greedy's centre-strip reuse won late. Fixed by a model change and tuning: lasting wear now also deadens a strip (`pitch.lastingDeadening` 6: pace and bounce × (1 − 6 × lasting wear), a tired surface plays slower and lower, named as a dead-pitch cause); lasting wear's consistency weight rose from 1 to 3; starting compaction rose from 0.55 to 0.62 (proper pre-season rolling, research section 5), so a fresh strip can be very good (14% in June against 1% before). Swept starting compaction 0.55/0.62, deadening 3/6/8 and weight 1/2/3 over 200 seasons; 0.62/6/3 was the setting where Gate B's shape appeared and planned play still clears check 3.
- Reuse now (harness `reuse`, 200 seasons, fresh in brackets): after 11 days 60% satisfactory or better (100%), 21 days 83%, 42 days 92% (100%) but never very good (14%); a third use 21 days apart 8% (99%).
- Decided with the user: check 3 now measures planned play (by-the-book preparation with the planned rotation), since rotating well is part of playing by the book from phase 4 and the season file's old fixed plan reuses three strips three times (77% satisfactory or better now). Gate B counts greedy as level early if it trails by 3 points or fewer.
- Gate B over 1,000 seasons, satisfaction points a season by month played (seeds 5001 to 6000 in brackets):
  - April and May: greedy 0.6 (−0.0), planned 2.9 (1.6).
  - August and September: planned leads by 15.7 (16.1), needs 10. PASSED on both.
  - Season: planned 18.4, greedy −22.1, by the book on the old plan −83.1, neglect −143.5. Greedy falls from 97% satisfactory or better in May to 74% in September as its favourite strips tire; planned holds 85% to 97%.
- Check 3 over 1,000 seasons: planned 91% satisfactory or better (91%), 19% very good (19%), banned in 1% of seasons; neglect demerits on 97% (97%). PASSED on both. Gate A unchanged: by the book 56%, neglect 14%.
- Worth watching in playtests: greedy dips in June (−16) when early televised matches find the centre strips' ends not yet grown back and go to outer strips; a third use is now close to certain unsatisfactory, which may be too harsh.
- Phase 4 complete.
- Phase 5 planned in `docs/phase-5-plan.md` with the user: saves are seed plus command log (replayed on load, refused if content changed), a fast-forward to the next event, testers send back one folder, testers are a mix of sim players and cricket people. A season is 353 turns today.
- Saves: a `RecordingGame` wraps the game in the Cli, records each accepted command with its turn (rejected ones change nothing, so replay doesn't need them) and writes the save after every command and turn (written beside the target then moved into place). A save holds a schema version, a SHA-256 over every content file, the seed and start, the turns played and the commands; `SaveFile.Restore` refuses another version or other content, rebuilds the game from the seed and replays each turn's commands, and reports the turn and command if one is refused on replay. Tested: every command type round-trips (checked against all `IGameCommand` types by reflection), and a restored game equals the original in view and truth after 70 turns of random commands of every kind on 12 seeds, and stays equal after another turn.
- Seasons live in Documents/Cricket Groundsman/season-<date>-<seed>/save.json (`--data` overrides it). On launch the Cli offers to resume the latest unfinished season; `save` writes a named copy; a resumed season doesn't repeat old commentary, verdicts or the review. The harness now references the Cli project instead of linking `ContentLoader.cs`, so saves are there for task 7.
- Telemetry: `telemetry.jsonl` beside each season's save, one JSON object a line, each with its event, turn, game time and wall-clock time (UTC, from an injected clock in the Cli; the core still never reads the clock). Events, by pass-test row:
  - planning ahead: `command` lines for `assign`, with `fixturesAhead` (0 is the next fixture not yet over);
  - readings drive decisions: every `command` line (type, strip, who, tool, heights, roller, minutes), accepted or not, with the refusal's reason;
  - pressures force choices: `answer` commands with stakeholder, kind and accept;
  - pace: `advance` lines with seconds spent on the turn, commands given, hours run and the time it started from;
  - readable verdicts: `screen` lines for status, fixtures, record, help, verdict and review;
  - context: `session_start` (resumed or not), `session_end`, `invalid` input (first 40 characters), and a `review` summary when the season ends.
- On first launch the Cli says what's recorded and that nothing is sent, and writes a README.txt in the playtest folder saying the same and what to send back. A restored season keeps appending to its log; replaying a save logs nothing.
- Fast-forward: `ff` advances until `FastForward.StopReason` (a pure function of the view) finds something that needs the player: news (any notice: a request, a lock, a satisfaction change, a verdict), a match starting, close of play when footholes can be filled, a fixture with no strip whose build-up starts today or tomorrow, likely rain (60% today or tomorrow) on an uncovered strip in its build-up (once a day, so choosing not to cover doesn't stop every turn), or the end of the season; 60 turns at most. Commentary from skipped sessions still prints. The log marks fast-forwarded advances (`ff`) and summarises each fast-forward (turns, why it stopped). Tested over a season that it never skips a turn that needs the player.
- First version stopped at every match session (132 of 248 stops in a season of only `ff`); narrowed to the start of a match and close of play. A season of only `ff` and no other commands now takes 173 presses instead of 353 turns; about 80 of those stops are a neglectful player's own doing (no strips assigned, nothing covered).
- Onboarding: a one-screen introduction in a panel on first launch (the job, the three judges, readings are ranges, the four commands to start with), and again with `intro`. Six first-time tips, each shown once ever (remembered in tips.json in the playtest folder, so later seasons start clean): the season start (choosing strips, spreading wear, centre strips), the first request, the first lock (the build-up routine), the first rain stop, the first match, the first verdict (demerits, repairs and rest). `tips off` turns them off for good. The help gained `intro`, `tips off` and a line of examples. `docs/playtest/guide.md` is the testers' quick-start, with a cricket glossary for sim players and an interface section for cricket people; every claim in it was checked against the game as built.
- Next: phase 5 task 5, tester builds.
