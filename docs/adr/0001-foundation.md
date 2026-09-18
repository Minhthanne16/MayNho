# ADR 0001 — Foundation, time and email

Status: accepted for local implementation; release validation pending.

Follow the supplied plan without architecture substitution: React 19 SPA, ASP.NET Core 10 API, PostgreSQL 17 with EF Core, independent worker, same-origin production hosting. NodaTime/IANA is the time boundary. Resend sends from an application-owned verified domain, not from a user's Gmail account; Mailpit is local only. No Redis, Gmail API, AI, or recurrence in P0.

On initial inspection the repository contained only the plan. Node reports 24.19.0. No .NET SDK is installed globally. Install the official SDK into ignored `.tools/dotnet`, without modifying machine PATH. Microsoft release metadata returned SDK 10.0.401/runtime 10.0.12. These are candidates until compilation succeeds. Docker CLI exists but its engine is unavailable. Integration tests must not fall back to EF InMemory.

Production deployment, digest promotion, verified sender and restore drills remain gated on real infrastructure. Local setup must not imply release readiness.
