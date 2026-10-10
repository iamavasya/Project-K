import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { Api } from '../../core/api';
import { apiUrl } from '../../runtime-config';
import {
  BadgeCatalogItemDto,
  BadgeProgressDto,
  GroupDto,
  GroupedProbeDto,
  KurinDto,
  LeadershipDto,
  MemberAwardDto,
  MemberDto,
  MemberDuesDto,
  MemberLookupDto,
  MembershipDto,
  MentorAssignmentDto,
  OwnProfileForm,
  ProbeProgressDto,
  ProbeSummaryDto,
  UpsertAwardRequest,
} from './kurin.models';

/**
 * The endpoints the web's kurinModule services call (kurin, group, member, leadership,
 * member-progress, catalogs, member-award) and the one dues read the card makes, as promises.
 */
@Injectable({ providedIn: 'root' })
export class KurinApi {
  private readonly api = inject(Api);

  kurin(kurinKey: string): Promise<KurinDto> {
    return this.api.get(`kurin/${kurinKey}`);
  }

  groups(kurinKey: string): Promise<GroupDto[]> {
    return this.api.get('group/groups', { kurinKey });
  }

  group(groupKey: string): Promise<GroupDto> {
    return this.api.get(`group/${groupKey}`);
  }

  kurinMembers(kurinKey: string): Promise<MemberLookupDto[]> {
    return this.api.get(`member/kurins/${kurinKey}/members`);
  }

  groupMembers(groupKey: string): Promise<MemberLookupDto[]> {
    return this.api.get(`member/groups/${groupKey}/members`);
  }

  kvMembers(kurinKey: string): Promise<MemberLookupDto[]> {
    return this.api.get(`member/members/kv/${kurinKey}`);
  }

  mentorAssignments(kurinKey: string): Promise<MentorAssignmentDto[]> {
    return this.api.get(`group/groups/${kurinKey}/mentor-assignments`);
  }

  leadership(type: 'kurin' | 'group', key: string): Promise<LeadershipDto> {
    return this.api.get(`leadership/type/${type}/${key}`);
  }

  /** The гуртки whose box this person keeps (web dues getReadableGroups). */
  duesGroups(kurinKey: string): Promise<{ groupKey: string; groupName: string }[]> {
    return this.api.get(`kurin/${kurinKey}/dues/groups`);
  }

  member(memberKey: string): Promise<MemberDto> {
    return this.api.get(`member/${memberKey}`);
  }

  memberships(memberKey: string): Promise<MembershipDto[]> {
    return this.api.get(`member/${memberKey}/dossier/memberships`);
  }

  /** A youth may read only their own; for anyone else the server refuses and the card shows none. */
  dues(memberKey: string): Promise<MemberDuesDto> {
    return this.api.get(`member/${memberKey}/dues`);
  }

  /** The server's own answer whether this person may change the card (web EntityService). */
  async canUpdateMember(memberKey: string): Promise<boolean> {
    const answer = await this.api.post<unknown>('auth/check-access', { entityType: 'member', entityKey: memberKey, action: 'Update' });
    return answer === true;
  }

  badgeCatalog(): Promise<BadgeCatalogItemDto[]> {
    return this.api.get('catalog/badges', { take: 1000 });
  }

  badgeProgress(memberKey: string): Promise<BadgeProgressDto[]> {
    return this.api.get(`member/${memberKey}/badges/progress`);
  }

  submitBadge(memberKey: string, badgeId: string): Promise<BadgeProgressDto> {
    return this.api.post(`member/${memberKey}/badges/${badgeId}/submit`, { note: null });
  }

  reviewBadge(memberKey: string, badgeId: string, isApproved: boolean): Promise<BadgeProgressDto> {
    return this.api.post(`member/${memberKey}/badges/${badgeId}/review`, { isApproved, note: null });
  }

  probes(): Promise<ProbeSummaryDto[]> {
    return this.api.get('catalog/probes');
  }

  groupedProbe(probeId: string): Promise<GroupedProbeDto> {
    return this.api.get(`catalog/probes/${probeId}/grouped`);
  }

  probeProgress(memberKey: string, probeId: string): Promise<ProbeProgressDto> {
    return this.api.get(`member/${memberKey}/probes/${probeId}/progress`);
  }

  signPoint(memberKey: string, probeId: string, pointId: string, sign: boolean): Promise<ProbeProgressDto> {
    return this.api.put(`member/${memberKey}/probes/${probeId}/points/${pointId}/${sign ? 'sign' : 'unsign'}`, { note: null });
  }

  closeProbe(memberKey: string, probeId: string): Promise<ProbeProgressDto> {
    return this.api.put(`member/${memberKey}/probes/${probeId}/progress/status`, { status: 'Completed', note: null });
  }

  saveAward(memberKey: string, request: UpsertAwardRequest): Promise<MemberAwardDto> {
    return this.api.post(`member/${memberKey}/awards`, request);
  }

  deleteAward(memberKey: string, awardKey: string): Promise<void> {
    return this.api.delete(`member/${memberKey}/awards/${awardKey}`);
  }

  reviewAward(memberKey: string, awardKey: string, isApproved: boolean): Promise<MemberAwardDto> {
    return this.api.post(`member/${memberKey}/awards/${awardKey}/review`, { isApproved, note: null });
  }

  /**
   * One's own card, as the web's upsert-member sends it: multipart, the email as it is (a youth
   * cannot move it) and the ступені as they are, since the server rewrites them from the list.
   */
  updateOwnProfile(member: MemberDto, form: OwnProfileForm): Promise<MemberDto> {
    const body = new FormData();
    body.append('firstName', form.firstName);
    body.append('middleName', form.middleName);
    body.append('lastName', form.lastName);
    body.append('email', member.email ?? '');
    body.append('phoneNumber', form.phoneNumber);
    body.append('dateOfBirth', form.dateOfBirth);
    (member.plastLevelHistories ?? [])
      .filter((history) => history.dateAchieved)
      .forEach((history, index) => {
        for (const [key, value] of Object.entries(history)) {
          if (value === null || value === undefined) continue;
          const text = key === 'dateAchieved' ? String(value).slice(0, 10) : String(value);
          body.append(`plastLevelHistories[${index}].${key}`, text);
        }
      });
    return this.api.put(`member/${member.memberKey}`, body);
  }
}

/**
 * Badge and award pictures sit behind the API's auth, so they come as blobs with the token and are
 * shown from object URLs (web BadgeImageBlobService). A picture that fails stays null: the row
 * shows its icon instead.
 */
@Injectable({ providedIn: 'root' })
export class ProtectedImages {
  private readonly http = inject(HttpClient);
  private readonly base = apiUrl();
  private readonly origin = originOf(this.base);
  private readonly urls = signal<Record<string, string | null>>({});
  private readonly pending = new Set<string>();

  /** A catalogue badge's picture (its path is relative to the API origin's /badges_images). */
  badge(imagePath: string | null | undefined): string | null {
    if (!imagePath) return null;
    if (/^(https?:|data:)/.test(imagePath)) return this.resolve(imagePath);
    const path = imagePath.replace(/^\//, '');
    return this.resolve(`${this.origin}/${path.startsWith('badges_images/') ? path : `badges_images/${path}`}`);
  }

  /** An award's medal, coloured once confirmed. */
  award(award: MemberAwardDto, level: number, confirmed: boolean): string | null {
    return this.resolve(award.imageUrl || `${this.base}/awards/images/${level}?colored=${confirmed}`);
  }

  private resolve(url: string): string | null {
    const known = this.urls()[url];
    if (known !== undefined) return known;
    if (!this.pending.has(url)) {
      this.pending.add(url);
      firstValueFrom(this.http.get(url, { responseType: 'blob' })).then(
        (blob) => this.urls.update((all) => ({ ...all, [url]: URL.createObjectURL(blob) })),
        () => this.urls.update((all) => ({ ...all, [url]: null })),
      );
    }
    return null;
  }
}

function originOf(url: string): string {
  try {
    return new URL(url).origin;
  } catch {
    return '';
  }
}
