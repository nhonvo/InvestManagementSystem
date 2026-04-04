# Dockerization & Desktop Environment Plan

This document outlines the strategy for containerizing the **Automated Inventory Alert System** to ensure consistent environments across Development, Test, and Production.

---

## 🏗️ 1. Dockerfile (API Container)

Use a **Multi-Stage Build** to keep the final image size small and secure.

### Stages:
1.  **Base:** `aspnet:10.0` (Runtime environment).
2.  **Build:** `sdk:10.0` (Compiles the application).
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
| **hangfire** | (Same as api) | (Planned) Running background jobs. |
| **redis** | `redis:alpine` | ✅ Distributed caching for API performance. |
| **moto** | `motoserver/moto` | ✅ AWS SNS/SQS emulation. |

---

## 🚀 3. Step-by-Step implementation

- [x] **Step 1: Create `.dockerignore`**
    - Done: Excludes `bin/`, `obj/`, and dev secrets to keep build contexts small.
- [x] **Step 2: Create `Dockerfile` in project root**
    - Done: Uses multi-stage build and entrypoint configuration.
- [x] **Step 3: Create `docker-compose.yml`**
    - Done: Configured volumes, networks, and depends_on healthchecks.
- [x] **Step 4: Update Configuration**
    - Done: Implemented `appsettings.Docker.json` for container-to-container communication.

---

## 🔒 4. Security Considerations

1.  **Non-Root User:** Run the API as a low-privileged user inside the container.
2.  **Secret Management:** Do NOT hardcode passwords in `docker-compose.yml`. Use environment variables or `.env` files (ignored by git).
3.  **Healtchecks:** Add health checks to the database to ensure the API only starts after the DB is ready.
