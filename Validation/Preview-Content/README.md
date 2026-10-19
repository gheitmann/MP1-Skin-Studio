# Known preview content conditions and localization

Date: October 19, 2026. Baseline: 133c0c322ab4f5eb5c9d7dbf2f63239dd18b490f.

The preview now handles Control.HasText and Control.HasThumb when the referenced logical control and its content are known. Control IDs must be unambiguous. Content lookup does not recurse through the target visibility condition, and uses a separate recursion key from Control.IsVisible. Unknown operands, malformed expressions and missing language entries remain unresolved.

HasText supports label, fadelabel, textbox and textboxscrollup content, including scoped defines, supported nested expressions and loaded language snapshots. Empty text is false; known nonempty plain text is true. Whitespace-only and markup-bearing text remain unknown because native font/markup metrics are not simulated. Other control types return false. Missing or duplicate target IDs remain unknown rather than assuming a runtime result.

HasThumb supports static local image textures with positive dimensions. Blank/sentinel textures and missing local files return false. Theme media takes precedence. Unknown bindings, paths outside Media, corrupt images and resource-bound exclusions remain unresolved. Image metadata observations are cached by path, size and modification timestamp, with a 512-entry limit. Image inspection releases streams immediately; there is no client/DirectX image loading. This is a bounded offline approximation, not native texture-cache equivalence.

Text expression L(...) now resolves integer and numeric-string IDs against loaded primary/fallback language snapshots, respects prefix selection and works in nested trim expressions. Missing/invalid IDs and expansions beyond 8,192 characters preserve the complete original expression.

Reference implementations inspected:
- https://github.com/MediaPortal/MediaPortal-1/blob/master/mediaportal/Core/guilib/GUIInfoManager.cs (CONTROL_HAS_TEXT and CONTROL_HAS_THUMB cases)
- https://github.com/MediaPortal/MediaPortal-1/blob/master/mediaportal/Core/guilib/Functions.cs (L overloads)

## Fresh validation

Final Windows build: 245 core checks, zero failures. Six added checks cover known/unknown self text conditions, ambiguity, localization, thumbnail replacement/removal, relative-path restrictions and theme precedence. Existing 25 expression probe checks and all eleven real Titan Complex-Page stages also passed against this build.

The same isolated Titan folder state before/after gives 211 controls, hidden 89 to 91, unresolved 89 to 87. HasText(7773) becomes false, HasThumb(60) becomes true, and Default Cover Art becomes false while Cover Art remains true. Unresolved ClearLogo content is retained. The targeted probe checks the real German nested L expression, saved-project roundtrip, unchanged XML signature/history and all 17 loaded source hashes. Probe.cs reproduces this comparison and can render a local PNG using the production SkinCanvas without launching an interactive editor.

A fresh offscreen production render was generated and visually inspected. It retains one Videos row and a folder image. Unknown metadata/image placeholders remain visible; clock/date, runtime state and font fidelity remain open. No fresh interactive editor or MP1 client test was performed for this source milestone. No export was installed and no original skin files changed. DirectX replacement remains deferred.

Next: address explicit folder metadata and missing-image presentation controls, then validate isolated exports in the client. Do not equate reduced unknown counts or hidden placeholders with runtime equivalence.
