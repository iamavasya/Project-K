import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ButtonModule } from '@openng/optimus-ui/button';
import { DividerModule } from '@openng/optimus-ui/divider';
import { TagModule } from '@openng/optimus-ui/tag';
import { environment } from '../../../../../environments/environment';

@Component({
  selector: 'app-welcome-page',
  imports: [RouterLink, ButtonModule, DividerModule, TagModule],
  templateUrl: './welcome-page.html',
  styleUrl: './welcome-page.css'
})
export class WelcomePageComponent {
  // The one place BRANDBOOK §0 still allows the technical name next to the version.
  readonly techLine = `ProjectK · ${environment.envName} · ${environment.version}`;
}
