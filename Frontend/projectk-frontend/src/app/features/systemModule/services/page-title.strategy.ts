import { Injectable, inject } from '@angular/core';
import { RouterStateSnapshot, TitleStrategy } from '@angular/router';
import { PageTitleService } from './page-title.service';

/**
 * Hands the route's own title to PageTitleService instead of writing document.title
 * directly, so the static and the entity-resolved halves go through one place.
 */
@Injectable({
  providedIn: 'root'
})
export class ProjectKTitleStrategy extends TitleStrategy {
  private readonly pageTitle = inject(PageTitleService);

  override updateTitle(state: RouterStateSnapshot): void {
    this.pageTitle.applyRouteState(this.buildTitle(state) ?? null, state);
  }
}
