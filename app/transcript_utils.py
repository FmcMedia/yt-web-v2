"""Pure utilities for converting YouTube auto-captions (SRT) to clean Markdown.

Shared between the web UI (single downloads) and the batch channel extractor.
No external dependencies beyond stdlib.
"""

import re
from pathlib import Path
from typing import Optional, Dict, Any


def _clean_srt_text(srt_text: str) -> str:
    """Core SRT cleaning logic (timestamps/indices removed, deduped, paragraphed)."""
    blocks = re.split(r'\n\s*\n', srt_text.strip())
    fragments = []
    for block in blocks:
        parts = block.strip().split('\n')
        dialogue = ' '.join(l.strip() for l in parts[2:] if l.strip())
        if dialogue:
            fragments.append(dialogue)

    # Deduplicate consecutive identical lines (common in auto-captions)
    deduped = []
    for frag in fragments:
        if not deduped or frag != deduped[-1]:
            deduped.append(frag)

    stream = ''
    for frag in deduped:
        stream = (stream + ' ' + frag) if stream else frag

    # Strip any residual HTML/tags and collapse whitespace
    stream = re.sub(r'<[^>]+>', '', stream)
    stream = re.sub(r'  +', ' ', stream).strip()

    return stream


def _split_into_paragraphs(text: str, sentences_per_para: int = 5) -> list[str]:
    """Split cleaned transcript text into readable paragraphs."""
    # Split on sentence boundaries (simple heuristic that works well for spoken English)
    sentences = re.split(r'(?<=[a-z0-9"\')\]][.!?])\s+(?=[A-Z])', text)

    paragraphs = []
    for i in range(0, len(sentences), sentences_per_para):
        para = ' '.join(s.strip() for s in sentences[i:i + sentences_per_para] if s.strip())
        if para:
            paragraphs.append(para)
    return paragraphs


def srt_text_to_markdown(
    srt_text: str,
    title: str,
    url: str = "",
    frontmatter: Optional[Dict[str, Any]] = None,
) -> str:
    """Convert raw SRT caption text into clean, readable Markdown.

    - Removes timestamps and cue numbers
    - Deduplicates repeated lines (very common with auto-captions)
    - Groups into ~5-sentence paragraphs
    - Adds word count + estimated read time
    - Optionally prepends YAML frontmatter (for batch/Obsidian use)
    """
    cleaned = _clean_srt_text(srt_text)
    paragraphs = _split_into_paragraphs(cleaned)

    word_count = len(cleaned.split())
    read_time = max(1, round(word_count / 200))

    meta_line = f"*{word_count:,} words · ~{read_time} min read*"

    body = '\n\n'.join(paragraphs)

    header = f"# {title}\n\n{meta_line}\n\n**Source:** {url}\n\n---\n\n{body}\n"

    if frontmatter:
        fm_lines = ["---"]
        for key, value in frontmatter.items():
            if value is None:
                continue
            if isinstance(value, str):
                # Safe quoting for strings containing special chars
                if any(c in value for c in [":", "\n", '"']):
                    safe = value.replace('"', '\\"')
                    fm_lines.append(f'{key}: "{safe}"')
                else:
                    fm_lines.append(f'{key}: {value}')
            else:
                fm_lines.append(f"{key}: {value}")
        fm_lines.append("---\n")
        return "\n".join(fm_lines) + header

    return header


def vtt_text_to_markdown(
    vtt_text: str,
    title: str,
    url: str = "",
    frontmatter: Optional[Dict[str, Any]] = None,
) -> str:
    """Convert WebVTT caption text to clean Markdown (same pipeline as SRT)."""
    # Normalise VTT to SRT-style blocks so the shared cleaner can handle it:
    # - strip WEBVTT header and NOTE/STYLE/REGION blocks
    # - drop cue identifiers (lines that don't contain -->)
    # - replace '.' millisecond separator with ',' to match SRT
    lines = vtt_text.splitlines()
    srt_lines: list[str] = []
    skip_block = False
    cue_num = 0
    i = 0
    while i < len(lines):
        line = lines[i]
        stripped = line.strip()
        # Skip header and metadata blocks
        if stripped.startswith("WEBVTT") or stripped.startswith("NOTE") or \
           stripped.startswith("STYLE") or stripped.startswith("REGION"):
            skip_block = True
            i += 1
            continue
        if skip_block:
            if stripped == "":
                skip_block = False
            i += 1
            continue
        if stripped == "":
            if srt_lines and srt_lines[-1] != "":
                srt_lines.append("")
            i += 1
            continue
        if "-->" in stripped:
            # Emit a cue number before each timestamp block
            cue_num += 1
            if srt_lines and srt_lines[-1] != "":
                srt_lines.append("")
            srt_lines.append(str(cue_num))
            # Convert VTT timestamp (.) to SRT timestamp (,) and strip positioning
            ts = stripped.split(" ")[0] + " --> " + stripped.split(" --> ")[1].split(" ")[0]
            srt_lines.append(ts.replace(".", ","))
        elif stripped:
            # Text line — strip any inline VTT tags like <c>, </c>, <00:00:01.000>
            clean = re.sub(r"<[^>]+>", "", stripped)
            if clean:
                srt_lines.append(clean)
        i += 1

    srt_body = "\n".join(srt_lines)
    return srt_text_to_markdown(srt_body, title, url, frontmatter=frontmatter)


def srt_to_markdown(srt_path: Path, title: str, url: str = "") -> str:
    """Backward-compatible wrapper used by the existing web UI."""
    text = srt_path.read_text(encoding="utf-8", errors="replace")
    return srt_text_to_markdown(text, title, url)


def make_frontmatter(
    *,
    title: str,
    video_id: str,
    url: str,
    upload_date: Optional[str] = None,   # YYYY-MM-DD preferred
    duration: Optional[int] = None,
    view_count: Optional[int] = None,
    like_count: Optional[int] = None,
    video_type: str = "short",           # "short" | "video"
    channel: str = "",
    **extra,
) -> Dict[str, Any]:
    """Build a clean frontmatter dict for individual transcript files."""
    fm: Dict[str, Any] = {
        "title": title,
        "video_id": video_id,
        "url": url,
        "type": video_type,
        "channel": channel,
    }
    if upload_date:
        fm["date"] = upload_date
    if duration:
        fm["duration"] = duration
    if view_count is not None:
        fm["views"] = view_count
    if like_count is not None:
        fm["likes"] = like_count
    fm.update({k: v for k, v in extra.items() if v is not None})
    return fm


def sanitize_filename_part(text: str, max_len: int = 80) -> str:
    """Make a safe filename component from a title."""
    # Remove filesystem-unsafe characters
    text = re.sub(r'[\\/*?:"<>|]', '', text)
    # Collapse whitespace and trim
    text = re.sub(r'\s+', ' ', text).strip()
    if len(text) > max_len:
        text = text[:max_len].rsplit(' ', 1)[0].strip() + "…"
    return text
