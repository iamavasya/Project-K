import { Component } from '@angular/core';
import {
  IonBadge,
  IonButton,
  IonButtons,
  IonCard,
  IonCardContent,
  IonCardHeader,
  IonCardSubtitle,
  IonCardTitle,
  IonCheckbox,
  IonContent,
  IonHeader,
  IonIcon,
  IonInput,
  IonItem,
  IonLabel,
  IonList,
  IonListHeader,
  IonNote,
  IonProgressBar,
  IonRadio,
  IonRadioGroup,
  IonSegment,
  IonSegmentButton,
  IonTabBar,
  IonTabButton,
  IonTitle,
  IonToggle,
  IonToolbar,
} from '@ionic/angular';
import { addIcons } from 'ionicons';
import { calendarOutline, ellipsisHorizontal, home, notificationsOutline, trendingUp } from 'ionicons/icons';

/**
 * The component sheet: every control the app uses, as Ionic ships it next to the Лілейка version
 * (src/theme/lileyka.scss, scoped to `.lk` until it is rolled out). Ionic picks iOS or Material
 * from the device; `?ionic:mode=ios|md` shows the other one in any browser. Open without signing
 * in, so it works on the PR preview too.
 */
@Component({
  selector: 'app-ui-sheet',
  imports: [
    IonHeader,
    IonToolbar,
    IonTitle,
    IonButtons,
    IonContent,
    IonButton,
    IonIcon,
    IonSegment,
    IonSegmentButton,
    IonLabel,
    IonToggle,
    IonCheckbox,
    IonRadioGroup,
    IonRadio,
    IonInput,
    IonList,
    IonListHeader,
    IonItem,
    IonNote,
    IonBadge,
    IonCard,
    IonCardHeader,
    IonCardSubtitle,
    IonCardTitle,
    IonCardContent,
    IonProgressBar,
    IonTabBar,
    IonTabButton,
  ],
  styles: `
    .sheet {
      max-width: 720px;
      margin: 0 auto;
      padding: 12px 16px 48px;
    }
    .intro {
      margin: 4px 0 8px;
      font-size: 14px;
      color: var(--ion-color-medium);
    }
    .modes {
      display: flex;
      gap: 8px;
      margin-bottom: 8px;
    }
    .modes a {
      flex: 1;
      padding: 8px;
      border-radius: 8px;
      border: 1px solid var(--ion-color-step-200, #ddd);
      text-align: center;
      text-decoration: none;
      color: inherit;
      font-size: 14px;
    }
    .modes a.current {
      border-color: var(--ion-color-primary);
      color: var(--ion-color-primary);
      font-weight: 600;
    }
    section {
      margin-top: 28px;
    }
    h2 {
      margin: 0 0 2px;
      font-size: 17px;
      font-weight: 700;
    }
    .why {
      margin: 0 0 12px;
      font-size: 13px;
      color: var(--ion-color-medium);
    }
    .pair {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 12px;
    }
    .pair.stack {
      grid-template-columns: 1fr;
    }
    .side {
      display: flex;
      flex-direction: column;
      gap: 10px;
      min-width: 0;
    }
    .cap {
      font-size: 11px;
      font-weight: 700;
      letter-spacing: 0.12em;
      text-transform: uppercase;
      color: var(--ion-color-medium);
    }
    .lk .cap {
      color: var(--lk-primary);
    }
    .row {
      display: flex;
      flex-wrap: wrap;
      gap: 6px;
    }
    .side ion-list {
      margin: 0;
    }
    .frame {
      border: 1px dashed var(--ion-color-step-250, #ccc);
      border-radius: 8px;
      overflow: hidden;
    }
    ion-tab-bar {
      position: static;
    }
  `,
  template: `
    <ion-header>
      <ion-toolbar>
        <ion-title>Довідник UI</ion-title>
      </ion-toolbar>
    </ion-header>
    <ion-content>
      <div class="sheet">
        <p class="intro">
          Ліворуч Ionic як є, праворуч Лілейка. Зараз показано {{ mode === 'ios' ? 'iOS' : 'Android' }}.
        </p>
        <div class="modes">
          <a href="ui?ionic:mode=ios" [class.current]="mode === 'ios'" data-testid="mode-ios">Як на iPhone</a>
          <a href="ui?ionic:mode=md" [class.current]="mode === 'md'" data-testid="mode-md">Як на Android</a>
        </div>

        <section data-testid="buttons">
          <h2>Кнопки</h2>
          <p class="why">{{ why.buttons }}</p>
          <div class="pair">
            <div class="side">
              <span class="cap">Ionic</span>
              <ion-button expand="block">Зберегти</ion-button>
              <ion-button expand="block" color="medium">Інша дія</ion-button>
              <ion-button expand="block" fill="outline">Відкрити</ion-button>
              <ion-button expand="block" fill="clear">Скасувати</ion-button>
              <ion-button expand="block" fill="clear" color="danger">Вийти</ion-button>
              <div class="row">
                <ion-button size="small">Почати</ion-button>
                <ion-button size="small" fill="outline">Зроблено</ion-button>
              </div>
              <ion-button expand="block" [disabled]="true">Вимкнено</ion-button>
            </div>
            <div class="side lk">
              <span class="cap">Лілейка</span>
              <ion-button expand="block">Зберегти</ion-button>
              <ion-button expand="block" class="lk-secondary" [color]="mode === 'ios' ? 'medium' : undefined">Інша дія</ion-button>
              <ion-button expand="block" fill="outline">Відкрити</ion-button>
              <ion-button expand="block" fill="clear">Скасувати</ion-button>
              <ion-button expand="block" fill="clear" class="lk-danger" [color]="mode === 'ios' ? 'danger' : undefined">Вийти</ion-button>
              <div class="row">
                <ion-button size="small">Почати</ion-button>
                <ion-button size="small" fill="outline">Зроблено</ion-button>
              </div>
              <ion-button expand="block" [disabled]="true">Вимкнено</ion-button>
            </div>
          </div>
        </section>

        <section data-testid="tabs">
          <h2>Вкладки</h2>
          <p class="why">Як вкладки дошки у вебі: лоток з рамкою, обрана вкладка біла з рамкою. Висота й шрифт як у сегмента Ionic на кожній платформі.</p>
          <div class="pair stack">
            <div class="side">
              <span class="cap">Ionic</span>
              <ion-segment value="events">
                <ion-segment-button value="events"><ion-label>Події</ion-label></ion-segment-button>
                <ion-segment-button value="tasks"><ion-label>Задачі</ion-label></ion-segment-button>
                <ion-segment-button value="growth"><ion-label>Поступ</ion-label></ion-segment-button>
              </ion-segment>
              <ion-segment value="maybe">
                <ion-segment-button value="going"><ion-label>Іду</ion-label></ion-segment-button>
                <ion-segment-button value="maybe"><ion-label>Можливо</ion-label></ion-segment-button>
                <ion-segment-button value="no"><ion-label>Не йду</ion-label></ion-segment-button>
              </ion-segment>
            </div>
            <div class="side lk">
              <span class="cap">Лілейка</span>
              <ion-segment value="events">
                <ion-segment-button value="events"><ion-label>Події</ion-label></ion-segment-button>
                <ion-segment-button value="tasks"><ion-label>Задачі</ion-label></ion-segment-button>
                <ion-segment-button value="growth"><ion-label>Поступ</ion-label></ion-segment-button>
              </ion-segment>
              <ion-segment value="maybe">
                <ion-segment-button value="going"><ion-label>Іду</ion-label></ion-segment-button>
                <ion-segment-button value="maybe"><ion-label>Можливо</ion-label></ion-segment-button>
                <ion-segment-button value="no"><ion-label>Не йду</ion-label></ion-segment-button>
              </ion-segment>
            </div>
          </div>
        </section>

        <section data-testid="switches">
          <h2>Перемикачі, прапорці, вибір</h2>
          <p class="why">Як в Ionic, лише брендовий шрифт підписів.</p>
          <div class="pair">
            <div class="side">
              <span class="cap">Ionic</span>
              <ion-toggle [checked]="true" justify="space-between">Нагадування</ion-toggle>
              <ion-toggle justify="space-between">Тиха година</ion-toggle>
              <ion-checkbox [checked]="true" justify="start" labelPlacement="end">Іду</ion-checkbox>
              <ion-checkbox justify="start" labelPlacement="end">Беру намет</ion-checkbox>
              <ion-radio-group value="a">
                <ion-radio value="a" justify="start" labelPlacement="end">Гурток</ion-radio><br />
                <ion-radio value="b" justify="start" labelPlacement="end">Курінь</ion-radio>
              </ion-radio-group>
            </div>
            <div class="side lk">
              <span class="cap">Лілейка</span>
              <ion-toggle [checked]="true" justify="space-between">Нагадування</ion-toggle>
              <ion-toggle justify="space-between">Тиха година</ion-toggle>
              <ion-checkbox [checked]="true" justify="start" labelPlacement="end">Іду</ion-checkbox>
              <ion-checkbox justify="start" labelPlacement="end">Беру намет</ion-checkbox>
              <ion-radio-group value="a">
                <ion-radio value="a" justify="start" labelPlacement="end">Гурток</ion-radio><br />
                <ion-radio value="b" justify="start" labelPlacement="end">Курінь</ion-radio>
              </ion-radio-group>
            </div>
          </div>
        </section>

        <section data-testid="inputs">
          <h2>Поля</h2>
          <p class="why">Як у вебі: підпис над полем, рамка 1px, радіус 8, у фокусі зелена рамка з ореолом. Крапки пароля системні, як в Ionic.</p>
          <div class="pair stack">
            <div class="side">
              <span class="cap">Ionic</span>
              <ion-list [inset]="true">
                <ion-item>
                  <ion-input label="Email" labelPlacement="stacked" placeholder="name@plast.org.ua" />
                </ion-item>
                <ion-item>
                  <ion-input label="Пароль" labelPlacement="stacked" type="password" value="секретик" />
                </ion-item>
              </ion-list>
            </div>
            <div class="side lk">
              <span class="cap">Лілейка</span>
              <div class="lk-field">
                <span class="lk-field__label">Email</span>
                <div class="lk-input-box"><ion-input aria-label="Email" placeholder="name@plast.org.ua" /></div>
              </div>
              <div class="lk-field">
                <span class="lk-field__label">Пароль</span>
                <div class="lk-input-box lk-invalid"><ion-input aria-label="Пароль" type="password" value="секретик" /></div>
                <span class="lk-field__error">Невірний email або пароль.</span>
              </div>
            </div>
          </div>
        </section>

        <section data-testid="list">
          <h2>Меню (список «Ще»)</h2>
          <p class="why">{{ why.list }}</p>
          <div class="pair stack">
            <div class="side">
              <span class="cap">Ionic</span>
              <ion-list [inset]="true">
                <ion-list-header>Акаунт</ion-list-header>
                <ion-item [button]="true" [detail]="true">
                  <ion-label>
                    <h3>Профіль</h3>
                    <p>ostap&#64;plast.org.ua</p>
                  </ion-label>
                </ion-item>
                <ion-item [button]="true" [detail]="true">
                  <ion-label>Двофакторний вхід</ion-label>
                  <ion-note slot="end">увімкнено</ion-note>
                </ion-item>
                <ion-item [button]="true">
                  <ion-label color="danger">Вийти</ion-label>
                </ion-item>
              </ion-list>
            </div>
            <div class="side lk">
              <span class="cap">Лілейка</span>
              <ion-list [inset]="true">
                <ion-list-header>Акаунт</ion-list-header>
                <ion-item [button]="true" [detail]="true">
                  <ion-label>
                    <h3>Профіль</h3>
                    <p>ostap&#64;plast.org.ua</p>
                  </ion-label>
                </ion-item>
                <ion-item [button]="true" [detail]="true">
                  <ion-label>Двофакторний вхід</ion-label>
                  <ion-note slot="end">увімкнено</ion-note>
                </ion-item>
                <ion-item [button]="true">
                  <ion-label color="danger">Вийти</ion-label>
                </ion-item>
              </ion-list>
            </div>
          </div>
        </section>

        <section data-testid="tags">
          <h2>Мітки</h2>
          <p class="why">Розмір і заокруглення з Ionic, кольори з вебу: зелений «прийнято», терракота «очікує», червоний «борг».</p>
          <div class="pair">
            <div class="side">
              <span class="cap">Ionic</span>
              <div class="row">
                <ion-badge color="success">Прийнято</ion-badge>
                <ion-badge color="warning">Очікує</ion-badge>
                <ion-badge color="danger">Борг</ion-badge>
                <ion-badge color="medium">Учасник</ion-badge>
              </div>
            </div>
            <div class="side lk">
              <span class="cap">Лілейка</span>
              <div class="row">
                <ion-badge class="lk-tag--info">Прийнято</ion-badge>
                <ion-badge class="lk-tag--warn">Очікує</ion-badge>
                <ion-badge class="lk-tag--danger">Борг</ion-badge>
                <ion-badge class="lk-tag--secondary">Учасник</ion-badge>
              </div>
            </div>
          </div>
        </section>

        <section data-testid="card">
          <h2>Картка</h2>
          <p class="why">{{ why.card }}</p>
          <div class="pair stack">
            <div class="side">
              <span class="cap">Ionic</span>
              <ion-card>
                <ion-card-header>
                  <ion-card-subtitle>Проба</ion-card-subtitle>
                  <ion-card-title>Перша проба</ion-card-title>
                </ion-card-header>
                <ion-card-content>
                  <p>Підписано 12 з 30</p>
                  <ion-progress-bar [value]="0.4" />
                </ion-card-content>
              </ion-card>
            </div>
            <div class="side lk">
              <span class="cap">Лілейка</span>
              <ion-card>
                <ion-card-header>
                  <ion-card-subtitle>Проба</ion-card-subtitle>
                  <ion-card-title>Перша проба</ion-card-title>
                </ion-card-header>
                <ion-card-content>
                  <p>Підписано 12 з 30</p>
                  <ion-progress-bar [value]="0.4" />
                </ion-card-content>
              </ion-card>
            </div>
          </div>
        </section>

        <section data-testid="bars">
          <h2>Шапка й нижнє меню</h2>
          <p class="why">Розміри з Ionic; білий фон, лінія 1px замість тіні, брендовий шрифт, зелений активний пункт, темні іконки шапки.</p>
          <div class="pair stack">
            <div class="side">
              <span class="cap">Ionic</span>
              <div class="frame">
                <ion-toolbar>
                  <ion-title>Профіль</ion-title>
                  <ion-buttons slot="end">
                    <ion-button aria-label="Сповіщення"><ion-icon slot="icon-only" name="notifications-outline" /></ion-button>
                  </ion-buttons>
                </ion-toolbar>
              </div>
              <div class="frame">
                <ion-tab-bar>
                  <ion-tab-button tab="home" [selected]="true"><ion-icon name="home" /><ion-label>Головна</ion-label></ion-tab-button>
                  <ion-tab-button tab="calendar"><ion-icon name="calendar-outline" /><ion-label>Календар</ion-label></ion-tab-button>
                  <ion-tab-button tab="growth"><ion-icon name="trending-up" /><ion-label>Поступ</ion-label></ion-tab-button>
                  <ion-tab-button tab="more"><ion-icon name="ellipsis-horizontal" /><ion-label>Ще</ion-label></ion-tab-button>
                </ion-tab-bar>
              </div>
            </div>
            <div class="side lk">
              <span class="cap">Лілейка</span>
              <div class="frame">
                <ion-toolbar>
                  <ion-title>Профіль</ion-title>
                  <ion-buttons slot="end">
                    <ion-button aria-label="Сповіщення"><ion-icon slot="icon-only" name="notifications-outline" /></ion-button>
                  </ion-buttons>
                </ion-toolbar>
              </div>
              <div class="frame">
                <ion-tab-bar>
                  <ion-tab-button tab="home" [selected]="true"><ion-icon name="home" /><ion-label>Головна</ion-label></ion-tab-button>
                  <ion-tab-button tab="calendar"><ion-icon name="calendar-outline" /><ion-label>Календар</ion-label></ion-tab-button>
                  <ion-tab-button tab="growth"><ion-icon name="trending-up" /><ion-label>Поступ</ion-label></ion-tab-button>
                  <ion-tab-button tab="more"><ion-icon name="ellipsis-horizontal" /><ion-label>Ще</ion-label></ion-tab-button>
                </ion-tab-bar>
              </div>
            </div>
          </div>
        </section>
      </div>
    </ion-content>
  `,
})
export class UiSheetPage {
  protected readonly mode = document.documentElement.getAttribute('mode') === 'md' ? 'md' : 'ios';

  /** What the Лілейка column takes from where, per platform (Rost's review, PLAN.md §18). */
  protected readonly why =
    this.mode === 'ios'
      ? {
          buttons: 'На iPhone лишаємо кнопки Ionic як є.',
          list: 'Розміри iOS і великий заголовок групи, кольори й лінія 1px з бренду.',
          card: 'Плитка з вебу (рамка без тіні, «Проба» над назвою); розміри, заокруглення й прогрес з Ionic.',
        }
      : {
          buttons: 'Вигляд з вебу (радіус 8, без капсу й тіні), розміри Material: 36px, шрифт 14/500.',
          list: 'Меню як у вебі: групи на рамці 1px з радіусом 12, без тіні.',
          card: 'Плитка з вебу (рамка без тіні, радіус 12, «Проба» над назвою); розміри шрифтів і прогрес з Ionic.',
        };

  constructor() {
    addIcons({ home, calendarOutline, trendingUp, ellipsisHorizontal, notificationsOutline });
  }
}
