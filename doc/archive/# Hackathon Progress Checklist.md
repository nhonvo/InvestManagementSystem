# Hackathon Progress Checklist

Use this checklist to track your team's progress and ensure you're hitting all the key technical requirements from the judging criteria. Good luck!

✅ Core Principles (40 points)

- [ ] Uniform Interface (15 pts)
- [ ] Endpoints are resource-based and follow RESTful naming conventions (e.g., /api/urls/{id}).
- [ ] Self-descriptive messages are used (e.g., Content-Type: application/json).
- [ ] Appropriate HTTP status codes are returned for different scenarios (e.g., 200 OK, 201 Created, 24 No Content, 400 Bad Request, 404 Not Found).

- [ ] Stateless (10 pts)
- [ ] Every request contains all necessary information for the server to process it.
- [ ] The server does not store any client session state between requests.
- [ ] Client-Server (10 pts)
- [ ] The application logic is clearly separated between the client and the server.
- [ ] Cacheable (5 pts)
- [ ] Caching headers (like ETag or Cache-Control) are implemented on GET endpoints.
✅ Methods & Endpoints (20 points)
- [ ] Correct HTTP Method Usage (20 pts)
- [ ] GET method is used to retrieve one or more resources.
- [ ] POST method is used to create a new resource.
- [ ] PUT method is used to completely replace an existing resource.
- [ ] PATCH method is used to partially update an existing resource.
- [ ] DELETE method is used to remove a resource.
- [ ] GET, PUT, and DELETE operations are idempotent.
✅ Connectedness & Advanced Features (40 points)
- [ ] Pagination (10 pts)
- [ ] Endpoints that return lists of resources support pagination (e.g., ?page=1&pageSize=10).
- [ ] Filtering & Ordering (10 pts)
- [ ] List endpoints support filtering results based on field values (e.g., ?status=active).
- [ ] List endpoints support ordering/sorting of results (e.g., ?sortBy=name_desc).
- [ ] Versioning (5 pts)
- [ ] The API has a versioning strategy implemented (e.g., /api/v1/urls).
- [ ] Security & Auth (5 pts)
- [ ] Basic authentication/authorization is implemented (e.g., API Key, JWT).
- [ ] HATEOAS (Hypermedia) (5 pts)
- [ ] Responses include links to related resources or possible actions.
- [ ] Input Validation (5 pts)
- [ ] Data from incoming POST, PUT, and PATCH requests is validated.
- [ ] Clear 400 Bad Request responses are returned for invalid data.
⭐ Bonus (Up to 10 points)
- [ ] Monitoring/Logging
- [ ] Basic logging is implemented to track requests or errors.
- [ ] Documentation
- [ ] The project includes a helpful README.md file.
- [ ] The API is documented using Swagger/OpenAPI.
- [ ] Containerization
- [ ] The application can be built and run using a Dockerfile
