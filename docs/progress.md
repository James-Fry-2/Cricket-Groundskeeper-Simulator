# Progress

## Current phase
Phase 1: strip model and daily loop

## Phase 1 tasks
- [x] Solution skeleton: Core, Cli, Harness, Tests projects, references, one passing test, `IGame` interface
- [ ] Seeded random generator with separate streams, plus replay tests
- [ ] `GameTime`, calendar, hourly tick and pace rules for the next decision point
- [ ] Strip state types, one command (water), a view built from readings
- [ ] Bare console loop that advances time and prints the day and strip summary

## Balance numbers to tune
(none yet)

## Session log

### 2026-09-27
- Solution skeleton: `Groundsman.sln` with Core (netstandard2.1, C# 9 pinned), Cli (Spectre.Console), Harness and Tests (xUnit). `IGame` plus placeholder `GameView`, `AdvanceResult`, `IGameCommand`, and `CommandResult` with two tests.
- Next: seeded random generator with separate streams, plus replay tests.
