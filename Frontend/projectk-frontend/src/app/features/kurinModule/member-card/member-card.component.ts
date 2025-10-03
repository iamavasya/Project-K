import { Component, inject, OnInit } from '@angular/core';
import { SkeletonModule } from 'primeng/skeleton';
import { MemberDto } from '../common/models/memberDto';
import { MemberService } from '../common/services/member-service/member.service';
import { ActivatedRoute, Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';

@Component({
  selector: 'app-member-card',
  imports: [SkeletonModule, ButtonModule],
  templateUrl: './member-card.component.html',
  styleUrl: './member-card.component.css'
})
export class MemberCardComponent implements OnInit {
  route = inject(ActivatedRoute);
  router = inject(Router);
  someText = 'Sample Text';
  memberService = inject(MemberService);
  member: MemberDto | null = null;
  memberKey: string | null = null;

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      this.memberKey = params.get('memberKey')!;
    });
    this.refreshData();
  }

  refreshData(): void {
    this.memberService.getByKey(this.memberKey!).subscribe({
      next: (member) => {
        this.member = member;
      },
      error: (error) => {
        console.error('Error fetching member:', error);
        if (this.member?.groupKey) {
          this.router.navigate(['/group', this.member?.groupKey], { replaceUrl: true });
        }
        else this.router.navigate(['/panel'], { replaceUrl: true });
      }
    });
  }

  onEditMember() {
    this.router.navigate(
      ['/group', this.member?.groupKey, 'member', 'upsert', this.memberKey],
      { state: { fromMember: true } }
    );
  }
}
