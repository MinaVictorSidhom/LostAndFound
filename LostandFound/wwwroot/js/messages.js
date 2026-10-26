(() => {
    if (!window.signalR) return;

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/messages")
        .withAutomaticReconnect()
        .build();

    const thread = document.getElementById("message-thread");
    const inbox = document.getElementById("conversation-list");
    const messagesButtons = [...document.querySelectorAll("[data-messages-nav]")];
    const currentUserId = messagesButtons[0]?.dataset.userId.toLowerCase();
    const unreadStorageKey = currentUserId ? `unread-messages:${currentUserId}` : null;
    let unreadByConversation = {};
    try {
        unreadByConversation = JSON.parse(sessionStorage.getItem(unreadStorageKey) || "{}");
        if (!unreadByConversation || Array.isArray(unreadByConversation) || typeof unreadByConversation !== "object")
            unreadByConversation = {};
    } catch {
        unreadByConversation = {};
    }

    const unreadCount = () => Object.values(unreadByConversation).reduce((total, count) => total + (Number(count) || 0), 0);

    const updateConversationCard = card => {
        const conversationId = card.dataset.conversationId.toLowerCase();
        const count = Number(unreadByConversation[conversationId]) || 0;
        const badge = card.querySelector("[data-unread-count]");
        if (badge) {
            badge.hidden = count === 0;
            badge.textContent = count > 99 ? "99+" : String(count);
            badge.setAttribute("aria-label", `${count} unread messages`);
        }
        card.classList.toggle("has-unread", count > 0);
    };

    const updateNotification = () => {
        const count = unreadCount();
        for (const button of messagesButtons) {
            const badge = button.querySelector("[data-message-count]");
            if (!badge) continue;
            badge.hidden = count === 0;
            badge.textContent = count > 99 ? "99+" : String(count);
            button.setAttribute("aria-label", count ? `Messages, ${count} unread` : "Messages");
        }
        if (inbox) inbox.querySelectorAll(".conversation-card").forEach(updateConversationCard);
        if (unreadStorageKey) sessionStorage.setItem(unreadStorageKey, JSON.stringify(unreadByConversation));
    };

    const clearConversationNotifications = conversationId => {
        if (!conversationId) return;
        delete unreadByConversation[conversationId.toLowerCase()];
        updateNotification();
    };

    updateNotification();

    document.addEventListener("click", event => {
        const link = event.target.closest("[data-open-conversation]");
        if (link) clearConversationNotifications(link.dataset.conversationId);
    });

    if (thread) clearConversationNotifications(thread.dataset.conversationId);

    connection.on("ConversationUpdated", update => {
        if (currentUserId && update.senderUserId.toLowerCase() !== currentUserId) {
            const conversationId = update.conversationId.toLowerCase();
            const isOpenConversation = thread && thread.dataset.conversationId.toLowerCase() === conversationId;
            if (!isOpenConversation) {
                unreadByConversation[conversationId] = (Number(unreadByConversation[conversationId]) || 0) + 1;
                updateNotification();
            }
        }
    });

    if (thread) {
        const list = document.getElementById("message-list");
        const form = thread.querySelector(".message-form");
        const input = form.querySelector("textarea");
        const conversationInput = form.querySelector("[name='ConversationId']");
        const status = document.getElementById("live-status");
        const userId = thread.dataset.userId.toLowerCase();
        let conversationId = thread.dataset.conversationId || null;
        list.scrollTop = list.scrollHeight;

        const appendMessage = message => {
            const bubble = document.createElement("article");
            bubble.className = "message-bubble";
            if (message.senderUserId.toLowerCase() === userId) bubble.classList.add("own-message");

            const sender = document.createElement("strong");
            sender.textContent = message.senderName;
            const body = document.createElement("p");
            body.textContent = message.body;
            const time = document.createElement("time");
            time.dateTime = message.sentAtUtc;
            time.textContent = new Date(message.sentAtUtc).toLocaleString();
            bubble.append(sender, body, time);
            list.append(bubble);
            bubble.scrollIntoView({ behavior: "smooth", block: "nearest" });
        };

        connection.on("MessageReceived", appendMessage);
        connection.onreconnecting(() => { status.textContent = "Reconnecting to live chat…"; });
        connection.onreconnected(async () => {
            if (conversationId) await connection.invoke("JoinConversation", thread.dataset.postId, conversationId);
            status.textContent = "Live chat connected";
        });
        connection.onclose(() => { status.textContent = "Live chat disconnected. Refresh to reconnect."; });

        form.addEventListener("submit", async event => {
            event.preventDefault();
            const body = input.value.trim();
            if (!body) return;

            const button = form.querySelector("button[type='submit']");
            button.disabled = true;
            status.textContent = "Sending…";
            try {
                conversationId = await connection.invoke("SendMessage", thread.dataset.postId, conversationId, body);
                conversationInput.value = conversationId;
                thread.dataset.conversationId = conversationId;
                status.textContent = "Live chat connected";
                input.value = "";
                input.focus();
            } catch (error) {
                status.textContent = error.message || "Message could not be sent. Please try again.";
            } finally {
                button.disabled = false;
            }
        });

        connection.on("ConversationUpdated", update => {
            if (update.postId.toLowerCase() === thread.dataset.postId.toLowerCase()) {
                conversationId = update.conversationId;
                conversationInput.value = conversationId;
                thread.dataset.conversationId = conversationId;
            }
        });

        connection.start().then(async () => {
            if (conversationId) await connection.invoke("JoinConversation", thread.dataset.postId, conversationId);
            status.textContent = "Live chat connected";
        }).catch(() => { status.textContent = "Live chat unavailable. Refresh to try again."; });
    }

    if (inbox) {
        const updateCard = update => {
            const postId = update.postId.toLowerCase();
            const conversationId = update.conversationId.toLowerCase();
            let card = [...inbox.querySelectorAll(".conversation-card")].find(item =>
                item.dataset.conversationId.toLowerCase() === conversationId);

            if (!card) {
                const empty = inbox.querySelector(".empty-state");
                if (empty) empty.remove();
                card = document.createElement("article");
                card.className = "post-item conversation-card";
                card.dataset.postId = postId;
                card.dataset.conversationId = conversationId;
                const title = document.createElement("h2");
                const itemTitle = document.createElement("span");
                itemTitle.dataset.itemTitle = "";
                const unreadBadge = document.createElement("span");
                unreadBadge.className = "conversation-unread";
                unreadBadge.dataset.unreadCount = "";
                unreadBadge.hidden = true;
                title.append(itemTitle, unreadBadge);
                const latest = document.createElement("p");
                latest.dataset.latestMessage = "";
                const date = document.createElement("p");
                date.className = "contact-note";
                const link = document.createElement("a");
                link.className = "button-primary find-it-button";
                link.dataset.openConversation = "";
                link.dataset.conversationId = conversationId;
                link.textContent = "Open conversation";
                card.append(title, latest, date, link);
                inbox.prepend(card);
            }

            card.querySelector("[data-item-title]").textContent = update.itemTitle;
            card.querySelector("[data-latest-message]").textContent = update.latestMessage;
            const date = card.querySelector(".contact-note");
            date.textContent = new Date(update.updatedAtUtc).toLocaleString();
            const link = card.querySelector("a");
            link.href = `/Posts/Contact?id=${encodeURIComponent(update.postId)}&conversationId=${encodeURIComponent(update.conversationId)}`;
            updateConversationCard(card);
            inbox.prepend(card);
        };

        connection.on("ConversationUpdated", updateCard);
        connection.start().catch(() => {});
    }

    if (!thread && !inbox && messagesButtons.length) {
        connection.start().catch(() => {});
    }
})();
