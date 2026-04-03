# Dockerization & Desktop Environment Plan

This document outlines the strategy for containerizing the **Automated Inventory Alert System** to ensure consistent environments across Development, Test, and Production.

---

## 🏗️ 1. Dockerfile (API Container)

Use a **Multi-Stage Build** to keep the final image size small and secure.

### Stages:
1.  **Base:** `aspnet:9.0` (Runtime environment).
2.  **Build:** `sdk:9.0` (Compiles the application).
3.  **Publish:** Optimizes the binaries.
4.  **Final:** Copies published artifacts to the Base image.

---

## 🛠️ 2. Docker Compose (Orchestration)

A single command (`docker-compose up`) should spin up the entire ecosystem.

### Services:
| Service | Image | Responsibility |
| :--- | :--- | :--- |
| **api** | (Your Dockerfile) | REST Endpoints & Application logic. |
| **db** | `postgres:17-alpine` | Primary data storage. |
| **hangfire** | (Same as api) | Running the background worker (Pricing & Alerts). |
| **redis** | `redis:alpine` | (Optionally) distributed caching for API performance. |

---

## 🚀 3. Step-by-Step implementation

- [ ] **Step 1: Create `.dockerignore`**
    - Ignore `bin/`, `obj/`, `.git/`, and `appsettings.Development.json` to keep build contexts small.
- [ ] **Step 2: Create `Dockerfile` in project root**
    - Use `ENTRYPOINT` to start the webserver.
- [ ] **Step 3: Create `docker-compose.yml`**
    - Configure **Volumes** for PostgreSQL persistence (`/var/lib/postgresql/data`).
    - Use **Networks** to allow services to talk to each other by name (e.g., `Host=db`).
- [ ] **Step 4: Update `appsettings.json`**
    - Use connection strings that point to the container name `db` instead of `localhost`.

---

## 🔒 4. Security Considerations

1.  **Non-Root User:** Run the API as a low-privileged user inside the container.
2.  **Secret Management:** Do NOT hardcode passwords in `docker-compose.yml`. Use environment variables or `.env` files (ignored by git).
3.  **Healtchecks:** Add health checks to the database to ensure the API only starts after the DB is ready.
