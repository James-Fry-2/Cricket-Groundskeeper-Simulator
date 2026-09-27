# Phase 2 plan: weather, moisture and readings

Sep 27, 2026 · @James Fry

## Context
Phase 1 gave a playable loop, but nothing changes the ground except the player's watering. Phase 2 makes the ground live: weather, a two-layer moisture model, covers, a forecast, and readings that age and can mislead. It ends at Gate A: in the harness, a by-the-book policy must beat neglect on a stand-in score, which proves decisions matter before match ratings arrive in phase 3.

Grounded in [the MVP plan](mvp-plan.md) and [the research notes](research.md), sections 3, 6 and 8.

## What the research changes
- **Target is moist at depth, dry at the surface** (research section 3). Around 25–30% moisture in the top 100mm is the working figure. This maps onto the two layers: surface (top ~25mm) and subsurface (to ~100mm).
- **Gate A score refined:** a match strip "lands" when, on the match morning, subsurface moisture is inside a target band **and** surface moisture is below a ceiling. This is still option 1 (match-morning moisture), made to match the research. Placeholders: subsurface 24–30%, surface under 22%.
- **Water reaches depth slowly**, a day or more, so surface-to-subsurface drainage is slow.
- **A flat sheet after watering slows evaporation**, so covers cut evaporation as well as keeping rain off.
- **Prep starts 10–14 days out, never fewer than 5**: water deeply 4–5 days out, then taper. This is the by-the-book policy.
- **Labour hours are the core resource** (section 8), which is why staff hours are in this phase.

## Cross-cutting decisions
- **Truth inspector:** `Game.Inspect()` returns a public `TruthSnapshot` (weather and strip truth), separate from `GameView`. It's used by harness scoring, replay snapshots and a Cli `--debug` flag that shows truth beside readings, as the MVP plan asks. `GameView` still never contains truth.
- **Replay snapshots:** from task 2, a fixed seed plus a scripted command list runs a season, and `TruthSnapshot` is compared against a stored JSON file in `tests/Snapshots/`. An environment variable regenerates it when a rule change is intended.
- **Harness trace mode:** writes daily truth per strip to CSV, for tuning weather and moisture by eye before any policy exists.
- Every new number goes in content and in the tuning list in `progress.md`.

## Tasks
Rough hours, following the MVP plan's 50–70 hour estimate for the phase.

### 1. Distributions (2–3 h)
- Add `NextGaussian` (Box-Muller, one value per call so no hidden state beyond the generator) and `NextExponential` to `RandomSource`.
- Tests: pinned output, and sample mean and spread over many draws.

### 2. Weather (8–12 h)
- `content/climate.json`: monthly normals for the fictional ground (English Midlands-like placeholders). Mean temperature, daily range, rain days, rain total, sunshine hours, mean wind, and one persistence value for wet-day spells.
- `WeatherSystem` (Weather step) generates a day at a time, a few days ahead so the forecast has truth to forecast:
  - Wet or dry day from a two-state Markov chain, which gives spells while keeping the monthly rain-day rate.
  - Wet-day totals are exponential with the monthly mean, falling in one spell with a random start and length.
  - Temperature is the monthly mean plus a day-to-day anomaly that carries over from one day to the next, with a daily curve (coolest before dawn, warmest mid-afternoon).
  - Wind is a daily value around the monthly mean. Sunshine is less on wet days.
- View: observed weather, meaning the rain gauge for the last 24 hours and the temperature now. These are observations, not hidden state.
- Tests:
  - Over 200 seeded years, monthly rain totals and rain days sit within tolerance of the normals.
  - Spells exist: wet days follow wet days more often than the average rate.
  - The first replay snapshot.
  - Harness trace output.

### 3. Loams and moisture (10–14 h)
- `content/loams.json` holds each loam's:
  - clay %;
  - saturation and field capacity per layer;
  - drainage rates, lower for more clay;
  - cracking tendency, stored for phase 3.
- `ground.json` strips name a loam, and saturation moves out of `ground.json`.
- Layer depths go in content (surface 25mm, subsurface 75mm). Rain and watering in mm convert to % through layer depth. `tasks.json` watering becomes mm (placeholder 5mm).
- `MoistureSystem` (Moisture step), hourly per strip:
  - Surface: plus rain (unless covered) and watering, minus evaporation, minus drainage. Evaporation comes from temperature, wind and sun, and falls as the surface dries.
  - Drainage to the subsurface only happens above field capacity. The subsurface drains slowly below that.
  - Anything above saturation runs off.
  - Watering moves from the Tasks step into Moisture as an input, so the tick order still holds.
- Tests are the MVP plan's invariants:
  - moisture stays between zero and saturation;
  - no rain means drying;
  - more clay drains more slowly;
  - watering reaches the subsurface over roughly a day.
- Tune against harness traces.

### 4. Covers (4–6 h)
- `c <strip>` and `u <strip>` commands, queued and applied in the Covers step.
- The number of covers is limited by content (placeholder 4). Covered strips get no rain and evaporate at a reduced rate (content factor).
- The view shows which strips are covered.
- Tests: a covered strip takes no rain; the cover limit is enforced; covered strips dry more slowly.

### 5. Staff hours (5–8 h)
- `content/staff.json` lists you plus two groundstaff (fictional names), each with hours per day and a reading-skill multiplier.
- Job costs go in content: watering, covering and uncovering, a probe reading, a feel reading. Hours reset each morning.
- Commands take an optional staff member and default to you. In the Cli that's `r 3 sam`.
- A command is rejected when the person's hours are used up. The view shows hours left.
- Tests: costs are deducted, over-budget work is rejected, hours reset at the morning decision point.

### 6. Forecast (5–7 h)
- Issued each morning for the next 7 days: a rain range (mm) and a maximum temperature range per day.
- The forecast is truth plus an error drawn once when it's issued (Forecast stream), with spread growing with lead time. Truth is usually inside the range but not always, like readings. Rain ranges are clamped at 0.
- The view and Cli show a forecast panel.
- Tests:
  - It stays stable between turns in the same day.
  - It's wider further ahead.
  - Hit rate over many seeds matches the content miss rate within tolerance.

### 7. Readings: ageing, misses, skill, feel (8–10 h)
- **Ageing:** the range shown widens from the stored reading by a per-day rate plus a per-mm rate for rain the strip has taken since (known from the gauge and cover history, so no truth leaks).
- **Misses:** each source has a miss rate. A miss shifts the range so the truth sits just outside it. The taker's skill scales both width and miss rate.
- **Feel readings** (`f <strip>`): cheap, and shown as dry, damp or wet from content bands, judged on a noisy value so a wrong word is possible. This is the second information level from the slice.
- The phase 1 intersection issue fades now that moisture moves every hour and readings cost hours.
- Tests:
  - Width grows with age and rain.
  - Miss frequency over many seeds matches content.
  - Skilled staff read tighter.
  - Feel words follow the bands.

### 8. Harness, policies and Gate A (8–10 h)
- `season.json` matches gain an assigned strip, since strip assignment is a phase 4 decision.
- `content/scoring.json` holds the stand-in Gate A band.
- The harness runs a policy over N seeded seasons, playing through `IGame` and the view only, like a player. It scores match mornings from `Inspect()` and writes a CSV per match plus a summary.
- Policies:
  - **Neglect:** does nothing.
  - **Random:** random legal commands, as a baseline.
  - **By the book:** follows the research loop, using only readings and the forecast. From 10 days out it reads the strip, and tops up water 5 to 4 days out towards the depth target. It then tapers, and in the final days covers when the forecast shows rain and uncovers on dry days.
- **Gate A:** by the book lands clearly more often than neglect over 1,000 seasons. "Clearly" means at least 25 percentage points better, a placeholder to agree when we see results.

## Dependencies
1 → 2 → 3 → 4. Task 5 needs 3. Task 6 needs 2. Task 7 needs 3 and 5. Task 8 needs everything.

## Out of scope
Grass growth, mowing, rolling and compaction (phase 3 needs these for pace and bounce), match simulation, saves, telemetry, strip assignment by the player.

## Verification
- Each task: `dotnet build -warnaserror` and `dotnet test` pass, and the replay snapshot changes only when a rule change is intended.
- Tasks 2 and 3: harness trace CSV checked by eye (monthly rain, drying after rain, watering reaching depth), with the figures noted in the session log.
- Task 8: Gate A result recorded in `progress.md` with seeds, policy results and the band used.
