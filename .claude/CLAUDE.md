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
- `CarData` (`Assets/Scripts/Core/Models/CarData.cs`) is a `ScriptableObject`,
  currently loaded via `Resources.LoadAll<CarData>("Cars")` from
  `Assets/Resources/Cars/`. **As of last check, only 1 of a planned 7 cars actually
  exists as an asset** (`TeslaModelS`) — confirm current state before assuming the
  catalog is complete, and don't be surprised if the count has changed.

### Planned: API-driven vehicle catalog (not yet built)

`Resources.LoadAll` is a known-temporary stepping stone, not the intended long-term
source of the car list. The plan is a real vehicles table in a database, fetched
through an API — following the same pattern as `ICustomerAuthApi`/`CustomerAuthApi`
in `Core/Auth` (e.g. an `IVehicleCatalogApi` returning `ApiResult<List<VehicleDto>>`).

**The seam that makes this low-risk:** `CarPager`, `CarCardView`, and
`CarSelectionScreenController` only ever interact with the `CarData` *type* — none of
them know or care that it currently comes from `Resources.LoadAll`. When the API
exists, the swap is: fetch DTOs, call `ScriptableObject.CreateInstance<CarData>()`
per result, copy fields over, hand the list to `CarPager.SetCars(...)` exactly as
today. No changes needed to pooling, drag/swipe, or card rendering.

**Before doing that migration, check whether it's still current** — if the vehicles
API now exists, treat everything above about `Resources.LoadAll`/`Assets/Resources/Cars`
as historical, not current architecture, and look for the actual `IVehicleCatalogApi`
(or equivalent) instead.

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
for **3D AR assets only**, not 2D catalog thumbnails. If `CarData` gains a
Tigris-backed model reference, the natural field name is something like
`tigrisModelKey : string`, matching the existing `DownloadModel(string modelKey)`
signature.

Once the planned vehicles DB table (see above) exists, each row is the natural place
to store that `tigrisModelKey` alongside the car's other fields — the DB becomes the
join point between "which car" and "which 3D model," rather than Tigris and the DB
being two unrelated systems Unity has to reconcile itself.

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
