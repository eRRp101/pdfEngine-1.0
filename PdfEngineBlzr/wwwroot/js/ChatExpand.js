window.chatTextarea = {
    init: function () {
        const textarea = document.getElementById('chat-input');
        if (!textarea) return;

        const autoResize = () => {
            textarea.style.height = 'auto';
            textarea.style.height = Math.min(textarea.scrollHeight, 105) + 'px';
        };

        textarea.addEventListener('input', autoResize);
        textarea.addEventListener('paste', () => setTimeout(autoResize, 0));

        autoResize();

        textarea.addEventListener('keydown', function (e) {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
            }
        });
    },

    reset: function () {
        const textarea = document.getElementById('chat-input');
        if (textarea) {
            textarea.style.height = 'auto';
        }
    },

    scrollToBottom: function () {
        const chatContainer = document.querySelector(".messages-area");
        if (chatContainer) {
            chatContainer.scrollTop = chatContainer.scrollHeight;
        }
    },

    focusInput: function () {
        const textarea = document.getElementById('chat-input');
        if (textarea) {
            textarea.focus();
        }
    }
};

// ✅ Separate global function for clipboard
window.copyToClipboard = (text) => {
    try {
        navigator.clipboard.writeText(text);
        console.log("Copied to clipboard:", text);
    } catch (err) {
        console.error("Copy failed", err);
    }
};
