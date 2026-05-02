# VeloForge Login Page — Implementation Phases

## OVERVIEW
Static but visually complete login page.
Matches VeloForge aerospace aesthetic exactly.
Auth not functional — UI only for now.
Can be wired to real backend later.

Stack:
  Next.js (existing project)
  Same design system as landing page
  Bierika font · #0A0A0B · #FF6B00

---

## PHASE 1 — Page scaffold
### New file only. Nothing else touched.

Create: src/app/login/page.tsx

Full viewport · dark background #0A0A0B
No navbar · no stats bar · no 3D canvas
Standalone page completely

Layout:
  Two columns on desktop:

  LEFT (45%):
    Full height dark panel
    VeloForge branding
    Tagline
    Background: subtle grid pattern
    or faint 3D model screenshot
    as static image

  RIGHT (55%):
    Login form centered vertically
    background: #0A0A0B

  Mobile (< 768px):
    Single column
    Left panel hidden
    Form full width

Route: localhost:3000/login

Success criteria:
  ✓ Page loads at /login
  ✓ Two column layout desktop
  ✓ Single column mobile
  ✓ No existing pages broken

---

## PHASE 2 — Left branding panel

LEFT PANEL content:

  Top:
    VELOFORGE wordmark
    Bierika · VELO white · FORGE #FF4500
    font-size: clamp(36px, 4vw, 56px)

    Above wordmark:
    "COMPUTATIONAL ENGINEERING"
    #FF6B00 · 10px · weight 300
    letter-spacing: 0.25em

  Middle:
    Tagline:
    "From parameters to
     validated part.
     Automatically."
    white · 24px · weight 300
    line-height: 1.6

  Below tagline:
    3 stat pills in a row:
    ┌──────────┐ ┌──────────┐ ┌──────────┐
    │ SF 1.847 │ │ 23 iter  │ │ −11.3%   │
    │ validated│ │ to solve │ │ lighter  │
    └──────────┘ └──────────┘ └──────────┘
    border: 1px solid rgba(255,107,0,0.3)
    background: rgba(255,107,0,0.04)
    padding: 12px 16px
    text: #FF6B00 · 11px · uppercase

  Bottom:
    "Trusted by engineers
     building the next generation
     of physical products."
    #FF6B00 · 60% opacity · 12px

  Left border accent:
    2px solid #FF6B00 · 30% opacity
    full height · left edge of panel

Success criteria:
  ✓ Wordmark renders in Bierika
  ✓ Stats pills visible
  ✓ Panel feels like landing page

---

## PHASE 3 — Login form shell

RIGHT PANEL form:

  Max-width: 400px · centered
  
  Header:
    "SIGN IN" · Bierika · white · 28px
    "Access your simulation workspace"
    #FF6B00 · 12px · opacity 70%
    margin-bottom: 32px

  SOCIAL AUTH BUTTONS:

    [ G  Continue with Google ]
    [ ⬡  Continue with GitHub ]

    Button styling both:
      width: 100%
      height: 48px
      background: rgba(255,255,255,0.04)
      border: 1px solid rgba(255,255,255,0.12)
      border-radius: 2px (sharp · aerospace)
      color: white · 14px · weight 500
      display: flex · align-items: center
      gap: 12px
      padding: 0 16px
      cursor: pointer

    Google button:
      Icon: Google SVG logo · 18px
      text: "Continue with Google"
      On hover:
        border-color: rgba(255,255,255,0.3)
        background: rgba(255,255,255,0.08)

    GitHub button:
      Icon: GitHub SVG logo · 18px · white
      text: "Continue with GitHub"
      On hover:
        border-color: rgba(255,107,0,0.5)
        background: rgba(255,107,0,0.06)

  DIVIDER:
    ─────── OR ───────
    line: 1px solid rgba(255,255,255,0.08)
    "OR" · #FF6B00 · 11px · 
    letter-spacing: 0.2em
    centered between lines

Success criteria:
  ✓ Both social buttons visible
  ✓ Hover states work
  ✓ Divider renders cleanly
  ✓ Sharp corners · no border-radius

---

## PHASE 4 — Email + password fields

Below divider:

  EMAIL FIELD:
    Label: "EMAIL ADDRESS"
    · #FF6B00 · 9px · weight 400
    · letter-spacing: 0.15em · uppercase
    · margin-bottom: 6px

    Input:
      type: email
      placeholder: "engineer@company.com"
      width: 100% · height: 48px
      background: rgba(255,255,255,0.04)
      border: 1px solid rgba(255,107,0,0.2)
      border-radius: 2px
      color: white · 14px
      padding: 0 16px
      outline: none

    On focus:
      border-color: #FF6B00
      box-shadow: 0 0 0 3px
        rgba(255,107,0,0.1)

    On error:
      border-color: #FF2200
      shake animation · 0.3s

  PASSWORD FIELD:
    Label: "PASSWORD"
    Same styling as email label

    Input:
      type: password
      placeholder: "••••••••••••"
      Same styling as email input

    Show/hide toggle:
      Eye icon · right side of input
      #FF6B00 · 16px
      Click toggles type
        password ↔ text

  FORGOT PASSWORD:
    Right-aligned below password:
    "Forgot password?"
    #FF6B00 · 12px · underline on hover
    cursor: pointer

  [ SIGN IN ] BUTTON:
    width: 100% · height: 48px
    background: #FF6B00
    color: #0A0A0B · weight 700
    font-size: 13px
    letter-spacing: 0.15em · uppercase
    border: none · border-radius: 2px
    cursor: pointer
    margin-top: 24px

    On hover:
      background: #FF4500
      transform: none (no movement)

    On click (UI only):
      Shows loading state:
        Spinner inside button
        text: "AUTHENTICATING..."
        button disabled
      After 1.5s:
        Shows error state:
        "Invalid credentials"
        · red · below button
        (since not wired to backend)

Success criteria:
  ✓ Email + password fields styled
  ✓ Focus states work
  ✓ Show/hide password works
  ✓ Sign in button loading state
  ✓ Error message appears after 1.5s

---

## PHASE 5 — Sign up section

Below [ SIGN IN ] button:

  SIGN UP LINK:
    "Don't have an account?"
    white · 13px · opacity 60%
    " Request access →"
    #FF6B00 · 13px · weight 600
    cursor: pointer
    On click: shows sign up form
              (toggle · no new page)

  SIGN UP FORM (hidden by default):
    Slides down smoothly when toggled:
      gsap.fromTo(signUpForm,
        { height: 0, autoAlpha: 0 },
        { height: "auto", autoAlpha: 1,
          duration: 0.4,
          ease: "power2.out" }
      )

    Fields:
      FULL NAME input
      EMAIL ADDRESS input
      PASSWORD input
      CONFIRM PASSWORD input

    [ CREATE ACCOUNT ] button:
      Same style as SIGN IN button
      On click:
        Loading state 1.5s
        Then: "Account created.
               Check your email."
        · green · #00FF88

    Already have account?
      "Back to sign in →"
      Collapses form back up

Success criteria:
  ✓ Toggle shows/hides signup form
  ✓ Slide animation smooth
  ✓ All 4 fields present
  ✓ Create account button works
  ✓ Success message shows

---

## PHASE 6 — Aerospace cursor + micro details

CURSOR:
  Same aerospace crosshair cursor
  as in D:\Landing page
  Import existing AerospaceCursor component
  Active on login page too

MICRO DETAILS:

  Top right corner:
    Small system status indicator:
    "● SYSTEM ONLINE"
    · #00FF88 · 9px · opacity 60%
    · dot blinks slowly

  Bottom left:
    "VELOFORGE v1.0.0"
    #FF6B00 · 9px · opacity 30%

  Bottom right:
    "256-bit encrypted"
    + small lock icon
    #FF6B00 · 9px · opacity 30%

  Background texture (left panel):
    Very faint engineering grid:
      SVG grid lines
      stroke: #FF6B00 · opacity 3%
      10px × 10px squares
      covers entire left panel

  Page load animation:
    On mount:
    gsap.from(".login-form > *", {
      autoAlpha: 0,
      y: 16,
      stagger: 0.08,
      duration: 0.5,
      ease: "power2.out"
    })

Success criteria:
  ✓ Aerospace cursor active
  ✓ System status blinks
  ✓ Version label visible
  ✓ Grid texture on left panel
  ✓ Form elements stagger in on load

---

## PHASE 7 — Mobile responsive

At 768px and below:

  Left panel: hidden completely
  Right panel: full width · full height
  Form: max-width 360px · centered

  Top of mobile form:
    Show condensed branding:
    VELOFORGE wordmark · smaller
    "COMPUTATIONAL ENGINEERING"
    margin-bottom: 32px

  All inputs: full width
  Font sizes: slightly larger for touch
  Buttons: height 52px (bigger tap target)

  Social buttons: stack vertically
    Google on top
    GitHub below
    Gap: 12px

gsap.matchMedia():
  isDesktop: "(min-width: 768px)"
  isMobile: "(max-width: 767px)"
  Apply different stagger animations
  per breakpoint

Success criteria:
  ✓ Left panel hidden on mobile
  ✓ VeloForge branding shows at top
  ✓ All inputs full width
  ✓ Buttons larger for touch
  ✓ No horizontal scroll

---

## EXECUTION ORDER

Phase 1 → Page scaffold + routing
Phase 2 → Left branding panel
Phase 3 → Social auth buttons
Phase 4 → Email + password fields
Phase 5 → Sign up toggle section
Phase 6 → Cursor + micro details
Phase 7 → Mobile responsive

Test after EACH phase.
Screenshot after each.
Never skip a phase.

---

## SKILLS TO READ PER PHASE

All phases:
  @frontend-design
  @brand-brief

Phases 4 5 6:
  @gsap-core

Phase 7:
  @gsap-core (matchMedia)

Paste at top of every prompt:
  Before executing read:
  @frontend-design @brand-brief @gsap-core
  Read PROJECT_CONTEXT.md first.
  Do NOT touch any existing pages.
  Only create/modify login page files.