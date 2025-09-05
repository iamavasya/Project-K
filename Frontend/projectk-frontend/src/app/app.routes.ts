import { Routes } from '@angular/router';
import { KurinPanelComponent } from './features/kurinModule/kurin-panel/kurin-panel.component';
import { GroupPanelComponent } from './features/kurinModule/group-panel/group-panel.component';
import { MemberPanelComponent } from './features/kurinModule/member-panel/member-panel.component';
import { MemberCardComponent } from './features/kurinModule/member-card/member-card.component';
import { UpsertMemberComponent } from './features/kurinModule/upsert-member/upsert-member.component';
import { KurinNumberComponent } from './features/kurinModule/common/components/kurin-number/kurin-number';

export const routes: Routes = [
    { path: 'panel', component: KurinPanelComponent },
    { path: 'kurin/:kurinKey', component: GroupPanelComponent },
    { path: 'group/:groupKey', component: MemberPanelComponent },
    { path: 'group/:groupKey/member/upsert/:memberKey', component: UpsertMemberComponent },
    { path: 'group/:groupKey/member/upsert', component: UpsertMemberComponent },
    { path: 'member/:memberKey', component: MemberCardComponent },
    { path: 'kurinNumber/:kurinNumber', component: KurinNumberComponent },
];
