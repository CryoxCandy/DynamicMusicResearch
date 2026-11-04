"""Analysis of the Tetris music study: adaptive vs static music, within-subjects, counterbalanced.

Usage (from this folder):
    pip install -r requirements.txt
    python analysis.py                      # reads ../Data/Tetris_Music_Study_Data.xlsx
    python analysis.py <data.xlsx> <out dir>

Writes results.json (every number reported in the final report and in ../Results.md) and prints a
readable summary. The EXAMPLE rows in the workbook are a data-entry example and are excluded.

Methods
- Paired comparisons: exact two-sided Wilcoxon signed-rank test. Zero differences are dropped
  (Wilcoxon's method), tied |differences| get mid-ranks, and the p-value comes from enumerating every
  sign assignment, so it is exact even with ties. Effect size: matched-pairs rank-biserial correlation
  r_rb = (W+ - W-) / (W+ + W-) (Kerby, 2014).
- 95% CIs for mean paired differences are t-based.
- Forced choices (Part 4): exact binomial sign tests, excluding "no difference".
- Order check: 2x2 crossover analysis comparing each player's first-minus-second-game difference
  between the two order groups with exact Mann-Whitney tests.
"""
import itertools
import json
import math
import sys
from pathlib import Path

import numpy as np
import pandas as pd
from scipy import stats

HERE = Path(__file__).resolve().parent
XLSX = Path(sys.argv[1]) if len(sys.argv) > 1 else HERE.parent / "Data" / "Tetris_Music_Study_Data.xlsx"
OUT = Path(sys.argv[2]) if len(sys.argv) > 2 else HERE
OUT.mkdir(parents=True, exist_ok=True)

parts = pd.read_excel(XLSX, sheet_name="Participants")
ratings = pd.read_excel(XLSX, sheet_name="Ratings")
parts = parts[parts["Participant ID"].astype(str).str.match(r"^P\d+$")].copy()
ratings = ratings[ratings["Participant ID"].astype(str).str.match(r"^P\d+$")].copy()

ITEMS = {
    "x.1 Highest score": "Highest score",
    "x.2 Game Overs": "Game Overs (0-3)",
    "a Enjoyed": "a. Enjoyed",
    "b Absorbed": "b. Absorbed",
    "c Lost track of time": "c. Lost track of time",
    "d Tense": "d. Tense / under pressure",
    "e More tense as stack rose": "e. More tense as stack rose",
    "f In control": "f. In control",
    "g Music fit the game": "g. Music fit the game",
    "h Music signalled danger": "h. Music signalled danger",
    "i Music distracting": "i. Music distracting (neg.)",
    "j Music repetitive": "j. Music repetitive (neg.)",
    "x.6 Music rating (1-10)": "Music rating (1-10)",
}


def wilcoxon_exact(d):
    """Exact two-sided Wilcoxon signed-rank test by full sign-flip enumeration.

    Zero differences are dropped (Wilcoxon's method); tied |d| get mid-ranks and the
    enumeration is conditional on those ranks, so the p-value is exact even with ties.
    Returns dict with n_nonzero, W+ , W-, p, matched-pairs rank-biserial r, and the
    tie-corrected normal-approximation z (for reference)."""
    d = np.asarray(d, dtype=float)
    nz = d[d != 0]
    n = len(nz)
    if n == 0:
        return dict(n=0, Wplus=0.0, Wminus=0.0, p=1.0, r_rb=0.0, z=0.0)
    ranks = stats.rankdata(np.abs(nz))
    wplus = ranks[nz > 0].sum()
    wminus = ranks[nz < 0].sum()
    total = ranks.sum()
    expected = total / 2
    obs = abs(wplus - expected)
    count = 0
    for signs in itertools.product([0, 1], repeat=n):
        w = sum(r for r, s in zip(ranks, signs) if s)
        if abs(w - expected) >= obs - 1e-9:
            count += 1
    p = count / 2 ** n
    # tie-corrected normal approximation (no continuity correction), for reference only
    _, tie_counts = np.unique(ranks, return_counts=True)
    var = n * (n + 1) * (2 * n + 1) / 24 - (tie_counts ** 3 - tie_counts).sum() / 48
    z = (wplus - expected) / math.sqrt(var) if var > 0 else 0.0
    r_rb = (wplus - wminus) / total
    return dict(n=int(n), Wplus=float(wplus), Wminus=float(wminus), p=float(p), r_rb=float(r_rb), z=float(z))


def mean_ci(x, level=0.95):
    x = np.asarray(x, dtype=float)
    m = x.mean()
    se = x.std(ddof=1) / math.sqrt(len(x))
    t = stats.t.ppf(0.5 + level / 2, len(x) - 1)
    return float(m), float(m - t * se), float(m + t * se)


ad = ratings[ratings["Condition"] == "Adaptive"].set_index("Participant ID").sort_index()
st = ratings[ratings["Condition"] == "Static"].set_index("Participant ID").sort_index()
assert list(ad.index) == list(st.index) and len(ad) == 10

results = {"n": int(len(ad)), "items": {}}
for col, label in ITEMS.items():
    a = ad[col].astype(float).values
    s = st[col].astype(float).values
    d = a - s
    w = wilcoxon_exact(d)
    md, lo, hi = mean_ci(d)
    results["items"][col] = dict(
        label=label,
        adaptive_mean=float(a.mean()), adaptive_sd=float(a.std(ddof=1)), adaptive_median=float(np.median(a)),
        static_mean=float(s.mean()), static_sd=float(s.std(ddof=1)), static_median=float(np.median(s)),
        diff_mean=md, diff_ci_lo=lo, diff_ci_hi=hi,
        n_higher_adaptive=int((d > 0).sum()), n_same=int((d == 0).sum()), n_higher_static=int((d < 0).sum()),
        **w,
    )
    if col == "x.1 Highest score":
        t = stats.ttest_rel(a, s)
        results["items"][col]["paired_t"] = float(t.statistic)
        results["items"][col]["paired_t_p"] = float(t.pvalue)
        results["items"][col]["pct_change"] = float((a.mean() - s.mean()) / s.mean() * 100)

# ---- 2x2 crossover analysis (period vs treatment) for performance and key items
def crossover(col):
    seq_ad_first = parts.set_index("Participant ID")["Version A was"] == "Adaptive"
    pid_ad_first = [p for p in ad.index if seq_ad_first[p]]
    pid_st_first = [p for p in ad.index if not seq_ad_first[p]]
    # period-1 minus period-2 difference for each participant
    d_ab = np.array([ad.loc[p, col] - st.loc[p, col] for p in pid_ad_first], dtype=float)  # A first
    d_ba = np.array([st.loc[p, col] - ad.loc[p, col] for p in pid_st_first], dtype=float)  # S first
    treat = (d_ab.mean() - d_ba.mean()) / 2      # adaptive - static, period-adjusted
    period = (d_ab.mean() + d_ba.mean()) / 2     # first game - second game, treatment-adjusted
    mw_t = stats.mannwhitneyu(d_ab, d_ba, alternative="two-sided", method="exact")
    mw_p = stats.mannwhitneyu(d_ab, -d_ba, alternative="two-sided", method="exact")
    return dict(treatment_effect=float(treat), period_effect=float(period),
                treatment_p=float(mw_t.pvalue), period_p=float(mw_p.pvalue),
                d_ab=d_ab.tolist(), d_ba=d_ba.tolist())

results["crossover"] = {c: crossover(c) for c in [
    "x.1 Highest score", "x.2 Game Overs", "a Enjoyed", "f In control", "h Music signalled danger",
    "g Music fit the game", "b Absorbed", "x.6 Music rating (1-10)"]}

# ---- first vs second game regardless of condition
first = ratings[ratings["Version"] == "A"].set_index("Participant ID").sort_index()
second = ratings[ratings["Version"] == "B"].set_index("Participant ID").sort_index()
results["order"] = {}
for col in ["x.1 Highest score", "x.2 Game Overs", "a Enjoyed", "f In control"]:
    results["order"][col] = dict(first_mean=float(first[col].mean()), second_mean=float(second[col].mean()))

# ---- Did the music change? (1 No, 2 few times, 3 often, 9 not sure)
def noticed(df):
    v = df["x.4 Music changed"].astype(int)
    return dict(yes=int(v.isin([2, 3]).sum()), no=int((v == 1).sum()), not_sure=int((v == 9).sum()))

results["music_changed"] = {"Adaptive": noticed(ad), "Static": noticed(st)}
seq = parts.set_index("Participant ID")["Version A was"]
ad_first = ad[seq.loc[ad.index] == "Adaptive"]
ad_second = ad[seq.loc[ad.index] == "Static"]
results["music_changed_adaptive_by_order"] = {"played_first": noticed(ad_first), "played_second": noticed(ad_second)}
results["game_overs_adaptive_by_order"] = {"played_first": float(ad_first["x.2 Game Overs"].mean()),
                                           "played_second": float(ad_second["x.2 Game Overs"].mean())}

# ---- Part 4 forced choices, mapped to conditions in the helper columns
part4 = {}
for q in ["4.1 Enjoyed more", "4.2 More tense", "4.3 Music fit better", "4.4 Played better", "4.5 Would choose"]:
    v = parts[q]
    n_ad = int((v == "Adaptive").sum())
    n_st = int((v == "Static").sum())
    n_nd = int((v == "No difference").sum())
    bt = stats.binomtest(n_ad, n_ad + n_st, 0.5) if n_ad + n_st > 0 else None
    part4[q] = dict(adaptive=n_ad, static=n_st, no_difference=n_nd, sign_test_p=float(bt.pvalue) if bt else None)
results["part4"] = part4

# order check on 4.1: did people pick the version played first (A) or second (B)?
raw41 = parts["4.1 Enjoyed more (A/B/N)"]
results["order_check_4_1"] = dict(chose_first=int((raw41 == "A").sum()), chose_second=int((raw41 == "B").sum()),
                                  no_difference=int((raw41 == "N").sum()))

# 4.6 noticed a difference (1 none, 2 slight, 3 clear, 9 not sure)
v46 = parts["4.6 Noticed difference"].astype(int)
results["noticed_difference"] = dict(clear=int((v46 == 3).sum()), slight=int((v46 == 2).sum()),
                                     none=int((v46 == 1).sum()), not_sure=int((v46 == 9).sum()))
# fit judgement among those who reported no / unsure difference
no_diff_ids = parts.loc[v46.isin([1, 9]), "Participant ID"]
fit_nd = parts.set_index("Participant ID").loc[no_diff_ids, "4.3 Music fit better"]
results["fit_among_no_or_unsure_difference"] = {k: int(v) for k, v in fit_nd.value_counts().items()}
results["fit_among_no_or_unsure_difference"]["n"] = int(len(fit_nd))
nd_only = parts.set_index("Participant ID").loc[parts.loc[v46 == 1, "Participant ID"], "4.3 Music fit better"]
results["fit_among_no_difference_only"] = {k: int(v) for k, v in nd_only.value_counts().items()}

# ---- Demographics
age_map = {1: "18–24", 2: "25–34", 3: "35–44", 4: "45+", 9: "Prefer not to say"}
games_map = {1: "Never", 2: "Rarely", 3: "A few times a month", 4: "A few times a week", 5: "Daily"}
tetris_map = {1: "Never", 2: "A few times", 3: "Fairly often", 4: "A lot"}
music_map = {1: "Never", 2: "Rarely", 3: "Sometimes", 4: "Usually", 5: "Always"}
train_map = {1: "None", 2: "Under 2 years", 3: "2–5 years", 4: "More than 5 years"}


def counts(col, mapping):
    vc = parts[col].astype(int).value_counts()
    return {mapping[k]: int(vc.get(k, 0)) for k in mapping if vc.get(k, 0)}

results["demographics"] = {
    "age": counts("1.2 Age", age_map),
    "play_games": counts("1.3 Play games", games_map),
    "played_tetris": counts("1.4 Played Tetris", tetris_map),
    "music_on": counts("1.5 Music on in games", music_map),
    "musical_training": counts("1.6 Musical training", train_map),
    "hearing_difficulty": int((parts["1.7 Hearing difficulty"].astype(int) == 1).sum()),
    "consented": int((parts["1.1 Consent"].astype(int) == 1).sum()),
}

# ---- Composite scales (means of paired items) for a robustness check
def composite(df, cols):
    return df[cols].astype(float).mean(axis=1)

comps = {
    "Immersion (b, c)": ["b Absorbed", "c Lost track of time"],
    "Tension (d, e)": ["d Tense", "e More tense as stack rose"],
    "Music responsiveness (g, h)": ["g Music fit the game", "h Music signalled danger"],
    "Music intrusiveness (i, j)": ["i Music distracting", "j Music repetitive"],
}
results["composites"] = {}
for name, cols in comps.items():
    a = composite(ad, cols).values
    s = composite(st, cols).values
    d = a - s
    w = wilcoxon_exact(d)
    results["composites"][name] = dict(adaptive_mean=float(a.mean()), static_mean=float(s.mean()),
                                       diff=float(d.mean()), n_higher_adaptive=int((d > 0).sum()),
                                       n_same=int((d == 0).sum()), n_higher_static=int((d < 0).sum()), **w)

# ---- written comments, verbatim, for coding
comments = []
for _, r in parts.iterrows():
    for q in ["4.7 What was different (text)", "4.8 Anything else (text)"]:
        if isinstance(r[q], str) and r[q].strip():
            comments.append(dict(pid=r["Participant ID"], q=q[:3], first=r["Version A was"], text=r[q].strip()))
for _, r in ratings.iterrows():
    t = r["x.5 What changed (text)"]
    if isinstance(t, str) and t.strip():
        comments.append(dict(pid=r["Participant ID"], q=("2.5" if r["Version"] == "A" else "3.5"),
                             condition=r["Condition"], text=t.strip()))
results["comments"] = comments

(OUT / "results.json").write_text(json.dumps(results, indent=2, ensure_ascii=False))

# ---- readable summary
print(f"n = {results['n']}")
print(f"{'item':34s} {'Ad M(SD)':>13s} {'St M(SD)':>13s} {'diff':>7s} {'95% CI':>17s} {'+/=/-':>7s} {'n':>2s} {'p':>6s} {'r_rb':>6s}")
for col, r in results["items"].items():
    print(f"{r['label']:34s} {r['adaptive_mean']:6.2f} ({r['adaptive_sd']:4.2f}) {r['static_mean']:6.2f} ({r['static_sd']:4.2f}) "
          f"{r['diff_mean']:7.2f} [{r['diff_ci_lo']:6.2f},{r['diff_ci_hi']:6.2f}] "
          f"{r['n_higher_adaptive']}/{r['n_same']}/{r['n_higher_static']} {r['n']:2d} {r['p']:6.3f} {r['r_rb']:6.2f}")
print("score paired t:", results["items"]["x.1 Highest score"]["paired_t"], results["items"]["x.1 Highest score"]["paired_t_p"],
      "pct", results["items"]["x.1 Highest score"]["pct_change"])
print("\ncomposites:")
for k, v in results["composites"].items():
    print(f"  {k:30s} ad {v['adaptive_mean']:.2f} st {v['static_mean']:.2f} diff {v['diff']:.2f} n {v['n']} p {v['p']:.3f} r {v['r_rb']:.2f}")
print("\ncrossover:")
for k, v in results["crossover"].items():
    print(f"  {k:28s} treat {v['treatment_effect']:+.2f} (p {v['treatment_p']:.3f})  period {v['period_effect']:+.2f} (p {v['period_p']:.3f})  dAB {v['d_ab']} dBA {v['d_ba']}")
print("\norder (first vs second game):", json.dumps(results["order"]))
print("music changed:", json.dumps(results["music_changed"]))
print("adaptive noticed by order:", json.dumps(results["music_changed_adaptive_by_order"]))
print("adaptive game overs by order:", json.dumps(results["game_overs_adaptive_by_order"]))
print("part4:", json.dumps(results["part4"], indent=1))
print("order check 4.1:", results["order_check_4_1"])
print("noticed difference 4.6:", results["noticed_difference"])
print("fit among no/unsure difference:", results["fit_among_no_or_unsure_difference"])
print("fit among no difference only:", results["fit_among_no_difference_only"])
print("demographics:", json.dumps(results["demographics"], ensure_ascii=False, indent=1))
print("\ncomments:")
for c in comments:
    print(" ", c)
