Blueprint
Repository layout (monorepo)
/src
  /api                 # ASP.NET Core Web API (+ serves Angular static build in prod)
  /api.Tests.Unit      # xUnit unit tests
  /api.Tests.Integration# xUnit integration tests (real DB via Testcontainers)
/web                  # Angular app (Angular Material)
/ops
  docker-compose.yml   # local dev stack (api + mysql)
  Dockerfile           # production build
/.github/workflows     # CI

Key architectural decisions (MVP-friendly, testable)

Backend: ASP.NET Core (latest LTS), EF Core code-first, Pomelo MySQL provider, NSwag OpenAPI.

Auth: ASP.NET Core Identity + JWT (simpler SPA integration than cookies/CSRF for MVP).

Soft delete + audit columns: enforced centrally via EF Core interceptors / SaveChanges pipeline.

Integration tests: real MySQL-compatible DB using Testcontainers (fast enough, catches real-world issues).

Frontend: Angular (latest stable) + Angular Material. Manual testing only, but keep code structured and deterministic.

Development workflow

Every step:

Add/adjust tests first (unit or integration).

Implement minimal code to pass.

Wire into DI/routing.

Ensure dotnet test passes.

Avoid big-bang migrations: introduce entities/endpoints in small vertical slices.

Test strategy (practical + strong)

Unit tests: pure logic (matching + substitution engine + cycle detection).

Integration tests: API + EF Core + MySQL schema + auth enforcement.

Frontend: manual smoke checks per iteration.

Iteration breakdown (right-sized)
Epic 0 — Foundation

API skeleton + swagger + health

Unit + integration test projects + CI pipeline

Epic 1 — Persistence + Identity

EF Core + MySQL + migrations + testcontainers infra

Audit/soft-delete plumbing

Identity + JWT auth + admin role seeding

Epic 2 — Reference data + user pantry

Ingredients reference endpoints + admin CRUD

Ingredient groups (quick add) endpoints

User ingredients (bulk replace) endpoints + max=100

Epic 3 — Recipes + Search (no substitutions)

Recipe sources + recipes + recipe ingredients (admin seed endpoints for MVP)

Filter metadata (cuisine/protein/dietary) + joins

/api/search v1: exact matching + missing counts + grouping + filters

Epic 4 — Substitutions + normalization queue

Substitution data model

Substitution engine (unit-tested) + integrate into search

Unmapped ingredient queue endpoints (admin)

Epic 5 — Scraping (manual trigger)

Scraper abstraction + admin trigger endpoint (idempotent by URL)

Store scrape status + logging + retry endpoint

Epic 6 — Angular MVP UI (wired end-to-end)

Angular shell + routes + Material layout

Ingredient list UX + local storage + autocomplete + ingredient groups

Filters + substitution toggle wired to search

Results grouped (0–3 missing) + card UI + open source links

Auth screens + account mgmt + admin route gating (no admin links in header)

Epic 7 — Containerization + deploy readiness

Single-container build (Angular static + API) + compose stack

CI “tests must pass” + docker build on release

One more round: smallest safe steps (no big jumps)

Below is the final “right-sized” sequence: each step is small enough to implement safely with tests, but moves the project forward and leaves no orphaned code.

Code-generation prompts (TDD, incremental, fully wired)

How to use: Run these prompts in order. Each assumes the repo state produced by the previous prompt.

Prompt 01 — Create solution skeleton + health + NSwag
You are implementing the Ingredient-Driven Recipe Search MVP.

Task:
1) Create a new .NET (latest LTS) solution in /src/api with an ASP.NET Core Web API project.
2) Add NSwag (OpenAPI) and expose swagger UI in Development.
3) Add a simple GET /api/health endpoint returning { "status": "ok" }.
4) Create test projects:
   - /src/api.Tests.Unit (xUnit)
   - /src/api.Tests.Integration (xUnit)
5) Add a minimal integration test that boots the API using WebApplicationFactory and asserts GET /api/health returns 200 and status=ok.
6) Ensure everything builds and `dotnet test` passes.

Constraints:
- Keep the API minimal (no DB yet).
- Wire all routes; no unused code.
- Use idiomatic ASP.NET Core minimal hosting model.

Deliverables:
- Solution + projects + references
- Passing unit/integration test

Prompt 02 — Add CI workflow to run tests
Task:
1) Add a GitHub Actions workflow that:
   - checks out code
   - sets up .NET SDK (latest LTS)
   - restores, builds
   - runs `dotnet test` for the solution
2) Ensure it triggers on pull_request and pushes to main and release.

Constraints:
- Keep it simple; no docker build yet.
- Do not break local dev.

Deliverable:
- .github/workflows/ci.yml

Prompt 03 — Add EF Core + MySQL provider + migrations baseline
Task:
1) Add EF Core and Pomelo.EntityFrameworkCore.MySql to the API.
2) Create an AppDbContext (empty for now, but wired).
3) Add configuration via appsettings + environment variables:
   - ConnectionStrings__Default
4) Add an endpoint GET /api/db/ping that opens a DB connection and returns { "db": "ok" } on success.
5) Add the first EF Core migration and ensure it can be applied.

Testing:
- Integration test using Testcontainers:
  - Start a MySQL-compatible container
  - Configure the API connection string to point at it
  - Apply migrations on startup (in test-only mode)
  - Call /api/db/ping and assert 200 with db=ok

Constraints:
- Do not introduce any domain tables yet.
- Make DB migration application explicit and test-safe.
- All code must be wired and used.

Prompt 04 — Add audit columns + soft delete base entity
Task:
1) Implement audit columns and soft delete for all domain entities:
   created_at, created_by, updated_at, updated_by, is_deleted, deleted_at, deleted_by
2) Add a base entity type (e.g., AuditedEntity) and configure EF Core:
   - Global query filter to exclude is_deleted == true
3) Implement a SaveChanges interceptor/service that sets created/updated timestamps automatically.
4) Add a small sample entity (e.g., SystemNote) to prove the pipeline works, including a soft-delete operation.

Testing:
- Unit test: SaveChanges sets created_at/updated_at.
- Integration test: create SystemNote, soft delete it, ensure it is excluded by default query filter.

Constraints:
- Keep “created_by/updated_by” nullable for now (wire user later).
- No unused abstractions.

Prompt 05 — Add ASP.NET Identity + JWT auth (register/login)
Task:
1) Add ASP.NET Core Identity using EF Core stores in AppDbContext.
2) Configure JWT authentication.
3) Add auth endpoints:
   - POST /api/auth/register (email, password)
   - POST /api/auth/login (email, password) -> returns access token + expires info
4) Add a protected endpoint GET /api/auth/me that returns current user’s email.

Testing (integration):
- Register -> Login -> Call /api/auth/me with Bearer token and assert email matches.
- Invalid login returns 401.

Constraints:
- No email verification/reset flows.
- Keep responses minimal and predictable.
- Wire Swagger security scheme for JWT so it’s testable.

Prompt 06 — Add change password + soft-delete account
Task:
1) Add:
   - POST /api/auth/change-password (currentPassword, newPassword) [auth required]
   - DELETE /api/auth/account [auth required] -> soft delete user (do not hard delete row)
2) Ensure soft-deleted users cannot log in anymore and cannot access /me.

Testing (integration):
- Register -> login -> change password -> login with new password works.
- Account deletion -> login fails -> /me fails.

Constraints:
- Use the same soft delete pattern as domain entities.
- Avoid breaking Identity invariants (implement carefully).

Prompt 07 — Add Admin role + admin-only policy + seed
Task:
1) Implement role-based authorization:
   - Admin role
2) Add an admin-only endpoint GET /api/admin/ping that returns { "admin": "ok" }.
3) Add startup seeding:
   - Ensure Admin role exists
   - Optionally create a bootstrap admin user if env vars ADMIN_EMAIL and ADMIN_PASSWORD are provided.

Testing (integration):
- Normal user cannot access /api/admin/ping (403).
- Bootstrap admin can access /api/admin/ping (200).

Constraints:
- Do not expose admin links anywhere (backend only for now).
- Seeding must be idempotent.

Prompt 08 — Ingredient entity + public reference endpoint
Task:
1) Add Ingredient entity per spec:
   - id, name, slug (unique), aliases (JSON), category, notes, global_recipe_count
   - audit + soft delete
2) Add endpoints:
   - GET /api/reference/ingredients?query=... (returns controlled list; query optional)
3) Add unique index on slug; slug generated from name (simple deterministic slugify).

Testing:
- Integration: create a few ingredients directly via DbContext (in test) and ensure endpoint returns them sorted by name.
- Integration: slug uniqueness enforced.

Constraints:
- Do not add admin CRUD yet.
- Keep response DTOs (don’t leak EF entities).

Prompt 09 — Admin ingredient CRUD
Task:
Add admin endpoints (Admin role required):
- POST /api/admin/ingredients
- PUT /api/admin/ingredients/{id}
- DELETE /api/admin/ingredients/{id} (soft delete)

Testing (integration):
- Admin can create/update/delete.
- Non-admin forbidden.
- Deleted ingredient does not appear in GET /api/reference/ingredients.

Constraints:
- Validate: no duplicate names (case-insensitive) and no duplicate slugs.
- Keep validation simple (model-state + minimal checks).

Prompt 10 — Ingredient Groups endpoints (quick add)
Task:
1) Add IngredientGroup + IngredientGroupItem tables:
   - IngredientGroup: id, name, description (optional), audit/soft delete
   - IngredientGroupItem: id, ingredient_group_id, ingredient_id, order_index
2) Public endpoint:
   - GET /api/reference/ingredient-groups -> returns groups with ingredient IDs + names
3) Admin endpoints:
   - POST/PUT/DELETE for groups
   - PUT group items (replace full list)

Testing (integration):
- Admin can create group with items.
- Public can fetch and see items ordered by order_index.

Constraints:
- Replacement update must be transactional.
- No orphaned items.

Prompt 11 — User ingredients (pantry) endpoints
Task:
1) Add UserIngredient table:
   - user_id, ingredient_id (composite unique)
2) Endpoints (auth required):
   - GET /api/user/ingredients
   - PUT /api/user/ingredients (bulk replace with ingredient_ids)
3) Enforce max 100 ingredients per user.

Testing (integration):
- Bulk replace stores exactly what is provided (no duplicates).
- Over 100 returns 400 with a clear message.
- GET returns sorted by ingredient name.

Constraints:
- PUT must be idempotent and transactional.
- Use DTOs; don’t expose join entity directly.

Prompt 12 — RecipeSource + Recipe + RecipeIngredient schema + admin seed endpoints
Task:
1) Add entities:
   - RecipeSource: id, name, base_url, scraper_key, is_active
   - Recipe: id, title, recipe_source_id, source_url, source_recipe_id, short_description, image_url,
            total_time_minutes, servings, raw_html, scraped_at, scrape_status
   - RecipeIngredient: id, recipe_id, ingredient_id, original_text, quantity, unit, order_index, is_optional
2) Add admin endpoints to create minimal recipes for MVP testing:
   - POST /api/admin/recipe-sources
   - POST /api/admin/recipes (includes ingredient lines referencing ingredient_id)
3) Add public endpoint:
   - GET /api/reference/sources

Testing (integration):
- Admin can create a source and a recipe with ingredients.
- Public can list sources.
- Enforce: do not store cooking instructions; ensure no “instructions” field exists anywhere.

Constraints:
- Keep scrape fields nullable for now; status can be enum/string.
- Ensure foreign keys and indexes on source_url and (recipe_source_id, source_recipe_id).

Prompt 13 — Filter metadata tables + public reference endpoints
Task:
1) Add Cuisine, Protein, DietaryTag tables + join tables:
   - recipe_cuisine, recipe_protein, recipe_dietary_tag
2) Add public endpoints:
   - GET /api/reference/cuisines
   - GET /api/reference/proteins
   - GET /api/reference/dietary-tags
3) Add admin endpoints to manage these lists and assign tags to recipes.

Testing (integration):
- Admin creates tags and assigns to a recipe.
- Public can list tags.
- Tag assignments persist.

Constraints:
- Keep operations “replace list” style for recipe tag assignments (idempotent).

Prompt 14 — Search DTOs + contract-first tests (no implementation yet)
Task:
1) Define POST /api/search request/response DTOs per spec:
Request includes:
- ingredient_ids: int[]
- filters: { proteinId?, cuisineIds?, dietaryTagIds?, mustIncludeIngredientIds?, sourceIds? }
- allow_substitutions: bool
- paging cursor (optional; can be null in v1)
Response includes groups 0-3 missing:
- for each recipe: metadata + have/missing lists + substitution notes (empty in v1)
2) Add integration tests that:
- Seed DB with 2-3 recipes + ingredients
- Call /api/search and assert the JSON shape matches and group keys exist (even if empty arrays)

Constraints:
- Implementation can return empty results for now, but must match contract.
- Wire endpoint and swagger schema.

Prompt 15 — Search v1 implementation: exact match + missing count + grouping
Task:
Implement /api/search v1:
1) Apply hard filters (AND logic):
   - protein (single)
   - cuisines (multi)
   - dietary tags (multi)
   - must include ingredients (subset of user ingredient_ids)
   - sources (multi)
2) Compute coverage:
   - required ingredients are recipe_ingredient where is_optional == false
   - missing count = required - provided
   - exclude recipes with missing > 3
3) Group into 0,1,2,3 missing.
4) Sort recipes alphabetically within each group by title.
5) For each recipe return:
   - have ingredients (intersection)
   - missing ingredients (difference)
   - substitution notes empty in v1

Testing (integration):
- Multiple cases:
  - exact match shows in group 0
  - 1 missing shows in group 1
  - >3 missing excluded
  - filters correctly include/exclude

Constraints:
- Do not implement paging yet; cursor can be null.
- Query must be efficient enough for MVP (use EF queries, minimal N+1).

Prompt 16 — Substitution schema
Task:
Add substitution tables:
- substitution_group (target ingredient)
- substitution_option (belongs to group)
- substitution_option_ingredient (option may require multiple ingredients)

Add admin endpoints:
- CRUD groups/options
- Replace option ingredient lists

Add public endpoint (optional for debugging):
- GET /api/reference/substitutions (can be admin-only if preferred)

Testing (integration):
- Admin can create a target ingredient substitution with a multi-ingredient option.

Constraints:
- Model must support chaining later.
- Keep deletes soft.

Prompt 17 — Substitution engine (unit-tested, no API integration yet)
Task:
Implement a substitution evaluation engine as a pure service with unit tests.

Inputs:
- recipe required ingredient IDs
- user ingredient IDs
- substitution rules
Output:
- which required ingredients are satisfied via exact match
- which are satisfied via substitutions (with notes like "Substitute with X" or "Substitute with X + Y")
- which remain missing
Rules:
- Chaining allowed (A can be substituted by B, and B by C) but must detect and break cycles.
- Prefer shortest substitution chain.
- Deterministic results.

Testing (unit):
- One-to-one substitution
- Multi-ingredient substitution
- Chaining
- Cycle detection (A->B->A)
- Deterministic selection when multiple options exist (e.g., lowest option id)

Constraints:
- No EF code inside this engine.
- Keep it small and well-documented.

Prompt 18 — Integrate substitutions into search (allow_substitutions toggle)
Task:
Update /api/search:
- If allow_substitutions == true, use substitution engine to satisfy required ingredients.
- Missing count should treat substituted ingredients as satisfied.
- Populate substitution notes in response.

Testing (integration):
- Seed substitution rules + recipes.
- Verify:
  - recipe moves from group 1 to group 0 when substitution enabled
  - cycle does not crash; yields stable result
  - substitution notes appear exactly as specified

Constraints:
- Keep output predictable and transparent.
- Do not add heuristics beyond rules.

Prompt 19 — Unmapped ingredient queue (admin)
Task:
Add UnmappedIngredient entity/table per spec:
- recipe_id, recipe_source_id, original_text, suggested_ingredient_id, resolved_ingredient_id, status
Add admin endpoints:
- GET /api/admin/unmapped-ingredients?status=...
- PUT /api/admin/unmapped-ingredients/{id}/resolve (resolved_ingredient_id)
- PUT /api/admin/unmapped-ingredients/{id}/suggest (suggested_ingredient_id)

Testing (integration):
- Create queue items, list by status, resolve, verify status changes.

Constraints:
- Keep status as enum/string with a small set: New, Suggested, Resolved.
- All admin-protected.

Prompt 20 — Scraper abstraction + manual trigger endpoint (stub)
Task:
Implement scraping skeleton (no real scraping yet):
1) Define IScraper interface:
   - CanHandle(source)
   - Scrape(url) -> returns parsed recipe fields + ingredient lines
2) Implement a stub scraper for development that returns a fixed recipe payload for a known URL pattern.
3) Admin endpoint:
   - POST /api/admin/scrape { url }
Behavior:
- Determine source by URL base_url match
- If recipe with source_url already exists, do not rescrape (idempotent) and return existing recipe id
- On scrape success, create recipe + recipe_ingredients (unmapped ones go to unmapped_ingredient queue)

Testing (integration):
- Trigger scrape twice -> only one recipe stored.
- Unmapped ingredient lines create queue items.

Constraints:
- Log failures and return 200 with a result payload indicating skipped/failed/success (no throwing raw exceptions).

Prompt 21 — Angular scaffold + routes + API client
Task:
Create Angular app in /web using Angular Material.
1) Routes:
   /, /about, /account, /login, /register
   Admin routes scaffolded (components can be placeholders):
     /admin/unmapped-ingredients, /admin/substitutions, /admin/scraping, /admin/ingredient-groups
2) Global header with logo (links to /) and right-side auth/account entry.
3) Implement a minimal API client service and a health check call displayed on About page.

Constraints:
- No frontend automated tests (manual only), but keep code clean and modular.
- No admin links exposed in header.

Prompt 22 — Ingredient sidebar UX: local storage + groups + autocomplete + search call
Task:
Implement the main / page layout per spec:
- Desktop: left sticky sidebar (ingredients + filters), right results panel
- Mobile: results-first with a slide-out drawer for ingredients/filters

Features:
1) Ingredient list stored in local storage (anonymous-first), auto-save on every change.
2) Blank state shows ingredient group cards; clicking adds all ingredients.
3) Autocomplete input using GET /api/reference/ingredients.
4) No duplicates; remove X; clear all.
5) On any change, call POST /api/search and render results grouped 0–3.

Constraints:
- Sorting: ingredient list alphabetical.
- Debounce search calls (e.g., 250–400ms).
- Show exact empty/error messages from spec.

Prompt 23 — Filters UI + substitution toggle wired to search
Task:
Add filters UI in the sidebar:
- Protein (single-select)
- Cuisine (multi)
- Dietary tags (multi)
- Must include ingredients (multi; from current user ingredient list)
- Recipe sources (multi)
- Substitution toggle

Wire:
- Load filter options from reference endpoints.
- Include filters + allow_substitutions in search request.
- Results update live.

Constraints:
- No free-text search.
- Keep filter state in local storage for anonymous users.

Prompt 24 — Auth UI + account management + admin route guarding
Task:
Implement:
1) Login/Register pages calling /api/auth/register and /api/auth/login.
2) Store JWT token (MVP: localStorage) and add an HTTP interceptor to attach Authorization header.
3) Account page:
   - show current email via /api/auth/me
   - change password
   - delete account (with confirmation)
4) Admin route guard:
   - Determine admin role from a /api/auth/me-expanded endpoint (add it in API if needed) or token claims.
   - Ensure admin routes require admin; show a plain "Not authorized" page if blocked.

Constraints:
- Do not add admin links to header.
- Keep messaging professional and minimal.
- Ensure backend endpoints are already enforcing Admin role regardless of UI.

Prompt 25 — Single-container Docker build + local compose
Task:
1) Add a production Dockerfile that:
   - builds Angular
   - builds/publishes API
   - serves Angular static assets from the API (wwwroot)
2) Add /ops/docker-compose.yml for local dev:
   - mysql service
   - api service (reads ConnectionStrings__Default)
3) Add a simple startup migration application strategy:
   - In Development: apply migrations automatically
   - In Production: require explicit env flag APPLY_MIGRATIONS=true

Testing:
- Update CI to build the docker image (but still run dotnet tests first).

Constraints:
- Must remain local-first.
- No orphan scripts; everything runnable with documented commands in README.

Prompt 26 — CI/CD guardrails for release
Task:
Update CI so that:
- On push to release:
  - run tests
  - build docker image
  - (optional) publish artifact or tag image (no deployment credentials assumed)

Constraints:
- “Tests must pass for CI” is mandatory.
- Keep secrets out of repo.