# Validation records

Grid-Preview contains fresh October 18 runs: 200 core checks, 188 UI checks and
ten original MP1 1.39 GridLayout checks, plus a visually inspected editor render.
See its README for reproduction and the outstanding desktop comparison blocker.

Core-Tests.txt and UI-Tests.txt contain translated descriptions of the original
version 0.9 test runs (189 core checks and 185 UI checks). Translation preserves
the recorded results; it does not constitute a fresh test run.

MP1-Native contains the ten-check probe against original MP1 1.39 Core classes,
its results, reproduction script and source provenance.

Titan contains historical checks of 528 XML files, 602 page/theme variants,
586 edit/undo probes and ten project/export probes.

Language fixtures may deliberately contain German text to test language fallback,
Unicode handling and byte-preserving XML editing. Original external skin filenames
and XML values in recorded test inputs are retained where their exact identity matters.
