import { Component, OnInit, inject, signal } from '@angular/core';
import {
  IonAvatar,
  IonBackButton,
  IonButtons,
  IonContent,
  IonHeader,
  IonItem,
  IonItemGroup,
  IonLabel,
  IonList,
  IonSkeletonText,
  IonTitle,
  IonToolbar,
} from '@ionic/angular';
import { AuthService } from '../auth/auth.service';
import { dateLabel, initials } from '../me/labels';
import { MemberDto } from '../me/me.models';
import { MeService } from '../me/me.service';

/** The person's own member card, read-only; changes go through the провід, as on the web. */
@Component({
  selector: 'app-profile',
  imports: [
    IonHeader,
    IonToolbar,
    IonButtons,
    IonBackButton,
    IonTitle,
    IonContent,
    IonList,
    IonItem,
    IonItemGroup,
    IonLabel,
    IonAvatar,
    IonSkeletonText,
  ],
  styles: `
    .head {
      display: flex;
      flex-direction: column;
      align-items: center;
      padding: 24px 16px 8px;
      text-align: center;
    }
    ion-avatar {
      width: 96px;
      height: 96px;
      background: var(--lk-primary);
      color: var(--lk-on-primary);
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 34px;
      font-weight: 700;
    }
    h1 {
      margin: 12px 0 2px;
      font-size: 24px;
      font-weight: 700;
    }
    .head p {
      margin: 0;
      color: var(--lk-muted);
    }
  `,
  template: `
    <ion-header [translucent]="true">
      <ion-toolbar>
        <ion-buttons slot="start"><ion-back-button defaultHref="/tabs/more" text="Ще" /></ion-buttons>
        <ion-title>Профіль</ion-title>
      </ion-toolbar>
    </ion-header>
    <ion-content [fullscreen]="true">
      @if (member(); as m) {
        <div class="head">
          <ion-avatar>
            @if (m.profilePhotoUrl) {
              <img [src]="m.profilePhotoUrl" alt="" />
            } @else {
              {{ initialsOf(m) }}
            }
          </ion-avatar>
          <h1>{{ m.firstName }} {{ m.lastName }}</h1>
          @if (m.latestPlastLevelDisplay) { <p>{{ m.latestPlastLevelDisplay }}</p> }
        </div>
        <ion-list [inset]="true">
          <ion-item-group>
            @for (row of rows(m); track row.label) {
              <ion-item>
                <ion-label class="ion-text-wrap">
                  <h3>{{ row.label }}</h3>
                  <p>{{ row.value }}</p>
                </ion-label>
              </ion-item>
            }
          </ion-item-group>
        </ion-list>
      } @else if (failed()) {
        <ion-list [inset]="true">
          <ion-item-group>
            <ion-item><ion-label class="ion-text-wrap">{{ failed() }}</ion-label></ion-item>
          </ion-item-group>
        </ion-list>
      } @else {
        <ion-list [inset]="true">
          <ion-item-group>
            <ion-item><ion-skeleton-text [animated]="true" style="height: 40px" /></ion-item>
            <ion-item><ion-skeleton-text [animated]="true" style="height: 40px" /></ion-item>
          </ion-item-group>
        </ion-list>
      }
    </ion-content>
  `,
})
export class ProfilePage implements OnInit {
  private readonly me = inject(MeService);
  private readonly memberKey = inject(AuthService).user()?.memberKey ?? null;
  protected readonly member = signal<MemberDto | null>(null);
  protected readonly failed = signal<string | null>(null);

  ngOnInit(): void {
    void this.load();
  }

  protected initialsOf(m: MemberDto): string {
    return initials(m.firstName, m.lastName);
  }

  protected rows(m: MemberDto): { label: string; value: string }[] {
    const rows = [
      { label: 'Повне імʼя', value: [m.lastName, m.firstName, m.middleName].filter(Boolean).join(' ') },
      { label: 'Гурток', value: m.groupName ?? '' },
      { label: 'Email', value: m.email },
      { label: 'Телефон', value: m.phoneNumber ?? '' },
      { label: 'Дата народження', value: m.dateOfBirth ? dateLabel(new Date(m.dateOfBirth)) : '' },
      { label: 'Школа', value: m.school ?? '' },
    ];
    return rows.filter((row) => row.value);
  }

  private async load(): Promise<void> {
    const memberKey = this.memberKey;
    if (!memberKey) {
      this.failed.set('Цей акаунт не привʼязаний до картки учасника.');
      return;
    }
    try {
      this.member.set(await this.me.member(memberKey));
    } catch {
      this.failed.set('Не вдалося завантажити профіль. Спробуй пізніше.');
    }
  }
}
