(() => {
  if (window.top !== window) return;
  const send = () => {
    const rows = [...document.querySelectorAll('a[href*="/messages/t/"],a[href*="/messages/e2ee/t/"]')].map(a => {
      const img = a.querySelector('img,image');
      return {
        href: a.getAttribute('href'),
        text: a.innerText || '',
        bold: [...a.querySelectorAll('span')].some(s => +getComputedStyle(s).fontWeight >= 600),
        avatar: img?.src || img?.getAttribute('xlink:href') || '',
        // Facebook's crossed-out bell icon on a chat with notifications off.
        muted: [...a.querySelectorAll('svg path')].some(p => p.getAttribute('d')?.startsWith('M2.5 6c0-.322')),
      };
    });
    try { chrome.webview.postMessage({ rows }); } catch {}
  };
  let pending = null;
  const soon = () => { pending ??= setTimeout(() => { pending = null; send(); }, 500); };
  const watch = () => new MutationObserver(soon).observe(document.documentElement,
    { subtree: true, childList: true, characterData: true, attributes: true, attributeFilter: ['class'] });
  if (document.documentElement) watch();
  else addEventListener('DOMContentLoaded', watch);
  setInterval(send, 5000);
})();
