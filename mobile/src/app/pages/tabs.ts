import { Component, ElementRef, OnDestroy, computed, inject } from '@angular/core';
import { IonIcon, IonLabel, IonTabBar, IonTabButton, IonTabs, ViewDidEnter, ViewDidLeave } from '@ionic/angular';
import { registerTabBarEffect, type registeredEffect } from '@rdlabo/ionic-theme-ios27';
import { addIcons } from 'ionicons';
import { calendar, checkmarkCircle, home, menu, people } from 'ionicons/icons';
import { NavModeService } from '../nav/nav-mode.service';

/*
 * Four tabs for every day and «Меню», which repeats the web's sidebar; filled symbols (the brand calls
 * for Lucide later, PLAN.md §5). With «Як у вебі» (Вигляд → Навігація) the bar is gone and the web's
 * ☰ drawer leads instead; the tabs still keep each section's stack.
 */
@Component({
  selector: 'app-tabs',
  imports: [IonTabs, IonTabBar, IonTabButton, IonIcon, IonLabel],
  styles: `
    ion-tab-bar.lk-hidden {
      display: none;
    }
  `,
  template: `
    <ion-tabs>
      <ion-tab-bar slot="bottom" [class.lk-hidden]="web()">
        <ion-tab-button tab="home">
          <ion-icon name="home" aria-hidden="true" />
          <ion-label>Головна</ion-label>
        </ion-tab-button>
        <ion-tab-button tab="calendar">
          <ion-icon name="calendar" aria-hidden="true" />
          <ion-label>Календар</ion-label>
        </ion-tab-button>
        <ion-tab-button tab="tasks">
          <ion-icon name="checkmark-circle" aria-hidden="true" />
          <ion-label>Задачі</ion-label>
        </ion-tab-button>
        <ion-tab-button tab="kurin">
          <ion-icon name="people" aria-hidden="true" />
          <ion-label>Курінь</ion-label>
        </ion-tab-button>
        <ion-tab-button tab="more">
          <ion-icon name="menu" aria-hidden="true" />
          <ion-label>Меню</ion-label>
        </ion-tab-button>
      </ion-tab-bar>
    </ion-tabs>
  `,
})
export class TabsPage implements ViewDidEnter, ViewDidLeave, OnDestroy {
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private effect: registeredEffect | undefined;
  private readonly nav = inject(NavModeService);
  protected readonly web = computed(() => this.nav.mode() === 'web');

  constructor() {
    addIcons({ home, calendar, checkmarkCircle, people, menu });
  }

  /** iOS 26/27 Liquid Glass lens that slides between tabs (a no-op outside ios mode). */
  ionViewDidEnter(): void {
    const tabBar = this.host.nativeElement.querySelector<HTMLElement>('ion-tab-bar');
    this.effect ??= tabBar ? registerTabBarEffect(tabBar) : undefined;
  }

  ionViewDidLeave(): void {
    this.effect?.destroy();
    this.effect = undefined;
  }

  ngOnDestroy(): void {
    this.ionViewDidLeave();
  }
}
