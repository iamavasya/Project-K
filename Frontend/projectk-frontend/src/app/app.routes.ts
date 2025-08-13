import { Routes } from '@angular/router';
import { KurinPanelComponent } from './features/kurinModule/kurin-panel/kurin-panel.component';
import { GroupPanelComponent } from './features/kurinModule/group-panel/group-panel.component';

export const routes: Routes = [
    { path: 'panel', component: KurinPanelComponent },
    { path: 'kurin/:kurinKey', component: GroupPanelComponent }
];
