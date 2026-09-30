# Cricket Groundsman Simulator

A management sim about preparing a cricket ground. Current goal: the MVP in docs/mvp-plan.md, which tests whether the core loop is fun.

- Design: docs/design.md
- MVP plan: docs/mvp-plan.md (follow its phase order)
- Current phase plan: docs/phase-4-plan.md (earlier: docs/phase-3-plan.md, docs/phase-2-plan.md)
- Research notes (groundskeeping practice, ICC ratings): docs/research.md
- Progress and next steps: docs/progress.md (read at session start, update at session end)

## Structure
- `src/Groundsman.Core`: netstandard2.1, C# 9. All rules and state. No Unity, console or file IO types.
- `src/Groundsman.Cli`: playable text UI (Spectre.Console), current .NET.
- `src/Groundsman.Harness`: headless season runner with scripted policies, current .NET.
- `tests/Groundsman.Tests`: xUnit.
- `content/*.json`: all tuning numbers. Never hard-code a balance value in the core.

## Rules
- True strip state never reaches `GameView`. Views are built from readings only.
- Readings are ranges that widen with age. The true value usually sits inside the range but can occasionally fall outside it; miss rates per source live in content.
- Randomness only through the core's seeded generator, one stream per system. No `System.Random`, no `DateTime.Now`, no logic that depends on dictionary iteration order.
- The hourly tick runs in a fixed order: weather, covers, moisture, grass, tasks in progress, match, wear and recovery.
- Check any newer C# feature against Unity 6 support before using it in the core.
- British spelling in code, text and content.
- Comments only for non-obvious why (constraints, workarounds, invariants). No end-of-line comments. Don't restate what the code does.

## Workflow
- Development is on a Mac. Tester builds go out for macOS and Windows.
- Write or update tests before changing a simulation rule. `dotnet test` must pass before a commit.
- Don't invent balance numbers silently. Put them in content with a sensible placeholder and list them in the session summary so they can be tuned.
- Keep commits small, one task per commit.
- Don't add Claude or Claude Code as an author or co-author: no `Co-Authored-By` trailers and no "Generated with Claude Code" lines in commits or pull requests.
