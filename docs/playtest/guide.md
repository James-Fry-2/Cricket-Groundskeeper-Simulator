# Cricket Groundsman: quick-start guide

You're head groundsman at Kestrel Lane, a county cricket ground. Over one season, April to September, the club plays 18 home matches. You prepare a pitch for each one, and three people judge how it plays: the match referee, the captain and the board.

This is an early test of the game's idea, played as text in a terminal. It will look plain, because it's the loop we're testing, not the looks. A season takes a few hours, and you can stop whenever you like: it saves itself after every turn.

## If you're new to cricket
- **The square:** the block of 12 grass strips in the middle of the ground. Each match is played on one strip, the pitch.
- **Footholes:** the holes bowlers dig where they land. Deep ones make the ball bounce unpredictably.
- **Seam:** the ball moving off the pitch. It's helped by grass and damp.
- **Spin:** the ball turning. It's helped by a dry, worn surface, usually late in a long match.
- **Carry:** how well the ball comes through to the wicketkeeper: pace and bounce together. A pitch with little carry is "slow and low", which the referee doesn't like.
- **The referee's rating:** very good, satisfactory, unsatisfactory or unfit. Unsatisfactory costs a demerit and unfit costs three. Five demerits in five years and the ground loses the right to host matches.
- **Formats:** a four-day match lasts up to four days and wears the strip most. One-day and T20 matches are a day each.

## If you're new to this kind of game
- **Typing commands:** you type short commands and press Enter. The screen shows the ground after each one.
- **Turns:** time only moves when you press Enter on an empty line, or type `ff`. Turns are a week in winter and a day in season. In a strip's final days they're morning and afternoon, and on match days they go session by session.
- **`ff` skips ahead** until something needs you: news, a match starting, a strip to choose, or rain threatening a strip you're preparing.
- **`h` lists every command.** `intro` shows the introduction again.

## What you're doing
1. **Choose a strip for each fixture.** Type `x` to see the fixtures and `p <fixture> <strip>` to choose one, for example `p 3 6`. You can change your mind until the build-up starts, ten days before the match. Things to weigh:
   - **Use wears a strip.** Its ends need a few weeks to grow back after repairs (`e <strip>`), and each match leaves wear that lasts the season. Reusing a strip soon, or a third time, makes for an uneven pitch.
   - **Centre strips,** marked `c` (5 to 8), are what the board wants for televised matches. There are eight televised matches, all season long.
   - **Neighbours wear too:** a match wears the strips either side of it.
2. **Prepare it over the ten-day build-up.** The research the game is based on suggests:
   - **Water deeply** about five days out (`w <strip>`). You want moisture below and a drier surface by match day.
   - **Roll** most days when the surface feels damp but not wet (`f <strip>` to feel it, then `l <strip> <roller> <minutes>`). Start with the light roller and work up to the heavy one. Rolling too wet damages the strip; rolling too dry does nothing.
   - **Mow** down in steps to about 7mm by the day before (`m <strip> <mm>`). Taking more than a third off at once scalps it.
   - **Cover** against rain near the end (`c <strip>`), and take the cover off afterwards (`u <strip>`). A covered strip dries slowly.
3. **Look after it during the match.** Clean the footholes at a break (`clean <strip>`), and fill them at close of play in a four-day match (`fill <strip>`).
4. **Repair it afterwards** (`e <strip>`), and give it time.

## Readings
You never see a strip's true state. Readings give a range, and they can be wrong:
- `f <strip>`: feel. Quick; says dry, damp or wet, and how the ends look.
- `r <strip>`: moisture probe. A range for the surface.
- `d <strip>`: soil core. Slow; a range for the moisture below the surface.

Readings get vaguer as they age and as rain falls. Every job takes staff hours: you, Sam and Jo. Add a name to give someone else the job, for example `w 3 sam`.

## The people
- **The captain** asks for a kind of pitch before some matches: green, turning, pace or flat. Answer with `yes <#>` or `no <#>`. A promise you don't keep costs more than saying no.
- **The board** wants four-day matches to last into day four, televised matches on centre strips, and no demerits. Sometimes it asks too.
- **The referee** rates every pitch, and the commentary says why the pitch played as it did.

At the end of the season you get a review: how each of them feels and why, your ratings, and how worn your square is.

## When you've finished
Your seasons are in `Documents/Cricket Groundsman`. Zip the season's folder and send it with the questionnaire. If you wrote notes as you played, put them in that folder as `notes.txt`. Nothing is sent from your computer on its own.
