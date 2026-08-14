# Activity Visibility Basics

Add `ActivityVisibilitySample` to a GameObject and assign three model objects.
The component's context-menu actions demonstrate flat Activity selection and
clearing back to the show-all baseline.

The sample intentionally keeps model lookup and Unity object mutation in one
small adapter. The package state owner and planner never receive a camera,
navigation service, backend DTO, or `GameObject`.
