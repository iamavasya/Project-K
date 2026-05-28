import { ComponentFixture, TestBed } from '@angular/core/testing';
import { GroupPanelComponent } from './group-panel.component';
import { ActivatedRoute, convertToParamMap, ParamMap, Router } from '@angular/router';
import { BehaviorSubject, of } from 'rxjs';
import { MemberService } from '../common/services/member-service/member.service';
import { GroupService } from '../common/services/group-service/group.service';
import { LeadershipService } from '../common/services/leadership-service/leadership-service';
import { GroupDto } from '../common/models/groupDto';
import { LeadershipDto } from '../common/models/requests/leadership/leadershipDto';
import { MemberDto } from '../common/models/memberDto';
import { EntityService } from '../../authModule/services/entity.service';
import { PermissionService } from '../../authModule/services/permission.service';

describe('GroupPanelComponent', () => {
  let fixture: ComponentFixture<GroupPanelComponent>;
  let component: GroupPanelComponent;

  let memberServiceSpy: jasmine.SpyObj<MemberService>;
  let groupServiceSpy: jasmine.SpyObj<GroupService>;
  let leadershipServiceSpy: jasmine.SpyObj<LeadershipService>;
  let entityServiceSpy: jasmine.SpyObj<EntityService>;
  let permissionServiceSpy: jasmine.SpyObj<PermissionService>;
  let routerSpy: jasmine.SpyObj<Router>;
  let paramMapSubject: BehaviorSubject<ParamMap>;

  const groupKey = 'group1';
  const group: GroupDto = {
    groupKey,
    name: 'Test Group',
    description: 'Test group description',
    silhouetteUrl: null,
    kurinKey: 'kurin1',
    kurinNumber: 1
  };

  const leadership: LeadershipDto = {
    leadershipKey: 'l1',
    startDate: '2026-01-01',
    endDate: null,
    leadershipHistories: []
  };

  beforeEach(async () => {
    memberServiceSpy = jasmine.createSpyObj('MemberService', ['getAll', 'getMentorCandidates']);
    groupServiceSpy = jasmine.createSpyObj('GroupService', ['getByKey', 'exists', 'getMentors', 'assignMentor', 'revokeMentor', 'update', 'uploadSilhouette', 'deleteSilhouette']);
    leadershipServiceSpy = jasmine.createSpyObj('LeadershipService', ['getLeadershipByTypeAndKey']);
    entityServiceSpy = jasmine.createSpyObj('EntityService', ['checkEntityAccess']);
    permissionServiceSpy = jasmine.createSpyObj('PermissionService', ['canManageMentors', 'canSetupLeadership']);
    routerSpy = jasmine.createSpyObj('Router', ['navigate']);
    paramMapSubject = new BehaviorSubject(convertToParamMap({ groupKey }));

    groupServiceSpy.exists.and.returnValue(of(true));
    groupServiceSpy.getByKey.and.returnValue(of(group));
    groupServiceSpy.getMentors.and.returnValue(of([]));
    groupServiceSpy.assignMentor.and.returnValue(of({}));
    groupServiceSpy.revokeMentor.and.returnValue(of({}));
    groupServiceSpy.update.and.returnValue(of(group));
    groupServiceSpy.uploadSilhouette.and.returnValue(of(group));
    groupServiceSpy.deleteSilhouette.and.returnValue(of(group));
    memberServiceSpy.getAll.and.returnValue(of([]));
    memberServiceSpy.getMentorCandidates.and.returnValue(of([]));
    leadershipServiceSpy.getLeadershipByTypeAndKey.and.returnValue(of(leadership));
    entityServiceSpy.checkEntityAccess.and.returnValue(of(true));
    permissionServiceSpy.canManageMentors.and.returnValue(true);
    permissionServiceSpy.canSetupLeadership.and.returnValue(true);

    await TestBed.configureTestingModule({
      imports: [GroupPanelComponent],
      providers: [
        { provide: MemberService, useValue: memberServiceSpy },
        { provide: GroupService, useValue: groupServiceSpy },
        { provide: LeadershipService, useValue: leadershipServiceSpy },
        { provide: EntityService, useValue: entityServiceSpy },
        { provide: PermissionService, useValue: permissionServiceSpy },
        { provide: Router, useValue: routerSpy },
        { provide: ActivatedRoute, useValue: { paramMap: paramMapSubject.asObservable() } }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(GroupPanelComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load group details on init', () => {
    expect(groupServiceSpy.getByKey).toHaveBeenCalledWith(groupKey);
    expect(component.group).toEqual(group);
    expect(component.profileForm.get('description')?.value).toBe('Test group description');
  });

  it('should navigate to panel if group does not exist', () => {
    groupServiceSpy.exists.and.returnValue(of(false));
    component.refreshData();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/panel'], { replaceUrl: true });
  });

  it('onMemberCreate should navigate to upsert route', () => {
    component.onMemberCreate();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/group', groupKey, 'member', 'upsert']);
  });

  it('onMemberSelect should navigate to member card', () => {
    component.selectedMember = { memberKey: 'm1' } as unknown as MemberDto;
    component.onMemberSelect();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/member', 'm1']);
  });

  it('openMentorDialog should load management data and open dialog', () => {
    component.openMentorDialog();

    expect(memberServiceSpy.getMentorCandidates).toHaveBeenCalledWith(group.kurinKey);
    expect(groupServiceSpy.getMentors).toHaveBeenCalledWith(groupKey);
    expect(component.mentorDialogVisible).toBeTrue();
  });

  it('saveProfile should update group description', () => {
    const updated = { ...group, description: 'Updated description' };
    groupServiceSpy.update.and.returnValue(of(updated));

    component.startProfileEdit();
    component.profileForm.patchValue({ description: '  Updated description  ' });
    component.saveProfile();

    expect(groupServiceSpy.update).toHaveBeenCalledWith(groupKey, {
      name: 'Test Group',
      description: 'Updated description'
    });
    expect(component.group).toEqual(updated);
    expect(component.profileEditMode).toBeFalse();
  });

  it('should collapse long description until toggled', () => {
    component.group = {
      ...group,
      description: 'A'.repeat(component.descriptionCollapseLimit + 1)
    };

    expect(component.isDescriptionLong).toBeTrue();
    expect(component.descriptionExpanded).toBeFalse();

    component.toggleDescription();

    expect(component.descriptionExpanded).toBeTrue();
  });

  it('groupEditMenuItems should expose permitted actions in one menu', () => {
    component.group = group;
    component.canEditGroupProfile = true;
    component.canCreateMembers = true;
    permissionServiceSpy.canManageMentors.and.returnValue(true);

    expect(component.groupEditMenuItems.map(item => item.label)).toEqual([
      'Редагувати профіль',
      'Додати учасника',
      'Виховники',
      'Завантажити сильветку'
    ]);

    component.group = { ...group, silhouetteUrl: 'group-silhouettes/2026/05/27/test.png' };
    expect(component.groupEditMenuItems.map(item => item.label)).toEqual([
      'Редагувати профіль',
      'Додати учасника',
      'Виховники',
      'Замінити сильветку',
      'Видалити сильветку'
    ]);
  });

  it('onSilhouetteSelected should open editor for valid image', () => {
    const file = new File(['image'], 'silhouette.png', { type: 'image/png' });
    const input = document.createElement('input');
    Object.defineProperty(input, 'files', { value: [file] });

    component.canEditGroupProfile = true;
    component.onSilhouetteSelected({ target: input } as unknown as Event);

    expect(groupServiceSpy.uploadSilhouette).not.toHaveBeenCalled();
    expect(component.silhouetteDialogVisible).toBeTrue();
    expect(component.silhouetteImageFile).toBe(file);
    expect(component.silhouetteError).toBeNull();
  });

  it('onSilhouetteSelected should reject unsupported image type', () => {
    const file = new File(['image'], 'silhouette.gif', { type: 'image/gif' });
    const input = document.createElement('input');
    Object.defineProperty(input, 'files', { value: [file] });

    component.canEditGroupProfile = true;
    component.onSilhouetteSelected({ target: input } as unknown as Event);

    expect(groupServiceSpy.uploadSilhouette).not.toHaveBeenCalled();
    expect(component.silhouetteError).toBe('Підтримуються лише PNG, JPEG або WebP.');
  });

  it('uploadOriginalSilhouette should send selected file and close editor', () => {
    const updated = { ...group, silhouetteUrl: 'group-silhouettes/2026/05/27/test.png' };
    const file = new File(['image'], 'silhouette.png', { type: 'image/png' });
    groupServiceSpy.uploadSilhouette.and.returnValue(of(updated));
    component.silhouetteImageFile = file;
    component.silhouetteDialogVisible = true;

    component.uploadOriginalSilhouette();

    expect(groupServiceSpy.uploadSilhouette).toHaveBeenCalledWith(groupKey, file);
    expect(component.group).toEqual(updated);
    expect(component.silhouetteDialogVisible).toBeFalse();
    expect(component.silhouetteSaving).toBeFalse();
  });

  it('uploadProcessedSilhouette should send processed png file', () => {
    const updated = { ...group, silhouetteUrl: 'group-silhouettes/2026/05/27/test.png' };
    const sourceFile = new File(['image'], 'source.jpg', { type: 'image/jpeg' });
    const processed = new Blob(['png'], { type: 'image/png' });
    groupServiceSpy.uploadSilhouette.and.returnValue(of(updated));
    component.silhouetteImageFile = sourceFile;
    component.silhouetteProcessedBlob = processed;
    component.silhouetteDialogVisible = true;

    component.uploadProcessedSilhouette();

    const uploaded = groupServiceSpy.uploadSilhouette.calls.mostRecent().args[1] as File;
    expect(groupServiceSpy.uploadSilhouette).toHaveBeenCalledWith(groupKey, jasmine.any(File));
    expect(uploaded.name).toBe('source.png');
    expect(uploaded.type).toBe('image/png');
    expect(component.group).toEqual(updated);
  });

  it('uploadProcessedSilhouette should require cropped image', () => {
    component.uploadProcessedSilhouette();

    expect(groupServiceSpy.uploadSilhouette).not.toHaveBeenCalled();
    expect(component.silhouetteError).toBe('Оберіть область зображення перед завантаженням.');
  });

  it('deleteSilhouette should delete existing image and update group', () => {
    const withSilhouette = { ...group, silhouetteUrl: 'group-silhouettes/2026/05/27/test.png' };
    const updated = { ...group, silhouetteUrl: null };
    groupServiceSpy.deleteSilhouette.and.returnValue(of(updated));
    component.group = withSilhouette;
    component.canEditGroupProfile = true;

    component.deleteSilhouette();

    expect(groupServiceSpy.deleteSilhouette).toHaveBeenCalledWith(groupKey);
    expect(component.group).toEqual(updated);
    expect(component.silhouetteSaving).toBeFalse();
  });

  it('saveMentorAssignments should call assign and revoke based on diff', () => {
    component.initialMentorUserKeys = ['u1', 'u2'];
    component.selectedMentorUserKeys = ['u2', 'u3'];
    component.mentorDialogVisible = true;

    component.saveMentorAssignments();

    expect(groupServiceSpy.assignMentor).toHaveBeenCalledWith(groupKey, 'u3');
    expect(groupServiceSpy.revokeMentor).toHaveBeenCalledWith(groupKey, 'u1');
    expect(component.mentorDialogVisible).toBeFalse();
  });
});
