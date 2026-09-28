// VN wall-time formatting. The backend stores and returns Vietnam wall time
// as plain ISO strings WITHOUT offset ("YYYY-MM-DDTHH:mm[:ss]") — there is no
// timezone conversion anywhere. These helpers slice the string directly so the
// same booking renders identically on every browser, in every timezone.

function pad(n: string): string {
  return n.padStart(2, '0');
}

/** "2026-09-28T08:00:00" (or with seconds/fraction) -> "08:00". */
export function formatTimeVN(iso: string): string {
  const t = iso.slice(11, 16);
  return /^\d{2}:\d{2}$/.test(t) ? t : iso;
}

/** "2026-09-28T08:00:00" -> "28/09/2026, 08:00". */
export function formatDateTimeVN(iso: string): string {
  const d = iso.slice(0, 10).split('-');
  if (d.length !== 3) return iso;
  return `${pad(d[2])}/${pad(d[1])}/${d[0]}, ${formatTimeVN(iso)}`;
}

/** Shift chip from WorkScheduleDto parts: ("2026-09-28", "08:00") -> "08:00". */
export function formatShiftTime(hhmm: string): string {
  return /^\d{2}:\d{2}/.test(hhmm) ? hhmm.slice(0, 5) : hhmm;
}

/** Wall-time minute arithmetic on "YYYY-MM-DDTHH:mm[:ss]" (handles day rollover). */
export function addMinutes(iso: string, minutes: number): string {
  const m = /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})/.exec(iso);
  if (!m) return iso;
  const base = Date.UTC(+m[1], +m[2] - 1, +m[3], +m[4], +m[5]) + minutes * 60_000;
  const d = new Date(base);
  const p = (n: number) => String(n).padStart(2, '0');
  return `${d.getUTCFullYear()}-${p(d.getUTCMonth() + 1)}-${p(d.getUTCDate())}T${p(d.getUTCHours())}:${p(d.getUTCMinutes())}`;
}

/** DateOnly "YYYY-MM-DD" -> "28/09/2026" (+ optional weekday short in Vietnamese). */
export function formatDateVN(dateOnly: string, opts?: { weekday?: boolean }): string {
  const d = dateOnly.split('-');
  if (d.length !== 3) return dateOnly;
  const base = `${pad(d[2])}/${pad(d[1])}/${d[0]}`;
  if (!opts?.weekday) return base;
  const wd = ['CN', 'T2', 'T3', 'T4', 'T5', 'T6', 'T7'][new Date(`${dateOnly}T00:00:00`).getDay()] ?? '';
  return wd ? `${wd}, ${base}` : base;
}
