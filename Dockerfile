FROM python:3.12-slim

# Install ffmpeg and clean up in one layer
RUN apt-get update && apt-get install -y --no-install-recommends \
    ffmpeg \
    && rm -rf /var/lib/apt/lists/*

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
