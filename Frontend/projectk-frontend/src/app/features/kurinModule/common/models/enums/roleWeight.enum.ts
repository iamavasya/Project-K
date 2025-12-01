export enum RoleWeight {
  NotImportant = 0,
  Low = 1.0,
  Medium = 3.0,
  High = 5.0
}

export const RoleWeightOptions = [
  { label: 'Не важливий (0)', value: RoleWeight.NotImportant },
  { label: 'Низький (1.0)', value: RoleWeight.Low },
  { label: 'Середній (3.0)', value: RoleWeight.Medium },
  { label: 'Високий (5.0)', value: RoleWeight.High },
];