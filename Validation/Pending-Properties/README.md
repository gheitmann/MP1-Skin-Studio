# Preserve unapplied property inputs

Date: October 19, 2026, Europe/Berlin. Baseline: 344fcc60f5d5dca5fa5862cdeb0a49a8f72cbfc4.

Tree selection, canvas gestures and several dialogs already protected pending
property input. Other lifecycle operations could still clear those inputs or
save/export the earlier applied model without making the omission clear.

This checkpoint extends the existing pending-input check:

- Page/theme changes are blocked while inputs are pending, restoring the active
  selector value without reloading or clearing fields. A failed page load also
  restores its selector to the model's retained page.
- Save Project and ZIP export require applying or reverting pending values.
  SaveSession also checks before touching its target file.
- Pending fields mark the session unsaved immediately, including changes to a
  previously saved model. Closing asks explicitly about discarding unsaved data.
- Undo/redo, insert/duplicate, alignment, navigation connections/focus traversal,
  navigation-mode changes and editor protection/visibility refresh actions are
  blocked before they can replace the property fields.
- Existing tree/canvas guards remain in place. Inputs are not silently applied.

Use Apply Properties to commit the inputs, or restore their original values to
continue without committing. No draft project-format changes were introduced.

## Validation and limits

Fresh final build succeeded. The console core suite still passes **228 checks,
zero failures** (Core-Tests.txt). Source review verified guards precede model or
file mutation and selector restoration suppresses selection callbacks.

These are interface lifecycle changes. The core suite does not exercise the
new GUI event paths. No UI smoke suite, editor GUI, client launch, Computer Use
or capture was run. Do not treat the core count as successful GUI validation.
The following manual checks remain pending under the automation suspension:

1. Edit a field without Apply; verify the title gains an unsaved marker.
2. Attempt page/theme switching; verify the previous selectors and input remain.
3. Attempt Save/Export; verify no output is written and the input remains.
4. Attempt Undo, Duplicate, navigation-mode and protection changes; verify the
   model and input remain unchanged.
5. Apply valid values; verify normal actions resume. Test invalid input and a
   protected binding too, without losing typed text.
6. Close with pending input; declining discard must preserve the window/input.

DirectX replacement and Windows automation remain deferred/suspended.
