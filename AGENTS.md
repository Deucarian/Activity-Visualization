# Deucarian Activity Visualization Agent Notes

Package ID: `com.deucarian.activity-visualization`

Repository: `Deucarian/Activity-Visualization`

Follow the canonical Deucarian Package Registry architecture rules.

## Ownership

This package owns normalized Activity/Step model membership, deterministic
visibility planning, baseline policy, revision ordering, and authoritative
preview/selection visibility state.

Registered capability: `activity-model-visualization`.

This package must not own backend DTOs, model loading, camera navigation, input,
command transports, browser code, persistence, or Unity object lookup policy.

## Policies

- Keep normalized contracts backend-neutral.
- Keep planning pure and side effects behind `IModelVisibilityController`.
- One `ActivityVisualizationStateOwner` owns the active preview/selection state.
- Never introduce camera/navigation references; selection changes visibility only.
- Use Deucarian Logging and Diagnostics; no direct Unity `Debug`.
- Keep production files below 500 lines.
- Dispose diagnostics registration idempotently.

## Validation

```powershell
python ../package-registry/Tools/deucarian_package_validator.py --registry-root ../package-registry --repository-root . --config deucarian-package.json
```

Run Unity EditMode tests and `git diff --check` before committing.
