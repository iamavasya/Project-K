import { Routes } from '@angular/router';
import { KurinPanelComponent } from './features/kurinModule/kurin-panel/kurin-panel.component';
import { GroupPanelComponent } from './features/kurinModule/group-panel/group-panel.component';
import { MemberPanelComponent } from './features/kurinModule/member-panel/member-panel.component';
import { MemberCardComponent } from './features/kurinModule/member-card/member-card.component';

export const routes: Routes = [
    { path: 'panel', component: KurinPanelComponent },
    { path: 'kurin/:kurinKey', component: GroupPanelComponent },
    { path: 'group/:groupKey', component: MemberPanelComponent },
    { path: 'member/:memberKey', component: MemberCardComponent }
];
