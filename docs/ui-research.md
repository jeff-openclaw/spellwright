# Spellwright UI Research

Research into UI frameworks, asset packs, and workflow patterns for migrating from uGUI to a more web-dev-friendly system with CRT terminal aesthetic.

---

## UI Research — Pass 1 | 2026-03-04 | Framework Researcher

### Finding
- **Unity UI Toolkit (UXML + USS)**: Unity's official web-inspired UI framework using UXML (markup like HTML), USS (styling like CSS), and C# (behavior). Uses Yoga (same flexbox engine as React Native) for layout. Supports `flex-direction`, `justify-content`, `align-items`, `flex-grow/shrink/basis`, `flex-wrap`. USS supports class selectors, pseudo-classes (`:hover`, `:active`, `:focus`, `:checked`, `:disabled`), CSS variables (`--name` / `var()`), and transitions with rich easing functions. In Unity 6.3: USS filters (`blur()`, `grayscale()`, `tint()`, `contrast()`), UI Shader Graph, world-space rendering, and glyph-level text animation. Missing vs CSS: no `calc()`, no media queries, no `gap`, no sibling selectors, no `::before`/`::after`, no `z-index`. Default `flex-direction` is `column` (not `row`). Reusable components via UXML Templates (like HTML includes) and Custom Controls (like React components with `[UxmlElement]`). Learning curve for web dev: USS layout productive in 1-2 days, full proficiency in 2-3 weeks. CRT post-processing works identically — renders to same screen buffer. DOTween needs manual wrappers but USS transitions cover most animation needs.
  Link: https://docs.unity3d.com/6000.3/Documentation/Manual/ui-systems/introduction-ui-toolkit.html
  Checklist: ✅ Unity 6 native ✅ Real flexbox layout ✅ USS theming perfect for terminal aesthetic ✅ Extensive official docs ✅ Free (built-in)

### Surviving findings (cumulative)
- Unity UI Toolkit (UXML + USS)

### Killed this pass
- None (first pass)

### Resume token
UI_LAST_PASS=1 | SURVIVING=1 | NEXT_ROLE=Asset Hunter

---

## UI Research — Pass 2 | 2026-03-04 | Asset Hunter

### Finding
- **1bit_UI Pixel Pack** (by piiixl): Complete monochrome 1-bit pixel UI kit — panels, windows, buttons, toggles, sliders, icons (hearts, coins, music), progress bars, numeric displays, settings/inventory/pause/score screens. 1,021 kB PNG sprites with transparent backgrounds. Modular mix-and-match. Being monochrome, just set `Image.color` or USS `tint()` to CRT green (#00FF00) and everything looks like a phosphor terminal. Most comprehensive structural UI kit found. $9 USD.
  Link: https://piiixl.itch.io/1bit-ui
  Checklist: ✅ Engine-agnostic PNGs (any Unity) ✅ Complete component coverage ✅ Green-tintable monochrome = perfect CRT fit ✅ Clear modular design ✅ $9 one-time

- **Retro-Cyberpunk UI Pack** (by Warco_exe): Modular UI boxes, buttons, labels, badges, callouts, icons. 6 color variations. Explicitly inspired by "CRT terminals, cyberpunk visuals, arcade machines and glitch aesthetics." Likely includes green monochrome variant. 250 kB sprite sheets. 4 EUR (~$4.30).
  Link: https://warco-exe.itch.io/retro-cyberpunk-ui-pack
  Checklist: ✅ Engine-agnostic sprites ✅ Modular box system ✅ CRT-terminal-inspired design ✅ Small focused pack ✅ ~$4 one-time

- **URP_RetroCRTShader** (by Cyanilux): Free Shader Graph-based CRT post-process — scanlines, curvature, static noise, scrolling glitch, RGB phosphor stripes. Uses FullscreenPassRendererFeature. MIT license.
  Link: https://github.com/Cyanilux/URP_RetroCRTShader
  Checklist: ✅ Unity 2022+ URP ✅ N/A (shader, not layout) ✅ Literally a CRT effect ✅ GitHub repo with examples ✅ Free MIT

- **Retro Shaders Pro** (by Daniel Ilett): CRT post-process + VHS artifacts + retro console palette emulation (Game Boy green monochrome!) + PSX shading + dithering. Tested on Unity 2022.3, 6.0-6.3. URP only. $15.99.
  Link: https://danielilett.itch.io/psx-shaders-pro-for-unity-urp
  Checklist: ✅ Explicitly tested Unity 6 ✅ N/A (shader pack) ✅ Game Boy palette = CRT green ✅ Demo scenes + guides ✅ $16 one-time

- **Hexany's 1-bit UI Panels** (by Hexany Ives): 12 pixel art 1-bit panels at 96x96. Free, CC0 public domain. Monochrome, green-tintable. Very small set — starter/supplement only.
  Link: https://hexany-ives.itch.io/hexanys-1-bit-ui-panels
  Checklist: ✅ Any Unity ✅ Minimal (only 12 panels) ✅ Monochrome = tintable ✅ Simple/obvious ✅ Free CC0

- **Kenney Pixel UI Pack**: 750 pixel art UI tiles — panels, buttons, sliders. 9-slice sprites. Free CC0. General-purpose colorful pixel art, NOT terminal-themed. Would need heavy re-theming.
  Link: https://kenney.nl/assets/pixel-ui-pack
  Checklist: ✅ Any Unity ✅ Comprehensive ❌ Not terminal-themed (colorful retro game style) ✅ Well-known ✅ Free CC0

### Surviving findings (cumulative)
- Unity UI Toolkit (UXML + USS)
- 1bit_UI Pixel Pack ($9, itch.io)
- Retro-Cyberpunk UI Pack (~$4, itch.io)
- URP_RetroCRTShader (free, GitHub)
- Retro Shaders Pro ($16, itch.io)
- Hexany's 1-bit UI Panels (free, itch.io)

### Killed this pass
- Kenney Pixel UI Pack — colorful retro game style, not terminal/CRT aesthetic. Would require too much re-theming to match Spellwright's green phosphor look.

### Resume token
UI_LAST_PASS=2 | SURVIVING=6 | NEXT_ROLE=Critic

---

## UI Research — Pass 3 | 2026-03-04 | Critic

### Finding
- **Critique target: URP_RetroCRTShader** — While a quality free CRT shader, Spellwright already has its own `CRT.shader` + `CRTRenderFeature` that works with URP 17. Adding a second CRT shader implementation creates maintenance overhead and potential conflicts with the existing `CRTSettings` singleton. The Cyanilux shader uses `FullscreenPassRendererFeature` (Shader Graph path) while Spellwright uses a legacy `ScriptableRendererFeature` — these are different rendering approaches that could conflict. Unless the existing CRT effect is inadequate, this is a solution looking for a problem.

- **Critique target: Hexany's 1-bit UI Panels** — Only 12 panels at 96x96. Spellwright has at minimum 5 distinct screens (MainMenu, Map, Encounter, Shop, Results) each with multiple sub-panels. 12 generic panels won't cover the variety needed. The 1bit_UI pack ($9) includes everything Hexany offers plus buttons, toggles, sliders, progress bars, icons, and more. Hexany is redundant if you buy 1bit_UI.

- **Proposed alternative to research**: Instead of more CRT shaders, research **Graphene** (github.com/LudiKha/Graphene) — a lightweight modular framework built ON TOP of UI Toolkit that adds MVVM data binding, routing, and component patterns familiar to React developers. Could bridge the gap between UI Toolkit's raw power and a web-dev-friendly component architecture.

### Surviving findings (cumulative)
- Unity UI Toolkit (UXML + USS)
- 1bit_UI Pixel Pack ($9, itch.io)
- Retro-Cyberpunk UI Pack (~$4, itch.io)
- Retro Shaders Pro ($16, itch.io)

### Killed this pass
- URP_RetroCRTShader — redundant with Spellwright's existing CRT.shader + CRTRenderFeature
- Hexany's 1-bit UI Panels — too limited (12 panels), entirely superseded by 1bit_UI pack

### Resume token
UI_LAST_PASS=3 | SURVIVING=4 | NEXT_ROLE=Framework Researcher

---

## UI Research — Pass 4 | 2026-03-04 | Framework Researcher

### Finding
- **Nova UI**: Third-party UI framework by Supernova Technologies ($40, source included). Uses "UIBlock" building blocks with CSS Box Model-inspired layout — AutoLayout (like flexbox axis flow), AutoSize Expand/Shrink (like flex-grow/fit-content), padding/margin, min/max constraints. Burst/Jobs-compiled for zero GC. Renders as scene geometry (not Canvas), so CRT post-processing applies naturally.
  Link: https://assetstore.unity.com/packages/tools/gui/nova-226304
  Checklist: ❌ **ABANDONED** — Last update Dec 2023 (2+ years ago). No Unity 6 compatibility confirmed. Multiple 1-star reviews: "devs went AWOL." GitHub bug reports from 2024-2025 unanswered. Fatal vendor risk.

### Surviving findings (cumulative)
- Unity UI Toolkit (UXML + USS)
- 1bit_UI Pixel Pack ($9, itch.io)
- Retro-Cyberpunk UI Pack (~$4, itch.io)
- Retro Shaders Pro ($16, itch.io)

### Killed this pass
- Nova UI — abandoned project, no Unity 6 support, fatal vendor risk

### Resume token
UI_LAST_PASS=4 | SURVIVING=4 | NEXT_ROLE=Asset Hunter

---

## UI Research — Pass 5 | 2026-03-04 | Asset Hunter

### Finding
- **Ultimate Oldschool PC Font Pack**: 200+ TrueType fonts pixel-perfect-recreated from real IBM PCs, VGA/EGA/CGA adapters, DEC, Amstrad, Tandy. Two encodings: Code Page 437 (256 chars) and Plus (782 chars, Latin/Greek/Cyrillic). These ARE the fonts that ran on actual CRT terminals. Free, CC BY-SA 4.0.
  Link: https://int10h.org/oldschool-pc-fonts/readme/
  Checklist: ✅ TTF imports into any Unity ✅ Authentic terminal fonts ✅ Literally from real CRTs ✅ Well-documented with previews ✅ Free

- **Retrofuturistic Computer SFX** (Bluezone): 123 sounds/168 files — button beeps, mechanical levers, computer vocalizations, data processing, telemetry, machine hums, malfunction/fault tones. WAV 24-bit/96kHz. Purpose-built for "retro-futuristic data terminals." ~$16.
  Link: https://www.bluezone-corporation.com/packs/retrofuturistic-computer-sound-effects
  Checklist: ✅ WAV imports directly ✅ N/A (audio) ✅ Purpose-built for CRT terminal ✅ Categorized, ready-to-use ✅ ~$16 one-time

- **Keyboard & Mouse SFX Pack** (Luna Desuwa): 26 sounds — 8 mouse clicks, 18 keyboard sounds (individual presses + typing sequences). WAV+OGG, 7.8 MB. Free (name your price).
  Link: https://okutasan.itch.io/keyboard-and-mouse-sound-pack
  Checklist: ✅ WAV/OGG import ✅ N/A (audio) ✅ Authentic keyboard feel for letter guessing ✅ Simple/obvious ✅ Free

- **VHS UI EFFECT** (Turishader): Scanline shaders, glitch distortion, blur — applied to INDIVIDUAL UI elements (not fullscreen). Full TextMeshPro support. Unlike fullscreen CRT post-processing, this lets you glitch specific panels on wrong guesses, add scanlines to selected elements. ~$15. Unity 6000.0.23+, URP compatible.
  Link: https://assetstore.unity.com/packages/vfx/vhs-ui-effect-stylized-glitch-scanline-shaders-for-ui-320186
  Checklist: ✅ Unity 6+ ✅ Per-element UI effects ✅ Scanlines + glitch = CRT native ✅ "Running in minutes" per reviews ✅ ~$15 one-time

- **Freesound CRT Monitor Recordings**: Real CRT hardware recordings — power-on click + rising whine, degauss thump, 60Hz hum loop (perfect ambient), power-off. Multiple contributors. Free (CC0/CC-BY varies per sound).
  Link: https://freesound.org/search/?q=CRT+monitor
  Checklist: ✅ WAV/FLAC import ✅ N/A (audio) ✅ Literally recorded from CRT hardware ✅ Previews on site ✅ Free

- **Cathoder** (Brandon Beauregard): CRT surface shader rendering content onto a 3D monitor mesh. Phosphor display types (Color, Monochrome, Amber), shadow map textures, analog color bleeding, RF static. MIT license, free.
  Link: https://spectrecarnival.itch.io/cathoder
  Checklist: ✅ Unity (needs URP adaptation check) ✅ N/A (shader) ✅ Monochrome phosphor mode = green terminal ✅ MIT source available ✅ Free

### Surviving findings (cumulative)
- Unity UI Toolkit (UXML + USS)
- 1bit_UI Pixel Pack ($9, itch.io)
- Retro-Cyberpunk UI Pack (~$4, itch.io)
- Retro Shaders Pro ($16, itch.io)
- Ultimate Oldschool PC Font Pack (free)
- Retrofuturistic Computer SFX (~$16)
- Keyboard & Mouse SFX Pack (free)
- VHS UI EFFECT (~$15, Asset Store)
- Freesound CRT Recordings (free)
- Cathoder (free, MIT)

### Killed this pass
- Share Tech Mono, Space Mono — nice fonts but too modern/clean; Spellwright already has VT323 and the Oldschool PC Font Pack gives authentic alternatives
- Retro: CRT TV shader (~$10) — redundant with existing CRT.shader; Retro Shaders Pro already covers this space better
- Fake Terminal (free) — reference only, Spellwright already has full terminal UI implementation

### Resume token
UI_LAST_PASS=5 | SURVIVING=10 | NEXT_ROLE=Critic

---

## UI Research — Pass 6 | 2026-03-04 | Critic

### Finding
- **Critique target: Retro-Cyberpunk UI Pack (~$4)** — While CRT-inspired, this pack overlaps significantly with 1bit_UI ($9) which is more comprehensive. The Retro-Cyberpunk pack is 250 KB of sprites with 6 color variations — small and focused but not enough to build a complete UI. The 1bit_UI pack already provides panels, buttons, toggles, sliders, progress bars, score screens, and icons. Buying both creates asset fragmentation (mixing two different pixel art styles). Kill it — consolidate on 1bit_UI.

- **Critique target: Cathoder (free CRT surface shader)** — Cool concept (render UI onto a 3D CRT mesh), but Spellwright is a 2D screen-space game. Adding a 3D CRT monitor object to frame the UI would be a significant architectural change. The existing fullscreen CRT post-process achieves the same visual result with zero complexity. Cathoder is a solution for a 3D game that wants a diegetic computer terminal, not for a game where the entire screen IS the terminal.

- **Proposed alternative to research**: **ReactUnity** (github.com/ReactUnity/core) — 855 stars, actively maintained (Dec 2025), lets you write actual React/JSX that renders to Unity UI. Since the developer has React experience, this could be the most natural migration path. But it adds a JS runtime — needs investigation of performance and architectural implications.

- **Also noted**: **Graphene** (github.com/LudiKha/Graphene) — React-like MVVM framework on top of UI Toolkit. Interesting concept (routing, data binding, template composition) but has broken local file path dependencies, requires Odin Inspector (paid), and is a solo dev passion project. Not production-ready. **Unity App UI** (`com.unity.dt.app-ui`) is the official Unity MVVM layer on top of UI Toolkit — pre-1.0 but Unity-maintained. Worth watching but too immature to recommend for shipping code.

### Surviving findings (cumulative)
- Unity UI Toolkit (UXML + USS)
- 1bit_UI Pixel Pack ($9, itch.io)
- Retro Shaders Pro ($16, itch.io)
- Ultimate Oldschool PC Font Pack (free)
- Retrofuturistic Computer SFX (~$16)
- Keyboard & Mouse SFX Pack (free)
- VHS UI EFFECT (~$15, Asset Store)
- Freesound CRT Recordings (free)

### Killed this pass
- Retro-Cyberpunk UI Pack — redundant with 1bit_UI, creates style fragmentation
- Cathoder — 3D CRT mesh solution for a 2D screen-space game; existing post-process suffices
- Graphene — broken dependencies, requires paid Odin asset, solo dev project, not production-ready

### Resume token
UI_LAST_PASS=6 | SURVIVING=8 | NEXT_ROLE=Framework Researcher

---

## UI Research — Pass 7 | 2026-03-04 | Framework Researcher

### Finding
- **FancyScrollView** (setchi/FancyScrollView): Versatile Unity scroll view component (3.5k stars, MIT). Passes normalized position (0-1) to each cell for custom animations (scale, rotation, opacity). Features: cell virtualization/recycling, snap-to-cell, infinite/loop scroll, animated scroll-to, grid layout support. Latest release v1.9.0 (Sept 2025) — actively maintained. Pure C# on uGUI ScrollRect/RectTransform. Does NOT work with UI Toolkit.
  Link: https://github.com/setchi/FancyScrollView
  Checklist: ✅ Unity 2019.4+ (likely Unity 6 compatible) ✅ Virtualization + animation beyond ScrollRect ✅ Neutral (works with any theme) ✅ Well-documented, many examples ✅ Free MIT

- **unity-flex-ui** (gilzoide/unity-flex-ui): Flexbox layout for uGUI using the **Yoga layout engine** (same engine UI Toolkit uses). Full flexbox properties on RectTransforms. Live edit-mode preview. Cross-platform. Can coexist with standard uGUI — only lays out children that also have FlexLayout. v1.2.2, MIT.
  Link: https://github.com/gilzoide/unity-flex-ui
  Checklist: ✅ Multi-platform including Unity 6 ✅ Real Yoga flexbox on uGUI ✅ Neutral (layout only) ✅ GitHub docs + examples ✅ Free MIT

- **Note on UI Toolkit ListView**: UI Toolkit has built-in virtualized ListView/TreeView for vertical lists. However, it lacks snap-to-cell, infinite scroll, horizontal virtualization, and per-cell animation control. For plain vertical lists it's sufficient; for anything fancier, FancyScrollView (uGUI) is still needed.

### Surviving findings (cumulative)
- Unity UI Toolkit (UXML + USS)
- 1bit_UI Pixel Pack ($9, itch.io)
- Retro Shaders Pro ($16, itch.io)
- Ultimate Oldschool PC Font Pack (free)
- Retrofuturistic Computer SFX (~$16)
- Keyboard & Mouse SFX Pack (free)
- VHS UI EFFECT (~$15, Asset Store)
- Freesound CRT Recordings (free)
- FancyScrollView (free, GitHub)
- unity-flex-ui (free, GitHub)

### Killed this pass
- LoopScrollRect — simpler but entirely superseded by FancyScrollView which includes recycling plus animation/snap
- Unity UI Extensions — large unfocused package, includes a fork of FancyScrollView; better to use the original

### Resume token
UI_LAST_PASS=7 | SURVIVING=10 | NEXT_ROLE=Asset Hunter

---

## UI Research — Pass 8 | 2026-03-04 | Asset Hunter

### Finding
- **QuizU** (Unity Technologies): Official Unity UI Toolkit sample project — a quiz game demonstrating runtime UI Toolkit patterns. Uses UXML templates, USS styling, C# event handling, screen management. Free on Asset Store.
  Link: https://assetstore.unity.com/packages/essentials/tutorial-projects/quizu-a-ui-toolkit-sample-268492
  Checklist: ✅ Official Unity sample ✅ Full UI Toolkit patterns ✅ Neutral (reference architecture) ✅ Official tutorial docs ✅ Free

- **UI Toolkit Debugger**: Built into Unity 6 — Chrome DevTools-like inspector for the visual tree. View/edit USS properties in real-time, inspect layout boxes, pick elements. Enable via Window > UI Toolkit > Debugger. Also supports **Live Reload** — editing UXML in UI Builder auto-updates the Game view.
  Link: https://docs.unity3d.com/6000.3/Documentation/Manual/UIE-ui-debugger.html
  Checklist: ✅ Built into Unity 6 ✅ Real-time style inspection ✅ N/A ✅ Official docs ✅ Free (built-in)

- **Theme Style Sheets (TSS)**: Unity's built-in theming system for UI Toolkit. TSS files are USS files treated as a distinct asset type — reference other USS files via `@import`, swap themes at runtime. Create a `terminal-dark.tss` and `terminal-light.tss` and switch between them. Perfect for the CRT green design system.
  Link: https://docs.unity3d.com/6000.0/Documentation/Manual/UIE-tss.html
  Checklist: ✅ Unity 6 native ✅ Runtime theme switching ✅ Ideal for terminal theme system ✅ Official docs ✅ Free (built-in)

- **Official uGUI to UI Toolkit Migration Guide**: Unity's official docs covering element mapping (Canvas→PanelSettings, Image→VisualElement, Button→Button, etc.), event system differences, layout comparison. Updated for Unity 6000.3.
  Link: https://docs.unity3d.com/6000.3/Documentation/Manual/UIE-Transitioning-From-UGUI.html
  Checklist: ✅ Unity 6 specific ✅ Step-by-step migration ✅ N/A ✅ Official comprehensive guide ✅ Free

### Surviving findings (cumulative)
- Unity UI Toolkit (UXML + USS)
- 1bit_UI Pixel Pack ($9, itch.io)
- Retro Shaders Pro ($16, itch.io)
- Ultimate Oldschool PC Font Pack (free)
- Retrofuturistic Computer SFX (~$16)
- Keyboard & Mouse SFX Pack (free)
- VHS UI EFFECT (~$15, Asset Store)
- Freesound CRT Recordings (free)
- FancyScrollView (free, GitHub)
- unity-flex-ui (free, GitHub)
- QuizU sample project (free, Asset Store)
- UI Toolkit Debugger + Live Reload (built-in)
- TSS theming system (built-in)
- Official Migration Guide (docs)

### Killed this pass
- unity-project-template-uitoolkit (Rivello) — 0 stars, 2 commits, minimal content; QuizU is a far better reference
- UIElementsExamples (Unity) — Editor-only examples, not runtime game UI

### Resume token
UI_LAST_PASS=8 | SURVIVING=14 | NEXT_ROLE=Critic

---

## UI Research — Pass 9 | 2026-03-04 | Critic

### Finding
- **Critique target: Retro Shaders Pro ($16)** — Spellwright already has a working CRT.shader + CRTRenderFeature. The main value proposition (CRT scanlines, curvature, color effects) is already implemented. The Game Boy palette emulation is a neat novelty but doesn't solve a real problem — Spellwright's green is applied via USS/Image.color tinting, not palette remapping. VHS artifacts and PSX shading are irrelevant to a terminal aesthetic. $16 for features you mostly already have.

- **Critique target: FancyScrollView** — Powerful but uGUI-only. If the recommendation is to migrate to UI Toolkit (which it likely will be), FancyScrollView becomes a dead end. Spellwright's scrollable content (shop items, tome list) is small enough that UI Toolkit's built-in ListView handles it fine. FancyScrollView's strength (carousel animations, snapping) isn't needed for a text-terminal UI.

- **Critique target: unity-flex-ui** — Same concern: adds Yoga flexbox to uGUI, but UI Toolkit already HAS Yoga flexbox natively. If migrating to UI Toolkit, unity-flex-ui is redundant. If staying on uGUI, it's excellent — but the strategic direction should be UI Toolkit.

- **Proposed reframe**: The research is converging on UI Toolkit as the clear winner. The remaining passes should focus on: (1) ReactUnity as a wildcard, (2) practical "how to structure Spellwright's specific screens in UI Toolkit", and (3) final summary.

### Surviving findings (cumulative)
- Unity UI Toolkit (UXML + USS)
- 1bit_UI Pixel Pack ($9, itch.io)
- Ultimate Oldschool PC Font Pack (free)
- Retrofuturistic Computer SFX (~$16)
- Keyboard & Mouse SFX Pack (free)
- VHS UI EFFECT (~$15, Asset Store)
- Freesound CRT Recordings (free)
- QuizU sample project (free, Asset Store)
- UI Toolkit Debugger + Live Reload (built-in)
- TSS theming system (built-in)
- Official Migration Guide (docs)

### Killed this pass
- Retro Shaders Pro — redundant with existing CRT.shader, solves no new problem for $16
- FancyScrollView — uGUI-only, dead end if migrating to UI Toolkit; Spellwright's lists are small
- unity-flex-ui — adds Yoga to uGUI, but UI Toolkit already has Yoga natively; redundant on the migration path

### Resume token
UI_LAST_PASS=9 | SURVIVING=11 | NEXT_ROLE=Framework Researcher

---

## UI Research — Pass 10 | 2026-03-04 | Framework Researcher

### Finding
- **ReactUnity** (ReactUnity/core): 855 stars, MIT license. Write actual React/JSX that renders to Unity UI (supports both uGUI and UI Toolkit backends). Uses Jint (JavaScript interpreter in C#) — no native plugin, runs everywhere Unity runs. Supports TypeScript, Redux, React Router, i18next. Last push: December 2025 — actively maintained. You write `.tsx` files, import Unity components, and ReactUnity renders them to the visual tree.
  Link: https://github.com/ReactUnity/core
  Checklist: ✅ Unity 2020+, likely Unity 6 ✅ Full React/JSX with flexbox ✅ Style via CSS/inline (theme-agnostic) ✅ Docs + examples + TypeScript support ✅ Free MIT
  **However**: Adds a JavaScript runtime (Jint) to your Unity project. Performance overhead on every frame for JS→C# interop. Debugging across two language runtimes (C# + JS) is painful. The community is small (855 stars). For a solo dev, maintaining two language ecosystems is a burden. The "use React in Unity" dream sounds appealing but the practical tradeoffs are significant.

- **Community consensus (Unity Forums/Reddit, late 2025)**: UI Toolkit is recommended for new projects on Unity 6+ but uGUI is still "the recommendation" per Unity's official comparison page. The gap is narrowing rapidly — 6.3 added filters, Shader Graph, world-space rendering. Key missing features still in development: CSS Grid, `gap`, `z-index`, media queries, `:nth-child`. Practical advice: "If building for 2025+, UI Toolkit is future-proof. If shipping in 3 months and you know uGUI, stick with it."

### Surviving findings (cumulative)
- Unity UI Toolkit (UXML + USS)
- 1bit_UI Pixel Pack ($9, itch.io)
- Ultimate Oldschool PC Font Pack (free)
- Retrofuturistic Computer SFX (~$16)
- Keyboard & Mouse SFX Pack (free)
- VHS UI EFFECT (~$15, Asset Store)
- Freesound CRT Recordings (free)
- QuizU sample project (free, Asset Store)
- UI Toolkit Debugger + Live Reload (built-in)
- TSS theming system (built-in)
- Official Migration Guide (docs)

### Killed this pass
- ReactUnity — impressive concept but adds JS runtime overhead, dual-language debugging, small community; too much complexity for a solo dev project

### Resume token
UI_LAST_PASS=10 | SURVIVING=11 | NEXT_ROLE=Asset Hunter

---

## UI Research — Pass 11 | 2026-03-04 | Asset Hunter

### Finding
- **UI Toolkit for Advanced Unity Developers (e-book)**: Free community e-book on GitHub covering advanced UI Toolkit patterns for Unity 6. Topics include custom controls, data binding, performance optimization, and architecture patterns.
  Link: https://github.com/unity-e-book/UIToolkit
  Checklist: ✅ Unity 6 focused ✅ Advanced patterns ✅ N/A (learning resource) ✅ Comprehensive written guide ✅ Free

- **DebugUI** (AnnulusGames/DebugUI): Framework for building runtime debugging tools using UI Toolkit. Shows patterns for creating runtime UI panels, property inspectors, and interactive controls — useful as architecture reference for how to build game UI panels in UI Toolkit.
  Link: https://github.com/AnnulusGames/DebugUI
  Checklist: ✅ Unity UI Toolkit based ✅ Runtime panel patterns ✅ N/A (dev tool) ✅ GitHub docs ✅ Free

- **Unity App UI** (`com.unity.dt.app-ui`): Official Unity package providing MVVM architecture, theming, localization, and pre-built components on top of UI Toolkit. Now at v1.1.0. The closest to an official "framework layer" for UI Toolkit. Includes a comprehensive theming system with dark/light modes and customizable design tokens.
  Link: https://docs.unity3d.com/Packages/com.unity.dt.app-ui@1.1/manual/theming.html
  Checklist: ✅ Unity official package ✅ MVVM + theming + components ✅ Theme system could implement terminal look ✅ Official docs at v1.1 ✅ Free (Unity package)

### Surviving findings (cumulative)
- Unity UI Toolkit (UXML + USS)
- 1bit_UI Pixel Pack ($9, itch.io)
- Ultimate Oldschool PC Font Pack (free)
- Retrofuturistic Computer SFX (~$16)
- Keyboard & Mouse SFX Pack (free)
- VHS UI EFFECT (~$15, Asset Store)
- Freesound CRT Recordings (free)
- QuizU sample project (free, Asset Store)
- UI Toolkit Debugger + Live Reload (built-in)
- TSS theming system (built-in)
- Official Migration Guide (docs)
- UI Toolkit Advanced e-book (free, GitHub)
- Unity App UI v1.1 (free, official)

### Killed this pass
- DebugUI — useful reference but too niche; QuizU and the e-book cover the same ground better for game UI

### Resume token
UI_LAST_PASS=11 | SURVIVING=13 | NEXT_ROLE=Critic

---

## UI Research — Pass 12 | 2026-03-04 | Critic

### Finding
- **Critique target: Unity App UI v1.1** — While officially maintained, App UI is designed for enterprise/tool UIs, not games. Its component library (buttons, dropdowns, sliders) has its own visual style that would fight against Spellwright's terminal aesthetic. You'd spend more time overriding its design tokens than building from scratch with raw USS. The MVVM pattern adds architectural overhead that Spellwright's EventBus-based architecture doesn't need. Spellwright has 5 screens with straightforward state — MVVM is overkill.

- **Critique target: QuizU sample** — Good for learning patterns but it's a quiz game (static questions, simple state). Spellwright's encounter screen is significantly more complex (tile board, real-time guess processing, animated reveals, dual input modes). QuizU's patterns transfer at a high level but won't help with the hard parts.

- **Assessment of remaining survivors**: The list has stabilized. The core recommendation is clear: UI Toolkit + USS theming + 1bit_UI sprites + audio packs. The learning resources (migration guide, e-book, debugger) support the migration path. No more framework hunting needed.

### Surviving findings (cumulative)
- Unity UI Toolkit (UXML + USS)
- 1bit_UI Pixel Pack ($9, itch.io)
- Ultimate Oldschool PC Font Pack (free)
- Retrofuturistic Computer SFX (~$16)
- Keyboard & Mouse SFX Pack (free)
- VHS UI EFFECT (~$15, Asset Store)
- Freesound CRT Recordings (free)
- UI Toolkit Debugger + Live Reload (built-in)
- TSS theming system (built-in)
- Official Migration Guide (docs)
- UI Toolkit Advanced e-book (free, GitHub)

### Killed this pass
- Unity App UI — enterprise/tool UI focus, fights terminal aesthetic, MVVM overkill for Spellwright
- QuizU — too simple to be a meaningful architectural reference for Spellwright's complexity

### Resume token
UI_LAST_PASS=12 | SURVIVING=11 | NEXT_ROLE=Framework Researcher

---

## UI Research — Pass 13 | 2026-03-04 | Framework Researcher

### Finding
- **UI Toolkit for Spellwright — Architecture Mapping**: How Spellwright's 5 screens would map to UI Toolkit:

  | Current (uGUI) | UI Toolkit Equivalent |
  |---|---|
  | MainMenuUI (Canvas + panels) | `MainMenu.uxml` + `MainMenu.uss` + `MainMenuController.cs` |
  | MapUI (wave/stats/nodes) | `MapScreen.uxml` + `MapScreen.uss` + `MapController.cs` |
  | EncounterUI (tile board + input) | `Encounter.uxml` + `Encounter.uss` + `EncounterController.cs` |
  | ShopUI (items + purchase) | `Shop.uxml` + `Shop.uss` + `ShopController.cs` |
  | ResultUI (stats + continue) | `Result.uxml` + `Result.uss` + `ResultController.cs` |

  Shared theming via `terminal-theme.tss` importing `variables.uss` (colors, fonts, spacing) + `components.uss` (buttons, panels, text styles). Each screen is a `UIDocument` on a GameObject, shown/hidden via `display: none`/`display: flex`.

- **Key USS variables for Spellwright's terminal theme**:
  ```
  :root {
      --terminal-green: #00FF00;
      --terminal-dark-green: #003300;
      --terminal-bg: #0A0A0A;
      --terminal-border: #1A3A1A;
      --font-terminal: url("VT323-SDF.asset");
      --font-size-sm: 14px;
      --font-size-md: 18px;
      --font-size-lg: 24px;
      --spacing-sm: 4px;
      --spacing-md: 8px;
      --spacing-lg: 16px;
  }
  ```

- **DOTween compatibility note**: UI Toolkit elements are NOT GameObjects, so DOTween can't tween them directly. Options: (1) Use USS transitions for most animations (hover, state changes), (2) Write thin C# wrappers that use `schedule.Execute()` for frame-by-frame property animation, (3) Use `element.experimental.animation` API. The existing UIAnimator and ButtonHoverEffect MonoBehaviours would need rewriting.

### Surviving findings (cumulative)
- Unity UI Toolkit (UXML + USS)
- 1bit_UI Pixel Pack ($9, itch.io)
- Ultimate Oldschool PC Font Pack (free)
- Retrofuturistic Computer SFX (~$16)
- Keyboard & Mouse SFX Pack (free)
- VHS UI EFFECT (~$15, Asset Store)
- Freesound CRT Recordings (free)
- UI Toolkit Debugger + Live Reload (built-in)
- TSS theming system (built-in)
- Official Migration Guide (docs)
- UI Toolkit Advanced e-book (free, GitHub)

### Killed this pass
- None (architecture mapping pass, no new findings to evaluate)

### Resume token
UI_LAST_PASS=13 | SURVIVING=11 | NEXT_ROLE=Asset Hunter

---

## UI Research — Pass 14 | 2026-03-04 | Asset Hunter

### Finding
- **Final asset consolidation** — After 14 passes, the asset recommendations are stable. Here is the definitive shopping list:

  **Must-buy (total: ~$40)**:
  | Asset | Price | Purpose |
  |---|---|---|
  | 1bit_UI Pixel Pack | $9 | Complete UI sprite kit (panels, buttons, toggles, bars) — tint green |
  | VHS UI EFFECT | ~$15 | Per-element scanlines + glitch (complements fullscreen CRT shader) |
  | Retrofuturistic Computer SFX | ~$16 | Button beeps, error sounds, data processing, machine hum |

  **Free essentials**:
  | Asset | Purpose |
  |---|---|
  | Ultimate Oldschool PC Font Pack | Authentic IBM/DOS terminal fonts (200+ options) |
  | Keyboard & Mouse SFX Pack | Letter typing feedback, input sounds |
  | Freesound CRT Recordings | CRT boot/shutdown, 60Hz ambient hum, degauss |

### Surviving findings (cumulative)
- Unity UI Toolkit (UXML + USS) — framework
- 1bit_UI Pixel Pack ($9) — sprites
- Ultimate Oldschool PC Font Pack (free) — fonts
- Retrofuturistic Computer SFX (~$16) — audio
- Keyboard & Mouse SFX Pack (free) — audio
- VHS UI EFFECT (~$15) — per-element shaders
- Freesound CRT Recordings (free) — ambient audio
- UI Toolkit Debugger + Live Reload — dev tools
- TSS theming system — theming
- Official Migration Guide — learning
- UI Toolkit Advanced e-book — learning

### Killed this pass
- None (consolidation pass)

### Resume token
UI_LAST_PASS=14 | SURVIVING=11 | NEXT_ROLE=Critic

---

## UI Research — Pass 15 | 2026-03-04 | Critic (Final)

### Finding
- **Final evaluation**: All 11 surviving findings pass the checklist. The framework recommendation (UI Toolkit) is strongly supported by: (a) native flexbox via Yoga engine, (b) USS styling identical to CSS syntax, (c) TSS theming for the terminal design system, (d) built-in debugger matching Chrome DevTools, (e) live reload for rapid iteration, (f) Unity's future direction with active development. The asset recommendations are complementary and non-overlapping. The learning resources cover the migration path.

- **One concern acknowledged**: UI Toolkit is not yet Unity's official "recommended" runtime system (uGUI still holds that title per the comparison page). However, for Spellwright specifically — a 2D screen-space game with text-heavy UI, no Timeline animations, already using DOTween (which needs rewiring regardless) — UI Toolkit's current feature set is more than sufficient. The CSS-like workflow is a major productivity win for a web developer.

- **Risk mitigation**: Migrate ONE screen first (MainMenuUI — simplest, ~3 elements) as a pilot. If it works well, continue with MapUI, then ShopUI, then ResultUI, leaving EncounterUI (most complex) for last. Keep uGUI screens functional during migration — both systems can coexist in the same scene.

### Surviving findings (final)
- Unity UI Toolkit (UXML + USS) — framework
- 1bit_UI Pixel Pack ($9) — sprites
- Ultimate Oldschool PC Font Pack (free) — fonts
- Retrofuturistic Computer SFX (~$16) — audio
- Keyboard & Mouse SFX Pack (free) — audio
- VHS UI EFFECT (~$15) — per-element shaders
- Freesound CRT Recordings (free) — ambient audio
- UI Toolkit Debugger + Live Reload — dev tools
- TSS theming system — theming
- Official Migration Guide — learning
- UI Toolkit Advanced e-book — learning

### Killed this pass
- None (all survivors validated)

### Resume token
UI_LAST_PASS=15 | SURVIVING=11 | NEXT_ROLE=COMPLETE

---

## UI RESEARCH SUMMARY

### Recommended framework
**Unity UI Toolkit (UXML + USS)** — The clear winner for a web developer building a retro game. USS is literally CSS syntax (same selectors, same box model, same flexbox via Yoga engine). UXML is simpler HTML. Theme Style Sheets (TSS) enable a global terminal design system with CSS variables. The built-in debugger works like Chrome DevTools. Live Reload gives hot-reload-like iteration speed. It's free, Unity-maintained, and the future of Unity UI. For someone coming from React/HTML/CSS, the learning curve is 2-3 weeks to full proficiency, with layout and styling productive on day one.

### Recommended asset packs

1. **1bit_UI Pixel Pack** — $9, https://piiixl.itch.io/1bit-ui
   Complete monochrome pixel UI kit (panels, buttons, toggles, sliders, progress bars, icons, score screens). Tint green with USS `tint()` or `background-color` and you have an instant terminal aesthetic. The single most impactful visual upgrade.

2. **VHS UI EFFECT** — ~$15, https://assetstore.unity.com/packages/vfx/vhs-ui-effect-stylized-glitch-scanline-shaders-for-ui-320186
   Per-element scanlines and glitch distortion (not fullscreen). Glitch text on wrong guesses, add scanlines to specific panels, VHS interference during boss encounters. Complements the existing fullscreen CRT shader.

3. **Retrofuturistic Computer SFX** — ~$16, https://www.bluezone-corporation.com/packs/retrofuturistic-computer-sound-effects
   123 sounds purpose-built for retro data terminals: button beeps, error tones, data processing, machine hum, malfunction effects. Covers all UI audio needs in one pack.

**Free bonuses**: Ultimate Oldschool PC Font Pack (200+ authentic CRT fonts), Keyboard & Mouse SFX Pack (typing feedback), Freesound CRT Recordings (boot sounds, 60Hz hum ambience).

### Migration path

1. **Create the terminal theme first** — Write `variables.uss` with all CRT green colors, font references, spacing tokens. Write `components.uss` with `.panel`, `.button`, `.label-title`, `.label-body` classes. Create `terminal-theme.tss` importing both. This is your design system — do this BEFORE touching any screens.

2. **Pilot: MainMenuUI** — Simplest screen (~3 elements: title, start button, version). Create `MainMenu.uxml` + `MainMenu.uss` + `MainMenuController.cs`. Add a `UIDocument` component alongside the existing Canvas. Verify CRT post-processing applies to UI Toolkit elements. Verify input works. Delete the old Canvas version once confirmed.

3. **MapUI** — Second simplest. Stats display, wave counter, node buttons. Good test of flexbox layout for the stat bar and node grid.

4. **ShopUI** — Tests list/grid layout with dynamic content (tome items, prices). Good test of data binding patterns.

5. **ResultUI** — Score display, continue button. Straightforward.

6. **EncounterUI (last)** — Most complex: tile board grid, guessed letters display, input mode toggle, terminal prompt. Migrate this last when you're confident with UI Toolkit patterns. The tile board is the hardest part — may benefit from a Custom Control (`[UxmlElement] TileBoard`).

7. **Remove uGUI dependencies** — Once all screens are migrated, remove Canvas components, EventSystem (UI Toolkit has its own), and uGUI-specific scripts. Keep DOTween for non-UI animations but replace UIAnimator/ButtonHoverEffect with USS transitions.

### Quick wins

1. **Add the 1bit_UI sprite pack + green tinting TODAY** — Even without migrating frameworks, import the 1bit_UI sprites into your current uGUI setup, slice them as 9-patch, and replace your current panel/button backgrounds. Set `Image.color = new Color(0, 1, 0)` on each. Instant visual upgrade, zero architectural change, ~2 hours of work.

2. **Add CRT audio ambience** — Download the free Freesound CRT recordings (60Hz hum loop, power-on/off). Add the hum as a looping AudioSource on your CRT camera. Play power-on when entering encounters, power-off when leaving. Add the free Keyboard SFX pack for letter input feedback. ~1 hour of work, massive atmosphere improvement.

3. **Install an Oldschool PC Font** — Download the Ultimate Oldschool PC Font Pack (free). Pick an IBM VGA or EGA font. Generate an SDF font asset. Try it as an alternative to VT323 on the tile board or encounter prompt. The authentic DOS font look may be a significant visual upgrade. ~30 minutes to evaluate.
