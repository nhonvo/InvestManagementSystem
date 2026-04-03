using InventoryAlert.Api.Application.DTOs;
using InventoryAlert.Api.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace InventoryAlert.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController(IProductService productService) : ControllerBase
    {
        private readonly IProductService _productService = productService;
        [HttpGet]
        public async Task<IActionResult> GetProducts(CancellationToken cancellationToken)
        {
            var result = await _productService.GetAllProductsAsync(cancellationToken);
            return Ok(result);
        }
        [HttpPost]
        public async Task<IActionResult> CreateProduct(ProductDto productDto, CancellationToken cancellationToken)
        {
            var result = await _productService.CreateProductAsync(productDto, cancellationToken);
            return Ok(result);
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, ProductDto productDto, CancellationToken cancellationToken)
        {
            var result = await _productService.UpdateProductAsync(id, productDto, cancellationToken);
            return Ok(result);
        }
        [HttpGet("{id}")] // TODO: Review the id parameter design, consider to use query parameter or route parameter
        public async Task<IActionResult> GetProductsByIds([FromQuery] int id, CancellationToken cancellationToken)
        {
            var result = await _productService.GetProductByIdAsync(id, cancellationToken);
            return Ok(result);
        }
        [HttpDelete]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var result = await _productService.DeleteProductAsync(id);
            return Ok(result);
        }
        [HttpPost("bulk-insert")]
        public async Task<IActionResult> BulkInsertProducts(IEnumerable<ProductRequestDto> productRequestDtos, CancellationToken cancellationToken)
        {
            await _productService.BulkInsertProductsAsync(productRequestDtos, cancellationToken);
            return Ok();
        }

        // TODO: Review change it suitable with patch method design
        [HttpPatch]
        public async Task<IActionResult> UpdateProductStockCount(int id, int stockCount, CancellationToken cancellationToken)
        {
            var product = await _productService.GetProductByIdAsync(id, CancellationToken.None);
            if (product == null) return NotFound();
            product.StockCount = stockCount;
            var result = await _productService.UpdateProductAsync(id, product, cancellationToken);
            return Ok(result);
        }
        // TODO: all the logic should in the service layer, consider to move the logic to service layer and make the controller more thin and simple, just for handling the http request and response.
        [HttpGet("low-stock")]
        public async Task<IActionResult> GetProductsBelowAlertThreshold(CancellationToken cancellationToken)
        {
            var result = await _productService.GetHighValueProducts(cancellationToken);
            return Ok(result);
        }
    }
}
// TODO: PLAN TO implement caching for frequently accessed data, such as product details, to improve performance. Consider using in-memory caching or distributed caching solutions like Redis, depending on the scale of the application and the expected load.
// todo: plan to add simple authentication and authorization mechanism to secure the API endpoints, such as JWT-based authentication, to ensure that only authorized users can access certain resources.
// TODO: review all cancellation token usage in the service layer, consider to use it more consistently and properly to ensure that the application can handle cancellation requests effectively and improve the overall responsiveness of the API.
// TODO: consider to add validation for the input data in the API endpoints, such as using FluentValidation or Data Annotations, to ensure that the data being processed is valid and to provide meaningful error messages to the clients when validation fails.
// TODO: review the design of the API endpoints, consider to follow RESTful principles more closely, such as using appropriate HTTP methods (GET, POST, PUT, DELETE) and status codes, and designing the endpoints in a way that is intuitive and easy to understand for the clients.
// TODO: consider to implement pagination for the endpoints that return lists of data, such as the GetProducts endpoint, to improve performance and usability when dealing with large datasets. This can be done by accepting query parameters for page number and page size, and returning the appropriate subset of data along with metadata about the total number of items and pages.
// TODO: review the design of the ProductDto and ProductRequestDto, consider to separate the properties that are required for creating a product and updating a product, and to use different DTOs for these operations to improve clarity and maintainability of the code.
// TODO: consider to implement a mapping layer, such as using AutoMapper, to handle the mapping between the domain models and the DTOs, to reduce boilerplate code and improve maintainability of the codebase. This can help to keep the controllers and services more focused on their core responsibilities, and to centralize the mapping logic in one place.
// TODO: review the design of the GetHighValueProducts method, consider to rename it to something more descriptive, such as GetProductsBelowAlertThreshold, to better reflect its purpose and to improve the readability of the code. Additionally, consider to move the logic for determining which products are below the alert threshold into the service layer, and to return a more specific DTO that includes the relevant information for these products, such as the current stock count and the alert threshold.
// TODO: consider to implement unit tests for the ProductController and the ProductService, to ensure that the functionality is working as expected and to help with regression testing in the future. This can be done using a testing framework such as xUnit or NUnit, and can include tests for both successful scenarios and error scenarios to ensure that the application behaves correctly in different situations.
// TODO: review the use of async/await in the controller and service methods, consider to ensure that all asynchronous operations are properly awaited and that there are no potential issues with unhandled exceptions or deadlocks. Additionally, consider to use ConfigureAwait(false) in the service layer to avoid potential issues with synchronization contexts in certain scenarios, such as when the service layer is called from a non-ASP.NET context.
// TODO: consider to implement a more robust error handling strategy in the service layer, such as using custom exceptions to represent different types of errors (e.g., NotFoundException, ValidationException), and to catch and handle these exceptions in the controller to return appropriate HTTP status codes and error messages to the clients. This can help to improve the clarity and maintainability of the code, and to provide better feedback to the clients when errors occur.
// TODO: review the use of the UnitOfWork pattern in the ProductService, consider to ensure that it is being used consistently and appropriately for all operations that involve multiple database operations, to ensure that transactions are properly managed and that the application can handle rollbacks effectively in case of errors. Additionally, consider to review the implementation of the UnitOfWork to ensure that it is efficient and does not introduce unnecessary overhead or complexity into the application.
// TODO: consider to implement a more comprehensive logging strategy in the application, such as logging important events (e.g., product creation, updates, deletions) and errors with appropriate log levels (e.g., Information, Warning, Error), and to include relevant contextual information in the logs (e.g., product ID, user ID) to help with troubleshooting and analysis in production environments. This can be done using a logging framework such as Serilog or NLog, and can be configured to write logs to different sinks (e.g., console, file, database) depending on the needs of the application.
// TODO: review the overall architecture and design of the application, consider to ensure that it follows best practices for clean architecture and separation of concerns, with clear boundaries between the different layers (e.g., API layer, service layer, data access layer) and a well-defined domain model. This can help to improve the maintainability and scalability of the application as it grows and evolves over time. Additionally, consider to document the architecture and design decisions in a way that is accessible and understandable for other developers who may work on the project in the future.
// TODO: consider to implement a more comprehensive API documentation strategy, such as using Swagger/OpenAPI to automatically generate documentation for the API endpoints, including details about the request and response models, status codes, and any relevant information about the behavior of the endpoints. This can help to improve the usability of the API for clients and to provide clear guidance on how to interact with the API effectively. Additionally, consider to include examples of requests and responses in the documentation to further enhance its usefulness for developers who are consuming the API.
// TODO: review the security of the API endpoints, consider to implement measures to protect against common vulnerabilities such as SQL injection, cross-site scripting (XSS), and cross-site request forgery (CSRF). This can include using parameterized queries in the data access layer, validating and sanitizing input data, and implementing appropriate authentication and authorization mechanisms to ensure that only authorized users can access certain resources. Additionally, consider to use HTTPS for all API communication to encrypt the data in transit and to protect
// todo: review the naming conventions for the API endpoints and the methods in the service layer, consider to follow consistent naming conventions that are descriptive and intuitive, to improve the readability and maintainability of the code. This can include using verbs for actions (e.g., Get, Create, Update, Delete) and nouns for resources (e.g., Product), and to ensure that the names accurately reflect the purpose and behavior of the endpoints and methods. Additionally, consider to use consistent naming conventions for DTOs and other classes in the application to further enhance the clarity of the codebase.
// todo: the naming of product entity and product dto should be more specific, consider to rename it to something more descriptive, such as InventoryProduct or StockProduct, to better reflect its purpose and to avoid confusion with other types of products that may exist in the application. This can help to improve the clarity and maintainability of the code, and to provide better context for developers who are working with the codebase. Additionally, consider to review the properties of the Product entity and DTO to ensure that they are comprehensive and appropriately named to reflect their purpose and usage in the application.
// TODO: REVIEW all the input parameters for the API endpoints, consider to ensure that they are consistent and appropriately designed for the intended use cases. This can include using route parameters for identifying specific resources (e.g., product ID), query parameters for filtering and pagination, and request bodies for creating and updating resources. Additionally, consider to review the use of data annotations or validation attributes on the DTOs to ensure that the input data is properly validated and that meaningful error messages are returned to the clients when validation fails. This can help to improve the robustness and usability of the API.
// TODO: review all the http status codes returned by the API endpoints, consider to ensure that they are appropriate for the different scenarios (e.g., 200 OK for successful operations, 201 Created for resource creation, 400 Bad Request for validation errors, 404 Not Found for missing resources, etc.), and to provide meaningful error messages in the response body when errors occur. This can help to improve the clarity and usability of the API for clients, and to provide better feedback when issues arise. Additionally, consider to review the use of IActionResult in the controller methods to ensure that it is being used effectively to return the appropriate status codes and responses based on the outcome of the operations.
// TODo: the userfriendly error messages should be returned to the clients when errors occur, consider to implement a global exception handling mechanism in the API, such as using middleware or filters, to catch unhandled exceptions and to return consistent and meaningful error responses to the clients. This can help to improve the user experience and to provide better feedback when issues arise, while also ensuring that sensitive information is not exposed in error messages. Additionally, consider to include error codes or other relevant information in the error responses to help clients understand and handle errors more effectively.

// TODO: setup middleware, logging, exception handling, and other cross-cutting concerns to enhance the robustness and maintainability of the application. Consider using Serilog or NLog for logging, and implement a global exception handler to manage exceptions gracefully. but make it simple and easy to understand for junior developers.
// TODO: review the use of dependency injection in the application, consider to ensure that it is being used effectively to manage dependencies and to promote loose coupling between components. This can include using constructor injection for services and repositories, and ensuring that the dependencies are registered appropriately in the DI container. Additionally, consider to review the lifetime of the registered services (e.g., transient, scoped, singleton) to ensure that they are appropriate for their intended use cases and do not introduce unintended side effects or performance issues in the application. This can help to improve the maintainability and scalability of the codebase as it evolves over time.
// todo: setup CI/CD pipeline for the application, consider using GitHub Actions, Azure DevOps, or another CI/CD tool to automate the build, test, and deployment processes for the application. This can help to ensure that changes are properly tested and deployed in a consistent and efficient manner, and can also help to catch issues early in the development process. Additionally, consider to include steps in the pipeline for running unit tests, integration tests, and any other relevant tests to ensure that the application is functioning correctly before it is deployed to production. This can help to improve the overall quality and reliability of the application as it evolves over time.


// important high priority todo: setup docker and containerization for the application, consider creating a Dockerfile to define the container image for the application, and using Docker Compose to manage the multi-container setup if there are additional services (e.g., database) that need to be included. This can help to simplify the deployment process and to ensure that the application can run consistently across different environments. Additionally, consider to review the configuration of the application to ensure that it can be easily configured using environment variables or other mechanisms when running in a containerized environment. This can help to improve the flexibility and maintainability of the application as it evolves over time.
//important high priority todo: setup motor include service: sns, sqs and hangfire for the application, consider using AWS SNS and SQS for messaging and notifications, and Hangfire for background job processing. This can help to improve the scalability and responsiveness of the application by offloading certain tasks to background processing and enabling asynchronous communication between components. Additionally, consider to review the design of the application to ensure that it can effectively leverage these services, such as by implementing appropriate message producers and consumers for SNS and SQS, and by defining relevant background jobs in Hangfire to handle tasks such as syncing data or sending notifications. This can help to enhance the overall functionality and performance of the application as it evolves over time.
