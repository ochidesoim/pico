// ─────────────────────────────────────────────────────────────
// VeloForge — Login Page
// Phases 1-7: Scaffold · Branding · Social Auth · Form Fields
//             Sign-up Toggle · Micro Details · Mobile Responsive
// ─────────────────────────────────────────────────────────────

import { useEffect, useRef, useState, useCallback } from "react";
import Head from "next/head";

// ── Aerospace crosshair cursor (Phase 6) ─────────────────────
function AerospaceCursor() {
  const cursorRef = useRef<HTMLDivElement>(null);
  const pos = useRef({ x: 0, y: 0 });
  const mouse = useRef({ x: 0, y: 0 });
  const [isHovering, setIsHovering] = useState(false);
  const [isClicked, setIsClicked] = useState(false);

  useEffect(() => {
    const onMove = (e: MouseEvent) => {
      mouse.current.x = e.clientX;
      mouse.current.y = e.clientY;
      const t = e.target as HTMLElement;
      setIsHovering(
        window.getComputedStyle(t).cursor === "pointer" ||
        ["button","a","input","label"].includes(t.tagName.toLowerCase()) ||
        t.closest("button") !== null || t.closest("a") !== null
      );
    };
    const onDown = () => setIsClicked(true);
    const onUp   = () => setIsClicked(false);
    window.addEventListener("mousemove", onMove);
    window.addEventListener("mousedown", onDown);
    window.addEventListener("mouseup",   onUp);
    let raf: number;
    const loop = () => {
      pos.current.x += (mouse.current.x - pos.current.x) * 0.12;
      pos.current.y += (mouse.current.y - pos.current.y) * 0.12;
      if (cursorRef.current)
        cursorRef.current.style.transform =
          `translate3d(${pos.current.x}px,${pos.current.y}px,0)`;
      raf = requestAnimationFrame(loop);
    };
    raf = requestAnimationFrame(loop);
    return () => {
      window.removeEventListener("mousemove", onMove);
      window.removeEventListener("mousedown", onDown);
      window.removeEventListener("mouseup",   onUp);
      cancelAnimationFrame(raf);
    };
  }, []);

  const scale  = isClicked ? 1.8 : isHovering ? 1.5 : 1;
  const op     = isHovering || isClicked ? 1 : 0.7;
  const bOff   = isHovering ? -2 : 0;

  return (
    <div ref={cursorRef} style={{ position:"fixed", top:0, left:0, pointerEvents:"none", zIndex:9999 }}>
      <div style={{ position:"relative" }}>
        {/* Ring */}
        <div style={{
          position:"absolute", width:32, height:32,
          border:"1px solid #FF6B00", borderRadius:"50%",
          transform:`translate(-50%,-50%) scale(${scale})`,
          opacity:op, transition:"transform 0.3s cubic-bezier(0.2,0.9,0.3,1),opacity 0.2s",
        }} />
        {/* Dot */}
        <div style={{ position:"absolute", width:2, height:2, background:"#FF6B00", transform:"translate(-50%,-50%)" }} />
        {/* Crosshair */}
        <div style={{ position:"absolute", width:1, height:6, background:"#FF6B00", top:-10, left:"-0.5px" }} />
        <div style={{ position:"absolute", width:1, height:6, background:"#FF6B00", top:4,   left:"-0.5px" }} />
        <div style={{ position:"absolute", width:6, height:1, background:"#FF6B00", top:"-0.5px", left:-10 }} />
        <div style={{ position:"absolute", width:6, height:1, background:"#FF6B00", top:"-0.5px", left:4  }} />
        {/* Brackets */}
        {[
          { top:-16+bOff, left:-16+bOff, borderTop:"1px solid #FF6B00", borderLeft:"1px solid #FF6B00" },
          { top:-16+bOff, left:11-bOff,  borderTop:"1px solid #FF6B00", borderRight:"1px solid #FF6B00" },
          { top:11-bOff,  left:-16+bOff, borderBottom:"1px solid #FF6B00", borderLeft:"1px solid #FF6B00" },
          { top:11-bOff,  left:11-bOff,  borderBottom:"1px solid #FF6B00", borderRight:"1px solid #FF6B00" },
        ].map((s, i) => (
          <div key={i} style={{ position:"absolute", width:5, height:5, opacity:op, transition:"top 0.2s,left 0.2s", ...s }} />
        ))}
      </div>
    </div>
  );
}

// ── Field label ───────────────────────────────────────────────
function FieldLabel({ children }: { children: string }) {
  return (
    <label style={{
      display:"block", color:"#FF6B00", fontSize:"9px", fontWeight:400,
      letterSpacing:"0.15em", textTransform:"uppercase",
      fontFamily:"var(--font-mono)", marginBottom:"6px",
    }}>
      {children}
    </label>
  );
}

// ── Eye icon ─────────────────────────────────────────────────
function EyeIcon({ open }: { open: boolean }) {
  return open ? (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#FF6B00" strokeWidth="2">
      <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/>
      <circle cx="12" cy="12" r="3"/>
    </svg>
  ) : (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#FF6B00" strokeWidth="2">
      <path d="M17.94 17.94A10.07 10.07 0 0112 20c-7 0-11-8-11-8a18.45 18.45 0 015.06-5.94M9.9 4.24A9.12 9.12 0 0112 4c7 0 11 8 11 8a18.5 18.5 0 01-2.16 3.19M1 1l22 22"/>
    </svg>
  );
}

// ── Google SVG ─────────────────────────────────────────────────
const GoogleIcon = () => (
  <svg width="18" height="18" viewBox="0 0 24 24" fill="none">
    <path d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z" fill="#4285F4"/>
    <path d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" fill="#34A853"/>
    <path d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l3.66-2.84z" fill="#FBBC05"/>
    <path d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z" fill="#EA4335"/>
  </svg>
);

// ── GitHub SVG ─────────────────────────────────────────────────
const GitHubIcon = () => (
  <svg width="18" height="18" viewBox="0 0 24 24" fill="white">
    <path d="M12 2C6.477 2 2 6.484 2 12.017c0 4.425 2.865 8.18 6.839 9.504.5.092.682-.217.682-.483 0-.237-.008-.868-.013-1.703-2.782.605-3.369-1.343-3.369-1.343-.454-1.158-1.11-1.466-1.11-1.466-.908-.62.069-.608.069-.608 1.003.07 1.531 1.032 1.531 1.032.892 1.53 2.341 1.088 2.91.832.092-.647.35-1.088.636-1.338-2.22-.253-4.555-1.113-4.555-4.951 0-1.093.39-1.988 1.029-2.688-.103-.253-.446-1.272.098-2.65 0 0 .84-.27 2.75 1.026A9.564 9.564 0 0112 6.844c.85.004 1.705.115 2.504.337 1.909-1.296 2.747-1.027 2.747-1.027.546 1.379.202 2.398.1 2.651.64.7 1.028 1.595 1.028 2.688 0 3.848-2.339 4.695-4.566 4.943.359.309.678.92.678 1.855 0 1.338-.012 2.419-.012 2.747 0 .268.18.58.688.482A10.019 10.019 0 0022 12.017C22 6.484 17.522 2 12 2z"/>
  </svg>
);

// ─────────────────────────────────────────────────────────────
// MAIN PAGE
// ─────────────────────────────────────────────────────────────
export default function LoginPage() {
  // Form state
  const [showPassword,   setShowPassword]   = useState(false);
  const [showSignUp,     setShowSignUp]      = useState(false);
  const [signingIn,      setSigningIn]       = useState(false);
  const [signInError,    setSignInError]     = useState("");
  const [signingUp,      setSigningUp]       = useState(false);
  const [signUpSuccess,  setSignUpSuccess]   = useState(false);
  const [pwError,        setPwError]         = useState(false);
  const [showConfirmPw,  setShowConfirmPw]   = useState(false);

  // ── Auth signal to gui.py embedded server ────────────────────────────────
  // gui.py opens the browser with ?port=XXXX so we know where to POST.
  // In normal Next.js dev mode (no port param) the POST is silently skipped.
  const signalAuth = useCallback(async () => {
    if (typeof window === "undefined") return;
    const params = new URLSearchParams(window.location.search);
    const port = params.get("port");
    if (!port) return;
    try {
      await fetch(`http://localhost:${port}/auth`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ ok: true }),
        // Short timeout so we don't hang if server is gone
        signal: AbortSignal.timeout(3000),
      });
    } catch {
      // Server already closed or not present — ignore
    }
  }, []);

  // Refs for GSAP
  const formRef    = useRef<HTMLDivElement>(null);
  const signupRef  = useRef<HTMLDivElement>(null);

  // Phase 6: GSAP page-load entrance stagger
  useEffect(() => {
    let gsap: typeof import("gsap").gsap;
    (async () => {
      const mod = await import("gsap");
      gsap = mod.gsap;
      if (!formRef.current) return;
      const children = Array.from(formRef.current.children);
      gsap.from(children, {
        autoAlpha: 0,
        y: 16,
        stagger: 0.08,
        duration: 0.5,
        ease: "power2.out",
        clearProps: "all",
      });
    })();
  }, []);

  // Phase 5: GSAP sign-up slide toggle
  const toggleSignUp = async (open: boolean) => {
    const mod  = await import("gsap");
    const gsap = mod.gsap;
    if (!signupRef.current) { setShowSignUp(open); return; }
    if (open) {
      setShowSignUp(true);
      gsap.fromTo(signupRef.current,
        { maxHeight: 0, autoAlpha: 0 },
        { maxHeight: 600, autoAlpha: 1, duration: 0.4, ease: "power2.out" }
      );
    } else {
      gsap.to(signupRef.current, {
        maxHeight: 0, autoAlpha: 0, duration: 0.3, ease: "power2.in",
        onComplete: () => setShowSignUp(false),
      });
    }
  };

  const handleSignIn = async () => {
    setPwError(false);
    setSignInError("");
    setSigningIn(true);
    // Signal gui.py that login is complete (if running inside the exe)
    await signalAuth();
    // In standalone mode (no port param) show a brief success state
    setTimeout(() => {
      setSigningIn(false);
    }, 800);
  };

  const handleSignUp = async () => {
    setSignUpSuccess(false);
    setSigningUp(true);
    await signalAuth();
    setTimeout(() => {
      setSigningUp(false);
      setSignUpSuccess(true);
    }, 800);
  };

  // Social auth buttons also complete the login flow
  const handleSocialAuth = async () => {
    await signalAuth();
  };

  // Shared input style
  const inputWrap: React.CSSProperties = { position:"relative", marginBottom:"16px" };
  const eyeBtn: React.CSSProperties = {
    position:"absolute", right:12, top:"50%", transform:"translateY(-50%)",
    background:"none", border:"none", cursor:"none", padding:0, display:"flex",
  };
  const fieldLabel = (txt: string) => (
    <label style={{
      display:"block", color:"#FF6B00", fontSize:"9px", fontWeight:400,
      letterSpacing:"0.15em", textTransform:"uppercase",
      fontFamily:"var(--font-mono)", marginBottom:"6px",
    }}>{txt}</label>
  );

  return (
    <>
      <Head>
        <title>Sign In — VeloForge</title>
        <meta name="description" content="Sign in to your VeloForge simulation workspace." />
      </Head>

      {/* Phase 6: Aerospace cursor */}
      <AerospaceCursor />

      {/* Phase 6: Top-right system status */}
      <div style={{
        position:"fixed", top:20, right:24, zIndex:100,
        display:"flex", alignItems:"center", gap:6,
      }}>
        <span className="status-dot" />
        <span style={{ color:"rgba(0,255,136,0.6)", fontSize:"9px", fontFamily:"var(--font-mono)", letterSpacing:"0.12em" }}>
          SYSTEM ONLINE
        </span>
      </div>

      {/* Phase 6: Bottom-left version */}
      <div style={{
        position:"fixed", bottom:20, left:24, zIndex:100,
        color:"rgba(255,107,0,0.3)", fontSize:"9px",
        fontFamily:"var(--font-mono)", letterSpacing:"0.1em",
      }}>
        VELOFORGE v1.0.0
      </div>

      {/* Phase 6: Bottom-right encryption */}
      <div style={{
        position:"fixed", bottom:20, right:24, zIndex:100,
        display:"flex", alignItems:"center", gap:5,
        color:"rgba(255,107,0,0.3)", fontSize:"9px",
        fontFamily:"var(--font-mono)", letterSpacing:"0.1em",
      }}>
        <svg width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="rgba(255,107,0,0.3)" strokeWidth="2">
          <rect x="3" y="11" width="18" height="11" rx="2" ry="2"/>
          <path d="M7 11V7a5 5 0 0110 0v4"/>
        </svg>
        256-bit encrypted
      </div>

      {/* ── MAIN LAYOUT ─────────────────────────────────────── */}
      <div style={{
        width:"100vw", height:"100vh",
        background:"var(--color-bg)",
        display:"flex", overflow:"hidden", position:"relative",
      }}>

        {/* ── PHASE 2: LEFT BRANDING PANEL ─────────────────── */}
        <div
          className="login-left-panel"
          style={{
            width:"45%", height:"100%",
            background:"var(--color-bg)",
            borderRight:"1px solid rgba(255,107,0,0.1)",
            flexShrink:0, display:"none",
            flexDirection:"column", justifyContent:"center",
            padding:"64px 56px", position:"relative", overflow:"hidden",
          }}
        >
          {/* Left border accent */}
          <div style={{
            position:"absolute", left:0, top:0, bottom:0, width:2,
            background:"rgba(255,107,0,0.3)",
          }} />

          {/* Grid texture */}
          <svg style={{ position:"absolute", inset:0, width:"100%", height:"100%", pointerEvents:"none" }}>
            <defs>
              <pattern id="vf-grid" width="10" height="10" patternUnits="userSpaceOnUse">
                <path d="M 10 0 L 0 0 0 10" fill="none" stroke="rgba(255,107,0,0.03)" strokeWidth="0.5"/>
              </pattern>
            </defs>
            <rect width="100%" height="100%" fill="url(#vf-grid)" />
          </svg>

          {/* Content */}
          <div style={{ position:"relative", zIndex:1 }}>
            <p style={{
              color:"#FF6B00", fontSize:10, fontWeight:300,
              letterSpacing:"0.25em", fontFamily:"var(--font-mono)",
              textTransform:"uppercase", margin:"0 0 16px",
            }}>
              COMPUTATIONAL ENGINEERING
            </p>

            <div style={{
              fontFamily:"var(--font-bierika)", fontWeight:400,
              fontSize:"clamp(36px,4vw,56px)", lineHeight:1,
              letterSpacing:"0.04em", margin:"0 0 40px",
            }}>
              <span style={{ color:"#FFFFFF" }}>VELO</span>
              <span style={{ color:"#FF4500" }}>FORGE</span>
            </div>

            <p style={{
              color:"#FFFFFF", fontSize:24, fontWeight:300,
              lineHeight:1.6, fontFamily:"var(--font-mono)", margin:"0 0 40px",
            }}>
              From parameters to<br/>
              validated part.<br/>
              Automatically.
            </p>

            {/* Stat pills */}
            <div style={{ display:"flex", gap:10, flexWrap:"wrap", marginBottom:48 }}>
              {[
                { v:"SF 1.847", l:"validated" },
                { v:"23 iter",  l:"to solve"  },
                { v:"−11.3%",  l:"lighter"   },
              ].map(s => (
                <div key={s.v} style={{
                  border:"1px solid rgba(255,107,0,0.3)",
                  background:"rgba(255,107,0,0.04)",
                  padding:"12px 16px",
                  display:"flex", flexDirection:"column", gap:4,
                }}>
                  <span style={{ color:"#FF6B00", fontSize:11, fontWeight:600, letterSpacing:"0.1em", textTransform:"uppercase", fontFamily:"var(--font-mono)" }}>
                    {s.v}
                  </span>
                  <span style={{ color:"rgba(255,107,0,0.6)", fontSize:9, letterSpacing:"0.15em", textTransform:"uppercase", fontFamily:"var(--font-mono)" }}>
                    {s.l}
                  </span>
                </div>
              ))}
            </div>

            <p style={{
              color:"rgba(255,107,0,0.6)", fontSize:12,
              fontFamily:"var(--font-mono)", lineHeight:1.7, margin:0,
            }}>
              Trusted by engineers<br/>
              building the next generation<br/>
              of physical products.
            </p>
          </div>
        </div>

        {/* ── PHASES 3–7: RIGHT PANEL ──────────────────────── */}
        <div
          className="login-right-panel"
          style={{
            flex:1, height:"100%",
            background:"var(--color-bg)",
            display:"flex", alignItems:"center", justifyContent:"center",
            padding:"40px 24px", overflowY:"auto",
          }}
        >
          <div
            ref={formRef}
            className="login-form"
            style={{ width:"100%", maxWidth:400 }}
          >
            {/* Phase 7: Mobile branding (hidden on desktop) */}
            <div className="mobile-branding" style={{ display:"none", marginBottom:32 }}>
              <p style={{ color:"#FF6B00", fontSize:9, letterSpacing:"0.25em", textTransform:"uppercase", fontFamily:"var(--font-mono)", margin:"0 0 8px" }}>
                COMPUTATIONAL ENGINEERING
              </p>
              <div style={{ fontFamily:"var(--font-bierika)", fontSize:32, lineHeight:1, letterSpacing:"0.04em" }}>
                <span style={{ color:"#FFF" }}>VELO</span>
                <span style={{ color:"#FF4500" }}>FORGE</span>
              </div>
            </div>

            {/* Phase 3: Header */}
            <div style={{ marginBottom:32 }}>
              <h1 style={{
                fontFamily:"var(--font-bierika)", fontSize:28, color:"#FFFFFF",
                margin:"0 0 6px", letterSpacing:"0.08em", textTransform:"uppercase", fontWeight:400,
              }}>
                SIGN IN
              </h1>
              <p style={{ color:"rgba(255,107,0,0.7)", fontSize:12, fontFamily:"var(--font-mono)", margin:0, letterSpacing:"0.04em" }}>
                Access your simulation workspace
              </p>
            </div>

            {/* Phase 3: Google */}
            <button onClick={handleSocialAuth} className="social-btn google-btn" style={{
              width:"100%", height:48,
              background:"rgba(255,255,255,0.04)",
              border:"1px solid rgba(255,255,255,0.12)",
              borderRadius:2, color:"#FFF",
              fontSize:14, fontWeight:500,
              display:"flex", alignItems:"center", gap:12,
              padding:"0 16px", marginBottom:10,
              transition:"border-color 0.2s,background 0.2s",
            }}>
              <GoogleIcon /> Continue with Google
            </button>

            {/* Phase 3: GitHub */}
            <button onClick={handleSocialAuth} className="social-btn github-btn" style={{
              width:"100%", height:48,
              background:"rgba(255,255,255,0.04)",
              border:"1px solid rgba(255,255,255,0.12)",
              borderRadius:2, color:"#FFF",
              fontSize:14, fontWeight:500,
              display:"flex", alignItems:"center", gap:12,
              padding:"0 16px", marginBottom:24,
              transition:"border-color 0.2s,background 0.2s",
            }}>
              <GitHubIcon /> Continue with GitHub
            </button>

            {/* Phase 3: Divider */}
            <div style={{ display:"flex", alignItems:"center", gap:12, marginBottom:24 }}>
              <div style={{ flex:1, height:1, background:"rgba(255,255,255,0.08)" }} />
              <span style={{ color:"#FF6B00", fontSize:11, fontFamily:"var(--font-mono)", letterSpacing:"0.2em" }}>OR</span>
              <div style={{ flex:1, height:1, background:"rgba(255,255,255,0.08)" }} />
            </div>

            {/* Phase 4: Email */}
            <div style={inputWrap}>
              {fieldLabel("Email Address")}
              <input
                id="email"
                type="email"
                placeholder="engineer@company.com"
                className="vf-input"
                style={{ paddingRight:16 }}
              />
            </div>

            {/* Phase 4: Password */}
            <div style={inputWrap}>
              {fieldLabel("Password")}
              <div style={{ position:"relative" }}>
                <input
                  id="password"
                  type={showPassword ? "text" : "password"}
                  placeholder="••••••••••••"
                  className={`vf-input${pwError ? " error" : ""}`}
                />
                <button style={eyeBtn} onClick={() => setShowPassword(p => !p)} type="button" aria-label="Toggle password">
                  <EyeIcon open={showPassword} />
                </button>
              </div>
            </div>

            {/* Phase 4: Forgot password */}
            <div style={{ textAlign:"right", marginBottom:0, marginTop:-8 }}>
              <span style={{
                color:"#FF6B00", fontSize:12, fontFamily:"var(--font-mono)",
                cursor:"pointer", textDecoration:"underline", opacity:0.8,
              }}>
                Forgot password?
              </span>
            </div>

            {/* Phase 4: Sign-in button */}
            <button
              className="signin-btn"
              onClick={handleSignIn}
              disabled={signingIn}
              id="sign-in-btn"
            >
              {signingIn ? (
                <><div className="spinner" /> AUTHENTICATING...</>
              ) : "SIGN IN"}
            </button>

            {/* Phase 4: Error message */}
            {signInError && (
              <p style={{ color:"#FF2200", fontSize:11, fontFamily:"var(--font-mono)", marginTop:8, letterSpacing:"0.06em" }}>
                {signInError}
              </p>
            )}

            {/* Phase 5: Sign-up toggle */}
            <div style={{ marginTop:24, display:"flex", alignItems:"center", gap:4, flexWrap:"wrap" }}>
              <span style={{ color:"rgba(255,255,255,0.6)", fontSize:13, fontFamily:"var(--font-mono)" }}>
                Don&apos;t have an account?
              </span>
              <button
                onClick={() => toggleSignUp(!showSignUp)}
                style={{ background:"none", border:"none", color:"#FF6B00", fontSize:13, fontWeight:600, fontFamily:"var(--font-mono)", cursor:"none" }}
              >
                {showSignUp ? "Back to sign in →" : "Request access →"}
              </button>
            </div>

            {/* Phase 5: Sign-up form (slide) */}
            <div
              ref={signupRef}
              className="signup-wrapper"
              style={{ maxHeight:0, overflow:"hidden", opacity:0 }}
            >
              {showSignUp && (
                <div style={{ paddingTop:24 }}>
                  {/* Full Name */}
                  <div style={inputWrap}>
                    {fieldLabel("Full Name")}
                    <input id="fullname" type="text" placeholder="Jane Engineer" className="vf-input" style={{ paddingRight:16 }} />
                  </div>
                  {/* Email */}
                  <div style={inputWrap}>
                    {fieldLabel("Email Address")}
                    <input id="su-email" type="email" placeholder="engineer@company.com" className="vf-input" style={{ paddingRight:16 }} />
                  </div>
                  {/* Password */}
                  <div style={inputWrap}>
                    {fieldLabel("Password")}
                    <div style={{ position:"relative" }}>
                      <input id="su-password" type={showConfirmPw ? "text" : "password"} placeholder="••••••••••••" className="vf-input" />
                      <button style={eyeBtn} onClick={() => setShowConfirmPw(p => !p)} type="button">
                        <EyeIcon open={showConfirmPw} />
                      </button>
                    </div>
                  </div>
                  {/* Confirm Password */}
                  <div style={inputWrap}>
                    {fieldLabel("Confirm Password")}
                    <input id="su-confirm" type="password" placeholder="••••••••••••" className="vf-input" style={{ paddingRight:16 }} />
                  </div>
                  {/* Create account button */}
                  <button
                    className="signin-btn"
                    onClick={handleSignUp}
                    disabled={signingUp}
                    id="create-account-btn"
                  >
                    {signingUp ? (
                      <><div className="spinner" /> CREATING ACCOUNT...</>
                    ) : "CREATE ACCOUNT"}
                  </button>
                  {/* Success message */}
                  {signUpSuccess && (
                    <p style={{ color:"#00FF88", fontSize:11, fontFamily:"var(--font-mono)", marginTop:10, letterSpacing:"0.06em" }}>
                      Account created. Check your email.
                    </p>
                  )}
                </div>
              )}
            </div>

          </div>
        </div>
      </div>
    </>
  );
}
