# Analysis

Python scripts that reproduce every statistic and chart in [`../Results.md`](../Results.md) and the final report.

| File | What it does |
|---|---|
| [`analysis.py`](analysis.py) | Reads the coded data, runs every test and writes [`results.json`](results.json) |
| [`figures.py`](figures.py) | Draws the charts in [`../figures/`](../figures/) from `results.json` |
| [`requirements.txt`](requirements.txt) | Python packages needed |
| [`output.txt`](output.txt) | The printed summary from the run used in the report |

```bash
pip install -r requirements.txt
python analysis.py
python figures.py
```

Methods are described at the top of `analysis.py` and under each table in [`../Results.md`](../Results.md).
