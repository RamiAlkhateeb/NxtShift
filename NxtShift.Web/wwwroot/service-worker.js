// In development, always fetch from the network and do not enable offline support.
self.addEventListener('fetch', () => { });

// Tapping a reminder brings the app forward; it routes itself to the right screen.
self.addEventListener('notificationclick', event => {
    event.notification.close();
    event.waitUntil(focusApp(event.notification.data && event.notification.data.url));
});

async function focusApp(url) {
    const windows = await self.clients.matchAll({ type: 'window', includeUncontrolled: true });
    const existing = windows.find(c => c.url.startsWith(self.registration.scope));
    if (existing) return existing.focus();
    return self.clients.openWindow(url || self.registration.scope);
}
