// Cài đặt Service Worker ngay lập tức
self.addEventListener('install', event => {
    self.skipWaiting();
});

// Kích hoạt và dọn dẹp bộ nhớ đệm
self.addEventListener('activate', event => {
    event.waitUntil(clients.claim());
});

// Cho phép trình duyệt tải dữ liệu từ server bình thường (Không bị trắng trang)
self.addEventListener('fetch', event => {
    event.respondWith(fetch(event.request).catch(() => {
        return new Response("Ứng dụng đang ngoại tuyến (Offline).");
    }));
});