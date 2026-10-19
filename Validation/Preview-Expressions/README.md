# Preview text expression subset

Date: October 19, 2026. Baseline: 2a8c879bcd682d5428ac5b014a2b840a37fda1aa.

Preview text now evaluates a bounded subset of MP1 `#(...)` expressions instead
of presenting every supported expression literally. Supported operations are
`string.trim`, `string.ltrim`, `string.rtrim` (one or two string arguments),
`string.replace`, `string.length`, `string.equals`, `string.contains`,
`string.starts`, `eq`, `neq` and `iif`. Nested calls, quoted strings, integer
literals and known preview properties are supported. Embedded scoped define
chains now resolve in ordinary text as well.

The reference is MediaPortal's own expression grammar and function definitions:
- https://github.com/MediaPortal/MediaPortal-1/blob/master/mediaportal/Core/guilib/ExpressionManager.cs
- https://github.com/MediaPortal/MediaPortal-1/blob/master/mediaportal/Core/guilib/Functions.cs

Expression string comparisons preserve whitespace and use invariant case-insensitive
comparison. This differs from MP1's legacy visibility-condition string functions;
the existing visibility evaluator has not been changed. Numeric and string literals
remain distinct for `eq`/`neq`. All arguments must be known, including both branches
of `iif`, consistent with eager evaluation. Missing plugin properties, unsupported
functions, malformed expressions and exceeded resource limits preserve the complete
original expression. Preview values inserted as text are not evaluated recursively.
Limits: 32 nested calls, 16 arguments, 8192 characters per evaluated expression or
replacement expansion. This is not a complete MP1 expression engine.

## Fresh validation

All production sources compile with Mono C# compiler 6.8 (two existing unused-local
warnings in CollectionTests). The console probe runs against that production assembly:
**25 checks passed**, covering nested evaluation, quotes/delimiters, types, sample
precedence, missing values, unsupported syntax, cycles, bounds and XML/undo preservation.
See Probe.cs and Results.txt. Compile Probe.cs with references to the built production
assembly and System.Drawing, and pass a fresh writable directory when running it.

The probe constructs the production parsed source and control directly, avoiding the
Windows-only path containment rules. Those rules were not altered. No Windows build,
full core suite, UI smoke suite, installed Titan render or MP1 runtime validation ran
in this Linux session. The prior 237 core checks are historical, not a result of this
change. The broader complex-page overlap and unsupported visibility conditions remain
open; this milestone addresses supported text expressions only.
