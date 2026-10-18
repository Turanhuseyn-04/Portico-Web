"use strict";

function contentNote(id, message, bad) {
  const note = document.getElementById(id);
  if (!note) return;
  note.hidden = !message;
  note.textContent = message || "";
  note.classList.toggle("is-bad", Boolean(bad));
}

function fieldValue(form, name) {
  const field = form.elements.namedItem(name);
  return field ? String(field.value || "") : "";
}

function fieldHtml(label, name, value, type, attrs) {
  const kind = type || "text";
  return `<label>${escapeHtml(label)}<input name="${name}" type="${kind}" value="${escapeHtml(value ?? "")}" ${attrs || ""}></label>`;
}

function mountTableEditor(config) {
  const root = document.getElementById(config.rootId);
  if (!root || typeof PorticoApi === "undefined") return;
  const query = document.getElementById(config.queryId);
  const editor = document.getElementById(config.editorId);
  const list = document.getElementById(config.listId);
  let rows = [];
  let editingId = null;

  function filtered() {
    const text = query.value.trim().toLowerCase();
    return rows.filter((row) => !text || config.search(row).toLowerCase().includes(text));
  }

  function draw() {
    const visible = filtered();
    const body = visible.map((row) => `<tr>
      ${config.cells(row).map((cell) => `<td>${cell}</td>`).join("")}
      <td class="row-actions">
        <button type="button" data-edit="${escapeHtml(row.id)}">Edit</button>
        <button type="button" data-delete="${escapeHtml(row.id)}">Delete</button>
      </td>
    </tr>`);
    const empty = rows.length
      ? `<div class="empty"><h3>Nothing matches</h3><p>Clear the filter to see every ${escapeHtml(config.noun)}.</p></div>`
      : `<div class="empty"><h3>${escapeHtml(config.emptyTitle)}</h3><p>${escapeHtml(config.emptyText)}</p></div>`;
    list.innerHTML = body.length ? sheet([...config.columns, ""], body) : empty;
    list.querySelectorAll("[data-edit]").forEach((button) => {
      button.addEventListener("click", () => openForm(rows.find((row) => String(row.id) === button.dataset.edit)));
    });
    list.querySelectorAll("[data-delete]").forEach((button) => {
      button.addEventListener("click", () => removeRow(button.dataset.delete));
    });
  }

  function openForm(row) {
    editingId = row ? row.id : null;
    editor.hidden = false;
    editor.innerHTML = `<form class="editor" id="${config.formId}">
      <h2>${row ? "Edit" : "Create"}</h2>
      <div class="editor-grid">${config.fields(row || {}).join("")}</div>
      <div class="editor-actions">
        <button class="btn-ink" type="submit">${row ? "Save" : "Create"}</button>
        <button type="button" data-cancel>Cancel</button>
      </div>
    </form>`;
    editor.querySelector("[data-cancel]").addEventListener("click", () => {
      editor.hidden = true;
      editor.innerHTML = "";
      editingId = null;
    });
    editor.querySelector("form").addEventListener("submit", async (event) => {
      event.preventDefault();
      contentNote(config.noteId, "");
      try {
        const body = config.read(event.currentTarget);
        if (editingId == null) await config.api.create(body);
        else await config.api.edit(editingId, body);
        editor.hidden = true;
        editor.innerHTML = "";
        editingId = null;
        await load();
        contentNote(config.noteId, "Saved.");
      } catch (error) {
        contentNote(config.noteId, error.message || "That form could not be saved.", true);
      }
    });
    editor.querySelector("input, textarea")?.focus();
  }

  async function removeRow(id) {
    const row = rows.find((item) => String(item.id) === String(id));
    if (!window.confirm(`Delete this ${config.noun}?`)) return;
    contentNote(config.noteId, "");
    try {
      await config.api.remove(id);
      if (String(editingId) === String(id)) {
        editor.hidden = true;
        editor.innerHTML = "";
        editingId = null;
      }
      await load();
      contentNote(config.noteId, "Deleted.");
    } catch (error) {
      contentNote(config.noteId, error.message || `That ${config.noun} could not be deleted.`, true);
    }
    return row;
  }

  async function load() {
    rows = await config.api.list();
    draw();
  }

  document.getElementById(config.filterId).addEventListener("click", draw);
  document.getElementById(config.clearId).addEventListener("click", () => {
    query.value = "";
    draw();
  });
  query.addEventListener("keydown", (event) => {
    if (event.key === "Enter") {
      event.preventDefault();
      draw();
    }
  });
  document.getElementById(config.newId).addEventListener("click", () => openForm(null));
  load().catch((error) => contentNote(config.noteId, error.message || "The list did not load.", true));
}

function renderSlides() {
  mountTableEditor({
    rootId: "adminSlides",
    queryId: "slideQuery",
    filterId: "slideFilter",
    clearId: "slideClear",
    newId: "newSlide",
    editorId: "slideEditor",
    listId: "slideList",
    noteId: "slideNote",
    formId: "slideForm",
    noun: "slide",
    columns: ["City", "Country", "Image", "Stays", "Order"],
    emptyTitle: "No slides yet",
    emptyText: "Create one with a city, a country, and an image path such as /assets/lisbon.jpg.",
    api: PorticoApi.adminResource("/admin/slides"),
    search: (row) => `${row.city} ${row.country} ${row.image}`,
    cells: (row) => [
      escapeHtml(row.city),
      escapeHtml(row.country),
      `<span class="path-cell">${row.image ? `<img src="${escapeHtml(row.image)}" alt="">` : ""}<code>${escapeHtml(row.image)}</code></span>`,
      escapeHtml(row.stays),
      escapeHtml(row.order)
    ],
    fields: (row) => [
      fieldHtml("City", "city", row.city),
      fieldHtml("Country", "country", row.country),
      fieldHtml("Image path", "image", row.image),
      fieldHtml("Stays", "stays", row.stays ?? 0, "number"),
      fieldHtml("Order", "order", row.order ?? 0, "number")
    ],
    read: (form) => ({
      city: fieldValue(form, "city").trim(),
      country: fieldValue(form, "country").trim(),
      image: fieldValue(form, "image").trim(),
      stays: Number(fieldValue(form, "stays")),
      order: Number(fieldValue(form, "order"))
    })
  });
}

function renderVideos() {
  mountTableEditor({
    rootId: "adminVideos",
    queryId: "videoQuery",
    filterId: "videoFilter",
    clearId: "videoClear",
    newId: "newVideo",
    editorId: "videoEditor",
    listId: "videoList",
    noteId: "videoNote",
    formId: "videoForm",
    noun: "video",
    columns: ["Video", "Order"],
    emptyTitle: "No videos yet",
    emptyText: "Create one with a path such as /assets/videos/villas/14041585_1920_1080_25fps.mp4.",
    api: PorticoApi.adminResource("/admin/videos"),
    search: (row) => row.src || "",
    cells: (row) => [
      `<code>${escapeHtml(row.src)}</code>`,
      escapeHtml(row.order)
    ],
    fields: (row) => [
      fieldHtml("Video path", "src", row.src),
      fieldHtml("Order", "order", row.order ?? 0, "number")
    ],
    read: (form) => ({
      src: fieldValue(form, "src").trim(),
      order: Number(fieldValue(form, "order"))
    })
  });
}

function reviewFields(review, rooms) {
  const options = [`<option value="">No particular room</option>`]
    .concat((rooms || []).map((room) => `<option value="${escapeHtml(room.id)}" ${review.roomId === room.id ? "selected" : ""}>${escapeHtml(room.name)}</option>`));
  return [
    `<label>Room<select name="roomId">${options.join("")}</select></label>`,
    fieldHtml("Author", "author", review.author),
    fieldHtml("Country", "country", review.country),
    fieldHtml("Traveller", "traveller", review.traveller),
    fieldHtml("Nights", "nights", review.nights ?? 1, "number"),
    fieldHtml("Date", "date", review.date || "", "date"),
    `<label>Score<input name="score" type="number" min="0" max="10" step="0.1" value="${escapeHtml(review.score ?? "")}"></label>`,
    fieldHtml("Title", "title", review.title),
    `<label class="span-2">Liked<textarea name="liked">${escapeHtml(review.liked || "")}</textarea></label>`,
    `<label class="span-2">Disliked<textarea name="disliked">${escapeHtml(review.disliked || "")}</textarea></label>`
  ];
}

function readReview(form, propertyId) {
  return {
    propertyId,
    roomId: fieldValue(form, "roomId"),
    author: fieldValue(form, "author").trim(),
    country: fieldValue(form, "country").trim(),
    traveller: fieldValue(form, "traveller").trim(),
    nights: Number(fieldValue(form, "nights")),
    date: fieldValue(form, "date"),
    score: Number(fieldValue(form, "score")),
    title: fieldValue(form, "title").trim(),
    liked: fieldValue(form, "liked").trim(),
    disliked: fieldValue(form, "disliked").trim()
  };
}

window.porticoRenderReviews = function renderReviewEditor() {
  const root = document.getElementById("adminReviews");
  if (!root) return;
  const select = document.getElementById("reviewStay");
  const query = document.getElementById("reviewQuery");
  const editor = document.getElementById("reviewEditor");
  const stays = properties();
  const api = PorticoApi.adminResource("/admin/reviews");
  select.innerHTML = stays.map((property) => `<option value="${escapeHtml(property.id)}">${escapeHtml(property.name)}</option>`).join("");
  const initial = (root.dataset.stayId || "").trim();
  if (initial && stays.some((property) => property.id === initial)) select.value = initial;
  let rows = [];
  let editingId = null;

  function currentStay() {
    return stays.find((item) => item.id === select.value);
  }

  function visible() {
    const text = query.value.trim().toLowerCase();
    return rows.filter((review) => {
      if (review.propertyId !== select.value) return false;
      const haystack = `${review.author} ${review.title} ${review.country}`.toLowerCase();
      return !text || haystack.includes(text);
    });
  }

  function draw() {
    const property = currentStay();
    const cards = visible().map((review) => {
      const room = ((property && property.rooms) || []).find((item) => item.id === review.roomId);
      return `<article class="review-card">
        <header><h2>${escapeHtml(review.title || "Review")}</h2><strong>${escapeHtml(review.score)}</strong></header>
        <p class="where-line">${escapeHtml(review.author || "Guest")} · ${escapeHtml(review.country || "")} · ${escapeHtml(review.traveller || "")}${room ? ` · ${escapeHtml(room.name)}` : ""} · ${escapeHtml(formatDay(review.date))} · ${escapeHtml(review.nights)} nights</p>
        <p><strong>Liked.</strong> ${escapeHtml(review.liked || "")}</p>
        <p><strong>Disliked.</strong> ${escapeHtml(review.disliked || "")}</p>
        <div class="row-actions">
          <button type="button" data-edit="${escapeHtml(review.id)}">Edit</button>
          <button type="button" data-delete="${escapeHtml(review.id)}">Delete</button>
        </div>
      </article>`;
    });
    document.getElementById("adminReviewList").innerHTML = cards.join("")
      || `<div class="empty"><h3>No written reviews for this stay</h3><p>Create one, or clear the filter.</p></div>`;
    document.querySelectorAll("#adminReviewList [data-edit]").forEach((button) => {
      button.addEventListener("click", () => openForm(rows.find((row) => String(row.id) === button.dataset.edit)));
    });
    document.querySelectorAll("#adminReviewList [data-delete]").forEach((button) => {
      button.addEventListener("click", () => removeReview(button.dataset.delete));
    });
  }

  function openForm(review) {
    const property = currentStay();
    editingId = review ? review.id : null;
    editor.hidden = false;
    editor.innerHTML = `<form class="editor" id="reviewForm">
      <h2>${review ? "Edit review" : "Create review"}</h2>
      <div class="editor-grid">${reviewFields(review || {}, property && property.rooms).join("")}</div>
      <div class="editor-actions">
        <button class="btn-ink" type="submit">${review ? "Save" : "Create"}</button>
        <button type="button" data-cancel>Cancel</button>
      </div>
    </form>`;
    editor.querySelector("[data-cancel]").addEventListener("click", () => {
      editor.hidden = true;
      editor.innerHTML = "";
      editingId = null;
    });
    editor.querySelector("form").addEventListener("submit", async (event) => {
      event.preventDefault();
      try {
        const body = readReview(event.currentTarget, select.value);
        if (editingId == null) await api.create(body);
        else await api.edit(editingId, body);
        editor.hidden = true;
        editor.innerHTML = "";
        editingId = null;
        await load();
        contentNote("reviewNote", "Saved.");
      } catch (error) {
        contentNote("reviewNote", error.message || "That review could not be saved.", true);
      }
    });
  }

  async function removeReview(id) {
    if (!window.confirm("Delete this review?")) return;
    try {
      await api.remove(id);
      await load();
      contentNote("reviewNote", "Deleted.");
    } catch (error) {
      contentNote("reviewNote", error.message || "That review could not be deleted.", true);
    }
  }

  async function load() {
    rows = await api.list();
    draw();
  }

  document.getElementById("reviewFilter").addEventListener("click", draw);
  document.getElementById("reviewClear").addEventListener("click", () => {
    query.value = "";
    draw();
  });
  select.addEventListener("change", () => {
    editor.hidden = true;
    editor.innerHTML = "";
    editingId = null;
    draw();
  });
  document.getElementById("newReview").addEventListener("click", () => openForm(null));
  load().catch((error) => contentNote("reviewNote", error.message || "The reviews did not load.", true));
};

function roomEditor(room) {
  const item = room || { id: "", name: "", price: "", sleeps: 2, refundable: true, breakfast: false, inventory: 1 };
  return `<fieldset class="room-edit">
    <label>Room id<input name="roomId" value="${escapeHtml(item.id)}"></label>
    <label>Name<input name="roomName" value="${escapeHtml(item.name)}"></label>
    <label>Price<input name="price" type="number" min="0" step="0.01" value="${escapeHtml(item.price)}"></label>
    <label>Sleeps<input name="sleeps" type="number" min="1" value="${escapeHtml(item.sleeps)}"></label>
    <label>For sale<input name="inventory" type="number" min="0" value="${escapeHtml(item.inventory)}"></label>
    <label class="check"><input name="refundable" type="checkbox" ${item.refundable ? "checked" : ""}> Refundable</label>
    <label class="check"><input name="breakfast" type="checkbox" ${item.breakfast ? "checked" : ""}> Breakfast</label>
    <button type="button" data-remove-room>Remove room</button>
  </fieldset>`;
}

function readStay(form) {
  const stars = fieldValue(form, "stars").trim();
  const rooms = [...form.querySelectorAll(".room-edit")].map((row) => ({
    id: row.querySelector('[name="roomId"]').value.trim(),
    name: row.querySelector('[name="roomName"]').value.trim(),
    price: Number(row.querySelector('[name="price"]').value),
    sleeps: Number(row.querySelector('[name="sleeps"]').value),
    refundable: row.querySelector('[name="refundable"]').checked,
    breakfast: row.querySelector('[name="breakfast"]').checked,
    inventory: Number(row.querySelector('[name="inventory"]').value)
  })).filter((room) => room.id);
  return {
    id: fieldValue(form, "id").trim(),
    name: fieldValue(form, "name").trim(),
    type: fieldValue(form, "type").trim(),
    stars: stars === "" ? null : Number(stars),
    street: fieldValue(form, "street").trim(),
    area: fieldValue(form, "area").trim(),
    city: fieldValue(form, "city").trim(),
    postcode: fieldValue(form, "postcode").trim(),
    country: fieldValue(form, "country").trim(),
    lat: Number(fieldValue(form, "lat")),
    lng: Number(fieldValue(form, "lng")),
    summary: fieldValue(form, "summary").trim(),
    description: fieldValue(form, "description").split(/\n/).map((line) => line.trim()).filter(Boolean),
    rooms
  };
}

function stayFields(stay, lockId) {
  const item = stay || { description: [], rooms: [] };
  const description = Array.isArray(item.description) ? item.description.join("\n") : "";
  return `<form class="editor" id="stayForm">
    <h2>${lockId ? "Edit stay" : "Create stay"}</h2>
    <div class="editor-grid">
      <label>Id<input name="id" value="${escapeHtml(item.id || "")}" ${lockId ? "readonly" : ""} placeholder="marble"></label>
      ${fieldHtml("Name", "name", item.name)}
      ${fieldHtml("Type", "type", item.type, "text")}
      ${fieldHtml("Stars", "stars", item.stars ?? "", "number")}
      ${fieldHtml("Street", "street", item.street)}
      ${fieldHtml("Area", "area", item.area)}
      ${fieldHtml("City", "city", item.city)}
      ${fieldHtml("Postcode", "postcode", item.postcode)}
      ${fieldHtml("Country", "country", item.country)}
      ${fieldHtml("Latitude", "lat", item.lat ?? "", "number", 'step="any"')}
      ${fieldHtml("Longitude", "lng", item.lng ?? "", "number", 'step="any"')}
      <label class="span-2">Summary<textarea name="summary">${escapeHtml(item.summary || "")}</textarea></label>
      <label class="span-2">Description<textarea name="description" placeholder="One paragraph on each line">${escapeHtml(description)}</textarea></label>
    </div>
    <h3>Rooms</h3>
    <div class="room-stack">${(item.rooms && item.rooms.length ? item.rooms : [{}]).map(roomEditor).join("")}</div>
    <div class="editor-actions">
      <button type="button" data-add-room>Add room</button>
      <button class="btn-ink" type="submit">${lockId ? "Save" : "Create"}</button>
      ${lockId ? `<button type="button" data-delete-stay>Delete</button>` : ""}
      <button type="button" data-cancel-stay>Cancel</button>
    </div>
  </form>`;
}

function bindStayForm(host, stay, onDone, noteId) {
  const note = noteId || "stayNote";
  host.innerHTML = stayFields(stay, Boolean(stay && stay.id));
  const form = host.querySelector("form");
  const stack = form.querySelector(".room-stack");
  form.querySelector("[data-add-room]").addEventListener("click", () => {
    stack.insertAdjacentHTML("beforeend", roomEditor(null));
  });
  stack.addEventListener("click", (event) => {
    const button = event.target.closest("[data-remove-room]");
    if (!button) return;
    const rows = stack.querySelectorAll(".room-edit");
    if (rows.length === 1) {
      button.closest(".room-edit").querySelectorAll("input").forEach((input) => {
        if (input.type === "checkbox") input.checked = false;
        else input.value = "";
      });
      return;
    }
    button.closest(".room-edit").remove();
  });
  const cancel = form.querySelector("[data-cancel-stay]");
  if (cancel) cancel.addEventListener("click", () => onDone("cancel"));
  const remove = form.querySelector("[data-delete-stay]");
  if (remove) {
    remove.addEventListener("click", async () => {
      if (!window.confirm(`Delete ${stay.name || stay.id}? A stay with a reservation cannot be deleted.`)) return;
      try {
        await PorticoApi.adminResource("/admin/properties").remove(stay.id);
        onDone("deleted");
      } catch (error) {
        contentNote(note, error.message || "That stay could not be deleted.", true);
      }
    });
  }
  form.addEventListener("submit", async (event) => {
    event.preventDefault();
    const body = readStay(form);
    try {
      if (stay && stay.id) await PorticoApi.adminResource("/admin/properties").edit(stay.id, body);
      else await PorticoApi.adminResource("/admin/properties").create(body);
      onDone("saved", body.id);
    } catch (error) {
      contentNote(note, error.message || "That stay could not be saved.", true);
    }
  });
}

function renderStayCreator() {
  const host = document.getElementById("stayCreator");
  const opener = document.getElementById("newStay");
  if (!host || !opener) return;
  opener.addEventListener("click", () => {
    host.hidden = false;
    contentNote("houseNote", "");
    bindStayForm(host, null, (result, id) => {
      if (result === "cancel") {
        host.hidden = true;
        host.innerHTML = "";
        return;
      }
      const list = document.getElementById("adminHouses");
      window.location.assign(`${list.dataset.stay}${encodeURIComponent(id)}`);
    }, "houseNote");
  });
}

function photoCard(photo) {
  return `<article class="photo-card" data-photo="${escapeHtml(photo.id)}">
    <img src="${escapeHtml(photo.src)}" alt="${escapeHtml(photo.alt || "")}">
    <div>
      <strong>${escapeHtml(photo.alt || "No alt text")}</strong>
      <code>${escapeHtml(photo.src)}</code>
      <span>Order ${escapeHtml(photo.order)}</span>
      <div class="row-actions">
        <button type="button" data-edit-photo="${escapeHtml(photo.id)}">Edit</button>
        <button type="button" data-delete-photo="${escapeHtml(photo.id)}">Delete</button>
      </div>
    </div>
  </article>`;
}

function renderPhotoEditor(propertyId) {
  const host = document.getElementById("adminPhotos");
  if (!host) return;
  const api = PorticoApi.adminPhotos(propertyId);
  let photos = [];
  let editingId = null;

  function filtered() {
    const text = document.getElementById("photoQuery").value.trim().toLowerCase();
    return photos.filter((photo) => !text || `${photo.src} ${photo.alt}`.toLowerCase().includes(text));
  }

  function draw() {
    const visible = filtered();
    document.getElementById("photoList").innerHTML = visible.length
      ? visible.map(photoCard).join("")
      : `<div class="empty"><h3>No photographs match</h3><p>Add a path such as /assets/lisbon.jpg, or clear the filter.</p></div>`;
  }

  function openForm(photo) {
    editingId = photo ? photo.id : null;
    const editor = document.getElementById("photoEditor");
    editor.hidden = false;
    editor.innerHTML = `<form class="editor" id="photoForm">
      <h2>${photo ? "Edit photograph" : "Add photograph"}</h2>
      <div class="editor-grid">
        ${fieldHtml("Image path", "src", photo && photo.src)}
        ${fieldHtml("Alt text", "alt", photo && photo.alt)}
        ${fieldHtml("Order", "order", photo ? photo.order : photos.length, "number")}
      </div>
      <div class="editor-actions">
        <button class="btn-ink" type="submit">${photo ? "Save" : "Create"}</button>
        <button type="button" data-cancel>Cancel</button>
      </div>
    </form>`;
    editor.querySelector("[data-cancel]").addEventListener("click", () => {
      editor.hidden = true;
      editor.innerHTML = "";
      editingId = null;
    });
    editor.querySelector("form").addEventListener("submit", async (event) => {
      event.preventDefault();
      const form = event.currentTarget;
      const body = {
        src: fieldValue(form, "src").trim(),
        alt: fieldValue(form, "alt").trim(),
        order: Number(fieldValue(form, "order"))
      };
      try {
        if (editingId == null) await api.create(body);
        else await api.edit(editingId, body);
        editor.hidden = true;
        editor.innerHTML = "";
        editingId = null;
        await load();
        contentNote("photoNote", "Saved.");
      } catch (error) {
        contentNote("photoNote", error.message || "That photograph could not be saved.", true);
      }
    });
  }

  async function load() {
    photos = await api.list();
    photos.sort((a, b) => a.order - b.order || a.id - b.id);
    draw();
  }

  document.getElementById("photoFilter").addEventListener("click", draw);
  document.getElementById("photoClear").addEventListener("click", () => {
    document.getElementById("photoQuery").value = "";
    draw();
  });
  document.getElementById("newPhoto").addEventListener("click", () => openForm(null));
  document.getElementById("photoList").addEventListener("click", async (event) => {
    const edit = event.target.closest("[data-edit-photo]");
    const remove = event.target.closest("[data-delete-photo]");
    if (edit) openForm(photos.find((photo) => String(photo.id) === edit.dataset.editPhoto));
    if (!remove) return;
    if (!window.confirm("Delete this photograph?")) return;
    try {
      await api.remove(remove.dataset.deletePhoto);
      await load();
      contentNote("photoNote", "Deleted.");
    } catch (error) {
      contentNote("photoNote", error.message || "That photograph could not be deleted.", true);
    }
  });
  load().catch((error) => contentNote("photoNote", error.message || "The photographs did not load.", true));
}

function renderStayEditor() {
  const host = document.getElementById("adminStayEditor");
  const root = document.getElementById("adminStay");
  if (!host || !root) return;
  const id = (root.dataset.stayId || "").trim();
  if (!id) return;
  host.innerHTML = `<p id="stayNote" class="editor-note" hidden></p><div id="stayFormHost"></div>
    <section class="photo-desk" id="adminPhotos">
      <h2>Photographs</h2>
      <div class="admin-tools">
        <input id="photoQuery" type="search" placeholder="Path or alt text" aria-label="Filter photographs">
        <button type="button" id="photoFilter">Filter</button>
        <button type="button" id="photoClear">Clear</button>
        <button type="button" class="btn-ink" id="newPhoto">Add photograph</button>
      </div>
      <p id="photoNote" class="editor-note" hidden></p>
      <div id="photoEditor" hidden></div>
      <div id="photoList" class="photo-grid"></div>
    </section>`;
  const formHost = document.getElementById("stayFormHost");
  function show(stay) {
    bindStayForm(formHost, stay, (result) => {
      if (result === "cancel") return;
      if (result === "deleted") {
        window.location.assign(root.dataset.admin || "/Admin/Stay");
        return;
      }
      contentNote("stayNote", "Saved.");
      PorticoApi.adminResource("/admin/properties").get(id).then(show).catch((error) => {
        contentNote("stayNote", error.message || "That stay did not load.", true);
      });
    });
  }
  PorticoApi.adminResource("/admin/properties").get(id).then(show).catch((error) => {
    contentNote("stayNote", error.message || "That stay did not load.", true);
  });
  renderPhotoEditor(id);
}

window.porticoContentBoot = function () {
  renderSlides();
  renderVideos();
  renderStayCreator();
  renderStayEditor();
};
