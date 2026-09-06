"use strict";

(function () {
    const userListEl = document.getElementById("user-list");
    const messageListEl = document.getElementById("message-list");

    if (!userListEl && !messageListEl) {
        return; // trang này không cần SignalR
    }

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/chatHub")
        .withAutomaticReconnect()
        .build();

    function setPresenceDot(userId, isOnline) {
        const dot = document.querySelector(`li[data-user-id="${userId}"] .presence-dot`);
        if (dot) {
            dot.title = isOnline ? "online" : "offline";
            dot.classList.toggle("online", isOnline);
            dot.classList.toggle("offline", !isOnline);
        }
    }

    connection.on("PresenceChanged", function (userId, isOnline) {
        setPresenceDot(userId, isOnline);
    });

    if (messageListEl) {
        const otherUserId = messageListEl.dataset.otherUserId;
        const sendForm = document.getElementById("send-form");
        const messageInput = document.getElementById("message-input");
        const sendErrorEl = document.getElementById("send-error");

        connection.on("ReceiveMessage", function (message) {
            const belongsToThisConversation =
                message.senderId === otherUserId || message.receiverId === otherUserId;
            if (!belongsToThisConversation) {
                return;
            }

            const isMine = message.receiverId === otherUserId;
            const div = document.createElement("div");
            div.className = "message " + (isMine ? "message-mine" : "message-theirs");

            const contentSpan = document.createElement("span");
            contentSpan.className = "message-content";
            contentSpan.textContent = message.content;

            const timeSpan = document.createElement("span");
            timeSpan.className = "message-time";
            timeSpan.textContent = new Date(message.sentAtUtc).toLocaleString();

            div.appendChild(contentSpan);
            div.appendChild(timeSpan);
            messageListEl.appendChild(div);
        });

        connection.on("SendFailed", function (errorMessage) {
            sendErrorEl.textContent = "Gửi thất bại: " + errorMessage;
            sendErrorEl.style.display = "block";
        });

        sendForm.addEventListener("submit", function (event) {
            event.preventDefault();
            const content = messageInput.value.trim();
            if (!content) {
                return;
            }
            sendErrorEl.style.display = "none";
            connection.invoke("SendMessage", otherUserId, content).catch(function (err) {
                sendErrorEl.textContent = "Lỗi kết nối: " + err.message;
                sendErrorEl.style.display = "block";
            });
            messageInput.value = "";
        });
    }

    connection.start()
        .then(function () {
            if (userListEl) {
                return connection.invoke("GetOnlineUsers").then(function (onlineUserIds) {
                    onlineUserIds.forEach(function (userId) {
                        setPresenceDot(userId, true);
                    });
                });
            }
        })
        .catch(function (err) {
            console.error("Không thể kết nối SignalR:", err);
        });
})();
