"use strict";

// The server checks the card. This script only spaces the number and slashes the expiry while they are typed.

function digits(value) {
  return String(value).replace(/\D/g, "");
}

function formatCard(value) {
  return digits(value).slice(0, 19).replace(/(\d{4})(?=\d)/g, "$1 ");
}

function formatExpiry(value) {
  const clean = digits(value).slice(0, 4);
  if (clean.length <= 2) return clean;
  return `${clean.slice(0, 2)}/${clean.slice(2)}`;
}

function bindFormat(input, format) {
  input.addEventListener("input", () => {
    const before = digits(input.value.slice(0, input.selectionStart)).length;
    const next = format(input.value);
    if (next === input.value) return;
    input.value = next;
    let pos = 0;
    let seen = 0;
    while (pos < input.value.length && seen < before) {
      if (/\d/.test(input.value[pos])) seen += 1;
      pos += 1;
    }
    input.setSelectionRange(pos, pos);
  });
}

document.querySelectorAll('[data-format="card"]').forEach((input) => bindFormat(input, formatCard));
document.querySelectorAll('[data-format="expiry"]').forEach((input) => bindFormat(input, formatExpiry));

// One click sends one reservation.
const paymentForm = document.getElementById("paymentForm");
if (paymentForm) {
  paymentForm.addEventListener("submit", () => {
    const submit = document.getElementById("paymentSubmit");
    submit.disabled = true;
    submit.textContent = "Paying…";
  });
}
