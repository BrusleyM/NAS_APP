# NAS Showroom — Unity AR car-showroom app

Unity project. UI is built with UI Toolkit (UXML/USS), not uGUI. Source lives under
`Assets/Scripts/`, UI documents under `Assets/UI Toolkit/UI Docs/`.

## Architecture

**Event-driven, not direct references.** Controllers never hold references to each
other. Everything communicates through a static, type-safe pub/sub bus:

- `Assets/Scripts/Core/Events/EventBus.cs` — `EventBus.Subscribe<T>/Unsubscribe<T>/Publish<T>`
- `Assets/Scripts/Core/Events/GameEvents.cs` — all event payload structs
  (`LoginRequestedEvent`, `AuthSucceededEvent`, `AuthFailedEvent`, `CarSelectedEvent`, etc.)

Always pair `Subscribe` in `OnEnable` with `Unsubscribe` in `OnDisable`, or destroyed
MonoBehaviours keep receiving events.

**TODO, not urgent — known deviation from the above:** `GameManager.Instance`
is already referenced directly (not via events) by several scripts —
`ParentPageController`, `EstimatorCardController`, `AuthController` (reads
`CurrentEnvironment`), and a couple of storage test scripts. This predates
the event-driven principle and keeps being extended rather than fixed.
Moving all of these to go through `EventBus` instead is a real future goal,
but should happen as one deliberate, dedicated pass across every call site —
not piecemeal, one at a time, whenever a new feature happens to touch
`GameManager`.

**Screen flow (current order):** Auth → car selection → AR placement →
affordability calculator/estimator. `ParentPageController` is a pure
router — it holds `VisualTreeAsset` references for each screen (assigned in
the Inspector) and swaps `_cardContainer`'s content in response to events. It
does not know how login/register/auth work, only which screen to show next.
Card controllers (`LoginCardController`, `RegisterCardController`,
`CarSelectionScreenController`, `EstimatorCardController`) are added
dynamically via `gameObject.AddComponent<T>()` — they are never pre-placed
in the scene, so they have no Inspector to drag references into.

**Important gotcha:** `AddComponent<T>()` runs `OnEnable` synchronously before the
calling code gets a chance to hand anything to the new component. Any controller that
needs data from its creator (e.g. `CarSelectionScreenController` needs a
`VisualTreeAsset` for the card template) uses an explicit `Initialize(...)` method
called right after `AddComponent`, with `OnEnable` doing only DOM-querying/event
wiring that doesn't depend on that data.

## Auth (`Assets/Scripts/Core/Auth/`)

`AuthController` is **not** a mock — it's backed by a real API layer
(`ICustomerAuthApi`/`CustomerAuthApi`, `NAS.Core.Networking`, `ApiResult<T>`,
`AuthSession`, `TokenStorage`). It validates client-side first (regex/length checks
matching the Figma-derived React reference in `mobile-designs.tsx`), publishing
per-field errors via `AuthFailedEvent.FieldErrors` (a `Dictionary<string,string>`
keyed by field name: `email`, `password`, `firstName`, `lastName`, `cellNumber`,
`confirmPassword`) *before* ever calling the real API. `AuthFailedEvent.Reason` is
for general/backend-level failures instead (wrong credentials, network error) —
`FieldErrors` is null in that case, `Reason` is null when `FieldErrors` is set.

`LoginCardController`/`RegisterCardController` show these via a `.input-field--error`
class + per-field `Label`s (`email-error-label`, `password-error-label`, etc.) —
shared styling lives in `Assets/UI Toolkit/UI Docs/Styles/LoginScreen.uss`.

**API base URL is a project-wide environment setting, not hardcoded and not
owned by AuthController.** `GameManager` (`Assets/Scripts/Core/GameManager.cs`,
`DontDestroyOnLoad` singleton, `GameManager.Instance`) has
`_environment : AppEnvironment` (`Assets/Scripts/Core/AppEnvironment.cs` —
`Local` or `ApiDomain`), exposed as `CurrentEnvironment`. `AuthController`
reads it in **`Start()`, not `Awake()`** — Unity doesn't guarantee `Awake()`
order across different GameObjects, and `GameManager.Instance` must already
be set; `Start()` is guaranteed to run only after every object's `Awake()`
has — and picks between `_apiSettings` (Local, `http://localhost:5080`) and
`_apiDomainSettings` (ApiDomain, `ApiSettings.ApiDomain.asset`,
`https://api.nas.test:8443` — the optional `NAS_Backend/nginx/` proxy). The
same `CurrentEnvironment` value also decides `trustAnyCertificate`, passed
explicitly through `CustomerAuthApi`'s and `ApiClient`'s constructors (see
the cert gotcha below) — **keep this a single source of truth for both
concerns.** A design with two independently-toggleable flags (one for which
`ApiSettings` to use, one for cert trust) will eventually drift out of sync,
since nothing stops either one being flipped without the other. **The
committed default is `AppEnvironment.Local`, and it must stay that way** —
`ApiDomain` only works on a machine with the nginx/mkcert/`/etc/hosts` setup
from `NAS_Backend/nginx/README.md` already running; on any other machine it
makes auth fail silently. If you find this set to `ApiDomain` in a diff that
isn't explicitly about device HTTPS testing, that's very likely an
accidental commit — flip it back. See the README's "Optional: HTTPS for
testing on a physical device" section for the full explanation.

**Recurring gotcha: adding a cross-folder script reference can compile
"clean" right up until it doesn't.** This project uses one `.asmdef` per
`Core/*` subfolder (`NAS.Core`, `NAS.Core.Auth`, `NAS.Core.Networking`,
etc. — run `find Assets/Scripts -iname "*.asmdef"` to see them all). Adding
a `using` for a type in a different folder is not enough — the *assembly*
also needs to reference the other assembly, or it's a compile error
(`CS0103: The name 'X' does not exist in the current context`), same as any
missing reference. The trap: Unity's incremental compiler can hand back a
transient "0 errors" / successful-reflection-lookup result for a beat after
an edit like this, using a stale cached assembly, before a later trigger
(e.g. entering Play mode, which forces a real check) surfaces the actual
error. **Don't trust an early "looks clean" read as final** after adding a
reference to a type in a different `Core/*` folder — confirm by actually
entering Play mode (which Unity refuses outright if there's a real compile
error) before relying on the change. Fix for a missing reference: add the
target assembly's GUID (from its `.asmdef.meta`) to the source assembly's
`"references"` array in its own `.asmdef`.

## Car selection carousel (`Assets/Scripts/UI Docs/Components/`, controller in `.../Controllers/`)

Shows **one car at a time**, drag/swipe only (no buttons), with object pooling.
`CarPager` and `CarCardView` live in `Assets/Scripts/UI Docs/Components/`;
`CarSelectionScreenController` lives in `Assets/Scripts/UI Docs/Controllers/`
— a different folder, despite all three being part of the same feature. Don't
assume "Components/" for a class just because related classes are there;
verify per-file if it matters for what you're doing.

- `CarPager` — owns a fixed 3-slot pool (`SlotCount = 3`, `CenterSlot = 1`).
  `RefreshCards()` never creates/destroys elements after setup, only rebinds data
  around the current index. `CanGoPrevious`/`CanGoNext` guard the boundaries.
- `CarCardView` — binds one cloned `CarCard.uxml` instance to a `CarData`.
  **Uses `visibility` (not `display`) to hide an empty boundary slot** — `display:none`
  removes the element from flex layout entirely, which breaks the 3-slot symmetry
  `justify-content:center` relies on to keep the current card centered.
- `CarSelectionScreenController` (`Controllers/`) drives the drag: `PointerDown` (capture pointer) →
  `PointerMove` (clamped `translate` on `_carsContainer`, follows the finger 1:1) →
  `PointerUp` (25%-of-viewport-width threshold decides commit vs. snap back;
  either way, snaps `translate` back to `0` — no easing, this is intentional).
- `CarData` (`Assets/Scripts/Core/Models/CarData.cs`) is a `ScriptableObject`, but
  there are no more hand-authored `.asset` fixtures — `Assets/Resources/Cars/` is
  gone. Every `CarData` instance is created at runtime by
  `VehicleDtoMapper.ToCarData()` (`Core/Vehicles/VehicleDtoMapper.cs`) from the
  customer vehicle API response. `CarPager`, `CarCardView`, and
  `CarSelectionScreenController` only ever interact with the `CarData` *type* —
  they don't know or care that it's API-sourced, which is what made this migration
  low-risk when it happened.

## Vehicle catalog: real 3D models, not just names (`Assets/Prefabs/Cars/`)

The catalog is 11 vehicles named `<Make/Model> DEMO` (e.g. `BMW M8 DEMO`) — the
`DEMO` suffix is deliberate on both the DB row and the Tigris object key, so
nothing in the shipped app reads as an actual partnership with the real
manufacturers. Backing 3D models live in `Assets/Models/cars/*.glb` (glTFast
imports), each wrapped by a corrective prefab in `Assets/Prefabs/Cars/` (e.g.
`BMWM8Demo.prefab`) — **never place a raw imported `.glb` directly**, always go
through its prefab.

**Why the wrapper prefabs exist — two real bugs found in the raw imports:**
1. **Scale.** Several of these Sketchfab-sourced `.glb` files import at wildly
   wrong scale (some ~100x too small, some ~5-10x too big) — glTF is spec'd in
   meters so this is baked into the source file, not a Unity import setting.
   Measure with combined `Renderer.bounds` across all child renderers (a
   *median-filtered* max-dimension check catches stray artifact meshes — e.g.
   `bmw_x6m.glb` had one tiny orphaned mesh sitting 7+ units from the car body
   that blew out the naive combined bounds to 10m) and compare against the
   vehicle's real-world length before trusting either the raw bounds or a
   "looks fine" visual at an arbitrary zoom level.
2. **Orientation and pivot.** All 11 source models import standing on their
   nose (length along Unity's +Y, not forward/Z) and with their pivot **not**
   at the vehicle's centered, ground-level point — a couple were off by
   several meters on X/Z, and one sat with its lowest point 0.6m below Y=0.
   Each prefab's `Model` child carries a corrective `localRotation` (-90° X,
   plus a further 90° Y for the one model whose length ended up on X instead
   of Z) and `localPosition` shift so that at rest: the car lies flat, length
   runs along +Z, height along +Y, and the prefab's own root pivot sits
   exactly at the car's centered, ground-touching point — required for
   `Instantiate(prefab, hitPose.position, hitPose.rotation)`-style AR
   placement to put the car where the user actually tapped, sitting on the
   ground, not floating/sunk/offset. If a new car model is ever added this
   way, verify scale + orientation + pivot the same way before trusting it —
   this was found the hard way, mid-session, on this exact batch.

**Not yet wired up:** each vehicle's Tigris-hosted AssetBundle (built via
`BuildPipeline.BuildAssetBundles`, bundle name `carmodels/<key>`, e.g.
`carmodels/bmwm8demo`) is uploaded and the DB's `vehicle_model.tigris_model_key`
column records which bundle belongs to which row — but nothing in the AR scene
actually downloads and instantiates it yet. `ObjectPlacerController`
(`Assets/Scripts/Core/ObjectPlacerController.cs`, on the "ar man" GameObject in
`AR Scene.unity`) still places a single fixed `raycastPrefab` regardless of
which car was selected. Wiring `GameManager.SelectedCar` through to a real
per-car download (mirroring `AssetBundleTest.cs`'s
download-bundle-then-`LoadAsset<GameObject>` pattern, but driven by the
selected vehicle's `tigrisModelKey` instead of a hardcoded key) is the natural
next step — not built yet, don't assume it exists.

**`AssetBundleTest.cs`** (same "asset manager" GameObject) is a manual dev/test
script, not production code — it's currently disabled (`m_Enabled: 0` in
`AR Scene.unity`) because its `Start()` does a full upload/download/instantiate
cycle every time the scene runs, which raced `GameManager.Instance`
initialization and threw on a cold Play-mode entry straight into `AR Scene`
(harmless in the real app flow, where `GameManager` is always already alive by
the time this scene loads — but still worth leaving off). Re-enable only for
deliberate manual testing.

## AR viewport screen

Design source: `ARViewportScreen` in `mobile-designs.tsx` (the code-bundle export
lives outside this repo, under `~/Downloads/NAS Showroom/src/app/components/` when
last checked — re-locate it via that folder name if it's moved).

**Important architectural correction vs. what this doc used to say:** the AR
viewport is a **real separate Unity scene** (`Assets/Scenes/AR Scene.unity`, with
its own `AR Session`/`XR Origin`/AR Foundation setup, already in
`ProjectSettings/EditorBuildSettings.asset`), not another UI Toolkit card added
dynamically via `AddComponent<T>()` into `ParentPageController`'s `_cardContainer`
the way Login/CarSelection/Estimator are. `Main App.unity` is also now registered
in Build Settings (it wasn't before this was built — required for
`SceneManager.LoadScene(string)` to resolve it by name).

Screen flow, as actually wired:
- `CarSelectionScreenController.OnStartARClicked` publishes `CarSelectedEvent`
  (`GameManager.SelectedCar` updates from it, including `VehicleInfo.id`, which
  survives the scene load since `GameManager` is `DontDestroyOnLoad`).
- `ParentPageController.OnCarSelected` calls `SceneManager.LoadScene("AR Scene")`
  — no card, no `ShowArViewportCard()`.
- `AR Scene.unity` has a pre-placed `AR UI` GameObject (`UIDocument` + a
  scene-placed, **not** `AddComponent`-dynamic, `ArViewportController`) —
  `Assets/UI Toolkit/UI Docs/ArViewportScreen.uxml` /
  `Styles/ArViewportScreen.uss`. `ArViewportController.OnEnable` reads
  `GameManager.Instance.SelectedCar.modelName` straight into the car-name label.
- **Back button** → `SceneManager.LoadScene("Main App")` with no event published.
  Back in `Main App.unity`, `ParentPageController.DecideInitialScreen()` sees
  `CurrentUser`+`SelectedCar` already set and `ReturnToEstimator` false, so it
  calls `ShowCarSelectionScreen()` — which restores the previously selected car's
  position via `CarSelectionScreenController`'s index-restore logic (matches
  `_filteredCars` against `GameManager.SelectedCar.id`, falls back to index 0 if
  not found).
- **Confirm (checkmark) button** → publishes `ReturnToEstimatorRequestedEvent`
  (the same event `GameManager` already listened for, previously only used when
  backing out of an in-progress estimate) then loads `Main App`.
  `DecideInitialScreen()` sees `ReturnToEstimator` true and calls
  `ShowEstimatorCard()` instead, resetting the flag.

**Built so far:** top bar (Back button, centered car name label, circular cyan
Confirm/checkmark button), the two gesture-hint text labels ("Swipe to rotate" /
"Pinch to scale", bottom-left, non-interactive — `picking-mode="Ignore"` so they
don't block AR touch gestures), and a bottom-center Settings button that opens a
placeholder "Customize" bottom sheet (drag handle, header with close button, just
a "Coming soon" label — tapping the backdrop or the close button closes it).
Colors match the Figma spec (`#C0C0C0` neutral, `#00D4FF` active/accent) but
`box-shadow` glow wasn't reproduced (UI Toolkit's USS support for it wasn't used
here — check current Unity version support before assuming it's unavailable).

**Not yet built:** the camera-feed gradient scrim, and the real contents of the
"Customize" sheet (the Wheel/Paint/Trims/Dashboard 4-item grid, Paint's 5 color
swatches, the other three categories' text-choice rows) — the sheet currently
opens/closes but shows only placeholder text. Deliberately deferred — there's no
customer-facing data model yet for which options are actually configurable per car
(see the "Planned: API-driven vehicle catalog" section above; trim/color/interior/
wheel data was explicitly scoped out of the car-selection API work). Build this
grid once that data exists, following the
same pattern as the top bar (scene-placed controller in `AR Scene.unity`, not
`AddComponent`-dynamic — this scene only ever shows one screen, so it doesn't need
`ParentPageController`'s router pattern).

## Recurring gotcha: inline UXML styles silently override USS class rules

This has caused real, hard-to-spot bugs multiple times in this project. Inline
`style="..."` attributes on a UXML element always beat a USS class rule of the same
specificity, even a rule added later. Before changing a USS class to fix a visual
issue, **check the UXML for an inline style on that same element** — if the element
already has an inline value for the property being changed, editing only the USS
does nothing.

## Recurring gotcha: script GUIDs

Never delete-and-recreate a `.cs` file to make a large change — deleting changes the
file's GUID, and any Inspector-serialized reference to that script (component on a
GameObject, prefab) breaks ("Missing Script"). Edit files in place. Most controllers
in this project are added via `AddComponent<T>()` in code rather than serialized in
the Inspector, so this specifically matters for anything that *is* Inspector-wired
(e.g. `ParentPageController`'s `VisualTreeAsset` fields).

## Tigris storage

`GameManager` owns an `IStorageService` (`DevStorageService`) for uploading/
downloading 3D models by string key (`UploadModel`, `DownloadModel`, etc.) — this is
for **3D AR assets only**, not 2D catalog thumbnails. There's also a second,
separate manual-test path to the same bucket: `TigrisStorageManager` (its own S3
client, own `UploadFileAsync`/`DownloadObjectAsync`) on the "asset manager"
GameObject in `AR Scene.unity` — used directly (not through `IStorageService`) to
upload the 11 demo car AssetBundles. **If you drive `TigrisStorageManager` (or any
`Task`-returning upload/download call) from an Editor script via
`execute_code`-style reflection, never block on it with
`.GetAwaiter().GetResult()` in a loop** — this deadlocked Unity's main thread mid-
session after the first of 11 uploads (killed the Editor's responsiveness to
Stop/console entirely; only a lightweight ping-style check still answered). Fire
each call without awaiting it in the same script invocation, or drive it through a
coroutine, and verify completion out-of-band (e.g. list the bucket) instead of
blocking in-process.

`vehicle_model.tigris_model_key` (backend, nullable `varchar(200)`) is where each
row's Tigris object key lives — see "Vehicle catalog: real 3D models" above for the
current state of that join (uploaded and recorded in the DB, not yet consumed by
Unity for actual AR placement).

### TODO, not urgent — Tigris environment handling doesn't match Auth's pattern

`GameManager._useProduction` (the other field under its `[Header("Environment")]`,
alongside `_environment`/`AppEnvironment`) is currently **dead** — `InitializeStorage()`
always constructs `DevStorageService` regardless of its value; the bool only
feeds a `Debug.Log` line, so flipping it to `true` gets you a log claiming
"Using PRODUCTION storage" while Dev storage silently keeps running underneath.

**Deliberately not fixed now** — there's no plan for multiple Tigris storage
environments at this stage of the project. Revisit this when AR integration
starts (the AR placement step in the screen flow above, i.e. once vehicles
actually need real downloaded 3D models rather than just placeholder testing).
At that point, follow `AuthController`'s pattern: one enum on `GameManager`
as the single source of truth, not a second independent bool that can drift
(see the Auth section above for why that matters).

## Design reference

`mobile-designs.tsx` (React/Tailwind) is the code-bundle export of the actual Figma
file ("NAS Showroom") and is the source of truth for exact copy, validation rules,
and layout intent where the Unity build is ambiguous or hasn't caught up yet. Notable
places the Unity build has intentionally diverged from it: car selection is a
swipe-carousel here vs. prev/next buttons there (deliberate choice, not a gap).
