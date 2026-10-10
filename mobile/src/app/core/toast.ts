import { Injectable, inject } from '@angular/core';
import { ToastController } from '@ionic/angular';

/** Short confirmations and errors, at the top where iOS and Material both put transient notes. */
@Injectable({ providedIn: 'root' })
export class Toasts {
  private readonly controller = inject(ToastController);

  async show(message: string, color?: 'danger'): Promise<void> {
    const toast = await this.controller.create({ message, duration: 2500, position: 'top', color });
    await toast.present();
  }
}
