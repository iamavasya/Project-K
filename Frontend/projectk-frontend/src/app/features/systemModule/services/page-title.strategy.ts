import { Injectable, inject } from '@angular/core';
import { RouterStateSnapshot, TitleStrategy } from '@angular/router';
import { PageTitleService } from './page-title.service';

@Injectable({
  providedIn: 'root'
})
export class ProjectKTitleStrategy extends TitleStrategy {
  private readonly pageTitle = inject(PageTitleService);

  override updateTitle(state: RouterStateSnapshot): void {
    this.pageTitle.applyRouteState(this.buildTitle(state) ?? null, state);
  }
}
