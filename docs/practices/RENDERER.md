# The renderer (Faultline.Web)

Read this when touching the renderer's structure.

- Blazor WASM, minimal. State flow: hold current `GameState`, send `Command`, receive `StepResult`, animate `Events` in order, then render `NewState`.
- Rendering is a pure function of state + a queue of events. No game state lives in components.
- Ugly is fine; wrong is not. Placeholder colored squares and text labels until M6. Intent telegraphs (enemy plans, crack markers, push previews) are NOT polish — they are rules-critical UI and ship with their milestone.
- Show the push preview: hovering an ability shows destination + collision/spike/pit outcome, sourced from a Core query (`PreviewDisplacement`), never computed in JS/Blazor.
