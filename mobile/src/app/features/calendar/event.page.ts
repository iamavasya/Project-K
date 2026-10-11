import { Component, computed, inject, viewChild } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import {
  IonBackButton,
  IonButton,
  IonButtons,
  IonContent,
  IonHeader,
  IonRefresher,
  IonRefresherContent,
  IonTitle,
  IonToolbar,
  NavController,
  ViewWillEnter,
} from '@ionic/angular';
import { AgendaItemView } from '../agenda/agenda-item-view';

/**
 * An event (or a dated task) opened from the calendar: everything the web's dialog shows, the
 * answer to it, «Точкування» for those who score, and «Редагувати» when the API allows it.
 */
@Component({
  selector: 'app-event',
  imports: [
    RouterLink,
    IonHeader,
    IonToolbar,
    IonButtons,
    IonBackButton,
    IonButton,
    IonTitle,
    IonContent,
    IonRefresher,
    IonRefresherContent,
    AgendaItemView,
  ],
  template: `
    <ion-header [translucent]="true">
      <ion-toolbar>
        <ion-buttons slot="start"><ion-back-button defaultHref="/tabs/calendar" text="Календар" /></ion-buttons>
        <ion-title>{{ kindTitle() }}</ion-title>
        @if (item()?.canEdit) {
          <ion-buttons slot="end">
            <ion-button [routerLink]="['/tabs/calendar/edit', itemKey]" data-testid="edit">Редагувати</ion-button>
          </ion-buttons>
        }
      </ion-toolbar>
    </ion-header>
    <ion-content [fullscreen]="true">
      <ion-refresher slot="fixed" (ionRefresh)="refresh($event)">
        <ion-refresher-content />
      </ion-refresher>
      <app-agenda-item-view [itemKey]="itemKey" [start]="start" (removed)="leave()" />
    </ion-content>
  `,
})
export class EventPage implements ViewWillEnter {
  private readonly route = inject(ActivatedRoute);
  private readonly nav = inject(NavController);
  private readonly view = viewChild(AgendaItemView);

  protected readonly itemKey = this.route.snapshot.paramMap.get('itemKey') ?? '';
  protected readonly start = this.route.snapshot.queryParamMap.get('start');
  protected readonly item = computed(() => this.view()?.item() ?? null);
  protected readonly kindTitle = computed(() => (this.item()?.kind === 'Task' ? 'Задача' : 'Подія'));
  private entered = false;

  /** Back from the form or the sheet: show what changed. */
  ionViewWillEnter(): void {
    if (this.entered) void this.view()?.load();
    this.entered = true;
  }

  protected async refresh(event: Event): Promise<void> {
    await this.view()?.load();
    await (event.target as HTMLIonRefresherElement).complete();
  }

  protected leave(): void {
    void this.nav.navigateBack('/tabs/calendar');
  }
}
