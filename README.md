# yt-web

A self-hosted web UI for downloading **DRM-free** YouTube content — videos, audio, and transcripts — powered by [yt-dlp](https://github.com/yt-dlp/yt-dlp) and FastAPI.

> **Legal notice**: Only use this for content you have the legal right to download — your own uploads, public domain, Creative Commons-licensed videos, etc. Downloading copyrighted content without permission may violate YouTube's Terms of Service and applicable law. DRM-protected content is explicitly blocked.

---

## Features

- **Paste and analyze** — paste a YouTube URL to see the title, thumbnail, channel, duration, and view count before downloading
- **Quality selection** — choose from all available resolutions (4K down to 144p), audio-only, or transcript-only
- **Transcript downloads** — fetches English captions (manual or auto-generated), converts to clean formatted Markdown with word count and read time
- **Live progress** — real-time progress bar, download speed, and ETA for active downloads
- **Cancel downloads** — cancel any active download mid-stream
- **DRM detection** — automatically detects and blocks download attempts on protected content
- **Persistent history** — source URL and video title survive server restarts, shown in the file list
- **Search and sort** — filter completed files by name or title; sort by date, name, or size
- **Disk usage indicator** — bar showing disk used/free and a breakdown by videos vs. transcripts
- **In-browser player** — watch downloaded videos directly in the browser without leaving the page
- **File management** — download or delete individual files from the UI

---

## Requirements

- Python 3.11+
- [ffmpeg](https://ffmpeg.org/) (required for merging video + audio streams)
- [yt-dlp](https://github.com/yt-dlp/yt-dlp)

Or run everything in Docker (recommended).

---

## Quick Start

### With Docker (recommended)

```bash
git clone https://github.com/FmcMedia/yt-web-v2.git
cd yt-web-v2
docker compose up --build
```

Open **https://your-server-ip** (Caddy handles HTTPS automatically).

Downloaded files are persisted in named Docker volumes.

### HTTPS + PWA on Android

The included Caddy reverse proxy provides HTTPS via a local self-signed certificate, which is required for Android to install the app properly (not just a Chrome shortcut).

**One-time Android setup:**

1. Edit `Caddyfile` — replace `192.168.1.2` with your server's actual local IP
2. Deploy with `docker compose up --build -d`
3. On Android, open `http://your-server-ip/caddy-root.crt` in Chrome — it will download a certificate file
4. Open Android **Settings → Security → Install a certificate → CA certificate** and install it
5. Now open `https://your-server-ip` in Chrome → three-dot menu → **Add to Home Screen** — installs as a real app

### Without Docker

```bash
git clone https://github.com/FmcMedia/yt-web-v2.git
cd yt-web-v2
python3 -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
cd app
uvicorn main:app --host 0.0.0.0 --port 8000 --reload
```

Open **http://localhost:8000**

> You must have `ffmpeg` installed and available on your PATH for high-quality downloads (video + audio merge).

---

## Usage

1. Paste a YouTube URL into the input and click **Analyze**
2. The video preview loads with metadata and available quality options
3. Select a quality/format from the list
4. Optionally check **Subtitles** or **Embed thumbnail**
5. Click **Download** — a progress card appears in Active Downloads
6. When complete, the file appears in Completed Videos (or Transcripts)
7. Click **Download** on any row to save to your machine, or **watch** to play in-browser

### Transcript downloads

Select **Transcript only (English)** from the format list. The app fetches English captions (manual or auto-generated), strips timestamps and formatting, and saves a clean `.md` file with:
- H1 title
- Word count and estimated read time
- Source YouTube URL
- Paragraphed body text (approximately 5 sentences per paragraph)

---

## API

The backend exposes a simple REST API:

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/info?url=` | Fetch video metadata and available formats |
| `POST` | `/api/download` | Start a download job |
| `GET` | `/api/status/{job_id}` | Poll job progress |
| `DELETE` | `/api/download/{job_id}` | Cancel an active download |
| `GET` | `/api/files` | List downloaded video/audio files |
| `DELETE` | `/api/files/{filename}` | Delete a video file |
| `GET` | `/api/transcripts` | List downloaded transcript files |
| `DELETE` | `/api/transcripts/{filename}` | Delete a transcript file |
| `GET` | `/api/disk` | Disk usage stats |
| `GET` | `/downloads/{filename}` | Serve a video/audio file |
| `GET` | `/transcripts/{filename}` | Serve a transcript file |
| `GET` | `/watch?file=` | In-browser video player page |

---

## Project Structure

```
yt-web-v2/
├── app/
│   ├── main.py              # FastAPI app — routes, yt-dlp logic, job store
│   ├── templates/
│   │   └── index.html       # Single-page UI (Tailwind CSS + vanilla JS)
│   ├── downloads/           # Downloaded video/audio files (volume mount target)
│   ├── transcripts/         # Downloaded transcript .md files
│   └── history.json         # Persistent download history (auto-created)
├── requirements.txt
├── PLAN.md                  # Original design document
└── README.md
```

---

## Tech Stack

| Layer | Choice |
|-------|--------|
| Language | Python 3.11+ |
| Web framework | FastAPI + Uvicorn |
| Download engine | yt-dlp (Python API) |
| Media processing | ffmpeg |
| Frontend | Tailwind CSS (CDN) + vanilla JS |
| Icons | Font Awesome 6 |
| Fonts | Inter + Space Grotesk (Google Fonts) |
| Persistence | JSON file (`history.json`) |
| Concurrency | Python `threading` with per-job cancel events |

---

## Security

- Intended for **localhost or trusted LAN use only**
- No authentication by default — do not expose publicly without adding auth
- YouTube URL validation is enforced server-side before any yt-dlp call
- Filenames are sanitized by yt-dlp's output template system
- DRM content is detected and blocked before any download attempt

---

## Changelog

### v2.5 (2026-06-24)
- PWA (Progressive Web App) support — installable on Android and iOS via "Add to Home Screen"
- `manifest.json` with app name, theme color, and icons
- Service worker (`sw.js`) — network-first, API/download calls never cached
- Apple touch icon and apple-mobile-web-app meta tags for iOS

### v2.4 (2026-06-24)
- Mobile-responsive UI — fluid padding, stacked search/sort, full-width buttons on small screens
- Mobile-optimized URL input (correct keyboard type on iOS/Android)
- Version number displayed in page footer

### v2.3.1 (2026-06-23)
- Fixed transcript downloads not working in Docker — yt-dlp Python API option is `writeautomaticsub` (not `writeautomaticsubs`); the wrong name silently skipped subtitle downloads
- Added Node.js to Docker image for yt-dlp JS runtime support
- Added yt-dlp config file in container to register Node.js as the JS runtime

### v2.3 (2026-06-23)
- Storage limit enforcement — downloads check available space before starting
- If limit would be exceeded, an interactive modal prompts the user to select files to delete before proceeding
- Live counter in modal shows how much space selected deletions would free
- Delete & Download button enabled only once enough space is selected
- `STORAGE_LIMIT_GB` env var (default 20) configures the cap — supports fractional values (e.g. `0.1` for 100MB testing)
- `/api/info` now returns `filesize_estimate` per format option so the UI can calculate space needed before download starts
- `/api/disk` now returns `storage_used`, `storage_limit`, and `storage_free` against the configured cap
- Disk usage bar in UI updated to show usage against the storage limit rather than total host disk

### v2.2 (2026-06-23)
- Switched to multi-stage Docker build using `mwader/static-ffmpeg` — copies static ffmpeg binary instead of `apt install`, reducing image content size significantly
- Colima support documented for Mac CLI-only Docker usage
- Host port changed to `8000:8000`

### v2.1 (2026-06-23)
- Added `Dockerfile` and `docker-compose.yml` for containerised deployment
- Non-root `appuser` inside container
- `downloads/` and `transcripts/` mounted as host volumes so files survive rebuilds
- `restart: unless-stopped` policy

### v2.0 (2026-06-23)
Initial versioned release.

- Full download UI with quality/format selection
- Transcript-only download mode with SRT → Markdown conversion
- Live progress tracking (progress bar, speed, ETA)
- Cancel active downloads
- Persistent download history (source URL + title survive restarts)
- Disk usage indicator with per-directory breakdown
- Search and sort for completed files (filter by name/title, sort by date/name/size)
- In-browser video player (`/watch`)
- DRM detection and blocking
- File management (download, delete) from UI
- Favicon

---

## Roadmap (v2.6+)

- **Trusted HTTPS for PWA install** — Android Chrome requires HTTPS with a trusted cert to install as a real app rather than a browser shortcut; Caddy reverse proxy is already included in the compose file, pending a viable cert solution for local network use
- Download queue with concurrency limit
- Playlist support
- Basic authentication
- Browser push notifications on completion
- Retry failed downloads
