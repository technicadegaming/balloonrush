# Gameplay and Redemption Balancing

## Core loop

A 35-second default round progresses from easy balloon spacing to Rush Mode during the final five seconds. Balloons rise through three lanes toward a bright Hit Zone. LEFT and RIGHT select the lane; POP judges the closest eligible balloon in that lane.

Every attempt receives a timing rating:

- Perfect: closest to the Hit Zone center
- Great: inside the middle timing window
- Good: inside the outer timing window
- Miss: outside the valid window, no valid balloon, or an avoidable passed reward balloon when that rule is enabled

Consecutive successful pops build the combo. A miss, bomb, passed reward balloon, or configurable timeout can reset it.

## Default balloon set

| Balloon | Purpose | Default ticket behavior |
|---|---|---|
| Green `+1` | Common target | 1 base ticket |
| Blue `+5` | Valuable target | 5 base tickets |
| Purple `x2` | Temporary multiplier | Doubles normal ticket awards for 3.5 seconds |
| Gold `?` | Mystery | 2–10 tickets, combo, slowdown, multiplier, or Golden chance |
| Red bomb | Avoid | Breaks combo; optional ticket penalty |
| Black super bomb | Rare danger | Resets combo and cancels active x2 |
| Gold star | Golden trigger | 3 base tickets and starts Golden Balloon Round |
| Gold crown | Final bonus target | Perfect = configured jackpot |

Normal Green and Blue weights are operator-configurable along with every special-balloon weight. This removes a previously hidden balancing dependency and lets the cabinet operator change the full spawn mix without editing assets or recompiling.

## Ticket multiplier strategy

Competitive score grows aggressively enough to make skilled play visible. Ticket growth is intentionally more restrained.

Default timing ticket multipliers:

`GOOD 1.00x / GREAT 1.05x / PERFECT 1.20x`

Default combo ticket multipliers:

`x5 1.05x / x10 1.10x / x15 1.20x / x20 1.35x / x30+ 1.50x`

The Operator Menu exposes both groups. Validation keeps them ordered so a better timing result or higher combo never reduces the award.

## Payout limits

The production limits are enforced in multiple layers:

- Jackpot is clamped to `1–500` tickets.
- Maximum total payout is clamped to `jackpot–1,000` tickets.
- `ScoreManager` clamps every ticket addition.
- `TicketManager` clamps the final physical payout again.
- Golden and Mystery rewards pass through the same cap.

Default visible ladder:

`500 / 250 / 100 / 50 / 25 / 10 / 5 / 1`

## Golden Round resolution

The final crown balloon receives a protected lead-in and minimum travel speed. When the visible bonus timer reaches zero, the round waits for that crown balloon to cross the Hit Zone or pass before resolving the result. This prevents low operator speed settings from making the jackpot target impossible.

Perfect awards the configured 500-ticket jackpot by default. Great, Good, and Miss use configurable 50, 20, and 5 ticket consolation values. Golden Round suppresses bombs and makes valuable balloons more common, while still respecting the total-game cap.

## Repeat-play drivers

The implementation encourages another attempt without using long menus:

- visible machine high score in Attract Mode
- result-screen `NEW HIGH SCORE` and `NEW TICKET RECORD` callouts
- result-screen score-gap message when the player narrowly misses the top score
- highest combo and timing breakdown
- rare Golden Balloon warning
- near-miss feedback and major combo effects
- escalating final-five-second Rush Mode
- skill-based timing grades
- Mystery outcomes and Golden chance
- an obvious 500-ticket jackpot

## Measurement workflow

The Operator Menu tracks lifetime totals, while the per-session CSV audit records each completed game. Use both:

- games played and credits
- average tickets per game
- score and ticket percentiles from exported audit data
- jackpot frequency
- Perfect/Great/Good/Miss mix
- bomb-hit frequency
- new-score and new-ticket-record frequency
- hardware enabled/disabled state
- payout settings active for each session

Start with `DEFAULT_BALANCE_PROFILE.md`, collect at least 500 paid-game samples, then adjust one category at a time. Do not tune from only staff experts or only young first-time players.
