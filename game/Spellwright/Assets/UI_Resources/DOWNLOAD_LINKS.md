# UI Resources - Download Links

Resources for the Spellwright UI overhaul. Items marked with [DOWNLOADED] were
fetched automatically; others require manual download.

---

## 1. Ultimate Oldschool PC Font Pack v2.2 [DOWNLOADED]

- **Status:** Downloaded to `Fonts/oldschool_pc_font_pack_v2.2_FULL.zip`
- **Source:** https://int10h.org/oldschool-pc-fonts/
- **License:** CC BY-SA 4.0
- **Contents:** 200+ character sets in TrueType (.ttf), bitmap (.fon), and web
  font formats. The TTF files in `ttf - Ac (aspect-corrected)/` and
  `ttf - Px (pixel outline)/` are most useful for Unity (import as Font Asset
  via TextMeshPro).
- **Extract:** Unzip and import the desired `.ttf` files into Unity, then
  create TMP Font Assets via `Window > TextMeshPro > Font Asset Creator`.

---

## 2. Keyboard & Mouse SFX Pack [MANUAL DOWNLOAD REQUIRED]

- **URL:** https://okutasan.itch.io/keyboard-and-mouse-sound-pack
- **Price:** Name-your-own-price (free / $0 allowed)
- **File:** Keyboard Mouse Soundpack.zip (~7.8 MB)
- **Contents:** 26 WAV + OGG files (keyboard typing, mouse clicks, etc.)
- **License:** Custom license allowing commercial use. Credit appreciated but
  only required if redistributing modified versions.

### Instructions:
1. Visit the URL above.
2. Click "Download Now".
3. Enter $0 (or any amount) and click "No thanks, just take me to the downloads".
4. Download `Keyboard Mouse Soundpack.zip`.
5. Place the ZIP (or extracted files) into `SFX/KeyboardMouse/`.

---

## 3. Freesound CRT Recordings [MANUAL DOWNLOAD REQUIRED]

Freesound requires a free account to download. Sign up at
https://freesound.org/home/register/ then download each file.

### 3a. CRT Computer Monitor Startup
- **URL:** https://freesound.org/people/corkob/sounds/415594/
- **File:** 415594__corkob__crt-computer-monitor-startup.wav
- **Format:** WAV, 48 kHz, 24-bit, stereo
- **License:** CC0 (public domain)
- **Save to:** `SFX/CRT/crt-monitor-startup.wav`

### 3b. Loopable 60Hz CRT Hum
- **URL:** https://freesound.org/people/Timbre/sounds/721295/
- **File:** 721295__timbre__loopable-60hz-synthesized-domestic-video-artifact-vcr-crt-buzz-hum.flac
- **Format:** FLAC, 44.1 kHz, 16-bit, mono, ~1m20s
- **License:** Check Freesound page for license details
- **Note:** This is a synthesized 60Hz hum loop (not a real recording). Great
  for ambient CRT background noise.
- **Save to:** `SFX/CRT/crt-60hz-hum-loop.flac`
- **Unity note:** Unity can import FLAC natively. Alternatively, convert to WAV
  before importing.

### 3c. CRT Monitor On/Off/Degauss
- **URL:** https://freesound.org/people/Sanderboah/sounds/838728/
- **File:** 838728__sanderboah__computer-crt-monitor-turn-onoffdegauss.wav
- **Format:** WAV, 96 kHz, 16-bit, mono, ~30s
- **License:** Check Freesound page for license details
- **Note:** Compaq PC CRT from early 2000s -- startup, degauss, and shutdown
  sounds in one file. May need to be split into individual clips.
- **Save to:** `SFX/CRT/crt-monitor-on-off-degauss.wav`

---

## Directory Structure (target)

```
UI_Resources/
  DOWNLOAD_LINKS.md          <-- this file
  Fonts/
    oldschool_pc_font_pack_v2.2_FULL.zip   [DOWNLOADED]
  SFX/
    KeyboardMouse/            (itch.io pack goes here)
    CRT/                      (Freesound recordings go here)
```
