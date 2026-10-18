# Project conventions

- Use English for new project names, filenames, documentation, code comments,
  application text, commit messages, issues and pull requests unless the user
  explicitly requests another language.
- Communicate with the user in German unless requested otherwise.
- Preserve intentional multilingual test fixtures and external MediaPortal
  schema identifiers; do not translate test data if that changes the behavior being tested.
- Keep this repository private unless the user explicitly changes its audience.
- Commit and upload completed development milestones, and keep STATUS.md current.
- Distinguish historical test reports from newly executed validation.
- Use isolated skin copies and preserve original skin files.
- Windows app automation is suspended after the owner reported a system freeze
  requiring a hard reset during the last attempt. Continue with source code and
  console/model tests only. Do not launch MediaPortal or the editor GUI, capture
  windows, or retry Computer Use unless the owner explicitly reauthorizes that
  activity after this incident. DirectX replacement remains deferred.

- Start future projects in English with a private GitHub repository from the beginning,
  and record their current state and remaining work there.
