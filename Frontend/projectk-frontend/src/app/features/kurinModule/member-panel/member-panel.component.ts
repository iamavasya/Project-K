import { Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TableModule } from 'primeng/table';
import { MemberService } from '../common/services/member-service/member.service';
import { MemberDto } from '../common/models/memberDto';
import { ButtonModule } from 'primeng/button';
import { GroupService } from '../common/services/group-service/group.service';
import { GroupChevron } from "../common/components/group-chevron/group-chevron";
import { GroupDto } from '../common/models/groupDto';

@Component({
  selector: 'app-member-panel',
  imports: [TableModule, ButtonModule, GroupChevron],
  templateUrl: './member-panel.component.html',
  styleUrl: './member-panel.component.css'
})
export class MemberPanelComponent implements OnInit {

  private readonly route: ActivatedRoute = inject(ActivatedRoute);
  private readonly router: Router = inject(Router);
  private readonly memberService = inject(MemberService);
  private readonly groupService = inject(GroupService);
  groupKey = '';
  group: GroupDto | null = null;
  members: MemberDto[] = [];
  selectedMember: MemberDto | null = null;


  tableHeaders: string[] = [
    'MemberKey',
    'FirstName',
    'LastName',
  ];

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      this.groupKey = params.get('groupKey')!;
    });
    this.refreshData();
  }

  refreshData(): void {
    this.groupService.exists(this.groupKey).subscribe({
      next: (exists) => {
        if (!exists) {
          this.router.navigate(['/panel'], { replaceUrl: true });
        }
      }
    });
    this.groupService.getByKey(this.groupKey).subscribe({
      next: (group) => {
        this.group = group;
      }
    });
    this.memberService.getAll(this.groupKey).subscribe({
      next: (members) => {
        this.members = members;
      },
      error: (err) => {
        console.error('Error fetching members:', err);
      }
    });
  }

  onMemberSelect() {
    this.router.navigate(['/member', this.selectedMember?.memberKey]);
  }

  onMemberCreate() {
    this.router.navigate(['/group', this.groupKey, 'member', 'upsert']);
  }
}
