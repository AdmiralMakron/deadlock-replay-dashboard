# Demos Directory

This directory is intentionally empty in the repository. Demo files (`.dem`) are
large binary assets and are not committed to git. They are pulled automatically
from GitHub Releases during the Docker build via the `curl` command in the
Dockerfile.

## Note on this branch

The model pulled the `.dem` file directly into this directory outside of the
Docker build process, which bloated the repository and tarball size from ~50MB
to ~500MB. The file has been removed from this commit. The original file is
still available at:

https://github.com/AdmiralMakron/deadlock-replay-dashboard/releases/download/v1.0.0/48525700.dem

And is pulled automatically when running `docker compose up --build`.
