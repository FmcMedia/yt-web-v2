FROM mwader/static-ffmpeg:latest AS ffmpeg

FROM python:3.12-slim

# Copy static ffmpeg binary (no apt install needed)
COPY --from=ffmpeg /ffmpeg /usr/local/bin/ffmpeg

# Non-root user
RUN useradd -m -u 1000 appuser

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
