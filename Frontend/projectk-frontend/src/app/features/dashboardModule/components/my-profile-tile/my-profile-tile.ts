import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ButtonModule } from '@openng/optimus-ui/button';
import { SkeletonModule } from '@openng/optimus-ui/skeleton';
import { TagModule } from '@openng/optimus-ui/tag';
import { MemberDto } from '../../../kurinModule/models/member.dto';
import { initials } from '../../functions/greeting.function';

/** Who the person is, in one glance, and the way to their card. */
@Component({
  selector: 'app-my-profile-tile',
  imports: [RouterLink, ButtonModule, SkeletonModule, TagModule],
  templateUrl: './my-profile-tile.html',
  styleUrl: './my-profile-tile.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MyProfileTileComponent {
  readonly member = input<MemberDto | null>(null);
  readonly loading = input(false);

  readonly fullName = computed(() => {
    const m = this.member();
    return m ? `${m.firstName} ${m.lastName}`.trim() : '';
  });

  readonly initials = computed(() => initials(this.member()?.firstName, this.member()?.lastName));
}
