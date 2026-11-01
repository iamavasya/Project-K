import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import { ButtonModule } from '@openng/optimus-ui/button';
import { PlastLevelHistoryDto } from '../../../../models/plast-level-history.dto';
import { HonoursService } from '../../../../services/honours-service/honours.service';
import { SENIOR_LADDER, YOUTH_LADDER, degreeRows } from './member-degrees.function';

/**
 * Найвищий ступінь людини на її драбині — УПЮ на юнацькій картці, УСП/УПС на старшій — великим планом,
 * з відзнакою з каталогу. Усі ступені показує пластовий життєпис. Вносять їх у формі картки: сама людина або провід.
 */
@Component({
  selector: 'app-member-degrees-tile',
  imports: [ButtonModule, DatePipe],
  templateUrl: './member-degrees-tile.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './member-degrees-tile.css'
})
export class MemberDegreesTileComponent {
  private readonly honours = toSignal(inject(HonoursService).all(), { initialValue: [] });

  readonly histories = input<PlastLevelHistoryDto[]>([]);
  readonly senior = input(false);
  readonly canEdit = input(false);
  readonly edit = output<void>();

  readonly rows = computed(() => degreeRows(this.histories(), this.senior() ? SENIOR_LADDER : YOUTH_LADDER, this.honours()));
  /** Only the highest ступінь on the ladder: the whole way is the пластовий життєпис's to show. */
  readonly highest = computed(() => this.rows().at(-1) ?? null);
}
