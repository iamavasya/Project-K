import { MemberSkillItemView } from './member-skill-item.view';

export interface MemberSkillsSummaryView {
  recentConfirmed: MemberSkillItemView[];
  pendingConfirmation: MemberSkillItemView[];
  orderedPreview: MemberSkillItemView[];
}