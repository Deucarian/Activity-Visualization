# Deucarian Activity Visualization

`com.deucarian.activity-visualization` turns normalized Activity or Step model
membership into deterministic, full-replacement visibility plans. One
revision-aware state owner coordinates preview snapshots, selection, clearing,
diagnostics, and a narrow model-specific visibility adapter.

Current package version: `0.1.0`.

## Ownership

This package owns normalized Activity/Step visibility contracts, pure planning,
revision ordering, baseline strategy selection, and authoritative applied
visibility state. It does not own backend DTOs, model loading, command routing,
camera navigation, Unity input, browser transport, or persistence.

The deliberate absence of any camera/navigation dependency means selecting or
editing an Activity cannot pan, zoom, reframe, or change navigation mode.

## Install

Stable after publication:

```json
"com.deucarian.activity-visualization": "https://github.com/Deucarian/Activity-Visualization.git#main"
```

Development after publication:

```json
"com.deucarian.activity-visualization": "https://github.com/Deucarian/Activity-Visualization.git#develop"
```

For local development, use a `file:` dependency. Git URLs above are publication
targets, not a claim that the repository already exists remotely.

Dependencies:

- `com.deucarian.diagnostics` `0.1.4`
- `com.deucarian.logging` `1.0.2`

Unity 2021.3 or newer is required.

## Composition

Normalize application data at the boundary, then compose the state owner with
an immutable index and one batch visibility adapter:

```csharp
IModelElementIndex index = new ModelElementIndex(modelElementIds);
IModelVisibilityController controller = new MyModelVisibilityController();

var state = new ActivityVisualizationStateOwner(
    index,
    controller,
    new ActivityVisibilityPlanner(),
    new ShowAllBaselineVisibilityStrategy());

state.ReplacePreview(normalizedSnapshot); // revision 10
state.Select(ActivitySelection.ForStep("activity-7", "step-2"), 11);
state.Clear(12);
```

Dispose the state owner when the model/viewer is unloaded. The supplied index,
controller, planner, and baseline strategy remain caller-owned.

## Behavioral contract

- All commands share one strictly increasing, non-negative revision sequence.
- Older or repeated revisions are rejected without changing visibility.
- A preview snapshot is complete and contains visibility-relevant data only.
- A selected Activity uses its exact members.
- A selected Step uses its exact Step members; it does not implicitly inherit
  its parent Activity members.
- Plans assign every indexed model element exactly once in identifier order.
- Required missing members reject the candidate plan. Optional missing members
  produce diagnostics and the remaining plan is applied.
- Invalid selections and failed applications preserve the last valid applied
  selection and plan while still advancing the accepted revision.
- Every accepted plan is reconciled through the model adapter. Adapters skip
  already-correct targets, so equivalent plans do no writes while repairing
  visibility changed by another model system between commands.
- A synchronous command raised while an adapter is applying is serialized.
  Only the newest reentrant candidate is drained before the outer command
  returns; the outer result reports `Superseded` and the nested call reports
  `Queued` rather than exposing an older plan as authoritative.
- Clearing delegates to `IBaselineVisibilityStrategy`; show-all is only the
  default. Consumers such as HoloHelmet can reapply design-option visibility.
- The state is temporary. This package never persists Activity edits.

`IModelVisibilityController.Apply` receives a full replacement plan. Adapters
should resolve/validate all targets before mutation so a missing target cannot
leave a half-applied state. They should skip already-correct targets and return
whether reconciliation actually changed model visibility.

## Public API map

- `ModelElementId`: backend-neutral scheme/value identity.
- `ModelElementMember`: membership plus required/optional policy.
- `ActivityVisibilityDefinition`: Activity members and explicit Steps.
- `ActivityStepVisibilityDefinition`: exact Step membership.
- `ActivityPreviewSnapshot`: complete normalized preview and revision.
- `ActivitySelection`: stable Activity or Step selection.
- `IModelElementIndex` / `ModelElementIndex`: immutable visible-target index.
- `IActivityVisibilityPlanner` / `ActivityVisibilityPlanner`: pure planning.
- `VisibilityPlan`: deterministic full replacement assignment set.
- `IModelVisibilityController`: narrow side-effect boundary.
- `IBaselineVisibilityStrategy`: application-specific clear behavior.
- `ShowAllBaselineVisibilityStrategy`: default clear behavior.
- `ActivityVisualizationStateOwner`: authoritative preview/selection state.
- `IActivityVisualizationCommands`: write surface for command handlers.
- `IReadOnlyActivityVisualizationState`: observation/diagnostic surface.

## Donor semantics

The initial boundary was proven against HoloHelmet's current Activity behavior:
selecting an Activity hides the indexed model then enables that Activity's
direct model members; clear restores configured display. HoloHelmet does not
currently contain Step membership, so Step definitions are explicit normalized
input owned by the integrating application rather than inferred here.

## Validation

Run the package validator, Unity EditMode tests, and whitespace validation:

```powershell
python ../package-registry/Tools/deucarian_package_validator.py --registry-root ../package-registry --repository-root . --config deucarian-package.json
git diff --check
```
