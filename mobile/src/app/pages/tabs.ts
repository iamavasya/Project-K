import { Component, ElementRef, OnDestroy, inject } from '@angular/core';
import { IonIcon, IonLabel, IonTabBar, IonTabButton, IonTabs, ViewDidEnter, ViewDidLeave } from '@ionic/angular';
import { registerTabBarEffect, type registeredEffect } from '@rdlabo/ionic-theme-ios27';
import { addIcons } from 'ionicons';
import { ellipsisHorizontal, home } from 'ionicons/icons';

// S1 placeholder icons; the brand calls for Lucide instead of Ionicons (PLAN.md §5).
@Component({
  selector: 'app-tabs',
  imports: [IonTabs, IonTabBar, IonTabButton, IonIcon, IonLabel],
  template: `
    <ion-tabs>
      <ion-tab-bar slot="bottom" class="tab-bar-position-center">
        <ion-tab-button tab="home">
          <ion-icon name="home" aria-hidden="true" />
          <ion-label>Головна</ion-label>
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
    addIcons({ home, ellipsisHorizontal });
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
