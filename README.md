# Consent-Driven Proximity Interaction Framework

A Unity VR framework for **Meta Quest 3** that models real-world consent as a first-class mechanic in shared virtual spaces. Players cannot enter another avatar's personal space without an explicit, mutual consent handshake — and can withdraw that consent at any time.

Built for Meta Quest 3 on Unity 6 (6000.3.6f1) with OpenXR and the Meta XR SDK.

---

## Demo

> 📹 **Demo video coming soon** — will be embedded here once recorded.

<!-- Replace the line above with the video once uploaded, e.g.:
[![Demo Video](docs/demo_thumbnail.png)](https://youtu.be/YOUR_VIDEO_ID)
-->

---

## Features

- 🎯 **Proximity-aware consent state machine** — `Idle → InRange → Requested → Active → Terminated`
- 🛡️ **Protected zone** — soft push-back when approaching without consent
- 🙋 **Request / Accept / Reject / Withdraw** flow with timeout handling
- ⏱️ **Rejection cooldown** and **one-consent-per-session** rules
- 🎨 **Visual feedback** — green/red panel flash, floor consent ring, countdown label, live status board
- 🎮 **Controller ray interaction** on wall buttons (Accept / Reject / Reset)
- 📦 **Standalone APK** — runs natively on Quest 3, no PC tether required

---

## Team — JARKOE

| Name | Role |
|---|---|
| Jeshua Herrera | Networking / Session Authority |
| Anna *(add role)* | *(add role)* |
| Rebekah Jensen | Proximity Service |
| Kerri Jensen | Consent State Machine & UI |
| Oscar Canoa | Feedback Manager |
| Edu Bussien | Integrator / TestHarness |

*(Update team roles as needed!)*

---

## Tech Stack

- **Unity 6** (6000.3.6f1)
- **OpenXR** + Meta XR SDK v85
- **Meta Interaction SDK** — ray-based UI interaction
- **XR Interaction Toolkit** — locomotion
- **TextMeshPro** — in-VR status displays
- **IL2CPP / ARM64** Android build target

---

## Project Structure

```
Assets/
├── ConsentProximityFramework/   # Core framework (runtime modules)
│   ├── Runtime/
│   │   ├── ConsentUI/           # Consent popup UI
│   │   ├── Feedback/            # Haptics / audio / visual feedback
│   │   ├── Networking/          # Consent flow networking
│   │   └── Proximity/           # Distance tracking
│   └── Samples/TestHarness/     # Integration demo scene
│       ├── Scripts/
│       │   ├── HarnessController.cs  # Central orchestrator
│       │   └── DebugHUD.cs           # In-VR status board
│       └── IntegrationTestHarness.unity
├── Runtime/
│   ├── Core/Interfaces/         # IConsentService, IProximityService, etc.
│   ├── StateMachine/            # ConsentStateMachine
│   └── UI/                      # ConsentUI prefab
├── Scripts/Networking/          # NGO network adapter, session registry
└── Tests/
    ├── EditMode/                # Unit tests (state machine, adapters)
    └── PlayMode/                # Integration tests
```

---

## How the Demo Works

1. **Player A** (VR user, wearing the headset) approaches **Player B** (static avatar)
2. When A crosses `maxRangeMeters`, a consent popup appears in front of A
3. Behind A are three wall buttons — **Accept**, **Reject**, **Reset** — simulating Player B's input
4. Clicking Accept/Reject queues a response that fires **5 seconds after** A re-enters B's range (with a countdown label above B)
5. **Accept** → green flash, A can enter the safe zone, can move freely for the session
6. **Reject** → red flash, 60-second cooldown before A can request again
7. **Reset** button — clears state for the next run

---

## Build & Run (Meta Quest 3)

### Requirements
- Unity 6 (6000.3.6f1) with Android Build Support
- Meta Quest 3 with Developer Mode enabled
- USB-C data cable

### Setup
1. Open the project in Unity
2. **File → Build Profiles** → switch to **Android**
3. **Project Settings → XR Plug-in Management → Android tab**: enable **OpenXR** + **Meta Quest Support**
4. **Player Settings → Android → Other Settings**: IL2CPP, ARM64, min API 29
5. Plug Quest in via USB-C, accept USB debugging prompt
6. **File → Build Profiles → Build And Run**

---

## Controls

**In VR:**
- **Left joystick** — smooth locomotion
- **Right joystick** — snap turn
- **Controller trigger** — click wall buttons (ray interaction)
- **Physically walk** into range of Player B to trigger the consent popup

**Keyboard shortcuts (Editor / desktop testing):**
| Key | Action |
|---|---|
| `R` | Request consent |
| `A` | Accept |
| `C` | Cancel / Reject |
| `W` | Withdraw |
| `B` | Queue Player B accept |
| `N` | Queue Player B reject |

---

## Testing

- **EditMode tests:** `Assets/Tests/EditMode/` — state machine logic, network adapters
- **PlayMode tests:** `Assets/Tests/PlayMode/` — proximity integration
- Run via **Window → General → Test Runner** in Unity

---

## License

University capstone project — internal use only (for now).
