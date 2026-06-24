from fastapi import FastAPI, Request, Form, HTTPException, Query
import urllib.parse
import html as html_lib
import re
import json
import shutil
from fastapi.responses import HTMLResponse, FileResponse
from fastapi.templating import Jinja2Templates
import yt_dlp
import os
import uuid
import threading
import time
from pathlib import Path
from typing import Dict, Any, Optional
from urllib.parse import urlparse

app = FastAPI(title="yt-web - DRM-Free YouTube Downloader")

# Paths
BASE_DIR = Path(__file__).parent
DOWNLOADS_DIR = BASE_DIR / "downloads"
TRANSCRIPTS_DIR = BASE_DIR / "transcripts"
TEMPLATES_DIR = BASE_DIR / "templates"
HISTORY_FILE = BASE_DIR / "history.json"

DOWNLOADS_DIR.mkdir(exist_ok=True)
TRANSCRIPTS_DIR.mkdir(exist_ok=True)

STORAGE_LIMIT_BYTES = int(os.environ.get("STORAGE_LIMIT_GB", 20)) * 1024 ** 3

templates = Jinja2Templates(directory=str(TEMPLATES_DIR))

# In-memory job store
jobs: Dict[str, Dict[str, Any]] = {}
jobs_lock = threading.Lock()

# Cancel events keyed by job_id
cancel_events: Dict[str, threading.Event] = {}
cancel_events_lock = threading.Lock()

# Download history (persisted to HISTORY_FILE)
history: Dict[str, Dict[str, Any]] = {}
history_lock = threading.Lock()

JOB_TTL = 3600

YTDLP_COMMON = {
    "quiet": True,
    "no_warnings": True,
    "noprogress": True,
}

MEDIA_EXTENSIONS = {
    '.mp4', '.mkv', '.webm', '.mov', '.avi', '.flv', '.m4v',
    '.mp3', '.m4a', '.opus', '.ogg', '.aac', '.flac', '.wav',
}

TRANSCRIPT_EXTENSIONS = {'.vtt', '.srt', '.ttml', '.json3', '.srv1', '.srv2', '.srv3', '.md'}


class DownloadCancelled(Exception):
    pass


def load_history():
    global history
    if HISTORY_FILE.exists():
        try:
            with open(HISTORY_FILE, "r", encoding="utf-8") as f:
                history = json.load(f)
        except Exception:
            history = {}


def save_history_entry(filename: str, entry: dict):
    with history_lock:
        history[filename] = entry
        try:
            with open(HISTORY_FILE, "w", encoding="utf-8") as f:
                json.dump(history, f, ensure_ascii=False, indent=2)
        except Exception:
            pass


load_history()


def is_valid_youtube_url(url: str) -> bool:
    try:
        parsed = urlparse(url)
        return parsed.scheme in ("http", "https") and parsed.netloc.lower().lstrip("www.") in (
            "youtube.com", "youtu.be", "m.youtube.com",
        )
    except Exception:
        return False


def is_drm_protected(info: dict, formats: list) -> bool:
    warnings = info.get("warnings", []) or []
    for w in warnings:
        if "drm" in str(w).lower() or "only images" in str(w).lower():
            return True
    for fmt in formats:
        if fmt.get("drm") or "drm" in str(fmt.get("format_note", "")).lower():
            return True
    video_formats = [f for f in formats if f.get("vcodec") != "none"]
    if not video_formats:
        return True
    return False


def prune_old_jobs():
    cutoff = time.time() - JOB_TTL
    with jobs_lock:
        to_delete = [
            jid for jid, j in jobs.items()
            if j.get("status") in ("completed", "error", "cancelled") and j.get("created_at", 0) < cutoff
        ]
        for jid in to_delete:
            del jobs[jid]
    with cancel_events_lock:
        to_delete = [jid for jid in cancel_events if jid not in jobs]
        for jid in to_delete:
            del cancel_events[jid]


@app.get("/", response_class=HTMLResponse)
async def index(request: Request):
    template = templates.get_template("index.html")
    return HTMLResponse(template.render({"request": request}))


@app.get("/api/info")
async def get_info(url: str):
    if not url or not is_valid_youtube_url(url):
        raise HTTPException(400, "Please provide a valid YouTube URL")

    ydl_opts = {
        **YTDLP_COMMON,
        "extract_flat": False,
        "skip_download": True,
        "noplaylist": True,
    }

    try:
        with yt_dlp.YoutubeDL(ydl_opts) as ydl:
            info = ydl.extract_info(url, download=False)
    except Exception as e:
        raise HTTPException(400, f"Failed to fetch video info: {str(e)}")

    metadata = {
        "id": info.get("id"),
        "title": info.get("title"),
        "uploader": info.get("uploader") or info.get("channel"),
        "duration": info.get("duration_string"),
        "thumbnail": info.get("thumbnail"),
        "webpage_url": info.get("webpage_url"),
        "view_count": info.get("view_count"),
    }

    formats = info.get("formats", [])

    def fmt_size(f):
        return f.get("filesize") or f.get("filesize_approx")

    # Best audio size for merging estimates
    audio_formats = [f for f in formats if f.get("acodec", "none") != "none" and f.get("vcodec", "none") == "none"]
    best_audio_size = max((fmt_size(f) or 0 for f in audio_formats), default=0)

    # Best overall size (for "best" option)
    all_video = [f for f in formats if f.get("vcodec", "none") != "none"]
    best_video_size = max((fmt_size(f) or 0 for f in all_video), default=0)

    download_options = [
        {"value": "best", "label": "Best quality available",
         "filesize_estimate": (best_video_size + best_audio_size) or None},
    ]

    video_heights = sorted(
        {f.get("height") for f in formats if f.get("height") and f.get("vcodec", "none") != "none"},
        reverse=True
    )

    for h in video_heights:
        if h >= 144:
            height_formats = [f for f in formats if f.get("height") == h and f.get("vcodec", "none") != "none"]
            best_at_height = max((fmt_size(f) or 0 for f in height_formats), default=0)
            estimate = (best_at_height + best_audio_size) or None
            download_options.append({
                "value": f"bestvideo[height<={h}]+bestaudio/best",
                "label": f"{h}p",
                "filesize_estimate": estimate,
            })

    audio_size = max((fmt_size(f) or 0 for f in audio_formats), default=0) or None
    download_options.append({"value": "bestaudio/best", "label": "Audio only (best)", "filesize_estimate": audio_size})

    has_subs = bool(info.get("subtitles") or info.get("automatic_captions"))
    download_options.append({
        "value": "transcript",
        "label": "Transcript only (English)" + ("" if has_subs else " — may not be available"),
        "filesize_estimate": None,
    })

    drm_detected = is_drm_protected(info, formats)

    return {
        "metadata": metadata,
        "download_options": download_options,
        "drm_detected": drm_detected,
    }


def srt_to_markdown(srt_path: Path, title: str, url: str = "") -> str:
    """Convert an SRT subtitle file to a clean, paragraph-formatted Markdown transcript."""
    text = srt_path.read_text(encoding="utf-8", errors="replace")

    blocks = re.split(r'\n\s*\n', text.strip())
    fragments = []
    for block in blocks:
        parts = block.strip().split('\n')
        dialogue = ' '.join(l.strip() for l in parts[2:] if l.strip())
        if dialogue:
            fragments.append(dialogue)

    deduped = []
    for frag in fragments:
        if not deduped or frag != deduped[-1]:
            deduped.append(frag)

    stream = ''
    for frag in deduped:
        if stream and not stream[-1] in '.!?':
            stream += ' ' + frag
        elif stream:
            stream += ' ' + frag
        else:
            stream = frag

    stream = re.sub(r'<[^>]+>', '', stream)
    stream = re.sub(r'  +', ' ', stream).strip()

    sentences = re.split(r'(?<=[a-z0-9"\')\]][.!?])\s+(?=[A-Z])', stream)

    SENTENCES_PER_PARA = 5
    paragraphs = []
    for i in range(0, len(sentences), SENTENCES_PER_PARA):
        para = ' '.join(s.strip() for s in sentences[i:i + SENTENCES_PER_PARA] if s.strip())
        if para:
            paragraphs.append(para)

    word_count = len(stream.split())
    read_time = max(1, round(word_count / 200))
    meta = f"*{word_count:,} words · ~{read_time} min read*"

    body = '\n\n'.join(paragraphs)
    return f"# {title}\n\n{meta}\n\n**Source:** {url}\n\n---\n\n{body}\n"


def update_job(job_id: str, **kwargs):
    with jobs_lock:
        if job_id in jobs:
            jobs[job_id].update(kwargs)


def make_progress_hook(job_id: str, cancel_event: threading.Event):
    def hook(d: dict):
        if cancel_event.is_set():
            raise DownloadCancelled("Download cancelled by user")

        if d["status"] == "downloading":
            total = d.get("total_bytes") or d.get("total_bytes_estimate", 0)
            downloaded = d.get("downloaded_bytes", 0)
            progress = (downloaded / total * 100) if total else 0
            speed = d.get("speed")
            eta = d.get("eta")

            speed_str = f"{speed / 1024 / 1024:.1f} MiB/s" if speed else None
            eta_str = f"{eta}s" if eta else None

            update_job(
                job_id,
                progress=round(progress, 1),
                speed=speed_str,
                eta=eta_str,
                downloaded=downloaded,
                total=total,
            )
        elif d["status"] == "finished":
            update_job(job_id, status="processing", progress=95)

    return hook


def download_worker(job_id: str, url: str, format_id: Optional[str], options: dict, cancel_event: threading.Event):
    is_transcript = format_id == "transcript"
    output_dir = TRANSCRIPTS_DIR if is_transcript else DOWNLOADS_DIR
    output_template = str(output_dir / "%(title)s [%(id)s].%(ext)s")

    update_job(job_id, status="starting", progress=0)

    ydl_opts = {
        **YTDLP_COMMON,
        "outtmpl": output_template,
        "noplaylist": True,
        "progress_hooks": [make_progress_hook(job_id, cancel_event)],
    }

    if is_transcript:
        ydl_opts.update({
            "skip_download": True,
            "writesubtitles": True,
            "writeautomaticsubs": True,
            "subtitleslangs": ["en.*"],
            "subtitlesformat": "srt",
            "nooverwrites": False,
        })
    else:
        ydl_opts["merge_output_format"] = "mp4"
        ydl_opts["format"] = format_id if format_id and format_id != "best" else "bestvideo+bestaudio/best"

        if options.get("write_subtitles"):
            ydl_opts["writesubtitles"] = True
            ydl_opts["subtitleslangs"] = ["en"]

        if options.get("embed_thumbnail"):
            ydl_opts["writethumbnail"] = True
            ydl_opts["embedthumbnail"] = True

    update_job(job_id, status="downloading", progress=0)

    video_title = ""
    if is_transcript:
        try:
            with yt_dlp.YoutubeDL({**YTDLP_COMMON, "skip_download": True, "noplaylist": True}) as ydl:
                info = ydl.extract_info(url, download=False)
                video_title = info.get("title") or ""
                update_job(job_id, title=video_title)
        except Exception:
            pass

    try:
        with yt_dlp.YoutubeDL(ydl_opts) as ydl:
            ydl.download([url])

        with jobs_lock:
            created_at = jobs.get(job_id, {}).get("created_at", time.time())

        ext_set = TRANSCRIPT_EXTENSIONS if is_transcript else MEDIA_EXTENSIONS
        candidates = [
            f for f in output_dir.glob("*")
            if f.is_file()
            and f.suffix.lower() in ext_set
            and not str(f).endswith(".part")
            and f.stat().st_mtime >= created_at - 2
        ]

        filename = None
        if candidates:
            latest = max(candidates, key=lambda f: f.stat().st_mtime)

            if is_transcript and latest.suffix.lower() == ".srt":
                with jobs_lock:
                    job_meta = jobs.get(job_id, {})
                title = job_meta.get("title") or job_meta.get("url", "Transcript")
                md_path = latest.with_suffix(".md")
                try:
                    md_text = srt_to_markdown(latest, title, url=url)
                    md_path.write_text(md_text, encoding="utf-8")
                    latest.unlink()
                    filename = md_path.name
                except Exception:
                    filename = latest.name
            else:
                filename = latest.name

        if is_transcript and filename is None:
            update_job(job_id, status="error", error="No transcript found. The video may not have English captions available.")
            prune_old_jobs()
            return

        with jobs_lock:
            if job_id in jobs:
                jobs[job_id]["filename"] = filename
                if filename:
                    fpath = output_dir / filename
                    jobs[job_id]["filepath"] = str(fpath)
                    try:
                        size = fpath.stat().st_size
                        jobs[job_id]["downloaded"] = size
                        jobs[job_id]["total"] = size
                    except Exception:
                        pass
                jobs[job_id]["status"] = "completed"
                jobs[job_id]["progress"] = 100

        # Persist to history
        if filename:
            save_history_entry(filename, {
                "source_url": url,
                "title": video_title or "",
                "job_type": "transcript" if is_transcript else "video",
                "completed_at": time.time(),
            })

    except DownloadCancelled:
        update_job(job_id, status="cancelled", progress=0, speed=None, eta=None)
        # Clean up any partial files
        try:
            for f in output_dir.glob("*.part"):
                f.unlink(missing_ok=True)
        except Exception:
            pass
    except Exception as e:
        update_job(job_id, status="error", error=str(e))

    prune_old_jobs()


@app.post("/api/download")
async def start_download(
    url: str = Form(...),
    format_id: Optional[str] = Form(None),
    write_subtitles: bool = Form(False),
    embed_thumbnail: bool = Form(False),
):
    if not url:
        raise HTTPException(400, "URL is required")

    job_id = str(uuid.uuid4())[:8]
    job_type = "transcript" if format_id == "transcript" else "video"
    cancel_event = threading.Event()

    with jobs_lock:
        jobs[job_id] = {
            "id": job_id,
            "job_type": job_type,
            "url": url,
            "status": "queued",
            "progress": 0,
            "speed": None,
            "eta": None,
            "filename": None,
            "error": None,
            "created_at": time.time(),
        }

    with cancel_events_lock:
        cancel_events[job_id] = cancel_event

    options = {
        "write_subtitles": write_subtitles,
        "embed_thumbnail": embed_thumbnail,
    }

    thread = threading.Thread(
        target=download_worker,
        args=(job_id, url, format_id, options, cancel_event),
        daemon=True
    )
    thread.start()

    return {"job_id": job_id, "job_type": job_type}


@app.delete("/api/download/{job_id}")
async def cancel_download(job_id: str):
    with cancel_events_lock:
        event = cancel_events.get(job_id)
    if not event:
        raise HTTPException(404, "Job not found or already finished")
    event.set()
    update_job(job_id, status="cancelled")
    return {"ok": True}


@app.get("/api/status/{job_id}")
async def get_status(job_id: str):
    with jobs_lock:
        job = jobs.get(job_id)
        if not job:
            raise HTTPException(404, "Job not found")
        return job


@app.get("/api/disk")
async def disk_usage():
    def dir_size(path: Path) -> int:
        return sum(f.stat().st_size for f in path.glob("*") if f.is_file())

    usage = shutil.disk_usage(str(BASE_DIR))
    downloads_bytes = dir_size(DOWNLOADS_DIR)
    transcripts_bytes = dir_size(TRANSCRIPTS_DIR)
    storage_used = downloads_bytes + transcripts_bytes
    return {
        "downloads_bytes": downloads_bytes,
        "transcripts_bytes": transcripts_bytes,
        "storage_used": storage_used,
        "storage_limit": STORAGE_LIMIT_BYTES,
        "storage_free": max(0, STORAGE_LIMIT_BYTES - storage_used),
        "disk_used": usage.used,
        "disk_total": usage.total,
        "disk_free": usage.free,
    }


@app.get("/api/files")
async def list_files():
    files = []
    with history_lock:
        hist = dict(history)
    for f in sorted(DOWNLOADS_DIR.glob("*"), key=os.path.getmtime, reverse=True):
        if f.is_file() and f.suffix.lower() in MEDIA_EXTENSIONS and not f.name.endswith(".part"):
            stat = f.stat()
            entry = hist.get(f.name, {})
            files.append({
                "name": f.name,
                "size": stat.st_size,
                "modified": stat.st_mtime,
                "source_url": entry.get("source_url"),
                "title": entry.get("title"),
            })
    return {"files": files}


@app.get("/downloads/{filename}")
async def download_file(filename: str):
    filepath = DOWNLOADS_DIR / filename
    if not filepath.exists() or not filepath.is_file():
        raise HTTPException(404, "File not found")
    return FileResponse(path=str(filepath), filename=filename, media_type="application/octet-stream")


@app.delete("/api/files/{filename}")
async def delete_file(filename: str):
    filepath = DOWNLOADS_DIR / filename
    if filepath.exists():
        filepath.unlink()
        return {"ok": True}
    raise HTTPException(404, "File not found")


@app.get("/api/transcripts")
async def list_transcripts():
    files = []
    with history_lock:
        hist = dict(history)
    for f in sorted(TRANSCRIPTS_DIR.glob("*"), key=os.path.getmtime, reverse=True):
        if f.is_file() and f.suffix.lower() in TRANSCRIPT_EXTENSIONS:
            stat = f.stat()
            entry = hist.get(f.name, {})
            files.append({
                "name": f.name,
                "size": stat.st_size,
                "modified": stat.st_mtime,
                "source_url": entry.get("source_url"),
                "title": entry.get("title"),
            })
    return {"files": files}


@app.get("/transcripts/{filename}")
async def serve_transcript(filename: str):
    filepath = TRANSCRIPTS_DIR / filename
    if not filepath.exists() or not filepath.is_file():
        raise HTTPException(404, "File not found")
    return FileResponse(path=str(filepath), filename=filename, media_type="text/plain; charset=utf-8")


@app.delete("/api/transcripts/{filename}")
async def delete_transcript(filename: str):
    filepath = TRANSCRIPTS_DIR / filename
    if filepath.exists():
        filepath.unlink()
        return {"ok": True}
    raise HTTPException(404, "File not found")


@app.get("/watch", response_class=HTMLResponse)
async def watch_video(file: str = Query(...)):
    safe_file = urllib.parse.quote(file)
    safe_title = html_lib.escape(file)
    video_src = f"/downloads/{safe_file}"

    ext = file.lower().rsplit(".", 1)[-1] if "." in file else ""
    mime = "video/mp4"
    if ext == "webm":
        mime = "video/webm"
    elif ext in ("ogg", "ogv"):
        mime = "video/ogg"
    elif ext == "mp3":
        mime = "audio/mpeg"
    elif ext in ("m4a", "aac"):
        mime = "audio/mp4"
    elif ext == "opus":
        mime = "audio/ogg"

    html = f"""<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Watch • {safe_title}</title>
    <style>
        body {{
            margin: 0;
            background: #0f172a;
            color: #e2e8f0;
            font-family: system-ui, sans-serif;
            display: flex;
            flex-direction: column;
            height: 100vh;
        }}
        .header {{
            padding: 12px 20px;
            background: #1e293b;
            border-bottom: 1px solid #334155;
            display: flex;
            justify-content: space-between;
            align-items: center;
        }}
        .header h1 {{
            margin: 0;
            font-size: 1.1rem;
            font-weight: 500;
            overflow: hidden;
            text-overflow: ellipsis;
            white-space: nowrap;
            max-width: 70%;
        }}
        .video-container {{
            flex: 1;
            display: flex;
            align-items: center;
            justify-content: center;
            background: #000;
            padding: 20px;
        }}
        video, audio {{
            max-width: 100%;
            max-height: 100%;
            box-shadow: 0 10px 30px rgba(0,0,0,0.6);
            border-radius: 8px;
        }}
        a {{ color: #60a5fa; text-decoration: none; }}
        a:hover {{ text-decoration: underline; }}
    </style>
</head>
<body>
    <div class="header">
        <h1>{safe_title}</h1>
        <a href="/downloads/{safe_file}" download>⬇ Download</a>
    </div>
    <div class="video-container">
        <video controls autoplay style="max-width: 100%; max-height: 100%; outline: none;">
            <source src="{video_src}" type="{mime}">
            Your browser does not support the video tag.
        </video>
    </div>
</body>
</html>"""
    return HTMLResponse(content=html)


if __name__ == "__main__":
    import uvicorn
    uvicorn.run("main:app", host="0.0.0.0", port=8000, reload=True)
