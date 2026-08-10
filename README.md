# NEO AR Showroom

NEO AR Showroom is a Unity mobile application for exploring a dealership's
vehicles in augmented reality. Customers can browse a vehicle catalogue, build
an estimate, place a selected vehicle in AR, and ultimately save configurations
or submit a lead to a dealership.

The companion API lives in the sibling `NAS_Backend` project. It is an ASP.NET
Core 10 Web API backed by PostgreSQL; it owns customer accounts, dealership
data, vehicle data, saved configurations, and leads.

## Current capabilities

- Browse locally authored vehicle catalogue entries.
- Filter and search vehicles, select one, and begin the AR flow.
- Place and interact with vehicles using Unity AR Foundation (ARCore on Android,
  ARKit on iOS).
- Calculate affordability estimates locally.
- Display login and registration screens through an event-driven UI flow.

Authentication in the Unity client is currently a development stub: it validates
basic input and publishes success locally. It is deliberately the seam where the
real backend integration will be added; it does not yet call the API.

## Architecture

The Unity app keeps presentation, flow, and reusable domain code separate:

```
Assets/Scripts/
  Core/           App state, domain models, events, AR flow and business services
  UI Docs/        UI Toolkit screen controllers and reusable UI components
  Storage/        Asset storage abstractions and S3-backed implementations
  Configurations/ Environment-specific storage configuration
  AR Scripts/     Input and AR raycast/placement adapters
```

UI controllers publish intent events such as `LoginRequestedEvent`,
`CarSelectedEvent`, and `EstimateSubmittedEvent`. Controllers such as
`AuthController` handle those events and publish success or failure events;
`GameManager` observes the resulting state changes. This keeps the UI independent
of API, AR, and storage implementation details.

Vehicle catalogue items are `CarData` ScriptableObjects stored in
`Assets/Resources/Cars`. They are loaded by the catalogue screen at runtime.

## Backend API integration plan

The intended client boundary is a small API layer, rather than HTTP calls from
UI controllers:

1. Add API DTOs and an `IApiClient`/feature services (for example,
   `ICustomerAuthApi`, `IVehicleApi`, and `ILeadApi`) under `Core`.
2. Implement those services with `UnityWebRequest`, using a development and a
   production API base URL supplied by a configuration asset. Keep API URLs and
   non-secret settings out of UI code.
3. Replace the stub logic in `AuthController` with calls to
   `POST /api/customer/auth/register` and `POST /api/customer/auth/login`.
   Map the response into the local `User` model, persist the access token using
   an appropriate platform-secure store, then publish the existing
   `AuthSucceededEvent` or `AuthFailedEvent`.
4. Have authenticated feature services attach `Authorization: Bearer <token>`.
   On a 401 response, clear the session and notify the UI to return to login.
5. Introduce vehicle catalogue, saved-configuration, and lead API calls as their
   backend endpoints become available. Existing selection and estimate events
   provide natural hand-off points for those services.

The current backend implements customer authentication at:

- `POST /api/customer/auth/register`
- `POST /api/customer/auth/login`

It also has a separate invite-only staff authentication flow for dealership
dashboards. That staff flow should not be part of the customer-facing AR app
unless a future staff/kiosk mode explicitly requires it.

## Prerequisites

- Unity **6.3** (the project currently uses `6000.3.9f1`)
- Android Build Support for Android development; an ARCore-compatible device for
  on-device AR tests
- iOS Build Support and an ARKit-compatible device for iOS development
- The `NAS_Backend` project and PostgreSQL when testing real API integration

## Getting started

1. Open this folder in Unity Hub using the supported Unity version.
2. Allow Unity to resolve packages and import assets.
3. Open the main scene and run it in the Editor for UI flow work.
4. Switch the build target to Android or iOS to test AR on a supported device.
5. When API integration begins, run the sibling backend locally and configure the
   client base URL for the target environment.

## Key packages

- Unity AR Foundation, ARCore, and ARKit
- Unity Input System and UI Toolkit
- Universal Render Pipeline
- AWS SDK-backed storage services for model assets

## Related project

The backend source is located at:

`/Users/neoxr/Documents/Projects/NAS_Backend`

Its README describes the PostgreSQL setup, migrations, and available API
endpoints.
