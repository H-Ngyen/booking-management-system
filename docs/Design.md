# Design

Visual system for the Service Booking Management System (customer booking flow + admin console). This file is the source of truth for UI/UX style constraints; the token layer committed in `apps/web/app/globals.css` implements it. Components must follow this contract instead of inventing local styles.

Required screens (from project requirements): `/login`, `/services`, `/booking`, `/my-bookings`, `/admin/services`, `/admin/schedules`, `/admin/bookings`.

## Theme

Light-first, calm, enterprise. One physical scene: a receptionist triaging the day's bookings on a desktop monitor in a bright office, and a customer booking a service from a phone. That forces a **clean light surface with one confident accent** — not a playful consumer app, not a dense ops-tool dark.

- Light mode ships first. Every token below reserves a `dark` slot in the CSS-vars contract so dark mode can land later without touching component code.
- Two shells, one language: customer pages use a top-nav + centered content column; admin pages use a sidebar + topbar + content area. Same tokens, type scale, and component vocabulary in both.
- No decorative color, no glassmorphism, no marketing gradients inside the product.

## Color

Strategy: **Restrained** (SaaS floor). Cool-tinted neutrals carry the surface; a single indigo primary carries identity and primary actions, held well under 10% of any screen; a fixed status vocabulary carries booking state. Color is never the only signal — always paired with a dot + text label.

### Canonical palette (source of truth)

Neutrals (cool slate, never pure `#000` for text):

| Token | Hex | Role |
| --- | --- | --- |
| `bg` | `#F1F5F9` | app shell / body |
| `bg-soft` / page | `#F8FAFC` | page content background |
| `surface` | `#FFFFFF` | card surface |
| `surface-2` | `#F1F5F9` | muted panel / hover |
| `surface-3` | `#E0E7FF` | selected / info highlight |
| `ink` | `#0F172A` | primary text |
| `ink-2…ink-4` | `#334155` `#64748B` `#94A3B8` | descending text emphasis |
| `line` / `line-2` | `#E2E8F0` / `#CBD5E1` | borders, dividers |

Brand + status (each state pairs a foreground with a `-bg` tint):

| Token | Hex | Meaning |
| --- | --- | --- |
| `primary` (indigo) | `#4F46E5` → hover `#4338CA` | identity, primary action, links |
| `pending` | `#B45309` (`#FEF3C7` bg) | booking Pending |
| `confirmed` | `#1D4ED8` (`#DBEAFE` bg) | booking Confirmed |
| `completed` | `#047857` (`#D1FAE5` bg) | booking Completed |
| `cancelled` | `#64748B` (`#F1F5F9` bg) | booking Cancelled |
| `destructive` | `#DC2626` (`#FEE2E2` bg) | conflict / validation errors, cancel actions |

### Token contract (CSS vars + Tailwind v4 `@theme`)

`globals.css` already uses Tailwind v4 (`@import "tailwindcss"` + `@theme inline`). Ship tokens as CSS vars and map them in `@theme` so utilities like `bg-surface text-ink border-line` work. Do not hardcode hex in components.

```
--background  ← page   #F8FAFC        --foreground ← ink    #0F172A
--card        ← surface #FFFFFF        --card-foreground ← ink #0F172A
--muted       ← surface-2 #F1F5F9     --muted-foreground ← ink-3 #64748B
--accent      ← surface-3 #E0E7FF     --accent-foreground ← ink #0F172A
--primary     ← indigo #4F46E5        --primary-foreground ← #FFFFFF
--destructive ← red   #DC2626         --border ← line #E2E8F0
--input       ← #E2E8F0               --ring ← primary #4F46E5
--sidebar     ← #0F172A               --sidebar-foreground ← #94A3B8
--status-pending / -confirmed / -completed / -cancelled (+ matching -bg tints)
```

Rules: primary is the only hue for buttons/links/focus. Status hues appear only in `StatusBadge`, calendar availability dots, and matching admin filter chips. The destructive red is reserved for errors, the 409-conflict state, and cancel confirmations — never for brand.

## Typography

One family: **Inter** (weights 400–700), chosen for Vietnamese diacritic coverage via the `vietnamese` subset and its neutral enterprise register. Booking codes (`BK-YYYYMMDD-XXXX`) use a monospace stack (`ui-monospace, Menlo, Consolas`).

- Current `layout.tsx` loads Geist with `latin` subset only — replace with Inter including the `vietnamese` subset so Vietnamese copy never falls back to a mismatched font.
- Fixed rem scale, not fluid: page title 20–24px/700, section head 16px/600, table header 12px/600 uppercase tracking-wide, body 14px/400–500, helper/error text 12–13px.
- Hierarchy through weight + scale, not color. Prose capped 65–75ch; tables and schedule grids may run denser.

## Iconography

**Lucide** (stroke icons, 24px, stroke-width 2). One icon style everywhere — `lucide-react` in Next.js. Icons label actions and status, never decorate. No emoji as status signals.

## Spacing & Layout

- Customer shell: sticky top nav (logo, Services, My Bookings, user menu) + content column `max-w-5xl`. Admin shell: 240–260px sticky sidebar (Services, Schedules, Bookings) + 60–64px topbar (date context, admin identity, logout) + content on the page surface.
- Predictable patterns per route: `/login` centered card; `/services` searchable + paginated card/table grid with a "Book" action; `/booking` 3-step form (service → staff + date/time → note + confirm) with computed `EndTime` shown before submit; `/my-bookings` filter-by-status + pagination + cancel-with-reason modal; `/admin/*` tables with search, date/status filters, and server-side pagination.
- Breakpoints **1024 / 768 / 640**: admin sidebar → drawer + scrim below 1024; booking steps stack below 768; tables scroll horizontally below 640 with sticky first column. Responsive is structural, not fluid type.
- Cards only when they are the best affordance; never nested. Vary padding for rhythm, not uniform boxes.

## Radius & Elevation

- Radius: cards **10–16px** (`--radius` base 0.625rem), buttons/inputs 8–10px, badges/pills fully rounded. Interactive controls at the tighter end, containers at the wider end.
- Elevation is restrained: hairline `line` borders do the separation; soft low-alpha shadows only on popovers, modals, dropdowns, and toasts. Flat cards carry no shadow.

## Motion

150–250ms, ease-out; no bounce, no elastic. Motion conveys state only: view enter fade, toast slide-up, modal scale-in, skeleton shimmer, spinner for in-flight requests. No orchestrated page-load sequences. Honor `prefers-reduced-motion`. Never animate layout properties (width/height/top/left).

Booking-specific: the slot picker updates availability inline (no full-page reload); on 409 Conflict the error banner appears above the form with the conflicting window and suggested alternatives — the selected slot is preserved, not cleared.

## Components

Every interactive component ships the full state set: default, hover, focus, active, disabled, loading, error. One button shape, one form-control vocabulary, one icon style across customer and admin.

- **Primitives:** Button (variants primary/secondary/ghost/destructive; disabled while submitting), StatusBadge (dot + label, bound to `status-*` tokens), Card, Table + sticky header + row hover, Pagination (server-driven, never client-slice), Tabs/Segmented filter, Input/Textarea/Select with inline error text, DatePicker/TimeSlot grid, Modal (cancel-with-reason, confirm status change), DropdownMenu, Toast, Spinner, Skeleton, EmptyState (teaches next action), ErrorState + retry.
- **Composed blocks:** Service list with search + pagination; slot-availability grid (available/booked/outside-hours visually distinct, outside-hours disabled with tooltip); booking summary card showing computed `EndTime = StartTime + DurationMinutes` before confirm; my-bookings filter bar (date + status); admin schedule editor (no-overlap validation messaging: `StartTime < EndTime`, no duplicate shifts).
- **Loading:** skeletons in content, inline spinners on buttons — never a bare full-page spinner after first paint. **Forms:** client validation mirrors backend rules (name required, duration > 0, price ≥ 0, cancel reason required), submit disabled while the request is in flight, backend errors rendered next to the field or as a banner for 409/403.

## Accessibility & Inclusion

WCAG 2.1 AA. Visible focus ring (`--ring` primary). Full keyboard operability (nav, slot picker, tables, modals). Status never conveyed by color alone (dot + label + weight). Vietnamese diacritics never degraded (Inter `vietnamese` subset). Dialog/menu primitives must provide baseline ARIA roles and focus management. Touch targets ≥ 40px for slot cells and primary actions.

## File Map & Compliance

- Tokens live in `apps/web/app/globals.css` (CSS vars + `@theme`); font loading lives in `apps/web/app/layout.tsx`. No color, font, radius, or shadow literals in components — import from tokens.
- Per-screen checklist before merge: loading / error / empty states present; forms validate + disable on submit; 409 conflict renders a readable message with the taken window; customer views never leak other customers' bookings; no secret or connection string in client code; responsive verified at 1024/768/640.
- Non-goals: dark mode (tokens reserved, not built), payment UI (out of scope per requirements), decorative illustration system.
