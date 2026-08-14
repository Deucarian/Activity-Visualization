# Changelog

All notable changes to this package are documented here.

## [Unreleased]

### Changed

- Removed the speculative nested Step contract. The package now models the
  proven flat Activity-to-model-membership domain only.
- Reject null membership collections; callers must explicitly provide an empty
  collection for an Activity with no model members.

### Fixed

- Canonicalize identifiers before membership and model-index deduplication.

## [0.1.0] - 2026-08-13

### Added

- Backend-neutral Activity and model membership contracts.
- Deterministic full-replacement visibility planning.
- Revision-aware authoritative preview and selection state.
- Required/optional missing-member policy and structured outcomes.
- Pluggable baseline and model visibility strategies.
- Automatic diagnostics registration with idempotent disposal.
- EditMode contract, lifecycle, ordering, and state-transition tests.
- Compiled explicit-composition sample.

### Fixed

- Serialize synchronous reentrant commands and coalesce them to the newest
  revision without allowing an older apply to overwrite authoritative state.
- Reconcile equivalent plans through idempotent adapters so external model
  visibility drift is repaired on the next accepted command.
