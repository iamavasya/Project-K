import { AfterViewInit, Component, inject, signal, ViewChild } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { BreadcrumbComponent } from './features/kurinModule/common/components/breadcrumb/breadcrumb';
import { ToolbarHeader } from "./features/kurinModule/common/components/toolbar-header/toolbar-header";
import { ColdStartBannerComponent } from './features/systemModule/components/cold-start-banner/cold-start-banner';
import { MfaSetupDialogComponent } from './features/authModule/components/mfa-setup-dialog/mfa-setup-dialog.component';
import { MfaEnforcerService } from './features/authModule/services/mfa-enforcer.service';
import { ToastModule } from 'primeng/toast';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, BreadcrumbComponent, ToolbarHeader, ColdStartBannerComponent, MfaSetupDialogComponent, ToastModule],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements AfterViewInit {
  protected readonly title = signal('projectk-frontend');
  private readonly mfaEnforcer = inject(MfaEnforcerService);

  @ViewChild('mfaDialog') mfaDialog!: MfaSetupDialogComponent;

  ngAfterViewInit(): void {
    this.mfaEnforcer.checkAndEnforce(this.mfaDialog);
  }

  onMfaEnabled(): void {
    this.mfaEnforcer.markMfaEnabledForCurrentSession();
  }
}
