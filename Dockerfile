FROM mwader/static-ffmpeg:latest AS ffmpeg

FROM python:3.12-slim

# Copy static ffmpeg binary (no apt install needed)
COPY --from=ffmpeg /ffmpeg /usr/local/bin/ffmpeg

# Install Node.js (required by yt-dlp for YouTube JS extraction)
RUN apt-get update && apt-get install -y --no-install-recommends \
    nodejs \
    && rm -rf /var/lib/apt/lists/*

# Non-root user
RUN useradd -m -u 1000 appuser

# yt-dlp config: enable Node.js JS runtime
RUN mkdir -p /home/appuser/.config/yt-dlp && \
    echo '--js-runtimes node:/usr/bin/node' > /home/appuser/.config/yt-dlp/config && \
    chown -R appuser:appuser /home/appuser/.config

WORKDIR /app

# Install Python deps first (better layer caching)
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# Copy app code
COPY app/ .

# Ensure download dirs exist and are owned by appuser
RUN mkdir -p downloads transcripts && chown -R appuser:appuser /app

USER appuser

EXPOSE 8000

CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8000"]
