---
description: CI approach to publish this repo's docs into the GitHub Wiki for InventoryManagementSystem.
type: reference
status: draft
version: 1.0
tags: [github-wiki, ci, github-actions, documentation, inventoryalert]
last_updated: 2026-04-30
---

# Publish GitHub Wiki from CI (InventoryManagementSystem)

Target wiki UI:

- `https://github.com/<owner>/<repo>/wiki` (example: `https://github.com/nhonvo/InventoryManagementSystem/wiki`)

GitHub wiki is backed by a separate git repository:

- `https://github.com/<owner>/<repo>.wiki.git` (example: `https://github.com/nhonvo/InventoryManagementSystem.wiki.git`)

This document describes a CI approach (GitHub Actions) to publish markdown pages into the wiki repo from this codebase.

Doc-only: no workflow is added by this task.

---

## 1) What we publish (recommended)

Canonical source (recommended):

- Author docs in this repo under `InventoryAlert.Wiki/docs/` (Docusaurus source)

Publish target:

- Exported plain Markdown into the `.wiki.git` repository

Why export?

- Docusaurus frontmatter/MDX may not render in GitHub wiki.
- Wiki navigation differs from Docusaurus sidebars.

---

## 2) Required GitHub secret

Use a Personal Access Token (PAT) with access to the target repo:

- Secret name: `WIKI_PUSH_TOKEN`

Notes:

- `GITHUB_TOKEN` often cannot push to the separate `.wiki.git` repo.
- Prefer a fine-scoped token and rotate it.

---

## 3) Export rules (Docusaurus → GitHub Wiki)

Minimum required transforms:

1) **Frontmatter stripping**
   - remove leading `--- ... ---` blocks
2) **Home page mapping**
   - `InventoryAlert.Wiki/docs/index.md` → `Home.md`
3) **Link rewriting**
   - rewrite relative links so they still work in the wiki
4) **Filename mapping**
   - GitHub wiki supports folders, but many teams prefer flat names to avoid broken links.
   - Choose one:
     - **Foldered**: keep directory structure (e.g. `04-execution-flows/alert-rule-creation.md`)
     - **Flat**: convert to `04-execution-flows-alert-rule-creation.md`

Recommended: keep folders (simpler link rewriting), but ensure links include correct relative paths.

---

## 4) Proposed workflow: `.github/workflows/sync-github-wiki.yml`

Triggers:

- on push to `main`
- only when wiki source changes:
  - `InventoryAlert.Wiki/docs/**`
  - (optional) `InventoryAlert.Wiki/sidebars.ts`

High-level stages:

1) Checkout this repo
2) Export markdown into an artifacts folder
3) Clone the wiki repo
4) Copy artifacts into the wiki repo
5) Commit & push (only if changes exist)

Illustrative workflow skeleton:

```yaml
name: Sync GitHub Wiki

on:
  push:
    branches: ["main"]
    paths:
      - "InventoryAlert.Wiki/docs/**"
      - ".github/workflows/sync-github-wiki.yml"
  workflow_dispatch:

jobs:
  sync:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Export docs to GitHub Wiki format
        run: |
          mkdir -p artifacts/wiki-export
          # TODO: implement export script
          # Example: node ./scripts/export-wiki.mjs

      - name: Clone wiki repository
        run: |
          git clone "https://x-access-token:${WIKI_PUSH_TOKEN}@github.com/${GITHUB_REPOSITORY}.wiki.git" artifacts/wiki-repo
        env:
          WIKI_PUSH_TOKEN: ${{ secrets.WIKI_PUSH_TOKEN }}
          GITHUB_REPOSITORY: ${{ github.repository }}

      - name: Sync + publish
        run: |
          rsync -a --delete artifacts/wiki-export/ artifacts/wiki-repo/
          cd artifacts/wiki-repo
          if [ -n "$(git status --porcelain)" ]; then
            git config user.name "github-actions[bot]"
            git config user.email "github-actions[bot]@users.noreply.github.com"
            git add .
            git commit -m "Sync wiki from InventoryAlert.Wiki/docs"
            git push
          fi
```

---

## 5) Export script design (recommended)

Add a repo script (example):

- `scripts/export-wiki.mjs`

Responsibilities:

- find all `InventoryAlert.Wiki/docs/**/*.md`
- strip frontmatter
- rewrite links (`](../foo/bar.md)` etc.)
- write output to `artifacts/wiki-export/`
- ensure `Home.md` is generated

Keep it deterministic:

- stable ordering
- stable output filenames

---

## 6) Validation checklist (before enabling CI)

- Wiki renders Mermaid blocks correctly (validate one page).
- No secrets/tokens/PII in published pages.
- Links work after export.
- Home page (`Home.md`) shows a clear table of contents and “getting started”.

---

## 7) Relationship to existing Pages deployment

This repo already has a Pages deploy workflow:

- `.github/workflows/deploy-wiki.yml` (Docusaurus → GitHub Pages)

You can run both:

- GitHub Pages = best reading UX
- GitHub Wiki = best contributor UX (inline edits, discoverability)

