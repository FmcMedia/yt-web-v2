# yt-web: Dockerized Web UI for DRM-Free YouTube Downloads

**Project Goal**
Create a self-contained Docker container that runs a web server with a clean, front-facing web UI. The UI allows users to:
- Paste YouTube URLs
- Preview video metadata
- Select quality/format
- Download the content using yt-dlp

**Strict Scope**
- **DRM-free content only**. The tool must detect and warn/refuse when only DRM-protected or image-only formats are available (e.g. official movies like "The Matrix").
- Personal / local / trusted network use.
- No circumvention of YouTube protections or DRM.

---

## 1. Goals & Non-Goals

### Goals (MVP)
- One-command Docker run (`docker compose up`)
- Paste URL → see nice preview (title, thumbnail, duration, channel)
- Choose between sensible options:
  - Best quality (video + audio merged)
  - Specific resolution (e.g. 1080p, 720p)
  - Audio only (best or m4a/mp3)
- Start download and see live progress
- List completed downloads with direct browser download links
- Files persist in a mounted volume

### Non-Goals (MVP)
- Full playlist batch downloading (support single videos first)
- User accounts / multi-user
- Public internet deployment (add auth later if needed)
- Bypassing age gates or DRM
- Video editing / post-processing beyond what yt-dlp does

---

## 2. Tech Stack & Justifications

| Layer          | Choice                    | Why |
|----------------|---------------------------|-----|
| Runtime        | Python 3.12               | Native language of yt-dlp, excellent ecosystem |
| Web Framework  | FastAPI + Uvicorn         | Async-friendly, automatic docs, modern, easy to extend |
| Templating     | Jinja2                    | Simple server-side rendering |
| Frontend       | Tailwind CSS (CDN) + HTMX | Modern look with zero build step. HTMX for dynamic parts without heavy JS framework |
| Download Engine| yt-dlp (Python API)       | Most reliable YouTube downloader |
| Media Tools    | ffmpeg (system package)   | Required for best quality merges |
| Progress       | In-memory jobs + polling / SSE | Simple and sufficient for personal use |
| Container      | python:3.12-slim          | Small image, control over deps |
| Orchestration  | docker + docker-compose   | Easy for end users |

**Alternatives Considered**
- Flask (slightly simpler but less future-proof)
- Full React frontend (adds complexity and build step — rejected for MVP)
- Existing projects (yt-dlp-web, alltube, etc.) — we are building from scratch for learning/control

---

## 3. Core Features

### UI / Frontend
- Clean, responsive single-page app (dark mode friendly)
- URL input + big "Analyze" button
- Video info card: thumbnail (large), title, uploader, duration, views
- Format / Quality selector (radio or dropdown groups):
  - "Best (video + audio)"
  - "Best Video + Best Audio (separate merge)"
  - Resolution list (1080p, 720p, 480p, etc. — dynamically from info)
  - "Audio only (best)"
  - "Audio only (m4a / opus)"
- Optional toggles:
  - Download subtitles (if available)
  - Embed thumbnail in file
  - Write info JSON
- "Start Download" button
- Section: **Active Downloads**
  - Progress bar (%, speed, ETA, filename)
  - Cancel button (nice to have)
- Section: **Completed Downloads**
  - Table or cards: filename, size, date, "Download" button, "Delete"
- Status messages, error handling, loading spinners
- Copy direct file URL

### Backend API
- `GET /api/info?url=...` — Extract metadata + non-DRM formats
- `POST /api/download` — Body: `{url, format: "best" | format_id, options...}`
- `GET /api/status/{job_id}` — Progress info
- `GET /api/files` — List of downloadable files with metadata
- `DELETE /api/files/{filename}` — Remove a file
- `GET /downloads/{filename}` — Serve the actual file (with proper Content-Disposition)

### Download Behavior
- Use `yt_dlp.YoutubeDL` Python API (not CLI) for better control
- Progress hooks update shared job state
- Output template: `%(title)s [%(id)s].%(ext)s` (sanitized)
- Automatic merge when needed
- Detect DRM / "only images available" and abort with clear message

---

## 4. Architecture & Data Flow

```
Browser (index.html + HTMX/JS)
        ↓
FastAPI (main.py)
   ├── /           → serves index.html
   ├── /api/info
   ├── /api/download
   ├── /api/status/*
   ├── /api/files
   └── /downloads/* → FileResponse
        ↓
yt_dlp (in background thread)
        ↓
/downloads/ (volume)
```

**Job State** (in-memory for v1):
```python
jobs = {
    "job-uuid": {
        "id": "...",
        "url": "...",
        "status": "pending" | "downloading" | "completed" | "error",
        "progress": 42.5,
        "speed": "1.2MiB/s",
        "eta": "00:01:23",
        "filename": "...",
        "error": None,
        ...
    }
}
```

For persistence across restarts (optional later): simple SQLite or JSON file.

**Progress Update Strategy (MVP)**
- Client polls `/api/status/{job_id}` every 800ms while active
- Later upgrade: Server-Sent Events (SSE) for push updates

---

## 5. Project Structure

```
/Users/rick/yt-web/
├── Dockerfile
├── docker-compose.yml
├── .dockerignore
├── README.md
├── PLAN.md
├── requirements.txt
├── app/
│   ├── main.py                 # FastAPI app + routes + yt-dlp logic
│   ├── templates/
│   │   └── index.html          # Main UI (Tailwind + HTMX)
│   ├── static/
│   │   └── (any extra css/js if needed)
│   └── downloads/              # .gitkeep or empty (volume mount target)
└── ...
```

Simple and flat is preferred for a single-container app.

---

## 6. Docker & Deployment

**Dockerfile highlights**
- `FROM python:3.12-slim`
- `apt-get install -y ffmpeg`
- `pip install --no-cache-dir -r requirements.txt`
- Non-root user (`appuser`)
- `VOLUME /app/downloads`
- `EXPOSE 8000`
- `CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8000"]`

**docker-compose.yml**
```yaml
version: "3.8"
services:
  yt-web:
    build: .
    ports:
      - "8080:8000"
    volumes:
      - ./downloads:/app/downloads
    environment:
      - DOWNLOAD_DIR=/app/downloads
    restart: unless-stopped
```

**Usage**
```bash
docker compose up --build
# open http://localhost:8080
```

Optional: Add a `cookies.txt` volume for more restricted (but DRM-free) content.

---

## 7. Implementation Phases

1. **Scaffold & Docker**
   - Create directories + basic files
   - Dockerfile + docker-compose that runs a hello FastAPI
   - requirements.txt

2. **Info Endpoint**
   - Implement `/api/info`
   - Use `yt_dlp` with `download=False`
   - Return title, thumbnail, formats (filtered), duration, etc.
   - Basic DRM / image-only detection

3. **Download Job System**
   - Job model + in-memory store
   - Background download function with progress hook
   - `/api/download` and `/api/status`

4. **Basic Frontend**
   - index.html with Tailwind CDN + HTMX
   - URL form → Analyze → show preview
   - Simple download button (hardcoded best format)

5. **Format Selection & Polish**
   - Dynamic format list from `/api/info`
   - Options (subtitles, etc.)
   - Full download flow with progress

6. **File Management**
   - `/api/files` + listing
   - Direct download links + delete

7. **Error Handling + UX**
   - Nice error states for DRM videos
   - Toasts / notifications
   - Input validation (only youtube domains)

8. **Documentation & Hardening**
   - Full README with warnings, usage, troubleshooting
   - .dockerignore, healthcheck, non-root user
   - Optional: basic auth example

9. **Testing & Iteration**
   - Test with various DRM-free videos
   - Test Docker build on clean machine
   - Edge cases (live streams, age restricted without cookies, shorts, etc.)

---

## 8. Risks, Legal, and Limitations

### Important Warnings (must be prominent in UI + README)
- Downloading videos may violate YouTube's Terms of Service.
- Only use for content you have the legal right to download (your own videos, public domain, CC-licensed, etc.).
- This tool **will not** download DRM-protected content (movies, many music videos, age-gated premium videos).
- You are responsible for how you use downloaded files.

### Technical Limitations
- No guaranteed support for every video (YouTube changes frequently).
- Progress accuracy depends on yt-dlp hooks.
- Large files will consume disk space in the volume.
- Concurrent downloads limited by system resources (no hard cap in v1).

### Security
- Run on localhost or trusted network only.
- The container has no authentication by default.
- Never expose port 8000 directly to the public internet without reverse proxy + auth.
- Sanitize filenames (yt-dlp does most of this).

### DRM Handling Strategy
- In info extraction, look for warnings like "DRM protected", "only images are available", or formats with `drm` in protocol.
- If detected → return clear message + do not allow download start.
- Log the detection.

---

## 9. Future Enhancements (Post-MVP)
- Server-Sent Events or WebSockets for real-time progress
- Playlist support with checkboxes
- Searchable download history
- Custom output filename template
- Simple login (basic auth or OAuth later)
- Multiple format downloads in one click
- Automatic cleanup of old files
- Docker healthcheck + resource limits

---

## 10. Next Steps After Plan Approval

1. Review and approve/adjust this plan.
2. Start Phase 1 (scaffold + basic Docker).
3. Iterate phase by phase, testing in Docker each time.

---

**Status**: Planning complete. Ready for implementation.

This plan balances simplicity, power, and maintainability while respecting the DRM-free constraint.