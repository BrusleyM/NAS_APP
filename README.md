# NEO AR Showroom

[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Build Status](https://img.shields.io/travis/BrusleyM/NAS_APP.svg)](https://travis-ci.org/BrusleyM/NAS_APP)

An **Augmented Reality Car Showroom** designed for automotive dealerships and their customers. Experience full‑scale 3D vehicles in your real environment, customize trims and colors in real time, and get an instant, non‑binding monthly payment estimate—all without sharing sensitive personal data. This app turns a smartphone into a powerful sales tool, generating warm, pre‑qualified leads for dealerships.

---

## Features

- **Full‑scale AR vehicle placement** – Life‑sized 3D models anchored on any flat surface using auto surface detection.
- **Real‑time customization** – Change paint, wheels, interiors, and trims on the fly.
- **Affordability estimator** – See estimated monthly payments based on deposit, trade‑in, and term (no credit check, no ID required).
- **Interior exploration** – Immerse yourself in the cabin from the driver’s seat.
- **Save configurations** – Build a personal “virtual garage” of favorite builds.
- **Lead submission** – Submit your name, configuration, and estimate directly to the dealership as a warm lead.
- **Dealership white‑label** – Fully branded as the dealer’s own app, with analytics dashboard and inventory management.

---

## Tech Stack

- **AR Framework** – AR Foundation (ARCore for Android, ARKit for iOS)
- **Game Engine** – **Unity 6.3** (or newer)
- **Backend & Cloud** – AWS S3 for asset storage, CloudFront CDN for fast delivery, custom REST API (Node.js / Python) for lead capture and configuration sync
- **Mobile Platforms** – Android (primary, ARCore‑ready), iOS (planned, ARKit)
- **Additional Libraries** – Unity’s Addressable Assets system, Newtonsoft.Json for API serialization

---

## Prerequisites

Before you begin, ensure you have the following installed:

- **Unity Hub** and **Unity 6.3** (or later) with Android Build Support (including SDK, NDK, JDK)
- **Git**
- An **ARCore‑compatible Android device** (see [Google’s list](https://developers.google.com/ar/discover/supported-devices)) or an iOS device with ARKit support for testing
- (Optional) **Android Studio** for logcat debugging

---

## Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/BrusleyM/NAS_APP.git
   cd NAS_APP

Open the project in Unity

Launch Unity Hub, click Open, and select the cloned folder.
Unity will import all assets and resolve dependencies.
Configure platform settings

Go to File → Build Settings.
Switch platform to Android (or iOS if building for iOS).
Ensure the Texture Compression is set to ASTC (recommended for AR apps).
Set up backend API keys (if required)

Create a file Assets/Resources/APIConfig.json (this file is ignored by Git).
Add your API endpoint and any necessary keys:

json
{
  "leadApiUrl": "https://your-dealership-api.com/submit",
  "assetBaseUrl": "https://your-cdn.cloudfront.net/"
}
For security, never commit real keys. Use the provided template APIConfig.json.example.
Build and run

Connect your ARCore‑compatible device via USB and enable USB Debugging.
In Unity, click File → Build and Run.
The app will be installed and launched on your device.
Usage

Basic Interaction

Point your camera at a flat surface (floor, table) – the app will detect it and show a placement grid.
Tap to place the vehicle at full scale.
Use on‑screen buttons to change colors, wheels, or trim.
Swipe to rotate the view, pinch to zoom.
Tap the “Estimate” button to open the affordability calculator.
After configuring, tap “Submit Lead” to send your interest to the dealership.
Showroom Kiosk Mode (Windows Standalone)

A Windows version is also available for in‑showroom kiosks.
Build the project for Windows Standalone from Unity, or request a pre‑built executable from the development team.

Android‑Specific Notes

The Android build uses ARCore via AR Foundation. Key considerations:

Minimum API level: 24 (Android 7.0)
ARCore must be installed on the device (the app will prompt installation if missing).
All AR session management is handled in the AR Session and AR Session Origin GameObjects.
For debugging, use adb logcat with the tag Unity to filter Unity logs.
Backend Integration

The app communicates with a cloud backend for:

Asset streaming – 3D models and textures are loaded at runtime from AWS S3/CloudFront using Unity Addressables, keeping the initial app size small.
Lead submission – When a user submits their interest, the app sends a JSON payload to a dealership‑specific API endpoint. The payload includes:

First name
Vehicle configuration (model, trim, color, wheels)
Estimated deposit and trade‑in
Preferred term and calculated monthly payment
Timestamp
No personally identifiable information (ID numbers, bank details) is ever collected or transmitted.

For development, you can mock the API locally. Contact the team for sample API contracts.

Contributing

We welcome contributions! Please follow our guidelines:

Fork the repository.
Create a feature branch
Branch names must follow the pattern:
<username>/<issue-type>/<ticket-number>
Examples:

brusley/feature/PROJ-123
jane/hotfix/PROJ-456
mike/bugfix/PROJ-789
This naming convention keeps work organized and links branches directly to Jira tickets.
Commit your changes with clear messages (reference the ticket number in commits, e.g., PROJ-123: Add affordability estimator UI).
Push to your branch.
Open a Pull Request against the develop branch.
For major changes, please open an issue first to discuss what you would like to change.

Read our Contributing Guidelines for more details.

License

Distributed under the MIT License. See LICENSE for more information.

Contact

NEOXR
Project Lead: Brusley Masemola

Email: brusleymasemola@hotmail.com
LinkedIn: Brusley Masemola
GitHub: @BrusleyM
Project Link: https://github.com/BrusleyM/NAS_APP

Acknowledgements

AR Foundation
Unity Addressables
AWS S3 & CloudFront
Inspired by the needs of modern automotive dealerships
