# Hackathon Challenge: The Perfect API

**Theme:** Design, build, and deploy the "perfect" REST API based on industry best practices.

**Duration:** 1 Day

**Technology Stack:** C# with ASP.NET Core Web API

### **Overview**

Welcome, developers! Your challenge today is not just to build a functional API, but to build a *well-designed* one. We're moving beyond complex business logic to focus purely on the **technical principles** that make APIs scalable, maintainable, and a pleasure to use.

The provided diagram on REST API Design is your guide and your rulebook. The team that best implements the core principles and features shown in the diagram will be the winner.

### **The Challenge**

To sharpen the focus on technical implementation over business logic, this challenge is centered around a **single entity**. You still have two options to choose from:

1. **Greenfield Project (Build from Scratch):**
   - Build a **URL Shortener API**.
   - The core resource is a "shortened URL" which has an original URL, a generated short code, a creation date, and a click count.
   - Your goal is to design the API endpoints for creating, retrieving, listing, and deleting these URLs from the ground up, following REST principles. Think about how to handle the redirect logic for a short code.
2. **Brownfield Project (Refactor an Existing App):**
   - We will provide you with a sample .NET project for a simple "To-Do List" API (with just one "Todo" entity) that is poorly designed and non-RESTful.
   - Your task is to refactor this "legacy" API, correct its flaws, and implement proper RESTful design patterns. You will need to fix endpoint naming, use HTTP methods correctly, implement proper status codes, and add essential features.

### **Judging Criteria & Scoring**

Your project will be judged based on how many concepts from the REST API Design diagram you successfully implement.

**Core Principles (Max 40 points):**

- **Uniform Interface (15 pts):** Are your resources clearly identified? Are your endpoints resource-based (e.g., `/api/urls/{id}`)? Are you using self-descriptive messages (e.g., correct JSON media types and HTTP status codes)?
- **Stateless (10 pts):** Does each request from the client contain all the information needed to understand and process it? The server should not store any client context between requests.
- **Client-Server (10 pts):** Is there a clear separation of concerns between the UI (client) and the data storage (server)?
- **Cacheable (5 pts):** Have you implemented caching headers (like `ETag` or `Cache-Control`) on appropriate GET endpoints?

**Methods & Endpoints (Max 20 points):**

- **Correct HTTP Method Usage (20 pts):**
  - `GET`: Retrieve resources.
  - `POST`: Create new resources.
  - `PUT`: Replace an existing resource entirely.
  - `PATCH`: Partially update an existing resource.
  - `DELETE`: Remove a resource.
  - Are your methods idempotent where they should be (GET, PUT, DELETE)?

**Connectedness & Advanced Features (Max 40 points):**

- **Pagination (10 pts):** For endpoints that return a list of resources, have you implemented paging (e.g., using `?page=1&pageSize=10`)?
- **Filtering & Ordering (10 pts):** Can clients filter or sort the results (e.g., `?clicks=0&sortBy=created_desc`)?
- **Versioning (5 pts):** How are you handling API versioning (e.g., `/api/v1/urls`)?
- **Security & Auth (5 pts):** Have you implemented basic security measures? This could be API Key authentication, JWT, or another simple auth mechanism.
- **HATEOAS (Hypermedia) (5 pts):** Do your resource representations include links to related actions or resources? (This is a key principle for a "perfect" API).
- **Input Validation (5 pts):** Are you validating incoming data and returning clear `400 Bad Request` errors?

**Bonus (Up to 10 extra points):**

- **Monitoring/Logging:** Basic logging implemented.
- **Documentation:** A simple `README.md` or a generated Swagger/OpenAPI page that is clean and easy to understand.
- **Containerization:** The application is fully runnable via a Dockerfile.

### **Getting Started**

1. Form your teams.
2. Choose your path: Greenfield or Brownfield.
3. Clone the starter repository (if provided for the brownfield option).
4. Design your resource models and API endpoints. **Plan before you code!**
5. Start building! Use the `dotnet new webapi` template to get going quickly.
6. Commit your code regularly.
7. Prepare a short (5-minute) demo for the judges.

Good luck, and may the best API win!
