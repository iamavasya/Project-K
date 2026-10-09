/** Де стоїть акаунт людини. Сервер віддає його лише тим, хто бачить приватні дані цієї людини. */
export enum AccountStatus {
  RegisteredInactive = 'RegisteredInactive',
  PendingActivation = 'PendingActivation',
  Active = 'Active',
  Suspended = 'Suspended',
  Archived = 'Archived'
}
