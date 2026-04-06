import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { MemberLookupDto } from '../../models/requests/member/memberLookupDto';

@Component({
  selector: 'app-mini-member-card',
  imports: [CommonModule, ButtonModule, TagModule],
  templateUrl: './mini-member-card.html',
  styleUrl: './mini-member-card.css'
})
export class MiniMemberCardComponent {
  @Input({ required: true }) member!: MemberLookupDto;
  @Output() navigate = new EventEmitter<MemberLookupDto>();

  onNavigate(): void {
    this.navigate.emit(this.member);
  }
}