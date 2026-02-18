# Architecture Review & Recommendations

> This document contains a review of the proposed `docs/architecture.md` and provides feedback, identifies missing details, and proposes solutions to potential issues.

## 1. Initial Architecture Review

The `architecture.md` document is comprehensive and demonstrates a strong architectural vision. The feedback below focuses on clarifying potential ambiguities, addressing missing details, and questioning certain trade-offs.

### 1.1. Missing Details

The document provides a high-level overview, but several critical implementation details that significantly impact the project's robustness, security, and maintainability are not specified.

*   **Configuration & Secret Management:** The document mentions Aspire for local development, but it's silent on production environments. How will secrets (Discord tokens, API keys, OAuth client secrets) and environment-specific configurations be managed in a deployed setting? A strategy using environment variables, a secrets vault (like Azure Key Vault or HashiCorp Vault), or Docker secrets is needed.
*   **Database Migration Strategy:** The use of EF Core is clear, but the strategy for applying database migrations is not defined. Will migrations be applied automatically on application startup (risky in multi-instance deployments) or as a separate, explicit step in a CI/CD pipeline?
*   **Detailed Testing Strategy:** The document names TUnit as the testing framework but doesn't outline a testing strategy. What is the expected scope for unit, integration, and end-to-end tests? How will the API layer be tested independently of the UI?
*   **Error Handling & Logging:** Beyond mentioning OTel for telemetry, the document lacks a coherent error handling and logging strategy. How will API errors be structured and propagated to clients? What is the structured logging strategy, and where will logs be aggregated?
*   **Web Frontend Architecture:** The plan for a "Vite + React SPA" needs more detail, including the choice of a state management library (e.g., Redux Toolkit, Zustand), a data fetching/caching solution (e.g., React Query, SWR), and a styling approach (e.g., Tailwind CSS, CSS Modules).

### 1.2. Questionable Architecture Choices

These points are not necessarily wrong, but they represent significant trade-offs that warrant further discussion and justification.

*   **Dropping `Dockerfile`:** The document states the `Dockerfile` is dropped because "Aspire handles containerization." This is a significant concern. Standalone `Dockerfile`s provide portability, are CI/CD-friendly, and align with universal container ecosystem practices. Aspire should be configured to *use* existing Dockerfiles, not replace them.
*   **CSRF Protection Strategy:** The plan to use a custom `X-Requested-With` header check is a legacy approach. Modern, more secure applications should use the built-in ASP.NET Core anti-forgery token generation and validation.
*   **Bot-to-API Communication:** The choice to have the Bot communicate with the API via synchronous HTTP introduces tight coupling and a single point of failure. An alternative using a message queue should be considered to improve resilience.
*   **Use of NCalc for Score Evaluation:** The inclusion of NCalc implies dynamic, string-based expression evaluation. If these expressions could ever be influenced by user-provided data, this presents a significant security risk (injection attacks) and a potential performance bottleneck.

### 1.3. Other Concerns & Recommendations

*   **Player Linking User Experience:** The proposed flow ("prompted to tag which platform users") could create significant user friction. It would be beneficial to brainstorm a more streamlined UX, such as suggesting mappings based on name similarity.
*   **Multi-Play File Handling:** Posting multiple embeds for a multi-play file is likely to be noisy. A better approach is to post a single summary embed with a link to a detailed view in the web application.
*   **API Key Rotation:** While a static key is sufficient for a simple deployment, building in a simple mechanism for rotation is a low-cost, high-value security feature.
*   **Technology Stack Versioning:** Targeting unreleased future versions of .NET is risky. It would be safer to target the latest stable LTS or STS versions.

---

## 2. Proposed Solution: Wolverine.Fx Integration

A discussion was held about leveraging the **Wolverine.Fx** library to address some of the concerns above. The conclusion is that adopting it would be a significant architectural improvement.

### 2.1. How Wolverine Solves Key Concerns

*   **Decouples Bot-to-API Communication:** By using Wolverine's asynchronous messaging, the Bot can send a command to a persistent queue instead of making a direct HTTP call. This decouples the services and makes the system more resilient; if the API is down, the request is simply processed later. This directly resolves the concern about the brittle `Bot -> API (HTTP)` link.
*   **Promotes a Clearer, More Testable Architecture:** Wolverine's command/handler pattern provides a clear structure for business logic. Handlers are easy to isolate and unit test, directly addressing the need for a better testing strategy for core logic.
*   **Provides Robust Error Handling:** Wolverine has a rich, declarative middleware for handling transient errors. Policies for automatic retries or moving failed messages to a dead-letter queue can be configured easily, filling the error handling gap identified in the initial review.

### 2.2. What Wolverine Doesn't Solve

Wolverine is a backend framework and would not address the following concerns:

*   Configuration & Secret Management in Production.
*   The need for a `Dockerfile` for portable builds.
*   CSRF Protection Strategy.
*   Web Frontend architectural choices.
*   Database Migration Strategy.

### 2.3. Conclusion

Adopting Wolverine.Fx is highly recommended. It directly mitigates the risks associated with service-to-service communication, provides a robust framework for error handling, and promotes a clean, highly testable command-handler architecture that is well-suited to the project's goals.
