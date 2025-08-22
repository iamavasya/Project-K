import { Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TableModule } from 'primeng/table';
import { MemberService } from '../common/services/member-service/member.service';
import { MemberDto } from '../common/models/memberDto';
import { ButtonModule } from 'primeng/button';

@Component({
  selector: 'app-member-panel',
  imports: [TableModule, ButtonModule],
  templateUrl: './member-panel.component.html',
  styleUrl: './member-panel.component.scss'
})
export class MemberPanelComponent implements OnInit {

  private route: ActivatedRoute = inject(ActivatedRoute);
  private router: Router = inject(Router);
  private memberService = inject(MemberService);
  groupKey = '';
  members: MemberDto[] = [];
  selectedMember: MemberDto | null = null;


  tableHeaders: string[] = [
    'MemberKey',
    'FirstName',
    'LastName',
  ];

  constructor() { }

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      this.groupKey = params.get('groupKey')!;
    });
    this.refreshData();
  }

  refreshData(): void {
    this.memberService.getAll(this.groupKey).subscribe({
      next: (members) => {
        this.members = members;
      },
      error: (err) => {
        console.error('Error fetching members:', err);
      }
    });
  }

  onMemberSelect(event: any) {
    this.router.navigate(['/member', this.selectedMember?.memberKey]);
  }
}
