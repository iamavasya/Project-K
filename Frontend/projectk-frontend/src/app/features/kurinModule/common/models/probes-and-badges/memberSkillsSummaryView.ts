import { MemberSkillItemView } from './memberSkillItemView';

export interface MemberSkillsSummaryView {
  recentConfirmed: MemberSkillItemView[];
  pendingConfirmation: MemberSkillItemView[];
  orderedPreview: MemberSkillItemView[];
}