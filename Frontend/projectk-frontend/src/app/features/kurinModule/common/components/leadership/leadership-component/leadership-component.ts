import { Component } from '@angular/core';
import { MemberDto } from '../../../models/memberDto';
import { TableModule } from 'primeng/table';
import { SelectModule } from 'primeng/select';
import { MemberLookupDto } from '../../../models/requests/member/memberLookupDto';
import { LeadershipDto } from '../../../models/requests/leadership/leadershipDto';

@Component({
  selector: 'app-leadership-component',
  imports: [TableModule, SelectModule],
  templateUrl: './leadership-component.html',
  styleUrl: './leadership-component.css'
})
export class LeadershipComponent {
  members: MemberLookupDto[] = [
    {
      memberKey: '1',
      firstName: 'Іван',
      lastName: 'Іваненко',
      middleName: 'Іванович',
    },
    {
      memberKey: '2',
      firstName: 'Петро',
      lastName: 'Петренко',
      middleName: 'Петрович',
    },
  ];
  leaderships: LeadershipDto[] = [];
}
