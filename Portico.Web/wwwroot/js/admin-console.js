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
  if (reservationCount) reservationCount.textContent = String(trips.length);
  if (stayCount) stayCount.textContent = String(properties().length);
  if (userCount) userCount.textContent = String((readAccount() && readAccount().email ? 1 : 0) + guestRows(trips).length);
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
