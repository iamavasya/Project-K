import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { ButtonModule } from '@openng/optimus-ui/button';
import { pwaPathFor } from '../../functions/install-app.function';
import { InstallAppService } from '../../services/install-app-service/install-app.service';

/**
 * A phone that chose «Повна версія» sees one quiet line back to «Мобільна версія».
 *
 * «Лілейка на телефоні»: on a phone, the web offers its phone app (the PWA under /m/). «Встановити»
 * opens Chrome's install dialog where Chrome has offered one, otherwise the step-by-step page in the
 * app; «Відкрити мобільну версію» continues on the same screen there, already signed in.
 */
@Component({
  selector: 'app-install-app-banner',
  imports: [ButtonModule],
  templateUrl: './install-app-banner.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './install-app-banner.css'
})
export class InstallAppBannerComponent {
  private readonly installApp = inject(InstallAppService);
  private readonly router = inject(Router);

  protected readonly isVisible = this.installApp.shouldOffer;
  protected readonly offersMobileVersion = this.installApp.offersMobileVersion;

  async install(): Promise<void> {
    if (this.installApp.canPrompt()) {
      await this.installApp.prompt();
      return;
    }
    // The app is another Angular app on this origin: a full page load, not a router navigation.
    this.goTo('/m/install');
  }

  /** Back to the phone app on the same screen; «Повна версія» is forgotten, so the next visit goes there too. */
  openMobile(): void {
    this.installApp.forgetPreferWeb();
    this.goTo(pwaPathFor(this.router.url));
  }

  dismiss(): void {
    this.installApp.dismiss();
  }

  /** A seam for the specs: a real navigation would take the test page with it. */
  protected goTo(url: string): void {
    window.location.assign(url);
  }
}
