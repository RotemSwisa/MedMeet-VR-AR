# MedMeet - VR/AR Medical Meeting Platform <img width="80" height="80" alt="logo" src="https://github.com/user-attachments/assets/6d0a8b52-661f-4873-aaaf-b5cb82132dc2" />

**A remote medical consultation should not be a flat video call.**

MedMeet is a multi-user virtual reality platform where doctors, patients and
medical students meet inside a shared 3D consultation room. Participants join as
avatars, talk with spatial voice, and work together on the same anatomical model
- one person rotates a heart while another points at it with a laser and
annotates it, and everyone sees the same thing at the same moment.

Built in Unity for the Meta Quest 3 as a final year Software Engineering project
at Kinneret Academic College. **Final grade: 99.**

### 🌐 [**See the project site - demo videos, gallery and documents**](https://rotemswisa.github.io/MedMeet/)

> The site carries the full demo videos from all four sprints, screenshots of
> every feature, the project poster and the written documentation. It is the
> fastest way to understand what the platform actually does.
>
> This repository holds the **source code**.

---

## Contents

- [The problem](#the-problem)
- [What MedMeet does](#what-medmeet-does)
- [How it works](#how-it-works)
- [How it was built](#how-it-was-built)
- [Tech stack](#tech-stack)
- [My role](#my-role)
- [About this repository](#about-this-repository)
- [Running the project](#running-the-project)
- [Team](#team)

---

## The problem

Telemedicine mostly means a video window. That is fine for a conversation and
poor for anything involving a body, a scan or a procedure. A doctor cannot point
at the exact spot on a patient's spine. A lecturer cannot show fifteen students
what a chest looks like when it is opened. A deaf patient gets a phone number and
a waiting list.

Video calls flatten the one thing medicine depends on: **shared attention on a
physical object.**

MedMeet puts the object back in the room. The model is in the middle, everyone
walks around it, and the conversation happens in the same space as the thing
being discussed.

---

## What MedMeet does

### Meeting in VR

- **Multi-room system** - several consultations run in parallel; the login screen
  shows live availability, and doors connect rooms with smooth transitions
- **Avatars** - male and female avatars with hand tracking (IK), walking,
  head direction and gestures synchronized to every other participant
- **Spatial voice** - VoIP with mute control and active-speaker highlighting
- **Shared 3D models** - rotate, zoom and switch between anatomical models;
  every interaction is mirrored for all participants in real time
- **Laser pointer and annotations** - each user gets a uniquely coloured pointer,
  paint markings persist on the model, and text annotations carry the author name
- **Medical image sharing** - upload X-rays and scans into a shared gallery with
  synchronized zoom and pan
- **Session recording** - avatar movement, positions and audio recorded in sync
  and replayable as a session file
- **Attendance and analytics** - join/leave events with timestamps, talk-time per
  participant and participation percentages, exported to CSV

### Accessibility

- **Real-time sign language avatar** - an interpreter figure in the corner of the
  view, driven by speech recognition, mapping spoken words to medical sign
  language animations in Hebrew and English
- **Voice commands** - hands-free navigation between rooms and menus, in Hebrew
  and English, with a fallback response for unrecognized commands
- **AI assistant** - ask a medical question by voice or text during a session and
  receive an answer on a scrollable panel with text-to-speech playback

### Medical training

- **Interactive human body** - select any organ to highlight it and open an
  information panel with its name, function and medical detail. Grab the organ,
  lift it out of the body, examine it closely, and return it to position
- **Surgery training rooms** - dedicated heart, lung and brain rooms, each with a
  step-by-step animated surgery tutorial and playback controls
- **Chest surgery simulation** - open a chest step by step with a virtual scalpel
  and reveal the organs beneath
- **Procedure simulations** - blood draw and throat examination, each with
  step logic, visual feedback for correct and incorrect actions, and scoring
- **Learning centre** - a room of interactive knowledge panels holding medical
  articles, presentations and images, browsable with hand gestures

### AR mode

Using Quest 3 passthrough, models can be anchored onto the real world and
examined in your actual room, with a button to switch between AR and full VR.

---

## How it works

```
        ┌──────────────┐   ┌──────────────┐   ┌──────────────┐
        │ Participant  │   │ Participant  │   │ Participant  │
        │  Quest 3     │   │  Quest 3     │   │  Quest 3     │
        └──────┬───────┘   └──────┬───────┘   └──────┬───────┘
               │                  │                  │
               └────────┬─────────┴─────────┬────────┘
                        │                   │
                 ┌──────▼───────┐   ┌───────▼────────┐
                 │   Normcore   │   │  Photon/Vivox  │
                 │  state sync  │   │   spatial VoIP │
                 └──────┬───────┘   └────────────────┘
                        │
     ┌──────────────────┼──────────────────┐
     │                  │                  │
┌────▼─────┐  ┌─────────▼────────┐  ┌──────▼────────┐
│ Avatars  │  │  Shared models,  │  │  Annotations, │
│ movement │  │  image gallery   │  │  laser, paint │
│ and IK   │  │                  │  │               │
└──────────┘  └──────────────────┘  └───────────────┘

                 ┌────────────────────────────┐
                 │  Speech and AI services    │
                 │  Whisper transcription     │
                 │  GPT assistant             │
                 │  Sign language mapping     │
                 └────────────────────────────┘
```

Every participant runs the same Unity application on their own headset.
**Normcore** owns the shared state - avatar transforms, model rotation, gallery
selection, annotations - so that a change made by one person appears for
everybody without a custom server. Voice runs on its own channel so audio
latency stays independent of state updates. Speech and AI features call external
services and return their results into the shared session.

---

## How it was built

Four Scrum sprints over an academic year, roughly 270–280 hours of team effort
each.

| Sprint | Period | Focus | Delivered |
|---|---|---|---|
| **1** | Oct – Dec 2025 | Foundation | VR meeting room for up to 5 participants, avatar system, real-time VoIP, session management, shared 3D model, automatic attendance logging with CSV export |
| **2** | Dec 2025 – Jan 2026 | Advanced features | Multi-room navigation, 360° model interaction, laser pointer and persistent annotations, medical image sharing, AI transcription with speaker diarization, people analytics, 3D session recording, AR passthrough mode |
| **3** | Mar – Jun 2026 | Innovation and accessibility | Sign language system, surgery training rooms, interactive human body, AI assistant, voice commands, learning centre, procedure simulations, full movement sync |
| **4** | 2026 | Simulation and closure | Chest surgery simulation with scalpel, organ inspection panels, UI overhaul, closing edges across all features |

---

## Tech stack

| Area | Technology |
|---|---|
| Engine | Unity 6 (6000.2.13f1) |
| Language | C# |
| Target hardware | Meta Quest 3 - VR and AR passthrough |
| XR | Meta XR SDK 83.0.1, Unity XR Interaction Toolkit 3.2.2, OpenXR 1.16.0 |
| Multiplayer state | Normcore 2.17.3 |
| Voice | Photon / Vivox VoIP |
| Speech to text | Whisper |
| In-session assistant | GPT-based API |
| Avatars | Ready Player Me |
| Process | Scrum - 4 sprints, backlog, sprint reviews |

---

## My role

I worked across the platform as a developer, writing feature code throughout all
four sprints, and served as the team's **Quality Owner**.

As Quality Owner I defined the acceptance criteria for each sprint's features and
ran end-to-end testing **on the headsets themselves** rather than only in the
Unity editor - which for this project mattered more than usual. Multi-user
synchronization, voice stability under load, hand tracking and AR passthrough all
behave differently on device than in a simulator, and several issues only ever
appeared with four people in a room at once.

---

## About this repository

This repository holds the project's **source code, scenes and configuration** -
546 C# scripts, the Unity scenes, prefabs, materials and project settings.

Three categories of files are deliberately excluded, and the project will not
build from a fresh clone without restoring them:

1. **`Library/`** - Unity's local cache. It is 24.7 GB here and Unity regenerates
   it automatically the first time the project is opened.
2. **Third-party asset packs** - Ready Player Me, TextMesh Pro and the course
   asset library ship under their own licences and are not redistributable in a
   public repository. They are re-imported from their original sources.
3. **Large binary source art** - the anatomy meshes and texture sources reach
   421 MB for a single file, far past GitHub's 100 MB per-file limit.

Media for the project - demo videos, screenshots, the poster and the
presentation - lives on the [project site](https://rotemswisa.github.io/MedMeet/)
and in its [companion repository](https://github.com/RotemSwisa/MedMeet), so that
this repository stays a code repository.

---

## Running the project

**Requirements**

- Unity 6 (6000.2.13f1)
- A Meta Quest 3, or the XR Device Simulator for editor-only testing
- The third-party asset packs listed above, re-imported into `Assets/`
- API credentials for the speech and assistant services

**Steps**

1. Clone the repository and open the folder with Unity Hub, selecting Unity
   6000.2.13f1. The first import takes a while - Unity is rebuilding `Library/`.
2. Re-import the excluded asset packages from the Unity Asset Store and Ready
   Player Me.
3. Open a scene from `Assets/Scenes/`.
4. To run on device, enable Android build support, connect the Quest 3 with
   developer mode on, and build and run.

---

## Team

Built by a four-person team at Kinneret Academic College, 2025–2026.

| Member | Role |
|---|---|
| Lidor Ben Simon | Product Owner |
| David Ran Cohen | Scrum Master |
| Libar Vizman | Architect |
| **Rotem Swisa** | **Quality Owner** |

*Final year project, Department of Software Engineering, Kinneret Academic
College on the Sea of Galilee.*
