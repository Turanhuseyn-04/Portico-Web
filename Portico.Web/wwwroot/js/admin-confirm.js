"use strict";

// A form with data-confirm-title (every Delete in the admin) first opens the confirmation panel. Cancel, Escape, or a
// click outside the panel leaves everything as it was; only "Delete" sends the form. Focus starts on Cancel, so
// pressing Enter by habit does not delete anything.
(function () {
  const panel = document.getElementById("confirmPanel");
  if (!panel || typeof panel.showModal !== "function") return; // without <dialog> support the form simply submits

  const title = document.getElementById("confirmTitle");
  const text = document.getElementById("confirmText");
  const go = document.getElementById("confirmGo");
  let pending = null;
  let opener = null;

  document.addEventListener("submit", (event) => {
    const form = event.target;
    if (!(form instanceof HTMLFormElement) || !form.dataset.confirmTitle) return;

    event.preventDefault();
    pending = form;
    opener = event.submitter || document.activeElement;
    title.textContent = form.dataset.confirmTitle;
    text.textContent = form.dataset.confirmText || "This cannot be undone.";
    go.textContent = form.dataset.confirmButton || "Delete";
    panel.returnValue = "cancel";
    panel.showModal();
  });

  // A click on the backdrop (outside the panel's box) cancels.
  panel.addEventListener("click", (event) => {
    if (event.target !== panel) return;
    const box = panel.getBoundingClientRect();
    const inside = event.clientX >= box.left && event.clientX <= box.right && event.clientY >= box.top && event.clientY <= box.bottom;
    if (!inside) panel.close("cancel");
  });

  panel.addEventListener("close", () => {
    const form = pending;
    pending = null;
    if (!form) return;

    if (panel.returnValue === "confirm") {
      form.submit(); // submit() does not fire "submit" again, so the panel does not reopen
      return;
    }

    if (opener && typeof opener.focus === "function") opener.focus();
  });
})();
