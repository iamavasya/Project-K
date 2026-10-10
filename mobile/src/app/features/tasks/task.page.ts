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
 * A task from the board: its description, dates and targets, and its progress — «Твоя частина»,
 * «X з N» and each person's box where the API's flags allow (the web's agenda-progress).
 */
@Component({
  selector: 'app-task',
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
        <ion-buttons slot="start"><ion-back-button defaultHref="/tabs/tasks" text="Задачі" /></ion-buttons>
        <ion-title>Задача</ion-title>
        @if (item()?.canEdit) {
          <ion-buttons slot="end">
            <ion-button [routerLink]="['/tabs/tasks/edit', itemKey]" data-testid="edit">Редагувати</ion-button>
          </ion-buttons>
        }
      </ion-toolbar>
    </ion-header>
    <ion-content [fullscreen]="true">
      <ion-refresher slot="fixed" (ionRefresh)="refresh($event)">
        <ion-refresher-content />
      </ion-refresher>
      <app-agenda-item-view [itemKey]="itemKey" (removed)="leave()" />
    </ion-content>
  `,
})
export class TaskPage implements ViewWillEnter {
  private readonly route = inject(ActivatedRoute);
  private readonly nav = inject(NavController);
  private readonly view = viewChild(AgendaItemView);

  protected readonly itemKey = this.route.snapshot.paramMap.get('itemKey') ?? '';
  protected readonly item = computed(() => this.view()?.item() ?? null);
  private entered = false;

  ionViewWillEnter(): void {
    if (this.entered) void this.view()?.load();
    this.entered = true;
  }

  protected async refresh(event: Event): Promise<void> {
    await this.view()?.load();
    await (event.target as HTMLIonRefresherElement).complete();
  }

  protected leave(): void {
    void this.nav.navigateBack('/tabs/tasks');
  }
}
