# Study protocol

How each playtest session was run. The aim was to compare the adaptive music with a static version of the same music while keeping everything else identical, and without telling players what was being tested.

## Setting and materials

- **Where and when:** participants’ homes, week 13 (early November 2026).
- **Build:** one Unity build of the Tetris prototype. The music condition is switched with the **Adaptive** checkbox on `DangerController`; nothing else changes between versions.
- **Audio:** the same headphones and volume setting for every participant.
- **Questionnaire:** the four-part paper form in [`Questionnaire/`](Questionnaire/), with the participant ID written in the corner of each page.
- **Data sheet:** the coded spreadsheet in [`Data/`](Data/).

## Conditions

| Condition | What the music does |
|---|---|
| **Adaptive** | `Danger` follows the stack height (rows ÷ 20), so layers fade in and out as the stack rises and falls. |
| **Static** | The same ten stems, held at `Danger` = 0.5 for the whole game: a fixed mid-intensity mix. |

The static control isolates the effect of adaptation itself: both versions use exactly the same music, mix settings and game.

## Session steps

1. Give the participant an ID (P01–P10). They read the participant information, tick consent and answer **Part 1** (background) before playing.
2. Refer to the builds only as **Version A** (played first) and **Version B** (played second). Never say “adaptive” or “static”.
3. Alternate the order: odd IDs play the adaptive version first, even IDs play the static version first. Record each person’s order on the data sheet, not on their form.
4. Set the condition for Version A with the **Adaptive** checkbox on `DangerController` (unticked = Danger held at **Static Danger** 0.5).
5. Turn off **Show Music Inputs** on `TetrisView`, so the on-screen Danger readout cannot reveal what the music follows.
6. The participant plays Version A for **five minutes**. After a game over they press Enter and keep playing.
7. They answer **Part 2** straight away.
8. Switch the condition, then they play Version B for five minutes and answer **Part 3** straight away.
9. They answer **Part 4** (comparing the two versions) and hand the form back.

## After the session

- Enter the answers into the spreadsheet using the codebook in [`Data/README.md`](Data/README.md): numbers for ticked boxes, blank for anything unanswered (never 0), and written answers word for word.
- The **Ratings** sheet looks up each person’s condition from their order, and the Part 4 helper columns convert “Version A / B” answers into Adaptive / Static / No difference.
- Run [`Analysis/analysis.py`](Analysis/analysis.py) to produce the statistics.

## Ethics and data handling

Participation was voluntary and every participant ticked the consent box before playing. The form collects no names or contact details; participants appear in the data only as P01–P10.
