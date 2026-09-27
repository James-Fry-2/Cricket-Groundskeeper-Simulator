# Phase 3 plan: matches and verdicts

Sep 27, 2026 · @James Fry

## Context
Phase 2 made the ground live and proved decisions matter (Gate A), but scored strips with a stand-in: moisture on the first morning. Phase 3 plays the matches. A strip's true state becomes pitch behaviour, the match runs session by session with commentary tied to that behaviour, and the match referee rates the pitch on the ICC's scale. The stand-in score retires.

Grounded in [the MVP plan](mvp-plan.md) ("Match and rating model"), [the design](design.md) ("Pitch behaviour and scoring", "Footholes and rough", Law 9) and [the research notes](research.md) (sections 3, 4 and 7).

Decisions made for this phase:
- **Rating scale:** the ICC's current one, from the research: very good, satisfactory, unsatisfactory, unfit. Unsatisfactory costs 1 demerit and unfit 3, on a rolling five-year window.
- **Preparation levers:** rolling and mowing come in now, so pace, bounce and seam come from the levers the research lists (section 4), not from moisture alone.
- **Match view:** session by session. Each session is a turn with commentary and a score, and each interval allows the jobs Law 9 permits.

Out of this phase: stakeholder requests and reactions (phase 4), the player choosing strips (phase 4), minigames, named individual players, ball-by-ball play, pitch maps, renovation, budget.

## What pitch behaviour comes from
The design's table, with the state each driver needs:

| Characteristic | Drivers | State it needs |
| --- | --- | --- |
| Pace | Hardness, compaction, dryness | Compaction (new), surface moisture |
| Bounce height | Clay, compaction, moisture at depth | Loam clay, compaction, subsurface moisture |
| Bounce consistency | Evenness of moisture and compaction, cracks, footholes | Structure damage (new), cracks (new), wear (new) |
| Carry | Pace and bounce together | Derived |
| Seam movement | Grass cover and height, surface moisture | Grass (new), surface moisture |
| Spin and grip | Dry surface, crumbling, cracks, rough | Surface moisture, wear, cracks |
| Cracking | Clay drying fast; dangerous where plates move | Loam cracking tendency, drying history |

## Tasks
Rough hours, 60 to 80 in total, in dependency order. Each task keeps the usual rules: tests first, numbers in content, replay snapshot updated only for intended changes.

### 1. Formats, teams and fixture details (4 to 6 h)
- `content/formats.json`: four-day, one-day (50 overs) and T20. For each: days, innings, overs limits, session play hours and match-day decision hours. T20 plays in the evening, which settles the phase 1 caveat that every format shared one set of match-day hours.
- `content/teams.json`: the home county and fictional opponents, each with batting and bowling strength and an attack profile (share of seam and spin, share of left-arm bowlers, and whether the seamers are heavy-footed).
- Fixtures in `season.json` gain a format and an opponent. The view and Cli name them.

### 2. Grass (6 to 8 h)
- Strip state: grass cover (%), height (mm), root depth (mm).
- A Grass system in the Grass step:
  - growth driven by month, temperature and moisture;
  - cover recovering towards its ceiling;
  - roots deepening in good conditions and shrinking in drought.
- `m <strip> <height>` mows to a height (costs hours). Cutting more than about a third of the height at once scalps the strip and costs cover.
- Research targets (section 3): final height 6–8mm with some soil visible. Cut 2–3 times a week in preparation, lowering 2–4mm a week.
- `content/grass.json` holds growth rates, the scalping rule, and starting grass per strip.

### 3. Compaction and rolling (8 to 10 h)
- Strip state:
  - **Compaction:** 0 to 1, built up from pre-season rolling in content.
  - **Structure damage:** hidden. It comes from rolling too wet and overusing the heavy roller.
- **Hardness** is derived from compaction, surface dryness and clay.
- `l <strip> <roller> <minutes>` rolls with the light, medium or heavy roller.
  - Compaction gain depends on roller weight and time, with diminishing returns near maximum.
  - Rolling only works inside a moisture window per loam: too wet adds structure damage, too dry adds nothing.
  - These are the research's rules (section 3): moist but never wet to the touch, heavy roller last and not overused.
- `content/rollers.json` holds the rollers. Loams gain their rolling window.
- Soil cores become the way to judge the window, alongside feel readings.

### 4. Pitch characteristics (6 to 8 h)
- A pure `PitchModel` maps true strip state to the seven characteristics on a 0 to 10 scale, using coefficients in `content/pitch.json`.
- It runs from the truth, so only the match engine and the harness call it. The player learns pitch character from commentary and the verdict, never as numbers.
- Tests are about direction, one per driver. For example, a harder and drier strip plays faster; more grass on a wet surface seams more; cracks and footholes lower consistency.
- A harness `pitch` trace records characteristics through a season, for tuning.

### 5. Wear and recovery (6 to 8 h)
- Wear state per strip:
  - **Footholes:** at the bowlers' ends. Seam overs dig them, heavier with heavy-footed seamers.
  - **Rough:** outside the batters' off stump. Follow-throughs make it, with the side set by the share of left-arm bowlers.
  - **Surface wear:** batters' marks and running.
- **Resistance** comes from compaction, clay binding, roots and moisture: too wet sinks, too dry dusts. Play in the wet multiplies damage.
- **Cracks** open as clay loams dry fast, scaled by the loam's cracking tendency.
- **Recovery:** after a match, `e <strip>` repairs the ends (hours, and seed and loam in later budgets). Wear then recovers over days. A strip reused before it recovers plays less consistently. This is the recovery clock phase 4's allocation puzzle needs.
- The Wear and recovery step handles all of this each hour.

### 6. Match engine (10 to 14 h)
- **Tick order:** a Match step joins the fixed order before Wear and recovery, giving weather, covers, moisture, grass, tasks, match, wear and recovery. This changes `CLAUDE.md`'s rule and needs agreeing when the task starts.
- **Play:** overs run in blocks inside each session, 8 overs by default from content. That lets footholes and drying evolve within a session. It's the MVP plan's fallback, adopted from the start.
- **Each block:**
  1. Read the pitch characteristics.
  2. Resolve runs and wickets from them, against the bowling attack's profile and the two teams' strengths (Match stream).
  3. Apply wear.
- **Rain stops play.** The ground staff cover the pitch, as county playing conditions require, and overs are lost to the rain plus a drying delay.
- **Innings and results:**
  - Innings end on 10 wickets or the overs limit.
  - Four-day declarations follow a simple rule from content.
  - The result is a win, draw or tie.
- **Interval jobs** (Law 9):
  - Covers on or off, in every format.
  - Clean and dry the footholes, in every format.
  - Fill the footholes at close of play, in multi-day matches only.
- The between-innings roll is the batting captain's choice. It's automatic for now and becomes a minigame later.

### 7. Commentary (4 to 6 h)
- `content/commentary.json` holds templates keyed by event, filled with team names and ends. They can be rewritten without code changes.
- An event fires when a characteristic crosses a threshold, for example:
  - uneven bounce from one end;
  - the ball keeping low;
  - seam movement early on;
  - dust or cracks appearing;
  - footholes growing;
  - a rain delay;
  - the referee inspecting.
- Every event carries its named cause, such as "footholes at the Pavilion End, dug by heavy seamers on a damp surface". This addresses the MVP plan's risk that verdicts feel random. The cause shows in debug mode and feeds the verdict.

### 8. Rating and demerits (5 to 7 h)
- At the end of a match the referee rates the pitch very good, satisfactory, unsatisfactory or unfit.
- **What counts, in order of weight:** bounce consistency and danger first (the ICC's main concern), then carry, then the balance between bat and ball over the match.
- **Named failure modes**, from the research's case studies:
  - Too much in the bowlers' favour: a four-day match over in two days, like the MCG in 2025. Unsatisfactory.
  - Lifeless: no carry, no seam, no turn, a dull draw, like the MCG's "poor" in 2017. Unsatisfactory.
  - Dangerous: unfit, and the match is abandoned.
- The verdict lists its reasons with their causes, from task 7.
- A demerit ledger keeps a rolling five-year window. Phase 4 adds the consequences.
- The stand-in moisture score retires from the game. The harness keeps it as a secondary metric, for comparison.

### 9. Cli (5 to 7 h)
- Commands: `l` to roll and `m` to mow, plus `e` for end repairs, `clean` for cleaning footholes, and filling footholes at close of play.
- **Match-day screens:** the scoreboard, then the session's commentary, then the interval prompt.
- **Verdict screen:** the rating, reasons, a brief scorecard and demerits.
- The table gains grass height (from mowing records and feel) and a "last rolled" column. There's no true hardness for the player.

### 10. Harness and the phase 3 check (6 to 8 h)
- By the book adds the research's rolling and mowing schedule:
  - Roll 20–30 minutes a day, working up from light to heavy, and only inside the moisture window (judged from cores and feel).
  - Mow to 6–8mm over the build-up.
  - Repair ends after each match.
- The harness reports ratings, demerits and results per policy. The stand-in score is kept alongside for comparison.
- **Check 3, proposed as the gate before phase 4:** over 1,000 seasons, by the book rates satisfactory or better on at least 70% of matches, and neglect earns demerits on at least 30% (placeholders to agree on first results). The MVP plan's own invariant, "a neglected strip rates worse than a prepared one", becomes a unit test.

## Dependencies
1 → 6. Tasks 2 and 3 → 4 → 5 → 6 → 7 → 8. Task 9 follows 6 to 8. Task 10 needs everything.

## New content files
`formats.json`, `teams.json`, `grass.json`, `rollers.json`, `pitch.json`, `wear.json`, `commentary.json`. `loams.json` gains the rolling window, and `season.json` gains fixture formats and opponents. Every number is a placeholder and goes on the tuning list.

## Risks
| Risk | Fallback |
| --- | --- |
| Session-level play hides foothole effects | Blocks of overs inside sessions from the start (task 6) |
| Verdicts feel random | Every commentary event and verdict reason carries a named cause; the debug view shows the chain |
| Too many hidden stats to tune at once | Harness traces per task (pitch, wear), tuned one system at a time before the match engine uses them |
| Rolling and mowing make daily turns long | Group commands (`l all`, `m all`), and let staff be given standing jobs in phase 4 if playtests show it |

## Verification
- Each task: `dotnet build -warnaserror` and `dotnet test` pass; the replay snapshot changes only when a rule change is intended.
- Tasks 2 to 5: harness traces checked by eye against the research (growth and cut heights, compaction only in the window, cracking in hot dry spells, footholes on day three of four).
- Task 8: the unit test that a neglected strip rates worse than a prepared one.
- Task 10: check 3 recorded in `progress.md` with seeds, policies and thresholds.
