import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { BreadcrumbComponent } from './features/kurinModule/common/components/breadcrumb/breadcrumb';
import { ToolbarHeader } from "./features/kurinModule/common/components/toolbar-header/toolbar-header";

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, BreadcrumbComponent, ToolbarHeader],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  protected readonly title = signal('projectk-frontend');
}
