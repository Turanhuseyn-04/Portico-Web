"use strict";

// File pickers in the admin forms (input[type=file][data-preview]): show the chosen image or video before it is
// uploaded, and stop a file that is too large or of the wrong kind before the form sends it.
(function () {
  const inputs = document.querySelectorAll("input[type=file][data-preview]");
  if (!inputs.length) return;

  const megabytes = (bytes) => Math.round(bytes / (1024 * 1024));
  let objectUrl = null;

  inputs.forEach((input) => {
    const preview = document.getElementById(input.dataset.preview);
    const kind = input.dataset.kind === "video" ? "video" : "image";
    const max = Number(input.dataset.maxBytes) || 0;
    const original = preview ? preview.innerHTML : "";
    const label = input.closest(".upload-pick")?.querySelector("span");
    const labelText = label ? label.textContent : "";

    input.addEventListener("change", () => {
      input.setCustomValidity("");
      if (objectUrl) URL.revokeObjectURL(objectUrl);
      objectUrl = null;

      const file = input.files && input.files[0];
      if (!file) {
        if (preview) preview.innerHTML = original;
        if (label) label.textContent = labelText;
        return;
      }

      let problem = "";
      if (!file.type.startsWith(kind + "/")) problem = kind === "video" ? "Choose an MP4 or WebM video." : "Choose a JPEG, PNG, or WebP image.";
      else if (max && file.size > max) problem = `That file is ${megabytes(file.size)} MB; the limit is ${megabytes(max)} MB.`;

      if (problem) {
        input.setCustomValidity(problem);
        input.reportValidity();
        input.value = "";
        if (preview) preview.innerHTML = original;
        if (label) label.textContent = labelText;
        return;
      }

      if (label) label.textContent = file.name;
      if (!preview) return;
      objectUrl = URL.createObjectURL(file);
      const media = document.createElement(kind === "video" ? "video" : "img");
      media.src = objectUrl;
      if (kind === "video") {
        media.controls = true;
        media.muted = true;
        media.preload = "metadata";
      } else {
        media.alt = "The chosen image";
      }
      preview.replaceChildren(media);
    });
  });
})();
