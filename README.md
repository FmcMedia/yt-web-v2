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

Open **http://localhost:8080**

Downloaded files are persisted to `./downloads` on your host machine via volume mount.

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

## Roadmap (v2.1+)

- Download queue with concurrency limit
- Playlist support
- Auto-cleanup of old files
- Basic authentication
- Browser push notifications on completion
- Retry failed downloads
