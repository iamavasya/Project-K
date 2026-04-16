import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { catchError, forkJoin, of } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { AccordionModule } from 'primeng/accordion';
import { SkeletonModule } from 'primeng/skeleton';
import { TagModule } from 'primeng/tag';
import { MemberService } from '../common/services/member-service/member.service';
import { ProbesCatalogService } from '../common/services/probes-and-badges/probes-catalog.service';
import { MemberProgressService } from '../common/services/probes-and-badges/member-progress.service';
import { MemberDto } from '../common/models/memberDto';
import { GroupedProbeDto } from '../common/models/probes-and-badges/groupedProbeDto';
import { ProbeProgressDto } from '../common/models/probes-and-badges/probeProgressDto';
import { MemberProbeDetailPointRowView } from '../common/models/probes-and-badges/memberProbeDetailPointRowView';
import { buildMemberProbeDetailPointRows } from '../common/functions/memberProbeDetailsViewMapper.function';

interface ProbeDetailSectionView {
  sectionId: string;
  sectionCode: string;
  sectionTitle: string;
  points: MemberProbeDetailPointRowView[];
}

@Component({
  selector: 'app-member-probe-page',
  imports: [ButtonModule, AccordionModule, SkeletonModule, TagModule],
  templateUrl: './member-probe-page.component.html',
  styleUrl: './member-probe-page.component.css'
})
export class MemberProbePageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly memberService = inject(MemberService);
  private readonly probesCatalogService = inject(ProbesCatalogService);
  private readonly memberProgressService = inject(MemberProgressService);

  memberKey: string | null = null;
  probeId: string | null = null;

  member: MemberDto | null = null;
  groupedProbe: GroupedProbeDto | null = null;
  probeProgress: ProbeProgressDto | null = null;

  probeDetailPointRows: MemberProbeDetailPointRowView[] = [];
  probeSections: ProbeDetailSectionView[] = [];

  isLoading = false;
  loadFailed = false;
  hasPartialDataWarning = false;

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      this.memberKey = params.get('memberKey');
      this.probeId = params.get('probeId');
      this.loadData();
    });
  }

  goBackToMember(): void {
    if (!this.memberKey) {
      this.router.navigate(['/panel']);
      return;
    }

    this.router.navigate(['/member', this.memberKey]);
  }

  getProbePointSignerLabel(point: MemberProbeDetailPointRowView): string {
    if (!point.isSigned) {
      return 'Не підписано';
    }

    if (point.signedByName && point.signedByRole) {
      return `${point.signedByName} (${point.signedByRole})`;
    }

    if (point.signedByName) {
      return point.signedByName;
    }

    return 'Підписант не вказаний';
  }

  getProbePointSignedAtLabel(point: MemberProbeDetailPointRowView): string {
    if (!point.isSigned || !point.signedAtUtc) {
      return '—';
    }

    return this.formatDate(point.signedAtUtc);
  }

  getSectionPanelValue(section: ProbeDetailSectionView, sectionIndex: number): string {
    return section.sectionId || `section-${sectionIndex}`;
  }

  private loadData(): void {
    if (!this.memberKey || !this.probeId) {
      this.loadFailed = true;
      this.isLoading = false;
      this.hasPartialDataWarning = false;
      return;
    }

    this.isLoading = true;
    this.loadFailed = false;
    this.hasPartialDataWarning = false;

    forkJoin({
      member: this.memberService.getByKey(this.memberKey).pipe(
        catchError((error) => {
          console.error('Error fetching member for probe page:', error);
          return of(null);
        })
      ),
      groupedProbe: this.probesCatalogService.getGroupedById(this.probeId).pipe(
        catchError((error) => {
          console.error(`Error fetching grouped probe ${this.probeId}:`, error);
          return of(null);
        })
      ),
      progress: this.memberProgressService.getProbeProgress(this.memberKey, this.probeId).pipe(
        catchError((error) => {
          console.error(`Error fetching probe progress ${this.probeId}:`, error);
          return of(null);
        })
      )
    }).subscribe(({ member, groupedProbe, progress }) => {
      this.member = member;
      this.groupedProbe = groupedProbe;
      this.probeProgress = progress;

      this.probeDetailPointRows = buildMemberProbeDetailPointRows(groupedProbe, progress);
      this.probeSections = this.buildProbeSections(groupedProbe, this.probeDetailPointRows);

      this.loadFailed = groupedProbe === null;
      this.hasPartialDataWarning = groupedProbe !== null && progress === null;
      this.isLoading = false;
    });
  }

  private buildProbeSections(
    groupedProbe: GroupedProbeDto | null,
    pointRows: MemberProbeDetailPointRowView[]
  ): ProbeDetailSectionView[] {
    if (!groupedProbe) {
      return [];
    }

    return groupedProbe.sections.map(section => ({
      sectionId: section.id,
      sectionCode: section.code,
      sectionTitle: section.title,
      points: pointRows.filter(row => row.sectionId === section.id)
    }));
  }

  private formatDate(value: string): string {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
      return value;
    }

    return date.toLocaleDateString('uk-UA');
  }
}
