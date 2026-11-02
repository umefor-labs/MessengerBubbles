(() => {
  if (window.top !== window) return;

  // Chat only: drop the site header and let the conversation fill the panel.
  const css = `
    :root { --header-height: 0px !important; }
    [role=banner] { display: none !important; }
    html, body { overflow: hidden !important; scrollbar-gutter: auto !important; }
    html::-webkit-scrollbar, body::-webkit-scrollbar { display: none !important; }
    [role=main]:has([role=log]) {
      position: fixed !important; inset: 0 !important;
      width: auto !important; height: auto !important;
      max-width: none !important; z-index: 2147483000;
      background: var(--surface-background, #fff) !important;
    }
    [role=main] div:has([role=log]) {
      height: auto !important; max-height: none !important; min-height: 0 !important; flex: 1 1 0 !important;
      margin: 0 !important; padding: 0 !important; border-radius: 0 !important;
    }
    /* Without a conversation, main is an empty placeholder that would cover the list. */
    [role=main]:not(:has([role=log])) { visibility: hidden !important; pointer-events: none !important; }
    /* The list always fills the panel; an open conversation sits above it. */
    [role=navigation]:has([role=grid]) {
      position: fixed !important; inset: 0 !important;
      width: auto !important; height: auto !important; margin: 0 !important; border-radius: 0 !important;
      max-width: none !important; max-height: none !important; z-index: 2147482000;
      background: var(--surface-background, #fff) !important;
    }
    [role=navigation] div:has([role=grid]) {
      margin: 0 !important; border-radius: 0 !important; max-height: none !important; max-width: none !important;
    }
  `;
  const addStyle = () => {
    const style = document.createElement('style');
    style.textContent = css;
    document.documentElement.appendChild(style);
  };
  if (document.documentElement) addStyle();
  else addEventListener('DOMContentLoaded', addStyle);

  // Clicking a row routes inside the page, without a reload; a direct URL at this width lands on the list.
  window.__cbOpen = href => {
    const rows = [...document.querySelectorAll('a[href*="/messages/t/"],a[href*="/messages/e2ee/t/"]')];
    if (rows.length === 0 && location.pathname.startsWith('/messages')) return 'wait';
    if (!location.pathname.startsWith('/messages')) return 'off-page';
    const row = rows.find(a => a.getAttribute('href') === href);
    if (row) {
      row.click();
      return 'clicked';
    }
    history.pushState(null, '', href);
    dispatchEvent(new PopStateEvent('popstate', { state: null }));
    return 'pushed';
  };

  // The back button only hides the conversation, so the path alone is not enough.
  window.__cbShowing = href => {
    const main = document.querySelector('[role=main]');
    const clean = p => p.replace(/\/$/, '');
    return clean(location.pathname) === clean(href) && !!main?.querySelector('[role=log]')
      && getComputedStyle(main).visibility === 'visible';
  };

  window.__cbFocus =() => document.querySelector('[role=main] [role=textbox]')?.focus();
})();
