import { PREFER_WEB_KEY, fullVersionUrl, webOnlyPage } from './web-links';

describe('links to the web', () => {
  afterEach(() => localStorage.removeItem(PREFER_WEB_KEY));

  it('keeps a page the app opens on the web there for that one load', () => {
    const url = new URL(webOnlyPage('/kurin/registry'));
    expect(url.pathname).toBe('/kurin/registry');
    expect(url.searchParams.get('web')).toBe('1');
  });

  it('remembers «Повна версія» here and tells the web in the URL as well', () => {
    const url = new URL(fullVersionUrl());
    expect(url.pathname).toBe('/');
    expect(url.searchParams.get('full')).toBe('1');
    expect(localStorage.getItem(PREFER_WEB_KEY)).toBe('1');
  });
});
