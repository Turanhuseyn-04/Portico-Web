"use strict";

// The header menu, for pages that have no script of their own.
(() => {
  const header = document.getElementById("header");
  const nav = document.getElementById("nav");
  const navToggle = document.getElementById("navToggle");
  if (!header || !nav || !navToggle) return;

  function closeMenu() {
    nav.classList.remove("is-open");
    header.classList.remove("menu-open");
    navToggle.setAttribute("aria-expanded", "false");
    navToggle.setAttribute("aria-label", "Open menu");
  }

  navToggle.addEventListener("click", () => {
    const open = nav.classList.toggle("is-open");
    header.classList.toggle("menu-open", open);
    navToggle.setAttribute("aria-expanded", String(open));
    navToggle.setAttribute("aria-label", open ? "Close menu" : "Open menu");
  });

  document.addEventListener("click", (event) => {
    if (event.target.closest(".nav a")) closeMenu();
  });

  document.addEventListener("keydown", (event) => {
    if (event.key === "Escape" && nav.classList.contains("is-open")) closeMenu();
  });
})();
