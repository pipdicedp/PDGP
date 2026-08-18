document.addEventListener('DOMContentLoaded', function () {

    var html = document.documentElement;

    // =========================================================
    // Font size toggle — cycles normal -> large -> extra-large
    // =========================================================
    var fontSizeSteps = ['', 'tl-fs-lg', 'tl-fs-xl'];
    var fontSizeIndex = fontSizeSteps.indexOf(localStorage.getItem('tlFontSize') || '');
    if (fontSizeIndex === -1) fontSizeIndex = 0;

    function applyFontSize() {
        fontSizeSteps.forEach(function (cls) { if (cls) html.classList.remove(cls); });
        if (fontSizeSteps[fontSizeIndex]) html.classList.add(fontSizeSteps[fontSizeIndex]);
        localStorage.setItem('tlFontSize', fontSizeSteps[fontSizeIndex]);
    }
    applyFontSize();

    var btnFontIncrease = document.getElementById('btnFontIncrease');
    var btnFontDecrease = document.getElementById('btnFontDecrease');
    var btnFontReset = document.getElementById('btnFontReset');

    if (btnFontIncrease) btnFontIncrease.addEventListener('click', function () {
        fontSizeIndex = Math.min(fontSizeIndex + 1, fontSizeSteps.length - 1);
        applyFontSize();
    });
    if (btnFontDecrease) btnFontDecrease.addEventListener('click', function () {
        fontSizeIndex = Math.max(fontSizeIndex - 1, 0);
        applyFontSize();
    });
    if (btnFontReset) btnFontReset.addEventListener('click', function () {
        fontSizeIndex = 0;
        applyFontSize();
    });

    // =========================================================
    // High-contrast toggle
    // =========================================================
    var btnContrastToggle = document.getElementById('btnContrastToggle');

    function applyContrast(enabled) {
        html.classList.toggle('tl-high-contrast', enabled);
        localStorage.setItem('tlHighContrast', enabled ? '1' : '0');
    }
    applyContrast(localStorage.getItem('tlHighContrast') === '1');

    if (btnContrastToggle) btnContrastToggle.addEventListener('click', function () {
        applyContrast(!html.classList.contains('tl-high-contrast'));
    });

    // =========================================================
    // Language toggle (English / Tamil)
    // Swaps text on any element carrying data-en / data-ta attributes.
    // To make more of the page translatable later, just add those two
    // attributes to the element — no JS changes needed.
    // =========================================================
    var btnLangToggle = document.getElementById('btnLangToggle');
    var langToggleLabel = document.getElementById('langToggleLabel');
    var translatable = document.querySelectorAll('[data-en][data-ta]');

    function applyLanguage(lang) {
        translatable.forEach(function (el) {
            el.textContent = lang === 'ta' ? el.getAttribute('data-ta') : el.getAttribute('data-en');
        });
        if (langToggleLabel) langToggleLabel.textContent = lang === 'ta' ? 'தமிழ்' : 'EN';
        html.setAttribute('lang', lang === 'ta' ? 'ta' : 'en');
        localStorage.setItem('tlLang', lang);
    }
    applyLanguage(localStorage.getItem('tlLang') || 'en');

    if (btnLangToggle) btnLangToggle.addEventListener('click', function () {
        var current = localStorage.getItem('tlLang') || 'en';
        applyLanguage(current === 'ta' ? 'en' : 'ta');
    });

    // =========================================================
    // Floating chat widget
    // Front-end shell only — wire tlChatForm's submit handler below up to
    // a real backend/bot endpoint when one is available.
    // =========================================================
    var chatPanel = document.getElementById('tlChatPanel');
    var btnOpenChat = document.getElementById('btnOpenChat');
    var btnCloseChat = document.getElementById('btnCloseChat');
    var chatForm = document.getElementById('tlChatForm');
    var chatInput = document.getElementById('tlChatInput');
    var chatMessages = document.getElementById('tlChatMessages');

    if (btnOpenChat && chatPanel) {
        btnOpenChat.addEventListener('click', function () {
            chatPanel.classList.toggle('tl-chat-open');
        });
    }
    if (btnCloseChat && chatPanel) {
        btnCloseChat.addEventListener('click', function () {
            chatPanel.classList.remove('tl-chat-open');
        });
    }
    // ---- FAQ knowledge base ----
    // Each entry is checked (in order) against the user's message in
    // lowercase; the first entry whose "keywords" all/any match wins.
    // To add a new question: add one object here — no other code changes.
    var chatFaq = [
        {
            keywords: ['apply', 'new application', 'new licence', 'new license', 'start application'],
            answer: 'To apply for a new trade licence: go to the Dashboard, click "New Application" under Trade Licence, and fill in the 8-step form (Application Details, Partners, Machinery, Photo, Documents, Shops & Establishment, Preview, Confirm).'
        },
        {
            keywords: ['status', 'track', 'tracking', 'application status'],
            answer: 'You can track your application under "Application Status Tracking" in the top menu. It shows Draft, Submitted, In Progress, Approved, or Rejected for each application.'
        },
        {
            keywords: ['document', 'documents', 'upload', 'aadhaar', 'property tax', 'building plan'],
            answer: 'Commonly required documents are: Aadhaar Copy, Property Tax Receipt, and Building Plan. Upload them in the "Upload Documents" step of your application (PDF, JPG or PNG).'
        },
        {
            keywords: ['fee', 'fees', 'payment', 'pay', 'charge', 'charges'],
            answer: 'Payments are made through "Make Payment" or the Unified Payments Gateway in the top menu, once your application reaches the payment stage.'
        },
        {
            keywords: ['certificate', 'download certificate', 'download'],
            answer: 'Once your application is approved, you can download your certificate from "Download Certificate" in the top menu.'
        },
        {
            keywords: ['password', 'forgot password', 'reset password', 'login', 'log in', 'cant login', "can't login"],
            answer: 'If you\'ve forgotten your password, use the "Forgot password?" link on the Login page to reset it.'
        },
        {
            keywords: ['register', 'new user', 'sign up', 'create account'],
            answer: 'New users can register by clicking "New User? Register Here" on the Login page.'
        },
        {
            keywords: ['partner', 'partners'],
            answer: 'Add business partners under the "Partners Details" step — enter each partner\'s name, designation, and address, then click Save.'
        },
        {
            keywords: ['machinery'],
            answer: 'Add machinery details under the "Machinery Details" step — enter machinery name, quantity, and horse power, then click Save.'
        },
        {
            keywords: ['renewal', 'renew'],
            answer: 'Licence renewal can be started the same way as a new application — select the Renewal option when starting your application.'
        },
        {
            keywords: ['contact', 'phone', 'helpline', 'support', 'email', 'toll free'],
            answer: 'For further help, please contact the Department of Industries and Commerce, Government of Puducherry, through the Grievance/Queries menu at the top of the page.'
        },
        {
            keywords: ['hi', 'hello', 'hai', 'hey'],
            answer: 'Hello! How can we help you with your trade licence application today?'
        },
        {
            keywords: ['thank', 'thanks', 'thank you'],
            answer: "You're welcome! Let us know if you need anything else."
        }
    ];

    function getFaqAnswer(message) {
        var lower = message.toLowerCase();
        for (var i = 0; i < chatFaq.length; i++) {
            var entry = chatFaq[i];
            for (var j = 0; j < entry.keywords.length; j++) {
                if (lower.indexOf(entry.keywords[j]) !== -1) {
                    return entry.answer;
                }
            }
        }
        return "Sorry, I didn't quite get that. Try asking about: applying for a licence, application status, required documents, payment, or your password.";
    }

    if (chatForm) {
        chatForm.addEventListener('submit', function (e) {
            e.preventDefault();
            var text = chatInput.value.trim();
            if (!text) return;

            var userBubble = document.createElement('div');
            userBubble.className = 'tl-chat-bubble tl-chat-bubble-user';
            userBubble.textContent = text;
            chatMessages.appendChild(userBubble);

            chatInput.value = '';
            chatMessages.scrollTop = chatMessages.scrollHeight;

            // Looks up a canned answer from chatFaq above — no server call,
            // no API key. To move to a real AI or human-agent backend later,
            // replace this setTimeout block with an $.ajax call instead.
            setTimeout(function () {
                var botBubble = document.createElement('div');
                botBubble.className = 'tl-chat-bubble tl-chat-bubble-bot';
                botBubble.textContent = getFaqAnswer(text);
                chatMessages.appendChild(botBubble);
                chatMessages.scrollTop = chatMessages.scrollHeight;
            }, 400);
        });
    }

});