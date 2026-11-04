"""Charts for the Tetris music study.

Usage (from this folder, after running analysis.py):
    python figures.py                       # reads results.json, writes ../figures/*.png
    python figures.py <results.json> <out dir>
"""
import json
import sys
from pathlib import Path

import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
from matplotlib.patches import Rectangle

HERE = Path(__file__).resolve().parent
RES = json.loads((Path(sys.argv[1]) if len(sys.argv) > 1 else HERE / "results.json").read_text())
OUT = Path(sys.argv[2]) if len(sys.argv) > 2 else HERE.parent / "figures"
OUT.mkdir(parents=True, exist_ok=True)

plt.rcParams.update({
    "font.family": "sans-serif",
    "font.sans-serif": ["Carlito", "Calibri", "Arial", "DejaVu Sans"],  # first one installed is used
    "font.size": 9,
    "axes.edgecolor": "#c3c2b7",
    "axes.linewidth": 0.8,
    "xtick.color": "#52514e",
    "ytick.color": "#52514e",
    "savefig.dpi": 300,
    "figure.facecolor": "white",
})

BLUE = "#2a78d6"      # adaptive
ORANGE = "#eb6834"    # static
NEUTRAL = "#c3c2b7"   # no difference / zero line
INK = "#0b0b0b"
INK2 = "#52514e"
GRID = "#e1e0d9"


def save(fig, name):
    fig.savefig(OUT / name, bbox_inches="tight", pad_inches=0.04, facecolor="white")
    plt.close(fig)


def forest_plot():
    """Mean paired difference (adaptive minus static) with 95% CI for each statement."""
    groups = [
        ("Music tracks danger", [("h Music signalled danger", "Music helped me tell I was in danger (h)"),
                                 ("g Music fit the game", "Music fit what was happening (g)"),
                                 ("e More tense as stack rose", "Felt more tense as my stack rose (e)")]),
        ("Transitions unobtrusive (lower is better)", [("i Music distracting", "Music distracted me (i)"),
                                                       ("j Music repetitive", "Music became repetitive (j)")]),
        ("Player experience", [("b Absorbed", "Felt absorbed (b)"),
                               ("c Lost track of time", "Lost track of time (c)"),
                               ("d Tense", "Felt tense or under pressure (d)"),
                               ("f In control", "Felt in control (f)"),
                               ("a Enjoyed", "Enjoyed playing (a)")]),
    ]
    rows = []
    for g, items in groups:
        rows.append(("header", g))
        rows.extend(("item", it) for it in items)
    fig, ax = plt.subplots(figsize=(6.3, 3.75))
    n = len(rows)
    ax.set_ylim(-0.8, n - 0.2)
    ax.set_xlim(-2, 2)
    ax.axvline(0, color=NEUTRAL, linewidth=1.0, zorder=1)
    for v in (-2, -1, 1, 2):
        ax.axvline(v, color=GRID, linewidth=0.6, zorder=0)
    labels = []
    for k, (kind, payload) in enumerate(rows):
        y = n - 1 - k
        if kind == "header":
            labels.append((y, payload, True))
            continue
        key, label = payload
        r = RES["items"][key]
        labels.append((y, label, False))
        ax.plot([r["diff_ci_lo"], r["diff_ci_hi"]], [y, y], color=BLUE, linewidth=2, solid_capstyle="round", zorder=2)
        ax.scatter([r["diff_mean"]], [y], s=34, color=BLUE, edgecolor="white", linewidth=1.5, zorder=3)
        ax.text(2.12, y, f"{r['adaptive_mean']:.1f}", ha="left", va="center", fontsize=8.4, color=INK, clip_on=False)
        ax.text(2.62, y, f"{r['static_mean']:.1f}", ha="left", va="center", fontsize=8.4, color=INK, clip_on=False)
    ax.text(2.12, n - 0.35, "Adaptive", ha="left", va="center", fontsize=8.2, color=INK2, fontweight="bold", clip_on=False)
    ax.text(2.62, n - 0.35, "Static", ha="left", va="center", fontsize=8.2, color=INK2, fontweight="bold", clip_on=False)
    ax.set_yticks([y for y, _, _ in labels])
    ax.set_yticklabels([t for _, t, _ in labels], fontsize=8.4)
    for tick, (_, _, is_header) in zip(ax.get_yticklabels(), labels):
        tick.set_fontweight("bold" if is_header else "normal")
        tick.set_color(INK if is_header else INK2)
    ax.tick_params(axis="y", length=0)
    for s in ("top", "right", "left"):
        ax.spines[s].set_visible(False)
    ax.set_xticks([-2, -1, 0, 1, 2])
    ax.set_xticklabels(["−2", "−1", "0", "+1", "+2"], fontsize=8.2)
    ax.set_xlabel("Adaptive minus static (points on the 1–5 agreement scale); dot = mean, line = 95% CI",
                  fontsize=8.4, color=INK2)
    save(fig, "forest-plot.png")


def forced_choices():
    """Part 4 answers mapped from Version A/B to adaptive/static."""
    qs = [("4.3 Music fit better", "Music fit the gameplay better"),
          ("4.2 More tense", "Felt more tense or exciting"),
          ("4.4 Played better", "I played better"),
          ("4.1 Enjoyed more", "Enjoyed more"),
          ("4.5 Would choose", "Would choose at home")]
    fig, ax = plt.subplots(figsize=(6.3, 2.15))
    for k, (key, label) in enumerate(qs):
        y = len(qs) - 1 - k
        r = RES["part4"][key]
        x = 0
        for count, color, txt in [(r["adaptive"], BLUE, "white"), (r["no_difference"], NEUTRAL, INK),
                                  (r["static"], ORANGE, "white")]:
            if count == 0:
                continue
            ax.barh(y, count - 0.06, left=x + 0.03, height=0.56, color=color, linewidth=0, zorder=2)
            ax.text(x + count / 2, y, str(count), ha="center", va="center", fontsize=8.4, color=txt,
                    fontweight="bold", zorder=3)
            x += count
    ax.set_yticks(range(len(qs)))
    ax.set_yticklabels([lbl for _, lbl in qs][::-1], fontsize=8.6, color=INK2)
    ax.set_xlim(0, 10)
    ax.set_xticks(range(0, 11, 2))
    ax.set_xlabel("Number of participants (n = 10)", fontsize=8.6, color=INK2)
    ax.tick_params(axis="y", length=0)
    for s in ("top", "right", "left"):
        ax.spines[s].set_visible(False)
    handles = [Rectangle((0, 0), 1, 1, color=c) for c in (BLUE, NEUTRAL, ORANGE)]
    ax.legend(handles, ["Adaptive version", "No difference", "Static version"], ncol=3, frameon=False,
              loc="lower left", bbox_to_anchor=(0.0, 1.0), fontsize=8.4, handlelength=1.1, handleheight=0.9,
              columnspacing=1.6, borderaxespad=0.2)
    save(fig, "forced-choices.png")


forest_plot()
forced_choices()
print("charts written to", OUT)
