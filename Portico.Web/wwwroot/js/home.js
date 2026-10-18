"use strict";

// The server renders the stays, the trending cards, and the city list. This script only runs the controls on top of them.

const DOW = ["Mo", "Tu", "We", "Th", "Fr", "Sa", "Su"];
const LIMITS = { adults: [1, 8], children: [0, 6], rooms: [1, 4] };

const state = {
  checkIn: null,
  checkOut: null,
  hover: null,
  adults: 2,
  children: 0,
  rooms: 1,
  viewMonth: startOfMonth(new Date()),
  active: null
};

const $ = (id) => document.getElementById(id);
const header = $("header");
const nav = $("nav");
const searchForm = $("searchForm");
const whereInput = $("whereInput");
const whereField = $("whereField");
const wherePopover = $("wherePopover");
const datesField = $("datesField");
const checkInBtn = $("checkInBtn");
const checkOutBtn = $("checkOutBtn");
const calPopover = $("calPopover");
const guestField = $("guestField");
const guestBtn = $("guestBtn");
const guestPopover = $("guestPopover");
const formError = $("formError");
const stayList = $("stayList");
const trendTrack = $("trendTrack");
const drawer = $("drawer");

let lastFocus = null;

function startOfMonth(date) {
  return new Date(date.getFullYear(), date.getMonth(), 1);
}

function addMonths(date, count) {
  return new Date(date.getFullYear(), date.getMonth() + count, 1);
}

function dayTime(date) {
  return Date.UTC(date.getFullYear(), date.getMonth(), date.getDate());
}

function toKey(date) {
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${date.getFullYear()}-${month}-${day}`;
}

function parseDay(key) {
  const [year, month, day] = key.split("-").map(Number);
  return new Date(year, month - 1, day);
}

function isPast(date) {
  const now = new Date();
  return dayTime(date) < Date.UTC(now.getFullYear(), now.getMonth(), now.getDate());
}

function nightCount(start, end) {
  return Math.round((dayTime(end) - dayTime(start)) / 86400000);
}

function formatPretty(date) {
  return date.toLocaleDateString("en-GB", { day: "numeric", month: "short", year: "numeric" });
}

function guestLabel() {
  const parts = [`${state.adults} adult${state.adults === 1 ? "" : "s"}`];
  if (state.children) parts.push(`${state.children} child${state.children === 1 ? "" : "ren"}`);
  parts.push(`${state.rooms} room${state.rooms === 1 ? "" : "s"}`);
  return parts.join(" · ");
}

function chooseCity(city) {
  whereInput.value = city;
  formError.textContent = "";
  $("hero").scrollIntoView({ behavior: "smooth", block: "start" });
  window.setTimeout(() => openPopover("cal"), 480);
}

function updateDateLabels() {
  checkInBtn.textContent = state.checkIn ? formatPretty(state.checkIn) : "Add date";
  checkOutBtn.textContent = state.checkOut ? formatPretty(state.checkOut) : "Add date";
  checkInBtn.classList.toggle("is-empty", !state.checkIn);
  checkOutBtn.classList.toggle("is-empty", !state.checkOut);
}

function renderGuests() {
  $("adultsCount").textContent = String(state.adults);
  $("childrenCount").textContent = String(state.children);
  $("roomsCount").textContent = String(state.rooms);
  guestBtn.textContent = guestLabel();
  guestPopover.querySelectorAll("[data-step]").forEach((button) => {
    const [min, max] = LIMITS[button.dataset.step];
    const value = state[button.dataset.step];
    const dir = Number(button.dataset.dir);
    button.disabled = dir < 0 ? value <= min : value >= max;
  });
}

function monthIndex(date) {
  return date.getFullYear() * 12 + date.getMonth();
}

function canShift(dir) {
  const min = monthIndex(startOfMonth(new Date()));
  const next = monthIndex(state.viewMonth) + dir;
  return next >= min && next <= min + 18;
}

function navButton(dir) {
  const label = dir < 0 ? "Previous month" : "Next month";
  const mark = dir < 0 ? "←" : "→";
  const disabled = canShift(dir) ? "" : "disabled";
  return `<button class="cal-nav" type="button" data-nav="${dir}" aria-label="${label}" ${disabled}>${mark}</button>`;
}

function daysMarkup(date) {
  const year = date.getFullYear();
  const month = date.getMonth();
  const pad = (new Date(year, month, 1).getDay() + 6) % 7;
  const count = new Date(year, month + 1, 0).getDate();
  const todayKey = toKey(new Date());
  let html = DOW.map((name) => `<span class="dow">${name}</span>`).join("");
  for (let i = 0; i < pad; i += 1) html += '<span class="day-pad"></span>';
  for (let day = 1; day <= count; day += 1) {
    const current = new Date(year, month, day);
    const key = toKey(current);
    const past = isPast(current);
    const today = key === todayKey ? " is-today" : "";
    const aria = current.toLocaleDateString("en-GB", { weekday: "long", day: "numeric", month: "long", year: "numeric" });
    html += `<button class="day${today}${past ? " is-past" : ""}" type="button" data-day="${key}" ${past ? "disabled" : ""} aria-label="${aria}">${day}</button>`;
  }
  return `<div class="cal-grid">${html}</div>`;
}

function monthMarkup(date, which) {
  const label = date.toLocaleDateString("en-GB", { month: "long", year: "numeric" });
  const prev = which === "prev" || which === "both" ? navButton(-1) : '<span class="cal-nav"></span>';
  const next = which === "next" || which === "both" ? navButton(1) : '<span class="cal-nav"></span>';
  return `<div class="cal-month"><div class="cal-head">${prev}<h3>${label}</h3>${next}</div>${daysMarkup(date)}</div>`;
}

function calHelp() {
  if (state.checkIn && !state.checkOut) return "Now choose a check-out date.";
  if (state.checkIn && state.checkOut) {
    const nights = nightCount(state.checkIn, state.checkOut);
    return `${nights} night${nights === 1 ? "" : "s"} selected. Pick a day to start again.`;
  }
  return "Select a check-in date.";
}

function renderCalendar() {
  const wide = window.matchMedia("(min-width: 801px)").matches;
  const first = monthMarkup(state.viewMonth, wide ? "prev" : "both");
  const second = wide ? monthMarkup(addMonths(state.viewMonth, 1), "next") : "";
  calPopover.innerHTML = `<div class="cal-wrap"><p class="cal-help">${calHelp()}</p><div class="cal">${first}${second}</div></div>`;
  paintRange();
}

function paintRange() {
  const start = state.checkIn ? dayTime(state.checkIn) : null;
  const end = state.checkOut ? dayTime(state.checkOut) : null;
  const preview = !state.checkOut && state.hover && start && dayTime(state.hover) > start ? dayTime(state.hover) : null;
  const rangeEnd = end || preview;
  calPopover.querySelectorAll("[data-day]").forEach((button) => {
    const time = dayTime(parseDay(button.dataset.day));
    button.classList.toggle("is-start", start !== null && time === start);
    button.classList.toggle("is-end", (end !== null && time === end) || (preview !== null && time === preview));
    button.classList.toggle("in-range", start !== null && rangeEnd !== null && time > start && time < rangeEnd);
  });
}

function selectDay(date) {
  if (isPast(date)) return;
  const time = dayTime(date);
  if (!state.checkIn || state.checkOut || time <= dayTime(state.checkIn)) {
    state.checkIn = date;
    state.checkOut = null;
  } else {
    state.checkOut = date;
  }
  state.hover = null;
  updateDateLabels();
  formError.textContent = "";
  if (state.checkOut) closePopovers();
  else renderCalendar();
}

// The city list is rendered by the server. Typing only hides the cities that do not match.
function filterWhere() {
  const query = whereInput.value.trim().toLowerCase();
  let shown = 0;
  wherePopover.querySelectorAll(".where-item").forEach((item) => {
    const match = !query
      || item.dataset.city.toLowerCase().includes(query)
      || item.dataset.country.toLowerCase().includes(query);
    item.hidden = !match;
    if (match) shown += 1;
  });
  wherePopover.querySelector(".where-empty").hidden = shown > 0;
}

function placePopover(popover, anchor) {
  if (window.matchMedia("(max-width: 800px)").matches) return;
  popover.hidden = false;
  const formBox = searchForm.getBoundingClientRect();
  const anchorBox = anchor.getBoundingClientRect();
  const width = popover.offsetWidth;
  const height = popover.offsetHeight;
  let left = anchorBox.left - formBox.left;
  left = Math.max(0, Math.min(left, searchForm.clientWidth - width));
  const spaceBelow = window.innerHeight - anchorBox.bottom;
  const top = spaceBelow < height + 16
    ? anchorBox.top - formBox.top - height - 10
    : anchorBox.bottom - formBox.top + 10;
  popover.style.left = `${left}px`;
  popover.style.top = `${top}px`;
}

function setExpanded(expanded) {
  [whereInput, checkInBtn, checkOutBtn, guestBtn].forEach((control) => {
    const open = expanded && (
      (control === whereInput && state.active === "where") ||
      ((control === checkInBtn || control === checkOutBtn) && state.active === "cal") ||
      (control === guestBtn && state.active === "guests")
    );
    control.setAttribute("aria-expanded", String(open));
  });
}

function closePopovers() {
  wherePopover.hidden = true;
  calPopover.hidden = true;
  guestPopover.hidden = true;
  state.active = null;
  state.hover = null;
  setExpanded(false);
}

function openPopover(name) {
  closePopovers();
  state.active = name;
  const popover = name === "where" ? wherePopover : name === "cal" ? calPopover : guestPopover;
  const anchor = name === "where" ? whereField : name === "cal" ? datesField : guestField;
  if (name === "where") filterWhere();
  if (name === "cal") renderCalendar();
  if (name === "guests") renderGuests();
  popover.hidden = false;
  placePopover(popover, anchor);
  setExpanded(true);
}

function togglePopover(name) {
  if (state.active === name) closePopovers();
  else openPopover(name);
}

// The form posts to Home/Search as a normal GET. Empty fields are left out of the address.
function submitSearch() {
  formError.textContent = "";
  if (!state.checkIn || !state.checkOut) {
    formError.textContent = "Add a check-in and a check-out. The total needs both dates.";
    datesField.classList.remove("is-shake");
    window.requestAnimationFrame(() => datesField.classList.add("is-shake"));
    openPopover("cal");
    return;
  }
  if (whereInput.value.trim().toLowerCase() === "anywhere") whereInput.value = "";
  $("checkInValue").value = toKey(state.checkIn);
  $("checkOutValue").value = toKey(state.checkOut);
  $("adultsValue").value = String(state.adults);
  $("childrenValue").value = String(state.children);
  $("roomsValue").value = String(state.rooms);
  searchForm.querySelectorAll("input[name]").forEach((input) => {
    input.disabled = !input.value.trim() || (input.name === "children" && input.value === "0");
  });
  searchForm.submit();
  searchForm.querySelectorAll("input[name]").forEach((input) => { input.disabled = false; });
}

function validDay(key) {
  if (!key || !/^\d{4}-\d{2}-\d{2}$/.test(key)) return null;
  const date = parseDay(key);
  return Number.isNaN(date.getTime()) ? null : date;
}

function clampInt(value, min, max, fallback) {
  const n = Number(value);
  if (!Number.isInteger(n)) return fallback;
  return Math.min(max, Math.max(min, n));
}

// A link back from the results page carries the last search. Put it back in the form.
function applyIncomingSearch() {
  const params = new URLSearchParams(window.location.search);
  const destination = (params.get("destination") || "").trim();
  if (destination && destination.toLowerCase() !== "anywhere") whereInput.value = destination;
  const checkIn = validDay(params.get("checkIn"));
  const checkOut = validDay(params.get("checkOut"));
  if (checkIn && checkOut && dayTime(checkOut) > dayTime(checkIn)) {
    state.checkIn = checkIn;
    state.checkOut = checkOut;
    state.viewMonth = startOfMonth(checkIn);
  }
  if (params.has("adults")) state.adults = clampInt(params.get("adults"), 1, 8, state.adults);
  if (params.has("children")) state.children = clampInt(params.get("children"), 0, 6, state.children);
  if (params.has("rooms")) state.rooms = clampInt(params.get("rooms"), 1, 4, state.rooms);
  const property = $("propertyValue").value;
  document.querySelectorAll(".property-card").forEach((item) => {
    const on = item.dataset.property === property;
    item.classList.toggle("is-on", on);
    item.setAttribute("aria-pressed", String(on));
  });
  updateDateLabels();
  renderGuests();
}

// Hearts are server forms or sign-in links, so a click on one is left to the browser.
function onCatalogClick(event) {
  const card = event.target.closest("article[data-stay]");
  if (!card || event.target.closest("button, a, select")) return;
  openDrawer(card);
}

function onCatalogKey(event) {
  if (event.key !== "Enter" && event.key !== " ") return;
  if (event.target.closest("button, a, input, select")) return;
  const card = event.target.closest("article[data-stay]");
  if (!card) return;
  event.preventDefault();
  openDrawer(card);
}

// Each card carries its drawer as a server-rendered template.
function openDrawer(card) {
  const template = card.querySelector("template[data-drawer]");
  if (!template) return;
  lastFocus = document.activeElement;
  drawer.dataset.stay = card.dataset.stay;
  closePopovers();
  $("drawerBody").replaceChildren(template.content.cloneNode(true));
  drawer.hidden = false;
  document.body.classList.add("no-scroll");
  drawer.querySelector(".icon-x").focus();
}

function closeDrawer() {
  drawer.hidden = true;
  drawer.dataset.stay = "";
  document.body.classList.remove("no-scroll");
  if (lastFocus && typeof lastFocus.focus === "function") lastFocus.focus();
}

function closeMenu() {
  nav.classList.remove("is-open");
  header.classList.remove("menu-open");
  $("navToggle").setAttribute("aria-expanded", "false");
}

searchForm.addEventListener("submit", (event) => {
  event.preventDefault();
  submitSearch();
});

whereInput.addEventListener("focus", () => openPopover("where"));
whereInput.addEventListener("input", () => {
  filterWhere();
  if (state.active !== "where") openPopover("where");
  else placePopover(wherePopover, whereField);
});

checkInBtn.addEventListener("click", () => togglePopover("cal"));
checkOutBtn.addEventListener("click", () => togglePopover("cal"));
guestBtn.addEventListener("click", () => togglePopover("guests"));

wherePopover.addEventListener("click", (event) => {
  event.stopPropagation();
  const button = event.target.closest("[data-city]");
  if (!button) return;
  whereInput.value = button.dataset.city;
  closePopovers();
  openPopover("cal");
});

calPopover.addEventListener("click", (event) => {
  event.stopPropagation();
  const navButtonEl = event.target.closest("[data-nav]");
  if (navButtonEl && !navButtonEl.disabled) {
    state.viewMonth = addMonths(state.viewMonth, Number(navButtonEl.dataset.nav));
    renderCalendar();
    return;
  }
  const day = event.target.closest("[data-day]");
  if (!day || day.disabled) return;
  selectDay(parseDay(day.dataset.day));
});

calPopover.addEventListener("pointerover", (event) => {
  const day = event.target.closest("[data-day]");
  if (!day || day.disabled) return;
  const next = parseDay(day.dataset.day);
  if (state.hover && dayTime(state.hover) === dayTime(next)) return;
  state.hover = next;
  paintRange();
});

calPopover.addEventListener("pointerleave", () => {
  state.hover = null;
  paintRange();
});

guestPopover.addEventListener("click", (event) => {
  event.stopPropagation();
  const step = event.target.closest("[data-step]");
  if (step && !step.disabled) {
    const key = step.dataset.step;
    const [min, max] = LIMITS[key];
    state[key] = Math.min(max, Math.max(min, state[key] + Number(step.dataset.dir)));
    renderGuests();
    return;
  }
  if (event.target.closest("[data-close-guests]")) closePopovers();
});

$("chips").addEventListener("click", (event) => {
  const chip = event.target.closest("[data-city]");
  if (!chip) return;
  event.stopPropagation();
  whereInput.value = chip.dataset.city;
  openPopover("cal");
});

stayList.addEventListener("click", onCatalogClick);
stayList.addEventListener("keydown", onCatalogKey);

trendTrack.addEventListener("click", (event) => {
  const card = event.target.closest("[data-city]");
  if (!card) return;
  chooseCity(card.dataset.city);
});

trendTrack.addEventListener("keydown", (event) => {
  if (event.key !== "Enter" && event.key !== " ") return;
  const card = event.target.closest("[data-city]");
  if (!card || event.target !== card) return;
  event.preventDefault();
  chooseCity(card.dataset.city);
});

// Sorting is done by the server. Changing the select sends the form.
$("sort").addEventListener("change", () => $("sortForm").submit());

const trending = $("trending");
let trendPaused = false;
let trendInView = false;
let trendIndex = 0;
let trendAnimating = false;

function trendStep() {
  const card = trendTrack.querySelector(".trend-card");
  if (!card) return 0;
  const styles = getComputedStyle(trendTrack);
  const gap = parseFloat(styles.columnGap || styles.gap) || 16;
  return Math.round(card.getBoundingClientRect().width + gap);
}

function realTrendCount() {
  return trendTrack.querySelectorAll(".trend-card:not([data-clone])").length;
}

function prepareTrendLoop() {
  trendTrack.querySelectorAll("[data-clone]").forEach((node) => node.remove());
  const cards = [...trendTrack.querySelectorAll(".trend-card")];
  const step = trendStep() || 296;
  const copies = Math.max(1, Math.ceil(trendTrack.parentElement.clientWidth / step));
  cards.slice(0, copies).forEach((card) => {
    const clone = card.cloneNode(true);
    clone.dataset.clone = "true";
    clone.removeAttribute("tabindex");
    clone.querySelectorAll("button").forEach((button) => { button.tabIndex = -1; });
    trendTrack.appendChild(clone);
  });
  trendIndex = 0;
  trendAnimating = false;
  setTrendShift(true);
}

function setTrendShift(instant) {
  const step = trendStep();
  trendTrack.classList.toggle("is-instant", instant);
  trendTrack.style.transform = `translate3d(${-trendIndex * step}px, 0, 0)`;
  if (!instant) return;
  trendTrack.getBoundingClientRect();
  trendTrack.classList.remove("is-instant");
}

function canSlideTrend() {
  const step = trendStep();
  return step > 0 && realTrendCount() * step > trendTrack.parentElement.clientWidth + 8;
}

function slideTrend(direction) {
  if (trendAnimating || !canSlideTrend()) return;
  const real = realTrendCount();
  trendAnimating = true;
  if (direction < 0 && trendIndex === 0) {
    trendIndex = real;
    setTrendShift(true);
    requestAnimationFrame(() => {
      trendIndex = real - 1;
      setTrendShift(false);
    });
    return;
  }
  trendIndex += direction > 0 ? 1 : -1;
  setTrendShift(false);
}

trendTrack.addEventListener("transitionend", (event) => {
  if (event.target !== trendTrack || event.propertyName !== "transform") return;
  trendAnimating = false;
  if (trendIndex >= realTrendCount()) {
    trendIndex = 0;
    setTrendShift(true);
  }
});

function tickTrend() {
  if (!trendInView || trendPaused || document.hidden) return;
  slideTrend(1);
}

trending.addEventListener("pointerenter", () => { trendPaused = true; });
trending.addEventListener("pointerleave", () => { trendPaused = false; });
trending.addEventListener("focusin", () => { trendPaused = true; });
trending.addEventListener("focusout", (event) => {
  if (!trending.contains(event.relatedTarget)) trendPaused = false;
});

$("trendNext").addEventListener("click", () => slideTrend(1));
$("trendPrev").addEventListener("click", () => slideTrend(-1));

new IntersectionObserver((entries) => {
  trendInView = entries.some((entry) => entry.isIntersecting);
}, { threshold: 0.35 }).observe(trending);

window.setInterval(tickTrend, 3600);

$("propertiesGrid").addEventListener("click", (event) => {
  const card = event.target.closest(".property-card");
  if (!card) return;
  window.location.href = `${searchForm.dataset.types}?type=${encodeURIComponent(card.dataset.property)}`;
});

drawer.addEventListener("click", (event) => {
  if (event.target.closest("[data-close='drawer']")) closeDrawer();
});

header.addEventListener("click", (event) => {
  if (event.target.closest("a")) closeMenu();
});

const reduceMotion = window.matchMedia("(prefers-reduced-motion: reduce)");
let glideFrame = 0;

function easeInOutCubic(t) {
  return t < 0.5 ? 4 * t * t * t : 1 - Math.pow(-2 * t + 2, 3) / 2;
}

function markArrival(target) {
  if (reduceMotion.matches) return;
  target.classList.remove("is-arrived");
  void target.offsetWidth;
  target.classList.add("is-arrived");
  window.setTimeout(() => target.classList.remove("is-arrived"), 1400);
}

function glideTo(target) {
  const margin = parseFloat(getComputedStyle(target).scrollMarginTop) || 0;
  const start = window.scrollY;
  const end = Math.max(0, target.getBoundingClientRect().top + start - margin);
  const distance = end - start;
  cancelAnimationFrame(glideFrame);
  if (reduceMotion.matches || Math.abs(distance) < 2) {
    window.scrollTo({ top: end, behavior: "instant" });
    markArrival(target);
    return;
  }
  const duration = Math.min(1200, Math.max(600, Math.abs(distance) * 0.45));
  const began = performance.now();
  const step = (now) => {
    const t = Math.min(1, (now - began) / duration);
    window.scrollTo({ top: start + distance * easeInOutCubic(t), behavior: "instant" });
    if (t < 1) glideFrame = requestAnimationFrame(step);
    else markArrival(target);
  };
  glideFrame = requestAnimationFrame(step);
}

document.addEventListener("click", (event) => {
  const link = event.target.closest('a[href^="#"]');
  if (!link || event.defaultPrevented || event.metaKey || event.ctrlKey || event.shiftKey) return;
  const id = link.getAttribute("href").slice(1);
  const target = id && document.getElementById(id);
  if (!target) return;
  event.preventDefault();
  history.pushState(null, "", `#${id}`);
  glideTo(target);
});

["wheel", "touchstart", "keydown"].forEach((type) => {
  window.addEventListener(type, () => cancelAnimationFrame(glideFrame), { passive: true });
});

$("navToggle").addEventListener("click", () => {
  const open = nav.classList.toggle("is-open");
  header.classList.toggle("menu-open", open);
  $("navToggle").setAttribute("aria-expanded", String(open));
  $("navToggle").setAttribute("aria-label", open ? "Close menu" : "Open menu");
});

document.addEventListener("click", (event) => {
  if (event.target.closest("#searchForm")) return;
  closePopovers();
});

document.addEventListener("keydown", (event) => {
  if (event.key === "Escape") {
    if (!drawer.hidden) closeDrawer();
    else if (nav.classList.contains("is-open")) closeMenu();
    else closePopovers();
    return;
  }
  if (event.key === "/" && !event.target.closest("input, textarea")) {
    event.preventDefault();
    whereInput.focus();
  }
});

const hero = $("hero");
const heroSlides = [...document.querySelectorAll("#heroSlides .hero-slide")];
let heroIndex = 0;

function showHero(index) {
  heroIndex = (index + heroSlides.length) % heroSlides.length;
  heroSlides.forEach((slide, i) => slide.classList.toggle("is-on", i === heroIndex));
}

if (heroSlides.length > 1) window.setInterval(() => showHero(heroIndex + 1), 5000);

const driftTargets = () => document.querySelectorAll(".trend-card img, .stay-media img, .video-band video, .property-card img");

const videoTrack = $("videoTrack");
const videoDots = [...$("videoDots").children];
const bandVideos = [...videoTrack.querySelectorAll("video")];
let videoIndex = 0;
let bandVisible = false;

function syncBandVideo() {
  bandVideos.forEach((video, index) => {
    if (bandVisible && index === videoIndex) video.play().catch(() => {});
    else video.pause();
  });
}

function showVideo(index) {
  videoIndex = index;
  videoTrack.style.transform = `translateX(-${index * 100}%)`;
  videoDots.forEach((dot, i) => {
    dot.classList.toggle("is-on", i === index);
    if (i === index) dot.setAttribute("aria-current", "true");
    else dot.removeAttribute("aria-current");
  });
  syncBandVideo();
}

videoDots.forEach((dot, index) => dot.addEventListener("click", () => showVideo(index)));

new IntersectionObserver((entries) => {
  bandVisible = entries.some((entry) => entry.isIntersecting);
  syncBandVideo();
}, { threshold: 0.1 }).observe(videoTrack.parentElement);

let motionFrame = 0;

function markVisible() {
  const height = window.innerHeight;
  document.querySelectorAll(".reveal, .reveal-track").forEach((el) => {
    const box = el.getBoundingClientRect();
    if (box.top < height * 0.92 && box.bottom > 72) el.classList.add("is-in");
    else if (box.bottom < 0 || box.top > height) el.classList.remove("is-in");
  });
}

function paintDrift() {
  const height = window.innerHeight;
  if (hero) {
    const scrolled = Math.min(Math.max(-hero.getBoundingClientRect().top, 0), height);
    hero.style.setProperty("--drift", `${(scrolled * 0.35).toFixed(1)}px`);
  }
  driftTargets().forEach((el) => {
    const box = el.getBoundingClientRect();
    if (box.bottom < -80 || box.top > height + 80) return;
    const progress = (box.top + box.height / 2 - height / 2) / height;
    const shift = Math.max(-1, Math.min(1, progress)) * 36;
    el.style.setProperty("--py", `${shift.toFixed(1)}px`);
  });
}

function onScroll() {
  header.classList.toggle("is-solid", window.scrollY > 20);
  if (motionFrame) return;
  motionFrame = requestAnimationFrame(() => {
    motionFrame = 0;
    markVisible();
    paintDrift();
  });
}

window.addEventListener("scroll", onScroll, { passive: true });

window.addEventListener("resize", () => {
  prepareTrendLoop();
  if (!state.active) return;
  if (state.active === "cal") renderCalendar();
  const popover = state.active === "where" ? wherePopover : state.active === "cal" ? calPopover : guestPopover;
  const anchor = state.active === "where" ? whereField : state.active === "cal" ? datesField : guestField;
  placePopover(popover, anchor);
});

document.addEventListener("error", (event) => {
  if (event.target && event.target.tagName === "IMG") event.target.classList.add("is-broken");
}, true);

requestAnimationFrame(() => {
  markVisible();
  paintDrift();
});
onScroll();
renderGuests();
updateDateLabels();
applyIncomingSearch();
prepareTrendLoop();
