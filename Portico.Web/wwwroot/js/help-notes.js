"use strict";

const form = document.getElementById("helpNote");
const sent = document.getElementById("helpSent");

if (form) {
  form.addEventListener("submit", async (event) => {
    event.preventDefault();
    const data = new FormData(form);
    const note = {
      name: String(data.get("name") || "").trim(),
      email: String(data.get("email") || "").trim(),
      stay: String(data.get("stay") || "").trim(),
      message: String(data.get("message") || "").trim()
    };
    if (note.name.length < 2 || !note.email.includes("@") || note.message.length < 10) return;
    await PorticoApi.createMessage(note);
    form.reset();
    if (sent) sent.hidden = false;
  });
}
