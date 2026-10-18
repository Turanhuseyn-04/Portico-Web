"use strict";

// The server signs in and registers. This script only shows or hides a password while it is typed.
document.querySelectorAll(".password-toggle").forEach((toggle) => {
  const input = toggle.closest(".password-field").querySelector("input");
  toggle.addEventListener("click", () => {
    const shown = input.type === "password";
    input.type = shown ? "text" : "password";
    toggle.classList.toggle("is-shown", shown);
    toggle.setAttribute("aria-pressed", String(shown));
    toggle.setAttribute("aria-label", shown ? "Hide password" : "Show password");
  });
});
