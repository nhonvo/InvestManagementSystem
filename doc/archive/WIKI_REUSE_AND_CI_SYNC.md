---
description: Plan to reuse the GitHub Wiki repo and update CI to keep it in sync with the docs in this repository (documentation only).
type: reference
status: draft
version: 1.0
tags: [wiki, documentation, github-actions, ci, docusaurus, inventoryalert]
last_updated: 2026-04-30
---

# GitHub Wiki Reuse + CI Sync Plan (InventoryManagementSystem)

Target GitHub Wiki (UI):

- `https://github.com/<owner>/<repo>/wiki` (example: `https://github.com/nhonvo/InventoryManagementSystem/wiki`)

Important note: GitHub Wikis are backed by a **separate git repository**:

- `https://github.com/<owner>/<repo>.wiki.git` (example: `https://github.com/nhonvo/InventoryManagementSystem.wiki.git`)

This document proposes how to **reuse** that wiki repo and how to **update CI** so changes in this repository can be published to the GitHub Wiki automatically.

No CI changes are implemented in this task (doc-only).

---

## 1) What we have today

- Wiki source site in this repo: `InventoryAlert.Wiki/` (Docusaurus)
  - primary authoring folder: `InventoryAlert.Wiki/docs/`
  - build output: `InventoryAlert.Wiki/build/` (static website)
- Current CI for docs: `.github/workflows/deploy-wiki.yml`
  - builds Docusaurus and deploys to **GitHub Pages** (not GitHub Wiki).

So today you effectively have:

- “nice website docs” = GitHub Pages (Docusaurus)
- “GitHub Wiki” = separate, currently not auto-synced

---

## 2) Choose the canonical source

Two valid strategies:

### Strategy A (recommended): Canonical = Docusaurus docs; Wiki = export/sync

- Author docs in `InventoryAlert.Wiki/docs/`.
- CI exports Markdown to the GitHub Wiki repo.
- Keep `.github/workflows/deploy-wiki.yml` (Pages) if you still want the Docusaurus site.

### Strategy B: Canonical = GitHub Wiki repo; Docusaurus optional

- Author directly in the GitHub wiki repo (via UI or git).
- Optionally mirror back into this repo (harder to automate cleanly).

This plan assumes Strategy A because it keeps “doc-as-code” together with the application source.

---

## 3) Export model: Docusaurus → GitHub Wiki

GitHub Wiki Markdown constraints to consider:

- Docusaurus frontmatter (`---`) may render as plain text in GitHub Wiki.
- Docusaurus MDX/shortcodes/admonitions may not render.

Recommended export rules:

1) Strip frontmatter blocks.
2) Convert `InventoryAlert.Wiki/docs/index.md` to `Home.md` in the wiki repo (GitHub wiki landing page).
3) Decide how to map paths:
   - **Flat pages (recommended)**: `06-background-jobs/workers.md` → `06-background-jobs-workers.md`
   - Keep links consistent by rewriting relative links during export.
4) Keep Mermaid diagrams (GitHub supports Mermaid in Markdown in most contexts; validate in the wiki UI).

Output folder suggestion in CI:

- `artifacts/wiki-export/` (generated)

---

## 4) Manual sync (local workflow)

One-time setup:

```bash
git clone https://github.com/<owner>/<repo>.wiki.git _wiki
```

Then copy exported `.md` files into `_wiki/`, commit, push:

```bash
cd _wiki
git add .
git commit -m "Sync wiki from InventoryAlert.Wiki/docs"
git push
```

Tip: do not copy Docusaurus `build/` output to the GitHub wiki repo. Keep the wiki as source Markdown only.

---

## 5) CI sync (GitHub Actions) — recommended workflow design

Create a new workflow (example filename):

- `.github/workflows/sync-github-wiki.yml`

Trigger:

- on push to `main`
- only when `InventoryAlert.Wiki/docs/**` changes (and optionally `InventoryAlert.Wiki/sidebars.ts`)

Authentication:

- Use a PAT stored in a secret like `WIKI_PUSH_TOKEN`
  - In practice, `GITHUB_TOKEN` often does not have sufficient permission to push to the `.wiki.git` repo.

High-level steps:

1) Checkout this repository.
2) Generate a GitHub-wiki-compatible export into `artifacts/wiki-export/`.
3) Checkout the wiki repo into `artifacts/wiki-repo/`.
4) Copy export output into the wiki repo working tree.
5) Commit and push if there are changes.

Workflow skeleton (illustrative only):

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

      # 1) Export docs => artifacts/wiki-export
      - name: Export wiki markdown
        run: |
          mkdir -p artifacts/wiki-export
          # TODO: implement export script later (strip frontmatter + rewrite links + map Home.md)
          # e.g. node ./scripts/export-wiki.mjs

      # 2) Checkout GitHub Wiki repo
      - name: Checkout wiki repo
        run: |
          git clone "https://x-access-token:${WIKI_PUSH_TOKEN}@github.com/${GITHUB_REPOSITORY}.wiki.git" artifacts/wiki-repo
        env:
          WIKI_PUSH_TOKEN: ${{ secrets.WIKI_PUSH_TOKEN }}
          GITHUB_REPOSITORY: ${{ github.repository }}

      # 3) Copy, commit, push
      - name: Publish
        run: |
          rsync -a --delete artifacts/wiki-export/ artifacts/wiki-repo/
          cd artifacts/wiki-repo
          git status --porcelain
          if [ -n \"$(git status --porcelain)\" ]; then
            git config user.name \"github-actions[bot]\"
            git config user.email \"github-actions[bot]@users.noreply.github.com\"
            git add .
            git commit -m \"Sync wiki\"
            git push
          fi
```

---

## 6) How this co-exists with the current `deploy-wiki.yml`

You have two publishing targets:

- GitHub Pages (Docusaurus site) → keep `.github/workflows/deploy-wiki.yml`
- GitHub Wiki (markdown) → add `.github/workflows/sync-github-wiki.yml`

This gives:

- “pretty docs” for reading (Pages)
- “quick editing / discoverability” for contributors (Wiki)

If you only want GitHub Wiki going forward, you can retire `deploy-wiki.yml` later.

