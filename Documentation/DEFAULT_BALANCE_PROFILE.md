# Default Commercial Floor-Test Profile

Balloon Rush ships with a deliberately conservative starting profile. It is designed for initial cabinet testing, not as a guaranteed profitability or legal-compliance setting. Final values must be tuned against the location's price per play, ticket value, player mix, and actual cabinet telemetry.

## Core defaults

| Setting | Default |
|---|---:|
| Round duration | 35 seconds |
| Jackpot | 500 tickets |
| Hard maximum payout | 1,000 tickets |
| Green balloon | 1 ticket |
| Blue balloon | 5 tickets |
| Golden trigger balloon | 3 tickets |
| Mystery range | 2–10 tickets |
| Golden GREAT consolation | 50 tickets |
| Golden GOOD consolation | 20 tickets |
| Golden MISS consolation | 5 tickets |
| x2 duration | 3.5 seconds |
| Input debounce | 25 ms |

## Spawn weights

Weights are relative rather than percentages. Every normal and special balloon weight is editable from the Operator Menu.

| Balloon | Default weight |
|---|---:|
| Green | 1.000 |
| Blue | 0.120 |
| Red bomb | 0.070 |
| Black super bomb | 0.008 |
| Golden trigger | 0.008 |
| Mystery | 0.050 |
| x2 multiplier | 0.040 |

The difficulty curve may increase danger frequency later in the round. Golden Round temporarily suppresses bombs and increases the relative frequency of valuable balloons.

## Ticket multipliers

Timing and combo affect ticket value, but the default growth is restrained so visual excitement can rise faster than redemption liability.

| Timing | Multiplier |
|---|---:|
| GOOD | 1.00x |
| GREAT | 1.05x |
| PERFECT | 1.20x |

| Combo milestone | Multiplier |
|---|---:|
| x5 | 1.05x |
| x10 | 1.10x |
| x15 | 1.20x |
| x20 | 1.35x |
| x30+ | 1.50x |

All timing and combo ticket multipliers are operator-configurable and validated as non-decreasing.

## Simplified simulation benchmark

A 200,000-game offline benchmark using the same simplified assumptions as the included Editor payout simulator produced approximately:

| Metric | Estimate |
|---|---:|
| Average | 74.3 tickets |
| Median | 59 tickets |
| 75th percentile | 70 tickets |
| 90th percentile | 87 tickets |
| 95th percentile | 108 tickets |
| 99th percentile | 562 tickets |
| Estimated jackpot rate | 2.65% |
| 1,000-ticket cap rate | 0% in this run |

Assumptions were a 78% successful-pop rate, 24% PERFECT share among successful hits, 43% GREAT share, 90% bomb avoidance, and 42% final-Golden success. This model is an estimate. It does not fully reproduce human hesitation, lane-choice mistakes, Golden Round bonus-balloon value, hardware latency, or every timing interaction.

## Floor-test tuning order

1. Run at least 100 staff games and verify the game is understandable without instruction.
2. Run `Tools > Balloon Rush > Payout Simulator` using weak, average, and strong skill assumptions.
3. Install the cabinet with the conservative profile.
4. Review `BalloonRushAudit/sessions.csv` after at least 500 paid games.
5. Change only one category at a time: spawn weights, base ticket values, timing multipliers, combo multipliers, or Golden frequency.
6. Re-run the simulator and retain the exported CSV with the operator change log.
7. Keep the 500-ticket jackpot and 1,000-ticket hard cap unless the cabinet's approved operating rules require lower values.
