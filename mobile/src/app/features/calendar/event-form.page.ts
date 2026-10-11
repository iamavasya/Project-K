import { Component, inject, viewChild } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { IonButton, IonButtons, IonContent, IonHeader, IonSpinner, IonTitle, IonToolbar, NavController } from '@ionic/angular';
import { AgendaForm } from '../agenda/agenda-form';

/** A new event from the calendar (on the day picked there), or an item opened from it, edited. */
@Component({
  selector: 'app-event-form',
  imports: [IonHeader, IonToolbar, IonButtons, IonButton, IonTitle, IonContent, IonSpinner, AgendaForm],
  template: `
    <ion-header [translucent]="true">
      <ion-toolbar>
        <ion-buttons slot="start"><ion-button (click)="cancel()">Скасувати</ion-button></ion-buttons>
        <ion-title>{{ itemKey ? 'Редагувати' : 'Нова подія' }}</ion-title>
        <ion-buttons slot="end">
          <ion-button [strong]="true" [disabled]="!form()?.canSave()" (click)="save()" data-testid="save">
            @if (form()?.saving()) { <ion-spinner name="crescent" /> } @else { Зберегти }
          </ion-button>
        </ion-buttons>
      </ion-toolbar>
    </ion-header>
    <ion-content [fullscreen]="true">
      <app-agenda-form defaultKind="Event" [itemKey]="itemKey" [day]="day" />
    </ion-content>
  `,
})
export class EventFormPage {
  private readonly route = inject(ActivatedRoute);
  private readonly nav = inject(NavController);
  protected readonly form = viewChild(AgendaForm);
  protected readonly itemKey = this.route.snapshot.paramMap.get('itemKey');
  protected readonly day = this.route.snapshot.queryParamMap.get('date');

  protected cancel(): void {
    void this.nav.back();
  }

  protected async save(): Promise<void> {
    if (await this.form()?.save()) void this.nav.back();
  }
}
