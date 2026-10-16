"use strict";

const STATUS_COLORS = [
  ["Upcoming", "#9c4224"],
  ["In house", "#142033"],
  ["Past", "#8a8178"],
  ["Cancelled", "#8a3d32"]
];

function deskRoot() {
  return document.getElementById("adminDesk")
    || document.getElementById("adminBooks")
    || document.getElementById("adminHouses")
    || document.getElementById("adminUsers")
    || document.body;
}

function paintCounts() {
  const trips = allTrips();
  const reservationCount = document.getElementById("sideReservationCount");
  const stayCount = document.getElementById("sideStayCount");
  const userCount = document.getElementById("sideUserCount");
  const messageCount = document.getElementById("sideMessageCount");
  if (reservationCount) reservationCount.textContent = String(trips.length);
  if (stayCount) stayCount.textContent = String(properties().length);
  if (userCount) userCount.textContent = String((readAccount() && readAccount().email ? 1 : 0) + guestRows(trips).length);
  if (messageCount) messageCount.textContent = String(readMessages().length);
}

function readMessages() {
  try {
    const list = JSON.parse(localStorage.getItem("portico-messages") || "[]");
    return Array.isArray(list) ? list : [];
  } catch {
    return [];
  }
}

function isoDay(date) {
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}-${String(date.getDate()).padStart(2, "0")}`;
}

function shiftMonth(value, delta) {
  const [year, month] = value.split("-").map(Number);
  const next = new Date(year, month - 1 + delta, 1);
  return `${next.getFullYear()}-${String(next.getMonth() + 1).padStart(2, "0")}`;
}

function monthLabel(value) {
  const [year, month] = value.split("-").map(Number);
  return new Intl.DateTimeFormat("en-GB", { month: "long", year: "numeric" }).format(new Date(year, month - 1, 1));
}

function tripNights(trip) {
  const keys = [];
  if (!trip.checkIn || !trip.checkOut) return keys;
  let day = parseKey(trip.checkIn);
  const end = parseKey(trip.checkOut);
  while (day < end) {
    keys.push(isoDay(day));
    day = new Date(day.getFullYear(), day.getMonth(), day.getDate() + 1);
  }
  return keys;
}

function heldOn(propertyId, roomId, key) {
  return allTrips()
    .filter((trip) => !trip.cancelled && trip.propertyId === propertyId && trip.roomId === roomId && tripNights(trip).includes(key))
    .reduce((sum, trip) => sum + (Number(trip.rooms) || 1), 0);
}

function stayIsOverbooked(property) {
  return (property.rooms || []).some((room) => allTrips()
    .filter((trip) => !trip.cancelled && trip.propertyId === property.id && trip.roomId === room.id)
    .some((trip) => tripNights(trip).some((key) => heldOn(property.id, room.id, key) > Number(room.inventory || 0))));
}

function guestRows(trips) {
  const groups = new Map();
  trips.forEach((trip) => {
    const email = String(trip.email || "").trim().toLowerCase();
    const key = email || `ref:${trip.reference}`;
    const current = groups.get(key) || {
      name: trip.name || "",
      email: trip.email || "",
      trips: [],
      due: 0
    };
    if (!current.name && trip.name) current.name = trip.name;
    current.trips.push(trip);
    current.due += due(trip);
    groups.set(key, current);
  });
  return [...groups.values()];
}

function statusClass(status) {
  return String(status || "").toLowerCase().replace(/\s+/g, "-");
}

function meters(rows, fillClass, scale) {
  if (!rows.length) return `<div class="empty"><h3>Nothing to chart yet</h3><p>A booking in this browser adds a bar.</p></div>`;
  const max = scale || Math.max(...rows.map((row) => row.value), 1);
  return rows.map((row) => `<div class="meter">
    <span class="meter-label">${escapeHtml(row.label)}</span>
    <span class="meter-track"><span class="meter-fill ${fillClass || ""}" style="width:${Math.max(6, Math.round((row.value / max) * 100))}%"></span></span>
    <span class="meter-value">${escapeHtml(row.display)}</span>
  </div>`).join("");
}

function donut(parts) {
  const total = parts.reduce((sum, part) => sum + part.value, 0);
  const radius = 46;
  const circumference = 2 * Math.PI * radius;
  let offset = 0;
  const active = parts.filter((part) => part.value > 0);
  const rings = active.length === 1
    ? `<circle cx="70" cy="70" r="${radius}" fill="none" stroke="${active[0].color}" stroke-width="14"></circle>`
    : active.map((part) => {
      const length = total ? (part.value / total) * circumference : 0;
      const circle = `<circle cx="70" cy="70" r="${radius}" fill="none" stroke="${part.color}" stroke-width="14" stroke-dasharray="${length} ${circumference - length}" stroke-dashoffset="${-offset}" transform="rotate(-90 70 70)"></circle>`;
      offset += length;
      return circle;
    }).join("");
  const legend = `<ul class="legend">${parts.map((part) => `<li><span class="swatch" style="background:${part.color}"></span>${escapeHtml(part.label)}<strong>${part.value}</strong></li>`).join("")}</ul>`;
  const label = parts.map((part) => `${part.label} ${part.value}`).join(", ");
  return `<svg viewBox="0 0 140 140" role="img" aria-label="Reservations by status: ${escapeHtml(label)}">${rings}<text x="70" y="76" text-anchor="middle">${total}</text></svg>${legend}`;
}

function sheet(headers, rows) {
  if (!rows.length) return "";
  return `<div class="sheet-scroll"><table class="sheet"><thead><tr>${headers.map((header) => `<th>${escapeHtml(header)}</th>`).join("")}</tr></thead><tbody>${rows.join("")}</tbody></table></div>`;
}

function renderDashboard() {
  const root = document.getElementById("adminDesk");
  const trips = allTrips();
  const stays = properties();
  const owed = trips.reduce((sum, trip) => sum + due(trip), 0);
  const rooms = stays.reduce((sum, property) => sum + (property.rooms || []).reduce((inner, room) => inner + (Number(room.inventory) || 0), 0), 0);
  const kpis = [
    ["Stays", stays.length],
    ["Rooms for sale", rooms],
    ["Reservations", trips.length],
    ["Guests named", guestRows(trips).filter((row) => row.email || row.name).length],
    ["Cancelled", trips.filter((trip) => trip.cancelled).length],
    ["Still due", money(owed)]
  ];
  document.getElementById("adminKpis").innerHTML = kpis.map(([label, value]) => `<div class="kpi"><span>${escapeHtml(label)}</span><strong>${escapeHtml(value)}</strong></div>`).join("");

  const parts = STATUS_COLORS.map(([label, color]) => ({
    label,
    color,
    value: trips.filter((trip) => statusOf(trip) === label).length
  }));
  document.getElementById("adminStatusChart").innerHTML = donut(parts);

  const byCity = new Map();
  trips.forEach((trip) => {
    const city = trip.city || "Unknown";
    byCity.set(city, (byCity.get(city) || 0) + due(trip));
  });
  document.getElementById("adminDueChart").innerHTML = meters(
    [...byCity.entries()].map(([label, value]) => ({ label, value, display: money(value) }))
  );

  const byType = new Map();
  stays.forEach((property) => {
    const type = property.type || "Stay";
    byType.set(type, (byType.get(type) || 0) + 1);
  });
  document.getElementById("adminTypeChart").innerHTML = meters(
    [...byType.entries()].map(([label, value]) => ({ label, value, display: String(value) })),
    "is-ink"
  );

  document.getElementById("adminScoreChart").innerHTML = meters(
    stays
      .slice()
      .sort((a, b) => Number(b.score) - Number(a.score))
      .map((property) => ({ label: property.name, value: Number(property.score) || 0, display: String(property.score) })),
    "",
    10
  );

  const upcoming = trips.filter((trip) => statusOf(trip) === "Upcoming");
  const arrivalRows = upcoming.map((trip) => `<tr>
    <td><a href="${escapeHtml(withPath(root.dataset.detail, trip.reference))}">${escapeHtml(trip.reference)}</a></td>
    <td>${escapeHtml(trip.propertyName)}</td>
    <td>${escapeHtml(trip.city || "—")}</td>
    <td>${escapeHtml(formatDay(trip.checkIn))}</td>
    <td><span class="pill ${statusClass(statusOf(trip))}">${escapeHtml(statusOf(trip))}</span></td>
    <td>${escapeHtml(money(due(trip)))}</td>
  </tr>`);
  document.getElementById("adminArrivals").innerHTML = arrivalRows.length
    ? sheet(["Reference", "Property", "City", "Check-in", "Status", "Due"], arrivalRows)
    : `<div class="empty"><h3>No upcoming arrivals</h3><p>Reservations that check in next appear here.</p></div>`;
}

function renderBooks() {
  const root = document.getElementById("adminBooks");
  const query = document.getElementById("bookQuery");
  const city = document.getElementById("bookCity");
  const status = document.getElementById("bookStatus");
  const cities = [...new Set(allTrips().map((trip) => trip.city).filter(Boolean))].sort();
  city.innerHTML = `<option value="">All cities</option>${cities.map((name) => `<option value="${escapeHtml(name)}">${escapeHtml(name)}</option>`).join("")}`;

  function draw() {
    const text = query.value.trim().toLowerCase();
    const rows = allTrips().filter((trip) => {
      const haystack = `${trip.reference} ${trip.email || ""} ${trip.name || ""} ${trip.propertyName}`.toLowerCase();
      if (text && !haystack.includes(text)) return false;
      if (city.value && trip.city !== city.value) return false;
      if (status.value && statusOf(trip) !== status.value) return false;
      return true;
    }).map((trip) => `<tr>
      <td><a href="${escapeHtml(withPath(root.dataset.detail, trip.reference))}">${escapeHtml(trip.reference)}</a></td>
      <td>${escapeHtml(trip.propertyName)}<br><span>${escapeHtml(trip.roomName || "")}</span></td>
      <td>${escapeHtml(trip.name || "No guest name")}<br><span>${escapeHtml(trip.email || "No email")}</span></td>
      <td>${escapeHtml(formatDay(trip.checkIn))} – ${escapeHtml(formatDay(trip.checkOut))}</td>
      <td>${escapeHtml(trip.city || "—")}</td>
      <td><span class="pill ${statusClass(statusOf(trip))}">${escapeHtml(statusOf(trip))}</span></td>
      <td>${escapeHtml(money(due(trip)))}</td>
      <td><a href="${escapeHtml(withPath(root.dataset.guest, trip.reference))}">Guest page</a></td>
    </tr>`);
    document.getElementById("adminBookTable").innerHTML = rows.length
      ? sheet(["Reference", "Stay", "Guest", "Dates", "City", "Status", "Due", ""], rows)
      : `<div class="empty"><h3>No reservations match</h3><p>Clear a filter, or book a stay in this browser.</p></div>`;
  }

  query.addEventListener("input", draw);
  city.addEventListener("change", draw);
  status.addEventListener("change", draw);
  draw();
}

function renderHouses() {
  const root = document.getElementById("adminHouses");
  const query = document.getElementById("houseQuery");
  const type = document.getElementById("houseType");
  const city = document.getElementById("houseCity");
  const cities = [...new Set(properties().map((property) => (property.address || {}).city).filter(Boolean))].sort();
  city.innerHTML = `<option value="">All cities</option>${cities.map((name) => `<option value="${escapeHtml(name)}">${escapeHtml(name)}</option>`).join("")}`;

  function draw() {
    const text = query.value.trim().toLowerCase();
    const rows = properties().filter((property) => {
      const address = property.address || {};
      if (type.value && property.type !== type.value) return false;
      if (city.value && address.city !== city.value) return false;
      if (text && !property.name.toLowerCase().includes(text)) return false;
      return true;
    }).map((property) => {
      const address = property.address || {};
      const rooms = property.rooms || [];
      const inventory = rooms.reduce((sum, room) => sum + (Number(room.inventory) || 0), 0);
      return `<tr>
        <td><a href="${escapeHtml(withPath(root.dataset.stay, property.id))}">${escapeHtml(property.name)}</a></td>
        <td>${escapeHtml(property.type || "Stay")}${property.stars ? ` · ${escapeHtml(property.stars)}★` : ""}</td>
        <td>${escapeHtml([address.area, address.city].filter(Boolean).join(" · "))}</td>
        <td>${escapeHtml(property.score)} · ${escapeHtml(property.reviewCount)}</td>
        <td>${rooms.length} rooms · ${inventory} for sale</td>
        <td>${escapeHtml(money(lowestRate(property)))}</td>
      </tr>`;
    });
    document.getElementById("adminHouseTable").innerHTML = rows.length
      ? sheet(["Stay", "Type", "Place", "Rating", "Rooms", "From"], rows)
      : `<div class="empty"><h3>No stays match</h3><p>The catalog is the eight houses already on the site.</p></div>`;
  }

  query.addEventListener("input", draw);
  type.addEventListener("change", draw);
  city.addEventListener("change", draw);
  draw();
}

function renderUsers() {
  const root = document.getElementById("adminUsers");
  const account = readAccount();
  const savedIds = readSavedStays();
  const saved = properties().filter((property) => savedIds.has(property.id));
  document.getElementById("adminAccountCard").innerHTML = account && account.email
    ? `<h2>Account in this browser</h2><dl class="trip-facts"><div><dt>Name</dt><dd>${escapeHtml(account.name || "—")}</dd></div><div><dt>Email</dt><dd>${escapeHtml(account.email)}</dd></div><div><dt>Saved stays</dt><dd>${saved.length}</dd></div></dl><p><a class="change-search" href="${escapeHtml(root.dataset.account || "/account")}">Open the account page</a></p>`
    : `<h2>Account in this browser</h2><div class="empty"><h3>No account yet</h3><p>Create one on the register page. The password is not kept.</p><a class="change-search" href="${escapeHtml(root.dataset.register || "/register")}">Register</a></div>`;
  document.getElementById("adminSavedStays").innerHTML = saved.length
    ? `<ul class="admin-saved">${saved.map((property) => `<li><a href="${escapeHtml(withPath(root.dataset.stay, property.id))}">${escapeHtml(property.name)}</a></li>`).join("")}</ul>`
    : `<div class="empty"><h3>No saved stays</h3><p>Hearts on a property page are kept in this browser.</p></div>`;

  const rows = guestRows(allTrips()).map((guest) => {
    const latest = guest.trips.slice().sort((a, b) => String(b.checkIn).localeCompare(String(a.checkIn)))[0];
    return `<tr>
      <td>${escapeHtml(guest.name || "No guest name")}</td>
      <td>${escapeHtml(guest.email || "No email on the record")}</td>
      <td>${guest.trips.map((trip) => `<a href="${escapeHtml(withPath(root.dataset.detail, trip.reference))}">${escapeHtml(trip.reference)}</a>`).join("<br>")}</td>
      <td>${escapeHtml(latest ? latest.propertyName : "—")}</td>
      <td>${escapeHtml(money(guest.due))}</td>
    </tr>`;
  });
  document.getElementById("adminDirectory").innerHTML = rows.length
    ? sheet(["Name", "Email", "Reservations", "Latest stay", "Still due"], rows)
    : `<div class="empty"><h3>No guests yet</h3><p>A name and email are written onto a reservation at payment.</p></div>`;
}

paintCounts();
if (document.getElementById("adminDesk")) renderDashboard();
if (document.getElementById("adminBooks")) renderBooks();
if (document.getElementById("adminHouses")) renderHouses();
if (document.getElementById("adminUsers")) renderUsers();
if (document.getElementById("adminAvailability")) renderAvailability();
if (document.getElementById("adminReviews")) renderReviews();
if (document.getElementById("adminMessages")) renderMessages();
if (document.getElementById("adminReport")) renderReport();

function renderAvailability() {
  const root = document.getElementById("adminAvailability");
  const body = document.getElementById("adminAvailabilityBody");
  const stayId = (root.dataset.stayId || "").trim();
  if (!stayId) {
    const rows = properties().map((property) => {
      const booked = allTrips().filter((trip) => !trip.cancelled && trip.propertyId === property.id).length;
      const pressure = stayIsOverbooked(property) ? "Overbooked" : "Within the rooms for sale";
      return `<tr>
        <td><a href="${escapeHtml(withPath(root.dataset.open, property.id))}">${escapeHtml(property.name)}</a></td>
        <td>${escapeHtml(property.type || "Stay")}</td>
        <td>${(property.rooms || []).length} rooms</td>
        <td>${booked}</td>
        <td><span class="pill ${pressure === "Overbooked" ? "cancelled" : "upcoming"}">${pressure}</span></td>
      </tr>`;
    });
    body.innerHTML = sheet(["Stay", "Type", "Catalog", "Reservations", "Nights"], rows);
    return;
  }

  const property = properties().find((item) => item.id === stayId);
  if (!property) {
    body.innerHTML = `<div class="empty"><h3>That stay is not in the catalog</h3><a class="change-search" href="${escapeHtml(root.dataset.open || "/Admin/Availability")}">All stays</a></div>`;
    return;
  }

  const trips = allTrips().filter((trip) => trip.propertyId === property.id && trip.checkIn);
  const cursor = { value: trips.map((trip) => trip.checkIn.slice(0, 7)).sort()[0] || isoDay(new Date()).slice(0, 7) };

  function draw() {
    const [year, month] = cursor.value.split("-").map(Number);
    const days = new Date(year, month, 0).getDate();
    const keys = Array.from({ length: days }, (_, index) => `${cursor.value}-${String(index + 1).padStart(2, "0")}`);
    const head = keys.map((key) => `<th>${Number(key.slice(-2))}</th>`).join("");
    const roomRows = (property.rooms || []).map((room) => {
      const cells = keys.map((key) => {
        const inventory = Number(room.inventory) || 0;
        const booked = heldOn(property.id, room.id, key);
        const free = inventory - booked;
        const kind = booked > inventory ? "over" : free === 0 ? "full" : booked > 0 ? "tight" : "open";
        const label = booked > inventory ? `Over by ${booked - inventory}` : `${free} free`;
        return `<td class="night ${kind}" title="${escapeHtml(`${room.name}: ${inventory} for sale, ${booked} booked`)}">${label === "0 free" ? "0" : escapeHtml(free < 0 ? `+${booked - inventory}` : String(free))}</td>`;
      }).join("");
      return `<tr><th class="room-name">${escapeHtml(room.name)}<span>${room.inventory} for sale</span></th>${cells}</tr>`;
    }).join("");
    const clashes = [];
    (property.rooms || []).forEach((room) => {
      keys.forEach((key) => {
        const booked = heldOn(property.id, room.id, key);
        if (booked > Number(room.inventory || 0)) clashes.push({ room: room.name, key, booked, inventory: room.inventory });
      });
    });
    const clashHtml = clashes.length
      ? `<div class="clash"><h2>Booked above the rooms for sale</h2><ul>${clashes.map((item) => `<li>${escapeHtml(formatDay(item.key))}: ${escapeHtml(item.room)} has ${item.booked} booked and ${item.inventory} for sale.</li>`).join("")}</ul></div>`
      : `<p class="where-line">No night this month is booked above the rooms for sale.</p>`;
    const monthTrips = trips.filter((trip) => tripNights(trip).some((key) => key.startsWith(cursor.value)));
    const tripRows = monthTrips.map((trip) => `<tr>
      <td><a href="${escapeHtml(withPath(root.dataset.detail, trip.reference))}">${escapeHtml(trip.reference)}</a></td>
      <td>${escapeHtml(trip.roomName || trip.roomId || "Room")}</td>
      <td>${escapeHtml(formatDay(trip.checkIn))} – ${escapeHtml(formatDay(trip.checkOut))}</td>
      <td>${Number(trip.rooms) || 1}</td>
      <td><span class="pill ${statusClass(statusOf(trip))}">${escapeHtml(statusOf(trip))}</span></td>
    </tr>`);
    body.innerHTML = `<div class="cal-tools">
        <a class="change-search" href="${escapeHtml((root.dataset.open || "/Admin/Availability/Index/").replace(/\/Index\/$/, ""))}">All stays</a>
        <button type="button" id="calPrev">Previous month</button>
        <strong id="calLabel">${escapeHtml(monthLabel(cursor.value))}</strong>
        <button type="button" id="calNext">Next month</button>
      </div>
      <div class="sheet-scroll"><table class="cal"><thead><tr><th class="room-name">${escapeHtml(property.name)}</th>${head}</tr></thead><tbody>${roomRows}</tbody></table></div>
      <ul class="cal-legend">
        <li><span class="night open"></span> All rooms free</li>
        <li><span class="night tight"></span> Some booked, rooms remain</li>
        <li><span class="night full"></span> None free</li>
        <li><span class="night over"></span> Overbooked</li>
      </ul>
      ${clashHtml}
      <h2>Reservations on these nights</h2>
      ${tripRows.length ? sheet(["Reference", "Room", "Dates", "Rooms", "Status"], tripRows) : `<div class="empty"><h3>No reservation touches this month</h3><p>PT-656069 occupies one Deluxe Courtyard Suite at The Marble Court from 12 to 15 November 2026.</p></div>`}`;
    document.getElementById("calPrev").addEventListener("click", () => {
      cursor.value = shiftMonth(cursor.value, -1);
      draw();
    });
    document.getElementById("calNext").addEventListener("click", () => {
      cursor.value = shiftMonth(cursor.value, 1);
      draw();
    });
  }

  draw();
}

function renderReviews() {
  const root = document.getElementById("adminReviews");
  const select = document.getElementById("reviewStay");
  const stays = properties();
  select.innerHTML = stays.map((property) => `<option value="${escapeHtml(property.id)}">${escapeHtml(property.name)}</option>`).join("");
  const initial = (root.dataset.stayId || "").trim();
  if (initial && stays.some((property) => property.id === initial)) select.value = initial;

  function draw() {
    const property = stays.find((item) => item.id === select.value);
    const reviews = (property && property.reviews) || [];
    const cards = reviews.map((review) => {
      const room = (property.rooms || []).find((item) => item.id === review.roomId);
      return `<article class="review-card">
        <header><h2>${escapeHtml(review.title || "Review")}</h2><strong>${escapeHtml(review.score)}</strong></header>
        <p class="where-line">${escapeHtml(review.author || "Guest")} · ${escapeHtml(review.country || "")} · ${escapeHtml(review.traveller || "")}${room ? ` · ${escapeHtml(room.name)}` : ""} · ${escapeHtml(formatDay(review.date))} · ${escapeHtml(review.nights)} nights</p>
        <p><strong>Liked.</strong> ${escapeHtml(review.liked || "")}</p>
        <p><strong>Disliked.</strong> ${escapeHtml(review.disliked || "")}</p>
      </article>`;
    }).join("");
    document.getElementById("adminReviewList").innerHTML = cards
      || `<div class="empty"><h3>No written reviews for this stay</h3><p>The score on the public page can still be an average.</p></div>`;
  }

  select.addEventListener("change", draw);
  draw();
}

function renderMessages() {
  const root = document.getElementById("adminMessages");
  const notes = readMessages().slice().reverse();
  const rows = notes.map((note) => `<tr>
    <td>${escapeHtml(formatStamp(note.at))}</td>
    <td>${escapeHtml(note.name || "—")}<br><span>${escapeHtml(note.email || "")}</span></td>
    <td>${escapeHtml(note.stay || "No particular stay")}</td>
    <td>${escapeHtml(note.message || "")}</td>
  </tr>`);
  document.getElementById("adminMessageList").innerHTML = rows.length
    ? sheet(["When", "From", "Stay", "Message"], rows)
    : `<div class="empty"><h3>No messages yet</h3><p>A note from Help is kept here.</p><a class="change-search" href="${escapeHtml(root.dataset.help || "/help")}">Write to us</a></div>`;
}

function formatStamp(iso) {
  const day = new Date(iso);
  if (Number.isNaN(day.getTime())) return "";
  return new Intl.DateTimeFormat("en-GB", { day: "numeric", month: "short", year: "numeric", hour: "2-digit", minute: "2-digit" }).format(day);
}

function renderReport() {
  const root = document.getElementById("adminReport");
  const input = document.getElementById("reportMonth");
  const now = new Date();
  input.value = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, "0")}`;

  function draw() {
    const value = input.value;
    if (!value) return;
    const [year, month] = value.split("-").map(Number);
    const start = `${value}-01`;
    const end = isoDay(new Date(year, month, 1));
    const checkingIn = allTrips().filter((trip) => trip.checkIn >= start && trip.checkIn < end);
    const nightsSold = checkingIn
      .filter((trip) => !trip.cancelled)
      .reduce((sum, trip) => sum + tripNights(trip).filter((key) => key >= start && key < end).length * (Number(trip.rooms) || 1), 0);
    const owed = checkingIn.reduce((sum, trip) => sum + due(trip), 0);
    const cancelled = checkingIn.filter((trip) => trip.cancelled);
    document.getElementById("reportKpis").innerHTML = [
      ["Nights sold", nightsSold],
      ["Reservations", checkingIn.length],
      ["Cancellations", cancelled.length],
      ["Still due", money(owed)]
    ].map(([label, amount]) => `<div class="kpi"><span>${escapeHtml(label)}</span><strong>${escapeHtml(amount)}</strong></div>`).join("");

    const parts = STATUS_COLORS.map(([label, color]) => ({
      label,
      color,
      value: checkingIn.filter((trip) => statusOf(trip) === label).length
    }));
    document.getElementById("reportStatusChart").innerHTML = donut(parts);

    const byCity = new Map();
    checkingIn.forEach((trip) => byCity.set(trip.city || "Unknown", (byCity.get(trip.city || "Unknown") || 0) + due(trip)));
    document.getElementById("reportDueChart").innerHTML = meters([...byCity.entries()].map(([label, amount]) => ({ label, value: amount, display: money(amount) })));

    const byStay = new Map();
    checkingIn.filter((trip) => !trip.cancelled).forEach((trip) => {
      const nights = tripNights(trip).filter((key) => key >= start && key < end).length * (Number(trip.rooms) || 1);
      byStay.set(trip.propertyName || "Stay", (byStay.get(trip.propertyName || "Stay") || 0) + nights);
    });
    document.getElementById("reportNightsChart").innerHTML = meters(
      [...byStay.entries()].map(([label, amount]) => ({ label, value: amount, display: String(amount) })),
      "is-ink"
    );
    document.getElementById("reportCancelChart").innerHTML = cancelled.length
      ? meters(cancelled.map((trip) => ({ label: trip.reference, value: Number(trip.total) || 0, display: trip.refund ? "Returned" : "Stays due" })))
      : `<div class="empty"><h3>No cancellations this month</h3><p>A cancelled reservation that checks in this month is listed here.</p></div>`;

    const rows = checkingIn.map((trip) => `<tr>
      <td><a href="${escapeHtml(withPath(root.dataset.detail, trip.reference))}">${escapeHtml(trip.reference)}</a></td>
      <td>${escapeHtml(trip.propertyName)}</td>
      <td>${escapeHtml(formatDay(trip.checkIn))}</td>
      <td>${tripNights(trip).filter((key) => key >= start && key < end).length}</td>
      <td><span class="pill ${statusClass(statusOf(trip))}">${escapeHtml(statusOf(trip))}</span></td>
      <td>${escapeHtml(money(due(trip)))}</td>
    </tr>`);
    document.getElementById("reportTable").innerHTML = rows.length
      ? sheet(["Reference", "Stay", "Check-in", "Nights", "Status", "Due"], rows)
      : `<div class="empty"><h3>Nothing checks in this month</h3></div>`;

    const later = allTrips().map((trip) => (trip.checkIn || "").slice(0, 7)).filter((item) => item && item !== value).sort();
    const hint = document.getElementById("reportHint");
    hint.textContent = checkingIn.length || !later.length
      ? ""
      : `Nothing checks in during ${monthLabel(value)}. ${monthLabel(later[0])} has a reservation.`;
  }

  input.addEventListener("change", draw);
  draw();
}
