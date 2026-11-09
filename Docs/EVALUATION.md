# Matching evaluation plan

No controlled accuracy benchmark against Playnite's official provider has been completed. Synthetic fixtures check specific rules; they do not estimate real-library accuracy.

## Dataset and procedure

1. Assemble permission-safe cases spanning PC and emulator platforms, clean and messy titles, numeral variants, aliases, editions, ports, bundles, short names and unknown platforms. Keep some cases held out from rule development.
2. Have each expected identity independently checked against catalog/platform/release evidence. Record disputed cases as unresolved instead of assigning convenient ground truth.
3. Freeze source commit, settings, catalog observation date, input fields and expected IDs. Avoid private library exports; publish only an explicitly reviewed minimal dataset.
4. Run read-only matching. Record selected IGDB ID, automatic eligibility, review/unmatched status and error class. Do not apply metadata during evaluation.
5. For a Playnite comparison, use the same cases and input information, record the provider/version/settings, and distinguish different automation policies. Do not assume deferred cases are incorrect matches.

## Report

- Automatic-match precision: correct automatically eligible matches / adjudicated automatically eligible matches.
- Automation coverage: automatically eligible cases / all evaluated cases.
- Review and unmatched rates, unresolved ground-truth count and sample sizes.
- Wrong identity, platform, sequel and edition counts; show failure examples as well as successes.
- Results by platform and naming category, with held-out results separated from development fixtures.

A conservative matcher can improve precision while reducing coverage. Report both; no single score establishes universal superiority. Synthetic assertion counts, UI history counts and completed imports are not substitutes for adjudicated matching outcomes.
