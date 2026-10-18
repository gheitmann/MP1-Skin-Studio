# Known-value string conditions

Date: October 19, 2026, Europe/Berlin (continuation begun October 18).
Baseline: baa5b71414caa598e5cb875ec59ec16abac87b14.

The real Titan Video page uses string.contains in visibility rules, including
the list scrolling popup. The preview previously returned unknown for every
such call even when all required sample data was explicitly available.

The three-valued interpreter now supports two-argument string.contains and
string.starts alongside string.equals. It resolves operands using the existing
sample/define scopes; quoted commas remain part of an operand. Missing values,
unsupported expressions and invalid argument counts stay unknown, including
under negation. Include conditions retain their captured define context.

The upstream [GUIInfoManager.cs](https://github.com/MediaPortal/MediaPortal-1/blob/master/mediaportal/Core/guilib/GUIInfoManager.cs)
STRING_EQUALS/STRING_STARTS/STRING_CONTAINS branch trims and lowercases both
operands. An empty second operand selects equality for all three functions.
The preview now follows those rules, also correcting whitespace handling in
its existing equality implementation. It deliberately does not implement the
full native property/function parser or the single-argument overload here.

## Fresh checks

The console probe failed at its first contains assertion before the change.
After the change, all ten dedicated checks passed, including the real Titan
listscroller background: visible with an explicit yes scroll flag, hidden with
two explicit no flags, and unresolved with a required flag missing. Sample
updates and evaluation leave skin XML and undo history unchanged.

A fresh build and the console core suite passed **223 checks, zero failures**.
The permanent regression additionally covers include/child define scope,
quoted commas, whitespace, empty search strings and unknown values.

No GUI, MediaPortal launch, Computer Use or window capture was used. No UI suite
was run. The previous filmstrip and complex-page reports remain historical;
the core suite includes the filmstrip regression. Visual/runtime equivalence
and further dynamic functions remain unverified. DirectX remains deferred.

Compile Probe.cs as a console executable next to MP1SkinStudio.exe, referencing
that assembly, System and System.Core. Pass the installed Titan root. Run the
core suite using Validation/Collection-Geometry/RunCore.cs and a scratch output
directory, without invoking the editor application entry point.
