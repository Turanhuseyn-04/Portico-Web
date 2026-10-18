"use strict";

// The server renders the stay, its rooms, prices, and availability for the dates in the address.
// This script runs the date picker, the guest stepper, the photo viewer, and the small effects on top.

const $ = (id) => document.getElementById(id);
const DOW = ["Mo", "Tu", "We", "Th", "Fr", "Sa", "Su"];
const LIMITS = { adults: [1, 8], children: [0, 6], rooms: [1, 4] };

const header = $("header");
const nav = $("nav");
const propertyEl = $("property");
const bookCard = $("bookCard");
const datesField = $("datesField");
const checkInBtn = $("checkInBtn");
const checkOutBtn = $("checkOutBtn");
const guestBtn = $("guestBtn");
const calPopover = $("calPopover");
const guestPopover = $("guestPopover");
const subnav = $("subnav");
const mobileBar = $("mobileBar");
const lightbox = $("lightbox");

const card = bookCard.dataset;
const stayId = propertyEl.dataset.stay;
const stayName = propertyEl.dataset.name;
const minNights = Number(card.minNights) || 1;
const maxNights = Number(card.maxNights) || 30;

const state = {
  checkIn: readDay(card.checkIn),
  checkOut: readDay(card.checkOut),
  hover: null,
  viewMonth: startOfMonth(readDay(card.checkIn) || new Date()),
  adults: Number($("adultsField").value) || 2,
  children: Number($("childrenField").value) || 0,
  rooms: Number($("roomsField").value) || 1,
  guestsChanged: false,
  dateError: card.dateError || "",
  active: null,
  photos: [],
  photoIndex: 0
};

let savedIds = new Set();
let lastFocus = null;
let toastTimer = 0;
let cardInView = true;
let scrollFrame = 0;

function count(value, one, many) {
  return `${value} ${value === 1 ? one : many}`;
}

function startOfMonth(date) {
  return new Date(date.getFullYear(), date.getMonth(), 1);
}

function addMonths(date, months) {
  return new Date(date.getFullYear(), date.getMonth() + months, 1);
}

function addDays(date, days) {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate() + days);
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

function readDay(key) {
  if (!key || !/^\d{4}-\d{2}-\d{2}$/.test(key)) return null;
  const date = parseDay(key);
  return toKey(date) === key ? date : null;
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

function formatShort(date) {
  return date.toLocaleDateString("en-GB", { weekday: "short", day: "numeric", month: "short" });
}

function escapeHtml(value) {
  return String(value).replace(/[&<>"']/g, (ch) => ({
    "&": "&amp;",
    "<": "&lt;",
    ">": "&gt;",
    '"': "&quot;",
    "'": "&#39;"
  }[ch]));
}

function toast(message) {
  const el = $("toast");
  el.textContent = message;
  el.classList.add("is-on");
  window.clearTimeout(toastTimer);
  toastTimer = window.setTimeout(() => el.classList.remove("is-on"), 3600);
}

function shake(el) {
  el.classList.remove("is-shake");
  window.requestAnimationFrame(() => el.classList.add("is-shake"));
}

function inView(el) {
  const box = el.getBoundingClientRect();
  return box.bottom > header.offsetHeight && box.top < window.innerHeight;
}

/* ---------- Sending the stay form ---------- */

// The booking card is a GET form back to this page. Fields left empty are dropped from the address.
function submitStay() {
  $("checkInField").value = state.checkIn && state.checkOut ? toKey(state.checkIn) : "";
  $("checkOutField").value = state.checkIn && state.checkOut ? toKey(state.checkOut) : "";
  $("adultsField").value = String(state.adults);
  $("childrenField").value = String(state.children);
  $("roomsField").value = String(state.rooms);
  bookCard.querySelectorAll("input[type=hidden]").forEach((input) => {
    input.disabled = !input.value || (input.name === "children" && input.value === "0");
  });
  bookCard.submit();
}

/* ---------- Dates ---------- */

function updateDateLabels() {
  const set = (el, date) => {
    el.textContent = date ? formatShort(date) : "Add date";
    el.classList.toggle("is-empty", !date);
  };
  set($("checkInValue"), state.checkIn);
  set($("checkOutValue"), state.checkOut);
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
  return `<button class="cal-nav" type="button" data-nav="${dir}" aria-label="${label}" ${canShift(dir) ? "" : "disabled"}>${dir < 0 ? "←" : "→"}</button>`;
}

function daysMarkup(date) {
  const year = date.getFullYear();
  const month = date.getMonth();
  const pad = (new Date(year, month, 1).getDay() + 6) % 7;
  const days = new Date(year, month + 1, 0).getDate();
  const todayKey = toKey(new Date());
  let html = DOW.map((name) => `<span class="dow">${name}</span>`).join("");
  for (let i = 0; i < pad; i += 1) html += '<span class="day-pad"></span>';
  for (let day = 1; day <= days; day += 1) {
    const current = new Date(year, month, day);
    const key = toKey(current);
    const past = isPast(current);
    const aria = current.toLocaleDateString("en-GB", { weekday: "long", day: "numeric", month: "long", year: "numeric" });
    html += `<button class="day${key === todayKey ? " is-today" : ""}${past ? " is-past" : ""}" type="button" data-day="${key}" ${past ? "disabled" : ""} aria-label="${aria}">${day}</button>`;
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
  if (state.dateError) return state.dateError;
  if (state.checkIn && !state.checkOut) return "Now choose a check-out date.";
  if (state.checkIn && state.checkOut) return `${count(nightCount(state.checkIn, state.checkOut), "night", "nights")} selected. Pick a day to start again.`;
  return "Select a check-in date.";
}

function renderCalendar() {
  const wide = window.matchMedia("(min-width: 801px)").matches;
  const first = monthMarkup(state.viewMonth, wide ? "prev" : "both");
  const second = wide ? monthMarkup(addMonths(state.viewMonth, 1), "next") : "";
  const rule = minNights > 1 ? `Minimum stay ${minNights} nights · up to ${maxNights}` : `Stays of 1 to ${maxNights} nights`;
  calPopover.innerHTML = `<div class="cal-wrap">
    <p class="cal-help${state.dateError ? " is-error" : ""}" role="status">${escapeHtml(calHelp())}</p>
    <div class="cal">${first}${second}</div>
    <div class="cal-foot"><span>${rule}</span><button class="text-link" type="button" data-clear-dates ${state.checkIn ? "" : "disabled"}>Clear dates</button></div>
  </div>`;
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

// A complete range goes to the server, which checks availability and prices the rooms.
function selectDay(date) {
  if (isPast(date)) return;
  const time = dayTime(date);
  if (!state.checkIn || state.checkOut || time <= dayTime(state.checkIn)) {
    state.checkIn = date;
    state.checkOut = null;
    state.dateError = "";
    updateDateLabels();
    renderCalendar();
    return;
  }
  const nights = nightCount(state.checkIn, date);
  if (nights > maxNights) {
    state.dateError = `Stays can be up to ${maxNights} nights. Choose a check-out on or before ${formatPretty(addDays(state.checkIn, maxNights))}.`;
  } else if (nights < minNights) {
    state.dateError = `${stayName} has a minimum stay of ${minNights} nights. Choose a check-out on or after ${formatPretty(addDays(state.checkIn, minNights))}.`;
  } else {
    state.checkOut = date;
    state.dateError = "";
    updateDateLabels();
    closePopovers();
    submitStay();
    return;
  }
  renderCalendar();
  shake(calPopover.querySelector(".cal-help"));
}

function clearDates() {
  state.checkIn = null;
  state.checkOut = null;
  state.dateError = "";
  submitStay();
}

/* ---------- Guests ---------- */

function renderGuests() {
  const unit = card.unit;
  const units = card.units;
  $("adultsCount").textContent = String(state.adults);
  $("childrenCount").textContent = String(state.children);
  $("roomsCount").textContent = String(state.rooms);
  const parts = [count(state.adults, "adult", "adults")];
  if (state.children) parts.push(count(state.children, "child", "children"));
  parts.push(count(state.rooms, unit, units));
  $("guestValue").textContent = parts.join(" · ");
  guestPopover.querySelectorAll("[data-step]").forEach((button) => {
    const [min, max] = LIMITS[button.dataset.step];
    const value = state[button.dataset.step];
    button.disabled = Number(button.dataset.dir) < 0 ? value <= min : value >= max;
  });
}

/* ---------- Popovers ---------- */

function placePopover(popover, anchor) {
  if (window.matchMedia("(max-width: 800px)").matches) return;
  const box = bookCard.getBoundingClientRect();
  const anchorBox = anchor.getBoundingClientRect();
  const width = popover.offsetWidth;
  const maxLeft = window.innerWidth - 16 - box.left - width;
  const minLeft = 16 - box.left;
  const left = Math.max(minLeft, Math.min(anchorBox.left - box.left, maxLeft));
  popover.style.left = `${left}px`;
  popover.style.top = `${anchorBox.bottom - box.top + 8}px`;
}

function setExpanded() {
  checkInBtn.setAttribute("aria-expanded", String(state.active === "cal"));
  checkOutBtn.setAttribute("aria-expanded", String(state.active === "cal"));
  guestBtn.setAttribute("aria-expanded", String(state.active === "guests"));
}

// Closing the guest stepper after a change sends it, the same as pressing Done.
function closePopovers() {
  const sendGuests = state.active === "guests" && state.guestsChanged;
  calPopover.hidden = true;
  guestPopover.hidden = true;
  state.active = null;
  state.hover = null;
  setExpanded();
  if (sendGuests) submitStay();
}

function openPopover(name) {
  closePopovers();
  state.active = name;
  const popover = name === "cal" ? calPopover : guestPopover;
  if (name === "cal") {
    if (state.checkIn) state.viewMonth = startOfMonth(state.checkIn);
    renderCalendar();
  } else {
    renderGuests();
  }
  popover.hidden = false;
  placePopover(popover, name === "cal" ? datesField : guestBtn);
  setExpanded();
}

function openDates() {
  if (!inView(bookCard)) bookCard.scrollIntoView({ behavior: "smooth", block: "center" });
  window.setTimeout(() => openPopover("cal"), inView(bookCard) ? 0 : 360);
}

/* ---------- Reserve ---------- */

// When the stay cannot be reserved yet, the server puts the reason on the button.
function explainReserve(button) {
  const message = button.dataset.problem;
  if (!message) return;
  $("bookError").textContent = message;
  shake($("bookError"));
  if (!inView(bookCard)) toast(message);
  if (button.dataset.focus === "dates") {
    shake(datesField);
    openDates();
  } else if (button.dataset.focus === "rooms") {
    $("rooms").scrollIntoView({ behavior: "smooth", block: "start" });
  }
}

function paintMobileBar() {
  mobileBar.classList.toggle("is-on", !cardInView);
}

/* ---------- Saved stays ---------- */

function paintSaved() {
  const saved = savedIds.has(stayId);
  const button = $("saveBtn");
  button.setAttribute("aria-pressed", String(saved));
  button.setAttribute("aria-label", saved ? "Remove from saved stays" : "Save this stay");
}

async function loadSaved() {
  try {
    savedIds = new Set(await PorticoApi.listSaved());
  } catch {
    savedIds = new Set();
  }
  paintSaved();
}

/* ---------- Lightbox ---------- */

function galleryPhotos() {
  return [...$("lbThumbs").querySelectorAll("[data-thumb]")].map((thumb) => ({ src: thumb.dataset.src, alt: thumb.dataset.alt }));
}

function openLightbox(photos, index, withThumbs) {
  state.photos = photos;
  $("lbThumbs").hidden = !withThumbs || photos.length < 2;
  openDialog(lightbox);
  showPhoto(index);
  lightbox.querySelector(".lb-close").focus();
}

function showPhoto(index) {
  const total = state.photos.length;
  if (!total) return;
  state.photoIndex = (index + total) % total;
  const photo = state.photos[state.photoIndex];
  const image = $("lbImage");
  image.classList.remove("is-in");
  image.src = photo.src;
  image.alt = photo.alt;
  window.requestAnimationFrame(() => image.classList.add("is-in"));
  $("lbCaption").textContent = photo.alt;
  $("lbCount").textContent = `${state.photoIndex + 1} / ${total}`;
  $("lbPrev").hidden = total < 2;
  $("lbNext").hidden = total < 2;
  $("lbThumbs").querySelectorAll("[data-thumb]").forEach((thumb) => {
    const on = Number(thumb.dataset.thumb) === state.photoIndex;
    thumb.classList.toggle("is-on", on);
    if (on) {
      thumb.setAttribute("aria-current", "true");
      if (!$("lbThumbs").hidden) thumb.scrollIntoView({ block: "nearest", inline: "center" });
    } else {
      thumb.removeAttribute("aria-current");
    }
  });
  [1, -1].forEach((step) => {
    const next = state.photos[(state.photoIndex + step + total) % total];
    if (next) new Image().src = next.src;
  });
}

function openDialog(el) {
  lastFocus = document.activeElement;
  el.hidden = false;
  document.body.classList.add("no-scroll");
}

function closeDialog(el) {
  if (el.hidden) return;
  el.hidden = true;
  document.body.classList.remove("no-scroll");
  if (lastFocus && typeof lastFocus.focus === "function") lastFocus.focus();
}

function trapFocus(event, container) {
  const nodes = [...container.querySelectorAll('a[href], button:not([disabled]), input, textarea, select, [tabindex]:not([tabindex="-1"])')]
    .filter((el) => el.offsetParent !== null && !el.hidden);
  if (!nodes.length) return;
  const first = nodes[0];
  const last = nodes[nodes.length - 1];
  if (event.shiftKey && document.activeElement === first) {
    event.preventDefault();
    last.focus();
  } else if (!event.shiftKey && document.activeElement === last) {
    event.preventDefault();
    first.focus();
  }
}

function closeMenu() {
  nav.classList.remove("is-open");
  header.classList.remove("menu-open");
  $("navToggle").setAttribute("aria-expanded", "false");
  $("navToggle").setAttribute("aria-label", "Open menu");
}

/* ---------- Scroll effects ---------- */

function paintSubnav() {
  const offset = header.offsetHeight + subnav.offsetHeight + 32;
  let current = null;
  subnav.querySelectorAll("a").forEach((link) => {
    const section = document.getElementById(link.hash.slice(1));
    if (section && section.getBoundingClientRect().top - offset <= 0) current = link;
  });
  subnav.querySelectorAll("a").forEach((link) => {
    if (link === current) link.setAttribute("aria-current", "true");
    else link.removeAttribute("aria-current");
  });
  subnav.classList.toggle("is-stuck", subnav.getBoundingClientRect().top <= header.offsetHeight + 1);
}

function revealSections() {
  if (!("IntersectionObserver" in window)) return;
  const observer = new IntersectionObserver((entries) => {
    entries.forEach((entry) => {
      if (!entry.isIntersecting) return;
      entry.target.classList.add("is-in");
      observer.unobserve(entry.target);
    });
  }, { rootMargin: "0px 0px -8% 0px" });
  document.querySelectorAll(".p-section").forEach((section) => {
    if (inView(section)) return;
    section.classList.add("reveal");
    observer.observe(section);
  });
}

new IntersectionObserver((entries) => {
  cardInView = entries.some((entry) => entry.isIntersecting);
  paintMobileBar();
}).observe(bookCard);

window.addEventListener("scroll", () => {
  if (scrollFrame) return;
  scrollFrame = window.requestAnimationFrame(() => {
    scrollFrame = 0;
    paintSubnav();
  });
}, { passive: true });

window.addEventListener("resize", () => {
  if (state.active === "cal") renderCalendar();
  if (state.active) placePopover(state.active === "cal" ? calPopover : guestPopover, state.active === "cal" ? datesField : guestBtn);
});

/* ---------- Events ---------- */

checkInBtn.addEventListener("click", () => (state.active === "cal" ? closePopovers() : openPopover("cal")));
checkOutBtn.addEventListener("click", () => (state.active === "cal" ? closePopovers() : openPopover("cal")));
guestBtn.addEventListener("click", () => (state.active === "guests" ? closePopovers() : openPopover("guests")));

calPopover.addEventListener("click", (event) => {
  const navEl = event.target.closest("[data-nav]");
  if (navEl && !navEl.disabled) {
    state.viewMonth = addMonths(state.viewMonth, Number(navEl.dataset.nav));
    renderCalendar();
    return;
  }
  if (event.target.closest("[data-clear-dates]")) {
    clearDates();
    return;
  }
  const day = event.target.closest("[data-day]");
  if (day && !day.disabled) selectDay(parseDay(day.dataset.day));
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
  const step = event.target.closest("[data-step]");
  if (!step || step.disabled) return;
  const key = step.dataset.step;
  const [min, max] = LIMITS[key];
  state[key] = Math.min(max, Math.max(min, state[key] + Number(step.dataset.dir)));
  state.guestsChanged = true;
  renderGuests();
});

bookCard.addEventListener("submit", (event) => {
  event.preventDefault();
  state.active = null;
  submitStay();
});

$("reserveBtn").addEventListener("click", (event) => {
  if (event.currentTarget.tagName === "BUTTON") explainReserve(event.currentTarget);
});

$("mobileReserve").addEventListener("click", () => {
  const reserve = $("reserveBtn");
  if (reserve.tagName === "A") window.location.href = reserve.href;
  else explainReserve(reserve);
});

$("saveBtn").addEventListener("click", async () => {
  try {
    savedIds = new Set(savedIds.has(stayId) ? await PorticoApi.unsaveStay(stayId) : await PorticoApi.saveStay(stayId));
  } catch {
    toast("Sign in to save stays.");
    return;
  }
  paintSaved();
  const button = $("saveBtn");
  button.classList.remove("is-pop");
  window.requestAnimationFrame(() => button.classList.add("is-pop"));
  toast(savedIds.has(stayId) ? `${stayName} saved.` : `${stayName} removed from saved stays.`);
});

$("shareBtn").addEventListener("click", async () => {
  const url = window.location.href;
  if (navigator.share) {
    try {
      await navigator.share({ title: document.title, url });
    } catch {
      /* the share sheet was dismissed */
    }
    return;
  }
  try {
    await navigator.clipboard.writeText(url);
    toast("Link copied. Your dates and guests are in it.");
  } catch {
    toast("Copy the address bar to share this stay.");
  }
});

$("gallery").addEventListener("click", (event) => {
  const tile = event.target.closest("[data-photo]");
  if (tile) openLightbox(galleryPhotos(), Number(tile.dataset.photo), true);
});

$("roomList").addEventListener("click", (event) => {
  const photo = event.target.closest("[data-room-photo]");
  if (photo) openLightbox([{ src: photo.dataset.src, alt: photo.dataset.alt }], 0, false);
});

const amenityToggle = $("amenityToggle");
if (amenityToggle) {
  amenityToggle.addEventListener("click", () => {
    const open = amenityToggle.getAttribute("aria-expanded") !== "true";
    $("amenityGroups").classList.toggle("is-collapsed", !open);
    amenityToggle.setAttribute("aria-expanded", String(open));
    amenityToggle.textContent = open ? amenityToggle.dataset.openText : amenityToggle.dataset.closedText;
  });
}

// Review filters are a GET form. A change sends it.
$("reviewForm").addEventListener("change", (event) => {
  if (event.target.matches("[data-autosubmit]")) $("reviewForm").submit();
});

lightbox.addEventListener("click", (event) => {
  if (event.target.closest("[data-close='lightbox']") || event.target === lightbox) {
    closeDialog(lightbox);
    return;
  }
  if (event.target.closest("#lbPrev")) showPhoto(state.photoIndex - 1);
  else if (event.target.closest("#lbNext")) showPhoto(state.photoIndex + 1);
  const thumb = event.target.closest("[data-thumb]");
  if (thumb) showPhoto(Number(thumb.dataset.thumb));
});

let swipeX = null;
$("lbStage").addEventListener("pointerdown", (event) => { swipeX = event.clientX; });
$("lbStage").addEventListener("pointerup", (event) => {
  if (swipeX === null || state.photos.length < 2) return;
  const delta = event.clientX - swipeX;
  swipeX = null;
  if (Math.abs(delta) > 48) showPhoto(state.photoIndex + (delta < 0 ? 1 : -1));
});

$("navToggle").addEventListener("click", () => {
  const open = nav.classList.toggle("is-open");
  header.classList.toggle("menu-open", open);
  $("navToggle").setAttribute("aria-expanded", String(open));
  $("navToggle").setAttribute("aria-label", open ? "Close menu" : "Open menu");
});

document.addEventListener("click", (event) => {
  if (event.target.closest("[data-open-dates]")) {
    openDates();
    return;
  }
  if (event.target.closest(".nav a")) closeMenu();
  if (!event.target.closest("#bookCard")) closePopovers();
});

document.addEventListener("keydown", (event) => {
  const dialogOpen = !lightbox.hidden;
  if (dialogOpen && (event.key === "ArrowLeft" || event.key === "ArrowRight")) {
    showPhoto(state.photoIndex + (event.key === "ArrowRight" ? 1 : -1));
    return;
  }
  if (event.key === "Tab" && dialogOpen) {
    trapFocus(event, lightbox);
    return;
  }
  if (event.key !== "Escape") return;
  if (dialogOpen) closeDialog(lightbox);
  else if (state.active) {
    const anchor = state.active === "cal" ? checkInBtn : guestBtn;
    closePopovers();
    anchor.focus();
  } else if (nav.classList.contains("is-open")) closeMenu();
});

document.addEventListener("error", (event) => {
  if (event.target && event.target.tagName === "IMG") event.target.classList.add("is-broken");
}, true);

if (state.dateError) {
  // The server dropped dates it could not book. Clear them from the address so a refresh starts clean.
  const params = new URLSearchParams(window.location.search);
  params.delete("checkIn");
  params.delete("checkOut");
  window.history.replaceState(null, "", `${window.location.pathname}?${params}${window.location.hash}`);
}

updateDateLabels();
revealSections();
paintSubnav();
paintMobileBar();
loadSaved();
