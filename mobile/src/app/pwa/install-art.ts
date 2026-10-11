import { Component, computed, input } from '@angular/core';

export type InstallArtName =
  | 'ios-share'
  | 'ios-more'
  | 'ios-sheet'
  | 'ios-add'
  | 'ios-chrome-share'
  | 'android-menu'
  | 'android-install'
  | 'in-app-menu'
  | 'desktop-chrome'
  | 'macos-dock';

/**
 * The pictures of the install steps: drawings of the system's own screens in public/install-art/*.svg
 * (not public/install/: nginx would serve that folder instead of the /install page)
 * (light and dark follow the phone's theme, as the system UI they show does). The same files are
 * in docs/user/images/install/ for the guide. A real screenshot replaces one by changing `src`
 * here (and its size); nothing else knows which it is.
 */
const ART: Record<InstallArtName, { src: string; width: number; height: number; alt: string }> = {
  'ios-share': {
    src: 'install-art/ios-share.svg',
    width: 320,
    height: 180,
    alt: 'Кнопка «Поділитися» внизу Safari',
  },
  'ios-more': {
    src: 'install-art/ios-more.svg',
    width: 320,
    height: 190,
    alt: 'Кнопка «⋯» біля адреси і пункт «Поділитися»',
  },
  'ios-sheet': {
    src: 'install-art/ios-sheet.svg',
    width: 320,
    height: 250,
    alt: 'Меню «Поділитися» з пунктом «На початковий екран»',
  },
  'ios-add': {
    src: 'install-art/ios-add.svg',
    width: 320,
    height: 200,
    alt: 'Екран «На початковий екран» з кнопкою «Додати»',
  },
  'ios-chrome-share': {
    src: 'install-art/ios-chrome-share.svg',
    width: 320,
    height: 150,
    alt: 'Кнопка «Поділитися» в адресному рядку Chrome',
  },
  'android-menu': {
    src: 'install-art/android-menu.svg',
    width: 320,
    height: 250,
    alt: 'Меню «⋮» Chrome з пунктом «Встановити застосунок»',
  },
  'android-install': {
    src: 'install-art/android-install.svg',
    width: 320,
    height: 200,
    alt: 'Вікно «Встановити застосунок?»',
  },
  'in-app-menu': {
    src: 'install-art/in-app-menu.svg',
    width: 320,
    height: 170,
    alt: 'Меню «⋯» з пунктом «Відкрити в браузері»',
  },
  'desktop-chrome': {
    src: 'install-art/desktop-chrome.svg',
    width: 360,
    height: 200,
    alt: 'Значок встановлення в адресному рядку і вікно «Встановити застосунок?»',
  },
  'macos-dock': {
    src: 'install-art/macos-dock.svg',
    width: 320,
    height: 170,
    alt: 'Меню «Файл» Safari з пунктом «Додати в Dock…»',
  },
};

@Component({
  selector: 'app-install-art',
  styles: `
    :host {
      display: block;
    }
    img {
      display: block;
      width: 100%;
      max-width: 340px;
      height: auto;
      margin: 0 auto;
      border-radius: 18px;
    }
  `,
  template: `<img
    [src]="art().src"
    [width]="art().width"
    [height]="art().height"
    [alt]="art().alt"
    decoding="async"
  />`,
})
export class InstallArt {
  readonly name = input.required<InstallArtName>();
  protected readonly art = computed(() => ART[this.name()]);
}
