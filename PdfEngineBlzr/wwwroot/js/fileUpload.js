// ============================
// FileUpload Interop Utilities
// ============================

// Trigger file picker programmatically
window.triggerFilePicker = function (wrapperElement) {
    const fileInput = wrapperElement.querySelector('input[type="file"]');
    if (fileInput) {
        fileInput.click();
    }
};

// Extract dropped files as raw byte arrays (if needed later)
window.getDroppedFiles = async function (dataTransfer) {
    const files = [];
    if (dataTransfer.items) {
        for (let i = 0; i < dataTransfer.items.length; i++) {
            const item = dataTransfer.items[i];
            if (item.kind === 'file') {
                const file = item.getAsFile();
                const buffer = await file.arrayBuffer();
                files.push({
                    name: file.name,
                    size: file.size,
                    type: file.type,
                    data: Array.from(new Uint8Array(buffer))
                });
            }
        }
    }
    return files;
};

// Prevent default drag/drop behavior for a drop zone
window.preventDefaultDragEvents = function (dropZoneElement) {
    const dropZone = dropZoneElement instanceof HTMLElement ? dropZoneElement : document.querySelector(dropZoneElement);

    if (!dropZone) return;

    const stop = (e) => {
        e.preventDefault();
        e.stopPropagation();
    };

    ['dragenter', 'dragover', 'dragleave', 'drop'].forEach(eventName => {
        dropZone.addEventListener(eventName, stop, false);
    });
};

// Trigger file picker on click (duplicate safe)
window.triggerFilePicker = function (fileInputWrapper) {
    const input = fileInputWrapper.querySelector('input[type="file"]');
    if (input) input.click();
};

// Initialize full drag-n-drop support with visual cue and file input sync
window.initializeDropZone = function (dropZoneElement, dotNetRef) {
    const stop = (e) => {
        e.preventDefault();
        e.stopPropagation();
    };

    const dropZone = dropZoneElement instanceof HTMLElement ? dropZoneElement : document.querySelector(dropZoneElement);

    if (!dropZone) return;

    ['dragenter', 'dragover'].forEach(eventName => {
        dropZone.addEventListener(eventName, (e) => {
            stop(e);
            dropZone.classList.add("dragging");
        });
    });

    ['dragleave', 'drop'].forEach(eventName => {
        dropZone.addEventListener(eventName, (e) => {
            stop(e);
            dropZone.classList.remove("dragging");
        });
    });

    dropZone.addEventListener("drop", async (e) => {
        stop(e);
        const files = Array.from(e.dataTransfer.files);
        if (files.length > 0) {
            const input = dropZone.querySelector('input[type="file"]');
            if (input) {
                const dataTransfer = new DataTransfer();
                files.forEach(f => dataTransfer.items.add(f));
                input.files = dataTransfer.files;

                const event = new Event('change', { bubbles: true });
                input.dispatchEvent(event);
            }
        }
    });
};
