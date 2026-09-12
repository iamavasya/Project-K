export enum RoleWeight {
  NotImportant = 0,
  Low = 1.0,
  Medium = 3.0,
  High = 5.0
}

export const RoleWeightOptions = [
  { label: 'Неважливий', value: RoleWeight.NotImportant },
  { label: 'Низький', value: RoleWeight.Low },
  { label: 'Середній', value: RoleWeight.Medium },
  { label: 'Високий', value: RoleWeight.High },
];