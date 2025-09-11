import { Routes } from '@angular/router';
import { KurinPanelComponent } from './features/kurinModule/kurin-panel/kurin-panel.component';
import { GroupPanelComponent } from './features/kurinModule/group-panel/group-panel.component';
import { MemberPanelComponent } from './features/kurinModule/member-panel/member-panel.component';
import { MemberCardComponent } from './features/kurinModule/member-card/member-card.component';
import { UpsertMemberComponent } from './features/kurinModule/upsert-member/upsert-member.component';

export const routes: Routes = [
  { 
    path: 'panel', 
    component: KurinPanelComponent,
    data: { breadcrumb: 'Panel' }
  },
  { 
    path: 'kurin/:kurinKey', 
    component: GroupPanelComponent,
    data: { breadcrumb: 'Kurin', parent: '/panel' }
  },
  { 
    path: 'group/:groupKey', 
    component: MemberPanelComponent,
    data: { breadcrumb: 'Group', parent: '/kurin/:kurinKey' }
  },
  { 
    path: 'group/:groupKey/member/upsert/:memberKey', 
    component: UpsertMemberComponent,
    data: { breadcrumb: 'Edit Member', parent: '/group/:groupKey' }
  },
  { 
    path: 'group/:groupKey/member/upsert', 
    component: UpsertMemberComponent,
    data: { breadcrumb: 'New Member', parent: '/group/:groupKey' }
  },
  { 
    path: 'member/:memberKey', 
    component: MemberCardComponent,
    data: { breadcrumb: 'Member Card', parent: '/group/:groupKey' }
  }
];
