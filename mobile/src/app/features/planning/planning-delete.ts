import { ActionSheetController } from '@ionic/angular';
import { PlanningSessionDto } from './planning.models';

/** The web's «Видалити це планування?», as a destructive sheet; true once the person confirms. */
export async function confirmPlanningDelete(
  sheets: ActionSheetController,
  session: PlanningSessionDto,
): Promise<boolean> {
  const sheet = await sheets.create({
    header: `Видалити планування «${session.name}»? Його не можна буде повернути.`,
    buttons: [
      { text: 'Видалити планування', role: 'destructive', data: 'delete' },
      { text: 'Скасувати', role: 'cancel' },
    ],
  });
  await sheet.present();
  const { data } = await sheet.onWillDismiss();
  return data === 'delete';
}
