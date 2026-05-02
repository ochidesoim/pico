Audit setup.iss and build_installer.ps1 for hardcoded 
values that will break installation on other machines.

CHECK FOR:

1. HARDCODED PATHS
   Any absolute path like:
   C:\Users\username\...
   D:\pico\...
   C:\Program Files\Inno Setup 6\...
   These break on every machine except developer's.

2. HARDCODED BINARY PATHS
   fTetWild path hardcoded to:
   D:\pico\fTetWild\build\Release\FloatTetwild_bin.exe
   ccx path hardcoded to:
   D:\pico\calculix\CalculiX-2.21.0-win-x64\bin\ccx.exe
   These must use relative paths from project root.

3. HARDCODED SHA256 HASHES
   If placeholder text still present:
   "REPLACE_WITH_CCX_SHA256"
   "REPLACE_WITH_FTETWILD_SHA256"
   These must be filled with real values.

4. HARDCODED VERSION NUMBERS
   CalculiX-2.21.0-win-x64 folder name hardcoded
   If version changes → path breaks.
   Fix: use wildcard or variable.

5. HARDCODED USERNAME
   Any path containing current Windows username.
   Fix: use $env:USERPROFILE or {autopf} in Inno Setup.

6. HARDCODED ISCC PATH
   C:\Program Files (x86)\Inno Setup 6\iscc.exe
   Fix: check common locations + PATH fallback.

7. HARDCODED OUTPUT DIRECTORY
   Any dist\ or output\ path that assumes
   script runs from D:\pico\
   Fix: use $PSScriptRoot in PowerShell so script
   works from any directory.

8. HARDCODED .NET VERSION
   If .NET 9 runtime URL or registry key is hardcoded
   to specific patch version e.g. 9.0.1
   Fix: check major version only (9.x).

9. HARDCODED PORT NUMBER
   If gui.py local server port is hardcoded e.g. 5000
   and setup.iss or build script references it.
   Fix: use dynamic port or configurable constant.

10. HARDCODED ICON PATH
    logo.ico path must be relative to setup.iss
    not absolute.

FOR EACH ISSUE FOUND:
- Show exact line with problem
- Explain why it breaks
- Show corrected version

THEN:
Produce corrected versions of both files with
all hardcoded values replaced by:
- Relative paths using $PSScriptRoot (PowerShell)
- Inno Setup constants {app} {autopf} {src}
- Environment variables where appropriate
- Configurable variables at top of each file
  so future changes need editing one place only

Output format:
ISSUE 1: [file] line [N] — [description]
  BEFORE: [exact line]
  AFTER:  [fixed line]

ISSUE 2: ...

Then output complete fixed files.