"use strict";

const NOTES_KEY = "portico-messages";
const form = document.getElementById("helpNote");
const sent = document.getElementById("helpSent");

if (form) {
  form.addEventListener("submit", (event) => {
    event.preventDefault();
    const data = new FormData(form);
    const note = {
      name: String(data.get("name") || "").trim(),
      email: String(data.get("email") || "").trim(),
      stay: String(data.get("stay") || "").trim(),
      message: String(data.get("message") || "").trim(),
      at: new Date().toISOString()
    };
    if (note.name.length < 2 || !note.email.includes("@") || note.message.length < 10) return;
    let list = [];
    try {
      const saved = JSON.parse(localStorage.getItem(NOTES_KEY) || "[]");
      list = Array.isArray(saved) ? saved : [];
    } catch {
      list = [];
    }
    list.push(note);
    localStorage.setItem(NOTES_KEY, JSON.stringify(list));
    form.reset();
    if (sent) sent.hidden = false;
  });
}
