import { Component, inject, Input, OnInit } from '@angular/core';
import { MemberService } from '../../services/member-service/member.service';
import { MemberDto } from '../../models/memberDto';
import { TableModule } from 'primeng/table';
import { InputIconModule } from 'primeng/inputicon';
import { IconFieldModule } from 'primeng/iconfield';
import { InputTextModule } from 'primeng/inputtext';
import { Router } from '@angular/router';

@Component({
  selector: 'app-member-list',
  imports: [TableModule, InputIconModule, IconFieldModule, InputTextModule],
  templateUrl: './member-list.html',
  styleUrl: './member-list.css'
})
export class MemberList implements OnInit {
  @Input() type: 'kurin' | 'group' = 'group';
  @Input() typeKey: string = '';

  private readonly memberService = inject(MemberService);
  private readonly router = inject(Router);
  
  members: MemberDto[] = [];
  selectedMember: MemberDto | null = null;

  ngOnInit(): void {
    if (this.type && this.typeKey) {
      switch (this.type) {
        case 'kurin':
          this.memberService.getAll(undefined, this.typeKey).subscribe({
            next: (members) => {
              this.members = members;
            }
          });
          break;
        case 'group':
          this.memberService.getAll(this.typeKey).subscribe({
            next: (members) => {
                this.members = members;
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
}
