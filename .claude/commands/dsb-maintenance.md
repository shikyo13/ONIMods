---
description: "Check game updates, GitHub issues, workshop comments, and build health for DuplicantStatusBar"
allowed-tools: "Bash,Read,Write,Edit,Grep,Glob,Agent"
---

# /dsb-maintenance — Automated Mod Maintenance Sweep

Run a full maintenance check for DuplicantStatusBar. Reports game updates, new issues, workshop feedback, and build health. Updates `DuplicantStatusBar/maintenance-state.json` with timestamps after each run.

**Automation mode**: This command is designed to run fully unattended. NEVER prompt for confirmation — make decisions autonomously. If something is ambiguous, log it to the report and move on. If a fix is needed, make the fix, commit it, and report what you did.

**Unattended invocation**: To auto-exit after the sweep, invoke with:
`claude --print "/dsb-maintenance"`

**Version control is MANDATORY**: Every change MUST be committed. Never leave dirty working state. Commit before building, commit after fixing. Always return to `master` branch when done.

## Configuration

Read `DuplicantStatusBar/maintenance-state.json` for:
- `lastVerifiedBuildId` — last ONI Steam build ID we verified against
- `lastDllTimestamp` — last known modification time of Assembly-CSharp.dll
- `lastIssueCheck` — ISO timestamp of last GitHub issue check
- `lastWorkshopCheck` — ISO timestamp of last workshop comment check
- `workshopItemId`, `steamOwnerId` — for Steam comment API
- `githubRepo` — for `gh` CLI

## Steps

Execute ALL steps, then present a single summary report at the end.

### 0. Previous Run Context

Read the previous report to establish continuity:
```
DuplicantStatusBar/maintenance-report.txt
```

Parse the **Action Items** section from the last report. For each open item (P0, P1, P2):

- **Draft PRs / fix branches**: Check if the PR was merged, closed, or is still open:
  ```bash
  gh pr list --repo shikyo13/ONIMods --state all --search "KEYWORD" --json number,title,state --limit 5
  ```
- **Open GitHub issues**: Check current status (still open? closed since last run? new comments?):
  ```bash
  gh issue view NUMBER --repo shikyo13/ONIMods --json state,comments --jq '{state: .state, commentCount: (.comments | length)}'
  ```
- **Feature requests logged as P2**: Check if an issue was created since last run (may have been created manually)

Build a **Previous Items Status** list. Items that are now resolved get marked as such. Items still open carry forward. This list appears in the final report as a section before the new findings.

### 1. Game Version Check

Read the Steam app manifest to get the current build ID:
```bash
grep '"buildid"' "D:/SteamLibrary/steamapps/appmanifest_457140.acf"
```

Also check Assembly-CSharp.dll modification time:
```bash
stat -c '%Y' "D:/SteamLibrary/steamapps/common/OxygenNotIncluded/OxygenNotIncluded_Data/Managed/Assembly-CSharp.dll"
```

Compare both against stored values. If EITHER changed:
- Flag as **GAME UPDATED** with old→new build IDs
- Check if the mod still builds: `dotnet build DuplicantStatusBar/DuplicantStatusBar.csproj`
- If build fails, this is **CRITICAL** — the game update broke the mod. Attempt an automated fix:
  1. Create branch `maintenance/YYYY-MM-DD-gameupdate`
  2. Read the build errors
  3. Use the `/decompile` command or `re-orchestrator` MCP tools to decompile the changed types
  4. Fix the compilation errors based on API changes
  5. Commit each fix with a descriptive message
  6. Rebuild and verify
  7. If fix succeeds, report what changed. If fix fails after 3 attempts, report as CRITICAL/NEEDS-HUMAN.
- If build succeeds, note as "build still passes but runtime testing needed"
- Use the `/decompile` command or `re-orchestrator` MCP tools to check if any types we patch have changed signatures. Our patched types are listed in `DuplicantStatusBar/Patches/GamePatches.cs` — read that file and decompile each patched type to check for API drift. Report any signature changes even if the build still passes (runtime breakage risk).

### 2. GitHub Issues

Fetch open issues newer than `lastIssueCheck`:
```bash
gh issue list --repo shikyo13/ONIMods --state open --json number,title,createdAt,labels,body --limit 20
```

For each issue:
- Classify as: **bug**, **feature request**, **question**, or **other**
- Extract the key ask in 1 sentence
- Note if it relates to bionic dupes, performance, or compatibility
- If it's a clear bug with enough info to reproduce, create a TODO in the report with the fix approach

### 3. Workshop Comments

Fetch recent comments via Steam's render API (substitute OWNER_ID and ITEM_ID from maintenance-state.json):
```bash
curl -s "https://steamcommunity.com/comment/PublishedFile_Public/render/OWNER_ID/ITEM_ID/?start=0&count=50"
```

Parse the JSON response — comments are in `comments_html`. Extract author, timestamp, and text using this Python snippet:
```python
import sys, json, re, html
data = json.load(sys.stdin)
for author, ts, body in re.findall(
    r'commentthread_author_link.*?<bdi>(.*?)</bdi>.*?'
    r'data-timestamp="(\d+)".*?'
    r'commentthread_comment_text[^>]*>(.*?)</div>',
    data.get('comments_html',''), re.DOTALL):
    body = html.unescape(re.sub(r'<[^>]+>', '', re.sub(r'<br\s*/?>', ' ', body))).strip()
    print(json.dumps({"author": author, "ts": int(ts), "text": body}))
```

Filter to comments newer than `lastWorkshopCheck`. Exclude comments from "ZeroTheAbsolute" (that's us) from the **new comments count**, but still read them for thread context.

For each non-author comment:
- Classify as: **bug report**, **feature request**, **praise**, **question**, or **translation request**
- Extract key info in 1 sentence
- Flag anything that sounds like a crash or compatibility issue as **HIGH PRIORITY**

#### Thread context and fix-acknowledged detection

After parsing all comments, scan for **fix-acknowledged** items: cases where a user reported a bug/issue and our reply (from "ZeroTheAbsolute") contains fix-related keywords: `fixed`, `will fix`, `fix in`, `patched`, `resolved`, `addressed`, `next update`, `next version`.

To detect these, look at our replies and check the preceding non-author comment(s) for bug/issue content. When a match is found, flag the original user comment as **fix-acknowledged**. These items flow into Step 4d for issue tracking regardless of whether they predate `lastWorkshopCheck` - the point is to ensure every acknowledged bug has a GitHub issue.

### 4. Triage Actionable Items

After steps 2-3, decide which items are **actionable** — meaning they describe a concrete bug, crash, or compatibility issue with enough detail to investigate. Feature requests and praise are NOT actionable (just log them in the report).

**Actionable criteria** (must meet at least one):
- User reports a crash, error, or exception
- User reports a specific broken behavior ("X doesn't work when Y")
- User reports compatibility issue with another mod or DLC
- Game update caused build failure or API drift (from step 1)

For each actionable item, do the following IN ORDER:

#### 4a. Check for existing GitHub issue
```bash
gh issue list --repo shikyo13/ONIMods --state open --search "KEYWORD" --json number,title --limit 5
```
If an existing issue covers it, add a comment linking the workshop feedback and skip to the next item.

#### 4b. Create a GitHub issue

Classify the item and pick a label: `bug`, `enhancement`, or `question`. Then create with the label inline:
```bash
gh issue create --repo shikyo13/ONIMods --title "TITLE" --label "bug" --body "BODY"
```

Use this template for the body:
```
## Source
{Workshop comment by AUTHOR on DATE / GitHub issue #N / Game update BUILD_ID}

## Problem
{1-2 sentence description of what's broken or requested}

## Reproduction
{Steps if available, or "Reported by user - needs reproduction"}

## Investigation Notes
{Your analysis of the likely cause - which file, which code path, why it might fail}
{For feature requests: where in the codebase this would be implemented}

## Proposed Fix
{Your recommended approach, with specific files and line numbers}

---
*Auto-created by DSB maintenance sweep on YYYY-MM-DD*
```

#### 4c. Investigate and propose a fix branch

For **bugs and crashes only** (not feature requests), create a fix branch and investigate:

1. Create and switch to a fix branch:
```bash
git checkout -b fix/SHORT-DESCRIPTION
```

2. Read the relevant source files to understand the problem. Use Grep/Read to trace the code path.

3. Write the fix. Keep changes minimal — fix the bug, nothing else.

4. Commit the fix:
```bash
git add SPECIFIC_FILES
git commit -m "fix: DESCRIPTION

Closes #ISSUE_NUMBER"
```

5. Build and verify:
```bash
dotnet build DuplicantStatusBar/DuplicantStatusBar.csproj
```

6. If build passes, the branch is ready for human review and testing. Push it:
```bash
git push -u origin fix/SHORT-DESCRIPTION
```

7. Create a draft PR (draft = not auto-mergeable, needs human testing):
```bash
gh pr create --repo shikyo13/ONIMods --title "fix: DESCRIPTION" --body "BODY" --draft
```

Use this PR body template:
```
## Fixes #ISSUE_NUMBER

## Problem
{What was broken}

## Root Cause
{What code was wrong and why}

## Fix
{What you changed and why this approach}

## Testing Needed
- [ ] Launch ONI, verify {specific scenario}
- [ ] Check with {DLC/no DLC} if relevant
- [ ] Check {edge case}

---
*Auto-generated by DSB maintenance sweep. Needs human testing before merge.*
```

8. Switch back to master:
```bash
git checkout master
```

**IMPORTANT**: Do NOT merge the PR. Do NOT deploy. The fix branch + draft PR is the deliverable. The human reviews, tests in-game, and merges.

#### 4d. Ensure workshop items are tracked in GitHub

All bug reports and specific feature requests from workshop comments should have a corresponding GitHub issue. This applies to:
- **Fix-acknowledged** items from Step 3 (bug reports where we replied with fix keywords)
- **Specific feature requests** that describe a concrete, actionable change (e.g., "let me scale portraits below 36px"). Skip vague wishes or praise.

For each item:

1. **Search all issues** (open AND closed) for a matching issue:
```bash
gh issue list --repo shikyo13/ONIMods --state all --search "KEYWORDS" --json number,title,state --limit 5
```
Use the most distinctive keywords from the description, not generic terms.

2. **If matching closed issue exists**: Already tracked. Log in report as "tracked (issue #N, closed)." Skip.

3. **If matching open issue exists and it's a fix-acknowledged bug**: Search git log for the fix commit, add the commit reference as a comment, and close the issue:
```bash
git log --oneline --all --grep="KEYWORD" --since="2 months ago"
gh issue comment NUMBER --repo shikyo13/ONIMods --body "Fix identified in commit HASH. Closing."
gh issue close NUMBER --repo shikyo13/ONIMods
```

4. **If no matching issue exists**:
   a. For **bugs**: search git log for the fix commit:
   ```bash
   git log --oneline --all --grep="KEYWORD" --since="2 months ago"
   ```
   b. Create a GitHub issue using the template and `--label` flag from Step 4b, noting the workshop comment source
   c. If fix commit found: immediately close the issue with a comment referencing the commit hash
   e. If no fix commit found but we acknowledged it: leave the issue open
   f. Feature requests: always leave open
   g. Log in report as "retroactively tracked (created issue #N)"

If the fix is too complex or ambiguous (you'd need to guess at behavior), skip the branch and just document your investigation in the GitHub issue. Add a comment like:
```
Investigation notes from automated sweep:
- Likely root cause: {analysis}
- Affected files: {list}
- Complexity: {simple/moderate/complex}
- Recommended approach: {description}
- Reason for not auto-fixing: {why it needs human judgment}
```

### 5. Build Health

```bash
dotnet build DuplicantStatusBar/DuplicantStatusBar.csproj 2>&1
```

Report: pass/fail, warning count, error details if any.
If build fails and this is NOT due to a game update (step 1), attempt to fix:
1. Create branch `maintenance/YYYY-MM-DD-buildfix`
2. Read errors, fix code
3. Commit the fix
4. Rebuild to verify
5. Push and create draft PR

### 6. Code Health (quick scan)

Search for potential issues:
- `TODO`, `FIXME`, `HACK`, `TEMP` comments in `DuplicantStatusBar/**/*.cs`
- Any `try {} catch {}` with empty catch blocks (swallowed exceptions). **Exclude** `DSBLog.cs` (can't log a log failure) and `DiagnosticDump.cs` (try-load asset patterns). Only report catches in business logic.
- Files over 500 lines (complexity creep)

### 7. Update State & Commit

After all checks complete, make sure you are on `master` branch:
```bash
git checkout master
```

Update `maintenance-state.json` with:
- New `lastVerifiedBuildId` and `lastDllTimestamp` (if game updated AND build passed)
- New `lastIssueCheck` and `lastWorkshopCheck` timestamps (current UTC time)
- New `lastCleanBuild` (if build passed)

**Always commit the state update**:
```bash
git add DuplicantStatusBar/maintenance-state.json DuplicantStatusBar/maintenance-report.txt
git commit -m "chore: update maintenance state (YYYY-MM-DD sweep)"
```

### 8. Summary Report

Present findings in this format:

```
## DSB Maintenance Report — {date}

### Previous Items
{for each item carried from last report:}
- [RESOLVED] {description} - {how it was resolved: PR merged, issue closed, etc.}
- [STILL OPEN] {description} - {current status update}
- [STALE] {description} - {no progress, escalate or drop}
{if no previous items: "First run / no prior action items."}

### Game Version
{status}: Build ID {id} - {changed/unchanged since last check}
{if changed: build result, API drift findings, fixes applied}

### GitHub Issues ({count} new)
{numbered list with classification and 1-line summary}

### Workshop Comments ({count} new, excluding ours)
{numbered list with classification and 1-line summary}
{HIGH PRIORITY items listed first}

### Triage Actions Taken
{for each actionable item:}
- Created issue #{N}: {title}
- Created fix branch: fix/{name} → draft PR #{N}
- OR: Documented investigation in issue #{N} (needs human judgment)
- Retroactively tracked bug: created issue #{N}, closed with fix commit HASH
- Retroactively tracked feature: created issue #{N} (enhancement)
- Already tracked: issue #{N} (closed)

### Build Health
{pass/fail, warnings, fixes applied}

### Code Health
{TODO count, empty catches, large files}

### Action Items
P0 — automated fix ready for review (see draft PR #N on branch fix/...)
P1 — needs human attention (issue created, investigation documented)
P2 — nice to have (feature requests, polish — logged only)
```

**Always** write this report to `DuplicantStatusBar/maintenance-report.txt` (overwrite previous).
Commit the report file alongside the state update.

### 9. Email Report

After committing, send the report as a formatted HTML email using the `mcp__gmail__send_email` tool.

**To**: shikyo13@gmail.com
**Subject**: `DSB Maintenance Report - YYYY-MM-DD`
**mimeType**: `text/html`

Build the `htmlBody` from the report data using this template structure:

```html
<!DOCTYPE html>
<html>
<head><meta charset="utf-8"><meta name="color-scheme" content="dark"><style>:root{color-scheme:dark}</style></head>
<body style="margin:0;padding:0;background:#1a1a2e;font-family:'Segoe UI',Roboto,sans-serif">
<div style="max-width:640px;margin:0 auto;padding:24px">

  <!-- Header -->
  <div style="background:linear-gradient(135deg,#16213e,#0f3460);border-radius:12px 12px 0 0;padding:24px 32px;border-bottom:3px solid #e94560">
    <h1 style="margin:0;color:#e8e8e8;font-size:22px;font-weight:600">DSB Maintenance Report</h1>
    <p style="margin:6px 0 0;color:#8899aa;font-size:14px">{date} - Automated Sweep</p>
  </div>

  <!-- Status Banner - use appropriate color -->
  <!-- Green (#06d6a0) = all clear, Yellow (#ffd166) = attention needed, Red (#ef476f) = critical -->
  <div style="background:#06d6a0;padding:12px 32px;color:#1a1a2e;font-weight:600;font-size:14px">
    {ALL CLEAR / ATTENTION NEEDED / CRITICAL} - {one-line summary}
  </div>

  <!-- Body -->
  <div style="background:#16213e;padding:24px 32px;border-radius:0 0 12px 12px">

    <!-- Previous Items card (only if there are prior action items) -->
    <div style="background:#1a1a3e;border-radius:8px;padding:16px 20px;margin-bottom:16px;border-left:3px solid #ffd166">
      <h2 style="margin:0 0 8px;color:#ffd166;font-size:15px;text-transform:uppercase;letter-spacing:1px">Previous Items</h2>
      <!-- Status badges: RESOLVED=#06d6a0, STILL OPEN=#ffd166, STALE=#ef476f -->
      <div style="padding:4px 0;color:#c8c8d8;font-size:14px">
        <span style="background:#06d6a0;color:#1a1a2e;padding:1px 8px;border-radius:10px;font-size:12px;font-weight:600">RESOLVED</span> {description}
      </div>
    </div>

    <!-- Each section as a card -->
    <div style="background:#1a1a3e;border-radius:8px;padding:16px 20px;margin-bottom:16px;border-left:3px solid #0f3460">
      <h2 style="margin:0 0 8px;color:#e94560;font-size:15px;text-transform:uppercase;letter-spacing:1px">Game Version</h2>
      <p style="margin:0;color:#c8c8d8;font-size:14px;line-height:1.6">{content}</p>
    </div>

    <!-- Repeat card pattern for: GitHub Issues, Workshop Comments, Triage Actions, Build Health, Code Health -->
    <!-- For list items use: -->
    <div style="padding:4px 0;color:#c8c8d8;font-size:14px;line-height:1.6">
      <span style="color:#e94560;font-weight:600">bug</span> | #2 Bar not appearing after update
    </div>
    <!-- Label colors: bug=#ef476f, enhancement=#06d6a0, question=#ffd166 -->

    <!-- Action Items section - only if there are items -->
    <div style="background:#1a1a3e;border-radius:8px;padding:16px 20px;margin-bottom:16px;border-left:3px solid #e94560">
      <h2 style="margin:0 0 8px;color:#e94560;font-size:15px;text-transform:uppercase;letter-spacing:1px">Action Items</h2>
      <!-- P0 items: red badge, P1: yellow, P2: gray -->
      <div style="padding:4px 0;color:#c8c8d8;font-size:14px">
        <span style="background:#ef476f;color:#fff;padding:1px 8px;border-radius:10px;font-size:12px;font-weight:600">P0</span> {description}
      </div>
    </div>

  </div>

  <!-- Footer -->
  <p style="text-align:center;color:#556677;font-size:12px;margin-top:16px">
    Auto-generated by DSB maintenance sweep - DuplicantStatusBar
  </p>

</div>
</body>
</html>
```

**Adapt the template to actual data** - only include sections that have content. If a section has zero items, either omit it or show a single "None" line. Pick the status banner color based on the worst priority item found.

Use `multipart/alternative` as the mimeType so the plain-text `body` field serves as fallback. Set `body` to the same report content as `maintenance-report.txt` (plain markdown).
