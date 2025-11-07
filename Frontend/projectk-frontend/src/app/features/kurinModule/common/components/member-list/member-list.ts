import { Component, inject, Input, OnInit } from '@angular/core';
import { MemberService } from '../../services/member-service/member.service';
import { MemberDto } from '../../models/memberDto';
import { TableModule } from 'primeng/table';
import { InputIconModule } from 'primeng/inputicon';
import { IconFieldModule } from 'primeng/iconfield';
import { InputTextModule } from 'primeng/inputtext';
import { Router } from '@angular/router';
import { LeadershipService } from '../../services/leadership-service/leadership-service';
import { LeadershipDto } from '../../models/requests/leadership/leadershipDto';
import { MemberLookupDto } from '../../models/requests/member/memberLookupDto';
import { ButtonModule } from 'primeng/button';

@Component({
  selector: 'app-member-list',
  imports: [TableModule, InputIconModule, IconFieldModule, InputTextModule, ButtonModule],
  templateUrl: './member-list.html',
  styleUrl: './member-list.css'
})
export class MemberList implements OnInit {
  @Input() type: 'kurin' | 'group' | 'leadership' = 'group';
  @Input() leadershipType: 'kurin' | 'group' | 'kv' = 'group';
  @Input() typeKey = '';

  private readonly memberService = inject(MemberService);
  private readonly leadershipService = inject(LeadershipService);
  private readonly router = inject(Router);
  
  members: MemberDto[] = []; // TODO: Замінити на MemberLookupDto
  membersLookup: MemberLookupDto[] = [];
  leadership: LeadershipDto | null = null;
  selectedMember: MemberLookupDto | null = null;

  // TODO: Список проводу (leadership).
  // - [ ] Потрібно створити сервіс для leadership.
  // - [ ] Темплейт під leadership з беджами ролей.
  // - [ ] Кнопка налаштуваня проводу, де можна додати провід, змінити людей, каденції і т.д.

  ngOnInit(): void {
    if (this.type && this.typeKey) {
      switch (this.type) {
        case 'kurin':
          this.memberService.getAll(undefined, this.typeKey).subscribe({
            next: (members) => {
              for (const member of members) {
                this.membersLookup.push({
                  memberKey: member.memberKey,
                  firstName: member.firstName,
                  lastName: member.lastName,
                  middleName: member.middleName,
                });
              }
            }
          });
          break;
        case 'group':
          this.memberService.getAll(this.typeKey).subscribe({
            next: (members) => {
              for (const member of members) {
                this.membersLookup.push({
                  memberKey: member.memberKey,
                  firstName: member.firstName,
                  lastName: member.lastName,
                  middleName: member.middleName,
                });
              }
            }
          });
          break;
        case 'leadership':
          this.leadershipService.getLeadershipByTypeAndKey(this.leadershipType, this.typeKey).subscribe({
            next: (leadership) => {
              this.leadership = leadership;
              for (const history of leadership.leadershipHistories) {
                this.membersLookup.push(history.member);
              }
            }
          });
          break;
      }
    }
  }

  onMemberSelect(): void {
    if (this.selectedMember) {
      this.router.navigate(['/member', this.selectedMember.memberKey]);
    }
  }

  onLeadershipSettingsSelect(): void {
    if (this.leadership) {
      this.router.navigate(['/leadership', this.leadership.leadershipKey]);
    } else if (this.type && this.typeKey) {
      this.router.navigate(['/leadership/create', this.leadershipType, this.typeKey]);
    }
  }

  getMemberRole(memberKey: string): string | null {
    if (this.leadership) {
      const history = this.leadership.leadershipHistories.find(h => h.member.memberKey === memberKey);
      return history ? history.role : null;
    }
    return null;
  }
}
