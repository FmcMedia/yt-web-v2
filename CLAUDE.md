# yt-web-v2 — Claude Session Guide

**Repo:** https://github.com/FmcMedia/yt-web-v2  
**Local path:** `~/yt-web-v2/`  
**Owner:** Rick Amos

---

## What This Is

Self-hosted YouTube downloader UI. Paste a URL, pick quality, download. Supports video, audio-only, and transcript-only (SRT → Markdown). Single-page app backed by FastAPI + yt-dlp.

---

## Stack

| Layer | Choice |
|-------|--------|
| Backend | FastAPI + Uvicorn (Python 3.12) |
| Download engine | yt-dlp Python API |
| Media processing | ffmpeg (static binary via mwader/static-ffmpeg) |
| Frontend | Tailwind CSS (CDN) + vanilla JS, Jinja2 templates |
| Icons | Font Awesome 6 |
| Persistence | `history/history.json` (named Docker volume) |
| Concurrency | Python threading + per-job cancel events |
| Container | Docker + docker-compose, non-root appuser |

---

## Project Structure

```
yt-web-v2/
├── app/
│   ├── main.py              # All backend logic — routes, yt-dlp, job store
│   ├── templates/index.html # Entire UI — single file, Tailwind + vanilla JS
│   ├── downloads/           # Local dev only — volume mount in Docker
│   ├── transcripts/         # Local dev only — volume mount in Docker
│   └── history/             # history.json lives here (named volume mount)
├── Dockerfile
├── docker-compose.yml
├── requirements.txt
├── PLAN.md                  # Original design doc
├── README.md                # User-facing docs + changelog
├── CLAUDE.md                # This file
└── docs/
    └── active-task.md       # Current work in progress
```

---

## Docker

**Named volumes:** `yt-web-downloads`, `yt-web-transcripts`, `yt-web-history`  
**Port:** 8000  
**Storage limit:** `STORAGE_LIMIT_GB` env var (default 20)

**Redeploy locally (Mac with Colima):**
```bash
DOCKER_HOST="unix://${HOME}/.colima/default/docker.sock" docker compose -f ~/yt-web-v2/docker-compose.yml up --build -d
```

**Portainer:** Stack pulls from GitHub `main` branch. Use "Re-pull image and redeploy" — don't manually delete containers.

**Caddy / HTTPS:** `Caddyfile` has the server IP hardcoded (`192.168.1.2`). Change it if the server IP changes. Caddy root CA cert is served at `http://<ip>/caddy-root.crt` for Android installation. Caddy data lives in `yt-web-caddy-data` named volume.

---

## Key Gotchas

- `writeautomaticsub: True` — correct yt-dlp Python API option (NOT `writeautomaticsubs`)
- `STORAGE_LIMIT_GB` must be parsed with `float()` before `int()` to support fractional values like `0.1`
- yt-dlp JS runtime: configured via `/home/appuser/.config/yt-dlp/config` file inside container (`--js-runtimes node:/usr/bin/node`) — not via Python API options
- `history.json` path: `BASE_DIR / "history" / "history.json"` — the `history/` subdirectory is what gets volume-mounted
- Subtitle lang: explicit list `["en", "en-orig", "en-US", "en-GB"]` — regex patterns don't work reliably in Docker

---

## Versioning

Tag each release: `git tag vX.Y && git push origin main --tags`  
Update `README.md` changelog with each version.  
Version displayed in page footer (hardcoded in `index.html`).

Current version: **v2.6**
