import { CanDeactivateFn } from '@angular/router';
import { Observable } from 'rxjs';

/** A page that may hold edits nobody saved yet, and asks before they are thrown away. */
export interface LeavesWithUnsavedChanges {
  confirmLeave(): boolean | Promise<boolean> | Observable<boolean>;
}

export const unsavedChangesGuard: CanDeactivateFn<LeavesWithUnsavedChanges> =
  component => component?.confirmLeave?.() ?? true;
