# Phase 4 plan: pressures and a playable season

Sep 30, 2026 · @James Fry

## Context
Phase 3 made matches play out and the referee rate them, and check 3 showed that preparation matters. But the player still doesn't choose strips (the season file fixes them), and nothing makes one strip better than another: every strip uses the same loam, and a used strip's repaired ends heal in 3 to 5 days. Phase 4 adds the MVP's central puzzle: rotating strips across a season while the captain and the board pull different ways. It ends at Gate B: in the harness, greedy play has to fall behind planned play late in the season.

Grounded in [the MVP plan](mvp-plan.md) (the stakeholders and fixtures systems, the greedy policy, Gate B), [the design](design.md) ("Outside pressures", "Allocation rules that create decisions", "Preparation and recovery", "Scoring layers") and [the research notes](research.md) (sections 5 and 7).

Decisions made for this phase:
- **What makes strips differ:** position on the square plus lasting wear. Centre strips are wanted for televised matches. Every use leaves wear that only autumn renovation clears, and repaired ends take weeks to establish. Varied soils per strip wait until after the MVP.
- **Strip assignment:** the player assigns and changes strips freely until the build-up starts 10 days out, when the strip locks. An unassigned fixture gets the head groundsman's default, with a warning.
- **Stakes:** a season review only. Unhappy stakeholders and a ban are scored at the end of the season, with no game over and no fixtures lost in season.
- **Saves and telemetry** move to phase 5, with playtest preparation.

Out of this phase: saves, telemetry, budget, renovation, varied soils, minor fixtures (Second XI, club hires; a fallback if strips aren't scarce enough), the broadcaster, governing body and members as separate stakeholders, minigames.

## The puzzle this phase has to create
- 18 first-team fixtures on 12 strips. The four centre strips are the only good ones for televised matches, and those come throughout the season, including late.
- A strip used again before its repaired ends have established plays less consistently and wears faster. Every use adds lasting wear, so a strip's third match of the season is worse than its first.
- Using a strip wears its neighbours' ends through run-ups, so clustering matches costs the strips around them.
- Greedy play (the best strip for every match now) burns the centre strips early and has none fit for the televised matches in August and September. Planned play spreads them out.

## Tasks
Rough hours, 60 to 80 in total, in dependency order. Each task keeps the usual rules: tests first, numbers in content, and the replay snapshot updated only for intended changes.

### 1. Fixture ids and strip assignment (8 to 10 h)
- Fixtures in `season.json` gain an id and a `televised` flag, and lose their strip.
- **An `AssignStrip` command** (fixture, strip) is accepted until the lock, `assignLockDaysOut` in `calendar.json` (10 by default). It's refused for a strip already assigned to a fixture whose match would overlap.
- **At the lock,** an unassigned fixture gets the head groundsman's default: the longest-rested strip that isn't in use. The view reports the lock with a message.
- **The view** shows each fixture with its assigned strip (or none), its lock date, and whether it's locked.
- `GameSetup` can take starting assignments, so tests and harness policies can keep fixed strips. By the book keeps today's strips as its plan, so Gate A and check 3 stay comparable.

### 2. Square positions and neighbour wear (5 to 7 h)
- **Positions:** each strip in `ground.json` gains its distance from the centre. Strips 5 to 8 are the centre.
- **Neighbour wear:** a match wears its neighbours' ends through run-ups, as a content share of the match strip's foothole wear, scaled up in wet play.
- **The board wants televised matches on a centre strip,** scored in task 4.
- Tests: neighbours take wear and strips two away don't; wet play adds more.

### 3. Lasting wear and establishment (8 to 10 h)
- **Lasting wear** (per strip, 0 to 1): added by each match in proportion to its footholes, rough and surface wear. It never heals in season, since only renovation clears it. It lowers bounce consistency and makes the strip wear faster.
- **Establishment:** repaired ends go through germination and establishment, driven by grass growth, with days to each stage in content (placeholders about 7 and 21 days in summer). Until the ends are established, they resist wear less and play less consistently.
- **Tuning target:** a strip reused within about three weeks of a four-day match plays noticeably worse, and a third use in a season is worse than the first. A harness trace checks this by eye.
- **What the player can see:** a strip's history (last played, format, days since, last repair) and a look at the ends as part of a feel reading (bare, seeded, thin or established). Truth stays out of the view.

### 4. Stakeholders and requests (10 to 14 h)
- `content/stakeholders.json` holds request rules and satisfaction weights for the captain, the board and the referee. Satisfaction runs 0 to 100 per stakeholder. Every change is logged with a reason, so the review can say why.
- **Captain:**
  - Asks for a pitch character for some fixtures (green, turning, true and fast, or flat), a few days before the lock so it can shape strip and preparation choices.
  - The player accepts or declines. A declined request costs a little satisfaction. An accepted one that's delivered gains a lot, and an accepted one that isn't loses more.
  - Delivery is judged from the match's pitch log against thresholds in content, and the commentary names the relevant characteristic, so the player can see why.
  - Home results also move the captain.
- **Board:**
  - Wants four-day matches to reach day four, for gate and hospitality income.
  - Wants limited-overs matches finished rather than washed out, televised matches on a centre strip, and no demerits.
  - Sometimes makes requests too, such as a pitch that lasts for members' day.
- **Referee:** satisfaction follows the ratings.
- **The conflicts should be real:** a green seamer can end a four-day match on day two, costing the board's income and risking the referee's "too much for the bowlers".
- Commands: `AcceptRequest` and `DeclineRequest`, with a deadline.

### 5. Season review (4 to 6 h)
- After the last fixture the season ends with a review:
  - each stakeholder's satisfaction and the main reasons for it;
  - the ratings record and demerits (a ban shows here);
  - home results;
  - square health, as lasting wear per strip, described in words.
- Advancing past the review is refused. The Cli offers a new season with a new seed.

### 6. Cli (6 to 8 h)
- **Fixtures screen:** date, format, opponent, televised, strip, lock date, and any open request.
- **Commands:** assign a strip to a fixture, and accept or decline a request.
- **Messages:** requests arriving, deadlines, and a strip assigned by default at the lock.
- **The strip table** gains last played and the look at the ends.
- **The review screen.**

### 7. Harness policies and Gate B (10 to 12 h)
- **Policies choose strips now.** Every policy prepares strips by the book, except neglect, which prepares nothing and takes the defaults.
  - **Greedy:** at each lock, the best-looking strip now (a centre strip if one is free, then the longest-rested), and it accepts every request.
  - **Planned:** a season rotation made at the start. It keeps centre strips for televised matches, spaces each strip's uses beyond the establishment time, avoids neighbours of strips in use soon, and declines requests that put the referee's rating at risk.
- **Season score:** stakeholder satisfaction plus the ratings, recorded per month, so early and late season can be compared.
- **Gate B, as agreed on first results:** over 1,000 seasons, greedy trails planned by no more than 3 points in April and May, and planned leads greedy by at least 10 points in August and September. Check 3 keeps running alongside, now measuring planned play (by-the-book preparation with the planned rotation) rather than the season file's old fixed plan.

## Dependencies
1 → 2 and 3 → 4 → 5. Task 6 follows 1 to 5. Task 7 needs everything.

## New content
- New file: `stakeholders.json`.
- Changed files:
  - `season.json`: fixture ids and the televised flag, with strips removed.
  - `ground.json`: strip positions.
  - `calendar.json`: the assignment lock.
  - `wear.json`: neighbour wear, lasting wear and establishment.
- Every number is a placeholder and goes on the tuning list.

## Risks
| Risk | Fallback |
| --- | --- |
| Greedy doesn't fall behind: strips recover too fast or aren't scarce | Tune establishment and lasting wear first; then add minor fixtures (Second XI, club hires) that need strips too |
| Captain requests feel arbitrary | Judge delivery on one named characteristic per request, and have commentary name it |
| Too many screens and messages slow the season down | A single inbox line per turn; the fixtures screen only on request |
| Stakeholder numbers swamp the ratings | Keep the referee's weight fixed in the season score, and trace each stakeholder separately in the harness |

## Verification
- Each task: `dotnet build -warnaserror` and `dotnet test` pass, and the replay snapshot changes only when a rule change is intended.
- Task 3: a harness trace of a strip reused at one, two, three and five weeks, checked against the tuning target.
- Task 7: Gate B recorded in `progress.md` with seeds, policies and thresholds, on tuning seeds and on held-out seeds.
