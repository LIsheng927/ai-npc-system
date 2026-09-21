# Project maintenance

- Keep changes small and preserve the runnable purchase-and-delivery demo.
- Before each GitHub upload, remove dead code, repeated explanations, tutorial placeholders and generic generated prose. Keep comments that explain rules or non-obvious decisions.
- Use consistent UTF-8 encoding and readable names. Keep the implementation approachable for a developer learning Python and Unity.
- Describe actual behavior and validation in README and commit messages. Do not invent features, performance results or test coverage.
- Do not commit credentials, machine-specific paths, Unity caches, logs or local settings.
- Preserve Unity .meta files and scene references. Never replace serialized field names without migrating the scene.
- Run the backend tests for protocol changes. For gameplay or scene changes, run DemoValidation and check Unity compilation. Report any verification that could not be run.
- Preserve repository history. Publish ordinary incremental commits; do not force-push unless explicitly requested.
