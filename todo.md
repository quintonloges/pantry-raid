# Ingredient-Driven Recipe Search – MVP TODO Checklist

> Use this as a running checklist. Prefer small PRs. Every backend change should keep `dotnet test` green.

---

## 0) Repo & Project Setup

- [x] Create monorepo folders:
  - [x] `/src/api`
  - [x] `/src/api.Tests.Unit`
  - [x] `/src/api.Tests.Integration`
  - [x] `/web`
  - [x] `/ops`
- [ ] Add `.editorconfig` (C# + general)
- [x] Add `.gitignore` (Visual Studio, node, env files)
- [x] Add `README.md` with:
  - [ ] Local setup steps
  - [ ] Dev commands (API + web)
  - [ ] How to run tests
  - [ ] How to run docker compose
- [x] Decide/standardize formatting:
  - [ ] `dotnet format` (optional) or IDE formatting baseline
  - [x] `npm` or `pnpm` (pick one and standardize)

---

## 1) API Skeleton + Health + Swagger (NSwag)

- [x] Create ASP.NET Core Web API project (latest LTS)
- [x] Configure NSwag / OpenAPI generation
- [x] Enable Swagger UI in Development only
- [x] Add endpoint `GET /api/health` → `{ "status": "ok" }`
- [x] Add basic API structure:
  - [x] `/Controllers` (or minimal endpoints; pick one style and stick to it)
  - [x] `/Dtos` (DTOs)
  - [x] `/Services` (domain logic)
  - [x] `/EFCore` (EF/Identity/etc.)

### Tests (API)
- [x] Add `api.Tests.Integration` with WebApplicationFactory bootstrapping
- [x] Integration test: `GET /api/health` returns 200 + correct JSON
- [x] Ensure `dotnet test` passes

---

## 2) CI: Run Tests on PR + Main/Release

- [x] Add GitHub Actions workflow:
  - [x] Trigger: `pull_request`, push to `main`, push to `release`
  - [x] Install .NET SDK (latest LTS)
  - [x] Restore/build
  - [x] Run all tests
- [x] Ensure CI is green for initial baseline

---

## 3) EF Core + MySQL + Migrations Baseline

- [x] Add packages:
  - [x] `Microsoft.EntityFrameworkCore`
  - [x] `Microsoft.EntityFrameworkCore.Design`
  - [x] `Pomelo.EntityFrameworkCore.MySql`
- [x] Add `AppDbContext` wired into DI
- [x] Add configuration:
  - [x] `ConnectionStrings:Default` in appsettings
  - [x] Env var override `ConnectionStrings__Default`
- [x] Add endpoint `GET /api/db/ping` that verifies DB connectivity
- [x] Add baseline migration (even if only contains Identity later; start minimal)

### Integration Testing Infrastructure
- [x] Add Testcontainers for integration tests
- [x] Spin up MySQL-compatible container per test collection/fixture
- [x] Configure API to use container connection string
- [x] Apply migrations in test boot
- [x] Integration test: `GET /api/db/ping` returns 200

---

## 4) Audit Columns + Soft Delete (Global)

- [x] Add audit fields to all domain entities:
  - [x] `created_at`, `created_by`
  - [x] `updated_at`, `updated_by`
  - [x] `is_deleted`, `deleted_at`, `deleted_by`
- [x] Add base entity type (e.g., `AuditedEntity`)
- [x] Add global query filter excluding `is_deleted = true`
- [x] Add SaveChanges pipeline (interceptor/service) to set timestamps automatically:
  - [x] on create: set created_at + updated_at
  - [x] on update: set updated_at
- [x] Implement soft delete helper (sets flags instead of removing)
- [x] Add a small “proof” entity (e.g., `SystemNote`) and expose minimal internal usage

### Tests
- [x] Unit test: SaveChanges sets timestamps correctly
- [x] Integration test: soft deleted rows are excluded by default query filter

---

## 5) Authentication & Authorization (Identity + JWT)

### Identity + JWT
- [x] Add ASP.NET Core Identity to API
- [x] Use EF stores backed by MySQL
- [x] Configure JWT auth:
  - [x] issuer, audience, signing key
  - [x] token expiration
- [x] Wire Swagger security scheme for bearer tokens

### Public Auth Endpoints (MVP)
- [x] `POST /api/auth/register` (email, password)
- [x] `POST /api/auth/login` (email, password) → token + expiry
- [x] `GET /api/auth/me` (auth required) → email

### Account Management (MVP)
- [x] `POST /api/auth/change-password` (auth required)
- [x] `DELETE /api/auth/account` (auth required) → soft delete user
- [x] Enforce: soft deleted user cannot login / cannot access `/me`

### Tests (Integration)
- [x] Register → Login → /me success
- [x] Wrong password → 401
- [x] Change password works
- [x] Account delete blocks future login and /me access

---

## 6) Admin Role + Policies + Seeding

- [x] Add role-based auth (`Admin`)
- [x] Add admin-only endpoint: `GET /api/admin/ping` → `{ "admin": "ok" }`
- [x] Add role seeding (idempotent):
  - [x] Ensure Admin role exists
  - [x] Optional bootstrap admin user if env vars provided:
    - [x] `ADMIN_EMAIL`
    - [x] `ADMIN_PASSWORD`

### Tests
- [x] Normal user gets 403 on admin ping
- [x] Bootstrap admin gets 200 on admin ping

---

## 7) Ingredients (Reference + Admin CRUD)

### Data Model
- [x] Add `Ingredient` entity:
  - [x] `id`
  - [x] `name`
  - [x] `slug` (unique)
  - [x] `aliases` (JSON array)
  - [x] `category`
  - [x] `notes`
  - [x] `global_recipe_count`
  - [x] audit + soft delete
- [x] Add unique index on `slug`
- [x] Add case-insensitive uniqueness rules for name (app-level validation)

### Public Endpoint
- [x] `GET /api/reference/ingredients?query=...`
  - [x] Returns controlled list
  - [x] Sorted by `name`

### Admin CRUD
- [x] `POST /api/admin/ingredients`
- [x] `PUT /api/admin/ingredients/{id}`
- [x] `DELETE /api/admin/ingredients/{id}` (soft delete)

### Tests
- [x] Public endpoint returns sorted
- [x] Slug uniqueness enforced
- [x] Deleted ingredient not returned
- [x] Non-admin forbidden for CRUD

---

## 8) Ingredient Groups (Quick Add)

### Data Model
- [x] `IngredientGroup`:
  - [x] id, name, description(optional), audit/soft delete
- [x] `IngredientGroupItem`:
  - [x] id, ingredient_group_id, ingredient_id, order_index

### Public Endpoint
- [x] `GET /api/reference/ingredient-groups`
  - [x] Includes items with ingredient name + id
  - [x] Ordered by `order_index`

### Admin Endpoints
- [x] CRUD groups
- [x] Replace items list (transactional, no orphans)

### Tests
- [x] Admin can create group + items
- [x] Public sees ordered items
- [x] Replace list is idempotent and transactional

---

## 9) User Ingredients (Pantry)

### Data Model
- [x] `UserIngredient` join table:
  - [x] user_id
  - [x] ingredient_id
  - [x] composite unique index

### Endpoints (Auth Required)
- [x] `GET /api/user/ingredients`
- [x] `PUT /api/user/ingredients` (bulk replace ingredient_ids)
  - [x] Enforce max 100
  - [x] Remove duplicates
  - [x] Transactional, idempotent

### Tests
- [x] PUT stores exact set
- [x] >100 returns 400 with clear message
- [x] GET returns sorted by ingredient name

---

## 10) Recipes + Sources + Recipe Ingredients (MVP Seed)

### Data Model
- [x] `RecipeSource`: id, name, base_url, scraper_key, is_active
- [x] `Recipe`: fields from spec (no instructions stored)
- [x] `RecipeIngredient`: recipe_id, ingredient_id, original_text, quantity, unit, order_index, is_optional

### Constraints / Indexes
- [x] Index on `Recipe.source_url` (unique or effectively unique)
- [x] Index on (`recipe_source_id`, `source_recipe_id`) if used
- [x] Enforce soft delete

### Endpoints
- [x] Admin:
  - [x] `POST /api/admin/recipe-sources`
  - [x] `POST /api/admin/recipes` (includes ingredient lines)
- [x] Public:
  - [x] `GET /api/reference/sources`

### Tests
- [x] Admin can create source + recipe + ingredients
- [x] Public sees sources
- [x] Confirm no “instructions” storage exists in schema/contracts

---

## 11) Filter Metadata (Cuisine / Protein / Dietary Tags)

### Data Model
- [x] `Cuisine`, `Protein`, `DietaryTag`
- [x] Join tables:
  - [x] `recipe_cuisine`
  - [x] `recipe_protein`
  - [x] `recipe_dietary_tag`

### Endpoints
- [x] Public:
  - [x] `GET /api/reference/cuisines`
  - [x] `GET /api/reference/proteins`
  - [x] `GET /api/reference/dietary-tags`
- [x] Admin:
  - [x] CRUD each list
  - [x] Replace assignments for recipe tags (idempotent)

### Tests
- [x] Admin creates tags
- [x] Admin assigns tags to recipe
- [x] Public lists tags
- [x] Assignments persist

---

## 12) Search API Contract (DTOs + Endpoint Shell)

- [ ] Define request DTO for `POST /api/search`:
  - [ ] ingredient_ids
  - [ ] filters (protein, cuisines, dietary tags, must include ingredients, sources)
  - [ ] allow_substitutions
  - [ ] paging cursor (nullable, v1 can ignore)
- [ ] Define response DTO:
  - [ ] groups 0–3 missing, always present
  - [ ] recipe metadata
  - [ ] breakdown: have / missing / substitution notes
  - [ ] cursor (nullable)

### Tests
- [ ] Integration test seeds DB and asserts response JSON shape (even if empty results)
- [ ] Swagger reflects DTO schema

---

## 13) Search v1 Implementation (Exact Match Only)

- [ ] Implement hard filters (AND logic):
  - [ ] protein (single)
  - [ ] cuisines (multi)
  - [ ] dietary tags (multi)
  - [ ] must-include ingredients (subset check)
  - [ ] recipe sources (multi)
- [ ] Compute coverage against required ingredients only (`is_optional == false`)
- [ ] Exclude recipes with > 3 missing required ingredients
- [ ] Group results by missing count (0–3)
- [ ] Sort alphabetically by recipe title within each group
- [ ] Return have/missing lists (substitution notes empty)

### Tests (Integration)
- [ ] Exact match shows in group 0
- [ ] 1/2/3 missing grouped correctly
- [ ] >3 missing excluded
- [ ] Each filter includes/excludes correctly
- [ ] Sorting stable and correct

---

## 14) Substitution Schema + Admin Endpoints

### Data Model
- [ ] `SubstitutionGroup` (target ingredient)
- [ ] `SubstitutionOption` (belongs to group)
- [ ] `SubstitutionOptionIngredient` (multi-ingredient option)

### Admin Endpoints
- [ ] CRUD groups/options
- [ ] Replace option ingredient lists

### Tests
- [ ] Admin can create one-to-one substitution
- [ ] Admin can create multi-ingredient substitution option
- [ ] Data loads correctly from DB

---

## 15) Substitution Engine (Pure Logic + Unit Tests)

- [ ] Implement substitution engine service (no EF inside)
- [ ] Supports:
  - [ ] one-to-one substitutions
  - [ ] multi-ingredient substitutions
  - [ ] chaining with cycle detection
  - [ ] deterministic selection when multiple options exist

### Unit Tests
- [ ] one-to-one
- [ ] multi-ingredient
- [ ] chaining
- [ ] cycle detection (A→B→A)
- [ ] deterministic option selection

---

## 16) Integrate Substitutions into Search (allow_substitutions)

- [ ] If allow_substitutions=true:
  - [ ] Treat substituted requirements as satisfied
  - [ ] Update missing count logic accordingly
  - [ ] Populate substitution notes: `* Substitute with <ingredient>` or `* Substitute with <ingredient> + <ingredient>`
- [ ] Ensure transparency: show which ingredients are missing vs substituted

### Tests (Integration)
- [ ] Recipe moves groups when substitutions enabled
- [ ] Cycle does not crash; stable output
- [ ] Substitution notes match expected format

---

## 17) Unmapped Ingredient Queue (Admin)

### Data Model
- [ ] `UnmappedIngredient`:
  - [ ] recipe_id
  - [ ] recipe_source_id
  - [ ] original_text
  - [ ] suggested_ingredient_id
  - [ ] resolved_ingredient_id
  - [ ] status (New/Suggested/Resolved)

### Admin Endpoints
- [ ] `GET /api/admin/unmapped-ingredients?status=...`
- [ ] `PUT /api/admin/unmapped-ingredients/{id}/suggest`
- [ ] `PUT /api/admin/unmapped-ingredients/{id}/resolve`

### Tests
- [ ] List by status
- [ ] Suggest updates fields/status
- [ ] Resolve updates fields/status

---

## 18) Scraping Skeleton (Manual Trigger Only)

- [ ] Define scraper abstraction:
  - [ ] `IScraper.CanHandle(source)`
  - [ ] `IScraper.Scrape(url)` → parsed payload
- [ ] Implement stub scraper for known pattern (dev-only)
- [ ] Admin endpoint: `POST /api/admin/scrape { url }`
  - [ ] Determine source by URL base match
  - [ ] Idempotent by `source_url` (no rescrape)
  - [ ] On success: store recipe + ingredients
  - [ ] Unmapped ingredient lines create queue items
  - [ ] On failure: log + return informative result payload

### Tests (Integration)
- [ ] Scrape same URL twice stores once
- [ ] Unmapped ingredient lines add queue items
- [ ] Failures don’t throw raw exceptions to client

---

## 19) Angular App Scaffold + Routing + Layout

- [ ] Create Angular app in `/web` (latest stable)
- [ ] Add Angular Material
- [ ] Set up routes:
  - [ ] `/` main
  - [ ] `/about`
  - [ ] `/account`
  - [ ] `/login`
  - [ ] `/register`
  - [ ] Admin: `/admin/unmapped-ingredients`, `/admin/substitutions`, `/admin/scraping`, `/admin/ingredient-groups`
- [ ] Global header:
  - [ ] logo/name links to `/`
  - [ ] auth/account menu on right
  - [ ] NO admin links in header
- [ ] Add API client service
- [ ] About page calls `/api/health` and displays result

### Manual checks
- [ ] All routes load
- [ ] API health visible on About

---

## 20) Main Page: Ingredients + Local Storage + Quick Add + Autocomplete

- [ ] Implement desktop layout:
  - [ ] left sticky sidebar for ingredients/filters
  - [ ] right results panel
- [ ] Implement mobile layout:
  - [ ] results-first
  - [ ] slide-out drawer for ingredients/filters
- [ ] Ingredient list behavior:
  - [ ] load from local storage on start (anonymous-first)
  - [ ] auto-save on every change
  - [ ] no duplicates
  - [ ] remove X
  - [ ] clear all
  - [ ] sort alphabetically
- [ ] Blank state:
  - [ ] show ingredient group cards
  - [ ] click adds all ingredients in group immediately
- [ ] Autocomplete:
  - [ ] calls `GET /api/reference/ingredients?query=...`
  - [ ] controlled selection only (no free text ingredient creation)

### Manual checks
- [ ] Refresh persists ingredients
- [ ] Duplicates blocked
- [ ] Quick add groups works

---

## 21) Wire Search Results (Grouped 0–3 Missing)

- [ ] Call `POST /api/search` whenever ingredient list changes
  - [ ] debounce requests (250–400ms)
- [ ] Render grouped sections:
  - [ ] 0 missing
  - [ ] 1 missing
  - [ ] 2 missing
  - [ ] 3 missing
- [ ] Sort alphabetically within group (should already come sorted from API)
- [ ] Recipe card contents:
  - [ ] thumbnail
  - [ ] title
  - [ ] source name
  - [ ] short description
  - [ ] missing count
  - [ ] ingredient preview (have/missing)
  - [ ] substitution notes line(s)
- [ ] Clicking a card opens original `source_url` in a new tab/window

### Empty/error states
- [ ] No results message:
  - [ ] “No recipes found with your current filters. Try removing some filters or adding more ingredients.”
- [ ] Backend error message:
  - [ ] “Something went wrong, please try again”

---

## 22) Filters UI + Substitution Toggle (Wired)

- [ ] Fetch filter options:
  - [ ] cuisines
  - [ ] proteins
  - [ ] dietary tags
  - [ ] sources
- [ ] Implement filters:
  - [ ] Protein (single)
  - [ ] Cuisine (multi)
  - [ ] Dietary tags (multi)
  - [ ] Must include ingredients (multi from current ingredient list)
  - [ ] Recipe source (multi)
  - [ ] Substitution toggle
- [ ] Persist filter state in local storage (anonymous-first)
- [ ] Search updates live when filters change

### Manual checks
- [ ] Filters apply correctly (spot check)
- [ ] Reload preserves filter settings

---

## 23) Auth UI + Token Handling + Account Page

- [ ] Login page:
  - [ ] calls `/api/auth/login`
  - [ ] stores JWT (MVP: localStorage)
- [ ] Register page:
  - [ ] calls `/api/auth/register` then login (optional)
- [ ] HTTP interceptor:
  - [ ] attaches `Authorization: Bearer <token>` when present
- [ ] Account page:
  - [ ] displays current email via `/api/auth/me`
  - [ ] change password UI
  - [ ] account deletion with confirmation

### Manual checks
- [ ] Register/login works end-to-end
- [ ] Token persists and `/account` loads after refresh
- [ ] Delete account blocks future logins

---

## 24) Admin UI (Bare Bones, No Header Links)

- [ ] Add route guards:
  - [ ] non-admin cannot access admin routes
  - [ ] show “Not authorized” page if blocked
- [ ] Admin pages (minimal):
  - [ ] Unmapped ingredients:
    - [ ] list by status
    - [ ] resolve/suggest controls
  - [ ] Substitutions:
    - [ ] list/create/edit groups/options
  - [ ] Scraping:
    - [ ] trigger scrape by URL
    - [ ] show result payload
  - [ ] Ingredient groups:
    - [ ] CRUD groups and assign ingredients

### Manual checks
- [ ] Admin pages reachable only as admin
- [ ] No admin navigation exposed in global header

---

## 25) Docker & Local Dev Compose

- [ ] Create production `Dockerfile`:
  - [ ] build Angular
  - [ ] publish API
  - [ ] serve Angular static from API `wwwroot`
- [ ] Add `/ops/docker-compose.yml`:
  - [ ] mysql service
  - [ ] api service pointing at mysql
- [ ] Add migration strategy:
  - [ ] Development: auto-apply migrations
  - [ ] Production: require `APPLY_MIGRATIONS=true`

### Manual checks
- [ ] `docker compose up` runs api + mysql
- [ ] App reachable, API can connect to DB

---

## 26) CI Enhancements (Docker Build + Release Guardrails)

- [ ] Update CI:
  - [ ] tests must pass first
  - [ ] build docker image
- [ ] Ensure release branch runs full pipeline
- [ ] (Optional) Save docker image as artifact (no secrets required)

---

## MVP Polishing & Safety Checks

- [ ] Confirm: always link to original recipe source (never embed instructions)
- [ ] Confirm: no “instructions” fields in DB, DTOs, UI
- [ ] Confirm: predictable results (no “smart” heuristics beyond spec)
- [ ] Confirm: admin endpoints are protected server-side
- [ ] Confirm: clear error messages match spec
- [ ] Confirm: max 100 ingredients enforced for logged-in pantry
- [ ] Confirm: substitutions are transparent (notes always shown when used)
- [ ] Confirm: cycle detection prevents runaway substitution logic

---

## Suggested Commands (Quick Reference)

### Backend
- [ ] `dotnet restore`
- [ ] `dotnet build`
- [ ] `dotnet test`

### Angular
- [ ] `npm install`
- [ ] `npm start` (or `ng serve`)

### Local Docker
- [ ] `docker compose -f ops/docker-compose.yml up --build`

---

## Done Criteria (MVP)

- [ ] Anonymous user can add ingredients + filters and see grouped results (0–3 missing)
- [ ] Recipe card opens original source link
- [ ] Login/register/account mgmt works (no email flows)
- [ ] Admin can manage ingredients, groups, substitutions, and unmapped queue
- [ ] Manual scraping trigger can add recipes (stub acceptable)
- [ ] Backend tests (unit + integration) pass in CI
- [ ] Single-container build works and runs locally via docker compose
