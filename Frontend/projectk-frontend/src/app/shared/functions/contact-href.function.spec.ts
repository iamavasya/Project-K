import { emailHref, phoneHref } from './contact-href.function';

describe('contact links', () => {
  it('dials the digits of a number written for people', () => {
    expect(phoneHref('+380 (97) 313-73-53')).toBe('tel:+380973137353');
  });

  it('does not link what is not a number or an address', () => {
    expect(phoneHref(null)).toBeNull();
    expect(phoneHref('—')).toBeNull();
    expect(emailHref('')).toBeNull();
    expect(emailHref('немає')).toBeNull();
  });

  it('mails an address as is', () => {
    expect(emailHref(' scout@plast.org.ua ')).toBe('mailto:scout@plast.org.ua');
  });
});
