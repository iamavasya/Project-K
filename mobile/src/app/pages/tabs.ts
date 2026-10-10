import { Component, ElementRef, OnDestroy, inject } from '@angular/core';
import { IonIcon, IonLabel, IonTabBar, IonTabButton, IonTabs, ViewDidEnter, ViewDidLeave } from '@ionic/angular';
import { registerTabBarEffect, type registeredEffect } from '@rdlabo/ionic-theme-ios27';
import { addIcons } from 'ionicons';
import { calendar, checkmarkCircle, ellipsisHorizontal, home, people } from 'ionicons/icons';

// Five tabs, as Apple's HIG allows on a phone; filled symbols. The brand calls for Lucide later (PLAN.md §5).
@Component({
  selector: 'app-tabs',
  imports: [IonTabs, IonTabBar, IonTabButton, IonIcon, IonLabel],
  template: `
    <ion-tabs>
      <ion-tab-bar slot="bottom">
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
          <ion-icon name="ellipsis-horizontal" aria-hidden="true" />
          <ion-label>Ще</ion-label>
        </ion-tab-button>
      </ion-tab-bar>
    </ion-tabs>
  `,
})
export class TabsPage implements ViewDidEnter, ViewDidLeave, OnDestroy {
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private effect: registeredEffect | undefined;

  constructor() {
    addIcons({ home, calendar, checkmarkCircle, people, ellipsisHorizontal });
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
