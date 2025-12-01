import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormArray, FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

// PrimeNG Imports (v18+)
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { DatePickerModule } from 'primeng/datepicker';
import { SelectModule } from 'primeng/select';
import { ButtonModule } from 'primeng/button';
import { PanelModule } from 'primeng/panel';
import { DividerModule } from 'primeng/divider';
import { FloatLabelModule } from 'primeng/floatlabel';

import { RoleWeight, RoleWeightOptions } from '../common/models/enums/roleWeight.enum';

import { PlanningService } from '../common/services/planning-service/planning-service';
import { MemberService } from '../common/services/member-service/member.service';


@Component({
  selector: 'app-create-planning',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, 
    InputTextModule, InputNumberModule, DatePickerModule, SelectModule, 
    ButtonModule, PanelModule, DividerModule, FloatLabelModule
  ],
  template: `
    <div class="max-w-5xl mx-auto p-6">
      <div class="flex items-center gap-4 mb-6">
        <p-button icon="pi pi-arrow-left" [text]="true" (click)="goBack()" />
        <h1 class="text-2xl font-bold">Нове планування</h1>
      </div>

      <form [formGroup]="form" (ngSubmit)="submit()">
        
        <p-panel header="Основні налаштування" styleClass="mb-6">
          <div class="grid grid-cols-1 md:grid-cols-3 gap-6">
            
            <div class="flex flex-col gap-2">
              <label class="font-semibold">Назва сесії</label>
              <input pInputText formControlName="name" placeholder="Напр. Літо 2025" />
            </div>

            <div class="flex flex-col gap-2">
              <label class="font-semibold">Тривалість (днів)</label>
              <p-input-number formControlName="durationDays" [min]="1" [max]="30" [showButtons]="true" />
            </div>

            <div class="flex flex-col gap-2">
              <label class="font-semibold">Вікно пошуку</label>
              <p-datepicker 
                formControlName="searchRange" 
                selectionMode="range" 
                [showIcon]="true"
                dateFormat="dd.mm.yy"
                placeholder="Виберіть період" />
            </div>
          </div>
        </p-panel>

        <p-panel header="Учасники та Зайнятість">
          <div formArrayName="participants" class="flex flex-col gap-4">
            
            <div *ngFor="let p of participantsArray.controls; let i = index" [formGroupName]="i" 
                 class="p-4 border border-slate-200 rounded-lg bg-white shadow-sm hover:shadow-md transition-shadow">
              
              <div class="flex flex-wrap md:flex-nowrap gap-4 items-start">
                
                <div class="w-full md:w-1/4 flex flex-col gap-3">
                  <div class="font-bold text-lg text-slate-800">
                    {{ p.get('fullName')?.value }}
                  </div>
                  
                  <div class="flex flex-col gap-1">
                    <label class="text-sm text-slate-500">Важливість голосу</label>
                    <p-select 
                      formControlName="roleWeight" 
                      [options]="weightOptions" 
                      optionLabel="label" 
                      optionValue="value" 
                      class="w-full" />
                  </div>
                </div>

                <div class="hidden md:block w-px bg-slate-200 self-stretch mx-2"></div>

                <div class="flex-1">
                  <div class="flex justify-between items-center mb-2">
                    <label class="text-sm font-semibold text-slate-600">
                      Коли ця людина ЗАЙНЯТА?
                    </label>
                    <p-button 
                      label="Додати період" 
                      icon="pi pi-plus" 
                      size="small" 
                      [outlined]="true" 
                      (click)="addBusyRange(i)" />
                  </div>

                  <div formArrayName="busyRanges" class="flex flex-col gap-2">
                    <div *ngFor="let range of getBusyRanges(i).controls; let j = index" [formGroupName]="j" 
                         class="flex items-center gap-2">
                      
                      <p-datepicker 
                        formControlName="range" 
                        selectionMode="range" 
                        [readonlyInput]="true"
                        placeholder="Виберіть дати"
                        appendTo="body"
                        [style]="{'width':'240px'}" />
                      
                      <p-button 
                        icon="pi pi-trash" 
                        severity="danger" 
                        [text]="true" 
                        (click)="removeBusyRange(i, j)" />
                    </div>
                    
                    <div *ngIf="getBusyRanges(i).length === 0" class="text-sm text-slate-400 italic py-2">
                      Зазначте дати, якщо людина має плани...
                    </div>
                  </div>
                </div>

              </div>
            </div>

          </div>
        </p-panel>

        <div class="flex justify-end gap-4 mt-6 pb-10">
          <p-button label="Скасувати" severity="secondary" (click)="goBack()" />
          <p-button label="Створити та Розрахувати" icon="pi pi-cog" type="submit" [loading]="loading" />
        </div>
      </form>
    </div>
  `
})
export class CreatePlanningComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly planningService = inject(PlanningService);
  private readonly memberService = inject(MemberService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  kurinKey = '';
  loading = false;
  weightOptions = RoleWeightOptions;

  form = this.fb.group({
    name: ['', Validators.required],
    kurinKey: ['', Validators.required],
    durationDays: [10, [Validators.required, Validators.min(1)]],
    searchRange: new FormControl<Date[] | null>(null, Validators.required),
    participants: this.fb.array([])
  });

  get participantsArray() {
    return this.form.get('participants') as FormArray;
  }

  ngOnInit() {
    this.route.paramMap.subscribe(params => {
      this.kurinKey = params.get('kurinKey')!;
      if (this.kurinKey) {
        this.form.patchValue({ kurinKey: this.kurinKey });
        this.setDefaultDates();
        this.loadMembers();
      }
    });
  }

  setDefaultDates() {
    const currentYear = new Date().getFullYear();
    const start = new Date(currentYear, 5, 1); // Червень (місяці з 0)
    const end = new Date(currentYear, 7, 31);  // Серпень
    
    this.form.get('searchRange')?.setValue([start, end]);
    this.form.get('name')?.setValue(`Табір ${currentYear}`);
  }

  loadMembers() {
    this.loading = true;
    this.memberService.getKVMembers(this.kurinKey).subscribe({
      next: (members) => {
        members.forEach(m => {
          this.participantsArray.push(this.createParticipantGroup(m));
        });
        this.loading = false;
      },
      error: () => this.loading = false
    });
  }

  createParticipantGroup(member: any): FormGroup {
    return this.fb.group({
      memberKey: [member.memberKey], // ID
      fullName: [`${member.firstName} ${member.lastName}`],
      roleWeight: [RoleWeight.Medium],
      busyRanges: this.fb.array([])
    });
  }

  // --- Date Range Logic ---

  getBusyRanges(participantIndex: number): FormArray {
    return this.participantsArray.at(participantIndex).get('busyRanges') as FormArray;
  }

  addBusyRange(participantIndex: number) {
    const rangeGroup = this.fb.group({
      range: [null, Validators.required]
    });
    this.getBusyRanges(participantIndex).push(rangeGroup);
  }

  removeBusyRange(pIndex: number, rIndex: number) {
    this.getBusyRanges(pIndex).removeAt(rIndex);
  }

  // --- Submit ---

  submit() {
    if (this.form.invalid) return;

    this.loading = true;
    const formVal = this.form.value;

    const payload = {
      name: formVal.name,
      kurinKey: formVal.kurinKey,
      durationDays: formVal.durationDays,
      searchStart: formVal.searchRange![0],
      searchEnd: formVal.searchRange![1],
      participants: formVal.participants?.map((p: any) => ({
        memberKey: p.memberKey,
        fullName: p.fullName,
        roleWeight: p.roleWeight,
        busyRanges: p.busyRanges.map((r: any) => ({
          start: r.range[0],
          end: r.range[1]
        })).filter((r: any) => r.start && r.end)
      }))
    };

    this.planningService.createSession(payload).subscribe({
      next: () => {
        this.loading = false;
        this.router.navigate(['/planning', this.kurinKey]);
      },
      error: () => this.loading = false
    });
  }

  goBack() {
    this.router.navigate(['/planning']);
  }
}