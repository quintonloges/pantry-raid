# Ingredient‑Driven Recipe Search – MVP Specification

---

## 1. Overview

### 1.1 Purpose

Build a web application that helps busy home cooks avoid food waste by discovering recipes they can make using ingredients they already have.

The system matches a user’s on‑hand ingredients against a pre‑scraped recipe database, returning:

* Recipes that can be made immediately
* Recipes that are close (missing 1–3 ingredients)
* Clear indicators for missing ingredients and valid substitutions

Users are always directed to the **original recipe source** to cook.

### 1.2 MVP Principles

* Anonymous‑first usage
* Predictable, transparent matching
* Full credit to recipe authors
* Admin‑driven data curation

---

## 2. User Personas & Core Flow

### 2.1 Primary Persona

Busy home cook trying to minimize food waste and decision fatigue.

### 2.2 Ideal User Flow

1. User lands on `/`
2. Ingredient list loads from:

   * Local storage (anonymous)
   * User account (logged in)
   * Blank (first visit)
3. User adds ingredients via:

   * Ingredient groups (quick add)
   * Autocomplete search
   * Suggested ingredients
4. User optionally sets filters and toggles substitutions
5. Results appear on the same page, grouped by missing ingredient count
6. Clicking a recipe opens the original blog link

---

## 3. UX & UI Specification

### 3.1 Global Header

* Visible on all routes
* Left: Site logo/name (links to `/`)
* Right: Log In / Sign Up (or account menu)
* No admin links exposed

### 3.2 Layout

#### Desktop

* Left sticky sidebar: ingredients + filters
* Right main panel: results

#### Mobile

* Results-first layout
* Ingredients/filters in slide‑out drawer

### 3.3 Ingredient Management

#### Blank State

* Show ingredient group cards
* Clicking a card adds all ingredients immediately

#### Manual Add

* Autocomplete input (controlled list)
* Suggested ingredients ranked by global unlock score
* No duplicates allowed

#### Current List

* Vertical list with remove (X)
* Clear all button
* Auto-save on every change

### 3.4 Filters (Hard Filters)

* Protein (single-select)
* Cuisine (multi-select)
* Dietary tags (multi-select)
* Must include ingredients (multi-select, from user list)
* Recipe source (multi-select)
* Substitution toggle

No free-text search or time filters in MVP.

### 3.5 Results Display

Grouped sections:

* 0 missing
* 1 missing
* 2 missing
* 3 missing

Sorted alphabetically within each group.

#### Recipe Card Contents

* Thumbnail image
* Title
* Source name
* Short description
* Missing ingredient count
* Ingredient preview:

  * You have
  * Missing
  * Substitution notes: `* Substitute with <ingredient>`

Clicking a card opens the original recipe URL.

### 3.6 Empty & Error States

* No results:

  > "No recipes found with your current filters. Try removing some filters or adding more ingredients."
* Backend error:

  > "Something went wrong, please try again"

---

## 4. Frontend Routing (Angular)

### User Routes

* `/` – Main search
* `/about`
* `/account`
* `/login`
* `/register`

### Admin Routes

* `/admin/unmapped-ingredients`
* `/admin/substitutions`
* `/admin/scraping`
* `/admin/ingredient-groups`

Frontend testing: manual only (MVP).

---

## 5. Backend Architecture

### 5.1 Stack

* .NET (latest LTS)
* EF Core (code-first)
* MySQL (Pomelo EF provider)
* NSwag (OpenAPI)

### 5.2 Hosting & Deployment

* Local-first MVP
* Deployment target: DigitalOcean App Platform
* Deploy branch: `release`
* Platform-managed SSL

---

## 6. Authentication & Authorization

### 6.1 Identity

* ASP.NET Core Identity
* Email/password only

### 6.2 MVP Auth Features

* Register / login / logout
* Change password
* Account deletion (soft delete)
* No email flows (verification/reset deferred)

### 6.3 Admin Access

* Role-based (`Admin` role)
* All `/api/admin/*` endpoints restricted

---

## 7. Data Model

All tables include audit columns:
`created_at`, `created_by`, `updated_at`, `updated_by`, `is_deleted`, `deleted_at`, `deleted_by`

### 7.1 Ingredient

* id
* name
* slug (unique)
* aliases (JSON array)
* category
* notes
* global_recipe_count

### 7.2 Ingredient Groups

* ingredient_group
* ingredient_group_item

### 7.3 Metadata Tables (Many-to-Many)

* cuisine
* protein
* dietary_tag

Join tables:

* recipe_cuisine
* recipe_protein
* recipe_dietary_tag

### 7.4 Recipe Source & Recipe

#### recipe_source

* id
* name
* base_url
* scraper_key
* is_active

#### recipe

* id
* title
* source_name
* recipe_source_id
* source_url
* source_recipe_id
* short_description
* image_url
* total_time_minutes
* servings
* raw_html
* scraped_at
* scrape_status

(No instructions stored.)

### 7.5 Recipe Ingredients

`recipe_ingredient`

* id
* recipe_id
* ingredient_id
* original_text
* quantity
* unit
* order_index
* is_optional

### 7.6 User Ingredients

`user_ingredient`

* user_id
* ingredient_id

Max 100 ingredients per user.

### 7.7 Normalization Queue

`unmapped_ingredient`

* id
* recipe_id
* recipe_source_id
* original_text
* suggested_ingredient_id
* resolved_ingredient_id
* status

### 7.8 Substitution Engine

* substitution_group (target ingredient)
* substitution_option
* substitution_option_ingredient

Supports:

* One-to-one substitutions
* Multi-ingredient substitutions
* Chaining with cycle detection

---

## 8. Search API

### Endpoint

`POST /api/search`

### Request

Includes:

* ingredient_ids
* filters
* allow_substitutions
* paging cursor

### Response

Grouped by missing ingredient count (0–3), includes:

* Recipe metadata
* Ingredient breakdown: have / missing / substitutions
* Paging cursor

---

## 9. Matching Algorithm

1. Apply hard filters (AND logic)
2. Evaluate ingredient coverage:

   * Exact match
   * Substitution match (if enabled)
   * Missing
3. Exclude recipes with >3 missing ingredients
4. Group by missing count
5. Sort alphabetically

Substitution rules:

* All required ingredients must be present
* Chaining allowed
* Cycles detected and broken

---

## 10. Scraping Specification

* Manual trigger only
* Identify recipes by URL
* Do not rescrape existing URLs
* Extract all required fields
* If any failure occurs: log and skip
* No detection of deleted recipes in MVP

---

## 11. Admin Capabilities

* Resolve unmapped ingredients
* Manage substitution rules
* Trigger scrapes and retry failures
* Manage ingredient groups

Bare-bones UI acceptable.

---

## 12. API Surface Summary

### Public

* Auth endpoints
* User ingredient bulk replace
* Reference data
* Search

### Admin

* Normalization queue
* Substitutions
* Scraping
* Ingredient groups

---

## 13. Testing Requirements

* Backend: unit + integration tests
* Frontend: manual testing
* CI: tests must pass before deploy

---

## 14. CI/CD & Deployment

* Docker-based build
* Single app container (Angular static + .NET API)
* `.env` files locally
* Env vars in production
* DigitalOcean App Platform
* Auto-deploy from `release` branch

---

## 15. Notes

* Always link to original recipe source
* Never store or display cooking instructions
* Prioritize clarity and predictability over “smart” heuristics

---

**End of spec.md**
