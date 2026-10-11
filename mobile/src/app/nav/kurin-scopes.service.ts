import { Injectable, computed, inject, signal } from '@angular/core';
import { KurinScopeOption } from '../auth/auth.models';
import { AuthService } from '../auth/auth.service';
import { sortKurins } from '../features/account/account.labels';

/**
 * The kurins this account may act in, asked once for the header's switcher and the menu (the web's
 * kurin-switcher). Switching starts the app again on the new token: every open screen belonged to
 * the previous kurin.
 */
@Injectable({ providedIn: 'root' })
export class KurinScopes {
  private readonly auth = inject(AuthService);
  private readonly list = signal<KurinScopeOption[]>([]);
  private pending: Promise<void> | null = null;
  private loadedFor: string | null = null;

  /** The kurin acted in first, then by number. */
  readonly options = computed(() => sortKurins(this.list(), this.auth.user()?.kurinKey ?? null));
  readonly current = computed(() => this.list().find((option) => option.kurinKey === this.auth.user()?.kurinKey) ?? null);

  /** Once per account; a failure leaves the list empty (the switcher is a convenience, as on the web). */
  load(): Promise<void> {
    const userKey = this.auth.user()?.userKey ?? null;
    if (!userKey) return Promise.resolve();
    if (this.loadedFor === userKey && this.pending) return this.pending;
    this.loadedFor = userKey;
    this.pending = this.auth.kurinScopeOptions().then(
      (options) => this.list.set(options),
      () => {
        this.pending = null;
        this.loadedFor = null;
      },
    );
    return this.pending;
  }

  /** Switches the token's kurin and starts the app again at `path` (relative to the app's base). */
  async switchTo(kurinKey: string, path: string): Promise<void> {
    await this.auth.setKurinScope(kurinKey);
    globalThis.location.replace(new URL(path, document.baseURI).href);
  }
}
