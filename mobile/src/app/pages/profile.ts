import { Component } from '@angular/core';
import { MemberPage } from '../features/kurin/member.page';

/**
 * «Профіль» in «Ще»: the person's own member card, the same screen the kurin tab opens, so it stays
 * in this tab's stack (back goes to «Ще») and offers «Редагувати» for their own fields.
 */
@Component({
  selector: 'app-profile',
  imports: [MemberPage],
  template: `<app-member [own]="true" />`,
})
export class ProfilePage {}
