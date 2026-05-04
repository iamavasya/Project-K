import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { BreadcrumbComponent } from './features/kurinModule/common/components/breadcrumb/breadcrumb';
import { ToolbarHeader } from "./features/kurinModule/common/components/toolbar-header/toolbar-header";
import { ColdStartBannerComponent } from './features/systemModule/components/cold-start-banner/cold-start-banner';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, BreadcrumbComponent, ToolbarHeader, ColdStartBannerComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('projectk-frontend');
}
